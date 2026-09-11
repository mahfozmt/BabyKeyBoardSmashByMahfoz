using System.Windows.Input;
using BabySmashBN.Models;

namespace BabySmashBN.Services;

public interface IKeyMapService
{
    SmashContent GetContentForVirtualKey(int vkCode);
    SmashContent GetContentForKey(Key key);
    SmashContent GetRandomContent();
    SmashContent GetRandomShapeContent();
}
