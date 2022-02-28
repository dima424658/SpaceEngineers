using SharpDX.Multimedia;

namespace VRage.Audio
{
    public struct MyWaveFormat : IEquatable<MyWaveFormat>
    {
        public WaveFormatEncoding Encoding;
        public int Channels;
        public int SampleRate;
        public WaveFormat WaveFormat;

        public bool Equals(MyWaveFormat y)
        {
            return Encoding == y.Encoding &&
                Channels == y.Channels &&
                (SampleRate == y.SampleRate ||
                Encoding == WaveFormatEncoding.Adpcm ||
                y.Encoding == WaveFormatEncoding.Adpcm);
        }

        public override int GetHashCode()
        {
            int hashCode = (int)Encoding * 397 ^ Channels;
            if (Encoding != WaveFormatEncoding.Adpcm)
                hashCode = hashCode * 397 ^ SampleRate;

            return hashCode;
        }
    }
}
