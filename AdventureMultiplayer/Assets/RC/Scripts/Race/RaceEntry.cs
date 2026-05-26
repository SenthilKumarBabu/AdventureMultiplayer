using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>Serializable per-player race state stored in RaceManager's NetworkList.</summary>
    public struct RaceEntry : INetworkSerializable, System.IEquatable<RaceEntry>
    {
        public ulong ClientId;
        public int   CheckpointIndex;  // highest checkpoint reached
        public int   RacePosition;     // 1 = 1st, 2 = 2nd, etc.
        public bool  Finished;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref CheckpointIndex);
            serializer.SerializeValue(ref RacePosition);
            serializer.SerializeValue(ref Finished);
        }

        public bool Equals(RaceEntry other) => ClientId == other.ClientId;
        public override int GetHashCode()   => ClientId.GetHashCode();
    }
}
