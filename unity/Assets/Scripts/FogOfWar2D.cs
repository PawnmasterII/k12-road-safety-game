using System.Collections.Generic;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 R4 — GGD FogOfWar port. Fixes on top of R3:
    ///   D. THE DARK ZONE WASN'T TRANSPARENT-CONTROLLED: R3 never set the fog
    ///      material's COLOR, so the dark complement rendered opaque white-ish and
    ///      only the vision hole showed the world. R4 tints via VERTEX COLORS:
    ///      vision-edge band alpha 0 -> full DimColor alpha, so darkness fades in
    ///      over EdgeWidth units and the dark zone is translucent (map shows
    ///      through, dimmed) exactly like GGD's fog.
    ///   E. CHOPPY EDGE: recompute now runs EVERY frame (120-160 raycasts is
    ///      trivial; every-2nd-frame made the boundary lag a frame behind the
    ///      player and visibly stutter), ray count raised to 160, and the hard
    ///      geometric cut is softened by the alpha band above.
    ///   F. ENTITY HIDING COVERS EVERYTHING: any component with a FogEntity
    ///      (NPCs, cars, ...) fades out in the dark zone via FogEntity (smooth
    ///      fade, not the R3 hard enable/disable that only covered DialogNPC).
    ///
    /// R3 fixes (still in effect): walls block sight via physics layer
    /// "Collision"; dark-complement mesh construction (overlay+light-mesh can't
    /// punch a hole); hinge vertices at corners; single sorting layer "Default"
    /// (order 20: above world <=10, below panels 40+).
    /// GGD reference (dump.cs 238551): accuracy, fieldOfView (360 default;
    /// 60-140 = flashlight mode), baseViewDistance, layerMask walls,
    /// recomputeMeshEveryXFrame.
    /// </summary>
    public class FogOfWar2D : MonoBehaviour
    {
        [Header("GGD tweakables")]
        public int Accuracy = 160;             // GGD: accuracy (1..720)
        [Range(10f, 360f)]
        public float FieldOfView = 360f;       // full circle by default (GGD); 60-140 = flashlight
        public float BaseViewDistance = 7f;    // GGD: baseViewDistance
        public int RecomputeEveryXFrame = 1;   // GGD: recomputeMeshEveryXFrame (1 = every frame = smooth)

        [Header("Look")]
        public Color DimColor = new Color(0.02f, 0.04f, 0.08f, 0.62f);  // outside vision
        public float EdgeWidth = 1.4f;         // penumbra band: 0 alpha at vision edge -> full here

        public bool Enabled2 { get; private set; } = true;

        private Transform _origin;
        private Mesh _fogMesh;                 // THE mesh: whole map minus vision polygon
        private MeshFilter _mf;
        private GameObject _fogGo;
        private int _frame;
        private bool _booted;
        private ContactFilter2D _wallFilter;
        private readonly RaycastHit2D[] _hitBuf = new RaycastHit2D[1];

        public int LastVertexCount { get; private set; }
        public string LastShader { get; private set; } = "n/a";
        public int LastHingeCount { get; private set; }

        private void Start() => Boot();

        /// <summary>Self-bootstraps at PLAY start (scene reload wipes runtime refs).</summary>
        private void Boot()
        {
            if (_booted) return;
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            _origin = player.transform;
            _wallFilter = new ContactFilter2D
            {
                useTriggers = false,       // GGD: walls block sight, trigger zones never do
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Collision"),
            };
            _frame = 0;

            _fogGo = new GameObject("FogMesh");
            _mf = _fogGo.AddComponent<MeshFilter>();
            var mr = _fogGo.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Sprites/Default");   // unlit — this project has no lights
            mr.material = new Material(shader);
            LastShader = shader != null ? shader.name : "NULL";
            _fogMesh = new Mesh { name = "FogComplementMesh" };
            _fogMesh.MarkDynamic();
            _mf.sharedMesh = _fogMesh;
            _fogMesh.vertices = new Vector3[0];
            _fogMesh.triangles = new int[0];
            mr.sortingLayerName = "Default";
            mr.sortingOrder = 20;                  // above world (<=10), below panels (40+)
            _booted = true;
        }

        private void LateUpdate()
        {
            if (!_booted) Boot();
            if (_origin == null || _mf == null) return;
            _frame++;
            if (_frame % RecomputeEveryXFrame != 0) return;

            Vector2 center = _origin.position;
            float facing = FacingAngle();
            int rays = Mathf.Max(8, Accuracy);
            float half = FieldOfView >= 360f ? 180f : FieldOfView * 0.5f;

            // ---- ring: cast rays, collect the vision polygon boundary ----
            var ring = new List<Vector3>(rays + 8);
            var ringDirs = new List<Vector2>(rays + 8);    // unit direction per ring vertex
            Collider2D prevHit = null;
            int hinges = 0;

            for (int i = 0; i < rays; i++)
            {
                float t = rays == 1 ? 0f : (float)i / (rays - 1);
                float ang = FieldOfView >= 360f
                    ? facing - 180f + 360f * (i / (float)rays)     // full circle, no duplicated seam ray
                    : (facing - half) + FieldOfView * t;
                var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

                float dist = BaseViewDistance;
                int hits = Physics2D.Raycast(center, dir, _wallFilter, _hitBuf);
                Collider2D hitCol = null;
                if (hits > 0 && _hitBuf[0].fraction > 0f)
                {
                    dist = Mathf.Clamp(_hitBuf[0].distance, 0.1f, BaseViewDistance);
                    hitCol = _hitBuf[0].collider;
                }

                var point = center + dir * dist;
                bool surfaceChanged = i > 0 && !SameSurface(hitCol, prevHit, dist, BaseViewDistance);
                if (surfaceChanged)
                {
                    ring.Add(ring[ring.Count - 1]);            // hinge: bend at the corner
                    ringDirs.Add(ringDirs[ringDirs.Count - 1]);
                    hinges++;
                }

                ring.Add(point);
                ringDirs.Add(dir);
                prevHit = hitCol;
            }
            LastHingeCount = hinges;

            // ---- build the DARK COMPLEMENT: ring edges extruded far outward ----
            // quad per segment: (a_i, a_j) ring edge -> (b_i, b_j) far edge.
            // FAR=60 covers the whole map from anywhere on it (max half-diagonal ~24).
            // R4: vertex-color gradient — ring vertices alpha 0 (vision edge, clear),
            // far vertices full DimColor alpha (deep dark). Sprites/Default is
            // vertex-color * additive, so this is THE transparency control.
            const float FAR = 60f;
            float edgeA = 0f;
            float darkA = DimColor.a;
            int n = ring.Count;
            var cols = new List<Color>(n * 2);
            var verts = new List<Vector3>(n * 2);
            var tris = new List<int>(n * 6);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;                            // wraps the ring closed

                // per-vertex alpha: full darkness once past EdgeWidth, fading to 0 at the ring
                float ai = AlphaFor(Vector2.Distance(ring[i], center));
                float aj = AlphaFor(Vector2.Distance(ring[j], center));
                var ci = DimColor; ci.a = ai;
                var cj = DimColor; cj.a = aj;
                var cb = DimColor;

                cols.Add(ci); cols.Add(cj);
                verts.Add(ring[i]);
                verts.Add(ring[j]);
                var bi = center + ringDirs[i] * FAR;
                var bj = center + ringDirs[j] * FAR;
                cols.Add(cb); cols.Add(cb);
                verts.Add(bj);
                verts.Add(bi);

                int v = i * 4;
                // quad (a_i, a_j, b_j, b_i) as two triangles; Cull Off shader — winding safe
                tris.Add(v);     tris.Add(v + 1); tris.Add(v + 2);   // a_i, a_j, b_j
                tris.Add(v);     tris.Add(v + 2); tris.Add(v + 3);   // a_i, b_j, b_i
            }

            _fogMesh.Clear();
            _fogMesh.vertices = verts.ToArray();
            _fogMesh.triangles = tris.ToArray();
            _fogMesh.colors = cols.ToArray();
            _fogMesh.RecalculateBounds();
            LastVertexCount = n;

            // ---- entity visibility: NPCs need LOS (GGD hides non-visible entities) ----
            UpdateEntityVisibility(center);
        }

        /// <summary>True when two adjacent rays effectively hit the same surface.</summary>
        private static bool SameSurface(Collider2D a, Collider2D b, float distA, float maxDist)
        {
            bool hitA = a != null && distA < maxDist - 0.001f;
            bool hitB = b != null;
            if (hitA != hitB) return false;
            if (!hitA) return true;                 // both rays ended in open space
            return a == b;
        }

        /// <summary>
        /// Darkness alpha at a distance from the player: 0 inside EdgeWidth of the
        /// vision edge, ramping linearly to full DimColor alpha beyond it. This is
        /// the penumbra band that kills the "choppy hard cut".
        /// </summary>
        private float AlphaFor(float distFromCenter)
        {
            float bandStart = BaseViewDistance - EdgeWidth;
            float t = (distFromCenter - bandStart) / Mathf.Max(0.05f, EdgeWidth);
            return Mathf.Clamp01(t) * DimColor.a;
        }

        private float FacingAngle()
        {
            var pc = _origin.GetComponent<PlayerController2D>();
            Vector2 f = pc != null ? pc.FaceDir : Vector2.right;
            return Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
        }

        private void UpdateEntityVisibility(Vector2 center)
        {
            float half = FieldOfView >= 360f ? 180f : FieldOfView * 0.5f;
            float facing = FacingAngle();

            foreach (var ent in Object.FindObjectsByType<FogEntity>(FindObjectsSortMode.None))
            {
                var pos = (Vector2)ent.transform.position;
                var to = pos - center;
                bool visible = false;

                if (to.sqrMagnitude <= BaseViewDistance * BaseViewDistance)
                {
                    bool inCone = FieldOfView >= 360f ||
                        Mathf.Abs(Mathf.DeltaAngle(facing, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg)) <= half + 5f;
                    if (inCone)
                    {
                        int blocked = Physics2D.Raycast(center, to.normalized, _wallFilter, _hitBuf,
                            to.magnitude - 0.05f);
                        visible = blocked == 0;
                    }
                }

                ent.SetInVision(visible);   // FogEntity fades smoothly (GGD-style)
            }
        }

        private void OnDestroy()
        {
            if (_fogMesh != null) Destroy(_fogMesh);
            if (_fogGo != null) Destroy(_fogGo);
        }
    }
}
