using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Singleton that survives scene loads and tracks each client's character choice.
    ///
    /// Flow:
    ///   1. Client selects character in Lobby → calls SelectCharacter(index)
    ///   2. If network is started, the selection is sent to the host via CustomMessaging
    ///   3. NetworkGameManager queries GetSelection(clientId) when spawning
    ///
    /// Must exist in the Lobby scene and be tagged DontDestroyOnLoad.
    /// </summary>
    [AddComponentMenu("Adventure Multiplayer/Character Picker")]
    public class CharacterPicker : MonoBehaviour
    {
        public static CharacterPicker Instance { get; private set; }

        private const string MessageName = "CharacterSelection";

        private readonly Dictionary<ulong, int> m_Selections = new();

        // The index this local client has picked
        public int LocalSelectedIndex { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Called by the Lobby UI ────────────────────────────────────────────

        public void SelectCharacter(int index)
        {
            LocalSelectedIndex = index;
            m_Selections[0] = index; // fallback for host before network starts

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

            var localId = NetworkManager.Singleton.LocalClientId;
            m_Selections[localId] = index;

            if (!NetworkManager.Singleton.IsServer)
                SendSelectionToHost(index);
        }

        // ── Host registration / querying ──────────────────────────────────────

        public void RegisterHostHandler()
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                MessageName, OnSelectionReceived);
            Debug.Log("[CharacterPicker] Host handler registered.");
        }

        public void UnregisterHostHandler()
        {
            NetworkManager.Singleton?.CustomMessagingManager
                ?.UnregisterNamedMessageHandler(MessageName);
        }

        public int GetSelection(ulong clientId) =>
            m_Selections.TryGetValue(clientId, out var idx) ? idx : 0;

        // ── Internals ─────────────────────────────────────────────────────────

        private void SendSelectionToHost(int index)
        {
            using var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(index);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                MessageName, NetworkManager.ServerClientId, writer);
            Debug.Log($"[CharacterPicker] Sent selection {index} to host.");
        }

        private void OnSelectionReceived(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int index);
            m_Selections[senderId] = index;
            Debug.Log($"[CharacterPicker] Host received selection {index} from client {senderId}.");
        }
    }
}
