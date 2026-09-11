using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using BabySmashBN.Services;
using Xunit;

namespace BabySmashBN.Tests;

public class KeyMapServiceTests
{
    private readonly KeyMapService _service;

    public KeyMapServiceTests()
    {
        // Use the assets directory from the main project
        string assetsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/BabySmashBN/Assets"));
        _service = new KeyMapService(assetsDir);
    }

    [Theory]
    [InlineData(Key.D0, "০")]
    [InlineData(Key.D1, "১")]
    [InlineData(Key.D2, "২")]
    [InlineData(Key.D3, "৩")]
    [InlineData(Key.D4, "৪")]
    [InlineData(Key.D5, "৫")]
    [InlineData(Key.D6, "৬")]
    [InlineData(Key.D7, "৭")]
    [InlineData(Key.D8, "৮")]
    [InlineData(Key.D9, "৯")]
    [InlineData(Key.NumPad1, "১")]
    [InlineData(Key.NumPad5, "৫")]
    public void Key_Digits_Map_Correctly_To_Bangla_Numerals(Key key, string expectedGlyph)
    {
        var content = _service.GetContentForKey(key);
        Assert.Equal(expectedGlyph, content.Glyph);
    }

    [Theory]
    [InlineData(Key.A, "অ")]
    [InlineData(Key.B, "ব")]
    [InlineData(Key.K, "ক")]
    [InlineData(Key.M, "ম")]
    public void Key_Letters_Map_Correctly_To_Bangla_Characters(Key key, string expectedGlyph)
    {
        var content = _service.GetContentForKey(key);
        Assert.Equal(expectedGlyph, content.Glyph);
    }

    [Fact]
    public void VirtualKey_Digits_Map_Correctly()
    {
        // 0x31 is VK '1'
        var content1 = _service.GetContentForVirtualKey(0x31);
        Assert.Equal("১", content1.Glyph);

        // 0x33 is VK '3'
        var content3 = _service.GetContentForVirtualKey(0x33);
        Assert.Equal("৩", content3.Glyph);
    }

    [Fact]
    public void Space_And_Return_Return_Celebrations()
    {
        var spaceContent = _service.GetContentForKey(Key.Space);
        Assert.Contains("দারুণ", spaceContent.Glyph);

        var returnContent = _service.GetContentForKey(Key.Return);
        Assert.Contains("সাবাশ", returnContent.Glyph);
    }

    [Fact]
    public void Colors_Are_Bright_And_Valid()
    {
        for (int i = 0; i < 20; i++)
        {
            var content = _service.GetRandomContent();
            Assert.True(content.Color.A > 0);
            Assert.False(content.Color == Colors.Black);
            Assert.False(content.Color == Colors.Transparent);
        }
    }
}
