using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BabySmashBN.Services;

public class MouseTrailService
{
    private readonly Canvas _canvas;
    private readonly Random _random = new();
    private Image? _cursorFollower;
    private Point _lastSpawnPos;
    private bool _enabled = true;

    private static readonly Color[] TrailColors = new[]
    {
        Color.FromRgb(255, 214, 0),  // Yellow
        Color.FromRgb(255, 64, 129),  // Pink
        Color.FromRgb(0, 176, 255),   // Azure
        Color.FromRgb(118, 255, 3),   // Lime
        Color.FromRgb(224, 64, 251)   // Purple
    };

    public bool Enabled
    {
        get => _enabled;
        set
        {
            _enabled = value;
            if (_cursorFollower != null)
            {
                _cursorFollower.Visibility = _enabled ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public MouseTrailService(Canvas canvas)
    {
        _canvas = canvas;
        InitializeFollower();
    }

    private void InitializeFollower()
    {
        string smilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Emoji", "smile.png");
        if (File.Exists(smilePath))
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(smilePath, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();

                _cursorFollower = new Image
                {
                    Source = bi,
                    Width = 48,
                    Height = 48,
                    IsHitTestVisible = false,
                    Opacity = 0.9
                };

                Canvas.SetZIndex(_cursorFollower, 999);
                _canvas.Children.Add(_cursorFollower);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load cursor follower: {ex.Message}");
            }
        }
    }

    public void OnMouseMove(Point pos)
    {
        if (!_enabled) return;

        // 1. Move follower face
        if (_cursorFollower != null)
        {
            Canvas.SetLeft(_cursorFollower, pos.X - 24);
            Canvas.SetTop(_cursorFollower, pos.Y - 24);
        }

        // 2. Spawn trail particle if mouse has moved at least 25 pixels
        var dist = (pos - _lastSpawnPos).Length;
        if (dist >= 25)
        {
            _lastSpawnPos = pos;
            SpawnTrailParticle(pos);
        }
    }

    private void SpawnTrailParticle(Point pos)
    {
        double size = _random.Next(8, 16);
        var color = TrailColors[_random.Next(TrailColors.Length)];

        var dot = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = new SolidColorBrush(color),
            IsHitTestVisible = false,
            Opacity = 0.8
        };

        Canvas.SetLeft(dot, pos.X - size / 2 + (_random.NextDouble() * 10 - 5));
        Canvas.SetTop(dot, pos.Y - size / 2 + (_random.NextDouble() * 10 - 5));

        _canvas.Children.Add(dot);

        // Fade and shrink animation
        var animFade = new DoubleAnimation(0.8, 0.0, TimeSpan.FromSeconds(0.6));
        animFade.Completed += (s, e) => _canvas.Children.Remove(dot);
        dot.BeginAnimation(UIElement.OpacityProperty, animFade);
    }
}
