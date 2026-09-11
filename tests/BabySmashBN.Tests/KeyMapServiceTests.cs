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
        Assert.False(content.IsShape);
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
        Assert.False(content.IsShape);
        Assert.Equal(expectedGlyph, content.Glyph);
    }

    [Fact]
    public void VirtualKey_Digits_Map_Correctly()
    {
        // 0x31 is VK '1'
        var content1 = _service.GetContentForVirtualKey(0x31);
        Assert.False(content1.IsShape);
        Assert.Equal("১", content1.Glyph);

        // 0x33 is VK '3'
        var content3 = _service.GetContentForVirtualKey(0x33);
        Assert.False(content3.IsShape);
        Assert.Equal("৩", content3.Glyph);
    }

    [Theory]
    [InlineData(Key.Space)]
    [InlineData(Key.Return)]
    [InlineData(Key.Back)]
    [InlineData(Key.Tab)]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    [InlineData(Key.F5)]
    public void Non_Alphanumeric_Keys_Return_Shapes_With_Funny_Sounds(Key key)
    {
        var content = _service.GetContentForKey(key);
        Assert.True(content.IsShape);
        Assert.NotNull(content.ShapeType);
        Assert.NotEmpty(content.ShapeType);
        Assert.NotNull(content.SecondaryText); // Bangla shape name like তারা, হৃদয়, বৃত্ত
        Assert.NotNull(content.SfxPath);       // Funny sound
        Assert.Null(content.Glyph);            // No letter or number
    }

    [Fact]
    public void VirtualKey_Non_Alphanumeric_Returns_Shape()
    {
        // VK_SPACE is 0x20
        var spaceContent = _service.GetContentForVirtualKey(0x20);
        Assert.True(spaceContent.IsShape);
        Assert.NotNull(spaceContent.ShapeType);
        Assert.NotNull(spaceContent.SfxPath);

        // VK_RETURN is 0x0D
        var returnContent = _service.GetContentForVirtualKey(0x0D);
        Assert.True(returnContent.IsShape);
        Assert.NotNull(returnContent.ShapeType);
        Assert.NotNull(returnContent.SfxPath);
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
