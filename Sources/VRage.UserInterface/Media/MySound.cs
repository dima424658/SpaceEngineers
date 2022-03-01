using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Media;
using VRage.Audio;

namespace VRage.UserInterface.Media
{
    public class MySound : SoundBase
    {
        private GuiSounds sound;

        public override float Volume { get; set; }

        public MySound(object nativeSound) : base(nativeSound)
        {
            Enum.TryParse(nativeSound.ToString(), out sound);
        }

        public override void Pause()
        {
        }

        public override void Play()
        {
            MyAudioDevice audioDevice = (MyAudioDevice)Engine.Instance.AudioDevice;
            if (Engine.Instance != null && audioDevice != null && audioDevice.GuiAudio != null)
                audioDevice.GuiAudio.PlaySound(sound);
        }

        public override SoundState State => SoundState.Stopped;

        public override void Stop() { }
    }
}
