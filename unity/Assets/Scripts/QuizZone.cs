using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// A walk-into trigger that opens a question card. Pauses the player while open.
    /// On finish, optionally POSTs the result to the web backend (activityId != "").
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class QuizZone : MonoBehaviour
    {
        public string zoneId;
        public string title;
        public string activityId = "";

        private bool _done;
        private bool _inside;

        private void Reset()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_done || _inside) return;
            if (!other.CompareTag("Player")) return;
            _inside = true;
            var def = QuestionBank.ByZoneId(zoneId);
            if (def == null) return;
            StartCoroutine(QuizUI.Instance.Show(def, activityId, result => { _done = result.completed; _inside = false; }));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player")) _inside = false;
        }
    }
}
