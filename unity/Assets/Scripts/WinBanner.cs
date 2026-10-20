using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Fired when the quest completes (all required crossings helped).
    /// Freezes the player, shows a celebration banner in the HUD slot.
    /// One instance; the scene builder wires the QuestManager event to it.
    /// </summary>
    public class WinBanner : MonoBehaviour
    {
        private TextMesh _hud;
        private Sprite _confettiSprite;   // shared: one sprite for all 24 particles, not 24 Sprite.Create leaks per win
        private static readonly Color Gold = new Color(1f, 0.84f, 0.3f);

        /// <summary>
        /// Self-subscribe at play start (NOT from the editor builder): the static
        /// OnAllCrossed event would otherwise hold the edit-mode banner instance,
        /// which dies on the edit->play transition — a dead-target subscription.
        /// </summary>
        private void Start()
        {
            QuestManager.OnAllCrossed += OnQuestComplete;
            _hud = GameObject.Find("QuestHUD")?.GetComponent<TextMesh>();
        }

        private void OnDestroy()
        {
            QuestManager.OnAllCrossed -= OnQuestComplete;
            if (_confettiSprite != null) Destroy(_confettiSprite);   // Sprite.Create'd assets are not GC'd with the GameObject
        }

        public static WinBanner Create(Transform parent)
        {
            var go = new GameObject("WinBanner");
            go.transform.SetParent(parent);
            return go.AddComponent<WinBanner>();
        }

        public void OnQuestComplete()
        {
            // R10: post-win is FREE ROAM — the banner celebrates but never freezes
            // the player. (The old terminal freeze + any modal that opened after
            // the win = permanently frozen player with dead input: the "Ada brick".)
            var playerGo = GameObject.FindWithTag("Player");
            // R7: dismiss anything that was open when the win fired — post-win
            // input routing is dead, so an open task/quiz/dialog/popup could never
            // close itself and would squat on the banner forever.
            var im = InteractionManager.Instance;
            if (im != null && im.Open != null) im.Close();
            var quiz = QuizManager.Instance;
            if (quiz != null && quiz.IsOpen) quiz.Close(false);
            var dlg = DialogManager.Instance;
            if (dlg != null && dlg.IsOpen) dlg.Close();
            foreach (var tp in FindObjectsByType<TaskPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (tp.gameObject.activeInHierarchy) tp.FailAndClose();

            // R10: free-roam guarantee. The modal resume callbacks decline to touch
            // movement post-win (R7 stale-close protection), so the win itself is the
            // last word: after dismissing everything, the player can ALWAYS move.
            if (playerGo != null)
            {
                var pcWin = playerGo.GetComponent<PlayerController2D>();
                if (pcWin != null) pcWin.Paused = false;
            }
            InteractionManager.ClearTaskOwnership();

            if (_hud != null)
            {
                // Demo polish: the HUD sits in the top-left corner at charSize 0.14, so a
                // two-line banner reached across the top half of the screen. One line,
                // wrapped/shrunk to the same slot the counter uses.
                TextFit.Apply(_hud, "ALL FRIENDS CROSSED - YOU DID IT!", boxW: 10f, boxH: 0.5f);
                _hud.color = Gold;
            }

            // simple confetti: 24 colored squares bursting from the player
            var colors = new[] { Gold, new Color(0.95f, 0.4f, 0.4f), new Color(0.4f, 0.8f, 0.95f),
                                 new Color(0.5f, 0.9f, 0.5f) };
            var origin = playerGo != null ? playerGo.transform.position : Vector3.zero;
            for (int i = 0; i < 24; i++)
            {
                var dir = new Vector3(Mathf.Cos(i * Mathf.PI / 12f), Mathf.Sin(i * Mathf.PI / 12f), 0f);
                StartCoroutine(Burst(origin, dir * Random.Range(2.5f, 4.5f),
                    colors[i % colors.Length], 1.6f));
            }
        }

        private System.Collections.IEnumerator Burst(Vector3 from, Vector3 vel, Color color, float life)
        {
            var go = new GameObject("confetti");
            go.transform.position = from;
            var sr = go.AddComponent<SpriteRenderer>();
            // runtime-safe square (Editor-only SceneGeo is not visible from runtime asmdef)
            if (_confettiSprite == null)                                        // shared across all 24 particles
                _confettiSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4),
                    new Vector2(0.5f, 0.5f), 5f, 0, SpriteMeshType.FullRect);   // 4px @ PPU5 = 0.8 units
            sr.sprite = _confettiSprite;
            sr.color = color;
            sr.sortingOrder = 70;
            go.transform.localScale = new Vector3(0.31f, 0.31f, 1f);        // ~0.25 world units

            float t = 0f;
            while (t < life)
            {
                t += Time.deltaTime;
                go.transform.position += vel * Time.deltaTime;
                vel *= 0.96f;
                yield return null;
            }
            Destroy(go);
        }
    }
}
