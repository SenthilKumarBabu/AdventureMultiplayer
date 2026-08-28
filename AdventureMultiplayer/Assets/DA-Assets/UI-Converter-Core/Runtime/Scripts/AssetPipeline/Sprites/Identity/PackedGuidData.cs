#if UNITY_EDITOR
using System;

namespace DA_Assets.UCC
{
    public readonly struct IntFloatGuid
    {
        private readonly Guid _guid;
        private IntFloatGuid(Guid g) => _guid = g;
        public Guid Value => _guid;










        public static IntFloatGuid Encode(int hash)
        {
            var bytes = new byte[16];
            var h = BitConverter.GetBytes(hash);
            Buffer.BlockCopy(h, 0, bytes, 0, 4);
            return new IntFloatGuid(new Guid(bytes));
        }

        public static int Decode(Guid g)
        {
            byte[] b = g.ToByteArray();
            return BitConverter.ToInt32(b, 0);
        }
    }
}
#endif