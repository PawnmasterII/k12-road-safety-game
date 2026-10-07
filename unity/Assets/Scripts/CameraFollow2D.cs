using UnityEngine;

namespace SafeStreets
{
    /// <summary>Orthographic follow camera for the top-down view.</summary>
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 0f, -10f);
        public float smooth = 5f;

        private void LateUpdate()
        {
            if (target == null) return;
            // offset includes the z pull-back (camera looks toward +z, so it sits at player.z - 10).
            // Do NOT clamp z to offset.z — that puts the camera ON the player's plane and the
            // player sprite stops rendering (the "invisible character" bug).
            Vector3 want = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, want, smooth * Time.deltaTime);
        }
    }
}
