using EmptyKeys.UserInterface.Media;
using VRage.Audio;

namespace VRage.UserInterface.Media
{
    public class MyAudioDevice : AudioDevice
    {
        public IMyGuiAudio GuiAudio { get; set; }

        public override SoundBase CreateSound(object nativeSound) => new MySound(nativeSound);
    }
}
