using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// In-race control buttons (top-right, next to the position badge):
    ///   - Reset : teleport the local player back to their last checkpoint.
    ///   - Exit  : ask for confirmation, then shut down the connection and return to the lobby.
    ///
    /// Setup: assign resetButton + exitButton in the Inspector. For the confirmation popup,
    /// assign confirmExitPopup (root, starts inactive), confirmExitYesButton and
    /// confirmExitNoButton. If the popup fields are left empty the Exit button leaves
    /// immediately (old behaviour).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Reset Button HUD")]
    public class ResetButtonHUD : MonoBehaviour
    {
        [SerializeField] private Button resetButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private string lobbySceneName = "Lobby";

        [Header("Exit Confirmation Popup")]
        [Tooltip("Root object of the 'Are you sure you want to exit?' popup. Starts inactive; " +
                 "shown when Exit is pressed. Leave empty to skip confirmation.")]
        [SerializeField] private GameObject confirmExitPopup;
        [SerializeField] private Button confirmExitYesButton;
        [SerializeField] private Button confirmExitNoButton;
        [Tooltip("Optional panel that scales in when the popup opens (for a small pop animation).")]
        [SerializeField] private RectTransform confirmExitPanel;

        private void Awake()
        {
            resetButton?.onClick.AddListener(OnResetClicked);
            exitButton?.onClick.AddListener(OnExitClicked);

            if (confirmExitYesButton != null) confirmExitYesButton.onClick.AddListener(OnConfirmExitYes);
            if (confirmExitNoButton  != null) confirmExitNoButton.onClick.AddListener(OnConfirmExitNo);
            if (confirmExitPopup     != null) confirmExitPopup.SetActive(false);
        }

        private void OnResetClicked()
        {
            if (NetworkManager.Singleton == null) return;
            var localObj  = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            var respawner = localObj?.GetComponent<NetworkRespawner>();
            if (respawner != null)
                respawner.RespawnNow();
            else
                Debug.LogWarning("[ResetButtonHUD] No NetworkRespawner found on local player.");
        }

        // ── Exit + confirmation ───────────────────────────────────────────────

        private void OnExitClicked()
        {
            if (confirmExitPopup == null)
            {
                // No popup wired — keep the old behaviour of leaving straight away.
                ExitToLobbyAsync(lobbySceneName).Forget();
                return;
            }

            confirmExitPopup.SetActive(true);
            if (confirmExitPanel != null)
            {
                confirmExitPanel.DOKill();
                confirmExitPanel.localScale = Vector3.one * 0.8f;
                confirmExitPanel.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void OnConfirmExitNo()
        {
            if (confirmExitPanel != null) confirmExitPanel.DOKill();
            if (confirmExitPopup != null) confirmExitPopup.SetActive(false);
        }

        private void OnConfirmExitYes()
        {
            if (confirmExitYesButton != null) confirmExitYesButton.interactable = false;
            ExitToLobbyAsync(lobbySceneName).Forget();
        }

        /// <summary>
        /// NGO's <see cref="NetworkManager.Shutdown"/> is deferred — it finishes on the next
        /// NetworkManager update, not synchronously. Loading the lobby scene in the same frame
        /// leaves the transport and NGO singletons half-torn-down, which is why the very next
        /// Host attempt then fails inside Relay allocation. Wait for shutdown to actually
        /// complete (plus one frame) before leaving.
        /// </summary>
        public static async UniTaskVoid ExitToLobbyAsync(string lobbySceneName)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && (nm.IsListening || nm.ShutdownInProgress))
            {
                if (!nm.ShutdownInProgress) nm.Shutdown();
                await UniTask.WaitUntil(() => nm == null || (!nm.IsListening && !nm.ShutdownInProgress));
                await UniTask.NextFrame();
            }
            SceneManager.LoadScene(lobbySceneName);
        }
    }
}
