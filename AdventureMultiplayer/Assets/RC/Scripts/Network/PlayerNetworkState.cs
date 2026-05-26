using UnityEngine;
using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Snapshot of everything needed to reconstruct a player's state on remote clients.
    /// Sent every network tick by the owning client via NetworkedMovementSync.
    /// </summary>
    public struct PlayerNetworkState : INetworkSerializable
    {
        public Vector3    Position;
        public Vector3    Velocity;
        public Quaternion Rotation;
        public int        StateIndex;
        public bool       IsGrounded;
        public int        JumpCounter;
        public bool       IsHolding;
        /// <summary>
        /// True when the player is on a wall/ledge surface (WallDrag, LedgeHanging,
        /// LedgeClimbing, PoleClimbing). Ghosts suppress dead reckoning in these states
        /// and snap directly to the authoritative position instead.
        /// </summary>
        public bool       IsOnSurface;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref StateIndex);
            serializer.SerializeValue(ref IsGrounded);
            serializer.SerializeValue(ref JumpCounter);
            serializer.SerializeValue(ref IsHolding);
            serializer.SerializeValue(ref IsOnSurface);
        }
    }
}
