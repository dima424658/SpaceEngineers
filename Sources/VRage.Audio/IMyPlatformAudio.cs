using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace VRage.Audio
{
  public interface IMyPlatformAudio
  {
    int DeviceCount { get; }

    XAudio2 InitAudioEngine();

    void DisposeAudioEngine();

    DeviceDetails GetDeviceDetails(int deviceIndex, bool forceRefresh);

    MasteringVoice CreateMasteringVoice(int deviceIndex);

    void InitializeAudioBuffer(AudioBuffer audioBuffer, WaveFormat format);
  }
}
