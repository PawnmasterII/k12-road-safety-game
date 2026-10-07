using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// The whole interaction feature, deliberately simple:
    ///   - Near + facing an Interactable  ->  floating "Press SPACE" hint
    ///   - SPACE (near+facing) or clicking the object  ->  popup titled "Interaction"
    ///   - SPACE again, or clicking the X in the corner  ->  popup exits, movement resumes
    /// Near = distance to the object's COLLIDER EDGE (buildings are big; center distance lies).
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public float range = 1.8f;        // edge distance that counts as "next to"
        public float facingDot = 0.35f;   // ~69 degrees half-cone

        public Interactable Candidate { get; private set; }
        public Interactable Open { get; private set; }

        private Transform _player;
        private GameObject _hint;
        private GameObject _panel;
        private TextMesh _panelBody;
        private GameObject _xButton;

        private void Awake()
        {
            Instance = this;
            BuildPanel();
        }

        private void Update()
        {
            if (_player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p == null) return;
                _player = p.transform;
            }

            if (Open != null)
            {
                UpdateHint(null);
                if (Input.GetKeyDown(KeyCode.Space)) Close();
                return;
            }

            Candidate = FindCandidate();
            UpdateHint(Candidate);
            if (Candidate != null && Input.GetKeyDown(KeyCode.Space)) OpenUp(Candidate);
        }

        /// <summary>Distance from a world point to the interactable's collider edge (2D).</summary>
        public static float EdgeDistance(Interactable it, Vector2 point)
        {
            var col = it.GetComponent<Collider2D>();
            if (col == null) return Vector2.Distance(it.transform.position, point);
            var closest = col.ClosestPoint(point);
            return Vector2.Distance(closest, point);
        }

        public bool TrySpace()
        {
            if (Open != null) { Close(); return true; }
            if (Candidate != null) { OpenUp(Candidate); return true; }
            return false;
        }

        public void TryClickOpen(Interactable it)
        {
            if (Open != null || it == null || _player == null) return;
            if (EdgeDistance(it, _player.position) > range + 0.8f) return;
            OpenUp(it);
        }

        private void OpenUp(Interactable it)
        {
            Open = it;
            UpdateHint(null);
            var player = GameObject.FindWithTag("Player");
            var pc = player?.GetComponent<PlayerController2D>();
            if (pc != null)
            {
                pc.Paused = true;                       // freezes FixedUpdate input
                pc.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;   // kill slide instantly
            }

            _panelBody.text = "You are looking at " + it.label + ".\n\n(More content coming soon.)";
            _panel.SetActive(true);
            PositionPanel();
        }

        public void Close()
        {
            Open = null;
            _panel.SetActive(false);
            var pc = GameObject.FindWithTag("Player")?.GetComponent<PlayerController2D>();
            if (pc != null) pc.Paused = false;
        }

        private Interactable FindCandidate()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                var col = it.GetComponent<Collider2D>();
                if (col == null) continue;

                var closest = col.ClosestPoint(_player.position);
                var to = closest - (Vector2)_player.position;
                float dist = to.magnitude;
                if (dist > range) continue;
                if (dist > 0.05f && Vector2.Dot(_player.up, to.normalized) < facingDot) continue;
                if (dist < bestDist) { bestDist = dist; best = it; }
            }
            return best;
        }

        // ---- floating hint above the candidate ----

        private static Font _builtinFont;

        internal static Font BuiltinFont()
        {
            if (_builtinFont == null)
                _builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _builtinFont;
        }

        /// <summary>Script-created TextMeshes have NO font — assign it or nothing renders.</summary>
        internal static void StyleText(TextMesh tm, string text, float charSize, int fontSize,
            TextAnchor anchor, Color color, int sortingOrder)
        {
            tm.text = text;
            tm.font = BuiltinFont();
            tm.characterSize = charSize;
            tm.fontSize = fontSize;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var r = tm.GetComponent<MeshRenderer>();
            r.sharedMaterial = tm.font.material;
            r.sortingOrder = sortingOrder;
        }

        private void UpdateHint(Interactable it)
        {
            if (it == null)
            {
                if (_hint != null) _hint.SetActive(false);
                return;
            }
            if (_hint == null)
            {
                _hint = new GameObject("InteractHint");
                var tm = _hint.AddComponent<TextMesh>();
                StyleText(tm, "Press SPACE", 0.16f, 30, TextAnchor.MiddleCenter,
                    new Color(1f, 0.95f, 0.6f), 30);
            }
            _hint.SetActive(true);
            _hint.transform.position = it.transform.position + new Vector3(0f, 3.6f, -3f);
        }

        // ---- the "Interaction" popup ----

        private void BuildPanel()
        {
            _panel = new GameObject("InteractionPanel");

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_panel.transform, false);
            var sr = backdrop.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
            sr.color = new Color(0.07f, 0.13f, 0.20f, 0.97f);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(8f, 5f);
            sr.sortingOrder = 40;

            var title = new GameObject("Title");
            title.transform.SetParent(_panel.transform, false);
            var ttm = title.AddComponent<TextMesh>();
            StyleText(ttm, "Interaction", 0.34f, 44, TextAnchor.MiddleCenter,
                new Color(1f, 0.8f, 0.3f), 42);
            title.transform.localPosition = new Vector3(0f, 1.7f, 0f);

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(_panel.transform, false);
            _panelBody = bodyGo.AddComponent<TextMesh>();
            StyleText(_panelBody, "", 0.22f, 36, TextAnchor.MiddleCenter, Color.white, 42);
            bodyGo.transform.localPosition = new Vector3(0f, 0f, 0f);

            var hintGo = new GameObject("ExitHint");
            hintGo.transform.SetParent(_panel.transform, false);
            var htm = hintGo.AddComponent<TextMesh>();
            StyleText(htm, "Press SPACE or click X to exit", 0.15f, 28, TextAnchor.MiddleCenter,
                new Color(0.7f, 0.8f, 0.9f), 42);
            hintGo.transform.localPosition = new Vector3(0f, -1.8f, 0f);

            _xButton = new GameObject("X");
            _xButton.transform.SetParent(_panel.transform, false);
            var xtm = _xButton.AddComponent<TextMesh>();
            StyleText(xtm, "X", 0.3f, 40, TextAnchor.MiddleCenter,
                new Color(1f, 0.4f, 0.35f), 43);
            _xButton.transform.localPosition = new Vector3(3.4f, 2.0f, 0f);
            var xcol = _xButton.AddComponent<BoxCollider2D>();
            xcol.size = new Vector2(0.9f, 0.9f);
            _xButton.AddComponent<InteractCloseButton>();
        }

        private void PositionPanel()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _panel.transform.position = cam.transform.position + cam.transform.forward * 5f;
        }
    }

    /// <summary>The X in the corner.</summary>
    public class InteractCloseButton : MonoBehaviour
    {
        private void OnMouseDown()
        {
            InteractionManager.Instance.Close();
        }
    }
}
