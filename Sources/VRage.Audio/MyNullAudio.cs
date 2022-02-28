using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public class MyNullAudio : IMyAudio
    {
        Dictionary<MyCueId, MySoundData>.ValueCollection? IMyAudio.CueDefinitions => null;

        MySoundData? IMyAudio.SoloCue { get; set; }

        bool IMyAudio.ApplyReverb { get => false; set { } }

        int IMyAudio.SampleRate => 0;

        void IMyAudio.SetReverbParameters(float diffusion, float roomSize) { }

        float IMyAudio.VolumeMusic { get; set; }

        float IMyAudio.VolumeHud
        {
            get => 0.0f;
            set { }
        }

        float IMyAudio.VolumeGame { get; set; }

        float IMyAudio.VolumeVoiceChat { get; set; }

        bool IMyAudio.GameSoundIsPaused => true;

        bool IMyAudio.Mute
        {
            get => true;
            set { }
        }

        bool IMyAudio.MusicAllowed
        {
            get => false;
            set { }
        }

        bool IMyAudio.EnableVoiceChat
        {
            get => false;
            set { }
        }

        bool IMyAudio.UseSameSoundLimiter
        {
            get => false;
            set { }
        }

        bool IMyAudio.EnableReverb
        {
            get => false;
            set { }
        }

        bool IMyAudio.EnableDoppler
        {
            get => false;
            set { }
        }

        /// <inheritdoc />
        public bool CacheLoaded
        {
            get => false;
            set { }
        }

        bool IMyAudio.UseVolumeLimiter
        {
            get => false;
            set { }
        }

        void IMyAudio.SetSameSoundLimiter() { }

        void IMyAudio.EnableMasterLimiter(bool e) { }

        void IMyAudio.ChangeGlobalVolume(float level, float time) { }

        event Action<bool> IMyAudio.VoiceChatEnabled
        {
            add { }
            remove { }
        }

        bool IMyAudio.IsValidTransitionCategory(MyStringId transitionCategory, MyStringId musicCategory) => false;

        List<MyStringId>? IMyAudio.GetCategories() => null;

        MySoundData? IMyAudio.GetCue(MyCueId cue) => null;

        Dictionary<MyStringId, List<MyCueId>>? IMyAudio.GetAllMusicCues() => null;

        void IMyAudio.PauseGameSounds() { }

        void IMyAudio.ResumeGameSounds() { }

        void IMyAudio.PlayMusic(MyMusicTrack? track, int priorityForRandom) { }

        IMySourceVoice? IMyAudio.PlayMusicCue(MyCueId musicCue, bool overrideMusicAllowed) => null;

        void IMyAudio.StopMusic() { }

        void IMyAudio.MuteHud(bool mute) { }

        bool IMyAudio.HasAnyTransition() => false;

        void IMyAudio.LoadData(MyAudioInitParams initParams, ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects) { }
        void IMyAudio.UnloadData() { }

        void IMyAudio.ReloadData() { }

        void IMyAudio.ReloadData(ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects) { }

        void IMyAudio.Update(int stepSizeInMS, Vector3 listenerPosition, Vector3 listenerUp, Vector3 listenerFront, Vector3 listenerVelocity) { }

        float IMyAudio.SemitonesToFrequencyRatio(float semitones) => 0.0f;

        int IMyAudio.GetUpdating3DSoundsCount() => 0;

        int IMyAudio.GetSoundInstancesTotal2D() => 0;

        int IMyAudio.GetSoundInstancesTotal3D() => 0;

        void IMyAudio.StopUpdatingAll3DCues() { }

        bool IMyAudio.SourceIsCloseEnoughToPlaySound(Vector3 position, MyCueId cueId, float? customMaxDistance) => false;

        void IMyAudio.WriteDebugInfo(StringBuilder sb) { }

        bool IMyAudio.IsLoopable(MyCueId cueId) => false;

        bool IMyAudio.ApplyTransition(MyStringId transitionEnum, int priority, MyStringId? category, bool loop) => false;

        IMySourceVoice? IMyAudio.PlaySound(MyCueId cue, IMy3DSoundEmitter? source, MySoundDimensions type, bool skipIntro, bool skipToEnd) => null;

        IMySourceVoice? IMyAudio.GetSound(MyCueId cue, IMy3DSoundEmitter? source, MySoundDimensions type) => null;


        ListReader<IMy3DSoundEmitter>? IMyAudio.Get3DSounds() => null;

        IMyAudioEffect? IMyAudio.ApplyEffect(IMySourceVoice input, MyStringHash effect, MyCueId[]? cueIds, float? duration, bool musicEffect) => null;

        IMySourceVoice? IMyAudio.GetSound(IMy3DSoundEmitter source, MySoundDimensions dimension) => null;

        public Vector3 GetListenerPosition() => Vector3.Zero;

        public void ClearSounds() { }

        public void EnumerateLastSounds(Action<StringBuilder, bool> a) { }

        public void DisposeCache() { }

        public void Preload(string soundFile) { }

        public bool CanPlay => false;
    }
}
