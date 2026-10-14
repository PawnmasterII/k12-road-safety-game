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
        public float OriginLerpSpeed = 14f;    // R6d: GGD computedOrigin smoothing

        [Header("Look")]
        public Color DimColor = new Color(0.02f, 0.04f, 0.08f, 0.62f);  // outside vision
        public float EdgeWidth = 1.4f;         // penumbra band: 0 alpha at vision edge -> full here

        public bool Enabled2 { get; private set; } = true;

        private Transform _origin;
        private Vector2 _smoothedOrigin;       // R6d: GGD computedOrigin
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
            _smoothedOrigin = _origin.position;   // R6d: seed the smoothed origin
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

        /// <summary>
        /// R6e: GGD runs vision in FixedUpdate (physics-aligned) — fog now steps in
        /// lockstep with the player's 50Hz movement clock, eliminating the inter-clock
        /// phase drift between sprite steps (50Hz), fog origin lerp (render rate), and
        /// camera lerp (render rate) that read as boundary jitter while walking.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_booted) Boot();
            if (_origin == null || _mf == null) return;
            _frame++;
            if (_frame % RecomputeEveryXFrame != 0) return;

            // R6d: GGD-style smoothed origin (computedOrigin) — kills micro-jitter.
            float k = 1f - Mathf.Exp(-OriginLerpSpeed * Time.fixedDeltaTime);
            _smoothedOrigin = Vector2.Lerp(_smoothedOrigin, _origin.position, k);

            Vector2 center = _smoothedOrigin;
            float facing = FacingAngle();
            int rays = Mathf.Max(8, Accuracy);
            float half = FieldOfView >= 360f ? 180f : FieldOfView * 0.5f;

            // R6d: in 360° mode the ring anchors to a FIXED world angle — a full circle
            // has no facing. Anchoring to `facing` shifted every ray on each direction
            // change, migrating hinges and making the silhouette "swim": THE original
            // choppy/tweaky complaint (the old version recomputed the ring off facing
            // every frame). Cone mode (60-140°) still tracks facing.

            // ---- ring: cast rays, collect the vision polygon boundary ----
            var ring = new List<Vector3>(rays + 8);
            var ringDirs = new List<Vector2>(rays + 8);    // unit direction per ring vertex
            var ringDists = new List<float>(rays + 8);     // distance from player per ring vertex
            Collider2D prevHit = null;
            int hinges = 0;

            for (int i = 0; i < rays; i++)
            {
                float t = rays == 1 ? 0f : (float)i / (rays - 1);
                float ang = FieldOfView >= 360f
                    ? -180f + 360f * (i / (float)rays)          // R6d: fixed world anchor — no swim
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
                    ringDists.Add(ringDists[ringDists.Count - 1]);
                    hinges++;
                }

                ring.Add(point);
                ringDirs.Add(dir);
                ringDists.Add(dist);
                prevHit = hitCol;
            }
            LastHingeCount = hinges;

            // ---- build the DARK COMPLEMENT — R5 correctness fix ----
            // R4 keyed alpha to DISTANCE FROM PLAYER: ring vertices hugging walls
            // (2-5 units away) got alpha 0, so shadow pockets rendered LIT — the user
            // saw "blocked zone still normal, only a border". Alpha must key off the
            // VISION BOUNDARY, not the player: clear (0) at every ring vertex, full
            // DimColor by EdgeWidth outward ALONG THE RAY, full to the map edge.
            // Two quads per ring segment:
            //   penumbra: a_i..a_j (ring, alpha 0) -> m_i..m_j (dist+EdgeWidth, full)
            //   solid:    m_i..m_j (full)          -> f_i..f_j (FAR, full)
            const float FAR = 60f;
            int n = ring.Count;
            var verts = new List<Vector3>(n * 6);
            var cols = new List<Color>(n * 6);
            var tris = new List<int>(n * 12);
            var clear = DimColor; clear.a = 0f;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;                            // wraps the ring closed
                var a_i = ring[i];
                var a_j = ring[j];
                var m_i = center + ringDirs[i] * (ringDists[i] + EdgeWidth);
                var m_j = center + ringDirs[j] * (ringDists[j] + EdgeWidth);
                var f_i = center + ringDirs[i] * FAR;
                var f_j = center + ringDirs[j] * FAR;

                // a_i, a_j, m_j, m_i, f_j, f_i
                verts.Add(a_i); verts.Add(a_j); verts.Add(m_j); verts.Add(m_i); verts.Add(f_j); verts.Add(f_i);
                cols.Add(clear); cols.Add(clear); cols.Add(DimColor); cols.Add(DimColor); cols.Add(DimColor); cols.Add(DimColor);

                int v = i * 6;
                // penumbra quad: a_i, a_j, m_j, m_i
                tris.Add(v);     tris.Add(v + 1); tris.Add(v + 2);
                tris.Add(v);     tris.Add(v + 2); tris.Add(v + 3);
                // solid quad: m_i, m_j, f_j, f_i
                tris.Add(v + 3); tris.Add(v + 2); tris.Add(v + 4);
                tris.Add(v + 3); tris.Add(v + 4); tris.Add(v + 5);
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
        /// Darkness alpha at a distance from the player. (R5: no longer used by the
        /// mesh — alpha now keys off the vision boundary via the two-quad extrusion.
        /// Kept for external callers/debug.)
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
