using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// R9: popups must FOLLOW THE CAMERA, not be placed once. The camera keeps
    /// lerping (CameraFollow2D) after a modal freezes the player, so a card
    /// positioned only at open drifts off-screen (found live: the quiz feedback
    /// line sat at viewport y=-0.03, clipped). Attach to a panel root; every
    /// LateUpdate it re-pins the panel relative to the camera.
    ///   bottomAnchor 0    -> centered on the camera (quiz card, interaction popup)
    ///   bottomAnchor 0.55 -> pushed toward the bottom (dialog box, GGD style)
    /// z stays camera-relative (+5 in front). TaskPanel already solves this with
    /// its own TaskPanelFollowCamera; this is the shared version for the rest.
    /// </summary>
    public class PopupFollowCamera : MonoBehaviour
    {
        [Range(0f, 0.9f)] public float bottomAnchor = 0f;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || !gameObject.activeInHierarchy) return;
            float y = cam.transform.position.y;
            if (bottomAnchor > 0f) y -= cam.orthographicSize * bottomAnchor;
            transform.position = new Vector3(cam.transform.position.x, y,
                cam.transform.position.z + 5f);
        }
    }
}
