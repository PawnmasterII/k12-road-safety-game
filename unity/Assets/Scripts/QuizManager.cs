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
                float y = 0.75f - i * 0.95f;
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
                labelGo.transform.localPosition = new Vector3(-4.35f, 0.05f, 0f);

                var col = row.AddComponent<BoxCollider2D>();
                col.size = new Vector2(9.2f, 0.84f);
                var btn = row.AddComponent<QuizChoiceButton>();
                btn.index = i;

                _rows[i] = row;
                _rowLabels[i] = tm;
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
            _header.text = $"{_bank.title}  -  Q{_q + 1}/{_bank.questions.Length}  -  {kind}";
            _prompt.anchor = TextAnchor.UpperLeft;
            _prompt.transform.localPosition = new Vector3(-4.4f, 2.9f, 0f);
            _prompt.text = Wrap(q.prompt, 58);

            int rows;
            if (q.kind == QuestionKind.TrueFalse)
            {
                rows = 2;
                _rowLabels[0].text = "1) TRUE";
                _rowLabels[1].text = "2) FALSE";
            }
            else
            {
                rows = Mathf.Min(q.choices.Length, 4);
                for (int i = 0; i < rows; i++)
                    _rowLabels[i].text = Wrap($"{i + 1}) {q.choices[i]}", 56);
            }
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].SetActive(i < rows);
                if (i < rows) _rows[i].GetComponentInChildren<SpriteRenderer>().color = RowCol;
            }
            _feedback.text = "Click an answer or press its number";
            _feedback.color = HintCol;
        }

        public void PressChoice(int index)
        {
            if (!IsOpen || _locked || _finished) return;
            var q = _bank.questions[_q];
            int rows = q.kind == QuestionKind.TrueFalse ? 2 : Mathf.Min(q.choices.Length, 4);
            if (index < 0 || index >= rows) return;

            int correctIdx = q.kind == QuestionKind.TrueFalse ? (q.trueIsCorrect ? 0 : 1) : q.correctIndex;
            bool right = index == correctIdx;
            if (right) _score++;

            for (int i = 0; i < rows; i++)
            {
                var sr = _rows[i].GetComponentInChildren<SpriteRenderer>();
                if (i == correctIdx) sr.color = GoodCol;
                else if (i == index) sr.color = BadCol;
            }

            _feedback.color = right ? GoodCol : BadCol;
            _feedback.text = right
                ? Wrap("Correct!  " + q.source, 74)
                : Wrap("Not quite - the answer is \"" + ChoiceText(q, correctIdx) + "\".  " + q.source, 74);
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
            _prompt.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            string praise = _score == n ? "Perfect!" : _score * 2 >= n ? "Good job!" : "Keep practicing!";
            _prompt.text = $"{_bank.title}: you got {_score} / {n}.\n{praise}";
            for (int i = 0; i < _rows.Length; i++) _rows[i].SetActive(false);
            _feedback.text = "Press SPACE to finish";
            _feedback.color = HintCol;
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
                // unpause under it.
                if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return;
                if (InteractionManager.TaskPaused) return;
                if (InteractionManager.QuestWon) return;   // R7: the win-freeze owns the player
                var player = GameObject.FindWithTag("Player");
                if (player != null) player.GetComponent<PlayerController2D>().Paused = false;
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
                if (Input.GetKeyDown(KeyCode.Space)) Close(true);
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

        /// <summary>TextMesh has no auto-wrap: break on spaces at maxChars.</summary>
        internal static string Wrap(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars) return text;
            var sb = new System.Text.StringBuilder();
            var line = "";
            foreach (var w in text.Split(' '))
            {
                if (line.Length == 0) line = w;
                else if (line.Length + 1 + w.Length <= maxChars) line += " " + w;
                else { sb.Append(line).Append("\n"); line = w; }
            }
            if (line.Length > 0) sb.Append(line);
            return sb.ToString();
        }
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
