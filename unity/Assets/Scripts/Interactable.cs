using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Put on anything the player can interact with (buildings, signs...).
    /// Click opens the interaction popup (when the player is within range).
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public string label = "something";

        private void OnMouseDown()
        {
            InteractionManager.Instance.TryClickOpen(this);
        }
    }
}
