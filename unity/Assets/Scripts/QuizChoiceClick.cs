using System;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>Clickable choice row for the world-space quiz card.</summary>
    public class QuizChoiceClick : MonoBehaviour
    {
        public Action onPick;

        private void OnMouseDown()
        {
            onPick?.Invoke();
        }

        private void OnMouseEnter()
        {
            var tm = GetComponent<TextMesh>();
            if (tm != null) tm.color = new Color(1f, 0.85f, 0.3f);
        }

        private void OnMouseExit()
        {
            var tm = GetComponent<TextMesh>();
            if (tm != null) tm.color = new Color(0.85f, 0.92f, 1f);
        }
    }
}
