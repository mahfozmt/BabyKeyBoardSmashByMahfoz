using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using BabySmashBN.Interop;
using BabySmashBN.Services;

namespace BabySmashBN;

public partial class MainWindow : Window
{
    private readonly LowLevelKeyboardHook _hook;
    private readonly AccessibilityHelper _accessibilityHelper;
    private readonly IKeyMapService _keyMapService;
    private readonly AudioService _audioService;
    private readonly RenderService _renderService;
    private readonly MouseTrailService _mouseTrail;

    private readonly DispatcherTimer _escTimer;
    private readonly Stopwatch _escStopwatch = new();
    private const int EscExitDurationMs = 2000;
    private bool _isEscapeHeld;
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();

        // 1. Accessibility safety lock (prevents Sticky Keys & Filter Keys popup)
        _accessibilityHelper = new AccessibilityHelper();
        _accessibilityHelper.DisableAccessibilityShortcuts();

        // 2. Win32 Low-level keyboard hook
        _hook = new LowLevelKeyboardHook();
        _hook.KeyIntercepted += OnKeyIntercepted;
        _hook.EscapeStateChanged += OnEscapeStateChanged;
        _hook.ExitRequested += OnExitRequested;
        _hook.Install();

        // 3. Core domain & presentation services
        string assetsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        _keyMapService = new KeyMapService(assetsDir);
        _audioService = new AudioService();
        _renderService = new RenderService(SmashCanvas, _audioService);
        _mouseTrail = new MouseTrailService(SmashCanvas);

        // 4. Timer for 2-second hold Escape to exit (smooth 25ms refresh)
        _escTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(25) };
        _escTimer.Tick += EscTimer_Tick;

        // 5. Wire up mouse, touch, and fallback keyboard events
        SmashCanvas.MouseMove += (s, e) => _mouseTrail.OnMouseMove(e.GetPosition(SmashCanvas));
        SmashCanvas.MouseDown += SmashCanvas_MouseDown;
        SmashCanvas.TouchDown += SmashCanvas_TouchDown;

        PreviewKeyDown += MainWindow_PreviewKeyDown;
        PreviewKeyUp += MainWindow_PreviewKeyUp;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Focus the window
        Activate();
        Focus();

        // Fade out welcome watermark banner after 6 seconds
        var fade = new DoubleAnimation(1.0, 0.0, TimeSpan.FromSeconds(2.0))
        {
            BeginTime = TimeSpan.FromSeconds(5.0)
        };
        fade.Completed += (s, ev) => WelcomeBanner.Visibility = Visibility.Collapsed;
        WelcomeBanner.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Direct WPF fallback for Alt+F4
        if ((e.Key == Key.System && e.SystemKey == Key.F4) ||
            (e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt))
        {
            e.Handled = true;
            RequestExit();
            return;
        }

        // Direct WPF fallback for Escape key
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HandleEscapePress();
        }
    }

    private void MainWindow_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HandleEscapeRelease();
        }
    }

    private void OnExitRequested()
    {
        Dispatcher.InvokeAsync(RequestExit);
    }

    private void RequestExit()
    {
        if (_isClosing) return;
        _isClosing = true;

        _escTimer.Stop();
        _escStopwatch.Reset();
        Close();
    }

    private void OnKeyIntercepted(int vkCode, bool isDown)
    {
        if (!isDown) return;

        Dispatcher.InvokeAsync(() =>
        {
            var content = _keyMapService.GetContentForVirtualKey(vkCode);
            _renderService.SpawnBurst(content);
            _audioService.PlaySmash(content.SfxPath, content.VoicePath);
        });
    }

    private void OnEscapeStateChanged(bool isDown)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (isDown)
            {
                HandleEscapePress();
            }
            else
            {
                HandleEscapeRelease();
            }
        });
    }

    private void HandleEscapePress()
    {
        if (_isEscapeHeld || _isClosing) return;
        _isEscapeHeld = true;

        _escStopwatch.Restart();
        ExitProgressBar.Value = 0;
        ExitOverlay.Visibility = Visibility.Visible;
        _escTimer.Start();
    }

    private void HandleEscapeRelease()
    {
        if (_isClosing) return;
        _isEscapeHeld = false;

        _escTimer.Stop();
        _escStopwatch.Reset();
        ExitProgressBar.Value = 0;
        ExitOverlay.Visibility = Visibility.Collapsed;
    }

    private void EscTimer_Tick(object? sender, EventArgs e)
    {
        if (_isClosing) return;

        long elapsed = _escStopwatch.ElapsedMilliseconds;
        ExitProgressBar.Value = Math.Min(elapsed, EscExitDurationMs);

        if (elapsed >= EscExitDurationMs)
        {
            RequestExit();
        }
    }

    private void SmashCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(SmashCanvas);
        var content = _keyMapService.GetRandomContent();
        _renderService.SpawnBurst(content, pos);
        _audioService.PlaySmash(content.SfxPath, content.VoicePath);
    }

    private void SmashCanvas_TouchDown(object? sender, TouchEventArgs e)
    {
        var pos = e.GetTouchPoint(SmashCanvas).Position;
        var content = _keyMapService.GetRandomContent();
        _renderService.SpawnBurst(content, pos);
        _audioService.PlaySmash(content.SfxPath, content.VoicePath);
    }

    private void BtnParentToggle_Click(object sender, RoutedEventArgs e)
    {
        ParentFlyout.Visibility = ParentFlyout.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void ChkVoice_Changed(object sender, RoutedEventArgs e)
    {
        if (_audioService != null)
        {
            _audioService.PlayVoiceEnabled = ChkVoice.IsChecked == true;
        }
    }

    private void ChkSfx_Changed(object sender, RoutedEventArgs e)
    {
        if (_audioService != null)
        {
            _audioService.PlaySfxEnabled = ChkSfx.IsChecked == true;
        }
    }

    private void ChkCursor_Changed(object sender, RoutedEventArgs e)
    {
        if (_mouseTrail != null)
        {
            _mouseTrail.Enabled = ChkCursor.IsChecked == true;
        }
    }

    private void BtnExit_Click(object sender, RoutedEventArgs e)
    {
        RequestExit();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _escTimer.Stop();
        _hook.Dispose();
        _accessibilityHelper.Dispose();
        _audioService.Dispose();
        Application.Current.Shutdown();
    }
}