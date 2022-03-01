using ProtoBuf;
using System.Xml.Serialization;

namespace VRageMath
{
    /// Reflective because it can be reflected to the opposite range.
    [ProtoContract]
    public struct SymmetricSerializableRange
    {
        [XmlAttribute(AttributeName = "Min")]
        public float Min;
        [XmlAttribute(AttributeName = "Max")]
        public float Max;
        private bool m_notMirror;

        [XmlAttribute(AttributeName = "Mirror")]
        public bool Mirror
        {
            get => !m_notMirror;
            set => m_notMirror = !value;
        }

        public SymmetricSerializableRange(float min, float max, bool mirror = true)
        {
            Max = max;
            Min = min;
            m_notMirror = !mirror;
        }

        public bool ValueBetween(float value)
        {
            if (!m_notMirror)
                value = Math.Abs(value);

            return value >= Min && value <= Max;
        }

        public override string ToString() => string.Format("{0}[{1}, {2}]", Mirror ? (object)"MirroredRange" : (object)"Range", (object)Min, (object)Max);

        /// When the range is an angle this method changes it to the cosines of the angle.
        ///             
        ///             The angle is expected to be in degrees.
        ///             
        ///             Also beware that cosine is a decreasing function in [0,90], for that reason the minimum and maximum are swaped.
        public SymmetricSerializableRange ConvertToCosine()
        {
            float max = Max;
            Max = MathF.Cos(Min * MathF.PI / 180.0f);
            Min = MathF.Cos(max * MathF.PI / 180.0f);

            return this;
        }

        /// When the range is an angle this method changes it to the sines of the angle.
        ///             
        ///             The angle is expected to be in degrees.
        public SymmetricSerializableRange ConvertToSine()
        {
            Max = MathF.Sin(Max * MathF.PI / 180.0f);
            Min = MathF.Sin(Min * MathF.PI / 180.0f);

            return this;
        }

        public SymmetricSerializableRange ConvertToCosineLongitude()
        {
            Max = CosineLongitude(Max);
            Min = CosineLongitude(Min);

            return this;
        }

        private static float CosineLongitude(float angle) => angle <= 0 ? MathF.Cos(angle * MathF.PI / 180.0f) : 2 - MathF.Cos(angle * MathF.PI / 180.0f);

        public string ToStringAsin() => string.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Asin(Min)), MathHelper.ToDegrees(Math.Asin(Max)));

        public string ToStringAcos() => string.Format("Range[{0}, {1}]", MathHelper.ToDegrees(Math.Acos(Min)), MathHelper.ToDegrees(Math.Acos(Max)));
    }
}
