using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Locks the local player's movement until the race countdown reaches Go (0).
    ///
    /// Three-layer lock:
    ///   1. Disables PlayerInputManager so no new input is read (also disables the
    ///      underlying InputActionAsset via its OnDisable).
    ///   2. Zeros lateralVelocity every frame so residual PLAYER TWO physics
    ///      can't slide the player even if the input component is re-enabled
    ///      by another system (e.g. NetworkedMovementSync at +50).
    ///   3. Disables the GraphicRaycaster on the joystick canvas so the on-screen
    ///      joystick stays visible but is not interactive during the countdown.
    ///
    /// Also guards against the player spawning after the countdown has already
    /// fired (e.g. late join) — checks CountdownValue on spawn and skips the
    /// lock if the race has already started.
    ///
    /// Runs at execution order +100 so it fires AFTER NetworkedMovementSync (+50).
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(Player))]
    [AddComponentMenu("Adventure Multiplayer/Network Player Input Locker")]
    public class NetworkPlayerInputLocker : NetworkBehaviour
    {
        private PlayerInputManager m_input;
        private Player             m_player;
        private GraphicRaycaster   m_joystickRaycaster;
        private bool               m_locked;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            m_input  = GetComponent<PlayerInputManager>();
            m_player = GetComponent<Player>();

            var mobileRig = FindFirstObjectByType<MobileRig>();
            if (mobileRig != null)
                m_joystickRaycaster = mobileRig.GetComponentInParent<GraphicRaycaster>();

            // If the countdown has already finished (late-spawning player), don't lock.
            var countdown = FindFirstObjectByType<RaceCountdown>();
            if (countdown != null && countdown.CountdownValue.Value == 0)
            {
                Debug.Log("[NetworkPlayerInputLocker] Race already started — skipping lock.");
                return;
            }

            ApplyLock();
            RaceCountdown.OnRaceStart += Unlock;
        }

        public override void OnNetworkDespawn()
        {
            RaceCountdown.OnRaceStart -= Unlock;
        }

        private void Update()
        {
            // Belt-and-suspenders: keep lateral velocity zeroed while locked so the
            // player can't slide even if PlayerInputManager gets re-enabled externally.
            if (!m_locked || !IsOwner || m_player == null) return;
            m_player.lateralVelocity = Vector3.zero;
        }

        private void ApplyLock()
        {
            m_locked = true;
            if (m_input != null) m_input.enabled = false;
            if (m_joystickRaycaster != null) m_joystickRaycaster.enabled = false;
            Debug.Log("[NetworkPlayerInputLocker] Input locked — waiting for countdown.");
        }

        private void Unlock()
        {
            RaceCountdown.OnRaceStart -= Unlock;
            m_locked = false;
            if (m_input != null) m_input.enabled = true;
            if (m_joystickRaycaster != null) m_joystickRaycaster.enabled = true;
            Debug.Log("[NetworkPlayerInputLocker] Input unlocked — GO!");
        }
    }
}
