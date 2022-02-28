using SharpDX.XAudio2;
using SharpDX.XAudio2.Fx;
using System;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Utils;

namespace VRage.Audio
{
    public class MyCueBank : IDisposable
    {
        private static MyStringId MUSIC_CATEGORY = MyStringId.GetOrCompute("Music");
        private XAudio2 m_audioEngine;
        public Dictionary<MyCueId, MySoundData>? m_cues;
        private MyWaveBank? m_waveBank;
        private VoiceSendDescriptor[]? m_hudVoiceDescriptors;
        private VoiceSendDescriptor[]? m_gameVoiceDescriptors;
        private VoiceSendDescriptor[]? m_musicVoiceDescriptors;
        private Dictionary<MyWaveFormat, MySourceVoicePool>? m_voiceHudPools;
        private Dictionary<MyWaveFormat, MySourceVoicePool>? m_voiceSoundPools;
        private Dictionary<MyWaveFormat, MySourceVoicePool>? m_voiceMusicPools;
        private Dictionary<MyStringId, Dictionary<MyStringId, MyCueId>>? m_musicTransitionCues;
        private Dictionary<MyStringId, List<MyCueId>>? m_musicTracks;
        private List<MyStringId>? m_categories;
        private Reverb? m_reverb;

        public bool UseSameSoundLimiter { get; set; }

        public bool DisablePooling { get; set; }

        public bool CacheLoaded { get; set; }

        public Dictionary<MyCueId, MySoundData>.ValueCollection CueDefinitions => (m_cues ?? new Dictionary<MyCueId, MySoundData>()).Values;

        public bool ApplyReverb { get; set; }

        public MyCueBank(XAudio2 audioEngine, ListReader<MySoundData> cues, VoiceSendDescriptor[] gameDesc, VoiceSendDescriptor[] hudDesc, VoiceSendDescriptor[] musicDesc, bool cacheLoaded)
        {
            ApplyReverb = false;
            CacheLoaded = cacheLoaded;
            m_audioEngine = audioEngine;
            if (cues.Count <= 0)
                return;

            m_cues = new Dictionary<MyCueId, MySoundData>(cues.Count, MyCueId.Comparer);
            InitTransitionCues();
            InitCues(cues);
            InitCategories();
            InitWaveBank();
            InitVoicePools(gameDesc, hudDesc, musicDesc);
            m_reverb = new Reverb(audioEngine);
        }

        public void SetAudioEngine(XAudio2 audioEngine)
        {
            m_audioEngine = audioEngine;

            if (m_voiceHudPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceHudPools.Values)
                    mySourceVoicePool.SetAudioEngine(audioEngine);

            if (m_voiceSoundPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceSoundPools.Values)
                    mySourceVoicePool.SetAudioEngine(audioEngine);

            if (m_voiceMusicPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceMusicPools.Values)
                    mySourceVoicePool.SetAudioEngine(audioEngine);
        }

        public void SetAudioEngine(XAudio2 audioEngine, VoiceSendDescriptor[] gameAudioVoiceDesc, VoiceSendDescriptor[] hudAudioVoiceDesc, VoiceSendDescriptor[] musicAudioVoiceDesc)
        {

            m_audioEngine = audioEngine;

            if (m_voiceHudPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceHudPools.Values)
                {
                    mySourceVoicePool.SetAudioEngine(null);
                    mySourceVoicePool.Dispose();
                }

            if (m_voiceSoundPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceSoundPools.Values)
                {
                    mySourceVoicePool.SetAudioEngine(null);
                    mySourceVoicePool.Dispose();
                }

            if (m_voiceMusicPools != null)
                foreach (MySourceVoicePool mySourceVoicePool in m_voiceMusicPools.Values)
                {
                    mySourceVoicePool.SetAudioEngine(null);
                    mySourceVoicePool.Dispose();
                }

            if (gameAudioVoiceDesc == null || hudAudioVoiceDesc == null)
                return;

            InitVoicePools(gameAudioVoiceDesc, hudAudioVoiceDesc, musicAudioVoiceDesc);
        }

        private void InitCues(ListReader<MySoundData> cues)
        {
            foreach (MySoundData cue in cues)
            {
                MyCueId myCueId = new MyCueId(cue.SubtypeId);
                m_cues[myCueId] = cue;
                if (cue.Category == MUSIC_CATEGORY)
                    AddMusicCue(cue.MusicTrack.TransitionCategory, cue.MusicTrack.MusicCategory, myCueId);
            }
        }

        private void InitCategories()
        {
            if (m_cues == null)
                return;

            m_categories = new List<MyStringId>();
            foreach (KeyValuePair<MyCueId, MySoundData> cue in m_cues)
            {
                if (!m_categories.Contains(cue.Value.Category))
                    m_categories.Add(cue.Value.Category);
            }
        }

        private void InitWaveBank()
        {
            if (m_cues == null)
                return;

            m_waveBank = new MyWaveBank();
            foreach (KeyValuePair<MyCueId, MySoundData> cue in m_cues)
            {
                if (cue.Value.Waves != null)
                {
                    foreach (MyAudioWave wave in cue.Value.Waves)
                        m_waveBank.Add(cue.Value, wave, CacheLoaded);
                }
            }
        }

        private void InitVoicePools(VoiceSendDescriptor[] gameDesc, VoiceSendDescriptor[] hudDesc, VoiceSendDescriptor[] musicDesc)
        {
            m_hudVoiceDescriptors = hudDesc;
            m_gameVoiceDescriptors = gameDesc;
            m_musicVoiceDescriptors = musicDesc;

            m_voiceHudPools = new Dictionary<MyWaveFormat, MySourceVoicePool>();
            m_voiceSoundPools = new Dictionary<MyWaveFormat, MySourceVoicePool>();
            m_voiceMusicPools = new Dictionary<MyWaveFormat, MySourceVoicePool>();
        }

        private MySourceVoicePool? GetVoicePool(MyVoicePoolType poolType, MyWaveFormat format)
        {
            Dictionary<MyWaveFormat, MySourceVoicePool>? dictionary = null;
            VoiceSendDescriptor[]? desc = null;
            switch (poolType)
            {
                case MyVoicePoolType.Hud:
                    dictionary = m_voiceHudPools;
                    desc = m_hudVoiceDescriptors;
                    break;
                case MyVoicePoolType.Sound:
                    dictionary = m_voiceSoundPools;
                    desc = m_gameVoiceDescriptors;
                    break;
                case MyVoicePoolType.Music:
                    dictionary = m_voiceMusicPools;
                    desc = m_musicVoiceDescriptors;
                    break;
            }

            MySourceVoicePool? voicePool = null;
            lock (m_audioEngine)
            {
                if (dictionary != null && !dictionary.TryGetValue(format, out voicePool) && desc != null)
                {
                    voicePool = new MySourceVoicePool(m_audioEngine, format.WaveFormat, this, desc);
                    voicePool.UseSameSoundLimiter = UseSameSoundLimiter;
                    dictionary.Add(format, voicePool);
                }
            }

            return voicePool;
        }

        public void SetSameSoundLimiter()
        {
            if (m_voiceHudPools != null)

                foreach (var mySourceVoicePool in m_voiceHudPools.Values)
                    mySourceVoicePool.UseSameSoundLimiter = UseSameSoundLimiter;

            if (m_voiceSoundPools != null)
                foreach (var mySourceVoicePool in m_voiceSoundPools.Values)
                    mySourceVoicePool.UseSameSoundLimiter = UseSameSoundLimiter;

            if (m_voiceMusicPools != null)
                foreach (var mySourceVoicePool in m_voiceMusicPools.Values)
                    mySourceVoicePool.UseSameSoundLimiter = UseSameSoundLimiter;
        }

        private void InitTransitionCues()
        {
            m_musicTransitionCues = new Dictionary<MyStringId, Dictionary<MyStringId, MyCueId>>(MyStringId.Comparer);
            m_musicTracks = new Dictionary<MyStringId, List<MyCueId>>(MyStringId.Comparer);
        }

        private void AddMusicCue(MyStringId musicTransition, MyStringId category, MyCueId cueId)
        {
            if (m_musicTransitionCues != null && !m_musicTransitionCues.ContainsKey(musicTransition))
                m_musicTransitionCues[musicTransition] = new Dictionary<MyStringId, MyCueId>(MyStringId.Comparer);

            if (m_musicTransitionCues != null && !m_musicTransitionCues[musicTransition].ContainsKey(category))
                m_musicTransitionCues[musicTransition].Add(category, cueId);

            if (m_musicTracks != null && !m_musicTracks.ContainsKey(category))
                m_musicTracks.Add(category, new List<MyCueId>());

            if (m_musicTracks != null)
                m_musicTracks[category].Add(cueId);
        }

        public Dictionary<MyStringId, List<MyCueId>> GetMusicCues() => m_musicTracks ?? new Dictionary<MyStringId, List<MyCueId>>();

        public void Update()
        {
            UpdatePools(m_voiceHudPools);
            UpdatePools(m_voiceSoundPools);
            UpdatePools(m_voiceMusicPools);

            static void UpdatePools(Dictionary<MyWaveFormat, MySourceVoicePool>? pools)
            {
                if (pools != null)
                    foreach (KeyValuePair<MyWaveFormat, MySourceVoicePool> pool in pools)
                        pool.Value.Update();
            }
        }

        public void ClearSounds()
        {
            ClearPools(m_voiceHudPools);
            ClearPools(m_voiceSoundPools);
            ClearPools(m_voiceMusicPools);

            static void ClearPools(Dictionary<MyWaveFormat, MySourceVoicePool>? pools)
            {
                if (pools != null)
                    foreach (KeyValuePair<MyWaveFormat, MySourceVoicePool> pool in pools)
                        pool.Value.StopAll();
            }
        }

        public void Dispose()
        {
            if (m_waveBank != null)
                m_waveBank.Dispose();

            if (m_reverb != null)
                m_reverb.Dispose();

            m_reverb = null;

            ClearSounds();

            DisposePools(m_voiceHudPools);
            DisposePools(m_voiceSoundPools);
            DisposePools(m_voiceMusicPools);

            if (m_cues != null)
                m_cues.Clear();

            static void DisposePools(Dictionary<MyWaveFormat, MySourceVoicePool>? pools)
            {
                if (pools != null)
                {
                    foreach (KeyValuePair<MyWaveFormat, MySourceVoicePool> pool in pools)
                        pool.Value.Dispose();

                    pools.Clear();
                }
            }
        }

        public MyStringId? GetRandomTransitionEnum()
        {
            if (m_musicTransitionCues == null)
                return new MyStringId();
            else
                return new MyStringId?(m_musicTransitionCues.Keys.ElementAt(MyUtils.GetRandomInt(m_musicTransitionCues.Count)));
        }

        public MyStringId GetRandomTransitionCategory(ref MyStringId transitionEnum, ref MyStringId noRandom)
        {
            if (m_musicTransitionCues != null && !m_musicTransitionCues.ContainsKey(transitionEnum))
            {
                do
                {
                    transitionEnum = GetRandomTransitionEnum() ?? new MyStringId();
                }
                while (transitionEnum == noRandom && m_musicTransitionCues.Count > 1);
            }

            if (m_musicTransitionCues != null)
            {
                int randomInt = MyUtils.GetRandomInt(m_musicTransitionCues[transitionEnum].Count);

                int num = 0;
                foreach (KeyValuePair<MyStringId, MyCueId> keyValuePair in m_musicTransitionCues[transitionEnum])
                {
                    if (num == randomInt)
                        return keyValuePair.Key;
                    ++num;
                }
            }

            throw new InvalidBranchException();
        }

        public bool IsValidTransitionCategory(MyStringId transitionEnum, MyStringId category)
        {
            if (m_musicTransitionCues == null || !m_musicTransitionCues.ContainsKey(transitionEnum))
                return false;

            return category == MyStringId.NullOrEmpty || m_musicTransitionCues[transitionEnum].ContainsKey(category);
        }

        public MyCueId GetTransitionCue(MyStringId transitionEnum, MyStringId category) => m_musicTransitionCues[transitionEnum][category];

        public MySoundData? GetCue(MyCueId cueId)
        {
            if (m_cues == null)
                return null;

            if (!m_cues.ContainsKey(cueId) && cueId.Hash != MyStringHash.NullOrEmpty)
                MyLog.Default.WriteLine("Cue was not found: " + cueId, LoggingOptions.AUDIO);

            MySoundData? cue = null;
            m_cues.TryGetValue(cueId, out cue);

            return cue;
        }

        public List<MyStringId> GetCategories() => m_categories ?? new List<MyStringId>();

        private MyInMemoryWave? GetRandomWave(MySoundData cue, MySoundDimensions type, out int waveNumber, out CuePart part, int tryIgnoreWaveNumber = -1)
        {
            int maxValue = 0;
            foreach (MyAudioWave wave in cue.Waves)
            {
                if (wave.Type == type)
                    ++maxValue;
            }

            if (maxValue == 0)
            {
                waveNumber = 0;
                part = CuePart.Start;
                return null;
            }

            waveNumber = MyUtils.GetRandomInt(maxValue);
            if (maxValue > 2 && waveNumber == tryIgnoreWaveNumber)
                waveNumber = (waveNumber + 1) % maxValue;

            MyInMemoryWave? wave1 = GetWave(cue, type, waveNumber, CuePart.Start);
            if (wave1 != null)
            {
                part = CuePart.Start;
            }
            else
            {
                wave1 = GetWave(cue, type, waveNumber, CuePart.Loop);
                part = CuePart.Loop;
            }

            return wave1;
        }

        private MyInMemoryWave? GetWave(MySoundData cue, MySoundDimensions dim, int waveNumber, CuePart cuePart)
        {
            if (m_waveBank == null)
                return null;

            foreach (MyAudioWave wave in cue.Waves)
            {
                if (wave.Type == dim)
                {
                    if (waveNumber == 0)
                    {
                        switch (cuePart)
                        {
                            case CuePart.Start:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.Start, cue, dim) : m_waveBank.GetWave(wave.Start);
                            case CuePart.Loop:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.Loop, cue, dim) : m_waveBank.GetWave(wave.Loop);
                            case CuePart.End:
                                return cue.StreamSound ? m_waveBank.GetStreamedWave(wave.End, cue, dim) : m_waveBank.GetWave(wave.End);
                        }
                    }
                    --waveNumber;
                }
            }

            return null;
        }

        private MySourceVoice? GetVoice(MyCueId cueId, MyInMemoryWave wave, CuePart part, MyVoicePoolType poolType)
        {
            var pool = GetVoicePool(poolType, new MyWaveFormat()
            {
                Encoding = wave.WaveFormat.Encoding,
                Channels = wave.WaveFormat.Channels,
                SampleRate = wave.WaveFormat.SampleRate,
                WaveFormat = wave.WaveFormat
            });
            if (pool == null)
                return null;

            var voice = pool.NextAvailable();
            if (voice == null)
                return null;

            voice.Flush();
            voice.SubmitSourceBuffer(cueId, wave, part);

            return voice;
        }

        internal MySourceVoice? GetVoice(MyCueId cueId, out int waveNumber, MySoundDimensions type = MySoundDimensions.D2, int tryIgnoreWaveNumber = -1, MyVoicePoolType poolType = MyVoicePoolType.Sound)
        {
            waveNumber = -1;

            if (m_audioEngine == null)
                return null;

            MySoundData? cue = GetCue(cueId);
            if (cue == null || cue.Waves == null || cue.Waves.Count == 0)
                return null;

            CuePart part;
            MyInMemoryWave? randomWave = GetRandomWave(cue, type, out waveNumber, out part, tryIgnoreWaveNumber);
            if (randomWave == null && type == MySoundDimensions.D2)
            {
                type = MySoundDimensions.D3;
                randomWave = GetRandomWave(cue, type, out waveNumber, out part, tryIgnoreWaveNumber);
            }
            if (randomWave == null)
                return null;

            MySourceVoice? voice = GetVoice(cueId, randomWave, part, poolType);
            if (voice == null)
                return null;

            if (cue.Loopable)
            {
                MyInMemoryWave? wave1 = GetWave(cue, type, waveNumber, CuePart.Loop);
                if (wave1 != null)
                {
                    if (voice.Owner.WaveFormat.Encoding == wave1.WaveFormat.Encoding)
                        voice.SubmitSourceBuffer(cueId, wave1, CuePart.Loop);
                    else
                        MyLog.Default.WriteLine(string.Format("Inconsistent encodings: '{0}', got '{1}', expected '{2}', part = '{3}'", cueId, wave1.WaveFormat.Encoding, voice.Owner.WaveFormat.Encoding, CuePart.Loop));
                }

                MyInMemoryWave? wave2 = GetWave(cue, type, waveNumber, CuePart.End);
                if (wave2 != null)
                {
                    if (voice.Owner.WaveFormat.Encoding == wave2.WaveFormat.Encoding)
                        voice.SubmitSourceBuffer(cueId, wave2, CuePart.End);
                    else
                        MyLog.Default.WriteLine(string.Format("Inconsistent encodings: '{0}', got '{1}', expected '{2}', part = '{3}'", cueId, wave2.WaveFormat.Encoding, voice.Owner.WaveFormat.Encoding, CuePart.End));
                }
            }

            return voice;
        }

        public void WriteDebugInfo(StringBuilder stringBuilder)
        {
            if (m_voiceHudPools == null && m_voiceSoundPools == null && m_voiceMusicPools == null)
                return;

            stringBuilder.Append("Playing: ");
            WritePlayingDebugPools(m_voiceHudPools);
            WritePlayingDebugPools(m_voiceSoundPools);
            WritePlayingDebugPools(m_voiceMusicPools);
            stringBuilder.AppendLine("");
            stringBuilder.Append("Not playing: ");
            WritePauseDebugPools(m_voiceHudPools);
            WritePauseDebugPools(m_voiceSoundPools);
            WritePauseDebugPools(m_voiceMusicPools);

            void WritePlayingDebugPools(Dictionary<MyWaveFormat, MySourceVoicePool>? pools)
            {
                if (pools == null)
                    return;

                foreach (KeyValuePair<MyWaveFormat, MySourceVoicePool> pool in pools)
                    pool.Value.WritePlayingDebugInfo(stringBuilder);
            }

            void WritePauseDebugPools(Dictionary<MyWaveFormat, MySourceVoicePool>? pools)
            {
                if (pools == null)
                    return;

                foreach (KeyValuePair<MyWaveFormat, MySourceVoicePool> pool in pools)
                    pool.Value.WritePausedDebugInfo(stringBuilder);
            }
        }

        public enum CuePart
        {
            Start,
            Loop,
            End,
        }
    }
}
