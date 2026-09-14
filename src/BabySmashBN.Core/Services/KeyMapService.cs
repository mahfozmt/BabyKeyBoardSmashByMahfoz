using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using BabySmashBN.Models;

namespace BabySmashBN.Services;

public class KeyMapService : IKeyMapService
{
    private readonly Random _random = new();
    private readonly string _assetsDir;
    private readonly List<string> _emojiFiles = new();
    private readonly List<string> _sfxFiles = new();
    private readonly List<string> _funnySfxFiles = new();

    private readonly Dictionary<string, (string Glyph, string? Word, string? Voice, string? Emoji)> _digitMap = new();
    private readonly Dictionary<string, (string Glyph, string? Word, string? Voice, string? Emoji)> _letterMap = new();

    private static readonly (string Type, string NameBn)[] SupportedShapes = new[]
    {
        ("Star", "তারা"),
        ("Heart", "হৃদয়"),
        ("Circle", "বৃত্ত"),
        ("Triangle", "ত্রিভুজ"),
        ("Square", "চতুর্ভুজ"),
        ("Diamond", "হীরা"),
        ("Moon", "চাঁদ"),
        ("Sun", "সূর্য"),
        ("Cloud", "মেঘ")
    };

    private static readonly string[] FunnySoundNames = new[]
    {
        "slide_whistle.wav",
        "twang.wav",
        "squeak.wav",
        "wobble.wav",
        "quack.wav",
        "boing.wav",
        "spring.wav",
        "pop.wav",
        "drum.wav",
        "bell.wav"
    };

    private static readonly Color[] Palette = new[]
    {
        Color.FromRgb(255, 82, 82),   // Coral Red
        Color.FromRgb(255, 214, 0),   // Sunflower Yellow
        Color.FromRgb(0, 176, 255),   // Vivid Azure
        Color.FromRgb(118, 255, 3),   // Electric Lime
        Color.FromRgb(224, 64, 251),  // Vibrant Purple
        Color.FromRgb(255, 109, 0),   // Deep Orange
        Color.FromRgb(29, 233, 182),  // Mint Turquoise
        Color.FromRgb(255, 171, 0),   // Warm Amber
        Color.FromRgb(255, 64, 129),  // Bubblegum Pink
        Color.FromRgb(24, 255, 255)   // Bright Cyan
    };

    public KeyMapService(string? assetsDir = null)
    {
        _assetsDir = assetsDir ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        LoadKeymapJson();
        LoadAssetInventories();
    }

    private void LoadKeymapJson()
    {
        // 1. Hardcoded default mappings (ensure reliability)
        var defaultDigits = new Dictionary<string, (string, string?, string?, string?)>
        {
            { "D0", ("০", "শূন্য", "num_0.mp3", "sparkles.png") },
            { "D1", ("১", "এক", "num_1.mp3", "apple.png") },
            { "D2", ("২", "দুই", "num_2.mp3", "star.png") },
            { "D3", ("৩", "তিন", "num_3.mp3", "balloon.png") },
            { "D4", ("৪", "চার", "num_4.mp3", "strawberry.png") },
            { "D5", ("৫", "পাঁচ", "num_5.mp3", "crown.png") },
            { "D6", ("৬", "ছয়", "num_6.mp3", "soccer.png") },
            { "D7", ("৭", "সাত", "num_7.mp3", "rainbow.png") },
            { "D8", ("৮", "আট", "num_8.mp3", "bear.png") },
            { "D9", ("৯", "নয়", "num_9.mp3", "rocket.png") },
            { "NumPad0", ("০", "শূন্য", "num_0.mp3", "sparkles.png") },
            { "NumPad1", ("১", "এক", "num_1.mp3", "apple.png") },
            { "NumPad2", ("২", "দুই", "num_2.mp3", "star.png") },
            { "NumPad3", ("৩", "তিন", "num_3.mp3", "balloon.png") },
            { "NumPad4", ("৪", "চার", "num_4.mp3", "strawberry.png") },
            { "NumPad5", ("৫", "পাঁচ", "num_5.mp3", "crown.png") },
            { "NumPad6", ("৬", "ছয়", "num_6.mp3", "soccer.png") },
            { "NumPad7", ("৭", "সাত", "num_7.mp3", "rainbow.png") },
            { "NumPad8", ("৮", "আট", "num_8.mp3", "bear.png") },
            { "NumPad9", ("৯", "নয়", "num_9.mp3", "rocket.png") }
        };

        foreach (var kvp in defaultDigits) _digitMap[kvp.Key] = kvp.Value;

        var defaultLetters = new Dictionary<string, (string, string?, string?, string?)>
        {
            { "A", ("অ", "অজগর", "let_a.mp3", "snake.png") },
            { "B", ("ব", "বই", "let_b.mp3", "book.png") },
            { "C", ("চ", "চাঁদ", "let_c.mp3", "moon.png") },
            { "D", ("দ", "দোয়েল", "let_d.mp3", "bird.png") },
            { "E", ("এ", "একতারা", "let_e.mp3", "guitar.png") },
            { "F", ("ফ", "ফুল", "let_f.mp3", "flower.png") },
            { "G", ("গ", "গোলাপ", "let_g.mp3", "flower.png") },
            { "H", ("হ", "হাতি", "let_h.mp3", "elephant.png") },
            { "I", ("ই", "ইলিশ", "let_i.mp3", "fish.png") },
            { "J", ("জ", "জাহাজ", "let_j.mp3", "ship.png") },
            { "K", ("ক", "কলা", "let_k.mp3", "banana.png") },
            { "L", ("ল", "লিচু", "let_l.mp3", "strawberry.png") },
            { "M", ("ম", "মাছ", "let_m.mp3", "fish.png") },
            { "N", ("ন", "নৌকা", "let_n.mp3", "boat.png") },
            { "O", ("ও", "ওলকপি", "let_o.mp3", "carrot.png") },
            { "P", ("প", "পাখি", "let_p.mp3", "bird.png") },
            { "Q", ("ৎ", "কৈতব", "let_q.mp3", "glowing_star.png") },
            { "R", ("র", "রংধনু", "let_r.mp3", "rainbow.png") },
            { "S", ("শ", "সিংহ", "let_s.mp3", "lion.png") },
            { "T", ("ট", "টিয়া", "let_t.mp3", "bird.png") },
            { "U", ("উ", "উট", "let_u.mp3", "camel.png") },
            { "V", ("ভ", "ভালুক", "let_v.mp3", "bear.png") },
            { "W", ("ঐ", "ঐরাবত", "let_w.mp3", "elephant.png") },
            { "X", ("ক্ষ", "ক্ষীর", "let_x.mp3", "icecream.png") },
            { "Y", ("য়", "ময়না", "let_y.mp3", "bird.png") },
            { "Z", ("ঝ", "ঝিনুক", "let_z.mp3", "shell.png") }
        };

        foreach (var kvp in defaultLetters) _letterMap[kvp.Key] = kvp.Value;

        // 2. Try loading keymap.json from disk to allow user customization
        string jsonPath = Path.Combine(_assetsDir, "keymap.json");
        if (File.Exists(jsonPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                var root = doc.RootElement;

                if (root.TryGetProperty("digits", out var digitsEl))
                {
                    foreach (var prop in digitsEl.EnumerateObject())
                    {
                        string glyph = prop.Value.GetProperty("glyph").GetString() ?? "";
                        string? name = prop.Value.TryGetProperty("name", out var n) ? n.GetString() : null;
                        string? voice = prop.Value.TryGetProperty("voice", out var v) ? v.GetString() : null;
                        string? emoji = prop.Value.TryGetProperty("emoji", out var em) ? em.GetString() : null;
                        _digitMap[prop.Name] = (glyph, name, voice, emoji);
                    }
                }

                if (root.TryGetProperty("letters", out var lettersEl))
                {
                    foreach (var prop in lettersEl.EnumerateObject())
                    {
                        string glyph = prop.Value.GetProperty("glyph").GetString() ?? "";
                        string? word = prop.Value.TryGetProperty("word", out var w) ? w.GetString() : null;
                        string? voice = prop.Value.TryGetProperty("voice", out var v) ? v.GetString() : null;
                        string? emoji = prop.Value.TryGetProperty("emoji", out var em) ? em.GetString() : null;
                        _letterMap[prop.Name] = (glyph, word, voice, emoji);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing keymap.json: {ex.Message}");
            }
        }
    }

    private void LoadAssetInventories()
    {
        string emojiDir = Path.Combine(_assetsDir, "Emoji");
        if (Directory.Exists(emojiDir))
        {
            _emojiFiles.AddRange(Directory.GetFiles(emojiDir, "*.png"));
        }

        string sfxDir = Path.Combine(_assetsDir, "Sounds", "Sfx");
        if (Directory.Exists(sfxDir))
        {
            _sfxFiles.AddRange(Directory.GetFiles(sfxDir, "*.wav"));

            foreach (var fn in FunnySoundNames)
            {
                string p = Path.Combine(sfxDir, fn);
                if (File.Exists(p))
                {
                    _funnySfxFiles.Add(p);
                }
            }
        }
    }

    public SmashContent GetContentForVirtualKey(int vkCode)
    {
        // 1. Digits 0-9 top row (VK 0x30 - 0x39)
        if (vkCode >= 0x30 && vkCode <= 0x39)
        {
            int num = vkCode - 0x30;
            return BuildContentForDigit($"D{num}");
        }

        // 2. Numpad digits 0-9 (VK 0x60 - 0x69)
        if (vkCode >= 0x60 && vkCode <= 0x69)
        {
            int num = vkCode - 0x60;
            return BuildContentForDigit($"NumPad{num}");
        }

        // 3. Letters A-Z (VK 0x41 - 0x5A)
        if (vkCode >= 0x41 && vkCode <= 0x5A)
        {
            char letter = (char)vkCode;
            return BuildContentForLetter(letter.ToString());
        }

        // 4. Space, Enter, Backspace, Arrows, Tab, Function keys, Symbols, etc. -> Shapes with Funny Sounds!
        return GetRandomShapeContent();
    }

    public SmashContent GetContentForKey(Key key)
    {
        string keyName = key.ToString();

        // 1. Check if key is a digit
        if (_digitMap.ContainsKey(keyName))
        {
            return BuildContentForDigit(keyName);
        }

        // 2. Check if key is a letter
        if (_letterMap.ContainsKey(keyName))
        {
            return BuildContentForLetter(keyName);
        }

        // 3. Any other key (Space, Return, Back, Tab, Arrows, etc.) -> Shapes with Funny Sounds!
        return GetRandomShapeContent();
    }

    public SmashContent GetRandomContent()
    {
        // 3-way random: 40% Shape, 30% Letter, 30% Number
        int roll = _random.Next(10);
        if (roll < 4)
        {
            return GetRandomShapeContent();
        }
        else if (roll < 7 && _letterMap.Count > 0)
        {
            var keys = _letterMap.Keys.ToList();
            return BuildContentForLetter(keys[_random.Next(keys.Count)]);
        }
        else if (_digitMap.Count > 0)
        {
            var keys = _digitMap.Keys.ToList();
            return BuildContentForDigit(keys[_random.Next(keys.Count)]);
        }

        return GetRandomShapeContent();
    }

    public SmashContent GetRandomShapeContent()
    {
        var shape = SupportedShapes[_random.Next(SupportedShapes.Length)];
        string? emoji = PickMatchingShapeEmoji(shape.Type);
        string? sfx = PickFunnySfx();

        return new SmashContent(
            Glyph: null,
            ShapeType: shape.Type,
            SecondaryText: shape.NameBn,
            EmojiPath: emoji,
            Color: PickRandomColor(),
            SfxPath: sfx,
            VoicePath: null,
            IsShape: true
        );
    }

    private SmashContent BuildContentForDigit(string key)
    {
        if (!_digitMap.TryGetValue(key, out var val))
        {
            val = ("১", "এক", "num_1.mp3", "apple.png");
        }

        string? voicePath = ResolveVoicePath(val.Voice);
        string? emojiPath = ResolveEmojiPath(val.Emoji) ?? PickRandomEmoji();

        return new SmashContent(
            Glyph: val.Glyph,
            ShapeType: null,
            SecondaryText: val.Word,
            EmojiPath: emojiPath,
            Color: PickRandomColor(),
            SfxPath: PickRandomSfx(),
            VoicePath: voicePath,
            IsShape: false
        );
    }

    private SmashContent BuildContentForLetter(string key)
    {
        if (!_letterMap.TryGetValue(key, out var val))
        {
            val = ("ক", "কলা", "let_k.mp3", "banana.png");
        }

        string? voicePath = ResolveVoicePath(val.Voice);
        string? emojiPath = ResolveEmojiPath(val.Emoji) ?? PickRandomEmoji();

        return new SmashContent(
            Glyph: val.Glyph,
            ShapeType: null,
            SecondaryText: val.Word,
            EmojiPath: emojiPath,
            Color: PickRandomColor(),
            SfxPath: PickRandomSfx(),
            VoicePath: voicePath,
            IsShape: false
        );
    }

    private string? ResolveVoicePath(string? filename)
    {
        if (string.IsNullOrEmpty(filename)) return null;
        string path = Path.Combine(_assetsDir, "Sounds", "Voice", filename);
        return File.Exists(path) ? path : null;
    }

    private string? ResolveEmojiPath(string? filename)
    {
        if (string.IsNullOrEmpty(filename)) return null;
        string path = Path.Combine(_assetsDir, "Emoji", filename);
        return File.Exists(path) ? path : null;
    }

    private string? PickRandomEmoji()
    {
        if (_emojiFiles.Count == 0) return null;
        return _emojiFiles[_random.Next(_emojiFiles.Count)];
    }

    private string? PickMatchingShapeEmoji(string shapeType)
    {
        string? match = shapeType switch
        {
            "Star" => _emojiFiles.FirstOrDefault(f => f.Contains("star")),
            "Heart" => _emojiFiles.FirstOrDefault(f => f.Contains("heart")),
            "Sun" => _emojiFiles.FirstOrDefault(f => f.Contains("sun")),
            "Moon" => _emojiFiles.FirstOrDefault(f => f.Contains("moon") || f.Contains("sparkles")),
            "Circle" => _emojiFiles.FirstOrDefault(f => f.Contains("soccer") || f.Contains("balloon")),
            _ => null
        };

        return match ?? PickRandomEmoji();
    }

    private string? PickRandomSfx()
    {
        if (_sfxFiles.Count == 0) return null;
        return _sfxFiles[_random.Next(_sfxFiles.Count)];
    }

    private string? PickFunnySfx()
    {
        if (_funnySfxFiles.Count > 0)
        {
            return _funnySfxFiles[_random.Next(_funnySfxFiles.Count)];
        }
        return PickRandomSfx();
    }

    private Color PickRandomColor()
    {
        return Palette[_random.Next(Palette.Length)];
    }
}
