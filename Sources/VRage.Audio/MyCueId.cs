using ProtoBuf;
using VRage.Utils;

namespace VRage.Audio
{
    [ProtoContract]
    public struct MyCueId
    {
        [ProtoMember(1)]
        public MyStringHash Hash;
        public static readonly ComparerType Comparer = new MyCueId.ComparerType();

        public MyCueId(MyStringHash hash) => Hash = hash;

        public bool IsNull => Hash == MyStringHash.NullOrEmpty;

        public static bool operator ==(MyCueId r, MyCueId l) => r.Hash == l.Hash;

        public static bool operator !=(MyCueId r, MyCueId l) => r.Hash != l.Hash;

        public bool Equals(MyCueId obj) => obj.Hash.Equals(Hash);

        public override bool Equals(object? obj)
        {
            return obj is MyCueId myCueId && myCueId.Hash.Equals(Hash);
        }

        public override int GetHashCode() => Hash.GetHashCode();

        public override string ToString() => Hash.ToString();

        public class ComparerType : IEqualityComparer<MyCueId>
        {
            bool IEqualityComparer<MyCueId>.Equals(MyCueId x, MyCueId y) => x.Hash == y.Hash;

            int IEqualityComparer<MyCueId>.GetHashCode(MyCueId obj) => obj.Hash.GetHashCode();
        }
    }
}
