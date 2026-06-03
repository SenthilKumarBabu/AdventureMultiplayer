using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Collections;
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
using System.Collections.Generic;

namespace AdventureMultiplayer
{
    [AddComponentMenu("Adventure Multiplayer/Lobby Manager")]
    public class LobbyManager : MonoBehaviour
    {
        private const int MinPlayersToStart = 1;
        private const int MaxConnections     = 3;

        private const string k_ReadyMsg      = "LobbyReady";
        private const string k_ReadyCountMsg = "LobbyReadyCount";

        private static readonly string[] k_levelNames = { "Level 1", "Level 2", "Level 3", "Obstacle L1" };
        private static readonly string[] k_charNames  = { "Gale", "Blaze", "Bolt", "Bruno", "Spike" };

        private bool   m_gameLocked;
        private string m_selectedLevelDisplay = "Level 1";

        [Header("Connection")]
        [FormerlySerializedAs("ipInputField")]
        [SerializeField] private TMP_InputField  joinCodeInputField;
        [SerializeField] private Button          hostButton;
        [SerializeField] private Button          joinButton;

        [Header("Lobby")]
        [SerializeField] private Button          startButton;
        [SerializeField] private Button          readyButton;
        [SerializeField] private TextMeshProUGUI readyStatusText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI joinCodeText;

        [Header("Tabs")]
        [SerializeField] private Button          levelTabButton;
        [SerializeField] private Button          characterTabButton;
        [SerializeField] private GameObject      levelTabContent;      // LevelSelectPanel
        [SerializeField] private GameObject      characterTabContent;  // CharacterTabContent
        [SerializeField] private TextMeshProUGUI selectionSummaryText;

        [Header("Level Buttons")]
        [SerializeField] private Button level1Button;
        [SerializeField] private Button level2Button;
        [SerializeField] private Button level3Button;
        [SerializeField] private Button level4Button;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "DeathRunL1";

        private readonly HashSet<ulong> m_readySet = new();
        private CharacterSelectUI m_charSelectUI;

        private void Awake()
        {
            hostButton.onClick.AddListener(() => HostAsync().Forget());
            joinButton.onClick.AddListener(() => JoinAsync().Forget());
            startButton.onClick.AddListener(OnStartClicked);
            if (readyButton != null)
                readyButton.onClick.AddListener(OnReadyClicked);

            if (levelTabButton     != null) levelTabButton.onClick.AddListener(() => ShowTab(true));
            if (characterTabButton != null) characterTabButton.onClick.AddListener(() => ShowTab(false));

            if (level1Button != null) level1Button.onClick.AddListener(() => SelectLevel("DeathRunL1",  0));
            if (level2Button != null) level2Button.onClick.AddListener(() => SelectLevel("DeathRunL2",  1));
            if (level3Button != null) level3Button.onClick.AddListener(() => SelectLevel("DeathRunL3",  2));
            if (level4Button != null) level4Button.onClick.AddListener(() => SelectLevel("ObstacleL1",  3));

            // Hide everything that appears only after connecting
            startButton.gameObject.SetActive(false);
            if (readyButton          != null) readyButton.gameObject.SetActive(false);
            if (readyStatusText      != null) readyStatusText.gameObject.SetActive(false);
            if (joinCodeText         != null) joinCodeText.gameObject.SetActive(false);
            if (selectionSummaryText != null) selectionSummaryText.gameObject.SetActive(false);
            if (levelTabButton       != null) levelTabButton.gameObject.SetActive(false);
            if (characterTabButton   != null) characterTabButton.gameObject.SetActive(false);
            if (levelTabContent      != null) levelTabContent.SetActive(false);
            if (characterTabContent  != null) characterTabContent.SetActive(false);

            SetStatus("Choose Host or Join");
            SetPlayerCount(0);
        }

        private void OnDestroy()
        {
            if (m_charSelectUI != null)
                m_charSelectUI.CharacterChanged -= OnCharacterChanged;
            if (NetworkManager.Singleton == null) return;
            NetworkManager.Singleton.OnClientConnectedCallback  -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        // ── Host ──────────────────────────────────────────────────────────────

        private async UniTaskVoid HostAsync()
        {
            SetButtonsInteractable(false);
            SetStatus("Signing in…");

            try { await InitServicesAsync(); }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] Auth failed: {e.Message}");
                SetStatus("Sign-in failed — check console.");
                SetButtonsInteractable(true);
                return;
            }

            SetStatus("Creating relay…");
            Allocation allocation;
            try { allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections); }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] CreateAllocation failed: {e.Message}");
                SetStatus("Relay error — check console.");
                SetButtonsInteractable(true);
                return;
            }

            string joinCode;
            try { joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId); }
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

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.ConnectionApprovalCallback       = OnConnectionApproval;
            NetworkManager.Singleton.OnClientConnectedCallback        += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback       += OnClientDisconnected;
            NetworkManager.Singleton.StartHost();
            CharacterPicker.Instance?.RegisterHostHandler();
            CharacterPicker.Instance?.SelectCharacter(CharacterPicker.Instance.LocalSelectedIndex);

            RegisterReadyHandlers();

            startButton.gameObject.SetActive(true);
            startButton.interactable = false;
            ShowReadyButton();
            ShowTabs(isHost: true);

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

            try { await InitServicesAsync(); }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] Auth failed: {e.Message}");
                SetStatus("Sign-in failed — check console.");
                SetButtonsInteractable(true);
                return;
            }

            SetStatus($"Joining relay '{code}'…");
            JoinAllocation joinAllocation;
            try { joinAllocation = await RelayService.Instance.JoinAllocationAsync(code); }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbyManager] JoinAllocation failed: {e.Message}");
                SetStatus("Join failed — invalid code?");
                SetButtonsInteractable(true);
                return;
            }

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(joinAllocation, "dtls"));

            NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.StartClient();
            RegisterReadyHandlers();

            SetStatus("Connecting…");
        }

        // ── Relay callbacks ───────────────────────────────────────────────────

        private void OnClientConnected(ulong clientId)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                RefreshPlayerCount();
                m_readySet.Clear();
                BroadcastReadyCount();
            }
            else
            {
                if (clientId == NetworkManager.Singleton.LocalClientId)
                {
                    SetStatus("Connected — press Ready when you're set!");
                    ShowReadyButton();
                    ShowTabs(isHost: false);
                }
                CharacterPicker.Instance?.SelectCharacter(CharacterPicker.Instance.LocalSelectedIndex);
            }
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
            m_gameLocked = true;
            Debug.Log($"[LobbyManager] Loading '{gameplaySceneName}'");
            NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);
        }

        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
                                          NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved           = !m_gameLocked;
            response.CreatePlayerObject = !m_gameLocked;
        }

        private void RefreshPlayerCount()
        {
            var count = NetworkManager.Singleton.ConnectedClients.Count;
            SetPlayerCount(count);
            startButton.interactable = count >= MinPlayersToStart;
        }

        // ── Ready system ─────────────────────────────────────────────────────

        private void ShowReadyButton()
        {
            if (readyButton    != null) { readyButton.gameObject.SetActive(true); readyButton.interactable = true; }
            if (readyStatusText != null) readyStatusText.gameObject.SetActive(true);
        }

        private void OnReadyClicked()
        {
            if (readyButton != null) readyButton.interactable = false;
            SetStatus("Ready!");
            if (NetworkManager.Singleton.IsServer)
                MarkReady(NetworkManager.Singleton.LocalClientId);
            else
                SendReadyToHost();
        }

        private void MarkReady(ulong clientId)
        {
            m_readySet.Add(clientId);
            BroadcastReadyCount();
            int total = NetworkManager.Singleton.ConnectedClients.Count;
            Debug.Log($"[LobbyManager] {m_readySet.Count}/{total} ready.");
            if (m_readySet.Count >= total && total >= MinPlayersToStart)
            {
                Debug.Log("[LobbyManager] All players ready — auto-starting.");
                OnStartClicked();
            }
        }

        private void BroadcastReadyCount()
        {
            int ready = m_readySet.Count;
            int total = NetworkManager.Singleton.ConnectedClients.Count;
            using var writer = new FastBufferWriter(8, Allocator.Temp);
            writer.WriteValueSafe(ready);
            writer.WriteValueSafe(total);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(k_ReadyCountMsg, writer);
            UpdateReadyStatusText(ready, total);
        }

        private void SendReadyToHost()
        {
            using var writer = new FastBufferWriter(4, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager
                .SendNamedMessage(k_ReadyMsg, NetworkManager.ServerClientId, writer);
        }

        private void RegisterReadyHandlers()
        {
            var msg = NetworkManager.Singleton.CustomMessagingManager;
            msg.RegisterNamedMessageHandler(k_ReadyMsg, (senderId, _) =>
            {
                Debug.Log($"[LobbyManager] Ready from client {senderId}.");
                MarkReady(senderId);
            });
            msg.RegisterNamedMessageHandler(k_ReadyCountMsg, (_, reader) =>
            {
                reader.ReadValueSafe(out int ready);
                reader.ReadValueSafe(out int total);
                UpdateReadyStatusText(ready, total);
            });
        }

        private void UpdateReadyStatusText(int ready, int total)
        {
            if (readyStatusText != null)
                readyStatusText.text = $"{ready} / {total} Ready";
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

        // ── Tabs ──────────────────────────────────────────────────────────────

        private void ShowTabs(bool isHost)
        {
            // CharacterTabContent is inactive at this point — must search inactive objects too
            m_charSelectUI = FindFirstObjectByType<CharacterSelectUI>(FindObjectsInactive.Include);
            if (m_charSelectUI != null)
                m_charSelectUI.CharacterChanged += OnCharacterChanged;

            if (selectionSummaryText != null) selectionSummaryText.gameObject.SetActive(true);

            if (isHost)
            {
                // Host: show both tab buttons, default to Level tab
                if (levelTabButton     != null) levelTabButton.gameObject.SetActive(true);
                if (characterTabButton != null) characterTabButton.gameObject.SetActive(true);
                // Default level selection
                SelectLevel(gameplaySceneName, GetLevelIndex(gameplaySceneName));
                ShowTab(true);
            }
            else
            {
                // Client: no tab bar — go straight to character tab
                if (characterTabContent != null) characterTabContent.SetActive(true);
                UpdateSummary();
            }
        }

        private void ShowTab(bool showLevel)
        {
            if (levelTabContent     != null) levelTabContent.SetActive(showLevel);
            if (characterTabContent != null) characterTabContent.SetActive(!showLevel);
            HighlightTabButton(levelTabButton,     showLevel);
            HighlightTabButton(characterTabButton, !showLevel);
        }

        private static readonly Color k_selectedColor   = new Color(1f,   0.85f, 0.1f);
        private static readonly Color k_unselectedColor = new Color(0.25f, 0.25f, 0.25f);

        private static void HighlightTabButton(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? k_selectedColor : k_unselectedColor;
        }

        private void OnCharacterChanged(int _) => UpdateSummary();

        private void UpdateSummary()
        {
            if (selectionSummaryText == null) return;
            int    charIdx  = CharacterPicker.Instance != null ? CharacterPicker.Instance.LocalSelectedIndex : 0;
            string charName = charIdx >= 0 && charIdx < k_charNames.Length ? k_charNames[charIdx] : "?";

            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            selectionSummaryText.text = isHost ? $"{m_selectedLevelDisplay}  ·  {charName}" : charName;
        }

        // ── Level selection ───────────────────────────────────────────────────

        private void SelectLevel(string sceneName, int displayIndex)
        {
            gameplaySceneName    = sceneName;
            m_selectedLevelDisplay = displayIndex >= 0 && displayIndex < k_levelNames.Length
                ? k_levelNames[displayIndex] : "Level 1";

            HighlightLevelButton(level1Button, displayIndex == 0);
            HighlightLevelButton(level2Button, displayIndex == 1);
            HighlightLevelButton(level3Button, displayIndex == 2);
            HighlightLevelButton(level4Button, displayIndex == 3);

            UpdateSummary();
            Debug.Log($"[LobbyManager] Level selected: {sceneName}");
        }

        private static int GetLevelIndex(string sceneName) => sceneName switch
        {
            "DeathRunL2" => 1,
            "DeathRunL3" => 2,
            "ObstacleL1" => 3,
            _            => 0,
        };

        private static void HighlightLevelButton(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = selected ? k_selectedColor : k_unselectedColor;
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
