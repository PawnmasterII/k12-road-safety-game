using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 R4 — marks an entity (NPC kid, officer, car...) as fog-revealable.
    /// FogOfWar2D raycasts to it every frame and fades it in/out: entities in the
    /// dark zone become INVISIBLE (GGD hides other players outside vision), with a
    /// short fade so they don't hard pop-in/out at the vision boundary.
    /// Attach to the entity root; all child renderers fade together.
    /// </summary>
    public class FogEntity : MonoBehaviour
    {
        public float FadeSpeed = 6f;               // 0..1 per second
        [Range(0f, 1f)]
        public float Reveal = 1f;                  // current visibility (drives alphas)

        /// <summary>All live fog entities — self-registered here so FogOfWar2D never
        /// sweeps FindObjectsByType every frame (project caching convention).</summary>
        public static readonly System.Collections.Generic.List<FogEntity> All = new();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => All.Clear();   // statics survive domain reloads (convention)

        private SpriteRenderer[] _srs;
        private TextMesh[] _texts;
        private float[] _srBase;
        private float[] _txtBase;
        private float _target = 1f;

        private void Awake()
        {
            _srs = GetComponentsInChildren<SpriteRenderer>(true);
            _texts = GetComponentsInChildren<TextMesh>(true);
            _srBase = new float[_srs.Length];
            for (int i = 0; i < _srs.Length; i++) _srBase[i] = _srs[i].color.a;
            _txtBase = new float[_texts.Length];
            for (int i = 0; i < _texts.Length; i++) _txtBase[i] = _texts[i].color.a;
        }

        private void LateUpdate()
        {
            // runs after FogOfWar2D.FixedUpdate set _target this frame (R6e: the fog
            // steps on the physics clock, which always precedes LateUpdate).
            // NOTE: must move EVERY frame — gating on "target changed" freezes the
            // fade one step after the first flip (the stuck-at-0.88 bug).
            if (Mathf.Abs(Reveal - _target) > 0.001f)
            {
                Reveal = Mathf.MoveTowards(Reveal, _target, FadeSpeed * Time.deltaTime);
                Apply();
            }
        }

        /// <summary>Called by the fog every frame with the LOS verdict.</summary>
        public void SetInVision(bool inVision)
        {
            _target = inVision ? 1f : 0f;
        }

        private void Apply()
        {
            for (int i = 0; i < _srs.Length; i++)
            {
                if (_srs[i] == null) continue;
                var c = _srs[i].color;
                _srs[i].color = new Color(c.r, c.g, c.b, _srBase[i] * Reveal);
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null) continue;
                var b = _texts[i].color;
                _texts[i].color = new Color(b.r, b.g, b.b, _txtBase[i] * Reveal);
            }
        }
    }
}
