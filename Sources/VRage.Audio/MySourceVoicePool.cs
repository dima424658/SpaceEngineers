// Decompiled with JetBrains decompiler
// Type: VRage.Audio.MySourceVoicePool
// Assembly: VRage.Audio, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: EC719E5D-7DA7-4B71-BF8F-7E4B42BD26A2
// Assembly location: D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64\VRage.Audio.dll

using SharpDX.Multimedia;
using SharpDX.XAudio2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using VRage.Collections;
using VRage.Data.Audio;
using VRage.Utils;

namespace VRage.Audio
{
  internal class MySourceVoicePool
  {
    private SharpDX.XAudio2.XAudio2 m_audioEngine;
    private readonly WaveFormat m_waveFormat;
    private MyCueBank m_owner;
    private readonly MyConcurrentQueue<MySourceVoice> m_voicesToRecycle;
    private readonly MyConcurrentQueue<MySourceVoice> m_availableVoices;
    private readonly List<MySourceVoicePool.FadeoutData> m_fadingOutVoices;
    private readonly VoiceSendDescriptor[] m_desc;
    public bool UseSameSoundLimiter;
    private int m_currentCount;
    private const int MAX_COUNT = 128;
    private readonly ConcurrentDictionary<MySourceVoice, byte> m_allVoices = new ConcurrentDictionary<MySourceVoice, byte>();
    private readonly List<MySourceVoice> m_voicesToRemove = new List<MySourceVoice>();
    private readonly List<MySourceVoice> m_distancedVoices = new List<MySourceVoice>();
    private readonly List<MySourceVoice> m_voicesToDispose = new List<MySourceVoice>();

    public event AudioEngineChanged OnAudioEngineChanged;

    public WaveFormat WaveFormat => this.m_waveFormat;

    public MySourceVoicePool(
      SharpDX.XAudio2.XAudio2 audioEngine,
      WaveFormat waveformat,
      MyCueBank owner,
      VoiceSendDescriptor[] desc = null)
    {
      this.m_audioEngine = audioEngine;
      this.m_waveFormat = waveformat;
      this.m_owner = owner;
      this.m_voicesToRecycle = new MyConcurrentQueue<MySourceVoice>(20);
      this.m_availableVoices = new MyConcurrentQueue<MySourceVoice>(128);
      this.m_fadingOutVoices = new List<MySourceVoicePool.FadeoutData>();
      this.m_currentCount = 0;
      this.m_desc = desc;
    }

    public void SetAudioEngine(SharpDX.XAudio2.XAudio2 audioEngine)
    {
      if (this.m_audioEngine == audioEngine)
        return;
      AudioEngineChanged audioEngineChanged = this.OnAudioEngineChanged;
      if (audioEngineChanged != null)
        audioEngineChanged();
      this.m_audioEngine = audioEngine;
      this.m_voicesToRecycle.Clear();
      this.m_availableVoices.Clear();
      this.m_fadingOutVoices.Clear();
      this.m_currentCount = 0;
    }

    internal MySourceVoice NextAvailable()
    {
      if (this.m_audioEngine == null)
        return (MySourceVoice) null;
      MySourceVoice instance = (MySourceVoice) null;
      if ((this.m_owner.DisablePooling || !this.m_availableVoices.TryDequeue(out instance)) && this.m_allVoices.Count < 128)
      {
        instance = new MySourceVoice(this, this.m_audioEngine, this.m_waveFormat);
        if (this.m_desc != null)
          instance.SetOutputVoices(this.m_desc);
        this.m_allVoices.TryAdd(instance, (byte) 0);
        ++this.m_currentCount;
      }
      return instance;
    }

    public void OnStopPlaying(MySourceVoice voice)
    {
      --this.m_currentCount;
      this.m_voicesToRecycle.Enqueue(voice);
    }

    public void Update()
    {
      if (this.m_owner == null || this.m_audioEngine == null)
        return;
      if (this.m_owner.DisablePooling)
      {
        foreach (MySourceVoice mySourceVoice in this.m_voicesToDispose)
          mySourceVoice.Dispose();
        this.m_voicesToDispose.Clear();
        MySourceVoice instance;
        while (this.m_voicesToRecycle.TryDequeue(out instance))
        {
          instance.CleanupBeforeDispose();
          this.m_voicesToDispose.Add(instance);
        }
      }
      else
      {
        MySourceVoice instance;
        while (this.m_voicesToRecycle.TryDequeue(out instance))
        {
          instance.Emitter = (IMy3DSoundEmitter) null;
          this.m_availableVoices.Enqueue(instance);
        }
      }
      int index1 = 0;
      while (index1 < this.m_fadingOutVoices.Count)
      {
        MySourceVoicePool.FadeoutData fadingOutVoice = this.m_fadingOutVoices[index1];
        MySourceVoice voice = fadingOutVoice.Voice;
        if (!voice.IsValid)
        {
          this.m_fadingOutVoices.RemoveAt(index1);
        }
        else
        {
          if (fadingOutVoice.RemainingSteps == 0)
          {
            voice.Voice.Stop();
            voice.Voice.FlushSourceBuffers();
            this.m_fadingOutVoices.RemoveAt(index1);
            continue;
          }
          float volumeRef;
          voice.Voice.GetVolume(out volumeRef);
          voice.Voice.SetVolume(volumeRef * 0.65f);
          --fadingOutVoice.RemainingSteps;
          this.m_fadingOutVoices[index1] = fadingOutVoice;
        }
        ++index1;
      }
      this.m_voicesToRemove.Clear();
      foreach (MySourceVoice key in (IEnumerable<MySourceVoice>) this.m_allVoices.Keys)
      {
        if (!key.IsValid)
          this.m_voicesToRemove.Add(key);
      }
      while (this.m_voicesToRemove.Count > 0)
      {
        this.m_allVoices.Remove<MySourceVoice, byte>(this.m_voicesToRemove[0]);
        this.m_voicesToRemove.RemoveAt(0);
      }
      if (!this.UseSameSoundLimiter)
        return;
      this.m_distancedVoices.Clear();
      foreach (MySourceVoice key in (IEnumerable<MySourceVoice>) this.m_allVoices.Keys)
        this.m_distancedVoices.Add(key);
      this.m_distancedVoices.Sort((Comparison<MySourceVoice>) ((x, y) => x.DistanceToListener.CompareTo(y.DistanceToListener)));
      while (this.m_distancedVoices.Count > 0)
      {
        MyCueId cueEnum = this.m_distancedVoices[0].CueEnum;
        int num1 = 0;
        MySoundData cue = MyAudio.Static.GetCue(cueEnum);
        int num2 = cue != null ? cue.SoundLimit : 0;
        for (int index2 = 0; index2 < this.m_distancedVoices.Count; ++index2)
        {
          if (this.m_distancedVoices[index2].CueEnum.Equals(cueEnum))
          {
            ++num1;
            this.m_distancedVoices[index2].Silent = num2 > 0 && num1 > num2;
            this.m_distancedVoices.RemoveAt(index2);
            --index2;
          }
        }
      }
    }

    public void AddToFadeoutList(MySourceVoice voice) => this.m_fadingOutVoices.Add(new MySourceVoicePool.FadeoutData(voice));

    public void StopAll()
    {
      foreach (MySourceVoice key in (IEnumerable<MySourceVoice>) this.m_allVoices.Keys)
        key.Stop(true);
    }

    public void Dispose()
    {
      this.m_availableVoices.Clear();
      this.m_fadingOutVoices.Clear();
      this.m_currentCount = 0;
      this.m_audioEngine = (SharpDX.XAudio2.XAudio2) null;
      this.m_owner = (MyCueBank) null;
      foreach (MySourceVoice key in (IEnumerable<MySourceVoice>) this.m_allVoices.Keys)
      {
        key.CleanupBeforeDispose();
        this.m_voicesToDispose.Add(key);
      }
      this.m_allVoices.Clear();
      foreach (MySourceVoice mySourceVoice in this.m_voicesToDispose)
        mySourceVoice.Dispose();
      this.m_voicesToDispose.Clear();
    }

    public void WritePlayingDebugInfo(StringBuilder stringBuilder)
    {
    }

    public void WritePausedDebugInfo(StringBuilder stringBuilder)
    {
    }

    private struct FadeoutData
    {
      public const float TargetVolume = 0.01f;
      public const float VolumeMultiplierPerStep = 0.65f;
      public int RemainingSteps;
      public readonly MySourceVoice Voice;

      public FadeoutData(MySourceVoice voice)
      {
        if (voice == null || voice.Voice == null)
        {
          MyLog.Default.WriteLine("FadeoutData initialized with " + (voice == null ? "MySourceVoice == null." : "MySourceVoice having SourceVoice null."));
          this.Voice = voice;
          this.RemainingSteps = 0;
        }
        else
        {
          this.Voice = voice;
          float volumeRef;
          voice.Voice.GetVolume(out volumeRef);
          if ((double) volumeRef <= 0.00999999977648258)
            this.RemainingSteps = 0;
          else
            this.RemainingSteps = (int) Math.Log(0.00999999977648258 / (double) volumeRef, 0.649999976158142);
        }
      }
    }
  }
}
