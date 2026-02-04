using OpenTK.Audio.OpenAL;
using NVorbis;
using System;
using System.Collections.Generic;
using System.IO;

public class AudioManager : IDisposable
{
    private readonly ALDevice _device;
    private readonly ALContext _context;
    private readonly Dictionary<string, List<int>> _buffers = new Dictionary<string, List<int>>();
    private readonly List<int> _sources = new List<int>();
    private readonly Random _rng = new Random();

    public AudioManager()
    {
        _device = ALC.OpenDevice(null);
        _context = ALC.CreateContext(_device, (int[])null);
        ALC.MakeContextCurrent(_context);

        AL.Listener(ALListener3f.Position, 0f, 0f, 0f);
        AL.Listener(ALListener3f.Velocity, 0f, 0f, 0f);
        AL.Listener(ALListenerfv.Orientation, new float[] { 0f, 0f, -1f, 0f, 1f, 0f });
    }

    public void LoadDefaults(string basePath)
    {
        LoadGroup("step_grass", basePath, "grass1.ogg", "grass2.ogg", "grass3.ogg", "grass4.ogg");
        LoadGroup("step_stone", basePath, "stone1.ogg", "stone2.ogg", "stone3.ogg", "stone4.ogg");
        LoadGroup("step_cloth", basePath, "cloth1.ogg", "cloth2.ogg", "cloth3.ogg", "cloth4.ogg");

        LoadGroup("dig_grass", basePath, "dig_grass1.ogg");
        LoadGroup("dig_stone", basePath, "dig_stone1.ogg");
        LoadGroup("dig_cloth", basePath, "dig_cloth1.ogg");

        LoadGroup("place_grass", basePath, "place_grass1.ogg");
        LoadGroup("place_stone", basePath, "place_stone1.ogg");
        LoadGroup("place_cloth", basePath, "place_cloth1.ogg");

        LoadGroup("ui_click", basePath, "click.ogg");
        LoadGroup("item_pop", basePath, "pop.ogg");
    }

    public bool PlayRandom(string key, float gain = 1.0f)
    {
        if (!_buffers.TryGetValue(key, out var list) || list.Count == 0) return false;
        int buffer = list[_rng.Next(list.Count)];

        int source = GetFreeSource();
        AL.Source(source, ALSourcei.Buffer, buffer);
        AL.Source(source, ALSourcef.Gain, gain);
        AL.Source(source, ALSource3f.Position, 0f, 0f, 0f);
        AL.SourcePlay(source);
        return true;
    }

    private int GetFreeSource()
    {
        for (int i = 0; i < _sources.Count; i++)
        {
            int src = _sources[i];
            AL.GetSource(src, ALGetSourcei.SourceState, out int state);
            if ((ALSourceState)state != ALSourceState.Playing) return src;
        }

        int newSource = AL.GenSource();
        _sources.Add(newSource);
        return newSource;
    }

    private void LoadGroup(string key, string basePath, params string[] files)
    {
        for (int i = 0; i < files.Length; i++)
        {
            string path = Path.Combine(basePath, files[i]);
            if (!File.Exists(path)) continue;
            int buffer = LoadOggToBuffer(path);
            if (!_buffers.TryGetValue(key, out var list))
            {
                list = new List<int>();
                _buffers[key] = list;
            }
            list.Add(buffer);
        }
    }

    private int LoadOggToBuffer(string path)
    {
        using VorbisReader vorbis = new VorbisReader(path);
        int channels = vorbis.Channels;
        int sampleRate = vorbis.SampleRate;

        int totalSamples = (int)(vorbis.TotalSamples * channels);
        float[] samples = new float[totalSamples];
        int read = vorbis.ReadSamples(samples, 0, samples.Length);

        short[] pcm = new short[read];
        for (int i = 0; i < read; i++)
        {
            float clamped = Math.Clamp(samples[i], -1f, 1f);
            pcm[i] = (short)(clamped * short.MaxValue);
        }

        ALFormat format = channels == 1 ? ALFormat.Mono16 : ALFormat.Stereo16;
        int buffer = AL.GenBuffer();
        AL.BufferData(buffer, format, pcm, sampleRate);
        return buffer;
    }

    public void Dispose()
    {
        for (int i = 0; i < _sources.Count; i++)
        {
            AL.SourceStop(_sources[i]);
            AL.DeleteSource(_sources[i]);
        }
        foreach (var list in _buffers.Values)
        {
            for (int i = 0; i < list.Count; i++)
            {
                AL.DeleteBuffer(list[i]);
            }
        }

        ALC.MakeContextCurrent(default(ALContext));
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
    }
}
