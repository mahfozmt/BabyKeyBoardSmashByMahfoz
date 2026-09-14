using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BabySmashBN.Interop;

public sealed class LowLevelKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const int VK_TAB = 0x09;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;
    private const int VK_MENU = 0x12; // Alt key
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_APPS = 0x5D;
    private const int VK_F4 = 0x73;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;

    private const int LLKHF_ALTDOWN = 0x20;

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _isEscapeDown;
    private bool _disposed;

    public event Action<int, bool>? KeyIntercepted; // (vkCode, isDown)
    public event Action<bool>? EscapeStateChanged; // isDown
    public event Action? ExitRequested; // Triggered when Alt+F4 is pressed

    public LowLevelKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookId != IntPtr.Zero) return;

        try
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            IntPtr hMod = GetModuleHandle(curModule?.ModuleName);
            if (hMod == IntPtr.Zero)
            {
                hMod = GetModuleHandle(null);
            }

            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hMod, 0);
            if (_hookId == IntPtr.Zero)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to install keyboard hook: {ex.Message}");
        }
    }

    public void Uninstall()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var msg = wParam.ToInt32();
            var isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            var isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var vkCode = (int)kb.vkCode;
            var altDown = (kb.flags & LLKHF_ALTDOWN) != 0 ||
                          (GetAsyncKeyState(VK_MENU) & 0x8000) != 0 ||
                          msg == WM_SYSKEYDOWN || msg == WM_SYSKEYUP;

            // 1. Alt + F4 for adult exit: trigger ExitRequested immediately and pass to system
            if (vkCode == VK_F4 && altDown)
            {
                if (isDown)
                {
                    ExitRequested?.Invoke();
                }
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // 2. Track Escape key press/release for hold-to-exit
            // Note: Filter out Windows auto-repeat WM_KEYDOWN messages so the timer isn't restarted!
            if (vkCode == VK_ESCAPE)
            {
                if (isDown)
                {
                    if (!_isEscapeDown)
                    {
                        _isEscapeDown = true;
                        EscapeStateChanged?.Invoke(true);
                    }
                }
                else if (isUp)
                {
                    _isEscapeDown = false;
                    EscapeStateChanged?.Invoke(false);
                }
                return (IntPtr)1; // Swallow Escape from OS
            }

            // 3. Swallow Windows keys, Context Menu key
            if (vkCode == VK_LWIN || vkCode == VK_RWIN || vkCode == VK_APPS)
            {
                return (IntPtr)1;
            }

            // 4. Swallow Alt + Tab, Alt + Esc, Alt + Space
            if (altDown && (vkCode == VK_TAB || vkCode == VK_ESCAPE || vkCode == VK_SPACE))
            {
                return (IntPtr)1;
            }

            // 5. Check Ctrl + Esc (Start menu)
            bool ctrlDown = (GetAsyncKeyState(VK_LCONTROL) & 0x8000) != 0 ||
                            (GetAsyncKeyState(VK_RCONTROL) & 0x8000) != 0;
            if (ctrlDown && vkCode == VK_ESCAPE)
            {
                return (IntPtr)1;
            }

            // 6. Notify application of keypress and swallow so other apps / OS shortcuts don't fire
            if (isDown)
            {
                KeyIntercepted?.Invoke(vkCode, true);
                return (IntPtr)1;
            }
            else if (isUp)
            {
                KeyIntercepted?.Invoke(vkCode, false);
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Uninstall();
            _isEscapeDown = false;
            _disposed = true;
        }
    }
}
