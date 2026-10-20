using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Feature 002: question popups. Opens a centered quiz card (player frozen),
    /// one question at a time, two question kinds:
    ///   TrueFalse      - statement answered TRUE or FALSE (keys 1/2 or T/F, or click)
    ///   MultipleChoice - up to 4 options (keys 1-4, or click)
    /// Every answer gets instant right/wrong feedback with its source citation;
    /// finishing shows the score. ESC or the X abandons (no completion logged).
    /// </summary>
    public class QuizManager : MonoBehaviour
    {
        public static QuizManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public GameObject Source { get; private set; }

        [Tooltip("Seconds the right/wrong feedback stays up (live-tunable for CLI testing)")]
        public float feedbackSeconds = 2.2f;

        private QuizBank _bank;
        private int _q;
        private int _score;
        private bool _locked;      // feedback showing — ignore answer input
        private bool _finished;    // results screen — SPACE closes
        private System.Action<bool> _onDone;

        private GameObject _panel;
        private TextMesh _header;
        private TextMesh _prompt;
        private TextMesh _feedback;
        private readonly GameObject[] _rows = new GameObject[4];
        private readonly TextMesh[] _rowLabels = new TextMesh[4];
        private readonly GameObject[] _rowBars = new GameObject[4];

        // ---- demo-polish layout constants (world units, panel is 9.6 x 7.6) ----
        private const float RowWidth = 9.2f;
        private const float RowStep = 0.95f;      // vertical pitch between answer rows
        private const float RowTop = 0.75f;       // first row center
        private const float RowTextW = 8.6f;      // label column inside the bar
        private const float TextLeft = -4.35f;    // label left edge (MiddleLeft anchor)
        private const float RowTextLineH = 0.52f; // one wrapped line at charSize 0.15

        private static readonly Color PanelCol = new Color(0.07f, 0.13f, 0.20f, 0.97f);
        private static readonly Color RowCol = new Color(0.15f, 0.23f, 0.34f);
        private static readonly Color GoodCol = new Color(0.30f, 0.78f, 0.38f);
        private static readonly Color BadCol = new Color(0.85f, 0.32f, 0.28f);
        private static readonly Color HintCol = new Color(0.70f, 0.80f, 0.90f);
        private static readonly Color TitleCol = new Color(1f, 0.82f, 0.35f);

        private void Awake()
        {
            Instance = this;
            Build();
            _panel.SetActive(false);
        }

        // ---- panel construction (script-created TextMeshes need the font fix) ----

        private void Build()
        {
            _panel = new GameObject("QuizPanel");

            var backdropGo = new GameObject("Backdrop");
            backdropGo.transform.SetParent(_panel.transform, false);
            var backdrop = backdropGo.AddComponent<SpriteRenderer>();
            // FullRect + scale, never Sliced (Tight-mesh degenerate geometry trap)
            backdrop.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
            backdrop.color = PanelCol;
            backdrop.sortingOrder = 55;
            backdropGo.transform.localScale = new Vector3(2.4f, 1.9f, 1f);   // 9.6 x 7.6 units

            _header = MakeText("Header", 0.17f, 30, TextAnchor.MiddleCenter, TitleCol, 56);
            _header.transform.localPosition = new Vector3(0f, 3.35f, 0f);

            _prompt = MakeText("Prompt", 0.19f, 32, TextAnchor.UpperLeft, Color.white, 56);
            _prompt.transform.localPosition = new Vector3(-4.4f, 2.9f, 0f);

            for (int i = 0; i < 4; i++)
            {
                float y = RowTop - i * RowStep;
                var row = new GameObject("Choice" + i);
                row.transform.SetParent(_panel.transform, false);
                row.transform.localPosition = new Vector3(0f, y, 0f);

                var barGo = new GameObject("Bar");
                barGo.transform.SetParent(row.transform, false);
                var bar = barGo.AddComponent<SpriteRenderer>();
                bar.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                    new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
                bar.color = RowCol;
                bar.sortingOrder = 56;
                barGo.transform.localScale = new Vector3(2.3f, 0.21f, 1f);   // 9.2 x 0.84

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(row.transform, false);
                var tm = labelGo.AddComponent<TextMesh>();
                DialogStyle.Style(tm, 0.15f, 30, TextAnchor.MiddleLeft, Color.white, 57);
                labelGo.transform.localPosition = new Vector3(TextLeft, 0f, 0f);

                var col = row.AddComponent<BoxCollider2D>();
                col.size = new Vector2(9.2f, 0.84f);
                var btn = row.AddComponent<QuizChoiceButton>();
                btn.index = i;

                _rows[i] = row;
                _rowLabels[i] = tm;
                _rowBars[i] = barGo;
            }

            _feedback = MakeText("Feedback", 0.14f, 28, TextAnchor.UpperCenter, HintCol, 56);
            _feedback.transform.localPosition = new Vector3(0f, -2.75f, 0f);

            var xGo = new GameObject("X");
            xGo.transform.SetParent(_panel.transform, false);
            var xtm = xGo.AddComponent<TextMesh>();
            DialogStyle.Style(xtm, 0.3f, 40, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.35f), 58);
            xGo.transform.localPosition = new Vector3(4.35f, 3.4f, 0f);
            var xcol = xGo.AddComponent<BoxCollider2D>();
            xcol.size = new Vector2(0.9f, 0.9f);
            xGo.AddComponent<QuizCloseButton>();

            _panel.SetActive(false);   // born hidden (the launch bug taught us)
            // R9: the card follows the camera — a card placed only at open drifts
            // off-screen while CameraFollow2D settles (feedback line clipped live).
            _panel.AddComponent<PopupFollowCamera>();
        }

        private TextMesh MakeText(string name, float charSize, int fontSize,
            TextAnchor anchor, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panel.transform, false);
            var tm = go.AddComponent<TextMesh>();
            DialogStyle.Style(tm, charSize, fontSize, anchor, color, order);
            tm.alignment = anchor == TextAnchor.UpperLeft ? TextAlignment.Left : TextAlignment.Center;
            return tm;
        }

        // ---- flow ----

        public bool StartQuiz(QuizBank bank, GameObject source, System.Action<bool> onDone)
        {
            if (bank == null || bank.questions == null || bank.questions.Length == 0) return false;
            if (ModalInputGate.SpaceBlocked) return false;   // R4 choke point (see StartDialog)
            if (IsOpen) return false;
            if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return false;

            _bank = bank;
            Source = source;
            _onDone = onDone;
            _q = 0;
            _score = 0;
            _locked = false;
            _finished = false;
            IsOpen = true;

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController2D>();
                if (pc != null)
                {
                    pc.Paused = true;
                    pc.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                }
            }

            _panel.SetActive(true);
            PanelAnimator.Ensure(_panel).FadeIn();
            PositionPanel();
            ShowQuestion();
            return true;
        }

        private void ShowQuestion()
        {
            var q = _bank.questions[_q];
            string kind = q.kind == QuestionKind.TrueFalse ? "TRUE or FALSE" : "MULTIPLE CHOICE";
            // Demo polish (header fit): the full "title - Q1/5 - MULTIPLE CHOICE" string is
            // 40+ chars and used to run past the card edges at charSize 0.17 (and the X
            // sits at the same height). Progress now leads, long titles shrink via TextFit.
            TextFit.Apply(_header, $"Q{_q + 1}/{_bank.questions.Length}  -  {kind}  -  {_bank.title}",
                boxW: 8.0f, boxH: 0.36f);
            _prompt.anchor = TextAnchor.UpperLeft;
            _prompt.alignment = TextAlignment.Left;
            _prompt.transform.localPosition = new Vector3(-4.4f, 2.9f, 0f);
            // R9: wrap + shrink to the panel — the prompt can never spill out
            TextFit.Apply(_prompt, q.prompt, boxW: 8.8f, boxH: 1.85f);

            int rows;
            if (q.kind == QuestionKind.TrueFalse)
            {
                rows = 2;
                TextFit.Apply(_rowLabels[0], "1) TRUE", boxW: RowTextW, boxH: 0.45f);
                TextFit.Apply(_rowLabels[1], "2) FALSE", boxW: RowTextW, boxH: 0.45f);
            }
            else
            {
                rows = Mathf.Min(q.choices.Length, 4);
                for (int i = 0; i < rows; i++)
                    TextFit.Apply(_rowLabels[i], $"{i + 1}) {q.choices[i]}", boxW: RowTextW, boxH: 0.6f);
            }
            LayoutRows(rows);
            _feedback.color = HintCol;
            TextFit.Apply(_feedback, "Click an answer or press its number", boxW: 9.0f, boxH: 1.2f);
        }

        /// <summary>
        /// Demo polish: size and stack the answer rows to the CONTENT. The fixed 0.84-tall
        /// bars clipped two-line answers (the city banks have 55-char options) and wasted
        /// 3.7 units of card whenever a bank had only a few questions. Each bar now gets
        /// one or two line-heights, rows stack downward from RowTop, and every row keeps a
        /// click collider matched to its new size.
        /// </summary>
        private void LayoutRows(int rows)
        {
            float y = RowTop;
            for (int i = 0; i < _rows.Length; i++)
            {
                bool on = i < rows;
                _rows[i].SetActive(on);
                if (!on) continue;

                int lineCount = TextFit.LineCount(_rowLabels[i].text);
                float h = Mathf.Clamp(0.30f + (lineCount - 1) * RowTextLineH, 0.84f, 1.5f);
                // The bar sprite is a 4x4 white texture at PPU 1 => 4 world units natural,
                // so its localScale is (width, height) / 4.
                _rowBars[i].transform.localScale = new Vector3(RowWidth / 4f, h / 4f, 1f);
                var col = _rows[i].GetComponent<BoxCollider2D>();
                if (col != null) col.size = new Vector2(RowWidth, h);
                _rows[i].transform.localPosition = new Vector3(0f, y, 0f);
                var sr = _rowBars[i].GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = RowCol;
                y -= h + 0.11f;                                  // 0.11 gap between rows
            }
        }

        public void PressChoice(int index)
        {
            if (!IsOpen || _locked || _finished) return;
            if (_bank == null) return;
            var q = _bank.questions[_q];
            int rows = q.kind == QuestionKind.TrueFalse ? 2 : Mathf.Min(q.choices.Length, 4);
            if (index < 0 || index >= rows) return;
            if (q.kind != QuestionKind.TrueFalse && q.choices == null) return;

            int correctIdx = q.kind == QuestionKind.TrueFalse ? (q.trueIsCorrect ? 0 : 1) : q.correctIndex;
            correctIdx = Mathf.Clamp(correctIdx, 0, rows - 1);   // authored data can't crash a demo
            bool right = index == correctIdx;
            if (right) _score++;

            for (int i = 0; i < rows; i++)
            {
                var sr = _rowBars[i] != null ? _rowBars[i].GetComponent<SpriteRenderer>() : null;
                if (sr == null) continue;
                if (i == correctIdx) sr.color = GoodCol;
                else if (i == index) sr.color = BadCol;
            }

            _feedback.color = right ? GoodCol : BadCol;
            // R9: feedback carries the citation — often the longest string on the card
            TextFit.Apply(_feedback,
                right
                    ? "Correct!  " + q.source
                    : "Not quite - the answer is \"" + ChoiceText(q, correctIdx) + "\".  " + q.source,
                boxW: 9.0f, boxH: 1.2f);
            _locked = true;
            StartCoroutine(NextIn());
        }

        private IEnumerator NextIn()
        {
            yield return new WaitForSeconds(feedbackSeconds);
            _locked = false;
            _q++;
            if (_q < _bank.questions.Length) ShowQuestion();
            else ShowResults();
        }

        private void ShowResults()
        {
            _finished = true;
            int n = _bank.questions.Length;
            _header.text = "Quiz complete";
            _prompt.anchor = TextAnchor.MiddleCenter;
            _prompt.alignment = TextAlignment.Center;   // non-UpperLeft anchor => centered lines (MakeText rule); was ragged-Left from ShowQuestion
            _prompt.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            string praise = _score == n ? "Perfect!" : _score * 2 >= n ? "Good job!" : "Keep practicing!";
            TextFit.Apply(_prompt, $"{_bank.title}: you got {_score} / {n}.\n{praise}", boxW: 8.8f, boxH: 3.4f);
            // Closable from the keyboard: the results card has no answers, so SPACE could
            // not do anything and ESC-or-X were the only ways out of a finished quiz.
            for (int i = 0; i < _rows.Length; i++) _rows[i].SetActive(false);
            _feedback.color = HintCol;
            TextFit.Apply(_feedback, "Press SPACE to finish", boxW: 9.0f, boxH: 1.2f);

            // Results row: a clickable "Finish" bar in the first answer slot, so the demo
            // can be driven with the mouse alone (and keyboard 1) as well as SPACE.
            _rows[0].transform.localPosition = new Vector3(0f, 0.1f, 0f);
            _rowBars[0].transform.localScale = new Vector3(RowWidth / 4f, 0.84f / 4f, 1f);
            var col0 = _rows[0].GetComponent<BoxCollider2D>();
            if (col0 != null) col0.size = new Vector2(RowWidth, 0.84f);
            var sr0 = _rowBars[0].GetComponent<SpriteRenderer>();
            if (sr0 != null) sr0.color = RowCol;
            TextFit.Apply(_rowLabels[0], "1) Finish", boxW: RowTextW, boxH: 0.45f);
            _rows[0].SetActive(true);
            Debug.Log($"[Quiz] completed activity={_bank.activityId} score={_score}/{n}");
        }

        public void Close(bool completed)
        {
            if (!IsOpen) return;
            ModalInputGate.SwallowSpacePress();   // R4: the closing press must not re-route
            IsOpen = false;
            Source = null;
            StopAllCoroutines();
            // R6 (GGD-style): fade out, THEN deactivate; resume movement when hidden
            var pa = PanelAnimator.Ensure(_panel);
            pa.FadeOut(() =>
            {
                // R5: state-validated resume — a newer dialog or task panel opened
                // during the fade owns the player; this stale callback must not
                // unpause under it. (R10: no special win case — free-roam win
                // guarantees movement via WinBanner, modals resume normally.)
                if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return;
                if (InteractionManager.TaskPaused) return;
                var player = GameObject.FindWithTag("Player");
                var pc = player != null ? player.GetComponent<PlayerController2D>() : null;
                if (pc != null) pc.Paused = false;
            });
            if (!completed && !_finished && _bank != null)
                Debug.Log($"[Quiz] abandoned activity={_bank.activityId}");
            _bank = null;
            var cb = _onDone;
            _onDone = null;
            cb?.Invoke(completed);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (!ModalInputGate.InputAllowed) return;   // R4/R5: swallowed press OR unfocused editor
            if (Input.GetKeyDown(KeyCode.Escape)) { Close(false); return; }
            if (_locked) return;
            if (_finished)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Alpha1)
                    || Input.GetKeyDown(KeyCode.Return)) Close(true);
                return;
            }
            var q = _bank.questions[_q];
            if (q.kind == QuestionKind.TrueFalse)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.T)) PressChoice(0);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.F)) PressChoice(1);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) PressChoice(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) PressChoice(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) PressChoice(2);
                if (Input.GetKeyDown(KeyCode.Alpha4)) PressChoice(3);
            }
        }

        private void PositionPanel()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _panel.transform.position = cam.transform.position + cam.transform.forward * 5f;
        }

        private static string ChoiceText(QuizQuestion q, int i)
            => q.kind == QuestionKind.TrueFalse ? (i == 0 ? "TRUE" : "FALSE") : q.choices[i];
    }

    /// <summary>One clickable answer row (bar + label).</summary>
    public class QuizChoiceButton : MonoBehaviour
    {
        public int index;

        private void OnMouseDown()
        {
            if (QuizManager.Instance != null) QuizManager.Instance.PressChoice(index);
        }
    }

    /// <summary>The X in the corner — abandons the quiz.</summary>
    public class QuizCloseButton : MonoBehaviour
    {
        private void OnMouseDown()
        {
            if (QuizManager.Instance != null) QuizManager.Instance.Close(false);
        }
    }
}
