using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public interface IMyAudio
    {
        Dictionary<MyCueId, MySoundData>.ValueCollection? CueDefinitions { get; }

        List<MyStringId>? GetCategories();

        MySoundData? GetCue(MyCueId cue);

        Dictionary<MyStringId, List<MyCueId>>? GetAllMusicCues();

        MySoundData? SoloCue { get; set; }

        bool ApplyReverb { get; set; }

        void SetReverbParameters(float diffusion, float roomSize);

        float VolumeMusic { get; set; }

        float VolumeHud { get; set; }

        float VolumeGame { get; set; }

        float VolumeVoiceChat { get; set; }

        void PauseGameSounds();

        void ResumeGameSounds();

        bool Mute { get; set; }

        bool MusicAllowed { get; set; }

        bool GameSoundIsPaused { get; }

        bool EnableVoiceChat { get; set; }

        bool UseVolumeLimiter { get; set; }

        bool UseSameSoundLimiter { get; set; }

        bool EnableReverb { get; set; }

        int SampleRate { get; }

        bool EnableDoppler { get; set; }

        bool CacheLoaded { get; set; }

        void SetSameSoundLimiter();

        void EnableMasterLimiter(bool enable);

        void ChangeGlobalVolume(float level, float time);

        event Action<bool> VoiceChatEnabled;

        void PlayMusic(MyMusicTrack? track = null, int priorityForRandom = 0);

        IMySourceVoice? PlayMusicCue(MyCueId musicCue, bool overrideMusicAllowed);

        void StopMusic();

        void MuteHud(bool mute);

        bool HasAnyTransition();

        bool IsValidTransitionCategory(MyStringId transitionCategory, MyStringId musicCategory);

        void LoadData(MyAudioInitParams initParams, ListReader<MySoundData> cues, ListReader<MyAudioEffect> effects);

        void UnloadData();

        void ReloadData();

        void ReloadData(ListReader<MySoundData> cues, ListReader<MyAudioEffect> effects);

        void Update(int stepSizeInMS, Vector3 listenerPosition, Vector3 listenerUp, Vector3 listenerFront, Vector3 listenerVelocity);

        IMySourceVoice? PlaySound(MyCueId cueId, IMy3DSoundEmitter? source = null, MySoundDimensions type = MySoundDimensions.D2, bool skipIntro = false, bool skipToEnd = false);

        IMySourceVoice? GetSound(MyCueId cueId, IMy3DSoundEmitter? source = null, MySoundDimensions type = MySoundDimensions.D2);

        IMySourceVoice? GetSound(IMy3DSoundEmitter source, MySoundDimensions dimension);

        float SemitonesToFrequencyRatio(float semitones);

        int GetUpdating3DSoundsCount();

        int GetSoundInstancesTotal2D();

        int GetSoundInstancesTotal3D();

        void StopUpdatingAll3DCues();

        bool SourceIsCloseEnoughToPlaySound(Vector3 position, MyCueId cueId, float? customMaxDistance = 0.0f);

        bool IsLoopable(MyCueId cueId);

        bool ApplyTransition(MyStringId transitionEnum, int priority = 0, MyStringId? category = null, bool loop = true);

        void WriteDebugInfo(StringBuilder sb);

        ListReader<IMy3DSoundEmitter>? Get3DSounds();

        /// <summary>Creates effect on input emitter</summary>
        /// <param name="input">Emitter to work with</param>
        /// <param name="effect"></param>
        /// <param name="cueIds">additional cues if effect mixes them (ie. crossfade)</param>
        /// <param name="duration"></param>
        /// <param name="musicEffect"></param>
        /// <returns>effect output sound</returns>
        IMyAudioEffect? ApplyEffect(IMySourceVoice input, MyStringHash effect, MyCueId[]? cueIds = null, float? duration = null, bool musicEffect = false);

        Vector3 GetListenerPosition();

        void ClearSounds();

        void EnumerateLastSounds(Action<StringBuilder, bool> a);

        void DisposeCache();

        void Preload(string soundFile);

        bool CanPlay { get; }
    }
}
