using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Walk animation per Game Code Library's Top Down Unity 2D #2:
    /// the 4x4 Walk sheet is COLUMN-major — frame indices go DOWN the columns:
    ///   down = 0,4,8,12 · up = 1,5,9,13 · left = 2,6,10,14 · right = 3,7,11,15
    /// (verified against the tutorial transcript, not guessed).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerAnimator : MonoBehaviour
    {
        public Sprite[] frames = new Sprite[16];   // column-major sheet
        public float framesPerSecond = 6f;

        private SpriteRenderer _sr;
        private PlayerController2D _controller;
        private float _timer;
        private int _step;

        private static readonly int[] Down = { 0, 4, 8, 12 };
        private static readonly int[] Up = { 1, 5, 9, 13 };
        private static readonly int[] Left = { 2, 6, 10, 14 };
        private static readonly int[] Right = { 3, 7, 11, 15 };

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _controller = GetComponent<PlayerController2D>();
        }

        private void LateUpdate()
        {
            if (_controller == null || _sr == null || frames.Length < 16) return;

            Vector2 v = _controller.CurrentMove;
            int[] row;
            if (v == Vector2.zero) row = Down;          // idle faces down
            else if (Mathf.Abs(v.y) >= Mathf.Abs(v.x)) row = v.y > 0 ? Up : Down;
            else row = v.x > 0 ? Right : Left;

            if (v == Vector2.zero)
            {
                _step = 0;                               // idle: standing frame
                _timer = 0f;
            }
            else
            {
                _timer += Time.deltaTime;
                if (_timer >= 1f / framesPerSecond)
                {
                    _timer = 0f;
                    _step = (_step + 1) % 4;
                }
            }

            var sprite = frames[row[_step]];
            if (sprite != null) _sr.sprite = sprite;
        }
    }
}
