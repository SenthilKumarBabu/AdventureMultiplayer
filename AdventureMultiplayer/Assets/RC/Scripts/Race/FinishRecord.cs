using Unity.Netcode;

namespace AdventureMultiplayer
{
    /// <summary>
    /// Appended to RaceManager.FinishRecords (a NetworkList) when a player crosses
    /// the finish line. Uses Add rather than [i]=value so there is no NGO staging
    /// delay — clients see the record immediately when it arrives.
    /// </summary>
    public struct FinishRecord : INetworkSerializable, System.IEquatable<FinishRecord>
    {
        public ulong ClientId;
        public float FinishTimeSeconds;
        public int   FinishOrder;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref FinishTimeSeconds);
            serializer.SerializeValue(ref FinishOrder);
        }

        public bool Equals(FinishRecord other) => ClientId == other.ClientId;
        public override int GetHashCode() => ClientId.GetHashCode();
    }
}
