using System.Collections.Generic;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 R3 — GGD FogOfWar port, third pass. R1/R2 "buggy all over the place"
    /// post-mortem, three independent root causes found and fixed here:
    ///
    ///   A. WALLS NEVER BLOCKED SIGHT: the raycast mask uses physics layer
    ///      "Collision" (slot 10), but SceneGeo never assigned it — buildings sat on
    ///      Default(0) and every sight ray passed through every wall. Sight was pure
    ///      distance. Fixed in SceneGeo.MakeBuilding (go.layer = Collision); verified
    ///      SCHOOL reports physicsLayer=10.
    ///   B. THE TWO-QUAD DESIGN WAS STRUCTURALLY WRONG: a dark overlay quad + faint
    ///      "light" mesh on top can never PUNCH A HOLE — alpha blending just stacks,
    ///      so the vision area stayed washed no matter what. R3 uses the standard
    ///      inversion: ONE dark mesh that covers everything EXCEPT the vision
    ///      polygon. Each ring edge is extruded outward past the map corners
    ///      (polar-space quads, seam- and overlap-free), so darkness ends exactly
    ///      where sight ends. Sharp wall corners use the hinge-vertex trick so
    ///      the silhouette bends instead of smearing.
    ///   C. THIS PROJECT HAS EXACTLY ONE SORTING LAYER ("Default") — every named
    ///      sortingLayerName in the project (Player/Collision/Decor) silently
    ///      no-ops to Default, and global sortingOrder does all the layering:
    ///      ground -10 ... buildings 0, NPCs 5, player 10 | fog 20 | panels 40+.
    ///      The fog is on Default @ 20 — above the world, below every panel.
    ///
    /// GGD reference (dump.cs 238551): accuracy (ray count), fieldOfView
    /// (default = full circle; lower it for the flashlight-mode variant),
    /// baseViewDistance, layerMask walls, recomputeMeshEveryXFrame.
    /// </summary>
    public class FogOfWar2D : MonoBehaviour
    {
        [Header("GGD tweakables")]
        public int Accuracy = 120;             // GGD: accuracy (1..720)
        [Range(10f, 360f)]
        public float FieldOfView = 360f;       // full circle by default (GGD); 60-140 = flashlight
        public float BaseViewDistance = 7f;    // GGD: baseViewDistance
        public int RecomputeEveryXFrame = 2;   // GGD: recomputeMeshEveryXFrame

        [Header("Look")]
        public Color DimColor = new Color(0.02f, 0.04f, 0.08f, 0.62f);  // outside vision

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
            const float FAR = 60f;
            int n = ring.Count;
            var verts = new List<Vector3>(n * 2);
            var tris = new List<int>(n * 6);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;                            // wraps the ring closed
                verts.Add(ring[i]);
                verts.Add(ring[j]);
                var bi = center + ringDirs[i] * FAR;
                var bj = center + ringDirs[j] * FAR;
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

            foreach (var npc in Object.FindObjectsByType<DialogNPC>(FindObjectsSortMode.None))
            {
                var pos = (Vector2)npc.transform.position;
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

                var sr = npc.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = visible;
                var label = npc.transform.Find("Name");
                if (label != null && label.gameObject.activeSelf != visible)
                    label.gameObject.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            if (_fogMesh != null) Destroy(_fogMesh);
            if (_fogGo != null) Destroy(_fogGo);
        }
    }
}
