using UnityEngine;
using Unity.Netcode;
using PLAYERTWO.PlatformerProject;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Shared network helpers that are safe to call on any machine (server or client).
    /// </summary>
    public static class NetworkUtils
    {
        /// <summary>
        /// Returns the Player component for the given client ID.
        /// Unlike SpawnManager.GetPlayerNetworkObject(), this works on non-server clients too.
        /// </summary>
        public static Player FindPlayerByClientId(ulong clientId)
        {
            foreach (var netObj in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
                if (netObj.IsPlayerObject && netObj.OwnerClientId == clientId)
                    return netObj.GetComponent<Player>();
            return null;
        }
    }
}
