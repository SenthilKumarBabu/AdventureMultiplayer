using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Player/Safe Player Camera")]
    public class SafePlayerCamera : PlayerCamera
    {
        /// <summary>
        /// Defers base initialization until Level.instance.player is available.
        /// In a multiplayer game the player spawns after scene Start(), so we
        /// subscribe to onPlayerChanged and initialize then instead.
        /// </summary>
        protected override void Start()
        {
            if (player != null)
            {
                base.Start();
                return;
            }

            if (Level.instance != null)
            {
                Debug.Log("[SafePlayerCamera] Player not ready at Start — deferring init.");
                Level.instance.onPlayerChanged.AddListener(OnPlayerReady);
            }
            else
            {
                Debug.LogWarning("[SafePlayerCamera] Level.instance is null at Start — camera will not initialize.");
            }
        }

        private void OnPlayerReady(Player p)
        {
            Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);
            Debug.Log("[SafePlayerCamera] Player ready — initializing camera.");
            base.Start();

            // Re-assign LookAt if the player reference changes again (e.g. late spawn).
            Level.instance.onPlayerChanged.AddListener(OnPlayerChanged);
        }

        private void OnPlayerChanged(Player p)
        {
            if (m_virtualCamera != null && p != null)
            {
                Debug.Log($"[SafePlayerCamera] Player changed — reassigning LookAt to {p.name}.");
                m_virtualCamera.LookAt = p.transform;
            }
        }

        /// <summary>
        /// Re-assigns LookAt every time the camera is activated so it always tracks
        /// the correct local player, not a ghost from a previous assignment.
        /// </summary>
        public override void Reset()
        {
            base.Reset();

            if (m_virtualCamera != null && player != null)
                m_virtualCamera.LookAt = player.transform;
        }

        protected override void MoveTarget()
        {
            var rotationSpeed = Mathf.Lerp(
                minRotationSpeed,
                maxRotationSpeed,
                player.targetTopSpeed > 0f
                    ? player.velocity.magnitude / player.targetTopSpeed
                    : 0f
            );
            var upRotationMaxDelta = rotationSpeed * Time.deltaTime;
            var canRotateUpward =
                player.gravityField
                || (player.rotateToGround && !player.IsSideScroller && player.groundAngle > 45f);

            var targetUp = canRotateUpward ? player.transform.up : Vector3.up;

            // Guard: only call FromToRotation when both vectors are non-degenerate.
            // A degenerate m_currentUpRotation (NaN or zero-length) triggers Unity's
            // "CompareApproximately(det, 1.0F)" assertion spam.
            var fromVec = m_currentUpRotation * Vector3.up;
            Quaternion upRotation;
            if (fromVec.sqrMagnitude > 0.001f && targetUp.sqrMagnitude > 0.001f)
            {
                upRotation = Quaternion.FromToRotation(fromVec, targetUp) * m_currentUpRotation;
            }
            else
            {
                // Reset to a safe quaternion so subsequent frames can recover
                upRotation = Quaternion.identity;
            }

            var smoothUpRotation = Quaternion.RotateTowards(
                m_currentUpRotation,
                upRotation,
                upRotationMaxDelta
            );

            m_currentUpRotation = smoothUpRotation;

            var yawRotation = Quaternion.Euler(0f, m_cameraTargetYaw, 0f);
            var pitchRotation = Quaternion.Euler(m_cameraTargetPitch, 0f, 0f);

            var baseRotation = yawRotation * pitchRotation;
            var finalRotation = m_currentUpRotation * baseRotation;

            m_target.SetPositionAndRotation(m_cameraTargetPosition, finalRotation);
            m_cameraBody.CameraDistance = m_cameraDistance;

            if (player.IsSideScroller)
            {
                var forward = -Vector3.Cross(targetUp, player.pathForward).normalized;

                if (forward.sqrMagnitude < 0.001f)
                    forward = m_target.forward;

                var newRotation = Quaternion.LookRotation(forward, targetUp);
                m_target.rotation = newRotation;
            }
        }
    }
}
