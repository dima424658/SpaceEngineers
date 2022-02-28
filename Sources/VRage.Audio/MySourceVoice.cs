using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;
using VRage.Data.Audio;
using VRage.Utils;

namespace VRage.Audio
{
    internal class MySourceVoice : IMySourceVoice
    {
        private MySourceVoicePool m_owner;
        private SourceVoice m_voice;
        private MyCueId m_cueId;
        private MyInMemoryWave[] m_loopBuffers = new MyInMemoryWave[3];
        private float m_frequencyRatio = 1f;
        private VoiceSendDescriptor[] m_currentDescriptor;
        private Queue<DataStream> m_dataStreams;
        private XAudio2 m_device;
        private bool m_isPlaying;
        private bool m_isPaused;
        private bool m_isLoopable;
        private int m_activeSourceBuffers;
        private bool m_valid;
        private bool m_buffered;
        private float m_volumeBase = 1f;
        private float m_volumeMultiplier = 1f;
        public string DebugData;
        public IMy3DSoundEmitter Emitter;

        public Action<IMySourceVoice> StoppedPlaying { get; set; }

        public float DistanceToListener { get; set; }

        public SourceVoice Voice => this.m_voice;

        public MyCueId CueEnum => this.m_cueId;

        public bool IsPlaying => this.m_isPlaying;

        public bool IsPaused => this.m_isPaused;

        public bool IsLoopable => this.m_isLoopable;

        public bool IsValid => this.m_valid && this.IsNativeValid;

        public bool IsNativeValid => this.m_voice != null && this.m_voice.IsValid() && this.m_device != null && !this.m_device.IsDisposed && this.m_device.NativePointer != IntPtr.Zero;

        public MySourceVoicePool Owner => this.m_owner;

        public VoiceSendDescriptor[] CurrentOutputVoices => this.m_currentDescriptor;

        public float FrequencyRatio
        {
            get => this.m_frequencyRatio;
            set
            {
                this.m_frequencyRatio = value;
                if (!this.IsValid)
                    return;
                MySoundData cue = MyAudio.Static.GetCue(this.m_cueId);
                if (cue != null && cue.DisablePitchEffects)
                    return;
                try
                {
                    if (this.m_voice.State.BuffersQueued <= 0)
                        return;
                    this.m_voice.SetFrequencyRatio(this.FrequencyRatio);
                }
                catch (NullReferenceException ex)
                {
                }
            }
        }

        public float Volume => !this.IsValid ? 0.0f : this.m_volumeBase;

        public float VolumeMultiplier
        {
            get => !this.IsValid ? 1f : this.m_volumeMultiplier;
            set
            {
                this.m_volumeMultiplier = value;
                this.SetVolume(this.m_volumeBase);
            }
        }

        public bool Silent { get; set; }

        public bool IsBuffered => this.m_buffered;

        public MySourceVoice(SharpDX.XAudio2.XAudio2 device, WaveFormat sourceFormat)
        {
            this.m_device = device;
            device.Disposing += new EventHandler<EventArgs>(this.OnDeviceDisposing);
            device.CriticalError += new EventHandler<ErrorEventArgs>(this.OnDeviceCrashed);
            this.m_voice = new SourceVoice(device, sourceFormat, true);
            this.m_voice.BufferEnd += new Action<IntPtr>(this.OnStopPlayingBuffered);
            this.m_valid = true;
            this.m_dataStreams = new Queue<DataStream>();
            this.DistanceToListener = float.MaxValue;
            this.Flush();
        }

        public MySourceVoice(MySourceVoicePool owner, SharpDX.XAudio2.XAudio2 device, WaveFormat sourceFormat)
        {
            this.m_device = device;
            this.m_voice = new SourceVoice(device, sourceFormat, VoiceFlags.UseFilter, 2f, true);
            this.m_voice.BufferEnd += new Action<IntPtr>(this.OnStopPlaying);
            this.m_valid = true;
            this.m_owner = owner;
            this.m_owner.OnAudioEngineChanged += new AudioEngineChanged(this.m_owner_OnAudioEngineChanged);
            this.DistanceToListener = float.MaxValue;
            this.Flush();
        }

        private void OnDeviceDisposing(object sender, EventArgs eventArgs) => this.Destroy();

        private void OnDeviceCrashed(object sender, ErrorEventArgs errorEventArgs)
        {
            this.m_valid = false;
            this.m_device = (SharpDX.XAudio2.XAudio2)null;
        }

        private void m_owner_OnAudioEngineChanged() => this.m_valid = false;

        public void Flush()
        {
            this.m_cueId = new MyCueId(MyStringHash.NullOrEmpty);
            this.DisposeWaves();
            this.m_isPlaying = false;
            this.m_isPaused = false;
            this.m_isLoopable = false;
        }

        public int GetOutputChannels()
        {
            if (this.m_loopBuffers == null)
                return -1;
            int? nullable = new int?();
            foreach (MyInMemoryWave loopBuffer in this.m_loopBuffers)
            {
                if (loopBuffer != null)
                {
                    int channels = loopBuffer.WaveFormat.Channels;
                    if (!nullable.HasValue)
                        nullable = new int?(channels);
                }
            }
            return nullable ?? 1;
        }

        internal void SubmitSourceBuffer(MyCueId cueId, MyInMemoryWave wave, MyCueBank.CuePart part)
        {
            this.m_loopBuffers[(int)part] = wave;
            this.m_cueId = cueId;
            this.m_isLoopable |= wave.Buffer.LoopCount > 0;
        }

        private void SubmitSourceBuffer(MyInMemoryWave wave)
        {
            if (wave == null)
                return;
            AudioBuffer buffer = wave.Buffer;
            int loopCount = buffer.LoopCount;
            this.m_isLoopable |= buffer.LoopCount > 0;
            try
            {
                if (this.m_activeSourceBuffers == 0)
                    this.m_voice.SourceSampleRate = wave.WaveFormat.SampleRate;
            }
            catch (SharpDXException ex)
            {
            }
            ++this.m_activeSourceBuffers;
            this.m_voice.SubmitSourceBuffer(buffer, wave.Stream.DecodedPacketsInfo);
        }

        public void Start(bool skipIntro, bool skipToEnd = false)
        {
            if (!this.IsValid)
                return;
            if (!skipIntro)
                this.SubmitSourceBuffer(this.m_loopBuffers[0]);
            if (this.m_isLoopable | skipToEnd)
            {
                if (!skipToEnd)
                    this.SubmitSourceBuffer(this.m_loopBuffers[1]);
                this.SubmitSourceBuffer(this.m_loopBuffers[2]);
            }
            if (this.m_voice.State.BuffersQueued > 0)
            {
                this.m_voice.SetFrequencyRatio(this.FrequencyRatio);
                this.m_voice.Start();
                this.m_isPlaying = true;
            }
            else
                this.OnAllBuffersFinished();
        }

        public int GetLengthInSeconds()
        {
            uint[] decodedPacketsInfo = this.m_loopBuffers[0].Stream.DecodedPacketsInfo;
            int num1 = (int)decodedPacketsInfo[decodedPacketsInfo.Length - 1];
            WaveFormat waveFormat = this.m_loopBuffers[0].WaveFormat;
            int num2 = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            return num1 / num2 / waveFormat.SampleRate;
        }

        public void StartBuffered()
        {
            if (!this.IsValid)
                return;
            if (this.m_voice.State.BuffersQueued > 0)
            {
                this.m_voice.SetFrequencyRatio(this.FrequencyRatio);
                this.m_voice.Start();
                this.m_isPlaying = true;
                this.m_buffered = true;
            }
            else
                this.OnAllBuffersFinished();
        }

        public void SubmitBuffer(byte[] buffer)
        {
            if (!this.IsValid || this.m_dataStreams == null || this.m_dataStreams.Count >= 62)
                return;
            DataStream stream = DataStream.Create<byte>(buffer, true, false);
            AudioBuffer bufferRef = new AudioBuffer(stream);
            bufferRef.Flags = BufferFlags.None;
            lock (this.m_dataStreams)
                this.m_dataStreams.Enqueue(stream);
            try
            {
                this.m_voice.SubmitSourceBuffer(bufferRef, (uint[])null);
            }
            catch
            {
                MyLog.Default.WriteLine(string.Format("IsValid: {0} Buffers: {1} Buffer: {2} DataPtr: {3}", (object)this.IsValid, (object)this.m_dataStreams.Count, (object)buffer.Length, (object)bufferRef.AudioDataPointer));
                throw;
            }
        }

        private void OnStopPlayingBuffered(IntPtr context)
        {
            if (this.m_dataStreams == null)
                return;
            lock (this.m_dataStreams)
            {
                if (this.m_dataStreams.Count > 0)
                    this.m_dataStreams.Dequeue().Dispose();
            }
            if (this.m_dataStreams.Count != 0)
                return;
            this.OnAllBuffersFinished();
        }

        private void OnStopPlaying(IntPtr context)
        {
            --this.m_activeSourceBuffers;
            if (this.m_activeSourceBuffers != 0)
                return;
            this.OnAllBuffersFinished();
        }

        private void OnAllBuffersFinished()
        {
            this.m_isPlaying = false;
            if (this.StoppedPlaying != null)
                MyXAudio2.Instance.EnqueueStopPlayingCallback(this);
            this.m_owner?.OnStopPlaying(this);
        }

        public void Stop(bool force = false)
        {
            if (!this.IsValid || !this.m_isPlaying)
                return;
            if ((force || this.m_isLoopable) && this.m_owner != null)
            {
                this.m_owner.AddToFadeoutList(this);
            }
            else
            {
                try
                {
                    this.m_voice.Stop();
                    this.m_voice.FlushSourceBuffers();
                }
                catch (NullReferenceException ex)
                {
                }
            }
        }

        public void Pause()
        {
            if (!this.IsValid)
                return;
            this.m_voice.Stop();
            this.m_isPaused = true;
        }

        public void Resume()
        {
            if (!this.IsValid)
                return;
            this.m_voice.Start();
            this.m_isPaused = false;
        }

        public void SetVolume(float volume)
        {
            this.m_volumeBase = volume;
            if (!this.IsValid)
                return;
            try
            {
                this.m_voice.SetVolume(this.m_volumeBase * this.m_volumeMultiplier);
            }
            catch (NullReferenceException ex)
            {
            }
        }

        public void SetOutputVoices(VoiceSendDescriptor[] descriptors)
        {
            if (!this.IsValid || this.m_currentDescriptor == descriptors)
                return;
            this.m_voice.SetOutputVoices(descriptors);
            this.m_currentDescriptor = descriptors;
        }

        public override string ToString() => string.Format(this.m_cueId.ToString());

        public void Dispose()
        {
            this.m_valid = false;
            if (this.IsNativeValid)
            {
                this.m_device.Disposing -= new EventHandler<EventArgs>(this.OnDeviceDisposing);
                this.m_device.CriticalError -= new EventHandler<ErrorEventArgs>(this.OnDeviceCrashed);
                this.m_voice.DestroyVoice();
                this.m_voice.Dispose();
                this.m_voice = (SourceVoice)null;
                this.m_device = (SharpDX.XAudio2.XAudio2)null;
            }
            while (true)
            {
                Queue<DataStream> dataStreams = this.m_dataStreams;
                // ISSUE: explicit non-virtual call
                if ((dataStreams != null ? (__nonvirtual(dataStreams.Count) > 0 ? 1 : 0) : 0) != 0)
                    this.m_dataStreams.Dequeue().Dispose();
                else
                    break;
            }
            this.DisposeWaves();
        }

        private void DisposeWaves()
        {
            if (this.m_loopBuffers == null)
                return;
            for (int index = 0; index < this.m_loopBuffers.Length; ++index)
            {
                if (this.m_loopBuffers[index] != null && this.m_loopBuffers[index].Streamed)
                    this.m_loopBuffers[index].Dereference();
                this.m_loopBuffers[index] = (MyInMemoryWave)null;
            }
        }

        internal void CleanupBeforeDispose()
        {
            this.m_valid = false;
            this.StoppedPlaying = (Action<IMySourceVoice>)null;
            this.m_currentDescriptor = (VoiceSendDescriptor[])null;
            if (this.m_owner != null)
            {
                this.m_owner.OnAudioEngineChanged -= new AudioEngineChanged(this.m_owner_OnAudioEngineChanged);
                this.m_owner = (MySourceVoicePool)null;
            }
            if (this.IsNativeValid)
                this.m_voice.Stop();
            this.m_valid = false;
        }

        public void Destroy()
        {
            this.CleanupBeforeDispose();
            this.Dispose();
        }
    }
}
