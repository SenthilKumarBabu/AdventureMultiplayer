using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;
using DG.Tweening;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative wrapper for GridPlatform.
    ///
    /// Problem: GridPlatform subscribes to Level.instance.player.OnJump, which only fires
    /// for the local player. In multiplayer each machine rotates the platform independently.
    ///
    /// Fix: intercept the local jump, send a ServerRpc, and let the server broadcast the
    /// new rotation state to all clients via a NetworkVariable.
    /// </summary>
    [RequireComponent(typeof(GridPlatform))]
    [AddComponentMenu("Adventure Multiplayer/Network Grid Platform")]
    public class NetworkGridPlatform : NetworkBehaviour
    {
        private GridPlatform _gridPlatform;

        // Tracks how many times the platform has been flipped (even=0°, odd=180°).
        private readonly NetworkVariable<int> _flipCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool _isAnimating;

        private void Awake()
        {
            _gridPlatform = GetComponent<GridPlatform>();
        }

        public override void OnNetworkSpawn()
        {
            // Disable the plugin's own jump listener — we handle jumps ourselves.
            _gridPlatform.enabled = false;

            _flipCount.OnValueChanged += OnFlipCountChanged;

            // Subscribe to any player's jump on every machine.
            // We route through ServerRpc so only the server increments _flipCount.
            Level.instance.onPlayerChanged.AddListener(SubscribeToJump);
            if (Level.instance.player != null)
                SubscribeToJump(Level.instance.player);
        }

        public override void OnNetworkDespawn()
        {
            _flipCount.OnValueChanged -= OnFlipCountChanged;
        }

        private void SubscribeToJump(Player player)
        {
            if (player == null) return;
            player.playerEvents.OnJump.AddListener(OnLocalPlayerJumped);
        }

        private void OnLocalPlayerJumped()
        {
            RequestFlipServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestFlipServerRpc()
        {
            _flipCount.Value++;
            Debug.Log($"[NetworkGridPlatform] '{name}' flipped. count={_flipCount.Value}");
        }

        private void OnFlipCountChanged(int previous, int current)
        {
            RotatePlatform(current);
        }

        private void RotatePlatform(int flipCount)
        {
            if (_gridPlatform.platform == null) return;
            if (_isAnimating) _gridPlatform.platform.DOKill();

            var targetZ = (flipCount % 2 == 0) ? 0f : 180f;
            _isAnimating = true;
            _gridPlatform.platform
                .DOLocalRotate(new Vector3(0, 0, targetZ), _gridPlatform.rotationDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() => _isAnimating = false);
        }
    }
}
