using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Project.Strapping
{
    /// <summary>右键轨道相机，滚轮缩放。</summary>
    public class StrappingOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform m_target;
        [SerializeField] private float m_distance = 4.5f;
        [SerializeField] private float m_minDistance = 2f;
        [SerializeField] private float m_maxDistance = 12f;
        [SerializeField] private Vector2 m_pitchRange = new Vector2(8f, 75f);

        private float _yaw = 35f;
        private float _pitch = 28f;

        private void LateUpdate()
        {
            if (m_target == null)
                return;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue();
                    _yaw += delta.x * 0.25f;
                    _pitch -= delta.y * 0.2f;
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    m_distance -= scroll * 0.01f;
            }
#else
            if (Input.GetMouseButton(1))
            {
                _yaw += Input.GetAxis("Mouse X") * 3f;
                _pitch -= Input.GetAxis("Mouse Y") * 3f;
            }

            m_distance -= Input.GetAxis("Mouse ScrollWheel") * 1.2f;
#endif

            _pitch = Mathf.Clamp(_pitch, m_pitchRange.x, m_pitchRange.y);
            m_distance = Mathf.Clamp(m_distance, m_minDistance, m_maxDistance);

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -m_distance);
            transform.position = m_target.position + offset;
            transform.rotation = rot;
        }

        public void SetTarget(Transform target) => m_target = target;
    }
}
