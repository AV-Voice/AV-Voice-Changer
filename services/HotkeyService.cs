using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace VybeShift.UI.Services;

/// <summary>
/// Registers Windows global hotkeys (no focus required).
/// F9 = Toggle, F10 = Mute, F11 = Cycle Preset.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int ID_F9  = 9001;
    private const int ID_F10 = 9002;
    private const int ID_F11 = 9003;
    private const int WM_HOTKEY = 0x0312;

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _source;

    public event Action? OnF9;
    public event Action? OnF10;
    public event Action? OnF11;

    public void Register(IntPtr hwnd)
    {
        _hwnd = hwnd;
        _source = HwndSource.FromHwnd(hwnd);
        _source?.AddHook(WndProc);

        RegisterHotKey(hwnd, ID_F9,  0, 0x78); // F9
        RegisterHotKey(hwnd, ID_F10, 0, 0x79); // F10
        RegisterHotKey(hwnd, ID_F11, 0, 0x7A); // F11
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if      (id == ID_F9)  OnF9?.Invoke();
            else if (id == ID_F10) OnF10?.Invoke();
            else if (id == ID_F11) OnF11?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, ID_F9);
            UnregisterHotKey(_hwnd, ID_F10);
            UnregisterHotKey(_hwnd, ID_F11);
        }
        _source?.RemoveHook(WndProc);
    }
}
