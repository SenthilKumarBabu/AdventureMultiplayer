using Unity.Netcode;
using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Locks the local player's input until the race countdown reaches Go (0).
    ///
    /// Runs at execution order +100 so it fires AFTER NetworkedMovementSync (+50),
    /// which re-enables PlayerInputManager on the owner path. This guarantees the
    /// lock takes effect even if MovementSync enabled inputs first.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Network Player Input Locker")]
    public class NetworkPlayerInputLocker : NetworkBehaviour
    {
        private PlayerInputManager m_input;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            m_input = GetComponent<PlayerInputManager>();

            if (m_input == null)
            {
                Debug.LogWarning("[NetworkPlayerInputLocker] No PlayerInputManager found on player.");
                return;
            }

            // Lock input at spawn — countdown hasn't fired yet.
            m_input.enabled = false;
            Debug.Log("[NetworkPlayerInputLocker] Input locked — waiting for countdown.");

            RaceCountdown.OnRaceStart += UnlockInput;
        }

        public override void OnNetworkDespawn()
        {
            RaceCountdown.OnRaceStart -= UnlockInput;
        }

        private void UnlockInput()
        {
            RaceCountdown.OnRaceStart -= UnlockInput;

            if (m_input != null)
                m_input.enabled = true;

            Debug.Log("[NetworkPlayerInputLocker] Input unlocked — GO!");
        }
    }
}
