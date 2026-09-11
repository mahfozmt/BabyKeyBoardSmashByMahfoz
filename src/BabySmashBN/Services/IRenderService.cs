using System.Windows;
using BabySmashBN.Models;

namespace BabySmashBN.Services;

public interface IRenderService
{
    void SpawnBurst(SmashContent content, Point? targetPoint = null);
    void Clear();
}
