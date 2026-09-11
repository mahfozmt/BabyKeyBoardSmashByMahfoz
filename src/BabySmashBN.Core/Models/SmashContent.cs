using System.Windows.Media;

namespace BabySmashBN.Models;

public record SmashContent(
    string? Glyph,
    string? ShapeType,
    string? SecondaryText,
    string? EmojiPath,
    Color Color,
    string? SfxPath,
    string? VoicePath,
    bool IsShape = false
);
