using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 006: single source of truth for zebra-crossing geometry.
    /// One instance over the stripe union. The road's own sprite bounds give the
    /// road half-height for the side() test; the zone bounds give the corridor.
    /// Replaces the per-frame FindObjectsByType("Zebra*") sweep in CrossingNPC.
    /// </summary>
    public class CrossingZone : MonoBehaviour
    {
        public static CrossingZone Instance { get; private set; }

        /// <summary>Corridor AABB (stripe union), world space, no forgiveness margin.</summary>
        [HideInInspector] public Rect corridor;

        /// <summary>Road half-height, from the Road sprite bounds.</summary>
        [HideInInspector] public float roadHalfY;

        /// <summary>Side of the road: +1 north, -1 south, 0 on the road.</summary>
        public static int SideOf(Vector3 pos)
        {
            if (Instance == null) return 0;
            if (pos.y > Instance.roadHalfY) return 1;
            if (pos.y < -Instance.roadHalfY) return -1;
            return 0;
        }

        /// <summary>True if pos is inside the corridor (exactly the painted stripes).</summary>
        public static bool InCorridor(Vector3 pos)
        {
            return Instance != null && Instance.corridor.Contains(pos);
        }

        private void Awake()
        {
            Instance = this;
            Recompute();
        }

        /// <summary>Editor/scene-build helper: keeps geometry honest after moving the zone.</summary>
        private void OnDrawGizmosSelected()
        {
            Recompute();
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.55f);
            Gizmos.DrawWireCube(corridor.center, corridor.size);
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.4f);
            var c = corridor.center;
            Gizmos.DrawLine(new Vector3(c.x - corridor.width, -roadHalfY, 0f),
                            new Vector3(c.x + corridor.width, -roadHalfY, 0f));
            Gizmos.DrawLine(new Vector3(c.x - corridor.width, roadHalfY, 0f),
                            new Vector3(c.x + corridor.width, roadHalfY, 0f));
        }

        /// <summary>
        /// Corridor = union of all active Zebra* sprites (cached once, not swept per frame);
        /// roadHalfY from the Road sprite. Falls back to own collider if stripes missing.
        /// </summary>
        public void Recompute()
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            bool found = false;
            var all = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            foreach (var z in all)
            {
                if (!z.name.StartsWith("Zebra")) continue;
                var b = z.bounds;
                minX = Mathf.Min(minX, b.min.x); maxX = Mathf.Max(maxX, b.max.x);
                minY = Mathf.Min(minY, b.min.y); maxY = Mathf.Max(maxY, b.max.y);
                found = true;
            }
            if (!found)
            {
                var col = GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    var b = col.bounds;
                    corridor = new Rect(b.center - b.extents, b.size);
                    found = true;
                }
            }
            else
            {
                corridor = Rect.MinMaxRect(minX, minY, maxX, maxY);
            }
            roadHalfY = 3f; // Road sprite is 40x6 at origin; fallback if lookup below fails
            foreach (var r in all)
            {
                if (r.name == "Road")
                {
                    roadHalfY = r.bounds.extents.y;
                    break;
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;   // statics survive domain reloads in the Editor (project convention)
        }
    }
}
