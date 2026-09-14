using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private readonly IAudioService? _audioService;
    private readonly Random _random = new();
    private readonly Queue<UIElement> _activeBursts = new();
    private readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
    private readonly FontFamily _banglaFont;
    private readonly int _maxConcurrentBursts;
    private int _zIndexCounter = 10;

    public RenderService(Canvas canvas, IAudioService? audioService = null, int maxConcurrentBursts = 18)
    {
        _canvas = canvas;
        _audioService = audioService;
        _maxConcurrentBursts = maxConcurrentBursts;

        // Try loading local font or embedded WPF resource font
        string localFontPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Fonts", "BalooDa2.ttf");
        if (File.Exists(localFontPath))
        {
            _banglaFont = new FontFamily(new Uri(localFontPath), "./#Baloo Da 2");
        }
        else
        {
            try
            {
                _banglaFont = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Baloo Da 2");
            }
            catch
            {
                _banglaFont = new FontFamily("Segoe UI, Kalpurush, SolaimanLipi, Arial");
            }
        }
    }

    private const double BurstWidth = 420;
    private const double BurstHeight = 460;
    private const double HalfBurstW = BurstWidth / 2.0; // 210
    private const double HalfBurstH = BurstHeight / 2.0; // 230

    public void SpawnBurst(SmashContent content, Point? targetPoint = null)
    {
        double screenW = _canvas.ActualWidth > 300 ? _canvas.ActualWidth : 1280;
        double screenH = _canvas.ActualHeight > 300 ? _canvas.ActualHeight : 800;

        // Keep burst safely inside canvas visible boundaries
        double minX = HalfBurstW + 20;
        double maxX = Math.Max(minX, screenW - HalfBurstW - 20);
        double minY = HalfBurstH + 20;
        double maxY = Math.Max(minY, screenH - HalfBurstH - 20);

        double cx = targetPoint?.X ?? (_random.NextDouble() * (maxX - minX) + minX);
        double cy = targetPoint?.Y ?? (_random.NextDouble() * (maxY - minY) + minY);

        cx = Math.Clamp(cx, minX, maxX);
        cy = Math.Clamp(cy, minY, maxY);

        var container = new Grid
        {
            Width = BurstWidth,
            Height = BurstHeight,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = Cursors.Hand,
            Background = Brushes.Transparent // Enables clicking anywhere inside burst area
        };

        Canvas.SetZIndex(container, ++_zIndexCounter);

        // 1. Background particle sparkles radiating outward
        SpawnBurstParticles(container, content.Color);

        // 2. Central Content Stack (Shape OR Letter/Number)
        var contentStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false // Click events bubble up to container
        };

        if (content.IsShape)
        {
            // --- PURE SHAPE VISUAL ---
            var shapeVisual = CreateShapeVisual(content.ShapeType ?? "Star", content.Color);
            contentStack.Children.Add(shapeVisual);

            // Shape Bangla name pill badge (e.g. "তারা", "হৃদয়", "বৃত্ত", "ত্রিভুজ")
            if (!string.IsNullOrEmpty(content.SecondaryText))
            {
                var shapeBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(150, 17, 17, 27)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(18),
                    Padding = new Thickness(20, 6, 20, 7),
                    Margin = new Thickness(0, 14, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 14,
                        ShadowDepth = 3,
                        Opacity = 0.6
                    }
                };

                var shapeLabel = new TextBlock
                {
                    Text = content.SecondaryText,
                    FontFamily = _banglaFont,
                    FontSize = 32,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                shapeBadge.Child = shapeLabel;
                contentStack.Children.Add(shapeBadge);
            }
        }
        else
        {
            // --- LETTER / NUMBER VISUAL ---
            // 1. Emoji Image (Animals, fruits, toys matching word)
            if (!string.IsNullOrEmpty(content.EmojiPath) && File.Exists(content.EmojiPath))
            {
                var bitmap = LoadCachedImage(content.EmojiPath);
                var img = new Image
                {
                    Source = bitmap,
                    Width = 105,
                    Height = 105,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 2),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 14,
                        ShadowDepth = 3,
                        Opacity = 0.45
                    }
                };
                RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                contentStack.Children.Add(img);
            }

            // 2. Big Bangla Glyph (১, ২, ৩, অ, ব, ক...)
            if (!string.IsNullOrEmpty(content.Glyph))
            {
                var glyphText = new TextBlock
                {
                    Text = content.Glyph,
                    FontFamily = _banglaFont,
                    FontSize = content.Glyph.Length > 2 ? 60 : 92,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(content.Color),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, -4, 0, 2),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 16,
                        ShadowDepth = 4,
                        Opacity = 0.65
                    }
                };
                contentStack.Children.Add(glyphText);
            }

            // 3. Secondary Word Text badge (e.g. "অজগর", "কলা", "বই")
            if (!string.IsNullOrEmpty(content.SecondaryText))
            {
                var wordBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(160, 17, 17, 27)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(18, 5, 18, 6),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0),
                    Effect = new DropShadowEffect
                    {
                        Color = Colors.Black,
                        BlurRadius = 12,
                        ShadowDepth = 3,
                        Opacity = 0.6
                    }
                };

                var secondaryText = new TextBlock
                {
                    Text = content.SecondaryText,
                    FontFamily = _banglaFont,
                    FontSize = 26,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(249, 226, 175)), // Warm Golden Yellow
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                wordBadge.Child = secondaryText;
                contentStack.Children.Add(wordBadge);
            }
        }

        container.Children.Add(contentStack);

        // Positioning on Canvas
        Canvas.SetLeft(container, cx - HalfBurstW);
        Canvas.SetTop(container, cy - HalfBurstH);

        // Transforms for elastic popping and gentle drifting
        var transformGroup = new TransformGroup();
        var scaleTransform = new ScaleTransform(0.2, 0.2, HalfBurstW, HalfBurstH);
        var rotateTransform = new RotateTransform(_random.Next(-10, 11), HalfBurstW, HalfBurstH);
        var translateTransform = new TranslateTransform(0, 0);

        transformGroup.Children.Add(scaleTransform);
        transformGroup.Children.Add(rotateTransform);
        transformGroup.Children.Add(translateTransform);
        container.RenderTransform = transformGroup;

        // Interactive Click/Touch handler on this element!
        container.MouseDown += (s, e) =>
        {
            e.Handled = true; // Stop background canvas from spawning duplicate
            AnimateElementClick(container, content);
        };
        container.TouchDown += (s, e) =>
        {
            e.Handled = true;
            AnimateElementClick(container, content);
        };

        // Enforce max burst limit to prevent memory leak and lag
        _activeBursts.Enqueue(container);
        if (_activeBursts.Count > _maxConcurrentBursts)
        {
            var oldest = _activeBursts.Dequeue();
            _canvas.Children.Remove(oldest);
        }

        _canvas.Children.Add(container);

        // Storyboard Animation for spawn
        StartSpawnAnimation(container, scaleTransform, translateTransform);
    }

    private void StartSpawnAnimation(Grid container, ScaleTransform scaleTransform, TranslateTransform translateTransform)
    {
        var sb = new Storyboard();

        // Scale pop (0.2 -> 1.15 -> 1.0)
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

        // Gentle floating drift
        double driftY = _random.Next(-55, -25);
        var driftAnim = new DoubleAnimation(0, driftY, TimeSpan.FromSeconds(3.0));
        Storyboard.SetTarget(driftAnim, container);
        Storyboard.SetTargetProperty(driftAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[2].(TranslateTransform.Y)"));
        sb.Children.Add(driftAnim);

        // Smooth fade out
        var fadeKeyFrames = new DoubleAnimationUsingKeyFrames();
        fadeKeyFrames.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
        fadeKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2.0))));
        fadeKeyFrames.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.0))));
        Storyboard.SetTarget(fadeKeyFrames, container);
        Storyboard.SetTargetProperty(fadeKeyFrames, new PropertyPath("Opacity"));
        sb.Children.Add(fadeKeyFrames);

        sb.Completed += (s, e) =>
        {
            _canvas.Children.Remove(container);
        };

        container.Tag = sb; // Keep reference to storyboard
        sb.Begin();
    }

    /// <summary>
    /// When toddler or adult clicks on an on-screen letter/shape, it jiggles, pops, plays sound, and bursts sparkles!
    /// </summary>
    public void AnimateElementClick(Grid container, SmashContent content)
    {
        // 1. Bring element to the top
        Canvas.SetZIndex(container, ++_zIndexCounter);

        // 2. Play funny sound and/or re-play voice!
        if (_audioService != null)
        {
            string? funSfx = content.SfxPath;
            _audioService.PlaySmash(funSfx, content.VoicePath);
        }

        // 3. Stop old fade-out animation and reset opacity
        if (container.Tag is Storyboard oldSb)
        {
            oldSb.Stop();
        }
        container.BeginAnimation(UIElement.OpacityProperty, null);
        container.Opacity = 1.0;

        // 4. Elastic Pop + Wobble Animation
        var transformGroup = (TransformGroup)container.RenderTransform;
        var scaleTransform = (ScaleTransform)transformGroup.Children[0];
        var rotateTransform = (RotateTransform)transformGroup.Children[1];

        // Elastic scale bounce (1.0 -> 1.4 -> 1.0)
        var popAnim = new DoubleAnimationUsingKeyFrames();
        popAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.35, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.12))));
        popAnim.KeyFrames.Add(new SplineDoubleKeyFrame(0.95, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.25))));
        popAnim.KeyFrames.Add(new SplineDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.38))));

        scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, popAnim);
        scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, popAnim);

        // Wobble rock (-30 deg -> +30 deg -> -12 deg -> +12 deg -> 0 deg)
        var wobbleAnim = new DoubleAnimationUsingKeyFrames();
        wobbleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(-28, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.08))));
        wobbleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(28, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.18))));
        wobbleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(-12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.28))));
        wobbleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.36))));
        wobbleAnim.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.44))));

        rotateTransform.BeginAnimation(RotateTransform.AngleProperty, wobbleAnim);

        // 5. Spawn mini burst particles radiating around the clicked element
        SpawnBurstParticles(container, content.Color, count: 8, spreadDistance: 90);

        // 6. Restart smooth fade-out over next 3 seconds so toddler can continue playing with it
        var fadeAnim = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(1.0))
        {
            BeginTime = TimeSpan.FromSeconds(2.5)
        };
        fadeAnim.Completed += (s, e) => _canvas.Children.Remove(container);
        container.BeginAnimation(UIElement.OpacityProperty, fadeAnim);
    }

    private void SpawnBurstParticles(Grid container, Color color, int count = 10, int spreadDistance = 140)
    {
        var particleBrush = new SolidColorBrush(color);
        for (int i = 0; i < count; i++)
        {
            double angle = (2.0 * Math.PI / count) * i + (_random.NextDouble() * 0.4 - 0.2);
            double distance = _random.Next(spreadDistance / 2, spreadDistance);
            double targetX = Math.Cos(angle) * distance;
            double targetY = Math.Sin(angle) * distance;
            double pSize = _random.Next(12, 22);

            var particle = new Ellipse
            {
                Width = pSize,
                Height = pSize,
                Fill = particleBrush,
                Opacity = 0.85,
                RenderTransform = new TranslateTransform(0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            container.Children.Add(particle);

            var pTrans = (TranslateTransform)particle.RenderTransform;
            var animX = new DoubleAnimation(0, targetX, TimeSpan.FromSeconds(0.75))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animY = new DoubleAnimation(0, targetY, TimeSpan.FromSeconds(0.75))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            var animFade = new DoubleAnimation(0.9, 0.0, TimeSpan.FromSeconds(0.75));
            animFade.Completed += (s, e) => container.Children.Remove(particle);

            pTrans.BeginAnimation(TranslateTransform.XProperty, animX);
            pTrans.BeginAnimation(TranslateTransform.YProperty, animY);
            particle.BeginAnimation(UIElement.OpacityProperty, animFade);
        }
    }

    private FrameworkElement CreateShapeVisual(string shapeType, Color color)
    {
        var brush = new SolidColorBrush(color);
        var shadow = new DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 16,
            ShadowDepth = 4,
            Opacity = 0.5
        };

        switch (shapeType)
        {
            case "Star":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,0 L 63,35 L 100,35 L 70,57 L 81,92 L 50,70 L 19,92 L 30,57 L 0,35 L 37,35 Z"),
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Heart":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,25 C 50,25 40,0 20,0 C 5,0 0,15 0,30 C 0,60 50,90 50,100 C 50,90 100,60 100,30 C 100,15 95,0 80,0 C 60,0 50,25 50,25 Z"),
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Circle":
                return new Ellipse
                {
                    Width = 140,
                    Height = 140,
                    Fill = brush,
                    Effect = shadow
                };

            case "Triangle":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,5 L 95,90 L 5,90 Z"),
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Square":
                return new System.Windows.Shapes.Rectangle
                {
                    Width = 130,
                    Height = 130,
                    RadiusX = 18,
                    RadiusY = 18,
                    Fill = brush,
                    Effect = shadow
                };

            case "Diamond":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,0 L 100,50 L 50,100 L 0,50 Z"),
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Moon":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,0 A 50,50 0 1,0 100,75 A 40,40 0 1,1 50,0 Z"),
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Sun":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 50,20 A 30,30 0 1,0 50,80 A 30,30 0 1,0 50,20 M 50,0 L 50,12 M 50,88 L 50,100 M 0,50 L 12,50 M 88,50 L 100,50 M 15,15 L 24,24 M 76,76 L 85,85 M 15,85 L 24,76 M 76,24 L 85,15"),
                    Stroke = brush,
                    StrokeThickness = 8,
                    Fill = brush,
                    Width = 140,
                    Height = 140,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            case "Cloud":
                return new System.Windows.Shapes.Path
                {
                    Data = Geometry.Parse("M 30,60 A 20,20 0 0,1 60,40 A 25,25 0 0,1 100,45 A 20,20 0 0,1 120,65 A 15,15 0 0,1 110,85 L 30,85 A 15,15 0 0,1 30,60 Z"),
                    Fill = brush,
                    Width = 150,
                    Height = 120,
                    Stretch = Stretch.Uniform,
                    Effect = shadow
                };

            default:
                return new Ellipse
                {
                    Width = 140,
                    Height = 140,
                    Fill = brush,
                    Effect = shadow
                };
        }
    }

    private BitmapImage LoadCachedImage(string path)
    {
        return _imageCache.GetOrAdd(path, p =>
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(p, UriKind.Absolute);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            bi.Freeze();
            return bi;
        });
    }

    public void Clear()
    {
        _activeBursts.Clear();
        _canvas.Children.Clear();
    }
}
