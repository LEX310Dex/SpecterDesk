using System.Runtime.InteropServices;

namespace GhostCursorV6;

internal static class Native
{
    internal const uint WDA_EXCLUDEFROMCAPTURE = 0x11;
    internal const uint SPI_SETCURSORS = 0x57;
    internal const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000;
    internal const int WM_HOTKEY = 0x0312, WM_NCHITTEST = 0x0084, WM_MOUSEACTIVATE = 0x0021;
    internal const int HTTRANSPARENT = -1, MA_NOACTIVATE = 3;
    internal const int GWL_EXSTYLE = -20;
    internal const long WS_EX_TRANSPARENT = 0x20, WS_EX_NOACTIVATE = 0x08000000;
    internal static readonly int[] CursorIds = { 32512,32513,32514,32515,32516,32642,32643,32644,32645,32646,32648,32649,32650,32651 };

    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetWindowDisplayAffinity(IntPtr hwnd, out uint affinity);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hwnd, int id, uint mods, uint key);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hwnd, int id);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr LoadCursorW(IntPtr instance, IntPtr name);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr CopyIcon(IntPtr hIcon);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr CreateCursor(IntPtr instance, int hotX, int hotY, int width, int height, byte[] andMask, byte[] xorMask);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyCursor(IntPtr cursor);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetSystemCursor(IntPtr cursor, uint id);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SystemParametersInfoW(uint action, uint param, IntPtr value, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    internal static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    internal static bool Exclude(IntPtr hwnd) =>
        SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE) &&
        GetWindowDisplayAffinity(hwnd, out uint a) && a == WDA_EXCLUDEFROMCAPTURE;
    internal static void Restore() => SystemParametersInfoW(SPI_SETCURSORS, 0, IntPtr.Zero, 0);



internal static readonly IntPtr HWND_TOPMOST = new(-1);

internal const uint SWP_NOSIZE = 0x0001;
internal const uint SWP_NOACTIVATE = 0x0010;
internal const uint SWP_SHOWWINDOW = 0x0040;

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool SetWindowPos(
    IntPtr hwnd,
    IntPtr insertAfter,
    int x,
    int y,
    int width,
    int height,
    uint flags
);

[StructLayout(LayoutKind.Sequential)]
internal struct ICONINFO
{
    [MarshalAs(UnmanagedType.Bool)]
    public bool fIcon;
    public uint xHotspot;
    public uint yHotspot;
    public IntPtr hbmMask;
    public IntPtr hbmColor;
}

[DllImport("user32.dll", SetLastError = true)]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool GetIconInfo(
    IntPtr icon,
    out ICONINFO info
);

[DllImport("gdi32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
internal static extern bool DeleteObject(IntPtr obj);


}
