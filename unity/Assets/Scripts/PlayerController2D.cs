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

        /// <summary>Last non-zero movement direction (for animator facing).</summary>
        public Vector2 CurrentMove { get; private set; }

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

            // With the animated sprite we never rotate the transform — the animator
            // picks the facing row instead (rotating would tip the top-down sprite).
            if (move.sqrMagnitude > 0.01f) CurrentMove = move;
            else CurrentMove = Vector2.zero;

            _rb.linearVelocity = move * speed;
        }
    }
}
