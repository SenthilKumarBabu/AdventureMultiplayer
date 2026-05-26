using System.Reflection;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Server-authoritative wrapper for the plugin's Panel component (used by Log With Coin).
    ///
    /// The Panel activates when stomped. Its Inspector OnActivate event calls SetActive(true)
    /// on the coin(s). In multiplayer this fires locally on whichever client detects the stomp,
    /// so we intercept it and route through a ServerRpc.
    ///
    /// Coin direction and collection sync use the same approach as NetworkBreakable:
    /// reflection on CollectiblePhysics.m_velocity + index-based collection tracking.
    ///
    /// Assign the coin Collectible(s) to the 'collectibles' field in the Inspector.
    /// The coins must NOT have a NetworkObject component.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Panel")]
    public class NetworkPanel : NetworkBehaviour
    {
        [SerializeField] private Collectible[] collectibles;

        private Panel _panel;
        private bool[] _coinCollected;
        private bool _localActivateApplied;

        private static readonly FieldInfo VelocityField =
            typeof(CollectiblePhysics).GetField("m_velocity",
                BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly NetworkVariable<bool> _isActivated = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private void Awake()
        {
            // Panel may be on a child GameObject (e.g. "Log" under "Log With Coin" root).
            _panel = GetComponentInChildren<Panel>(true);

            if (_panel == null)
                Debug.LogError($"[NetworkPanel] '{name}' could not find a Panel component in children.");
        }

        public override void OnNetworkSpawn()
        {
            _coinCollected = new bool[collectibles.Length];

            if (IsServer)
            {
                _panel.OnActivate.AddListener(OnActivateServer);
            }
            else
            {
                _panel.OnActivate.AddListener(OnActivateClient);
                _isActivated.OnValueChanged += OnIsActivatedChanged;

                if (_isActivated.Value)
                    ApplyLocalActivate(null); // Late joiner
            }
        }

        public override void OnNetworkDespawn()
        {
            _panel.OnActivate.RemoveListener(OnActivateServer);
            _panel.OnActivate.RemoveListener(OnActivateClient);
            _isActivated.OnValueChanged -= OnIsActivatedChanged;
        }

        // ── Server ────────────────────────────────────────────────────────────

        private void OnActivateServer()
        {
            if (_isActivated.Value) return;
            _isActivated.Value = true;

            var velocities = ReadCoinVelocities();
            SubscribeToCoinCollect();

            SyncActivateClientRpc(velocities);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RequestActivateServerRpc()
        {
            if (_panel.activated) return;
            _panel.Activate();
        }

        [ClientRpc]
        private void SyncActivateClientRpc(Vector3[] coinVelocities)
        {
            if (IsServer) return;
            ApplyLocalActivate(coinVelocities);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SyncCoinCollectedServerRpc(int coinIdx)
        {
            Debug.Log($"[NetworkPanel] '{name}' SyncCoinCollectedServerRpc coin[{coinIdx}]. already={_coinCollected[coinIdx]}");
            if (_coinCollected[coinIdx]) return;
            // Do NOT set _coinCollected here — let ClientRpc set it so the host processes it too.
            SyncCoinCollectedClientRpc(coinIdx);
        }

        [ClientRpc]
        private void SyncCoinCollectedClientRpc(int coinIdx)
        {
            Debug.Log($"[NetworkPanel] '{name}' SyncCoinCollectedClientRpc coin[{coinIdx}]. already={_coinCollected[coinIdx]} IsServer={IsServer}");
            if (_coinCollected[coinIdx]) return;
            _coinCollected[coinIdx] = true;

            if (coinIdx >= collectibles.Length) return;
            var c = collectibles[coinIdx];
            if (c != null && c.gameObject.activeSelf)
            {
                c.gameObject.SetActive(false);
                Debug.Log($"[NetworkPanel] '{name}' coin[{coinIdx}] removed on this machine.");
            }
        }

        // ── Client ────────────────────────────────────────────────────────────

        private void OnActivateClient()
        {
            // Panel's Inspector OnActivate already called SetActive on the coin locally.
            // Record local activation and ask the server to confirm.
            _localActivateApplied = true;
            SubscribeToCoinCollect();
            RequestActivateServerRpc();
        }

        private void OnIsActivatedChanged(bool _, bool activated)
        {
            if (activated) ApplyLocalActivate(null);
        }

        private void ApplyLocalActivate(Vector3[] velocities)
        {
            if (_localActivateApplied)
            {
                // Already active locally — only override velocities.
                if (velocities != null)
                {
                    for (int i = 0; i < collectibles.Length; i++)
                    {
                        if (i < velocities.Length && collectibles[i] != null)
                            SetCoinVelocity(collectibles[i], velocities[i]);
                    }
                }
                return;
            }

            _localActivateApplied = true;

            for (int i = 0; i < collectibles.Length; i++)
            {
                var c = collectibles[i];
                if (c == null) continue;
                c.gameObject.SetActive(true);
                if (velocities != null && i < velocities.Length)
                    SetCoinVelocity(c, velocities[i]);
            }

            SubscribeToCoinCollect();
        }

        // ── Collection sync ───────────────────────────────────────────────────

        private void SubscribeToCoinCollect()
        {
            for (int i = 0; i < collectibles.Length; i++)
            {
                int idx = i;
                var c = collectibles[i];
                if (c == null) continue;
                c.onCollect.AddListener(_ => OnCoinCollectedLocally(idx));
            }
        }

        private void OnCoinCollectedLocally(int idx)
        {
            if (_coinCollected[idx]) return;
            _coinCollected[idx] = true;

            Debug.Log($"[NetworkPanel] '{name}' coin[{idx}] collected locally. IsServer={IsServer}");

            if (IsServer)
                SyncCoinCollectedClientRpc(idx);
            else
                SyncCoinCollectedServerRpc(idx);
        }

        // ── Velocity helpers (reflection) ─────────────────────────────────────

        private Vector3[] ReadCoinVelocities()
        {
            var velocities = new Vector3[collectibles.Length];
            for (int i = 0; i < collectibles.Length; i++)
            {
                var c = collectibles[i];
                if (c == null || VelocityField == null) continue;
                var physics = c.GetComponent<CollectiblePhysics>();
                if (physics != null)
                    velocities[i] = (Vector3)VelocityField.GetValue(physics);
            }
            return velocities;
        }

        private void SetCoinVelocity(Collectible c, Vector3 velocity)
        {
            if (VelocityField == null) return;
            var physics = c.GetComponent<CollectiblePhysics>();
            if (physics != null)
                VelocityField.SetValue(physics, velocity);
        }
    }
}
