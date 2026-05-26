using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Drives the Lobby scene via Unity Relay (internet play).
    ///
    /// Host  → authenticates → creates Relay allocation → displays join code → StartHost
    /// Client → enters join code → authenticates → joins Relay → StartClient
    ///
    /// Requires: Project linked in Edit > Project Settings > Services (Unity Project ID).
    /// Wire all fields in the Inspector.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Lobby Manager")]
    public class LobbyManager : MonoBehaviour
    {
        private const int MinPlayersToStart = 1; // raise to 2 for production
        private const int MaxConnections     = 3; // host + 3 clients = 4 total

        [Header("Connection")]
        [FormerlySerializedAs("ipInputField")]
        [SerializeField] private TMP_InputField  joinCodeInputField; // client types host's code here
        [SerializeField] private Button          hostButton;
        [SerializeField] private Button          joinButton;

        [Header("Lobby")]
        [SerializeField] private Button          startButton;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI joinCodeText; // optional — shows host's code

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "DeathRunL1";

        private void Awake()
        {
            hostButton.onClick.AddListener(() => HostAsync().Forget());
            joinButton.onClick.AddListener(() => JoinAsync().Forget());
            startButton.onClick.AddListener(OnStartClicked);

            startButton.gameObject.SetActive(false);
            if (joinCodeText != null) joinCodeText.gameObject.SetActive(false);
            SetStatus("Choose Host or Join");
            SetPlayerCount(0);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // ── Host ──────────────────────────────────────────────────────────────

        private async UniTaskVoid HostAsync()
        {
            SetButtonsInteractable(false);
            SetStatus("Signing in…");

            try
            {
                await InitServicesAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] Auth failed: {e.Message}");
                SetStatus("Sign-in failed — check console.");
                SetButtonsInteractable(true);
                return;
            }

            SetStatus("Creating relay…");
            Allocation allocation;
            try
            {
                allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] CreateAllocation failed: {e.Message}");
                SetStatus("Relay error — check console.");
                SetButtonsInteractable(true);
                return;
            }

            string joinCode;
            try
            {
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] GetJoinCode failed: {e.Message}");
                SetStatus("Relay error — check console.");
                SetButtonsInteractable(true);
                return;
            }

            Debug.Log($"[LobbyManager] Relay join code: {joinCode}");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

            NetworkManager.Singleton.OnClientConnectedCallback  += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.StartHost();
            CharacterPicker.Instance?.RegisterHostHandler();
            CharacterPicker.Instance?.SelectCharacter(
                CharacterPicker.Instance.LocalSelectedIndex);

            startButton.gameObject.SetActive(true);
            startButton.interactable = false;

            if (joinCodeText != null)
            {
                joinCodeText.gameObject.SetActive(true);
                joinCodeText.text = $"Join Code: {joinCode}";
            }

            SetStatus($"Hosting  |  code: {joinCode}");
            RefreshPlayerCount();
        }

        // ── Join ──────────────────────────────────────────────────────────────

        private async UniTaskVoid JoinAsync()
        {
            var code = joinCodeInputField != null ? joinCodeInputField.text.Trim().ToUpper() : "";
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Enter the host's join code first.");
                return;
            }

            SetButtonsInteractable(false);
            SetStatus("Signing in…");

            try
            {
                await InitServicesAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] Auth failed: {e.Message}");
                SetStatus("Sign-in failed — check console.");
                SetButtonsInteractable(true);
                return;
            }

            SetStatus($"Joining relay '{code}'…");
            JoinAllocation joinAllocation;
            try
            {
                joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] JoinAllocation failed: {e.Message}");
                SetStatus("Join failed — invalid code?");
                SetButtonsInteractable(true);
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.StartClient();

            SetStatus("Connecting…");
        }

        // ── Relay callbacks ───────────────────────────────────────────────────

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
                RefreshPlayerCount();
            else
                SetStatus("Connected — waiting for host to start…");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
                RefreshPlayerCount();
            else
                SetStatus("Disconnected.");
        }

        private void OnStartClicked()
        {
            if (!NetworkManager.Singleton.IsHost) return;
            Debug.Log($"[LobbyManager] Host loading '{gameplaySceneName}'");
            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }

        private void RefreshPlayerCount()
        {
            var count = NetworkManager.Singleton.ConnectedClients.Count;
            SetPlayerCount(count);
            startButton.interactable = count >= MinPlayersToStart;
        }

        // ── Services ──────────────────────────────────────────────────────────

        private static async UniTask InitServicesAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"[LobbyManager] Signed in as {AuthenticationService.Instance.PlayerId}");
        }

        // ── UI helpers ────────────────────────────────────────────────────────

        private void SetPlayerCount(int count) =>
            playerCountText.text = $"Players: {count}";

        private void SetStatus(string msg) =>
            statusText.text = msg;

        private void SetButtonsInteractable(bool value)
        {
            hostButton.interactable = value;
            joinButton.interactable = value;
        }
    }
}
