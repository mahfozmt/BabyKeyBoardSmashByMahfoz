using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AssetGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        string assetsRoot = Path.Combine(FindRepoRoot(), "src", "BabySmashBN", "Assets");
        string sfxDir = Path.Combine(assetsRoot, "Sounds", "Sfx");
        string voiceDir = Path.Combine(assetsRoot, "Sounds", "Voice");
        string emojiDir = Path.Combine(assetsRoot, "Emoji");

        Directory.CreateDirectory(sfxDir);
        Directory.CreateDirectory(voiceDir);
        Directory.CreateDirectory(emojiDir);

        Console.WriteLine("1. Downloading Missing Google Noto Emojis...");
        await DownloadMissingEmojis(emojiDir);

        Console.WriteLine("2. Synthesizing High-Quality Procedural SFX WAVs...");
        GenerateProceduralSfx(sfxDir);

        Console.WriteLine("3. Downloading Native Bangla Voice Clips (Letter + Word combo)...");
        await DownloadBanglaVoiceClips(voiceDir);

        Console.WriteLine("All assets are ready!");

        TestAudioPlayback(Path.Combine(assetsRoot, "Sounds"));
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && 
               !File.Exists(Path.Combine(dir.FullName, "BabyKeyBoardSmashByMahfoz.slnx")) && 
               !File.Exists(Path.Combine(dir.FullName, "KeyBoardSmashByMahfoz.slnx")) && 
               !Directory.Exists(Path.Combine(dir.FullName, "src")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? AppContext.BaseDirectory;
    }

    static void TestAudioPlayback(string baseDir)
    {
        Console.WriteLine("\n--- Testing Audio Devices ---");

        try
        {
            Console.WriteLine("Testing WaveOutEvent playback...");
            var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            var waveOut = new WaveOutEvent { DesiredLatency = 100 };
            var mixer = new MixingSampleProvider(targetFormat) { ReadFully = true };
            waveOut.Init(mixer);
            waveOut.Play();

            string testWav = Path.Combine(baseDir, "Sfx", "boing.wav");
            var csWav = LoadSound(testWav, targetFormat);
            Console.WriteLine($"Adding boing.wav ({csWav.Length} samples) to mixer...");
            mixer.AddMixerInput(new CachedSoundSampleProvider(new CachedSoundForTest(csWav, targetFormat)));
            System.Threading.Thread.Sleep(600); // Wait for playback
            Console.WriteLine("WaveOutEvent playback test completed.");
            waveOut.Stop();
            waveOut.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WaveOutEvent FAILED: {ex}");
        }

        try
        {
            Console.WriteLine("Testing WasapiOut playback...");
            var targetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            var wasapi = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 100);
            var mixer = new MixingSampleProvider(targetFormat) { ReadFully = true };
            wasapi.Init(mixer);
            wasapi.Play();

            string testWav = Path.Combine(baseDir, "Sfx", "chime.wav");
            var csWav = LoadSound(testWav, targetFormat);
            Console.WriteLine($"Adding chime.wav ({csWav.Length} samples) to Wasapi mixer...");
            mixer.AddMixerInput(new CachedSoundSampleProvider(new CachedSoundForTest(csWav, targetFormat)));
            System.Threading.Thread.Sleep(800); // Wait for playback
            Console.WriteLine("WasapiOut playback test completed.");
            wasapi.Stop();
            wasapi.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WasapiOut FAILED: {ex}");
        }

        try
        {
            string testWav = Path.Combine(baseDir, "Sfx", "pop.wav");
            string testMp3 = Path.Combine(baseDir, "Voice", "num_1.mp3");

            Console.WriteLine($"Testing AudioFileReader on {testWav}...");
            using (var r = new AudioFileReader(testWav))
            {
                Console.WriteLine($"WAV format: {r.WaveFormat}, Length: {r.Length}");
            }

            Console.WriteLine($"Testing AudioFileReader on {testMp3}...");
            using (var r = new AudioFileReader(testMp3))
            {
                Console.WriteLine($"MP3 format: {r.WaveFormat}, Length: {r.Length}");
            }

            Console.WriteLine("Testing CachedSound on testWav and testMp3...");
            var targetFmt = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            var csWav = LoadSound(testWav, targetFmt);
            Console.WriteLine($"Cached WAV samples: {csWav.Length}");
            var csMp3 = LoadSound(testMp3, targetFmt);
            Console.WriteLine($"Cached MP3 samples: {csMp3.Length}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FileReader FAILED: {ex}");
        }
    }

    static float[] LoadSound(string audioFileName, WaveFormat targetFormat)
    {
        using var reader = new AudioFileReader(audioFileName);
        ISampleProvider provider = reader;

        if (provider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
        {
            provider = new MonoToStereoSampleProvider(provider);
        }

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
        return wholeFile.ToArray();
    }

    class CachedSoundForTest
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }
        public CachedSoundForTest(float[] data, WaveFormat format)
        {
            AudioData = data;
            WaveFormat = format;
        }
    }

    class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSoundForTest _sound;
        private long _position;
        public CachedSoundSampleProvider(CachedSoundForTest sound) => _sound = sound;
        public WaveFormat WaveFormat => _sound.WaveFormat;
        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _sound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(_sound.AudioData, _position, buffer, offset, samplesToCopy);
            _position += samplesToCopy;
            return (int)samplesToCopy;
        }
    }

    static void GenerateProceduralSfx(string sfxDir)
    {
        // 1. Pop (bubble pop)
        WriteWav(Path.Combine(sfxDir, "pop.wav"), 0.12, (t, d) =>
        {
            double freq = 700.0 * Math.Exp(-t * 35.0) + 120.0;
            double phase = 2.0 * Math.PI * freq * t;
            double env = Math.Exp(-t * 28.0);
            return Math.Sin(phase) * env;
        });

        // 2. Boing (cartoon spring)
        WriteWav(Path.Combine(sfxDir, "boing.wav"), 0.45, (t, d) =>
        {
            double vibrato = Math.Sin(2.0 * Math.PI * 18.0 * t) * 45.0;
            double baseFreq = 220.0 + (t * 300.0) + vibrato;
            double phase = 2.0 * Math.PI * baseFreq * t;
            double env = Math.Pow(Math.Max(0.0, 1.0 - (t / d)), 1.5);
            return Math.Sin(phase) * env * 0.85;
        });

        // 3. Chime (magical sparkle)
        WriteWav(Path.Combine(sfxDir, "chime.wav"), 0.65, (t, d) =>
        {
            double env = Math.Exp(-t * 6.5);
            double w1 = Math.Sin(2.0 * Math.PI * 1046.5 * t); // C6
            double w2 = Math.Sin(2.0 * Math.PI * 1318.5 * t); // E6
            double w3 = Math.Sin(2.0 * Math.PI * 1567.98 * t); // G6
            double w4 = Math.Sin(2.0 * Math.PI * 2093.0 * t); // C7
            return (w1 * 0.4 + w2 * 0.3 + w3 * 0.2 + w4 * 0.1) * env;
        });

        // 4. Bell (clear resonant bell)
        WriteWav(Path.Combine(sfxDir, "bell.wav"), 0.70, (t, d) =>
        {
            double env = Math.Exp(-t * 5.0);
            double w1 = Math.Sin(2.0 * Math.PI * 880.0 * t);
            double w2 = Math.Sin(2.0 * Math.PI * 1760.0 * t) * 0.4;
            double w3 = Math.Sin(2.0 * Math.PI * 2640.0 * t) * 0.2;
            return (w1 + w2 + w3) * env * 0.7;
        });

        // 5. Spring
        WriteWav(Path.Combine(sfxDir, "spring.wav"), 0.35, (t, d) =>
        {
            double freq = 350.0 + Math.Sin(2.0 * Math.PI * 30.0 * t) * 120.0;
            double env = Math.Exp(-t * 7.0);
            return Math.Sin(2.0 * Math.PI * freq * t) * env * 0.8;
        });

        // 6. Drum (low thump)
        WriteWav(Path.Combine(sfxDir, "drum.wav"), 0.25, (t, d) =>
        {
            double freq = 120.0 * Math.Exp(-t * 20.0) + 40.0;
            double env = Math.Exp(-t * 12.0);
            return Math.Sin(2.0 * Math.PI * freq * t) * env;
        });

        // Xylophone scale (C4, D4, E4, F4, G4, A4, B4, C5)
        var xyloNotes = new Dictionary<string, double>
        {
            { "xylo_c1.wav", 261.63 },
            { "xylo_d.wav",  293.66 },
            { "xylo_e.wav",  329.63 },
            { "xylo_f.wav",  349.23 },
            { "xylo_g.wav",  392.00 },
            { "xylo_a.wav",  440.00 },
            { "xylo_b.wav",  493.88 },
            { "xylo_c2.wav", 523.25 }
        };

        foreach (var note in xyloNotes)
        {
            WriteWav(Path.Combine(sfxDir, note.Key), 0.55, (t, d) =>
            {
                double env = Math.Exp(-t * 8.0);
                double fundamental = Math.Sin(2.0 * Math.PI * note.Value * t);
                double overtone = Math.Sin(2.0 * Math.PI * (note.Value * 3.0) * t) * 0.25;
                return (fundamental + overtone) * env * 0.8;
            });
        }

        // 7. Slide Whistle (classic cartoon whoop up)
        WriteWav(Path.Combine(sfxDir, "slide_whistle.wav"), 0.50, (t, d) =>
        {
            double progress = t / d;
            double freq = 400.0 + Math.Pow(progress, 1.8) * 1200.0;
            double env = Math.Sin(Math.PI * progress);
            return Math.Sin(2.0 * Math.PI * freq * t) * env * 0.85;
        });

        // 8. Twang (cartoon jaw-harp bounce)
        WriteWav(Path.Combine(sfxDir, "twang.wav"), 0.40, (t, d) =>
        {
            double vib = Math.Sin(2.0 * Math.PI * 24.0 * t) * 60.0;
            double freq = 180.0 * Math.Exp(-t * 3.0) + vib;
            double env = Math.Exp(-t * 6.0);
            double h1 = Math.Sin(2.0 * Math.PI * freq * t);
            double h2 = Math.Sin(2.0 * Math.PI * (freq * 2.0) * t) * 0.5;
            double h3 = Math.Sin(2.0 * Math.PI * (freq * 3.0) * t) * 0.25;
            return (h1 + h2 + h3) * env * 0.85;
        });

        // 9. Squeak (rubber toy squeak)
        WriteWav(Path.Combine(sfxDir, "squeak.wav"), 0.22, (t, d) =>
        {
            double freq = 1200.0 + Math.Sin(2.0 * Math.PI * 14.0 * t) * 350.0;
            double env = Math.Sin(Math.PI * (t / d));
            return Math.Sin(2.0 * Math.PI * freq * t) * env * 0.75;
        });

        // 10. Wobble (goofy jelly wobble)
        WriteWav(Path.Combine(sfxDir, "wobble.wav"), 0.45, (t, d) =>
        {
            double wobbleFreq = 260.0 + Math.Sin(2.0 * Math.PI * 10.0 * t) * 90.0;
            double tremolo = 0.5 + 0.5 * Math.Sin(2.0 * Math.PI * 20.0 * t);
            double env = Math.Pow(1.0 - (t / d), 1.2);
            return Math.Sin(2.0 * Math.PI * wobbleFreq * t) * tremolo * env * 0.9;
        });

        // 11. Quack (funny cartoon honk)
        WriteWav(Path.Combine(sfxDir, "quack.wav"), 0.30, (t, d) =>
        {
            double f1 = 450.0;
            double f2 = 900.0;
            double env = Math.Sin(Math.PI * (t / d));
            double tone = Math.Sin(2.0 * Math.PI * f1 * t) * 0.6 + Math.Sin(2.0 * Math.PI * f2 * t) * 0.4;
            // Add a little harshness for cartoon quack
            tone = Math.Max(-0.8, Math.Min(0.8, tone * 1.5));
            return tone * env * 0.8;
        });
    }

    static void WriteWav(string path, double durationSeconds, Func<double, double, double> generator, int sampleRate = 44100)
    {
        int totalSamples = (int)(sampleRate * durationSeconds);
        int dataSize = totalSamples * 2; // 16-bit mono

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        // RIFF header
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(dataSize + 36);
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // chunk size
        bw.Write((short)1); // PCM
        bw.Write((short)1); // mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // byte rate
        bw.Write((short)2); // block align
        bw.Write((short)16); // bits per sample

        // data chunk
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(dataSize);

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / sampleRate;
            double val = generator(t, durationSeconds);
            if (val > 1.0) val = 1.0;
            else if (val < -1.0) val = -1.0;
            short sample = (short)(val * 32767.0);
            bw.Write(sample);
        }

        Console.WriteLine($"  Created: {Path.GetFileName(path)}");
    }

    static async Task DownloadMissingEmojis(string emojiDir)
    {
        var emojis = new Dictionary<string, string>
        {
            { "snake.png", "emoji_u1f40d.png" },
            { "book.png", "emoji_u1f4d6.png" },
            { "moon.png", "emoji_u1f319.png" },
            { "bird.png", "emoji_u1f426.png" },
            { "ship.png", "emoji_u1f6a2.png" },
            { "boat.png", "emoji_u26f5.png" },
            { "camel.png", "emoji_u1f42b.png" },
            { "shell.png", "emoji_u1f41a.png" },
            { "carrot.png", "emoji_u1f955.png" }
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        foreach (var (filename, codepointFile) in emojis)
        {
            string dest = Path.Combine(emojiDir, filename);
            if (File.Exists(dest) && new FileInfo(dest).Length > 500) continue;

            string url = $"https://raw.githubusercontent.com/googlefonts/noto-emoji/main/png/128/{codepointFile}";
            try
            {
                byte[] bytes = await client.GetByteArrayAsync(url);
                if (bytes.Length > 200)
                {
                    await File.WriteAllBytesAsync(dest, bytes);
                    Console.WriteLine($"  Emoji: {filename} ({bytes.Length} bytes)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed downloading emoji {filename}: {ex.Message}");
            }
        }
    }

    static async Task DownloadBanglaVoiceClips(string voiceDir)
    {
        var voiceItems = new Dictionary<string, string>
        {
            // Numerals 0-9
            { "num_0.mp3", "শূন্য" },
            { "num_1.mp3", "এক" },
            { "num_2.mp3", "দুই" },
            { "num_3.mp3", "তিন" },
            { "num_4.mp3", "চার" },
            { "num_5.mp3", "পাঁচ" },
            { "num_6.mp3", "ছয়" },
            { "num_7.mp3", "সাত" },
            { "num_8.mp3", "আট" },
            { "num_9.mp3", "নয়" },

            // Letters A-Z (Letter + Associated Word combo)
            { "let_a.mp3", "অ, অজগর" },
            { "let_b.mp3", "ব, বই" },
            { "let_c.mp3", "চ, চাঁদ" },
            { "let_d.mp3", "দ, দোয়েল" },
            { "let_e.mp3", "এ, একতারা" },
            { "let_f.mp3", "ফ, ফুল" },
            { "let_g.mp3", "গ, গোলাপ" },
            { "let_h.mp3", "হ, হাতি" },
            { "let_i.mp3", "ই, ইলিশ" },
            { "let_j.mp3", "জ, জাহাজ" },
            { "let_k.mp3", "ক, কলা" },
            { "let_l.mp3", "ল, লিচু" },
            { "let_m.mp3", "ম, মাছ" },
            { "let_n.mp3", "ন, নৌকা" },
            { "let_o.mp3", "ও, ওলকপি" },
            { "let_p.mp3", "প, পাখি" },
            { "let_q.mp3", "ৎ, কৈতব" },
            { "let_r.mp3", "র, রংধনু" },
            { "let_s.mp3", "শ, সিংহ" },
            { "let_t.mp3", "ট, টিয়া" },
            { "let_u.mp3", "উ, উট" },
            { "let_v.mp3", "ভ, ভালুক" },
            { "let_w.mp3", "ঐ, ঐরাবত" },
            { "let_x.mp3", "ক্ষ, ক্ষীর" },
            { "let_y.mp3", "য়, ময়না" },
            { "let_z.mp3", "ঝ, ঝিনুক" },

            // Celebratory clips
            { "cheer_1.mp3", "দারুণ" },
            { "cheer_2.mp3", "সাবাশ" },
            { "cheer_3.mp3", "বাহ্" }
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        foreach (var kvp in voiceItems)
        {
            string dest = Path.Combine(voiceDir, kvp.Key);
            // Re-download let_* files to get the full letter+word pronunciation
            if (File.Exists(dest) && !kvp.Key.StartsWith("let_") && new FileInfo(dest).Length > 500)
            {
                continue;
            }

            string encoded = Uri.EscapeDataString(kvp.Value);
            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&q={encoded}&tl=bn&client=tw-ob";

            try
            {
                byte[] bytes = await client.GetByteArrayAsync(url);
                if (bytes.Length > 200)
                {
                    await File.WriteAllBytesAsync(dest, bytes);
                    Console.WriteLine($"  Voice: {kvp.Key} ({kvp.Value}) - {bytes.Length} bytes");
                }
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Failed {kvp.Key}: {ex.Message}");
            }
        }
    }
}
