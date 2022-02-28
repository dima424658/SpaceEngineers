using SharpDX;
using SharpDX.Mathematics.Interop;
using SharpDX.Multimedia;
using SharpDX.X3DAudio;
using SharpDX.XAPO;
using SharpDX.XAPO.Fx;
using SharpDX.XAudio2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace VRage.Audio
{
    public class MyXAudio2 : IMyAudio
    {
        private static readonly object lockObject = new object();
        private VoiceSendDescriptor[] m_gameAudioVoiceDesc;
        private VoiceSendDescriptor[] m_musicAudioVoiceDesc;
        private VoiceSendDescriptor[] m_hudAudioVoiceDesc;
        private MyAudioInitParams m_initParams;
        private SharpDX.XAudio2.XAudio2 m_audioEngine;
        private DeviceDetails m_deviceDetails;
        private MasteringVoice m_masterVoice;
        private SubmixVoice m_gameAudioVoice;
        private SubmixVoice m_musicAudioVoice;
        private SubmixVoice m_hudAudioVoice;
        private MyCueBank m_cueBank;
        private MyEffectBank m_effectBank;
        private SharpDX.X3DAudio.X3DAudio m_x3dAudio;
        private bool m_canPlay;
        private bool m_loading;
        private float m_volumeHud;
        private float m_volumeDefault;
        private float m_volumeMusic;
        private float m_volumeVoiceChat;
        private bool m_mute;
        private bool m_musicAllowed;
        private bool m_musicOn;
        private bool m_gameSoundsOn;
        private bool m_voiceChatEnabled;
        private MyMusicState m_musicState;
        private bool m_loopMusic;
        private MySourceVoice m_musicCue;
        private CalculateFlags m_calculateFlags;
        internal static MyXAudio2 Instance;
        private SortedList<int, MyXAudio2.MyMusicTransition> m_nextTransitions = new SortedList<int, MyXAudio2.MyMusicTransition>();
        private MyXAudio2.MyMusicTransition? m_currentTransition;
        private bool m_transitionForward;
        private float m_volumeAtTransitionStart;
        private int m_timeFromTransitionStart;
        private const int TRANSITION_TIME = 1000;
        private Listener m_listener;
        private Emitter m_helperEmitter;
        private List<IMy3DSoundEmitter> m_3Dsounds;
        private bool m_canUpdate3dSounds = true;
        private int m_soundInstancesTotal2D;
        private int m_soundInstancesTotal3D;
        private SharpDX.XAudio2.Fx.Reverb m_reverb;
        private bool m_applyReverb;
        private float m_globalVolumeLevel = 1f;
        private float m_globalVolumeTarget = 1f;
        private float m_globalVolumeIncrement = 1f;
        private bool m_globalVolumeRaising = true;
        private bool m_globalVolumeChanging;
        private bool m_useVolumeLimiter;
        private bool m_useSameSoundLimiter;
        private bool m_enableReverb;
        private bool m_enableDoppler = true;
        private bool m_reverbSet;
        private bool m_soundLimiterReady;
        private bool m_soundLimiterSet;
        private Thread m_updateInProgress;
        private volatile bool m_deviceLost;
        private int m_lastDeviceCount;
        private ListReader<MySoundData> m_sounds;
        private ListReader<MyAudioEffect> m_effects;
        private int m_deviceNumber;
        private readonly IMyPlatformAudio m_audioPlatform;
        private static MyStringId NO_RANDOM = MyStringId.GetOrCompute("NoRandom");
        private MyTimeSpan m_nextDeviceCountCheck;
        private static readonly float[] m_outputMatrixMono = new float[8]
        {
      0.5f,
      0.5f,
      0.0f,
      0.0f,
      0.4f,
      0.4f,
      0.0f,
      0.0f
        };
        private static readonly float[] m_outputMatrixStereo = new float[16]
        {
      1f,
      0.0f,
      0.0f,
      1f,
      0.0f,
      0.0f,
      0.0f,
      0.0f,
      0.8f,
      0.0f,
      0.0f,
      0.8f,
      0.0f,
      0.0f,
      0.0f,
      0.0f
        };
        private MyConcurrentQueue<MySourceVoice> m_voicesForStopPlayingCallback = new MyConcurrentQueue<MySourceVoice>();

        private event MyXAudio2.VolumeChangeHandler OnSetVolumeHud;

        private event MyXAudio2.VolumeChangeHandler OnSetVolumeGame;

        private event MyXAudio2.VolumeChangeHandler OnSetVolumeMusic;

        Dictionary<MyCueId, MySoundData>.ValueCollection IMyAudio.CueDefinitions => !this.m_canPlay ? (Dictionary<MyCueId, MySoundData>.ValueCollection)null : this.m_cueBank.CueDefinitions;

        List<MyStringId> IMyAudio.GetCategories() => !this.m_canPlay ? (List<MyStringId>)null : this.m_cueBank.GetCategories();

        MySoundData IMyAudio.GetCue(MyCueId cueId) => !this.m_canPlay ? (MySoundData)null : this.m_cueBank.GetCue(cueId);

        Dictionary<MyStringId, List<MyCueId>> IMyAudio.GetAllMusicCues() => this.m_cueBank == null ? (Dictionary<MyStringId, List<MyCueId>>)null : this.m_cueBank.GetMusicCues();

        public int SampleRate => this.m_masterVoice == null || this.m_deviceLost ? 0 : this.m_masterVoice.VoiceDetails.InputSampleRate;

        public MySoundData SoloCue { get; set; }

        public bool GameSoundIsPaused { get; private set; }

        bool IMyAudio.UseVolumeLimiter
        {
            get => this.m_useVolumeLimiter;
            set => this.m_useVolumeLimiter = value;
        }

        bool IMyAudio.UseSameSoundLimiter
        {
            get => this.m_useSameSoundLimiter;
            set => this.m_useSameSoundLimiter = value;
        }

        bool IMyAudio.EnableReverb
        {
            get => this.m_enableReverb;
            set
            {
                this.m_enableReverb = value;
                if (!(!this.m_reverbSet & value) || this.m_masterVoice == null)
                    return;
                if (this.m_masterVoice.VoiceDetails.InputSampleRate > MyAudio.MAX_SAMPLE_RATE)
                    return;
                try
                {
                    this.m_reverb = new SharpDX.XAudio2.Fx.Reverb(this.m_audioEngine);
                    this.m_gameAudioVoice.SetEffectChain(new EffectDescriptor((AudioProcessor)this.m_reverb, this.m_masterVoice.VoiceDetails.InputChannelCount));
                    this.m_gameAudioVoice.DisableEffect(0);
                    this.m_reverbSet = true;
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine("Failed to enable Reverb" + ex.ToString());
                }
            }
        }

        bool IMyAudio.EnableDoppler
        {
            get => this.m_enableDoppler;
            set => this.m_enableDoppler = value;
        }

        public bool CacheLoaded
        {
            get => this.m_initParams.CacheLoaded;
            set => this.m_initParams.CacheLoaded = value;
        }

        public bool CanPlay => this.m_canPlay;

        public IMyPlatformAudio AudioPlatform => this.m_audioPlatform;

        public MyXAudio2(IMyPlatformAudio audioPlatform) => this.m_audioPlatform = audioPlatform;

        private void Init()
        {
            MyXAudio2.Instance = this;
            this.StartEngine();
            this.CreateX3DAudio();
        }

        private int GetDeviceCount() => this.m_audioPlatform.DeviceCount;

        private DeviceDetails GetDeviceDetails(int index) => this.m_audioPlatform.GetDeviceDetails(index, false);

        private void StartEngine()
        {
            if (this.m_audioEngine != null)
            {
                this.DisposeVoices();
                this.m_audioEngine.CriticalError -= new EventHandler<ErrorEventArgs>(this.m_audioEngine_CriticalError);
                this.m_audioPlatform.DisposeAudioEngine();
            }
            this.m_audioEngine = this.m_audioPlatform.InitAudioEngine();
            this.m_audioEngine.CriticalError += new EventHandler<ErrorEventArgs>(this.m_audioEngine_CriticalError);
            this.m_lastDeviceCount = this.GetDeviceCount();
            this.m_deviceNumber = 0;
            bool flag = false;
            do
            {
                try
                {
                    this.m_deviceDetails = this.GetDeviceDetails(this.m_deviceNumber);
                    if (this.m_deviceDetails.Role == DeviceRole.DefaultCommunicationsDevice)
                    {
                        ++this.m_deviceNumber;
                        if (this.m_deviceNumber == this.GetDeviceCount())
                        {
                            --this.m_deviceNumber;
                            goto label_13;
                        }
                    }
                    else
                        goto label_13;
                }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLine(string.Format("Failed to get device details:"));
                    MyLog.Default.WriteLine(ex.ToString());
                    flag = true;
                }
            }
            while (!flag);
            try
            {
                MyLog.Default.WriteLine(string.Format("Device no.: {0}\n\tdevice count: {1}", (object)this.m_deviceNumber, (object)this.GetDeviceCount()), LoggingOptions.AUDIO);
            }
            catch (Exception ex)
            {
            }
            this.m_deviceNumber = 0;
            this.m_deviceDetails = this.GetDeviceDetails(this.m_deviceNumber);
        label_13:
            this.m_masterVoice = this.m_audioPlatform.CreateMasteringVoice(this.m_deviceNumber);
            if (this.m_useVolumeLimiter)
            {
                MasteringLimiter effect = new MasteringLimiter(this.m_audioEngine);
                effect.Parameter = effect.Parameter with
                {
                    Loudness = 0
                };
                this.m_masterVoice.SetEffectChain(new EffectDescriptor((AudioProcessor)effect));
                this.m_soundLimiterReady = true;
                this.m_masterVoice.DisableEffect(0);
            }
            this.m_calculateFlags = CalculateFlags.Matrix | CalculateFlags.Doppler;
            if ((this.m_deviceDetails.OutputFormat.ChannelMask & Speakers.LowFrequency) != Speakers.None)
                this.m_calculateFlags |= CalculateFlags.RedirectToLfe;
            VoiceDetails voiceDetails = this.m_masterVoice.VoiceDetails;
            this.m_gameAudioVoice = new SubmixVoice(this.m_audioEngine, voiceDetails.InputChannelCount, voiceDetails.InputSampleRate);
            this.m_musicAudioVoice = new SubmixVoice(this.m_audioEngine, voiceDetails.InputChannelCount, voiceDetails.InputSampleRate);
            this.m_hudAudioVoice = new SubmixVoice(this.m_audioEngine, voiceDetails.InputChannelCount, voiceDetails.InputSampleRate);
            this.m_gameAudioVoiceDesc = new VoiceSendDescriptor[1]
            {
        new VoiceSendDescriptor((Voice) this.m_gameAudioVoice)
            };
            this.m_musicAudioVoiceDesc = new VoiceSendDescriptor[1]
            {
        new VoiceSendDescriptor((Voice) this.m_musicAudioVoice)
            };
            this.m_hudAudioVoiceDesc = new VoiceSendDescriptor[1]
            {
        new VoiceSendDescriptor((Voice) this.m_hudAudioVoice)
            };
            if (!this.m_mute)
                return;
            this.m_gameAudioVoice.SetVolume(0.0f);
            this.m_musicAudioVoice.SetVolume(0.0f);
        }

        public void SetReverbParameters(float diffusion, float roomSize)
        {
        }

        public void ChangeGlobalVolume(float level, float time)
        {
            level = MyMath.Clamp(level, 0.0f, 1f);
            this.m_globalVolumeChanging = false;
            if ((double)level == (double)this.m_globalVolumeLevel)
                return;
            if ((double)time <= 0.0)
            {
                this.m_globalVolumeLevel = level;
                if (this.m_musicAudioVoice != null && !this.m_musicAudioVoice.IsDisposed)
                    this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * level);
                if (this.m_hudAudioVoice != null && !this.m_hudAudioVoice.IsDisposed)
                    this.m_hudAudioVoice.SetVolume(this.m_volumeHud * level);
                if (this.m_gameAudioVoice == null || this.m_gameAudioVoice.IsDisposed)
                    return;
                this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * level);
            }
            else
            {
                this.m_globalVolumeChanging = true;
                this.m_globalVolumeIncrement = (float)(((double)level - (double)this.m_globalVolumeLevel) / 60.0) / time;
                this.m_globalVolumeTarget = level;
                this.m_globalVolumeRaising = (double)level > (double)this.m_globalVolumeLevel;
            }
        }

        private void GlobalVolumeUpdate()
        {
            this.m_globalVolumeLevel += this.m_globalVolumeIncrement;
            if (this.m_globalVolumeRaising && (double)this.m_globalVolumeLevel >= (double)this.m_globalVolumeTarget || !this.m_globalVolumeRaising && (double)this.m_globalVolumeLevel <= (double)this.m_globalVolumeTarget)
            {
                this.m_globalVolumeLevel = this.m_globalVolumeTarget;
                this.m_globalVolumeChanging = false;
            }
            if (this.m_musicAudioVoice != null)
                this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
            if (this.m_hudAudioVoice != null)
                this.m_hudAudioVoice.SetVolume(this.m_volumeHud * this.m_globalVolumeLevel);
            if (this.m_gameAudioVoice == null)
                return;
            this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * this.m_globalVolumeLevel);
        }

        public void EnableMasterLimiter(bool enable)
        {
            if (!this.m_useVolumeLimiter || !this.m_soundLimiterReady || enable == this.m_soundLimiterSet)
                return;
            if (enable)
                this.m_masterVoice.EnableEffect(0);
            else
                this.m_masterVoice.DisableEffect(0);
            this.m_soundLimiterSet = enable;
        }

        private void m_audioEngine_CriticalError(object sender, ErrorEventArgs e)
        {
            this.m_cueBank.SetAudioEngine((SharpDX.XAudio2.XAudio2)null);
            if (e.ErrorCode.Code == -2004353023)
                MyLog.Default.WriteLine("Audio device removed");
            else
                MyLog.Default.WriteLine("Audio error: " + (object)e.ErrorCode);
            this.m_deviceLost = true;
        }

        private void CreateX3DAudio()
        {
            if (this.m_audioEngine == null)
                return;
            this.m_x3dAudio = new SharpDX.X3DAudio.X3DAudio(this.m_deviceDetails.OutputFormat.ChannelMask);
            string str = this.m_deviceDetails.DisplayName;
            int length = str.IndexOf(char.MinValue);
            if (length != -1)
                str = str.Substring(0, length);
            MyLog.Default.WriteLine(string.Format("MyAudio.CreateX3DAudio - Device: {0} - Channel #: {1} - Sample rate: {2}", (object)str, (object)this.m_deviceDetails.OutputFormat.Channels, (object)this.SampleRate));
        }

        private void DisposeVoices()
        {
            this.m_hudAudioVoice?.Dispose();
            this.m_musicAudioVoice?.Dispose();
            this.m_gameAudioVoice?.Dispose();
            this.m_masterVoice?.Dispose();
        }

        private void CheckIfDeviceChanged()
        {
            MyTimeSpan myTimeSpan = new MyTimeSpan(Stopwatch.GetTimestamp());
            if (myTimeSpan > this.m_nextDeviceCountCheck)
            {
                this.m_nextDeviceCountCheck = myTimeSpan + MyTimeSpan.FromSeconds(1.0);
                try
                {
                    this.m_deviceLost = this.m_lastDeviceCount != this.GetDeviceCount();
                }
                catch (SharpDXException ex)
                {
                    this.m_deviceLost = true;
                }
            }
            if (!this.m_deviceLost)
                return;
            try
            {
                this.Init();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device ID: " + this.m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device name: " + this.m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device role: " + (object)this.m_deviceDetails.Role, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Output format: " + (object)this.m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);
                this.m_canPlay = false;
                return;
            }
            this.m_deviceLost = false;
            if (this.m_initParams.SimulateNoSoundCard)
                this.m_canPlay = false;
            if (!this.m_canPlay)
                return;
            if (this.m_cueBank != null)
                this.m_cueBank.SetAudioEngine(this.m_audioEngine, this.m_gameAudioVoiceDesc, this.m_hudAudioVoiceDesc, this.m_musicAudioVoiceDesc);
            this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * this.m_globalVolumeLevel);
            this.m_hudAudioVoice.SetVolume(this.m_volumeHud * this.m_globalVolumeLevel);
            this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
            lock (this.m_3Dsounds)
                this.m_3Dsounds.Clear();
            if (this.m_musicCue == null || !this.m_musicCue.IsPlaying)
                return;
            this.m_musicCue = this.PlaySound(this.m_musicCue.CueEnum, (IMy3DSoundEmitter)null, MySoundDimensions.D2, false, false, true);
            if (this.m_musicCue != null)
                this.m_musicCue.SetOutputVoices(this.m_musicAudioVoiceDesc);
            this.UpdateMusic(0);
        }

        public void LoadData(
          MyAudioInitParams initParams,
          ListReader<MySoundData> sounds,
          ListReader<MyAudioEffect> effects)
        {
            MyLog.Default.WriteLine("MyAudio.LoadData - START");
            MyLog.Default.IncreaseIndent();
            this.m_initParams = initParams;
            this.m_sounds = sounds;
            this.m_effects = effects;
            this.m_canPlay = true;
            try
            {
                this.Init();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine("Exception during loading audio engine. Game continues, but without sound. Details: " + ex.ToString(), LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device ID: " + this.m_deviceDetails.DeviceID, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device name: " + this.m_deviceDetails.DisplayName, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Device role: " + (object)this.m_deviceDetails.Role, LoggingOptions.AUDIO);
                MyLog.Default.WriteLine("Output format: " + (object)this.m_deviceDetails.OutputFormat, LoggingOptions.AUDIO);
                this.m_canPlay = false;
            }
            if (this.m_initParams.SimulateNoSoundCard)
                this.m_canPlay = false;
            if (this.m_canPlay)
            {
                this.m_cueBank = new MyCueBank(this.m_audioEngine, sounds, this.m_gameAudioVoiceDesc, this.m_hudAudioVoiceDesc, this.m_musicAudioVoiceDesc, initParams.CacheLoaded);
                this.m_cueBank.UseSameSoundLimiter = this.m_useSameSoundLimiter;
                this.m_cueBank.SetSameSoundLimiter();
                this.m_cueBank.DisablePooling = initParams.DisablePooling;
                this.m_effectBank = new MyEffectBank(effects, this.m_audioEngine);
                this.m_3Dsounds = new List<IMy3DSoundEmitter>();
                this.m_listener = new Listener();
                this.m_listener.SetDefaultValues();
                this.m_helperEmitter = new Emitter();
                this.m_helperEmitter.SetDefaultValues();
                this.m_musicOn = true;
                this.m_gameSoundsOn = true;
                this.m_musicAllowed = true;
                if (this.m_musicCue != null && this.m_musicCue.IsPlaying)
                {
                    this.m_musicCue = this.PlaySound(this.m_musicCue.CueEnum, (IMy3DSoundEmitter)null, MySoundDimensions.D2, false, false, true);
                    if (this.m_musicCue != null)
                    {
                        this.m_musicCue.SetOutputVoices(this.m_musicAudioVoiceDesc);
                        this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
                    }
                    this.UpdateMusic(0);
                }
                else
                    this.m_musicState = MyMusicState.Stopped;
                this.m_loopMusic = true;
                this.m_transitionForward = false;
                this.m_timeFromTransitionStart = 0;
                this.m_soundInstancesTotal2D = 0;
                this.m_soundInstancesTotal3D = 0;
            }
            MyLog.Default.DecreaseIndent();
            MyLog.Default.WriteLine("MyAudio.LoadData - END");
        }

        public void SetSameSoundLimiter()
        {
            if (this.m_cueBank == null)
                return;
            this.m_cueBank.UseSameSoundLimiter = this.m_useSameSoundLimiter;
            this.m_cueBank.SetSameSoundLimiter();
        }

        public void UnloadData()
        {
            MyLog.Default.WriteLine("MyAudio.UnloadData - START");
            if (this.m_3Dsounds != null)
            {
                lock (this.m_3Dsounds)
                    this.m_3Dsounds.Clear();
            }
            if (this.m_canPlay)
            {
                this.m_audioEngine.StopEngine();
                this.m_cueBank?.Dispose();
            }
            this.SoloCue = (MySoundData)null;
            this.DisposeVoices();
            if (this.m_audioEngine != null)
            {
                this.m_audioEngine.CriticalError -= new EventHandler<ErrorEventArgs>(this.m_audioEngine_CriticalError);
                this.m_audioPlatform.DisposeAudioEngine();
                this.m_audioEngine = (SharpDX.XAudio2.XAudio2)null;
                this.m_cueBank = (MyCueBank)null;
                this.m_masterVoice = (MasteringVoice)null;
                this.m_hudAudioVoice = (SubmixVoice)null;
                this.m_gameAudioVoice = (SubmixVoice)null;
                this.m_musicAudioVoice = (SubmixVoice)null;
            }
            this.m_canPlay = false;
            this.m_reverbSet = false;
            MyLog.Default.WriteLine("MyAudio.UnloadData - END");
            MyXAudio2.Instance = (MyXAudio2)null;
        }

        public void ClearSounds()
        {
            if (this.m_cueBank == null)
                return;
            this.m_cueBank.ClearSounds();
        }

        public void ReloadData()
        {
            this.UnloadData();
            this.LoadData(this.m_initParams, this.m_sounds, this.m_effects);
        }

        public void ReloadData(ListReader<MySoundData> sounds, ListReader<MyAudioEffect> effects)
        {
            lock (MyXAudio2.lockObject)
                this.m_loading = true;
            this.UnloadData();
            this.LoadData(this.m_initParams, sounds, effects);
            lock (MyXAudio2.lockObject)
                this.m_loading = false;
        }

        public bool ApplyReverb
        {
            get => this.m_canPlay && this.m_enableReverb && this.m_cueBank != null && this.m_masterVoice.VoiceDetails.InputSampleRate <= MyAudio.MAX_SAMPLE_RATE && this.m_applyReverb;
            set
            {
                if (!this.m_canPlay || !this.m_reverbSet || this.m_deviceLost || this.m_cueBank == null || !this.m_enableReverb && !this.m_applyReverb)
                    return;
                if (this.m_gameAudioVoice != null)
                {
                    if (value)
                    {
                        this.m_gameAudioVoice.EnableEffect(0);
                    }
                    else
                    {
                        RawBool enabledRef;
                        this.m_gameAudioVoice.IsEffectEnabled(0, out enabledRef);
                        if ((bool)enabledRef)
                            this.m_gameAudioVoice.DisableEffect(0);
                    }
                }
                this.m_cueBank.ApplyReverb = value;
                this.m_applyReverb = value;
            }
        }

        public float VolumeMusic
        {
            get => !this.m_canPlay || !this.m_musicOn ? 0.0f : this.m_volumeMusic;
            set
            {
                if (!this.m_canPlay || !this.m_musicOn || this.m_deviceLost)
                    return;
                this.m_volumeMusic = MathHelper.Clamp(value, 0.0f, 1f);
                this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
                if (this.OnSetVolumeMusic == null)
                    return;
                this.OnSetVolumeMusic(this.m_volumeMusic);
            }
        }

        public float VolumeHud
        {
            get => !this.m_canPlay ? 0.0f : this.m_volumeHud;
            set
            {
                if (!this.m_canPlay || this.m_deviceLost)
                    return;
                this.m_volumeHud = MathHelper.Clamp(value, 0.0f, 1f);
                this.m_hudAudioVoice.SetVolume(this.m_volumeHud * this.m_globalVolumeLevel);
                if (this.OnSetVolumeHud == null)
                    return;
                this.OnSetVolumeHud(this.m_volumeHud);
            }
        }

        public float VolumeGame
        {
            get => !this.m_canPlay || !this.m_gameSoundsOn ? 0.0f : this.m_volumeDefault;
            set
            {
                if (!this.m_canPlay || !this.m_gameSoundsOn || this.m_deviceLost)
                    return;
                this.m_volumeDefault = MathHelper.Clamp(value, 0.0f, 1f);
                this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * this.m_globalVolumeLevel);
                if (this.OnSetVolumeGame == null)
                    return;
                this.OnSetVolumeGame(this.m_volumeDefault);
            }
        }

        public float VolumeVoiceChat
        {
            get => this.m_volumeVoiceChat;
            set => this.m_volumeVoiceChat = MathHelper.Clamp(value, 0.0f, 5f);
        }

        public bool EnableVoiceChat
        {
            get => this.m_canPlay && this.m_voiceChatEnabled;
            set
            {
                if (this.m_voiceChatEnabled == value)
                    return;
                this.m_voiceChatEnabled = value;
                if (this.VoiceChatEnabled == null)
                    return;
                this.VoiceChatEnabled(this.m_voiceChatEnabled);
            }
        }

        public event Action<bool> VoiceChatEnabled;

        public void PauseGameSounds()
        {
            if (!this.m_canPlay)
                return;
            this.GameSoundIsPaused = true;
            this.m_gameAudioVoice.SetVolume(0.0f);
            this.m_canUpdate3dSounds = false;
            if (this.m_musicCue == null)
                return;
            this.m_musicCue.VolumeMultiplier = 0.0f;
        }

        public void ResumeGameSounds()
        {
            if (!this.m_canPlay)
                return;
            this.GameSoundIsPaused = false;
            if (!this.Mute)
                this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * this.m_globalVolumeLevel);
            this.m_canUpdate3dSounds = true;
            if (this.m_musicCue == null)
                return;
            this.m_musicCue.VolumeMultiplier = 1f;
        }

        public bool Mute
        {
            get => this.m_mute;
            set
            {
                if (this.m_mute == value)
                    return;
                this.m_mute = value;
                if (this.m_mute)
                {
                    if (!this.m_canPlay)
                        return;
                    this.m_gameAudioVoice.SetVolume(0.0f);
                    this.m_musicAudioVoice.SetVolume(0.0f);
                }
                else
                {
                    if (!this.m_canPlay)
                        return;
                    if (!this.GameSoundIsPaused)
                        this.m_gameAudioVoice.SetVolume(this.m_volumeDefault * this.m_globalVolumeLevel);
                    this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
                    this.m_hudAudioVoice.SetVolume(this.m_volumeHud * this.m_globalVolumeLevel);
                }
            }
        }

        public bool MusicAllowed
        {
            get => this.m_musicAllowed;
            set => this.m_musicAllowed = value;
        }

        public bool IsValidTransitionCategory(MyStringId transitionCategory, MyStringId musicCategory) => this.m_canPlay && this.m_cueBank.IsValidTransitionCategory(transitionCategory, musicCategory);

        public void PlayMusic(MyMusicTrack? track = null, int priorityForRandom = 0)
        {
            if (!this.m_canPlay || !this.m_musicAllowed)
                return;
            bool flag = false;
            if (track.HasValue)
            {
                if (this.HasAnyTransition())
                    this.m_nextTransitions.Clear();
                if (!this.m_cueBank.IsValidTransitionCategory(track.Value.TransitionCategory, track.Value.MusicCategory))
                    flag = true;
                else
                    this.ApplyTransition(track.Value.TransitionCategory, 1, new MyStringId?(track.Value.MusicCategory), false);
            }
            else if (this.m_musicState == MyMusicState.Stopped && !this.HasAnyTransition())
                flag = true;
            if (!flag)
                return;
            MyStringId? randomTransitionEnum = this.GetRandomTransitionEnum();
            if (!randomTransitionEnum.HasValue)
                return;
            this.ApplyTransition(randomTransitionEnum.Value, priorityForRandom, new MyStringId?(), false);
        }

        public IMySourceVoice PlayMusicCue(MyCueId musicCue, bool overrideMusicAllowed = false)
        {
            if (!this.m_canPlay || !this.m_musicAllowed && !overrideMusicAllowed)
                return (IMySourceVoice)null;
            this.m_musicCue = this.PlaySound(musicCue, (IMy3DSoundEmitter)null, MySoundDimensions.D2, false, false, true);
            if (this.m_musicCue != null)
            {
                this.m_musicCue.SetOutputVoices(this.m_musicAudioVoiceDesc);
                this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
            }
            return (IMySourceVoice)this.m_musicCue;
        }

        public void StopMusic()
        {
            this.m_currentTransition = new MyXAudio2.MyMusicTransition?();
            this.m_nextTransitions.Clear();
            this.m_musicState = MyMusicState.Stopped;
            if (this.m_musicCue == null)
                return;
            try
            {
                this.m_musicCue.Stop(false);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
                if (this.m_audioEngine != null && !this.m_audioEngine.IsDisposed)
                    return;
                MyLog.Default.WriteLine("Audio engine disposed!", LoggingOptions.AUDIO);
            }
        }

        public void MuteHud(bool mute)
        {
            if (!this.m_canPlay)
                return;
            this.m_hudAudioVoice.SetVolume(mute ? 0.0f : this.m_volumeHud * this.m_globalVolumeLevel);
        }

        public bool HasAnyTransition() => this.m_nextTransitions.Count > 0;

        public void Update(
          int stepSizeInMS,
          Vector3 listenerPosition,
          Vector3 listenerUp,
          Vector3 listenerFront,
          Vector3 listenerVelocity)
        {
            lock (MyXAudio2.lockObject)
            {
                if (this.m_loading)
                    return;
            }
            if (this.m_canPlay)
                this.CheckIfDeviceChanged();
            if (this.Mute)
                return;
            this.m_updateInProgress = Thread.CurrentThread;
            try
            {
                if (this.m_canPlay)
                    this.m_cueBank?.Update();
                if (!this.m_canPlay)
                    return;
                this.m_listener.Position = new RawVector3();
                this.m_listener.OrientTop = new RawVector3(listenerUp.X, listenerUp.Y, listenerUp.Z);
                this.m_listener.OrientFront = new RawVector3(listenerFront.X, listenerFront.Y, listenerFront.Z);
                this.m_listener.Velocity = new RawVector3(listenerVelocity.X, listenerVelocity.Y, listenerVelocity.Z);
                this.FireCallbacks();
                this.UpdateMusic(stepSizeInMS);
                this.Update3DCuesPositions();
                this.m_effectBank.Update(stepSizeInMS);
                if (!this.m_globalVolumeChanging)
                    return;
                this.GlobalVolumeUpdate();
            }
            finally
            {
                this.m_updateInProgress = (Thread)null;
            }
        }

        private void FireCallbacks()
        {
            MySourceVoice instance;
            while (this.m_voicesForStopPlayingCallback.TryDequeue(out instance))
                instance.StoppedPlaying.InvokeIfNotNull<IMySourceVoice>((IMySourceVoice)instance);
        }

        private void UpdateMusic(int stepSizeinMS)
        {
            if (this.m_musicState == MyMusicState.Transition)
            {
                this.m_timeFromTransitionStart += stepSizeinMS;
                if (this.m_timeFromTransitionStart >= 1000)
                {
                    this.m_musicState = MyMusicState.Stopped;
                    if (this.m_musicCue != null && this.m_musicCue.IsPlaying)
                    {
                        this.m_musicCue.Stop(true);
                        this.m_musicCue = (MySourceVoice)null;
                    }
                }
                else if (this.m_musicCue != null && this.m_musicCue.IsPlaying)
                {
                    float volumeRef;
                    this.m_musicAudioVoice.GetVolume(out volumeRef);
                    if ((double)volumeRef > 0.0 && this.m_musicOn)
                        this.m_musicAudioVoice.SetVolume((float)(1.0 - (double)this.m_timeFromTransitionStart / 1000.0) * this.m_volumeAtTransitionStart * this.m_globalVolumeLevel);
                }
            }
            if (this.m_musicState == MyMusicState.Stopped)
            {
                MyXAudio2.MyMusicTransition? nextTransition = this.GetNextTransition();
                if (this.m_currentTransition.HasValue && this.m_nextTransitions.Count > 0 && nextTransition.HasValue && nextTransition.Value.Priority > this.m_currentTransition.Value.Priority)
                    this.m_nextTransitions[this.m_currentTransition.Value.Priority] = this.m_currentTransition.Value;
                this.m_currentTransition = nextTransition;
                if (this.m_currentTransition.HasValue)
                {
                    this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
                    this.PlayMusicByTransition(this.m_currentTransition.Value);
                    this.m_nextTransitions.Remove(this.m_currentTransition.Value.Priority);
                    this.m_musicState = MyMusicState.Playing;
                }
            }
            if (this.m_musicState != MyMusicState.Playing || this.m_musicCue != null && this.m_musicCue.IsPlaying)
                return;
            if (this.m_loopMusic && this.m_currentTransition.HasValue)
            {
                this.PlayMusicByTransition(this.m_currentTransition.Value);
            }
            else
            {
                this.m_currentTransition = new MyXAudio2.MyMusicTransition?();
                MyStringId? nullable = new MyStringId?(MyStringId.GetOrCompute("Default"));
                if (!nullable.HasValue)
                    return;
                this.ApplyTransition(nullable.Value, 0, new MyStringId?(), false);
            }
        }

        private MyStringId? GetRandomTransitionEnum()
        {
            if (this.m_cueBank == null)
                return new MyStringId?();
            MyStringId? randomTransitionEnum = this.m_cueBank.GetRandomTransitionEnum();
            while (true)
            {
                MyStringId? nullable = randomTransitionEnum;
                MyStringId noRandom = MyXAudio2.NO_RANDOM;
                if ((nullable.HasValue ? (nullable.HasValue ? (nullable.GetValueOrDefault() == noRandom ? 1 : 0) : 1) : 0) != 0)
                    randomTransitionEnum = this.m_cueBank.GetRandomTransitionEnum();
                else
                    break;
            }
            return randomTransitionEnum;
        }

        bool IMyAudio.ApplyTransition(
          MyStringId transitionEnum,
          int priority,
          MyStringId? category,
          bool loop)
        {
            return this.ApplyTransition(transitionEnum, priority, category, loop);
        }

        private bool ApplyTransition(
          MyStringId transitionEnum,
          int priority,
          MyStringId? category,
          bool loop)
        {
            if (!this.m_canPlay || !this.m_musicAllowed)
                return false;
            if (category.HasValue)
            {
                if (category.Value == MyStringId.NullOrEmpty)
                    category = new MyStringId?();
                else if (!this.m_cueBank.IsValidTransitionCategory(transitionEnum, category.Value))
                {
                    MyLog.Default.WriteLine(string.Format("Category {0} doesn't exist for this transition!", (object)category));
                    return false;
                }
            }
            MyStringId? nullable;
            if (this.m_currentTransition.HasValue && this.m_currentTransition.Value.Priority == priority && this.m_currentTransition.Value.TransitionEnum == transitionEnum)
            {
                if (category.HasValue)
                {
                    MyStringId category1 = this.m_currentTransition.Value.Category;
                    nullable = category;
                    if ((nullable.HasValue ? (category1 == nullable.GetValueOrDefault() ? 1 : 0) : 0) == 0)
                        goto label_13;
                }
                if (this.m_musicState != MyMusicState.Transition || this.m_transitionForward)
                    return false;
                this.m_musicState = MyMusicState.Playing;
                return true;
            }
        label_13:
            nullable = category;
            MyStringId category2 = nullable ?? this.m_cueBank.GetRandomTransitionCategory(ref transitionEnum, ref MyXAudio2.NO_RANDOM);
            this.m_nextTransitions[priority] = new MyXAudio2.MyMusicTransition(priority, transitionEnum, category2);
            if (this.m_currentTransition.HasValue && this.m_currentTransition.Value.Priority > priority)
                return false;
            this.m_loopMusic = loop;
            switch (this.m_musicState)
            {
                case MyMusicState.Stopped:
                case MyMusicState.Transition:
                    return true;
                case MyMusicState.Playing:
                    this.StartTransition(true);
                    goto case MyMusicState.Stopped;
                default:
                    throw new InvalidBranchException();
            }
        }

        private MyXAudio2.MyMusicTransition? GetNextTransition() => this.m_nextTransitions.Count > 0 ? new MyXAudio2.MyMusicTransition?(this.m_nextTransitions[this.m_nextTransitions.Keys[this.m_nextTransitions.Keys.Count - 1]]) : new MyXAudio2.MyMusicTransition?();

        private void StartTransition(bool forward)
        {
            this.m_transitionForward = forward;
            this.m_musicState = MyMusicState.Transition;
            this.m_timeFromTransitionStart = 0;
            this.m_volumeAtTransitionStart = this.m_volumeMusic;
        }

        internal void StopTransition(int priority)
        {
            if (this.m_nextTransitions.ContainsKey(priority))
                this.m_nextTransitions.Remove(priority);
            if (!this.m_currentTransition.HasValue || priority != this.m_currentTransition.Value.Priority || this.m_musicState == MyMusicState.Transition)
                return;
            this.StartTransition(false);
        }

        private void PlayMusicByTransition(MyXAudio2.MyMusicTransition transition)
        {
            if (this.m_cueBank == null || !this.m_musicAllowed)
                return;
            this.m_musicCue = this.PlaySound(this.m_cueBank.GetTransitionCue(transition.TransitionEnum, transition.Category), (IMy3DSoundEmitter)null, MySoundDimensions.D2, false, false, true);
            if (this.m_musicCue == null)
                return;
            this.m_musicCue.SetOutputVoices(this.m_musicAudioVoiceDesc);
            this.m_musicAudioVoice.SetVolume(this.m_volumeMusic * this.m_globalVolumeLevel);
        }

        private float VolumeVariation(MySoundData cue) => (float)((double)MyUtils.GetRandomFloat(-1f, 1f) * (double)cue.VolumeVariation * 0.0700000002980232);

        private float PitchVariation(MySoundData cue) => (float)((double)MyUtils.GetRandomFloat(-1f, 1f) * (double)cue.PitchVariation / 100.0);

        float IMyAudio.SemitonesToFrequencyRatio(float semitones) => this.SemitonesToFrequencyRatio(semitones);

        private float SemitonesToFrequencyRatio(float semitones) => SharpDX.XAudio2.XAudio2.SemitonesToFrequencyRatio(semitones);

        private void Add3DCueToUpdateList(IMy3DSoundEmitter source)
        {
            lock (this.m_3Dsounds)
            {
                if (this.m_3Dsounds.Contains(source))
                    return;
                this.m_3Dsounds.Add(source);
            }
        }

        public int GetUpdating3DSoundsCount() => this.m_3Dsounds == null ? 0 : this.m_3Dsounds.Count;

        public int GetSoundInstancesTotal2D() => this.m_soundInstancesTotal2D;

        public int GetSoundInstancesTotal3D() => this.m_soundInstancesTotal3D;

        private void Update3DCuesState(bool updatePosition = false)
        {
            if (!this.m_canUpdate3dSounds || this.m_3Dsounds == null || this.m_3Dsounds.Count <= 0)
                return;
            lock (this.m_3Dsounds)
            {
                int index = 0;
                while (index < this.m_3Dsounds.Count)
                {
                    IMy3DSoundEmitter source = this.m_3Dsounds[index];
                    object syncRoot = source.SyncRoot;
                    if (Monitor.TryEnter(syncRoot))
                    {
                        try
                        {
                            IMySourceVoice sound = source.Sound;
                            if (sound == null)
                                this.m_3Dsounds.RemoveAt(index);
                            else if (!sound.IsPlaying && !sound.IsBuffered)
                            {
                                source.SetSound((IMySourceVoice)null, nameof(Update3DCuesState));
                                this.m_3Dsounds.RemoveAt(index);
                            }
                            else
                            {
                                if (((MySourceVoice)sound).Emitter != source)
                                    MyLog.Default.WriteLine(string.Format("Emitter sound history: {0}", source.DebugData));
                                if (updatePosition)
                                    this.Update3DCuePosition(source);
                                ++index;
                            }
                        }
                        finally
                        {
                            Monitor.Exit(syncRoot);
                        }
                    }
                    else
                        ++index;
                }
            }
        }

        private void Update3DCuesPositions()
        {
            if (!this.m_canPlay)
                return;
            this.Update3DCuesState(true);
        }

        private void Update3DCuePosition(IMy3DSoundEmitter source)
        {
            MySoundData cue = this.m_cueBank?.GetCue(source.SoundId);
            if (cue == null || source.Sound == null || !(source.Sound is MySourceVoice sound))
                return;
            int num = 0;
            try
            {
                float maxDistance = (float)((double)source.CustomMaxDistance ?? (double)cue.MaxDistance);
                if (!sound.IsBuffered)
                {
                    num = 1;
                    Vector3D sourcePosition = source.SourcePosition;
                    num = 2;
                    Vector3 velocity = source.Velocity;
                    num = 3;
                    this.m_helperEmitter.UpdateValuesOmni((Vector3)sourcePosition, velocity, cue, this.m_deviceDetails.OutputFormat.Channels, new float?(maxDistance), source.DopplerScaler);
                    num = 4;
                    sound.DistanceToListener = this.Apply3D(sound, this.m_listener, this.m_helperEmitter, source.SourceChannels, this.m_deviceDetails.OutputFormat.Channels, this.m_calculateFlags, maxDistance, sound.FrequencyRatio, sound.Silent, !source.Realistic, this.m_enableDoppler);
                }
                else
                {
                    num = 5;
                    Vector3D sourcePosition = source.SourcePosition;
                    num = 6;
                    Vector3 velocity = source.Velocity;
                    num = 7;
                    this.m_helperEmitter.UpdateValuesOmni((Vector3)sourcePosition, velocity, maxDistance, this.m_deviceDetails.OutputFormat.Channels, cue.VolumeCurve, source.DopplerScaler);
                    num = 8;
                    sound.DistanceToListener = this.Apply3D(sound, this.m_listener, this.m_helperEmitter, source.SourceChannels, this.m_deviceDetails.OutputFormat.Channels, this.m_calculateFlags, maxDistance, sound.FrequencyRatio, sound.Silent, !source.Realistic, this.m_enableDoppler);
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
                MyLog.Default.WriteLine(string.Format("{0} {1} {2} {3}", (object)sound.IsBuffered, (object)num, (object)(sound.Voice == null), (object)(this.m_listener == null)));
                MyLog.Default.WriteLine(string.Format("Emitter sound history: {0}", source.DebugData));
                MyLog.Default.WriteLine("SourceVoice history: " + sound.DebugData);
                IMy3DSoundEmitter emitter = sound.Emitter;
                throw;
            }
        }

        private float Apply3D(
          MySourceVoice managedVoice,
          Listener listener,
          Emitter emitter,
          int srcChannels,
          int dstChannels,
          CalculateFlags flags,
          float maxDistance,
          float frequencyRatio,
          bool silent,
          bool use3DCalculation = true,
          bool fullDoppler = true)
        {
            DspSettings settings = new DspSettings(srcChannels, dstChannels);
            int num1 = srcChannels * dstChannels;
            SourceVoice voice = managedVoice.Voice;
            if (use3DCalculation)
            {
                if (!fullDoppler)
                {
                    Listener listener1 = listener;
                    Emitter emitter1 = emitter;
                    RawVector3 rawVector3_1 = new RawVector3();
                    RawVector3 rawVector3_2;
                    RawVector3 rawVector3_3 = rawVector3_2 = rawVector3_1;
                    emitter1.Velocity = rawVector3_2;
                    RawVector3 rawVector3_4 = rawVector3_3;
                    listener1.Velocity = rawVector3_4;
                }
                this.m_x3dAudio.Calculate(listener, emitter, flags, settings);
                settings.DopplerFactor = MathHelper.Clamp(settings.DopplerFactor, 0.9f, 1f);
                voice.SetFrequencyRatio(frequencyRatio * settings.DopplerFactor);
            }
            else
            {
                settings.EmitterToListenerDistance = Vector3.Distance(new Vector3(listener.Position.X, listener.Position.Y, listener.Position.Z), new Vector3(emitter.Position.X, emitter.Position.Y, emitter.Position.Z));
                if (srcChannels <= 2 && dstChannels <= 8)
                {
                    float[] numArray = srcChannels == 1 ? MyXAudio2.m_outputMatrixMono : MyXAudio2.m_outputMatrixStereo;
                    for (int index = 0; index < settings.MatrixCoefficients.Length; ++index)
                        settings.MatrixCoefficients[index] = numArray[index];
                }
                else
                {
                    for (int index = 0; index < num1; ++index)
                        settings.MatrixCoefficients[index] = 1f;
                }
            }
            if ((double)emitter.InnerRadius == 0.0)
            {
                float num2 = !silent ? MathHelper.Clamp((float)(1.0 - (double)settings.EmitterToListenerDistance / (double)maxDistance), 0.0f, 1f) : 0.0f;
                for (int index = 0; index < num1; ++index)
                    settings.MatrixCoefficients[index] *= num2;
            }
            try
            {
                voice.SetOutputMatrix((Voice)null, settings.SourceChannelCount, settings.DestinationChannelCount, settings.MatrixCoefficients);
            }
            catch
            {
                MyLog myLog = MyLog.Default;
                myLog.WriteLine(string.Format("Exception at SetOutputMatrix [{0}x{1}] IsValid: {2}", (object)srcChannels, (object)dstChannels, (object)voice.IsValid()));
                try
                {
                    myLog.WriteLine(string.Format("{0};{1};{2};{3}", (object)managedVoice.CueEnum.Hash.String, (object)managedVoice.GetOutputChannels(), (object)managedVoice.IsPlaying, (object)managedVoice.IsPaused));
                    myLog.WriteLine("Output voices: ");
                    VoiceSendDescriptor[] currentOutputVoices = managedVoice.CurrentOutputVoices;
                    if (currentOutputVoices == null)
                    {
                        myLog.WriteLine("NULL");
                    }
                    else
                    {
                        foreach (VoiceSendDescriptor voiceSendDescriptor in currentOutputVoices)
                        {
                            VoiceDetails voiceDetails = voiceSendDescriptor.OutputVoice.VoiceDetails;
                            myLog.WriteLine(string.Format("{0} {1} {2}", (object)(VoiceFlags)voiceDetails.ActiveFlags, (object)voiceDetails.InputChannelCount, (object)voiceDetails.InputSampleRate));
                        }
                    }
                    myLog.WriteLine("Current matrix");
                    try
                    {
                        voice.GetOutputMatrix((Voice)null, srcChannels, dstChannels, settings.MatrixCoefficients);
                        myLog.WriteLine(string.Join<float>(";", (IEnumerable<float>)settings.MatrixCoefficients));
                    }
                    catch
                    {
                        myLog.WriteLine("Get failed");
                        for (int destinationChannels = 1; destinationChannels <= 10; ++destinationChannels)
                        {
                            try
                            {
                                float[] numArray = new float[srcChannels * destinationChannels];
                                voice.GetOutputMatrix((Voice)null, srcChannels, destinationChannels, numArray);
                                myLog.WriteLine(string.Format("Success: GetOutputMatrix [{0}x{1}]", (object)srcChannels, (object)destinationChannels));
                                myLog.WriteLine(string.Join<float>(";", (IEnumerable<float>)numArray));
                                break;
                            }
                            catch
                            {
                            }
                        }
                    }
                    if (!currentOutputVoices.IsNullOrEmpty<VoiceSendDescriptor>())
                    {
                        try
                        {
                            myLog.WriteLine("Retry with single");
                            voice.SetOutputMatrix(currentOutputVoices[0].OutputVoice, settings.SourceChannelCount, settings.DestinationChannelCount, settings.MatrixCoefficients);
                        }
                        catch
                        {
                            myLog.WriteLine("Single failed");
                        }
                    }
                    myLog.WriteLine("Test outChannels");
                    for (int destinationChannels = 1; destinationChannels <= 5; ++destinationChannels)
                    {
                        for (int sourceChannels = 1; sourceChannels <= 5; ++sourceChannels)
                        {
                            try
                            {
                                float[] levelMatrixRef = new float[sourceChannels * destinationChannels];
                                voice.SetOutputMatrix((Voice)null, sourceChannels, destinationChannels, levelMatrixRef);
                                myLog.WriteLine(string.Format("Success: SetOutputMatrix [{0}x{1}]", (object)sourceChannels, (object)destinationChannels));
                                break;
                            }
                            catch
                            {
                            }
                        }
                    }
                    int outputChannels = managedVoice.GetOutputChannels();
                    DeviceDetails deviceDetails = this.AudioPlatform.GetDeviceDetails(0, true);
                    myLog.WriteLine(string.Format("Retry with new dimensions [{0}x{1}]", (object)outputChannels, (object)deviceDetails.OutputFormat.Channels));
                    float[] levelMatrixRef1 = new float[outputChannels * deviceDetails.OutputFormat.Channels];
                    voice.SetOutputMatrix((Voice)null, outputChannels, deviceDetails.OutputFormat.Channels, levelMatrixRef1);
                    myLog.WriteLine("Retry succeeded");
                }
                catch
                {
                    myLog.WriteLine("FAIL");
                }
                throw;
            }
            return settings.EmitterToListenerDistance;
        }

        private void StopUpdating3DCue(IMy3DSoundEmitter source)
        {
            if (!this.m_canPlay)
                return;
            lock (this.m_3Dsounds)
                this.m_3Dsounds.Remove(source);
        }

        public void StopUpdatingAll3DCues()
        {
            if (!this.m_canPlay)
                return;
            lock (this.m_3Dsounds)
                this.m_3Dsounds.Clear();
        }

        bool IMyAudio.SourceIsCloseEnoughToPlaySound(
          Vector3 sourcePosition,
          MyCueId cueId,
          float? customMaxDistance)
        {
            return this.SourceIsCloseEnoughToPlaySound(sourcePosition, cueId, customMaxDistance);
        }

        public bool SourceIsCloseEnoughToPlaySound(
          Vector3 sourcePosition,
          MyCueId cueId,
          float? customMaxDistance = 0.0f)
        {
            if (this.m_cueBank == null || cueId.Hash == MyStringHash.NullOrEmpty)
                return false;
            MySoundData cue = this.m_cueBank.GetCue(cueId);
            if (cue == null)
                return false;
            float num1 = sourcePosition.LengthSquared();
            float? nullable1 = customMaxDistance;
            float num2 = 0.0f;
            if ((double)nullable1.GetValueOrDefault() > (double)num2 & nullable1.HasValue)
            {
                double num3 = (double)num1;
                float? nullable2 = customMaxDistance;
                float? nullable3 = customMaxDistance;
                nullable1 = nullable2.HasValue & nullable3.HasValue ? new float?(nullable2.GetValueOrDefault() * nullable3.GetValueOrDefault()) : new float?();
                double valueOrDefault = (double)nullable1.GetValueOrDefault();
                return num3 <= valueOrDefault & nullable1.HasValue;
            }
            return (double)cue.UpdateDistance > 0.0 ? (double)num1 <= (double)cue.UpdateDistance * (double)cue.UpdateDistance : (double)num1 <= (double)cue.MaxDistance * (double)cue.MaxDistance;
        }

        private MySourceVoice PlaySound(
          MyCueId cueId,
          IMy3DSoundEmitter source = null,
          MySoundDimensions type = MySoundDimensions.D2,
          bool skipIntro = false,
          bool skipToEnd = false,
          bool isMusic = false)
        {
            int waveNumber;
            MySourceVoice sound = this.GetSound(cueId, out waveNumber, source, type, isMusic);
            if (source != null)
                source.LastPlayedWaveNumber = -1;
            if (sound != null)
            {
                sound.Start(skipIntro, skipToEnd);
                if (source != null)
                    source.LastPlayedWaveNumber = waveNumber;
            }
            return sound;
        }

        private MySourceVoice GetSound(
          MyCueId cueId,
          out int waveNumber,
          IMy3DSoundEmitter source = null,
          MySoundDimensions type = MySoundDimensions.D2,
          bool isMusic = false)
        {
            waveNumber = -1;
            if (cueId.Hash == MyStringHash.NullOrEmpty || !this.m_canPlay || this.m_cueBank == null)
                return (MySourceVoice)null;
            MySoundData cue = this.m_cueBank.GetCue(cueId);
            if (cue == null)
                return (MySourceVoice)null;
            if (this.SoloCue != null && this.SoloCue != cue)
                return (MySourceVoice)null;
            MyVoicePoolType poolType = MyVoicePoolType.Sound;
            if (cue.IsHudCue)
                poolType = MyVoicePoolType.Hud;
            else if (isMusic)
                poolType = MyVoicePoolType.Music;
            int tryIgnoreWaveNumber = source != null ? source.LastPlayedWaveNumber : -1;
            MySourceVoice voice = this.m_cueBank.GetVoice(cueId, out waveNumber, type, tryIgnoreWaveNumber, poolType);
            if (voice == null && source != null && source.Force3D)
            {
                MySoundDimensions type1 = type == MySoundDimensions.D3 ? MySoundDimensions.D2 : MySoundDimensions.D3;
                voice = this.m_cueBank.GetVoice(cueId, out waveNumber, type1, tryIgnoreWaveNumber, poolType);
            }
            if (voice == null)
                return (MySourceVoice)null;
            float volume = cue.Volume;
            float? nullable;
            if (source != null)
            {
                nullable = source.CustomVolume;
                if (nullable.HasValue)
                {
                    nullable = source.CustomVolume;
                    volume = nullable.Value;
                }
            }
            if ((double)cue.VolumeVariation != 0.0)
            {
                float num = this.VolumeVariation(cue);
                volume = MathHelper.Clamp(volume + num, 0.0f, 1f);
            }
            voice.Emitter = source;
            voice.VolumeMultiplier = 1f;
            voice.SetVolume(volume);
            float semitones = cue.Pitch;
            if ((double)cue.PitchVariation != 0.0)
                semitones += this.PitchVariation(cue);
            if (cue.DisablePitchEffects)
                semitones = 0.0f;
            voice.FrequencyRatio = (double)semitones == 0.0 ? 1f : this.SemitonesToFrequencyRatio(semitones);
            if (source != null)
            {
                if (source.DebugData is string debugData1 && debugData1.Length > 1000)
                    source.DebugData = (object)string.Empty;
                string debugData2 = voice.DebugData;
                if ((debugData2 != null ? (debugData2.Length > 1000 ? 1 : 0) : 0) != 0)
                    voice.DebugData = string.Empty;
            }
            if (type == MySoundDimensions.D3)
            {
                nullable = source.CustomMaxDistance;
                float maxDistance = (float)((double)nullable ?? (double)cue.MaxDistance);
                this.m_helperEmitter.UpdateValuesOmni((Vector3)source.SourcePosition, source.Velocity, cue, this.m_deviceDetails.OutputFormat.Channels, new float?(maxDistance), source.DopplerScaler);
                source.SourceChannels = voice.GetOutputChannels();
                voice.DistanceToListener = this.Apply3D(voice, this.m_listener, this.m_helperEmitter, source.SourceChannels, this.m_deviceDetails.OutputFormat.Channels, this.m_calculateFlags, maxDistance, voice.FrequencyRatio, voice.Silent, !source.Realistic, this.m_enableDoppler);
                this.Update3DCuesState();
                this.Add3DCueToUpdateList(source);
                ++this.m_soundInstancesTotal3D;
            }
            else
            {
                voice.DistanceToListener = 0.0f;
                int outputChannels = voice.GetOutputChannels();
                int length = outputChannels * this.m_deviceDetails.OutputFormat.Channels;
                float[] levelMatrixRef = new float[length];
                for (int index = 0; index < length; ++index)
                    levelMatrixRef[index] = 1f;
                voice.Voice.SetOutputMatrix((Voice)null, outputChannels, this.m_deviceDetails.OutputFormat.Channels, levelMatrixRef);
                this.StopUpdating3DCue(source);
                ++this.m_soundInstancesTotal2D;
            }
            return voice;
        }

        IMySourceVoice IMyAudio.PlaySound(
          MyCueId cueId,
          IMy3DSoundEmitter source,
          MySoundDimensions type,
          bool skipIntro,
          bool skipToEnd)
        {
            return (IMySourceVoice)this.PlaySound(cueId, source, type, skipIntro, skipToEnd, false);
        }

        IMySourceVoice IMyAudio.GetSound(
          MyCueId cueId,
          IMy3DSoundEmitter source,
          MySoundDimensions type)
        {
            return (IMySourceVoice)this.GetSound(cueId, out int _, source, type);
        }

        IMySourceVoice IMyAudio.GetSound(
          IMy3DSoundEmitter source,
          MySoundDimensions dimension)
        {
            if (!this.m_canPlay || this.m_deviceLost)
                return (IMySourceVoice)null;
            WaveFormat sourceFormat = new WaveFormat(24000, 16, 1);
            if (source.DebugData is string debugData && debugData.Length > 1000)
                source.DebugData = (object)string.Empty;
            source.SourceChannels = sourceFormat.Channels;
            MySourceVoice managedVoice = new MySourceVoice(this.m_audioEngine, sourceFormat);
            managedVoice.Emitter = source;
            float? nullable = source.CustomVolume;
            float volume = (float)((double)nullable ?? 1.0);
            nullable = source.CustomMaxDistance;
            float maxDistance = (float)((double)nullable ?? 0.0);
            managedVoice.SetVolume(volume);
            if (dimension == MySoundDimensions.D3)
            {
                this.m_helperEmitter.UpdateValuesOmni((Vector3)source.SourcePosition, source.Velocity, maxDistance, this.m_deviceDetails.OutputFormat.Channels, MyCurveType.Linear, source.DopplerScaler);
                managedVoice.DistanceToListener = this.Apply3D(managedVoice, this.m_listener, this.m_helperEmitter, source.SourceChannels, this.m_deviceDetails.OutputFormat.Channels, this.m_calculateFlags, maxDistance, managedVoice.FrequencyRatio, managedVoice.Silent, !source.Realistic, this.m_enableDoppler);
                this.Update3DCuesState();
                this.Add3DCueToUpdateList(source);
                ++this.m_soundInstancesTotal3D;
            }
            return (IMySourceVoice)managedVoice;
        }

        public void WriteDebugInfo(StringBuilder sb)
        {
            if (this.m_cueBank == null)
                return;
            this.m_cueBank.WriteDebugInfo(sb);
        }

        public bool IsLoopable(MyCueId cueId)
        {
            if (cueId.Hash == MyStringHash.NullOrEmpty || this.m_cueBank == null)
                return false;
            MySoundData cue = this.m_cueBank.GetCue(cueId);
            return cue != null && cue.Loopable;
        }

        public ListReader<IMy3DSoundEmitter> Get3DSounds() => (ListReader<IMy3DSoundEmitter>)this.m_3Dsounds;

        public IMyAudioEffect ApplyEffect(
          IMySourceVoice input,
          MyStringHash effect,
          MyCueId[] cueIds = null,
          float? duration = null,
          bool musicEffect = false)
        {
            if (this.m_effectBank == null)
                return (IMyAudioEffect)null;
            List<MySourceVoice> mySourceVoiceList = new List<MySourceVoice>();
            if (cueIds != null)
            {
                foreach (MyCueId cueId in cueIds)
                {
                    MySourceVoice sound = this.GetSound(cueId, out int _);
                    if (sound != null)
                        mySourceVoiceList.Add(sound);
                }
            }
            IMyAudioEffect effect1 = (IMyAudioEffect)this.m_effectBank.CreateEffect(input, effect, mySourceVoiceList.ToArray(), duration);
            if (musicEffect && effect1.OutputSound is MySourceVoice)
                (effect1.OutputSound as MySourceVoice).SetOutputVoices(this.m_musicAudioVoiceDesc);
            return effect1;
        }

        public Vector3 GetListenerPosition() => this.m_listener == null ? Vector3.Zero : new Vector3(this.m_listener.Position.X, this.m_listener.Position.Y, this.m_listener.Position.Z);

        public void EnumerateLastSounds(Action<StringBuilder, bool> a)
        {
        }

        public void DisposeCache() => MyInMemoryWaveDataCache.Static.Dispose();

        public void Preload(string soundFile) => MyInMemoryWaveDataCache.Static.Preload(soundFile);

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckUpdate()
        {
        }

        internal void EnqueueStopPlayingCallback(MySourceVoice voice) => this.m_voicesForStopPlayingCallback.Enqueue(voice);

        private struct MyMusicTransition
        {
            public int Priority;
            public MyStringId TransitionEnum;
            public MyStringId Category;

            public MyMusicTransition(int priority, MyStringId transitionEnum, MyStringId category)
            {
                this.Priority = priority;
                this.TransitionEnum = transitionEnum;
                this.Category = category;
            }
        }

        private delegate void VolumeChangeHandler(float newVolume);
    }
}
