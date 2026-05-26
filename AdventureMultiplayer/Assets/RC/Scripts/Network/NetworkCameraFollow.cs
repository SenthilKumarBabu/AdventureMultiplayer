using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Fixed third-person follow camera.
    /// Maintains a fixed world-space offset from the player — does not rotate
    /// with the player or respond to mouse input.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Camera Follow")]
    public class NetworkCameraFollow : MonoBehaviour
    {
        [Header("Offset")]
        [SerializeField] private Vector3 offset = new Vector3(-10f, 8f, 0f);

        [Header("Smoothing")]
        [SerializeField] private float positionSmooth = 10f;
        [SerializeField] private float lookAtSmooth   = 8f;

        private Transform m_target;
        private Vector3   m_velocity;
        private Vector3   m_smoothedLookAt;
        private Vector3   m_lookAtVelocity;

        /// <summary>
        /// Called by NetworkCameraTarget when the local player spawns.
        /// </summary>
        public void SetTarget(Transform target)
        {
            m_target         = target;
            m_smoothedLookAt = target.position;
            m_lookAtVelocity = Vector3.zero;
            m_velocity       = Vector3.zero;

            // Snap camera to correct position immediately on spawn.
            transform.position = target.position + offset;
            transform.LookAt(target.position);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            Debug.Log($"[NetworkCameraFollow] Following '{target.name}'");
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void LateUpdate()
        {
            if (m_target == null) return;

            // Smooth look-at point to avoid Y-snap jitter on ledge steps.
            m_smoothedLookAt = Vector3.SmoothDamp(
                m_smoothedLookAt, m_target.position, ref m_lookAtVelocity,
                1f / lookAtSmooth, Mathf.Infinity, Time.deltaTime);

            // Desired position is always the fixed offset from the player.
            var desiredPos = m_target.position + offset;

            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPos, ref m_velocity,
                1f / positionSmooth, Mathf.Infinity, Time.deltaTime);

            transform.LookAt(m_smoothedLookAt);
        }
    }
}
