using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative sync for FallingPlatform.
    ///
    /// The plugin runs the full simulation (shake, fall, reset) only on the server.
    /// Position and state are replicated to clients each tick via NetworkVariables.
    /// Clients disable the plugin's own Update-driven logic and just follow the
    /// server-replicated transform.
    /// </summary>
    [RequireComponent(typeof(FallingPlatform))]
    [AddComponentMenu("Adventure Multiplayer/Network Falling Platform")]
    public class NetworkFallingPlatform : NetworkBehaviour
    {
        private FallingPlatform _platform;

        private readonly NetworkVariable<Vector3> _position = new(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _activated = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _falling = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            _platform = GetComponent<FallingPlatform>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Server runs the plugin simulation normally.
                _position.Value = transform.position;
            }
            else
            {
                // Clients disable the plugin — they follow server position instead.
                _platform.enabled = false;

                _position.OnValueChanged   += OnPositionChanged;
                _activated.OnValueChanged  += OnActivatedChanged;
                _falling.OnValueChanged    += OnFallingChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            _position.OnValueChanged   -= OnPositionChanged;
            _activated.OnValueChanged  -= OnActivatedChanged;
            _falling.OnValueChanged    -= OnFallingChanged;
        }

        // ── Server: push state each tick ─────────────────────────────────────

        private void Update()
        {
            if (!IsServer) return;

            _position.Value  = transform.position;
            _activated.Value = _platform.activated;
            _falling.Value   = _platform.falling;
        }

        // ── Client: receive state ─────────────────────────────────────────────

        private void OnPositionChanged(Vector3 _, Vector3 newPos)
        {
            transform.position = newPos;
        }

        private void OnActivatedChanged(bool _, bool activated)
        {
            // Visual shake is driven by position sync — nothing extra needed.
        }

        private void OnFallingChanged(bool _, bool falling)
        {
            if (falling)
            {
                // Match the plugin: disable collider so players fall through.
                var col = GetComponent<Collider>();
                if (col != null) col.isTrigger = true;
            }
            else
            {
                // Platform reset.
                var col = GetComponent<Collider>();
                if (col != null) col.isTrigger = false;
            }
        }
    }
}
