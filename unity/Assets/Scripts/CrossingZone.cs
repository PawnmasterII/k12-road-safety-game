using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 006: single source of truth for zebra-crossing geometry — now PER CROSSING.
    ///
    /// Original model: one instance over the stripe union, road half-height taken from
    /// the sprite literally named "Road", side computed against world y = 0. That only
    /// works for the old single crosswalk at the origin. The city scene has crossings at
    /// several junctions (and no "Road" sprite), so this component now describes ONE
    /// crossing: the road it crosses (axis, centerline, half-thickness) and its stripe
    /// corridor. A static registry lets a CrossingNPC resolve the crossing it is
    /// actually standing on.
    ///
    /// ALWAYS place one at every road edge an escort must cross — the corridor zone is a
    /// SOLID collider (it keeps the player on the stripes), so a missing zone lets the
    /// player wander off the stripes, and a missing zone at a walkable road edge walls
    /// the player out of the road entirely.
    /// </summary>
    public class CrossingZone : MonoBehaviour
    {
        /// <summary>Legacy single-instance accessor (most recently enabled crossing).
        /// Prefer <see cref="Nearest(Vector3)"/> so multi-crossing scenes resolve locally.</summary>
        public static CrossingZone Instance { get; private set; }

        /// <summary>Every crossing in the scene (self-registered — no per-frame sweeps).</summary>
        public static readonly System.Collections.Generic.List<CrossingZone> All = new();

        // ---- Primary generator: the road being crossed ----
        [Header("Road crossed (world space)")]
        [Tooltip("true = the road runs along X (you travel north/south across it)")]
        public bool roadRunsAlongX = true;
        public Vector2 roadCenter = Vector2.zero;
        [Tooltip("Road half-thickness along the crossing axis (width / 2)")]
        public float roadHalfExtent = 3f;
        [Tooltip("Optional: sprite whose bounds define the road band (overrides the numbers)")]
        public SpriteRenderer roadSprite;

        // ---- Secondary generator: the stripe corridor ----
        [Tooltip("Stripe-union AABB in world space — the painted walking band")]
        public Rect corridor;
        [Tooltip("Optional: sprite whose bounds define the corridor (overrides the rect)")]
        public SpriteRenderer corridorSprite;

        /// <summary>Half-thickness of the road ALONG Y (axis-aware legacy accessor).</summary>
        public float roadHalfY => roadRunsAlongX ? HalfThickness : CorridorRect.height * 0.5f;

        /// <summary>+1 far side, -1 near side, 0 on the road — relative to the NEAREST
        /// crossing's road, along its crossing axis (was hardwired to world y / 0).</summary>
        public static int SideOf(Vector3 pos)
        {
            var z = Nearest(pos);
            return z != null ? z.Side(pos) : 0;
        }

        public int Side(Vector3 pos)
        {
            float along = roadRunsAlongX ? pos.y : pos.x;
            float c = CenterAlongAxis;
            float h = HalfThickness;
            if (along > c + h) return 1;
            if (along < c - h) return -1;
            return 0;
        }

        /// <summary>True if pos is inside ANY crossing's painted stripe band.</summary>
        public static bool InCorridor(Vector3 pos) => Nearest(pos)?.InBand(pos) ?? false;

        public bool InBand(Vector3 pos)
        {
            var r = CorridorRect;
            return r.width > 0.001f && r.height > 0.001f && r.Contains(pos);
        }

        // ---- resolved geometry (sprite references win when present) ----

        public Rect CorridorRect
        {
            get
            {
                if (corridorSprite != null)
                {
                    var b = corridorSprite.bounds;
                    return Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
                }
                return corridor;
            }
        }

        /// <summary>Road band as a world AABB (x = along the road, y = thickness).</summary>
        public Rect RoadBand
        {
            get
            {
                if (roadSprite != null)
                {
                    var b = roadSprite.bounds;
                    return Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
                }
                return roadRunsAlongX
                    ? Rect.MinMaxRect(roadCenter.x - 100f, roadCenter.y - roadHalfExtent,
                                      roadCenter.x + 100f, roadCenter.y + roadHalfExtent)
                    : Rect.MinMaxRect(roadCenter.x - roadHalfExtent, roadCenter.y - 100f,
                                      roadCenter.x + roadHalfExtent, roadCenter.y + 100f);
            }
        }

        /// <summary>Half-thickness of the road measured along the crossing axis.</summary>
        public float HalfThickness => roadSprite != null
            ? (roadRunsAlongX ? roadSprite.bounds.extents.y : roadSprite.bounds.extents.x)
            : roadHalfExtent;

        /// <summary>Position of the road centerline along the crossing axis.</summary>
        public float CenterAlongAxis => roadSprite != null
            ? (roadRunsAlongX ? roadSprite.bounds.center.y : roadSprite.bounds.center.x)
            : (roadRunsAlongX ? roadCenter.y : roadCenter.x);

        private void Awake()
        {
            Instance = this;
            Recompute();
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Nearest crossing whose corridor contains pos, else nearest by distance.</summary>
        public static CrossingZone Nearest(Vector3 pos)
        {
            CrossingZone best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < All.Count; i++)
            {
                var z = All[i];
                if (z == null) continue;
                if (z.InBand(pos)) return z;                        // exact cover wins
                float d = Vector2.Distance(z.CorridorRect.center, pos);
                if (d < bestD) { bestD = d; best = z; }
            }
            return best;
        }

        /// <summary>Nearest crossing, but only within <paramref name="maxDistance"/>.</summary>
        public static CrossingZone NearestWithin(Vector3 pos, float maxDistance)
        {
            var z = Nearest(pos);
            if (z == null) return null;
            float d = z.InBand(pos) ? 0f : Vector2.Distance(z.CorridorRect.center, pos);
            return d <= maxDistance ? z : null;
        }

        /// <summary>Scene-build helper: fill the serialized geometry from sprite refs so
        /// inspector values are honest before play.</summary>
        public void Recompute()
        {
            if (roadSprite != null)
            {
                var b = roadSprite.bounds;
                roadRunsAlongX = b.size.x >= b.size.y;
                roadCenter = b.center;
                roadHalfExtent = roadRunsAlongX ? b.extents.y : b.extents.x;
            }
            if (corridorSprite != null)
            {
                var b = corridorSprite.bounds;
                corridor = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
            }
            else if (corridor.width <= 0.001f)
            {
                var col = GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    var b = col.bounds;
                    corridor = Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y);
                }
            }
        }

        /// <summary>Editor/scene-build helper: visualize both generators.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.55f);
            var c = CorridorRect;
            Gizmos.DrawWireCube(c.center, c.size);
            Gizmos.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            var rb = RoadBand;
            Gizmos.DrawWireCube(rb.center, new Vector3(rb.width - 0.2f, rb.height - 0.2f, 0f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;   // statics survive domain reloads in the Editor (project convention)
            All.Clear();
        }
    }
}
