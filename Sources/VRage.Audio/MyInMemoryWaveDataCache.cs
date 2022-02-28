using SharpDX;
using SharpDX.Multimedia;
using VRage.FileSystem;
using VRage.Library.Memory;
using VRage.Library.Threading;

namespace VRage.Audio
{
    public sealed class MyInMemoryWaveDataCache
    {
        public static MyInMemoryWaveDataCache Static = new MyInMemoryWaveDataCache();
        private MyMemorySystem MemorySystem = Singleton<MyMemoryTracker>.Instance.ProcessMemorySystem.RegisterSubsystem("Audio");
        private SpinLockRef Lock = new SpinLockRef();
        private Dictionary<string, CacheData> Cache = new Dictionary<string, CacheData>(StringComparer.InvariantCultureIgnoreCase);

        public CacheData Get(string path, out bool owns)
        {
            MakePath(ref path);
            using (Lock.Acquire())
            {
                CacheData cacheData;
                if (Cache.TryGetValue(path, out cacheData))
                {
                    owns = false;
                    return cacheData;
                }
            }

            owns = true;

            return Load(path);
        }

        public void Preload(string path)
        {
            MakePath(ref path);
            using (Lock.Acquire())
            {
                if (Cache.ContainsKey(path))
                    return;

                Cache.Add(path, new CacheData());
            }
            CacheData cacheData = Load(path);
            using (Lock.Acquire())
                Cache[path] = cacheData;
        }

        private CacheData Load(string path)
        {
            using (Stream stream = MyFileSystem.OpenRead(path))
            {
                CacheData cacheData = new CacheData();
                if (stream != null)
                {
                    SoundStream soundStream = new SoundStream(stream);
                    cacheData = new CacheData(soundStream, path);
                    soundStream.Close();
                }

                return cacheData;
            }
        }

        public CacheData LoadCached(string path)
        {
            MakePath(ref path);
            using (Lock.Acquire())
            {
                CacheData cacheData;
                if (Cache.TryGetValue(path, out cacheData))
                    return cacheData;

                cacheData = Load(path);
                Cache.Add(path, cacheData);

                return cacheData;
            }
        }

        private void MakePath(ref string path)
        {
            if (Path.IsPathRooted(path))
                return;

            path = Path.Combine(MyFileSystem.ContentPath, "Audio", path);
        }

        public void Dispose()
        {
            using (Lock.Acquire())
            {
                foreach (KeyValuePair<string, CacheData> keyValuePair in Cache)
                    keyValuePair.Value.Dispose();
                Cache.Clear();
            }
        }

        public struct CacheData : IDisposable
        {
            public readonly DataStream DataStream;
            public readonly SoundStream SoundStream;
            public MyMemorySystem.AllocationRecord? AllocationRecord;

            public CacheData(SoundStream soundStream, string debugName)
            {
                SoundStream = soundStream;
                DataStream = soundStream.ToDataStream();
                AllocationRecord = new MyMemorySystem.AllocationRecord?(Static.MemorySystem.RegisterAllocation(debugName, soundStream.Length));
            }

            public void Dispose()
            {
                DataStream?.Dispose();
                ref MyMemorySystem.AllocationRecord? local = ref AllocationRecord;
                if (!local.HasValue)
                    return;
                local.GetValueOrDefault().Dispose();
            }
        }
    }
}
