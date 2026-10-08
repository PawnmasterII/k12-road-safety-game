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

        /// <summary>GGD PlayerController.IS_RUNNING_THRESHOLD — below this, you're idle.</summary>
        public const float IsRunningThreshold = 0.01f;

        /// <summary>True while input magnitude exceeds GGD's run threshold.</summary>
        public bool IsRunning { get; private set; }

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

            if (move.sqrMagnitude > IsRunningThreshold)
            {
                move.Normalize();
                _face = move;
                CurrentMove = move;
                IsRunning = true;
            }
            else
            {
                CurrentMove = Vector2.zero;
                IsRunning = false;
            }

            _rb.linearVelocity = move * speed;
            HandleStepAndFlip(move);
        }

        /// <summary>GGD PlayableEntity.TeleportTo: move to a position and zero velocity,
        /// so physics doesn't carry momentum into the new spot.</summary>
        public void TeleportTo(Vector2 position)
        {
            _rb.linearVelocity = Vector2.zero;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        [Header("GGD PlayableEntity port (spec 004)")]
        /// <summary>GGD RUN_CYCLE_DEFAULT — seconds per footstep beat while running.</summary>
        public float RunCycle = 0.25f;
        /// <summary>GGD facingRight — sprite flip mirrors horizontal input.</summary>
        public bool FacingRight { get; private set; } = true;
        /// <summary>Fired on each RunCycle beat while moving (footstep/anim hook).</summary>
        public event System.Action OnStep;

        private float _stepAccum;
        private SpriteRenderer _sr;

        private void HandleStepAndFlip(Vector2 move)
        {
            // GGD facingRight: flip the sprite with horizontal input
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && Mathf.Abs(move.x) > IsRunningThreshold)
                _sr.flipX = move.x < 0f;

            // GGD runCycle: OnStep every RunCycle seconds while running
            if (IsRunning)
            {
                _stepAccum += Time.fixedDeltaTime;
                if (_stepAccum >= RunCycle)
                {
                    _stepAccum = 0f;
                    OnStep?.Invoke();
                }
            }
            else _stepAccum = 0f;
        }
    }
}
