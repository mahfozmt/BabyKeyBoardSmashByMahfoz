using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BabySmashBN.Models;

namespace BabySmashBN.Services;

public class RenderService : IRenderService
{
    private readonly Canvas _canvas;
    private readonly Random _random = new();
    private readonly Queue<UIElement> _activeBursts = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
    private readonly FontFamily _banglaFont;
    private readonly int _maxConcurrentBursts;

    public RenderService(Canvas canvas, int maxConcurrentBursts = 15)
    {
        _canvas = canvas;
        _maxConcurrentBursts = maxConcurrentBursts;

        // Try loading embedded or local font
        string localFontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "BalooDa2.ttf");
        if (File.Exists(localFontPath))
        {
            _banglaFont = new FontFamily(new Uri(localFontPath), "./#Baloo Da 2");
        }
        else
        {
            _banglaFont = new FontFamily("Segoe UI, Arial");
        }
    }

    public void SpawnBurst(SmashContent content, Point? targetPoint = null)
    {
        double screenW = _canvas.ActualWidth > 200 ? _canvas.ActualWidth : 1200;
        double screenH = _canvas.ActualHeight > 200 ? _canvas.ActualHeight : 800;

        // Determine center point for this burst
        double cx = targetPoint?.X ?? (_random.NextDouble() * (screenW - 350) + 175);
        double cy = targetPoint?.Y ?? (_random.NextDouble() * (screenH - 350) + 175);

        var container = new Grid
        {
            Width = 320,
            Height = 320,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // 1. Background particle sparkles radiating outward
        var particleBrush = new SolidColorBrush(content.Color);
        int particleCount = _random.Next(8, 14);
        for (int i = 0; i < particleCount; i++)
        {
            double angle = (2.0 * Math.PI / particleCount) * i + (_random.NextDouble() * 0.4 - 0.2);
            double distance = _random.Next(80, 160);
            double targetX = Math.Cos(angle) * distance;
            double targetY = Math.Sin(angle) * distance;
            double pSize = _random.Next(12, 24);

            var particle = new Ellipse
            {
                Width = pSize,
                Height = pSize,
                Fill = particleBrush,
                Opacity = 0.85,
                RenderTransform = new TranslateTransform(0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            container.Children.Add(particle);

            // Animate particle radiating outward
            var pTrans = (TranslateTransform)particle.RenderTransform;
            var animX = new DoubleAnimation(0, targetX, TimeSpan.FromSeconds(0.8))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animY = new DoubleAnimation(0, targetY, TimeSpan.FromSeconds(0.8))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animFade = new DoubleAnimation(0.9, 0.0, TimeSpan.FromSeconds(0.8));

            pTrans.BeginAnimation(TranslateTransform.XProperty, animX);
            pTrans.BeginAnimation(TranslateTransform.YProperty, animY);
            particle.BeginAnimation(UIElement.OpacityProperty, animFade);
        }

        // 2. Central Content Stack (Emoji + Bangla Glyph + Word)
        var contentStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Emoji Image
        if (!string.IsNullOrEmpty(content.EmojiPath) && File.Exists(content.EmojiPath))
        {
            var bitmap = _imageCache.GetOrAdd(content.EmojiPath, path =>
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(path, UriKind.Absolute);
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.EndInit();
                bi.Freeze();
                return bi;
            });

            var img = new Image
            {
                Source = bitmap,
                Width = 130,
                Height = 130,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Opacity = 0.4
                }
            };
            contentStack.Children.Add(img);
        }

        // Bangla Glyph
        var glyphText = new TextBlock
        {
            Text = content.Glyph,
            FontFamily = _banglaFont,
            FontSize = content.Glyph.Length > 2 ? 64 : 110,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(content.Color),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Effect = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 14,
                ShadowDepth = 4,
                Opacity = 0.5
            }
        };
        contentStack.Children.Add(glyphText);

        // Secondary Text (Word name, e.g. "এক", "কলা")
        if (!string.IsNullOrEmpty(content.SecondaryText))
        {
            var secondaryText = new TextBlock
            {
                Text = content.SecondaryText,
                FontFamily = _banglaFont,
                FontSize = 28,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 10,
                    ShadowDepth = 2,
                    Opacity = 0.6
                }
            };
            contentStack.Children.Add(secondaryText);
        }

        container.Children.Add(contentStack);

        // Positioning on Canvas
        Canvas.SetLeft(container, cx - 160);
        Canvas.SetTop(container, cy - 160);

        // Transforms for elastic popping and gentle drifting
        var transformGroup = new TransformGroup();
        var scaleTransform = new ScaleTransform(0.2, 0.2, 160, 160);
        var rotateTransform = new RotateTransform(_random.Next(-14, 15), 160, 160);
        var translateTransform = new TranslateTransform(0, 0);

        transformGroup.Children.Add(scaleTransform);
        transformGroup.Children.Add(rotateTransform);
        transformGroup.Children.Add(translateTransform);
        container.RenderTransform = transformGroup;

        // Enforce max burst limit to prevent memory leak and lag
        _activeBursts.Enqueue(container);
        if (_activeBursts.Count > _maxConcurrentBursts)
        {
            var oldest = _activeBursts.Dequeue();
            _canvas.Children.Remove(oldest);
        }

        _canvas.Children.Add(container);

        // Storyboard Animation:
        // 0.0s -> 0.15s: Scale 0.2 -> 1.15 (elastic overshoot)
        // 0.15s -> 0.30s: Scale 1.15 -> 1.0
        // 0.3s -> 2.5s: Floating drift upwards
        // 2.0s -> 3.0s: Fade out
        var sb = new Storyboard();

        var scaleXKeyFrames = new DoubleAnimationUsingKeyFrames();
        scaleXKeyFrames.KeyFrames.Add(new SplineDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.15))));
        scaleXKeyFrames.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.30))));
        Storyboard.SetTarget(scaleXKeyFrames, container);
        Storyboard.SetTargetProperty(scaleXKeyFrames, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleX)"));
        sb.Children.Add(scaleXKeyFrames);

        var scaleYKeyFrames = new DoubleAnimationUsingKeyFrames();
        scaleYKeyFrames.KeyFrames.Add(new SplineDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.15))));
        scaleYKeyFrames.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.30))));
        Storyboard.SetTarget(scaleYKeyFrames, container);
        Storyboard.SetTargetProperty(scaleYKeyFrames, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(ScaleTransform.ScaleY)"));
        sb.Children.Add(scaleYKeyFrames);

        // Floating drift
        double driftY = _random.Next(-50, -20);
        var driftAnim = new DoubleAnimation(0, driftY, TimeSpan.FromSeconds(2.8));
        Storyboard.SetTarget(driftAnim, container);
        Storyboard.SetTargetProperty(driftAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
        sb.Children.Add(driftAnim);

        // Fade out
        var fadeKeyFrames = new DoubleAnimationUsingKeyFrames();
        fadeKeyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
        fadeKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.8))));
        fadeKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.8))));
        Storyboard.SetTarget(fadeKeyFrames, container);
        Storyboard.SetTargetProperty(fadeKeyFrames, new PropertyPath("Opacity"));
        sb.Children.Add(fadeKeyFrames);

        sb.Completed += (s, e) =>
        {
            _canvas.Children.Remove(container);
        };

        sb.Begin();
    }

    public void Clear()
    {
        _activeBursts.Clear();
        _canvas.Children.Clear();
    }
}
