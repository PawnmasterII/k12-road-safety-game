using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Spec 004 — GGD SeeThroughMesh port (wall transparency).
    /// GGD reference (dump.cs 56623): SeeThroughMesh has seeThroughAngle,
    /// seeThroughRange, visionCollider, SetMeshActive(active) — occluders near
    /// the player switch to a see-through state so the character is never
    /// hidden behind a wall they're standing against.
    ///
    /// This port: attach to a building (with its spec-003 Proximity2D zone).
    /// While the player is inside the zone, the building's sprites (building +
    /// its label) fade to SeeThroughAlpha; when they leave, back to 1.
    /// </summary>
    public class SeeThroughWall2D : MonoBehaviour
    {
        [Header("GGD tweakables")]
        public float SeeThroughAlpha = 0.35f;
        public float FadeSpeed = 5f;          // lerp speed toward target alpha

        private Interactable _interactable;
        private SpriteRenderer[] _sprites;    // building box
        private TextMesh[] _texts;            // building label (renders via MeshRenderer, no SR)
        private Color[] _textBase;
        private float[] _baseAlpha;
        private float _target = 1f;

        private void Awake()
        {
            _interactable = GetComponent<Interactable>();
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            _baseAlpha = new float[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++)
                _baseAlpha[i] = _sprites[i].color.a;
            _texts = GetComponentsInChildren<TextMesh>(true);
            _textBase = new Color[_texts.Length];
            for (int i = 0; i < _texts.Length; i++)
                _textBase[i] = _texts[i].color;
        }

        private void Update()
        {
            bool inZone = _interactable != null && _interactable.IsPlayerInProximity;
            _target = inZone ? SeeThroughAlpha : 1f;

            for (int i = 0; i < _sprites.Length; i++)
            {
                if (_sprites[i] == null) continue;
                var c = _sprites[i].color;
                float a = Mathf.Lerp(c.a, _baseAlpha[i] * _target, FadeSpeed * Time.deltaTime);
                if (!Mathf.Approximately(a, c.a))
                    _sprites[i].color = new Color(c.r, c.g, c.b, a);
            }

            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null) continue;
                var b = _textBase[i];
                float a = Mathf.Lerp(_texts[i].color.a, b.a * _target, FadeSpeed * Time.deltaTime);
                if (!Mathf.Approximately(a, _texts[i].color.a))
                    _texts[i].color = new Color(b.r, b.g, b.b, a);
            }
        }
    }
}
