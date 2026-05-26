using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative sync for Panel (Button Panel, Spike Panel trigger, etc).
    ///
    /// Syncs Panel.activated state across all clients and fires the Panel's
    /// OnActivate / OnDeactivate Inspector events on every machine.
    ///
    /// Use this on Panels that control movers, toggles, or other scene objects.
    /// Do NOT use this on Log With Coin — that uses NetworkPanel (coin spawning).
    /// </summary>
    [RequireComponent(typeof(Panel))]
    [AddComponentMenu("Adventure Multiplayer/Network Panel Sync")]
    public class NetworkPanelSync : NetworkBehaviour
    {
        private Panel _panel;

        private readonly NetworkVariable<bool> _activated = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            _panel = GetComponent<Panel>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _panel.OnActivate.AddListener(OnActivateServer);
                _panel.OnDeactivate.AddListener(OnDeactivateServer);
            }
            else
            {
                // Clients disable the plugin's own contact detection — server drives state.
                _panel.enabled = false;
                _activated.OnValueChanged += OnActivatedChanged;

                // Apply initial state for late joiners.
                if (_activated.Value)
                    _panel.OnActivate?.Invoke();
            }
        }

        public override void OnNetworkDespawn()
        {
            _panel.OnActivate.RemoveListener(OnActivateServer);
            _panel.OnDeactivate.RemoveListener(OnDeactivateServer);
            _activated.OnValueChanged -= OnActivatedChanged;
        }

        private void OnActivateServer()
        {
            _activated.Value = true;
            Debug.Log($"[NetworkPanelSync] '{name}' activated.");
        }

        private void OnDeactivateServer()
        {
            _activated.Value = false;
            Debug.Log($"[NetworkPanelSync] '{name}' deactivated.");
        }

        private void OnActivatedChanged(bool previous, bool current)
        {
            if (current)
                _panel.OnActivate?.Invoke();
            else
                _panel.OnDeactivate?.Invoke();

            Debug.Log($"[NetworkPanelSync] '{name}' state changed to {current} on client.");
        }
    }
}
