using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Windows.Media;
using NAudio.CoreAudioApi;
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
        else if (provider.WaveFormat.Channels == 2 && targetFormat.Channels == 1)
        {
            provider = new MultiplexingSampleProvider(new[] { provider }, 1);
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
    // Fixed standard mixing format for all cached audio: 44.1kHz Stereo 32-bit Float
    private readonly WaveFormat _mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
    private readonly IWavePlayer? _outputDevice;
    private readonly MixingSampleProvider? _mixer;
    private readonly ConcurrentDictionary<string, CachedSound> _soundCache = new();
    private readonly ConcurrentDictionary<string, SoundPlayer> _fallbackWavPlayers = new();
    private readonly ConcurrentDictionary<MediaPlayer, byte> _activeMediaPlayers = new();
    private readonly string _logFile;
    private bool _disposed;

    public bool PlaySfxEnabled { get; set; } = true;
    public bool PlayVoiceEnabled { get; set; } = true;

    public AudioService()
    {
        _logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "audio_debug.log");
        Log("=== AudioService Initializing ===");

        _mixer = new MixingSampleProvider(_mixerFormat)
        {
            ReadFully = true
        };

        _outputDevice = InitializeAudioDevice(_mixer);
        Log($"Audio output driver active: {(_outputDevice != null ? _outputDevice.GetType().Name : "Tier 4 Native Fallback")}");
    }

    private IWavePlayer? InitializeAudioDevice(ISampleProvider mixerProvider)
    {
        // Tier 1: WASAPI Shared Mode (Standard for modern Windows 10 & 11)
        try
        {
            Log("Attempting Tier 1: WasapiOut (Shared mode)...");
            var wasapi = new WasapiOut(AudioClientShareMode.Shared, 100);
            var deviceFormat = wasapi.OutputWaveFormat;
            Log($"WASAPI hardware endpoint format: {deviceFormat.SampleRate}Hz, {deviceFormat.Channels} channels");

            ISampleProvider downstreamProvider = mixerProvider;
            if (deviceFormat.SampleRate != _mixerFormat.SampleRate)
            {
                Log($"Adapting mixer sample rate: {_mixerFormat.SampleRate}Hz -> {deviceFormat.SampleRate}Hz");
                downstreamProvider = new WdlResamplingSampleProvider(mixerProvider, deviceFormat.SampleRate);
            }

            wasapi.Init(downstreamProvider);
            wasapi.Play();
            Log("WasapiOut initialized and playing successfully.");
            return wasapi;
        }
        catch (Exception ex1)
        {
            Log($"Tier 1 (WasapiOut) failed: {ex1.Message}");
        }

        // Tier 2: DirectSoundOut
        try
        {
            Log("Attempting Tier 2: DirectSoundOut...");
            var ds = new DirectSoundOut(100);
            ds.Init(mixerProvider);
            ds.Play();
            Log("DirectSoundOut initialized and playing successfully.");
            return ds;
        }
        catch (Exception ex2)
        {
            Log($"Tier 2 (DirectSoundOut) failed: {ex2.Message}");
        }

        // Tier 3: WaveOutEvent (WinMM MME fallback)
        try
        {
            Log("Attempting Tier 3: WaveOutEvent...");
            var waveOut = new WaveOutEvent { DesiredLatency = 120 };
            waveOut.Init(mixerProvider);
            waveOut.Play();
            Log("WaveOutEvent initialized and playing successfully.");
            return waveOut;
        }
        catch (Exception ex3)
        {
            Log($"Tier 3 (WaveOutEvent) failed: {ex3.Message}");
        }

        Log("All NAudio players failed. Operating in Tier 4 (Native Windows SoundPlayer & MediaPlayer).");
        return null;
    }

    public void PlaySfx(string? sfxPath)
    {
        if (!PlaySfxEnabled || string.IsNullOrEmpty(sfxPath)) return;
        PlaySoundFile(sfxPath);
    }

    public void PlayVoice(string? voicePath)
    {
        if (!PlayVoiceEnabled || string.IsNullOrEmpty(voicePath)) return;
        PlaySoundFile(voicePath);
    }

    public void PlaySmash(string? sfxPath, string? voicePath)
    {
        if (PlaySfxEnabled && !string.IsNullOrEmpty(sfxPath))
        {
            PlaySoundFile(sfxPath);
        }

        if (PlayVoiceEnabled && !string.IsNullOrEmpty(voicePath))
        {
            PlaySoundFile(voicePath);
        }
    }

    private void PlaySoundFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log($"Warning: sound file does not exist: {filePath}");
            return;
        }

        // Primary Route: NAudio Realtime Mixer
        if (_mixer != null && _outputDevice != null)
        {
            try
            {
                var sound = _soundCache.GetOrAdd(filePath, path => new CachedSound(path, _mixerFormat));
                _mixer.AddMixerInput(new CachedSoundSampleProvider(sound));
                return;
            }
            catch (Exception ex)
            {
                Log($"NAudio mixer error playing {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        // Fallback Route: Native Windows Media Subsystems
        try
        {
            if (filePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                var player = _fallbackWavPlayers.GetOrAdd(filePath, path =>
                {
                    var sp = new SoundPlayer(path);
                    sp.Load();
                    return sp;
                });
                player.Play();
            }
            else
            {
                var media = new MediaPlayer();
                _activeMediaPlayers.TryAdd(media, 0);

                media.MediaEnded += (s, e) =>
                {
                    media.Close();
                    _activeMediaPlayers.TryRemove(media, out _);
                };
                media.MediaFailed += (s, e) =>
                {
                    Log($"MediaPlayer failed on {Path.GetFileName(filePath)}: {e.ErrorException?.Message}");
                    media.Close();
                    _activeMediaPlayers.TryRemove(media, out _);
                };

                media.Open(new Uri(filePath, UriKind.Absolute));
                media.Play();
            }
        }
        catch (Exception exFallback)
        {
            Log($"Native fallback playback failed on {Path.GetFileName(filePath)}: {exFallback.Message}");
        }
    }

    private void Log(string message)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            File.AppendAllText(_logFile, line);
            System.Diagnostics.Debug.WriteLine(message);
        }
        catch { }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                _outputDevice?.Stop();
                _outputDevice?.Dispose();

                foreach (var player in _fallbackWavPlayers.Values)
                {
                    player.Dispose();
                }

                foreach (var media in _activeMediaPlayers.Keys)
                {
                    media.Close();
                }
            }
            catch { }
            _disposed = true;
        }
    }
}
