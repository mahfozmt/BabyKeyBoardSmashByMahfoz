namespace BabySmashBN.Services;

public interface IAudioService
{
    void PlaySfx(string? sfxPath);
    void PlayVoice(string? voicePath);
    void PlaySmash(string? sfxPath, string? voicePath);
}
