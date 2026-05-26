using Unity.Netcode.Components;
using UnityEngine;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Owner-authoritative NetworkTransform.
    /// The owning client writes its world position/rotation each tick;
    /// the server and all other clients interpolate from those values.
    ///
    /// Add this component to the player prefab IN PLACE of the default NetworkTransform.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Adventure Multiplayer/Client Network Transform")]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
