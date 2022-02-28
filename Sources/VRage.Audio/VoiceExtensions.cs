using SharpDX.XAudio2;

namespace VRage.Audio
{
    internal static class VoiceExtensions
    {
        public static bool IsValid(this SourceVoice self) => !self.IsDisposed && self.NativePointer != IntPtr.Zero;
    }
}
