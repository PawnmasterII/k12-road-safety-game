using System;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 R6 — GGD-style panel animation (repurposed from dump.cs:
    /// GGD's UI fades everything — CanvasGroup alpha fades + FADE_SPEED-style
    /// constants on every panel; ours snapped via SetActive).
    ///
    /// Sits on the panel root. FadeIn = activate GO + alpha 0→1 with a subtle
    /// scale pop; FadeOut = alpha 1→0, THEN deactivates the GO (callback when
    /// fully hidden). Alpha-only modulation (RGB untouched) so runtime color
    /// changes (quiz rows, slot-ok green) survive the fade.
    /// The panel GameObject should start inactive, as all our panels do.
    /// </summary>
    public class PanelAnimator : MonoBehaviour
    {
        public float FadeTime = 0.16f;   // seconds — GGD fades are quick
        public float PopFrom = 0.92f;    // scale at alpha 0 (1.0 = no pop)

        public float Alpha { get; private set; }
        public bool FullyShown => Alpha > 0.999f;

        private SpriteRenderer[] _srs;
        private TextMesh[] _texts;
        private float[] _srBase;
        private float[] _txtBase;
        private int _dir;                // 0 idle, 1 in, -1 out
        private Action _onHidden;
        private Vector3 _baseScale;
        private bool _collected;

        /// <summary>Get-or-add the animator on a panel root.</summary>
        public static PanelAnimator Ensure(GameObject go)
        {
            var pa = go.GetComponent<PanelAnimator>();
            if (pa == null) pa = go.AddComponent<PanelAnimator>();
            return pa;
        }

        private void Awake()
        {
            Collect();
        }

        private void Collect()
        {
            _srs = GetComponentsInChildren<SpriteRenderer>(true);
            _texts = GetComponentsInChildren<TextMesh>(true);
            _srBase = new float[_srs.Length];
            for (int i = 0; i < _srs.Length; i++) _srBase[i] = _srs[i].color.a;
            _txtBase = new float[_texts.Length];
            for (int i = 0; i < _texts.Length; i++) _txtBase[i] = _texts[i].color.a;
            _baseScale = transform.localScale;
            Alpha = 0f;
            _collected = true;
        }

        /// <summary>Activate and fade in. Call instead of SetActive(true).</summary>
        public void FadeIn()
        {
            gameObject.SetActive(true);
            if (!_collected) Collect();
            _dir = 1;
        }

        /// <summary>Fade out, then deactivate. Call instead of SetActive(false).</summary>
        public void FadeOut(Action onHidden = null)
        {
            if (!gameObject.activeInHierarchy)
            {
                Alpha = 0f;
                onHidden?.Invoke();
                return;
            }
            if (!_collected) Collect();
            _onHidden = onHidden;
            _dir = -1;
        }

        private void LateUpdate()
        {
            if (_dir == 0) return;

            float step = FadeTime <= 0f ? 1f : Time.deltaTime / FadeTime;
            Alpha = Mathf.MoveTowards(Alpha, _dir > 0 ? 1f : 0f, step);

            if (PopFrom < 1f)
            {
                float s = Mathf.Lerp(PopFrom, 1f, Alpha);
                transform.localScale = _baseScale * s;
            }

            for (int i = 0; i < _srs.Length; i++)
            {
                if (_srs[i] == null) continue;
                var c = _srs[i].color;
                _srs[i].color = new Color(c.r, c.g, c.b, _srBase[i] * Alpha);
            }
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null) continue;
                var t = _texts[i];
                t.color = new Color(t.color.r, t.color.g, t.color.b, _txtBase[i] * Alpha);
            }

            if (_dir > 0 && Alpha >= 0.999f) _dir = 0;
            if (_dir < 0 && Alpha <= 0.001f)
            {
                _dir = 0;
                Alpha = 0f;
                if (PopFrom < 1f) transform.localScale = _baseScale;
                gameObject.SetActive(false);
                var cb = _onHidden;
                _onHidden = null;
                cb?.Invoke();
            }
        }
    }
}
