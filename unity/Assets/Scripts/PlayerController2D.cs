using UnityEngine;

namespace SafeStreets
{
    /// <summary>Top-down WASD/arrow movement for the kid character (2D).</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController2D : MonoBehaviour
    {
        public float speed = 4f;

        /// <summary>When true (interaction popup open) the player is fully frozen —
        /// velocity is zeroed every physics step, so held keys or residual motion stop dead.</summary>
        public bool Paused { get; set; }

        /// <summary>Test hook: set via editor tooling to drive the player without a keyboard.</summary>
        public Vector2 debugMove;

        private Rigidbody2D _rb;
        private Vector2 _face = Vector2.up;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;   // top-down: no gravity
            _rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (Paused)
            {
                // Disabling this component is NOT enough — the Rigidbody2D keeps its last
                // velocity and the player slides on. Zero it every physics frame while paused.
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector2 move = new Vector2(h, v);
            if (move.sqrMagnitude > 0.01f)
            {
                move.Normalize();
                _face = move;
            }
            else if (debugMove.sqrMagnitude > 0.01f)
            {
                move = debugMove.normalized;
                _face = move;
            }

            _rb.linearVelocity = move * speed;
            if (move.sqrMagnitude > 0.01f)
                transform.up = _face;   // face the walking direction
        }
    }
}
