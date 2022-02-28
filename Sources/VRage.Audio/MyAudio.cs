using VRage.Collections;
using VRage.Data.Audio;

namespace VRage.Audio
{
    public class MyAudio
    {
        public static readonly int MAX_SAMPLE_RATE = 48000;

        public static MySoundErrorDelegate? OnSoundError { get; private set; }

        public static IMyAudio? Static { get; private set; }

        public static void LoadData(MyAudioInitParams initParams, ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            Static = initParams.Instance;
            OnSoundError = initParams.OnSoundError;
            Static.LoadData(initParams, sounds, effects);
        }

        public static void ReloadData(ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects) => Static.ReloadData(sounds, effects);

        public static void UnloadData()
        {
            if (Static == null)
                return;
            Static.UnloadData();
            Static.DisposeCache();
            Static = null;
        }
    }
}
