using System.Collections;
using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drop-in replacement for Pickable on any NetworkObject crate.
    /// Skips transform.parent assignments — NGO handles parenting via TrySetParent
    /// in NetworkedPickable (server-authoritative).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Pickable")]
    public class NetworkPickable : Pickable
    {
        public Vector3 LastThrowDirection { get; private set; }
        public float   LastThrowForce     { get; private set; }

        private static readonly WaitForSeconds k_colliderRestoreDelay = new(0.3f);

        public override void PickUp(PlayerObjectGrabber grabber)
        {
            if (beingHold) return;

            m_currentGrabber  = grabber;
            beingHold         = true;

            // Parent is set by the server via TrySetParent — skip it here.
            m_rigidBody.isKinematic   = true;
            m_collider.isTrigger      = true;
            m_interpolation           = m_rigidBody.interpolation;
            m_rigidBody.interpolation = RigidbodyInterpolation.None;
            onPicked?.Invoke();
        }

        public override void Release(Vector3 direction, float force)
        {
            if (!beingHold) return;

            LastThrowDirection = direction;
            LastThrowForce     = force;

            m_currentGrabber          = null;
            beingHold                 = false;
            m_rigidBody.isKinematic   = false;
            m_rigidBody.interpolation = m_interpolation;
            // Keep isTrigger=true until the server has unparented the crate.
            // The crate is still a child of the player at this point — making it
            // solid now would overlap the player capsule and cause pushback.
            m_collider.isTrigger = true;
#if UNITY_6000_0_OR_NEWER
            m_rigidBody.linearVelocity = direction * force;
#else
            m_rigidBody.velocity = direction * force;
#endif
            onReleased?.Invoke();
            StartCoroutine(RestoreColliderDelayed());
        }

        private IEnumerator RestoreColliderDelayed()
        {
            // Wait for TrySetParent(null) to propagate — one round trip is ~1-2 frames.
            yield return k_colliderRestoreDelay;
            if (m_collider != null)
                m_collider.isTrigger = false;
        }

        public override void Respawn()
        {
            m_rigidBody.isKinematic = m_collider.isTrigger = beingHold = false;
#if UNITY_6000_0_OR_NEWER
            m_rigidBody.linearVelocity = Vector3.zero;
#else
            m_rigidBody.velocity = Vector3.zero;
#endif
            transform.SetLocalPositionAndRotation(m_initialPosition, m_initialRotation);
            onRespawn?.Invoke();
        }
    }
}
