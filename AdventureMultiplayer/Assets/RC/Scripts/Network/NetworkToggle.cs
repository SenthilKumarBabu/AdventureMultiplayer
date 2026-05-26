using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative sync for Toggle (used by Spike Panels, Button Panels, etc).
    ///
    /// Problem: Toggle.autoToggle runs independently on each machine using local Time.time,
    /// so spike panels desync between host and client.
    ///
    /// Fix: Server runs the Toggle simulation. _state NetworkVariable replicates to clients.
    /// Clients disable the plugin's Update loop and fire onActivate/onDeactivate locally
    /// when the server state changes.
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("Adventure Multiplayer/Network Toggle")]
    public class NetworkToggle : NetworkBehaviour
    {
        private Toggle _toggle;

        private readonly NetworkVariable<bool> _state = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Server runs normally — mirror state changes to the NetworkVariable.
                // We poll in Update since Toggle doesn't expose a callback.
                _state.Value = _toggle.state;
            }
            else
            {
                // Clients disable the plugin's autoToggle loop and follow server state.
                _toggle.autoToggle = false;
                _state.OnValueChanged += OnStateChanged;

                // Apply initial state.
                ApplyState(_state.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateChanged;
        }

        private void Update()
        {
            if (!IsServer) return;

            // Mirror Toggle.state to the NetworkVariable whenever it changes.
            if (_toggle.state != _state.Value)
                _state.Value = _toggle.state;
        }

        private void OnStateChanged(bool _, bool newState)
        {
            ApplyState(newState);
        }

        private void ApplyState(bool newState)
        {
            // Directly set the state and fire the appropriate events without
            // going through Toggle.Set() which uses a coroutine with delay.
            _toggle.state = newState;
            if (newState)
                _toggle.onActivate?.Invoke();
            else
                _toggle.onDeactivate?.Invoke();
        }
    }
}
