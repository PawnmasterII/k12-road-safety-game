using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Borrowed from 2DRPGKit's DialogManager (CC0), heavily simplified:
    /// multi-line dialog with name plate, SPACE advances, freezes the player,
    /// shows the pack's DialogBox UI sprite. One line = one box of text.
    /// The 2DRPGKit version tangles in shops/inns/party/battle — those stay out.
    /// </summary>
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public GameObject Source { get; private set; }

        private Conversation _conversation;
        private int _line;
        private GameObject _panel;
        private TextMesh _nameMesh;
        private TextMesh _bodyMesh;
        private SpriteRenderer _backdrop;

        private void Awake()
        {
            Instance = this;
            Build();
            _panel.SetActive(false);
        }

        private void Build()
        {
            _panel = new GameObject("DialogPanel");

            // backdrop from the pack's DialogBox sprite (930x... scaled), tinted dark
            _backdrop = _panel.AddComponent<SpriteRenderer>();
            var box = Resources.Load<Sprite>("");
            _backdrop.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);
            _backdrop.color = new Color(0.08f, 0.12f, 0.19f, 0.97f);
            _backdrop.drawMode = SpriteDrawMode.Sliced;
            _backdrop.size = new Vector2(9.4f, 2.2f);
            _backdrop.sortingOrder = 50;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(_panel.transform, false);
            _nameMesh = nameGo.AddComponent<TextMesh>();
            DialogStyle.Style(_nameMesh, 0.2f, 34, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.35f), 52);
            nameGo.transform.localPosition = new Vector3(-4.2f, 0.72f, 0f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(_panel.transform, false);
            _bodyMesh = bodyGo.AddComponent<TextMesh>();
            DialogStyle.Style(_bodyMesh, 0.19f, 32, TextAnchor.UpperLeft, Color.white, 52);
            bodyGo.transform.localPosition = new Vector3(-4.2f, 0.35f, 0f);

            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(_panel.transform, false);
            var hint = hintGo.AddComponent<TextMesh>();
            DialogStyle.Style(hint, 0.13f, 26, TextAnchor.LowerRight, new Color(0.7f, 0.8f, 0.9f), 52);
            hint.text = "SPACE ▸";
            hintGo.transform.localPosition = new Vector3(4.2f, -0.75f, 0f);
        }

        public void StartDialog(Conversation conversation, GameObject source)
        {
            StartDialog(conversation, source, null);
        }

        public void StartDialog(Conversation conversation, GameObject source, System.Action onDone)
        {
            if (conversation == null || conversation.lines == null || conversation.lines.Length == 0) return;
            _conversation = conversation;
            Source = source;
            _onDone = onDone;
            _line = 0;
            IsOpen = true;

            var player = GameObject.FindWithTag("Player");
            if (player != null) player.GetComponent<PlayerController2D>().Paused = true;

            _panel.SetActive(true);
            Position();
            ShowCurrent();
        }

        public void Close()
        {
            IsOpen = false;
            Source = null;
            _panel.SetActive(false);
            var player = GameObject.FindWithTag("Player");
            if (player != null) player.GetComponent<PlayerController2D>().Paused = false;
            var cb = _onDone; _onDone = null;
            cb?.Invoke();
        }

        private System.Action _onDone;

        private void ShowCurrent()
        {
            var line = _conversation.lines[_line];
            if (line.StartsWith("//"))
            {
                _nameMesh.text = line.Substring(2);
                _line++;
                if (_line >= _conversation.lines.Length) { Close(); return; }
                line = _conversation.lines[_line];
            }
            else if (_line == 0)
            {
                _nameMesh.text = _conversation.speakerName;
            }
            _bodyMesh.text = line;
        }

        public void Advance()
        {
            if (!IsOpen) return;
            _line++;
            if (_line >= _conversation.lines.Length)
            {
                if (!string.IsNullOrEmpty(_conversation.closingTip))
                {
                    _nameMesh.text = "TIP";
                    _bodyMesh.text = _conversation.closingTip;
                    _conversation.closingTip = null;   // shown once per conversation asset instance
                }
                Close();
                return;
            }
            ShowCurrent();
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (Input.GetKeyDown(KeyCode.Space)) Advance();
        }

        private void Position()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // bottom of the screen: panel center 2.2 units below camera center
            _panel.transform.position = cam.transform.position + new Vector3(0f, -cam.orthographicSize * 0.55f, 5f);
        }
    }

    /// <summary>Shared TextMesh styling — the fontless-TextMesh fix lives here.</summary>
    public static class DialogStyle
    {
        private static Font _font;

        public static void Style(TextMesh tm, float charSize, int fontSize, TextAnchor anchor,
            Color color, int sortingOrder)
        {
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = _font;
            tm.characterSize = charSize;
            tm.fontSize = fontSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Left;
            tm.color = color;
            var r = tm.GetComponent<MeshRenderer>();
            r.sharedMaterial = _font.material;
            r.sortingOrder = sortingOrder;
        }
    }
}
