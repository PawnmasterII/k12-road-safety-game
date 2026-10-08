using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Player movement per Game Code Library's Top Down Unity 2D #1-#2:
    /// Rigidbody2D, gravity 0, velocity-based WASD/arrows, no transform rotation.
    /// Exposes MoveInput for the animator; Paused freezes physics during popups.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public float speed = 4f;

        /// <summary>Set by InteractionManager while a popup is open.</summary>
        public bool Paused { get; set; }

        /// <summary>Last non-zero movement direction (animator facing / interaction facing).</summary>
        public Vector2 CurrentMove { get; private set; }

        /// <summary>The direction the player last moved — the "facing" for interactions.</summary>
        public Vector2 FaceDir => _face;

        /// <summary>Test hook: drives the player without a keyboard. Dev/test only —
        /// strip before any kid-facing build.</summary>
        public Vector2 debugMove;

        private Rigidbody2D _rb;
        private Vector2 _face = Vector2.down;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;      // top-down RPG: no gravity (tutorial #1)
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (Paused)
            {
                _rb.linearVelocity = Vector2.zero;
                CurrentMove = Vector2.zero;
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 move = new Vector2(h, v);
            if (move.sqrMagnitude < 0.01f && debugMove.sqrMagnitude > 0.01f)
                move = debugMove;

            if (move.sqrMagnitude > 0.01f)
            {
                move.Normalize();
                _face = move;
                CurrentMove = move;
            }
            else
            {
                CurrentMove = Vector2.zero;
            }

            _rb.linearVelocity = move * speed;
        }
    }
}
