using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Diagnostic: logs WASD / movement input and component enabled states.
    /// Attach to the HumanPlayer prefab. Remove once the host-movement issue is resolved.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Input Logger (Debug)")]
    public class NetworkedInputLogger : NetworkBehaviour
    {
        private PLAYERTWO.PlatformerProject.PlayerInputManager m_inputManager;
        private Player           m_player;
        private EntityController m_entityController;

        private bool m_loggedThisSession;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            m_inputManager     = GetComponent<PLAYERTWO.PlatformerProject.PlayerInputManager>();
            m_player           = GetComponent<Player>();
            m_entityController = GetComponent<EntityController>();

            Debug.Log($"[InputLogger] OnNetworkSpawn (owner, clientId={OwnerClientId} IsHost={IsHost}): " +
                      $"PlayerInputManager.enabled={m_inputManager?.enabled} " +
                      $"Player.enabled={m_player?.enabled} " +
                      $"EntityController.enabled={m_entityController?.enabled}");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
                Debug.Log($"[InputLogger] W pressed — IsSpawned={IsSpawned} IsOwner={IsOwner} " +
                          $"pos={transform.position} " +
                          $"Player.enabled={m_player?.enabled} " +
                          $"EC.enabled={m_entityController?.enabled} " +
                          $"PIM.enabled={m_inputManager?.enabled}");

            if (!IsSpawned || !IsOwner) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            bool wPressed = kb.wKey.wasPressedThisFrame;
            bool aPressed = kb.aKey.wasPressedThisFrame;
            bool sPressed = kb.sKey.wasPressedThisFrame;
            bool dPressed = kb.dKey.wasPressedThisFrame;

            if (wPressed || aPressed || sPressed || dPressed)
            {
                string keys = $"{(wPressed ? "W" : "")}{(aPressed ? "A" : "")}{(sPressed ? "S" : "")}{(dPressed ? "D" : "")}";
                Debug.Log($"[InputLogger] WASD pressed ({keys}) — " +
                          $"PlayerInputManager.enabled={m_inputManager?.enabled} " +
                          $"Player.enabled={m_player?.enabled} " +
                          $"EntityController.enabled={m_entityController?.enabled} " +
                          $"IsOwner={IsOwner} IsHost={IsHost}");
            }

            if (!m_loggedThisSession || Time.frameCount % 60 == 0)
            {
                if (m_inputManager != null && m_inputManager.enabled)
                {
                    var moveDir = m_inputManager.GetMovementDirection();
                    if (moveDir.sqrMagnitude > 0.01f)
                    {
                        m_loggedThisSession = true;
                        Debug.Log($"[InputLogger] Movement direction from PlayerInputManager: {moveDir}");
                    }
                }
            }
        }
    }
}
