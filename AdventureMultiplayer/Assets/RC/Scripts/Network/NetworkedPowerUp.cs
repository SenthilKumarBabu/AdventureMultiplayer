// Replaced by the new PowerUp system:
//   Assets/RC/Scripts/PowerUp/PlayerPowerUpInventory.cs  — 3-slot inventory + all 9 effects
//   Assets/RC/Scripts/PowerUp/NetworkedPowerUpBox.cs     — world box with respawn
//   Assets/RC/Scripts/PowerUp/BananaPeel.cs
//   Assets/RC/Scripts/PowerUp/DecoyBoxPickup.cs
// This file is kept as an empty stub so existing scene references do not break
// until the NetworkedPowerUp component is manually removed from any prefabs/scenes.

using Unity.Netcode;
using UnityEngine;

namespace AdventureMultiplayer
{
    [RequireComponent(typeof(NetworkObject))]
    [AddComponentMenu("Adventure Multiplayer/Networked Power Up (DEPRECATED)")]
    public class NetworkedPowerUp : NetworkBehaviour
    {
        // Intentionally empty — do not add new logic here.
        // Remove this component from all prefabs and scenes and delete this file.
    }
}
