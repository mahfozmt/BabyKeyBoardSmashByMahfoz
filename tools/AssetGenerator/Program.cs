using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AssetGenerator;

class Program
{
    static async Task Main(string[] args)
    {
        string baseDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../src/BabySmashBN/Assets/Sounds"));
        string sfxDir = Path.Combine(baseDir, "Sfx");
        string voiceDir = Path.Combine(baseDir, "Voice");

        Directory.CreateDirectory(sfxDir);
        Directory.CreateDirectory(voiceDir);

        Console.WriteLine("1. Synthesizing High-Quality Procedural SFX WAVs...");
        GenerateProceduralSfx(sfxDir);

        Console.WriteLine("2. Downloading Native Bangla Voice Clips...");
        await DownloadBanglaVoiceClips(voiceDir);

        Console.WriteLine("All audio assets are ready!");
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

            // Letters A-Z
            { "let_a.mp3", "অ" },
            { "let_b.mp3", "ব" },
            { "let_c.mp3", "চ" },
            { "let_d.mp3", "দ" },
            { "let_e.mp3", "এ" },
            { "let_f.mp3", "ফ" },
            { "let_g.mp3", "গ" },
            { "let_h.mp3", "হ" },
            { "let_i.mp3", "ই" },
            { "let_j.mp3", "জ" },
            { "let_k.mp3", "ক" },
            { "let_l.mp3", "ল" },
            { "let_m.mp3", "ম" },
            { "let_n.mp3", "ন" },
            { "let_o.mp3", "ও" },
            { "let_p.mp3", "প" },
            { "let_q.mp3", "ৎ" },
            { "let_r.mp3", "র" },
            { "let_s.mp3", "শ" },
            { "let_t.mp3", "ট" },
            { "let_u.mp3", "উ" },
            { "let_v.mp3", "ভ" },
            { "let_w.mp3", "ঐ" },
            { "let_x.mp3", "ক্ষ" },
            { "let_y.mp3", "য়" },
            { "let_z.mp3", "ঝ" },

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
            if (File.Exists(dest) && new FileInfo(dest).Length > 500)
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
