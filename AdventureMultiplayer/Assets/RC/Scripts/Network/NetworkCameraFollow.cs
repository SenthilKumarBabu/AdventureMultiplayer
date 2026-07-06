using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Fixed third-person follow camera.
    /// Sits directly behind the player (-Z world) at a set height and distance.
    /// Y rotation is always 0 — the camera never rotates, only translates.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Camera Follow")]
    public class NetworkCameraFollow : MonoBehaviour
    {
        [Header("Camera Position")]
        [Tooltip("How many units above the player the camera sits.")]
        [SerializeField] private float height   = 10f;
        [Tooltip("How many units behind the player (-Z world) the camera sits.")]
        [SerializeField] private float distance = 15f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmooth = 8f;

        private Transform m_target;
        private Vector3   m_velocity;
        private Camera    m_camera;

        private void Awake()
        {
            m_camera = GetComponent<Camera>();
            if (m_camera != null) m_camera.enabled = false;
        }

        /// <summary>Called by NetworkCameraTarget when the local player spawns.</summary>
        public void SetTarget(Transform target)
        {
            m_target   = target;
            m_velocity = Vector3.zero;

            // Snap immediately to the correct position and rotation.
            transform.position = DesiredPos();
            transform.rotation = DesiredRot();
            if (m_camera != null) m_camera.enabled = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            Debug.Log($"[NetworkCameraFollow] Following '{target.name}' | pos={transform.position} | rot={transform.rotation.eulerAngles}");
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        private void LateUpdate()
        {
            if (m_target == null) return;

            transform.position = Vector3.SmoothDamp(
                transform.position, DesiredPos(), ref m_velocity,
                1f / positionSmooth, Mathf.Infinity, Time.deltaTime);

            transform.rotation = DesiredRot();
        }

        // Camera sits directly behind (-Z) and above the player.
        private Vector3 DesiredPos() =>
            m_target.position + new Vector3(0f, height, -distance);

        // Look toward the player with Y rotation locked to 0 (no side rotation).
        private Quaternion DesiredRot()
        {
            var toPlayer = m_target.position - transform.position;
            if (toPlayer.sqrMagnitude < 0.001f) return Quaternion.identity;
            return Quaternion.LookRotation(toPlayer, Vector3.up);
        }
    }
}
