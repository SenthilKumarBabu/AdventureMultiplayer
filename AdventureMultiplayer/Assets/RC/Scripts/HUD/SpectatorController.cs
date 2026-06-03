using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Activated when the local player finishes the race while others are still running.
    /// Switches the camera to follow another player and lets the local player cycle
    /// through spectate targets with Prev / Next buttons.
    ///
    /// Setup (Inspector):
    ///   spectatorPanel  → root panel containing label + buttons (starts inactive)
    ///   spectatingLabel → "Spectating: Player X"
    ///   prevButton      → cycle to previous player
    ///   nextButton      → cycle to next player
    ///   racingHUD       → the in-race HUD root (hidden when spectating or results show)
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Spectator Controller")]
    public class SpectatorController : MonoBehaviour
    {
        [Header("Spectator UI")]
        [SerializeField] private GameObject      spectatorPanel;
        [SerializeField] private TextMeshProUGUI spectatingLabel;
        [SerializeField] private Button          prevButton;
        [SerializeField] private Button          nextButton;

        [Header("References")]
        [SerializeField] private GameObject racingHUD;

        public static SpectatorController Instance { get; private set; }

        private System.Collections.Generic.List<Transform> m_targets = new();
        private int m_index;

        private void Awake()
        {
            Instance = this;
            spectatorPanel?.SetActive(false);
            prevButton?.onClick.AddListener(() => Cycle(-1));
            nextButton?.onClick.AddListener(() => Cycle(1));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void EnterSpectatorMode()
        {
            DisableLocalInput();
            if (racingHUD != null) racingHUD.SetActive(false);

            RefreshTargets();

            m_index = 0;
            if (m_targets.Count > 0)
                SetCamera(m_targets[0]);

            UpdateLabel();
            spectatorPanel?.SetActive(true);
            Debug.Log("[SpectatorController] Entered spectator mode.");
        }

        public void ExitSpectatorMode()
        {
            spectatorPanel?.SetActive(false);
            Debug.Log("[SpectatorController] Exited spectator mode.");
        }

        // Called by FinishScreenHUD so DNF players (who never enter spectator) also get HUD hidden.
        public void HideRacingHUD()
        {
            if (racingHUD != null) racingHUD.SetActive(false);
        }

        public void DisableLocalInput()
        {
            if (NetworkManager.Singleton == null) return;
            var localObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var input    = localObj?.GetComponent<PlayerInputManager>();
            if (input != null)
            {
                input.enabled = false;
                Debug.Log("[SpectatorController] Local player input disabled.");
            }
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void RefreshTargets()
        {
            m_targets.Clear();
            if (NetworkManager.Singleton == null) return;

            ulong localId = NetworkManager.Singleton.LocalClientId;
            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            {
                if (kv.Key == localId) continue;
                var playerObj = kv.Value.PlayerObject;
                if (playerObj != null)
                    m_targets.Add(playerObj.transform);
            }
        }

        private void Cycle(int dir)
        {
            if (m_targets.Count == 0) return;
            m_index = (m_index + dir + m_targets.Count) % m_targets.Count;
            SetCamera(m_targets[m_index]);
            UpdateLabel();
        }

        private void SetCamera(Transform target)
        {
            var cam = FindFirstObjectByType<NetworkCameraFollow>();
            cam?.SetTarget(target);
        }

        private void UpdateLabel()
        {
            if (spectatingLabel == null) return;
            if (m_targets.Count == 0)
            {
                spectatingLabel.text = "Waiting for others...";
                return;
            }
            var netObj = m_targets[m_index].GetComponent<NetworkObject>();
            string name = netObj != null ? $"Player {netObj.OwnerClientId}" : "Player";
            spectatingLabel.text = $"Spectating: {name}  ({m_index + 1}/{m_targets.Count})";
        }
    }
}
