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
            Vector3 want = target.position + offset;
            want.z = offset.z;                    // keep ortho camera plane fixed
            transform.position = Vector3.Lerp(transform.position, want, smooth * Time.deltaTime);
        }
    }
}
