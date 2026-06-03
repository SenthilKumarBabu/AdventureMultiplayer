using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Wires the in-race reset button to the local player's NetworkRespawner.
    /// Pressing the button instantly teleports the local player back to their
    /// last reached checkpoint.
    ///
    /// Setup: assign resetButton in the Inspector.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/HUD/Reset Button HUD")]
    public class ResetButtonHUD : MonoBehaviour
    {
        [SerializeField] private Button resetButton;

        private void Awake()
        {
            resetButton?.onClick.AddListener(OnResetClicked);
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
    }
}
