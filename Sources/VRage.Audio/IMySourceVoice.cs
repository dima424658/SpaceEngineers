﻿namespace VRage.Audio
{
    public interface IMySourceVoice
    {
        bool IsValid { get; }

        Action<IMySourceVoice>? StoppedPlaying { get; set; }

        bool IsPlaying { get; }

        float FrequencyRatio { get; set; }

        bool IsLoopable { get; }

        MyCueId CueEnum { get; }

        void Start(bool skipIntro, bool skipToEnd = false);

        void Stop(bool force = false);

        void StartBuffered();

        void SubmitBuffer(byte[] buffer);

        bool IsBuffered { get; }

        void Pause();

        bool IsPaused { get; }

        void Resume();

        void SetVolume(float value);

        float Volume { get; }

        float VolumeMultiplier { get; set; }

        void Destroy();
    }
}
