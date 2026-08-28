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

        private const string SelectMessageName    = "CharacterSelection";
        private const string BroadcastMessageName = "CharacterSelectionBroadcast";

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

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                // Not connected yet, so the real ClientId isn't known. Stash under 0 as a
                // placeholder for "the eventual host" — this gets corrected once the network
                // starts and SelectCharacter is re-invoked with the real LocalClientId
                // (see LobbyManager StartHost/OnClientConnected). Must NOT run once listening,
                // otherwise a non-host client (LocalClientId != 0) would clobber client 0's
                // (the host's) real selection with its own every time it re-applies its pick.
                m_Selections[0] = index;
                return;
            }

            var localId = NetworkManager.Singleton.LocalClientId;
            m_Selections[localId] = index;

            if (NetworkManager.Singleton.IsServer)
                BroadcastSelection(localId, index);
            else
                SendSelectionToHost(index);
        }

        // ── Host registration / querying ──────────────────────────────────────

        public void RegisterHostHandler()
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                SelectMessageName, OnSelectionReceivedByHost);
            Debug.Log("[CharacterPicker] Host handler registered.");
        }

        public void UnregisterHostHandler()
        {
            NetworkManager.Singleton?.CustomMessagingManager
                ?.UnregisterNamedMessageHandler(SelectMessageName);
        }

        /// <summary>Lets a (non-host) client receive other players' selections relayed by
        /// the host. Call once, right after StartClient.</summary>
        public void RegisterClientHandler()
        {
            if (NetworkManager.Singleton?.CustomMessagingManager == null) return;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
                BroadcastMessageName, OnBroadcastReceived);
        }

        /// <summary>Host-only: sends every currently known selection to one client
        /// (typically one that just connected), so late joiners see already-picked
        /// players correctly instead of falling back to the default character.</summary>
        public void SendRosterTo(ulong targetClientId)
        {
            if (NetworkManager.Singleton?.IsServer != true) return;
            foreach (var kvp in m_Selections)
                SendBroadcastMessage(kvp.Key, kvp.Value, targetClientId);
        }

        public int GetSelection(ulong clientId) =>
            m_Selections.TryGetValue(clientId, out var idx) ? idx : 0;

        /// <summary>
        /// Server-only: registers an AI bot's character index the same way a human's
        /// SelectCharacter pick would. Bots never go through the Lobby's pick flow, so
        /// without this GetSelection(botId) always falls back to 0 ("Gale") for every
        /// bot — the results screen would show every bot with the same wrong name.
        /// Broadcasts to all clients so remote peers resolve the bot's name correctly too.
        /// </summary>
        public void RegisterBotSelection(ulong botId, int charIndex)
        {
            m_Selections[botId] = charIndex;
            if (NetworkManager.Singleton?.IsServer == true)
                BroadcastSelection(botId, charIndex);
        }

        // ── Internals ─────────────────────────────────────────────────────────

        private void SendSelectionToHost(int index)
        {
            using var writer = new FastBufferWriter(sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(index);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                SelectMessageName, NetworkManager.ServerClientId, writer);
            Debug.Log($"[CharacterPicker] Sent selection {index} to host.");
        }

        private void OnSelectionReceivedByHost(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int index);
            m_Selections[senderId] = index;
            Debug.Log($"[CharacterPicker] Host received selection {index} from client {senderId}.");
            BroadcastSelection(senderId, index);
        }

        // Host-only: tells every connected client which character a given clientId picked,
        // so remote clients (not just the host) can resolve everyone's real name/character.
        private void BroadcastSelection(ulong clientId, int index)
        {
            if (NetworkManager.Singleton?.IsServer != true) return;
            using var writer = new FastBufferWriter(sizeof(ulong) + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(index);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(BroadcastMessageName, writer);
        }

        private void SendBroadcastMessage(ulong clientId, int index, ulong targetClientId)
        {
            using var writer = new FastBufferWriter(sizeof(ulong) + sizeof(int), Allocator.Temp);
            writer.WriteValueSafe(clientId);
            writer.WriteValueSafe(index);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                BroadcastMessageName, targetClientId, writer);
        }

        private void OnBroadcastReceived(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ulong clientId);
            reader.ReadValueSafe(out int index);
            m_Selections[clientId] = index;
            Debug.Log($"[CharacterPicker] Received broadcast: client {clientId} → {index}.");
        }
    }
}
