using System;
using System.Runtime.InteropServices;

namespace BabySmashBN.Interop;

/// <summary>
/// Disables Windows accessibility shortcut keys (Sticky Keys, Filter Keys, Toggle Keys)
/// while BabySmash is active so toddler keyboard smashing doesn't pop up OS dialogs.
/// Restores the original settings upon Dispose().
/// </summary>
public sealed class AccessibilityHelper : IDisposable
{
    private const uint SPI_GETSTICKYKEYS = 0x003A;
    private const uint SPI_SETSTICKYKEYS = 0x003B;
    private const uint SPI_GETFILTERKEYS = 0x0032;
    private const uint SPI_SETFILTERKEYS = 0x0033;
    private const uint SPI_GETTOGGLEKEYS = 0x0034;
    private const uint SPI_SETTOGGLEKEYS = 0x0035;

    private const uint SKF_HOTKEYACTIVE = 0x00000004;
    private const uint FKF_HOTKEYACTIVE = 0x00000004;
    private const uint TKF_HOTKEYACTIVE = 0x00000004;

    [StructLayout(LayoutKind.Sequential)]
    private struct STICKYKEYS
    {
        public uint cbSize;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILTERKEYS
    {
        public uint cbSize;
        public uint dwFlags;
        public uint iWaitMSec;
        public uint iDelayMSec;
        public uint iRepeatMSec;
        public uint iBounceMSec;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOGGLEKEYS
    {
        public uint cbSize;
        public uint dwFlags;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref STICKYKEYS pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref FILTERKEYS pvParam, uint fWinIni);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref TOGGLEKEYS pvParam, uint fWinIni);

    private STICKYKEYS _originalStickyKeys;
    private FILTERKEYS _originalFilterKeys;
    private TOGGLEKEYS _originalToggleKeys;
    private bool _initialized;
    private bool _disposed;

    public void DisableAccessibilityShortcuts()
    {
        if (_initialized) return;

        try
        {
            _originalStickyKeys = new STICKYKEYS { cbSize = (uint)Marshal.SizeOf<STICKYKEYS>() };
            _originalFilterKeys = new FILTERKEYS { cbSize = (uint)Marshal.SizeOf<FILTERKEYS>() };
            _originalToggleKeys = new TOGGLEKEYS { cbSize = (uint)Marshal.SizeOf<TOGGLEKEYS>() };

            SystemParametersInfo(SPI_GETSTICKYKEYS, _originalStickyKeys.cbSize, ref _originalStickyKeys, 0);
            SystemParametersInfo(SPI_GETFILTERKEYS, _originalFilterKeys.cbSize, ref _originalFilterKeys, 0);
            SystemParametersInfo(SPI_GETTOGGLEKEYS, _originalToggleKeys.cbSize, ref _originalToggleKeys, 0);

            var disabledSticky = _originalStickyKeys;
            disabledSticky.dwFlags &= ~SKF_HOTKEYACTIVE;
            SystemParametersInfo(SPI_SETSTICKYKEYS, disabledSticky.cbSize, ref disabledSticky, 0);

            var disabledFilter = _originalFilterKeys;
            disabledFilter.dwFlags &= ~FKF_HOTKEYACTIVE;
            SystemParametersInfo(SPI_SETFILTERKEYS, disabledFilter.cbSize, ref disabledFilter, 0);

            var disabledToggle = _originalToggleKeys;
            disabledToggle.dwFlags &= ~TKF_HOTKEYACTIVE;
            SystemParametersInfo(SPI_SETTOGGLEKEYS, disabledToggle.cbSize, ref disabledToggle, 0);

            _initialized = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disabling accessibility keys: {ex.Message}");
        }
    }

    public void RestoreAccessibilityShortcuts()
    {
        if (!_initialized || _disposed) return;

        try
        {
            SystemParametersInfo(SPI_SETSTICKYKEYS, _originalStickyKeys.cbSize, ref _originalStickyKeys, 0);
            SystemParametersInfo(SPI_SETFILTERKEYS, _originalFilterKeys.cbSize, ref _originalFilterKeys, 0);
            SystemParametersInfo(SPI_SETTOGGLEKEYS, _originalToggleKeys.cbSize, ref _originalToggleKeys, 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restoring accessibility keys: {ex.Message}");
        }

        _disposed = true;
    }

    public void Dispose()
    {
        RestoreAccessibilityShortcuts();
    }
}
