
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GhostCursorV6;

internal sealed class CursorLayer : Form
{
    private readonly IntPtr arrowCopy;

    private int hotspotX;
    private int hotspotY;

    private readonly System.Windows.Forms.Timer follow =
        new() { Interval = 10 };

    internal bool Ready { get; private set; }
    internal bool GhostEnabled { get; private set; }

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const uint DI_NORMAL = 0x0003;
    private const uint DI_DEFAULTSIZE = 0x0008;

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO
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
    private static extern bool GetIconInfo(
        IntPtr hIcon,
        out ICONINFO info
    );

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(
        IntPtr obj
    );

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags
    );

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DrawIconEx(
        IntPtr hdc,
        int x,
        int y,
        IntPtr hIcon,
        int width,
        int height,
        uint step,
        IntPtr brush,
        uint flags
    );

    public CursorLayer()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        TopMost = true;

        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;

        Size = SystemInformation.CursorSize;

        // Copy the original Windows arrow before
        // replacing system cursors with transparent ones.
        IntPtr systemArrow = Native.LoadCursorW(
            IntPtr.Zero,
            (IntPtr)32512
        );

        if (systemArrow == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Cannot load Windows arrow cursor."
            );
        }

        arrowCopy = Native.CopyIcon(systemArrow);

        if (arrowCopy == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Cannot copy Windows arrow cursor."
            );
        }

        // Read the original cursor hotspot.
        if (GetIconInfo(arrowCopy, out ICONINFO info))
        {
            hotspotX = (int)info.xHotspot;
            hotspotY = (int)info.yHotspot;

            if (info.hbmMask != IntPtr.Zero)
                DeleteObject(info.hbmMask);

            if (info.hbmColor != IntPtr.Zero)
                DeleteObject(info.hbmColor);
        }

        follow.Tick += (_, _) =>
        {
            if (GhostEnabled)
                UpdateCursorPosition();
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var p = base.CreateParams;

            p.ExStyle |= unchecked((int)(
                Native.WS_EX_TRANSPARENT |
                Native.WS_EX_NOACTIVATE
            ));

            return p;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Exclude the replacement cursor from
        // supported Windows screen capture.
        Ready = Native.Exclude(Handle);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_NCHITTEST)
        {
            m.Result = (IntPtr)Native.HTTRANSPARENT;
            return;
        }

        if (m.Msg == Native.WM_MOUSEACTIVATE)
        {
            m.Result = (IntPtr)Native.MA_NOACTIVATE;
            return;
        }

        base.WndProc(ref m);
    }

    // Render the copied Windows cursor using
    // native Win32 cursor dimensions.
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (arrowCopy == IntPtr.Zero)
            return;

        IntPtr dc = e.Graphics.GetHdc();

        try
        {
            DrawIconEx(
                dc,
                0,
                0,
                arrowCopy,
                0,
                0,
                0,
                IntPtr.Zero,
                DI_NORMAL | DI_DEFAULTSIZE
            );
        }
        finally
        {
            e.Graphics.ReleaseHdc(dc);
        }
    }

    // Track the real mouse position while keeping
    // the replacement pointer above other windows.
    private void UpdateCursorPosition()
    {
        if (!GhostEnabled || !IsHandleCreated)
            return;

        Point pos = System.Windows.Forms.Cursor.Position;

        SetWindowPos(
            Handle,
            HWND_TOPMOST,
            pos.X - hotspotX,
            pos.Y - hotspotY,
            0,
            0,
            SWP_NOSIZE |
            SWP_NOACTIVATE |
            SWP_SHOWWINDOW
        );
    }

    internal bool Enable()
    {
        if (GhostEnabled)
            return true;

        // Ensure the overlay has a window handle.
        if (!IsHandleCreated)
        {
            _ = Handle;
        }

        // Never hide the real cursor unless
        // capture exclusion is configured.
        if (!Ready)
            return false;

        // Make standard Windows system cursors
        // transparent, preserving the background.
        foreach (var id in Native.CursorIds)
        {
            byte[] andMask = new byte[128];
            byte[] xorMask = new byte[128];

            Array.Fill(andMask, (byte)0xFF);

            IntPtr empty = Native.CreateCursor(
                IntPtr.Zero,
                0,
                0,
                32,
                32,
                andMask,
                xorMask
            );

            if (empty == IntPtr.Zero)
            {
                Native.Restore();
                return false;
            }

            if (!Native.SetSystemCursor(
                    empty,
                    (uint)id))
            {
                Native.DestroyCursor(empty);
                Native.Restore();
                return false;
            }

            // SetSystemCursor destroys the supplied
            // cursor handle after a successful call.
        }

        GhostEnabled = true;

        Show();

        UpdateCursorPosition();
        Invalidate();

        follow.Start();

        return true;
    }

    internal void Disable()
    {
        follow.Stop();

        GhostEnabled = false;

        Hide();

        // Restore the configured Windows cursor scheme.
        Native.Restore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disable();

            follow.Dispose();

            if (arrowCopy != IntPtr.Zero)
                Native.DestroyIcon(arrowCopy);
        }

        base.Dispose(disposing);
    }
}
