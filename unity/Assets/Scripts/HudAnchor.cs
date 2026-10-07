using UnityEngine;

namespace SafeStreets
{
    /// <summary>Pins a TextMesh to the top-left of the ortho camera view every frame.</summary>
    public class HudAnchor : MonoBehaviour
    {
        private Camera _cam;
        private TextMesh _tm;
        private Vector3 _offset = new Vector3(-2f, 1.2f, 5f);   // relative to camera center

        private void Start()
        {
            _cam = Camera.main;
            _tm = GetComponent<TextMesh>();
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.position = _cam.transform.position +
                                 new Vector3(-_cam.orthographicSize * _cam.aspect * 0.85f,
                                              _cam.orthographicSize * 0.85f, 5f) + _offset * 0f;
        }
    }
}
