using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drop this onto any platform that uses an external script for movement/rotation
    /// (e.g. OscillateRotation, OscillatePosition). It registers the object as a Platform
    /// so the Entity attachment system attaches standing players, then propagates the
    /// platform's position/rotation delta to them each frame via LateUpdate.
    ///
    /// Order: external script moves platform in Update → LateUpdate picks up the delta.
    /// </summary>
    [UnityEngine.AddComponentMenu("Adventure Multiplayer/Dynamic Platform")]
    public class DynamicPlatform : Platform
    {
        // Called by Platform.Awake — sets GameTags.Platform tag automatically.

        public override void PlatformUpdate()
        {
            // Intentionally empty: movement is driven by the external script.
            // LateUpdate handles caching and propagation.
        }

        /// <summary>
        /// Overrides the base to move attached transforms with the platform
        /// but NOT apply the platform's rotation delta to the player's orientation.
        /// This prevents the player from tilting/slanting when standing on a spinning disc.
        /// </summary>
        protected override void HandleAttachedTransforms()
        {
            foreach (var attach in m_attachedTransforms)
            {
                var attachOffset   = attach.position - transform.position;
                var positionOffset = transform.position - m_lastPosition;
                var rotationOffset = transform.rotation * UnityEngine.Quaternion.Inverse(m_lastRotation);
                var finalOffset    = attachOffset + positionOffset;
                attach.position    = transform.position + rotationOffset * finalOffset;
                // Rotation intentionally not applied — player manages its own orientation.
            }
        }

        private void LateUpdate()
        {
            HandleAttachedTransforms(); // apply position delta to standing players
            CacheTransform();           // snapshot for next frame's delta calculation
        }
    }
}
