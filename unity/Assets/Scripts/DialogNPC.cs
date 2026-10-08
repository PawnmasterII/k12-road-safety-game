using System.Collections;
using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Borrowed from 2DRPGKit's DialogStarter (CC0), simplified for SafeStreets:
    /// an NPC/sign with a Conversation. Player walks up, faces it, presses SPACE
    /// (or clicks) — the DialogManager shows the conversation; the player is frozen
    /// while it's open. Advances on SPACE, closes at the end.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class DialogNPC : MonoBehaviour
    {
        public Conversation conversation;
        public string interactLabel = "someone";

        private bool _playerNear;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player")) _playerNear = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerNear = false;
                // if the popup is open and the player is gone (shouldn't happen while frozen), close
                if (DialogManager.Instance != null && DialogManager.Instance.IsOpen &&
                    DialogManager.Instance.Source == gameObject)
                    DialogManager.Instance.Close();
            }
        }

        /// <summary>
        /// Called by InteractionManager's input router (single SPACE owner).
        /// Kept coarse: any nearness counts — the router only routes when the player
        /// is within its own range, so this stays simple.
        /// </summary>
        public void StartDialogue()
        {
            if (!_playerNear) _playerNear = true;   // router already gated on distance
            if (DialogManager.Instance == null || DialogManager.Instance.IsOpen) return;
            DialogManager.Instance.StartDialog(conversation, gameObject);
        }
    }
}
