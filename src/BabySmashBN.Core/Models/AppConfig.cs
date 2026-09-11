namespace BabySmashBN.Models;

public class AppConfig
{
    public bool PlayVoice { get; set; } = true;
    public bool PlaySfx { get; set; } = true;
    public bool ShowCursorFollower { get; set; } = true;
    public int MaxConcurrentBursts { get; set; } = 15;
}
