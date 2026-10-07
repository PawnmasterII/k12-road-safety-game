using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Animated player for the Ninja Adventure 16x16 sheet (7 rows x 4 frames).
    /// Row mapping (documented pack layout): 0=walk down, 1=walk up, 2=walk left,
    /// 3=walk right, 4-6=run variants (unused). Sprites are sliced at build time by
    /// SceneBuilder2D via SpriteImportMode.Multiple.
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

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _controller = GetComponent<PlayerController2D>();
        }

        private void FixedUpdate()
        {
            var v = _rb(); // placeholder to avoid unused warning in older compilers
        }

        private System.Collections.IEnumerator Start()
        {
            yield return null;
        }

        private Rigidbody2D _rb() => GetComponent<Rigidbody2D>();

        private void LateUpdate()
        {
            if (_controller == null || _sr == null) return;

            bool moving = _controller.CurrentMove != Vector2.zero;
            if (!moving)
            {
                // idle: first frame of the last faced direction
                _frame = 0;
                _timer = 0f;
                ShowCurrentRow();
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= 1f / framesPerSecond)
            {
                _timer = 0f;
                _frame = (_frame + 1) % 4;
            }
            ShowCurrentRow();
        }

        private void ShowCurrentRow()
        {
            var v = _controller.CurrentMove;
            Sprite[] row;
            if (Mathf.Abs(v.y) >= Mathf.Abs(v.x))
                row = v.y > 0 ? walkUp : walkDown;
            else
                row = v.x > 0 ? walkRight : walkLeft;

            if (row == null || row.Length == 0 || row[_frame] == null) return;
            _sr.sprite = row[_frame];
        }
    }
}
