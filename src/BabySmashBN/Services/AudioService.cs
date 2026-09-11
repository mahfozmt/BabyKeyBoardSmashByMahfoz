using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BabySmashBN.Services;

public class CachedSound
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }

    public CachedSound(string audioFileName, WaveFormat targetFormat)
    {
        WaveFormat = targetFormat;

        using var reader = new AudioFileReader(audioFileName);
        ISampleProvider provider = reader;

        // Convert mono to stereo if needed
        if (provider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }

        // Resample if sample rate doesn't match
        if (provider.WaveFormat.SampleRate != targetFormat.SampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, targetFormat.SampleRate);
        }

        var wholeFile = new List<float>((int)(reader.Length / 4));
        var readBuffer = new float[targetFormat.SampleRate * targetFormat.Channels];
        int samplesRead;
        while ((samplesRead = provider.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i++)
            {
                wholeFile.Add(readBuffer[i]);
            }
        }

        AudioData = wholeFile.ToArray();
    }
}

public class CachedSoundSampleProvider : ISampleProvider
{
    private readonly CachedSound _cachedSound;
    private long _position;

    public CachedSoundSampleProvider(CachedSound cachedSound)
    {
        _cachedSound = cachedSound;
    }

    public WaveFormat WaveFormat => _cachedSound.WaveFormat;

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = _cachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(_cachedSound.AudioData, _position, buffer, offset, samplesToCopy);
        _position += samplesToCopy;
        return (int)samplesToCopy;
    }
}

public class AudioService : IAudioService, IDisposable
{
    private readonly WaveFormat _targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    private readonly IWavePlayer? _outputDevice;
    private readonly MixingSampleProvider? _mixer;
    private readonly ConcurrentDictionary<string, CachedSound> _soundCache = new();
    private bool _disposed;

    public bool PlaySfxEnabled { get; set; } = true;
    public bool PlayVoiceEnabled { get; set; } = true;

    public AudioService()
    {
        try
        {
            var waveOut = new WaveOutEvent { DesiredLatency = 80 };
            _mixer = new MixingSampleProvider(_targetFormat)
            {
                ReadFully = true
            };
            waveOut.Init(_mixer);
            waveOut.Play();
            _outputDevice = waveOut;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Audio output initialization failed: {ex.Message}");
        }
    }

    public void PlaySfx(string? sfxPath)
    {
        if (!PlaySfxEnabled || string.IsNullOrEmpty(sfxPath)) return;
        PlayCached(sfxPath);
    }

    public void PlayVoice(string? voicePath)
    {
        if (!PlayVoiceEnabled || string.IsNullOrEmpty(voicePath)) return;
        PlayCached(voicePath);
    }

    public void PlaySmash(string? sfxPath, string? voicePath)
    {
        if (PlaySfxEnabled && !string.IsNullOrEmpty(sfxPath))
        {
            PlayCached(sfxPath);
        }

        if (PlayVoiceEnabled && !string.IsNullOrEmpty(voicePath))
        {
            PlayCached(voicePath);
        }
    }

    private void PlayCached(string filePath)
    {
        if (_mixer == null || !File.Exists(filePath)) return;

        try
        {
            var sound = _soundCache.GetOrAdd(filePath, path => new CachedSound(path, _targetFormat));
            _mixer.AddMixerInput(new CachedSoundSampleProvider(sound));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error playing sound {filePath}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _outputDevice?.Stop();
                _outputDevice?.Dispose();
            }
            catch { }
            _disposed = true;
        }
    }
}
