using SharpDX.Multimedia;
using VRage.Data.Audio;
using VRage.FileSystem;

namespace VRage.Audio
{
    public class MyWaveBank : IDisposable
    {
        private readonly Dictionary<string, MyInMemoryWave> m_waves = new Dictionary<string, MyInMemoryWave>();
        internal readonly Dictionary<string, MyInMemoryWave> LoadedStreamedWaves = new Dictionary<string, MyInMemoryWave>();

        public int Count => m_waves.Count;

        private static bool FindAudioFile(MySoundData cue, string fileName, out string fsPath)
        {
            fsPath = Path.IsPathRooted(fileName) ? fileName : Path.Combine(MyFileSystem.ContentPath, "Audio", fileName);
            var dirName = Path.GetDirectoryName(fsPath);

            bool audioFile = MyFileSystem.FileExists(fsPath);
            if (!audioFile && dirName != null)
            {
                string path = Path.Combine(dirName, Path.GetFileNameWithoutExtension(fsPath) + ".wav");
                audioFile = MyFileSystem.FileExists(path);
                if (audioFile)
                    fsPath = path;
            }

            if (!audioFile)
            {
                MySoundErrorDelegate? onSoundError = MyAudio.OnSoundError;
                if (onSoundError != null)
                    onSoundError(cue, "Unable to find audio file: '" + cue.SubtypeId.ToString() + "', '" + fileName + "'");
            }

            return audioFile;
        }

        private static bool CheckWaveErrors(MySoundData cue, MyInMemoryWave wave, ref WaveFormatEncoding encoding, MySoundDimensions dim, string waveFileName)
        {
            bool flag = false;

            if (encoding == WaveFormatEncoding.Unknown)
                encoding = wave.WaveFormat.Encoding;

            if (wave.WaveFormat.Encoding == WaveFormatEncoding.Unknown)
            {
                flag = true;
                var onSoundError = MyAudio.OnSoundError;
                if (onSoundError != null)
                    onSoundError(cue, "Unknown audio encoding '" + cue.SubtypeId.ToString() + "', '" + waveFileName + "'");
            }

            if (dim == MySoundDimensions.D3 && wave.WaveFormat.Channels != 1)
            {
                flag = true;
                var onSoundError = MyAudio.OnSoundError;
                if (onSoundError != null)
                    onSoundError(cue, string.Format("3D sound '{0}', '{1}' must be in mono, got {2} channels", cue.SubtypeId.ToString(), waveFileName, wave.WaveFormat.Channels));
            }

            if (wave.WaveFormat.Encoding != encoding)
            {
                flag = true;
                var onSoundError = MyAudio.OnSoundError;
                if (onSoundError != null)
                    onSoundError(cue, string.Format("Inconsistent sound encoding in '{0}', '{1}', got '{2}', expected '{3}'", cue.SubtypeId.ToString(), waveFileName, wave.WaveFormat.Encoding, encoding));
            }

            return flag;
        }

        public void Add(MySoundData cue, MyAudioWave cueWave, bool cacheLoaded)
        {
            string[] files = { cueWave.Start, cueWave.Loop, cueWave.End };
            WaveFormatEncoding encoding = WaveFormatEncoding.Unknown;

            int i = 0;
            string fsPath;

            foreach (string waveFilename in files)
            {
                ++i;
                if (string.IsNullOrEmpty(waveFilename) || m_waves.ContainsKey(waveFilename) || !MyWaveBank.FindAudioFile(cue, waveFilename, out fsPath))
                    continue;

                if (cue.StreamSound)
                    break;

                try
                {
                    MyInMemoryWave wave = new MyInMemoryWave(cue, fsPath, this, cached: cacheLoaded);
                    if (i != 2)
                    {
                        wave.Buffer.LoopCount = 0;
                        wave.Buffer.LoopBegin = 0;
                        wave.Buffer.LoopLength = 0;
                    }
                    m_waves[waveFilename] = wave;

                    CheckWaveErrors(cue, wave, ref encoding, cueWave.Type, fsPath);
                }
                catch (Exception ex)
                {
                    MySoundErrorDelegate? onSoundError = MyAudio.OnSoundError;
                    if (onSoundError != null)
                        onSoundError(cue, string.Format("Unable to load audio file: '{0}', '{1}': {2}", cue.SubtypeId.ToString(), waveFilename, ex));
                }
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, MyInMemoryWave> wave in m_waves)
                wave.Value.Dispose();
            m_waves.Clear();
        }

        public MyInMemoryWave? GetWave(string filename) => string.IsNullOrEmpty(filename) || !m_waves.ContainsKey(filename) ? null : m_waves[filename];

        public MyInMemoryWave? GetStreamedWave(string waveFileName, MySoundData cue, MySoundDimensions dim = MySoundDimensions.D2)
        {
            if (string.IsNullOrEmpty(waveFileName))
                return null;

            string fsPath;
            if (FindAudioFile(cue, waveFileName, out fsPath))
            {
                try
                {
                    MyInMemoryWave? wave;
                    if (!LoadedStreamedWaves.TryGetValue(fsPath, out wave))
                    {
                        wave = new MyInMemoryWave(cue, fsPath, this, true);
                        LoadedStreamedWaves[fsPath] = wave;
                    }
                    else
                        wave.Reference();
                    WaveFormatEncoding encoding = WaveFormatEncoding.Unknown;
                    if (CheckWaveErrors(cue, wave, ref encoding, dim, waveFileName))
                    {
                        wave.Dereference();
                        wave = null;
                    }
                    return wave;
                }
                catch (Exception ex)
                {
                    MySoundErrorDelegate? onSoundError = MyAudio.OnSoundError;
                    if (onSoundError != null)
                        onSoundError(cue, string.Format("Unable to load audio file: '{0}', '{1}': {2}", cue.SubtypeId.ToString(), waveFileName, ex));
                }
            }

            return null;
        }
    }
}
