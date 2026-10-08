using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Put on anything the player can interact with (buildings, signs...).
    /// GGD-style: the candidate is whoever's Proximity zone the player is
    /// currently standing in (see Proximity2D / InteractionManager).
    /// The sprite glows toward HighlightColor while the player is inside the
    /// zone and fades back at GGD's FADE_SPEED — "you can use this" feedback.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public string label = "something";

        [Header("GGD glow")]
        public Color HighlightColor = new Color(1.3f, 1.25f, 1.0f);  // warm brighten (HDR-ish)
        public float FadeSpeed = 0.05f;                              // GGD FADE_SPEED

        [Header("Spec 004: task minigame (GGD TaskPanel)")]
        /// <summary>Set by the scene build: when present, SPACE opens this minigame instead of the info popup.</summary>
        public TaskPanel TaskGame;

        /// <summary>Proximity zones collected from children (GGD keeps proximities[]).</summary>
        public Proximity2D[] Proximities { get; private set; } = System.Array.Empty<Proximity2D>();

        /// <summary>True while the player stands in any of this object's zones.</summary>
        public bool IsPlayerInProximity
        {
            get
            {
                foreach (var p in Proximities)
                    if (p != null && p.IsInProximity) return true;
                return false;
            }
        }

        /// <summary>Most recently entered active zone (tiebreak for overlapping zones).</summary>
        public Proximity2D CurrentZone
        {
            get
            {
                Proximity2D best = null;
                foreach (var p in Proximities)
                {
                    if (p == null || !p.IsInProximity) continue;
                    if (best == null || p.EnteredAt > best.EnteredAt) best = p;
                }
                return best;
            }
        }

        private SpriteRenderer _sr;
        private Color _baseColor;
        private Color _target;

        private void Awake()
        {
            CollectProximities();
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
            _target = _baseColor;
        }

        /// <summary>Called by Proximity2D setup (scene build) or manually after adding zones.</summary>
        public void CollectProximities()
        {
            Proximities = GetComponentsInChildren<Proximity2D>();
        }

        private void Update()
        {
            if (_sr == null) return;
            _target = IsPlayerInProximity ? _baseColor * HighlightColor : _baseColor;
            // GGD fades glow per-frame at FADE_SPEED; use the same cadence scaled to Color range
            _sr.color = Color.Lerp(_sr.color, _target, FadeSpeed * Time.deltaTime * 60f);
        }

        private void OnMouseDown()
        {
            InteractionManager.Instance.TryClickOpen(this);
        }
    }
}
