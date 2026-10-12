using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Feature 002: a walk-in quiz zone. Player enters the trigger -> the quiz
    /// card opens (player frozen). Standing inside and pressing SPACE also
    /// starts it (InteractionManager routes that here). The zone re-arms when
    /// the player leaves, so walking out and back restarts the quiz.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class QuizZone : MonoBehaviour
    {
        public QuizBank bank;

        private bool _armed = true;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || !_armed) return;
            _armed = false;                        // one start attempt per entry
            if (!TryStart()) _armed = true;        // refused (dialog open etc.) — retry next frame
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player")) _armed = true;
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
