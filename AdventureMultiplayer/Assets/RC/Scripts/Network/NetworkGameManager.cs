using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Lives in the gameplay scene (DeathRunL1).
    /// Spawns a player object for every client once the scene is fully loaded
    /// on all peers, using NGO's OnLoadEventCompleted callback.
    ///
    /// Connection and lobby logic has moved to LobbyManager (Lobby scene).
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Network Game Manager")]
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Player Spawning")]
        [SerializeField] private GameObject[] characterPrefabs; // 0=Lily, 1=Saphy, …
        [SerializeField] private Transform[]  spawnPoints;

        private void Awake()
        {
            if (NetworkManager.Singleton?.SceneManager == null)
            {
                Debug.LogWarning("[NetworkGameManager] No NetworkManager.SceneManager at Awake — " +
                                 "was the game started directly from DeathRunL1? Use the Lobby scene.");
                return;
            }

            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            Debug.Log("[NetworkGameManager] Subscribed to OnLoadEventCompleted.");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton?.SceneManager == null) return;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
        }

        // ── Scene loaded ──────────────────────────────────────────────────────

        private void OnSceneLoadCompleted(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            Debug.Log($"[NetworkGameManager] '{sceneName}' loaded on " +
                      $"{clientsCompleted.Count} client(s). Spawning players.");

            var spawnIndex = 0;
            foreach (var clientId in clientsCompleted)
                SpawnPlayer(clientId, spawnIndex++);

            if (clientsTimedOut.Count > 0)
                Debug.LogWarning($"[NetworkGameManager] {clientsTimedOut.Count} client(s) timed out during scene load.");
        }

        // ── Spawning ──────────────────────────────────────────────────────────

        private void SpawnPlayer(ulong clientId, int spawnIndex)
        {
            if (characterPrefabs == null || characterPrefabs.Length == 0)
            {
                Debug.LogError("[NetworkGameManager] characterPrefabs is empty — assign prefabs in the Inspector.");
                return;
            }

            var charIndex = CharacterPicker.Instance != null
                ? CharacterPicker.Instance.GetSelection(clientId)
                : 0;
            charIndex = Mathf.Clamp(charIndex, 0, characterPrefabs.Length - 1);
            var prefab = characterPrefabs[charIndex];

            if (prefab == null)
            {
                Debug.LogError($"[NetworkGameManager] characterPrefabs[{charIndex}] is null.");
                return;
            }

            var spawnPos = GetSpawnPoint(spawnIndex);
            var player   = Object.Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 90f, 0f));
            var netObj   = player.GetComponent<NetworkObject>();

            if (netObj == null)
            {
                Debug.LogError("[NetworkGameManager] platformPlayerPrefab is missing a NetworkObject.");
                Object.Destroy(player);
                return;
            }

            netObj.SpawnAsPlayerObject(clientId);
            Debug.Log($"[NetworkGameManager] Spawned player for clientId={clientId} at {spawnPos}.");
        }

        private Vector3 GetSpawnPoint(int index)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return Vector3.zero;
            return spawnPoints[index % spawnPoints.Length].position;
        }
    }
}
