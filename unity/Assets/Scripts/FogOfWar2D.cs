using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 — GGD FogOfWar port (mechanics only).
    /// GGD reference (dump.cs 238551): accuracy (ray count), fieldOfView cone,
    /// baseViewDistance, layerMask walls, recomputeMeshEveryXFrame, vision mesh
    /// (verticesBuffer/trianglesBuffer -> MeshFilter), flashlight mode optional.
    ///
    /// This port: casts Rays from the player across a FOV cone, builds the lit
    /// vision polygon as a Mesh (MeshFilter), and draws a dark overlay quad
    /// UNDER the mesh — inside the polygon you see the world, outside it is dim.
    /// Walls block rays (ContactFilter2D, useTriggers=false: only solid
    /// "Collision" layer colliders — the same layer buildings are on).
    /// Non-visible entities (NPCs) are hidden; the player is never hidden.
    /// </summary>
    public class FogOfWar2D : MonoBehaviour
    {
        [Header("GGD tweakables")]
        public int Accuracy = 96;              // GGD: accuracy (1..720)
        [Range(10f, 360f)]
        public float FieldOfView = 140f;       // GGD: fieldOfView (degrees, cone)
        public float BaseViewDistance = 7f;    // GGD: baseViewDistance
        public int RecomputeEveryXFrame = 2;   // GGD: recomputeMeshEveryXFrame
        public LayerMask WallMask;             // GGD: layerMask (walls only)

        [Header("Look")]
        public Color DimColor = new Color(0.02f, 0.04f, 0.08f, 0.82f); // outside vision
        public Color LightColor = new Color(1.0f, 0.95f, 0.8f, 0.22f); // inside vision

        public bool Enabled2 { get; private set; } = true;

        [Header("Map coverage (fog quad bounds)")]
        public float MapMinX = -20f;
        public float MapMaxX = 20f;
        public float MapMinY = -13f;
        public float MapMaxY = 13f;

        private Transform _origin;             // the player
        private Mesh _visionMesh;              // lit polygon (GGD verticesBuffer)
        private MeshFilter _mf;
        private GameObject _overlay;           // dim quad
        private GameObject _lightMeshGo;
        private int _frame;
        private bool _booted;
        private ContactFilter2D _wallFilter;   // solid colliders only — triggers never block sight
        private readonly RaycastHit2D[] _hitBuf = new RaycastHit2D[1];

        public int LastVertexCount { get; private set; }

        /// <summary>
        /// Self-bootstraps at PLAY start (scene reload wipes runtime refs, so the rig
        /// is rebuilt here, not in the editor-time scene build).
        /// </summary>
        private void Start()
        {
            Boot();
        }

        private void Boot()
        {
            if (_booted) return;
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            _origin = player.transform;
            _wallFilter = new ContactFilter2D
            {
                useTriggers = false,       // GGD: walls block, trigger zones never do
                useLayerMask = true,
                layerMask = LayerMask.GetMask("Collision"),
            };
            _frame = 0;

            // ---- dark overlay quad (covers the whole map, above ground/road/decor) ----
            _overlay = new GameObject("FogOverlay");
            var sr = _overlay.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            sr.color = DimColor;
            _overlay.transform.localScale = new Vector3(MapMaxX - MapMinX + 4f, MapMaxY - MapMinY + 4f, 1f);
            _overlay.transform.position = new Vector3((MapMinX + MapMaxX) * 0.5f, (MapMinY + MapMaxY) * 0.5f, -1f);
            sr.sortingLayerName = "Decor";
            sr.sortingOrder = 20;

            // ---- lit vision mesh (rebuilt every N frames, GGD style) ----
            _lightMeshGo = new GameObject("FogLightMesh");
            _mf = _lightMeshGo.AddComponent<MeshFilter>();
            var mr = _lightMeshGo.AddComponent<MeshRenderer>();
            _visionMesh = new Mesh { name = "VisionMesh" };
            _visionMesh.MarkDynamic();
            _mf.sharedMesh = _visionMesh;
            _visionMesh.vertices = new Vector3[0];
            _visionMesh.triangles = new int[0];
            mr.sortingLayerName = "Decor";
            mr.sortingOrder = 21;
            _booted = true;
        }

        private void LateUpdate()
        {
            if (!_booted) Boot();
            if (_origin == null || _mf == null) return;
            _frame++;
            if (_frame % RecomputeEveryXFrame != 0) return;

            var center = _origin.position;
            float facing = FacingAngle();
            float half = FieldOfView * 0.5f;

            // GGD: cast `accuracy` rays across the cone, each stops at the wall it hits
            var verts = new Vector3[Accuracy + 2];
            var tris = new int[Accuracy * 3];
            verts[0] = center;

            for (int i = 0; i < Accuracy; i++)
            {
                float ang = (facing - half) + (FieldOfView * i) / (Accuracy - 1);
                var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

                float dist = BaseViewDistance;
                int hits = Physics2D.Raycast(center, dir, _wallFilter, _hitBuf);
                if (hits > 0 && _hitBuf[0].fraction > 0f)
                    dist = Mathf.Clamp(_hitBuf[0].distance, 0.1f, BaseViewDistance);

                verts[i + 1] = center + (Vector3)(dir * dist);
            }

            for (int i = 0; i < Accuracy; i++)
            {
                int t = i * 3;
                tris[t] = 0;
                tris[t + 1] = i + 1;
                tris[t + 2] = i + 2;
            }

            _visionMesh.Clear();
            _visionMesh.vertices = verts;
            _visionMesh.triangles = tris;
            _visionMesh.RecalculateBounds();
            LastVertexCount = verts.Length;

            // tint via material color (unlit default material)
            _lightMeshGo.GetComponent<MeshRenderer>().material.color = LightColor;

            // ---- entity visibility: raycast to each NPC (GGD hides non-visible) ----
            UpdateEntityVisibility(center, facing, half);
        }

        private float FacingAngle()
        {
            var pc = _origin.GetComponent<PlayerController2D>();
            Vector2 f = pc != null ? pc.FaceDir : Vector2.right;
            return Mathf.Atan2(f.y, f.x) * Mathf.Rad2Deg;
        }

        private void UpdateEntityVisibility(Vector2 center, float facing, float half)
        {
            foreach (var npc in Object.FindObjectsByType<DialogNPC>(FindObjectsSortMode.None))
            {
                var pos = (Vector2)npc.transform.position;
                var to = pos - center;
                bool visible = false;

                if (to.sqrMagnitude <= BaseViewDistance * BaseViewDistance)
                {
                    float ang = Mathf.Abs(Mathf.DeltaAngle(facing, Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg));
                    if (ang <= half + 5f) // slight grace so edge NPCs don't flicker
                    {
                        int n = Physics2D.Raycast(center, to.normalized, _wallFilter, _hitBuf,
                            to.magnitude - 0.05f);
                        visible = n == 0; // wall between? (triggers excluded by filter)
                    }
                }

                var sr = npc.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = visible;
                var label = npc.transform.Find("Name");
                if (label != null) label.gameObject.SetActive(visible);
            }
        }

        private void OnDestroy()
        {
            if (_visionMesh != null) Destroy(_visionMesh);
            if (_overlay != null) Destroy(_overlay);
            if (_lightMeshGo != null) Destroy(_lightMeshGo);
        }
    }
}
