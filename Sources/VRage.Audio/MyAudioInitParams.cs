namespace VRage.Audio
{
    public struct MyAudioInitParams
    {
        public IMyAudio Instance;
        public bool SimulateNoSoundCard;
        public bool DisablePooling;
        public bool CacheLoaded;
        public MySoundErrorDelegate OnSoundError;
    }
}
