using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// The whole interaction feature — GGD-style since spec 003:
    ///   - Each Interactable owns Proximity2D child zones (trigger colliders).
    ///   - Walking into a zone (player sense bubble overlaps) makes it the
    ///     candidate: floating "Press SPACE" hint + glow on the object.
    ///   - SPACE opens the popup; SPACE again / X closes it, movement resumes.
    ///   - No facing check: GGD candidates by overlap, not aim (any approach works).
    /// Feature 002 router priority = quiz zone > crossing NPC > talk NPC > building.
    /// </summary>
    public class InteractionManager : MonoBehaviour
    {
        public static InteractionManager Instance { get; private set; }

        public float range = 1.8f;        // kept for click-to-open reach check
        public float facingDot = 0.35f;   // legacy knob (unused by overlap model)

        public Interactable Candidate { get; private set; }
        public Interactable Open { get; private set; }

        // ---- Spec 004: task minigame state (GGD TaskPanel flow) ----
        /// <summary>True while a TaskPanel (minigame) owns input/movement.</summary>
        public static bool TaskPaused { get; private set; }

        /// <summary>
        /// R10 — free-roam win: true once the quest is complete. The win is NOT a
        /// terminal freeze; WinBanner guarantees the player can move and modals
        /// that open post-win (deferred thank-you dialogs etc.) never take the
        /// freeze. Everything stays playable after the win.
        /// </summary>
        public static bool QuestWon
        {
            get
            {
                var q = QuestManager.Find();
                return q != null && q.Complete;
            }
        }

        /// <summary>Statics survive play-mode transitions and domain reloads in the
        /// Editor; a stale true from a previous run would permanently freeze the
        /// next session's player before any panel exists to clear it.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            TaskPaused = false;
        }

        /// <summary>R7: drop task ownership WITHOUT touching the player pause —
        /// used when the terminal win-freeze owns the player instead.</summary>
        public static void ClearTaskOwnership() => TaskPaused = false;

        /// <summary>Called by TaskPanel.Open/Close — pauses movement like popups do.</summary>
        public static void PauseForTask(bool pause)
        {
            TaskPaused = pause;
            var player = GameObject.FindWithTag("Player");
            var pc = player != null ? player.GetComponent<PlayerController2D>() : null;
            if (pc != null)
            {
                pc.Paused = pause;
                if (pause) pc.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            }
        }

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
            if (_pc == null) _pc = _player.GetComponent<PlayerController2D>();

            // R4/R5: SPACE swallowed by a modal that closed this frame/last frame, or
            // the Editor window is unfocused — stray physical keys never drive the game.
            if (!ModalInputGate.InputAllowed) return;

            // Feature 002: an open quiz (or dialog) owns input — nothing else routes
            // Spec 004: an open minigame owns input first of all
            if (TaskPaused) return;
            if (QuizManager.Instance != null && QuizManager.Instance.IsOpen) return;
            if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return;

            if (Open != null)
            {
                UpdateHint(null, null);
                if (Input.GetKeyDown(KeyCode.Space)) Close();
                return;
            }

            // --- ONE input router: priority = quiz zone > crossing NPC > talk NPC > building ---
            var quizZone = FindNearQuizZone();
            if (quizZone != null)
            {
                UpdateHint(quizZone.transform, "Press SPACE for the quiz");
                if (Input.GetKeyDown(KeyCode.Space)) quizZone.StartQuiz();
                return;
            }

            var crossNpc = FindNearCrossingNpc();
            if (crossNpc != null)
            {
                UpdateHint(crossNpc.transform, "Press SPACE to talk");
                if (Input.GetKeyDown(KeyCode.Space)) crossNpc.Talk();
                return;
            }

            var talkNpc = FindNearDialogNpc();
            if (talkNpc != null)
            {
                UpdateHint(talkNpc.transform, "Press SPACE to talk");
                if (Input.GetKeyDown(KeyCode.Space)) talkNpc.StartDialogue();
                return;
            }

            Candidate = FindCandidate();
            if (Candidate != null)
            {
                // Spec 004: buildings with a minigame open it; others show the info popup
                UpdateHint(Candidate.transform,
                    Candidate.TaskGame != null ? "Press SPACE to start the task" : "Press SPACE");
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (Candidate.TaskGame != null) Candidate.TaskGame.Open();
                    else OpenUp(Candidate);
                }
            }
            else
            {
                UpdateHint(null, null);
            }
        }

        /// <summary>
        /// Facing check fixed: uses the player's last MOVE direction (PlayerController2D.FaceDir),
        /// not transform.up — the player never rotates (freezeRotation), so transform.up is
        /// always world-up and the old check silently rejected approaches from the south.
        /// </summary>
        private bool PlayerFacing(Vector2 toTarget)
        {
            var face = _pc != null ? _pc.FaceDir : Vector2.down;
            if (face.sqrMagnitude < 0.01f) face = Vector2.down;
            return Vector2.Dot(face, toTarget.normalized) >= facingDot;
        }

        private PlayerController2D _pc;

        private SafeStreets.QuizZone FindNearQuizZone()
        {
            // R8: edge distance (not center distance) — zones are big; a player
            // standing in a far corner must still get the "Press SPACE" prompt.
            foreach (var zone in FindObjectsByType<SafeStreets.QuizZone>(FindObjectsSortMode.None))
            {
                var col = zone.GetComponent<Collider2D>();
                float d = col != null
                    ? Vector2.Distance(col.ClosestPoint(_player.position), _player.position)
                    : Vector2.Distance(zone.transform.position, _player.position);
                if (d <= 2.6f) return zone;
            }
            return null;
        }

        private SafeStreets.CrossingNPC FindNearCrossingNpc()
        {
            foreach (var cnpc in FindObjectsByType<SafeStreets.CrossingNPC>(FindObjectsSortMode.None))
            {
                if (Vector2.Distance(cnpc.transform.position, _player.position) <= 2.2f) return cnpc;
            }
            return null;
        }

        private SafeStreets.DialogNPC FindNearDialogNpc()
        {
            foreach (var npc in FindObjectsByType<SafeStreets.DialogNPC>(FindObjectsSortMode.None))
            {
                if (Vector2.Distance(npc.transform.position, _player.position) <= 2.2f) return npc;
            }
            return null;
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
            // A modal (quiz/dialog/minigame) owns all input — clicking a building
            // behind it must not yank the interaction popup up underneath.
            if (TaskPaused) return;
            if (QuizManager.Instance != null && QuizManager.Instance.IsOpen) return;
            if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return;
            if (EdgeDistance(it, _player.position) > range + 0.8f) return;
            OpenUp(it);
        }

        // R9: popup text goes through TextFit — wrap + shrink inside the 2x1.25-scaled box
        private void SetPopupText(string s) => TextFit.Apply(_panelBody, s, boxW: 7.4f, boxH: 3.2f);

        private void OpenUp(Interactable it)
        {
            Open = it;
            ModalInputGate.SwallowSpacePress();   // R4: opening press is consumed
            UpdateHint(null, null);
            var player = GameObject.FindWithTag("Player");
            var pc = player?.GetComponent<PlayerController2D>();
            if (pc != null)
            {
                pc.Paused = true;                       // freezes FixedUpdate input
                pc.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;   // kill slide instantly
            }

            SetPopupText("You are looking at " + it.label + ".\n\n(More content coming soon.)");
            _panel.SetActive(true);
            PanelAnimator.Ensure(_panel).FadeIn();      // R6: GGD-style fade-in
            PositionPanel();
        }

        public void Close()
        {
            Open = null;
            ModalInputGate.SwallowSpacePress();   // R4: the closing press must not re-route
            // R6 (GGD-style): fade out, then deactivate; resume movement when hidden.
            // R5: state-validated resume — a newer popup/modal opened during the fade
            // owns the player; this stale callback must not unpause under it.
            PanelAnimator.Ensure(_panel).FadeOut(() =>
            {
                if (Open != null) return;   // a newer modal owns the player now
                var pc = GameObject.FindWithTag("Player")?.GetComponent<PlayerController2D>();
                if (pc != null) pc.Paused = false;
            });
        }

        /// <summary>
        /// GGD model: candidate = the Interactable whose Proximity zone the
        /// player is currently overlapping. Nearest zone-edge wins; ties broken
        /// by most-recent entry. No facing requirement — any approach counts.
        /// </summary>
        private Interactable FindCandidate()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            float bestEnter = float.MinValue;

            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                if (!it.IsPlayerInProximity) continue;           // GGD: overlap, not distance
                if (it == Open) continue;

                var zone = it.CurrentZone;
                float entered = zone != null ? zone.EnteredAt : float.MinValue;

                float dist = 0f;
                var col = it.GetComponent<Collider2D>();
                if (col != null && _player != null)
                    dist = Vector2.Distance(col.ClosestPoint(_player.position), _player.position);

                if (dist < bestDist - 0.05f ||                       // clearly nearer edge wins
                    (dist <= bestDist + 0.05f && entered > bestEnter)) // near-tie: latest entry
                {
                    bestDist = dist;
                    bestEnter = entered;
                    best = it;
                }
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

        private void UpdateHint(Transform target, string text)
        {
            if (target == null)
            {
                if (_hint != null) _hint.SetActive(false);
                return;
            }
            if (_hint == null)
            {
                _hint = new GameObject("InteractHint");
                var tm = _hint.AddComponent<TextMesh>();
                DialogStyle.Style(tm, 0.14f, 28, TextAnchor.MiddleCenter,
                    new Color(1f, 0.95f, 0.6f), 30);
                _hint.GetComponent<MeshRenderer>().sortingLayerName = "Decor";
            }
            _hint.GetComponent<TextMesh>().text = text;
            _hint.SetActive(true);
            _hint.transform.position = target.position + new Vector3(0f, 0.8f, -3f);
        }

        // ---- the "Interaction" popup ----

        private void BuildPanel()
        {
            _panel = new GameObject("InteractionPanel");

            var backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(_panel.transform, false);
            var sr = backdrop.AddComponent<SpriteRenderer>();
            // 4x4 white texture at PPU 1 = 4x4 world units natural; scale to 8x5 exactly.
            // (Sliced mode is banned — Tight-mesh degenerate geometry trap.)
            sr.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            sr.color = new Color(0.07f, 0.13f, 0.20f, 0.97f);
            backdrop.transform.localScale = new Vector3(2f, 1.25f, 1f);
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

            // Hidden until OpenUp shows it. (BuildPanel runs at Awake; without this the
            // panel is born visible and squats on screen from launch — the launch bug.)
            _panel.SetActive(false);
            // R9: follow the camera — a panel placed only at open drifts off-screen
            // while CameraFollow2D settles after the freeze.
            _panel.AddComponent<PopupFollowCamera>();
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
