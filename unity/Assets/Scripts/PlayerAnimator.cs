using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Animated player for Ninja Adventure "SeparateAnim/Walk" sheets (4x4: down/left/right/up).
    /// Rows are selected by movement direction; idle shows frame 0 of the last faced row.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerAnimator : MonoBehaviour
    {
        public Sprite[] walkDown = new Sprite[4];
        public Sprite[] walkUp = new Sprite[4];
        public Sprite[] walkLeft = new Sprite[4];
        public Sprite[] walkRight = new Sprite[4];
        public float framesPerSecond = 8f;

        private SpriteRenderer _sr;
        private PlayerController2D _controller;
        private float _timer;
        private int _frame;
        private Sprite[] _lastRow;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _controller = GetComponent<PlayerController2D>();
        }

        private void LateUpdate()
        {
            if (_controller == null || _sr == null) return;

            Vector2 v = _controller.CurrentMove;
            bool moving = v != Vector2.zero;

            Sprite[] row = PickRow(v);
            if (row == null || row.Length == 0 || row[0] == null) return;

            if (!moving)
            {
                _frame = 0;
                _timer = 0f;
            }
            else
            {
                _timer += Time.deltaTime;
                if (_timer >= 1f / framesPerSecond)
                {
                    _timer = 0f;
                    _frame = (_frame + 1) % 4;
                }
            }

            if (_lastRow != row) { _frame = 0; _lastRow = row; }
            if (row[_frame] != null) _sr.sprite = row[_frame];
        }

        private Sprite[] PickRow(Vector2 v)
        {
            if (Mathf.Abs(v.y) >= Mathf.Abs(v.x))
                return v.y > 0 ? walkUp : walkDown;
            return v.x > 0 ? walkRight : walkLeft;
        }
    }
}
