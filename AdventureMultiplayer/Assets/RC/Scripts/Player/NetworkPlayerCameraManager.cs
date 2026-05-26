using UnityEngine;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Multiplayer-safe replacement for PlayerCameraManager.
    /// Defers player-dependent initialization until Level.instance.player is set,
    /// because in a networked game the player spawns after scene Start().
    ///
    /// In the scene: remove the PlayerCameraManager component and add this one instead.
    /// All Inspector fields (thirdPersonCamera, sideScrollerCamera) are inherited.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Player Camera Manager")]
    public class NetworkPlayerCameraManager : PlayerCameraManager
    {
        protected override void Start()
        {
            // Camera/brain setup has no player dependency — safe to run now.
            InitializeCameras();
            InitializeBrain();

            if (player != null)
            {
                InitializeCallbacks();
                ActivateCurrentCamera();
                return;
            }

            Debug.Log("[NetworkPlayerCameraManager] Player not ready at Start — deferring callbacks.");

            if (Level.instance != null)
                Level.instance.onPlayerChanged.AddListener(OnPlayerReady);
            else
                Debug.LogWarning("[NetworkPlayerCameraManager] Level.instance is null at Start.");
        }

        private void OnPlayerReady(Player p)
        {
            Level.instance.onPlayerChanged.RemoveListener(OnPlayerReady);
            Debug.Log("[NetworkPlayerCameraManager] Player ready — activating camera.");
            InitializeCallbacks();
            ActivateCurrentCamera();
        }
    }
}
