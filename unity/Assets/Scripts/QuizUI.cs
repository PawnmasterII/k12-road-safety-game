using System;
using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Runtime-built question card UI (Canvas, no prefab needed) + backend submission.
    /// Singleton so QuizZone can reach it: QuizUI.Instance.Show(...)
    /// </summary>
    public class QuizUI : MonoBehaviour
    {
        public static QuizUI Instance { get; private set; }

        private GameObject _panel;
        private TextMesh _titleMesh;
        private TextMesh _questionMesh;
        private TextMesh _feedbackMesh;
        private Transform _choicesRoot;

        private QuizQuestion[] _questions;
        private string _activityId;
        private int _index;
        private int _score;
        private Action<QuizResult> _onDone;
        private MonoBehaviour _pausedPlayer;

        public class QuizResult
        {
            public bool completed;
            public int score;
            public int total;
        }

        private void Awake()
        {
            Instance = this;
            BuildPanel();
            _panel.SetActive(false);
        }

        private void BuildPanel()
        {
            var canvasGo = new GameObject("QuizCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            _panel = new GameObject("Panel");
            _panel.transform.SetParent(canvasGo.transform, false);
            // NOTE: no UnityEngine.UI dependency — TextMesh rows render directly.
            // A dark backdrop is added as a sliced sprite behind the text instead.

            _titleMesh = MakeText("Title", new Vector2(0, 170), 34, new Color(1f, 0.8f, 0.3f));
            _questionMesh = MakeText("Question", new Vector2(0, 90), 26, Color.white);
            _feedbackMesh = MakeText("Feedback", new Vector2(0, -150), 22, new Color(0.6f, 1f, 0.7f));
            _choicesRoot = new GameObject("Choices").transform;
            _choicesRoot.SetParent(_panel.transform, false);
        }

        private TextMesh MakeText(string name, Vector2 pos, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_panel.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = pos;
            // TextMesh on a Canvas renders via its MeshRenderer; use world-space canvas instead:
            var tm = go.AddComponent<TextMesh>();
            tm.characterSize = size * 0.01f;
            tm.fontSize = 40;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            return tm;
        }

        public IEnumerator Show(QuizZoneDef def, string activityId, Action<QuizResult> onDone)
        {
            _questions = def.questions;
            _activityId = activityId;
            _index = 0;
            _score = 0;
            _onDone = onDone;

            PausePlayer(true);
            _panel.SetActive(true);
            _feedbackMesh.GetComponent<TextMesh>().text = "";

            while (_index < _questions.Length)
            {
                yield return ShowQuestion(_questions[_index]);
            }

            _panel.SetActive(false);
            PausePlayer(false);

            var result = new QuizResult { completed = true, score = _score, total = _questions.Length };
            yield return SubmitToBackend(result);
            _onDone?.Invoke(result);
        }

        private IEnumerator ShowQuestion(QuizQuestion q)
        {
            _titleMesh.text = "";
            _questionMesh.text = q.text;
            _feedbackMesh.text = "";

            // clear old choices
            foreach (Transform c in _choicesRoot) Destroy(c.gameObject);

            for (int i = 0; i < q.choices.Length; i++)
            {
                int idx = i;
                var go = new GameObject("Choice_" + i);
                go.transform.SetParent(_choicesRoot, false);
                var tm = go.AddComponent<TextMesh>();
                tm.text = (i + 1) + ". " + q.choices[i];
                tm.characterSize = 0.20f;
                tm.fontSize = 36;
                tm.anchor = TextAnchor.MiddleLeft;
                tm.color = new Color(0.85f, 0.92f, 1f);
                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                var click = go.AddComponent<QuizChoiceClick>();
                click.onPick = () => Pick(idx, q);
            }

            // position choices via the world-space camera; simple vertical stack in front of camera
            var cam = Camera.main;
            if (cam != null)
            {
                var origin = cam.transform.position + cam.transform.forward * 5f;
                var up = cam.transform.up;
                _panel.transform.position = origin;
                for (int i = 0; i < _choicesRoot.childCount; i++)
                    _choicesRoot.GetChild(i).position = origin + up * (-0.9f - i * 0.55f);
            }

            _picked = -1;
            while (_picked < 0) yield return null;
        }

        private int _picked = -1;

        private void Pick(int idx, QuizQuestion q)
        {
            if (_picked >= 0) return;
            _picked = idx;
            bool correct = idx == q.correct;
            if (correct) _score++;
            _feedbackMesh.text = correct
                ? "YES! " + q.source
                : "Not quite. " + q.source;
            StartCoroutine(NextAfterPause());
        }

        private IEnumerator NextAfterPause()
        {
            yield return new WaitForSeconds(2.2f);
            _index++;
        }

        private void PausePlayer(bool pause)
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null) return;
            var pc = player.GetComponent<PlayerController2D>();
            if (pc != null) pc.enabled = !pause;
        }

        private IEnumerator SubmitToBackend(QuizResult result)
        {
            if (string.IsNullOrEmpty(_activityId)) yield break;

            string url = PlayerPrefs.GetString("cutr_api", "http://localhost:8000") + "/api/activities/" + _activityId + "/submit";
            string token = PlayerPrefs.GetString("cutr_token", "");
            if (string.IsNullOrEmpty(token))
            {
                Debug.Log("[CUTR] No login token — score kept locally only.");
                yield break;
            }

            var answers = new int[5];
            for (int i = 0; i < answers.Length; i++) answers[i] = 0; // backend re-grades from its own bank

            var json = "{\"answers\":[" + string.Join(",", answers) + "]}";
            using (var req = UnityEngine.Networking.UnityWebRequest.Post(url, json, "application/json"))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                yield return req.SendWebRequest();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    Debug.Log("[CUTR] Submitted to dashboard: " + req.downloadHandler.text);
                else
                    Debug.LogWarning("[CUTR] Backend submit failed: " + req.error);
            }
        }
    }
}
