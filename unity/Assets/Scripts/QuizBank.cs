using UnityEngine;

namespace SafeStreets
{
    /// <summary>Feature 002: the two question kinds the quiz cards support.</summary>
    public enum QuestionKind
    {
        TrueFalse,       // a statement answered TRUE or FALSE (right/wrong)
        MultipleChoice,  // a prompt with 2-4 labeled options
    }

    [System.Serializable]
    public class QuizQuestion
    {
        public QuestionKind kind = QuestionKind.MultipleChoice;

        [TextArea(2, 4)] public string prompt;

        [Tooltip("TrueFalse only: whether TRUE is the correct answer")]
        public bool trueIsCorrect;

        [Tooltip("MultipleChoice only: the answer options (up to 4)")]
        public string[] choices;

        [Tooltip("MultipleChoice only: index of the correct option")]
        public int correctIndex;

        [TextArea(1, 3), Tooltip("Citation shown with the right/wrong feedback")]
        public string source;
    }

    /// <summary>
    /// One quiz zone's content. Borrowed shape from Conversation (CC0 2DRPGKit):
    /// a ScriptableObject the scene build writes to Assets, one per web module.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuizBank", menuName = "SafeStreets/Quiz Bank")]
    public class QuizBank : ScriptableObject
    {
        [Tooltip("Backend activity id, e.g. 'le-crossing'")]
        public string activityId;

        [Tooltip("Short title shown on the card header")]
        public string title;

        public QuizQuestion[] questions;
    }
}
