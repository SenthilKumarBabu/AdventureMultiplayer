using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>Serializable per-player race state stored in RaceManager's NetworkList.</summary>
    public struct RaceEntry : INetworkSerializable, System.IEquatable<RaceEntry>
    {
        public ulong ClientId;
        public int   CheckpointIndex;    // highest checkpoint reached
        public int   RacePosition;       // 1 = 1st, 2 = 2nd, etc.
        public bool  Finished;
        public int   FinishOrder;        // 1 = first to finish/DNF, 2 = second, etc. (0 = still racing)
        public float FinishTimeSeconds;  // seconds from race start to finish (0 if not finished/DNF)

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref CheckpointIndex);
            serializer.SerializeValue(ref RacePosition);
            serializer.SerializeValue(ref Finished);
            serializer.SerializeValue(ref FinishOrder);
            serializer.SerializeValue(ref FinishTimeSeconds);
        }

        public bool Equals(RaceEntry other) => ClientId == other.ClientId;
        public override int GetHashCode()   => ClientId.GetHashCode();
    }
}
