using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Feature 002: a quiz zone. R8: walking in NO LONGER auto-starts the quiz —
    /// it works like every other interaction: stand near it, the router shows
    /// "Press SPACE for the quiz", and the press starts it.
    /// QuizManager still refuses while open, so mashing SPACE can't restart it.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class QuizZone : MonoBehaviour
    {
        public QuizBank bank;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        public void StartQuiz()
        {
            TryStart();
        }

        private bool TryStart()
        {
            if (bank == null) return false;
            if (InteractionManager.QuestWon) return false;   // R7: game is won — quizzes are over
            if (QuizManager.Instance == null || QuizManager.Instance.IsOpen) return false;
            if (DialogManager.Instance != null && DialogManager.Instance.IsOpen) return false;
            return QuizManager.Instance.StartQuiz(bank, gameObject, completed => { });
        }
    }
}
