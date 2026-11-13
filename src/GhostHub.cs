using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using GhostCursorV6.UI;

namespace GhostCursorV6;

internal sealed class GhostHub : Form
{
    private const int HK_HIDE = 1, HK_RESTORE = 2, HK_QUIT = 3, HK_SHOT = 4, HK_PANEL = 5;
    private const string UiBuild = "UI v0.7.2";

    private readonly CursorLayer cursor = new();
    private DashboardForm? dashboard;
    private readonly NotifyIcon tray;

    private readonly WebView2 browser = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly TextBox notes = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Both,
        AcceptsReturn = true,
        BorderStyle = BorderStyle.None,
        Margin = Padding.Empty
    };

    private readonly TextBox url = new()
    {
        BorderStyle = BorderStyle.None,
        AutoSize = false,
        Margin = Padding.Empty
    };

    private readonly Panel host = new()
    {
        Dock = DockStyle.Fill,
        Margin = Padding.Empty,
        Padding = new Padding(1)
    };

    private readonly Label state = new()
    {
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true,
        Margin = Padding.Empty
    };

    private readonly string dataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GhostCursor");

    private bool allowExit, captureReady, browserReady, notesMode, notesDirty;
    private readonly HashSet<int> registered = new();

    public GhostHub()
    {
        Text = "GhostCursor v0.7.2  /  Private Workspace";
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = GhostTheme.Canvas;
        ForeColor = GhostTheme.Text;
        Font = GhostTheme.Font(10f);

        // Program.cs already enables PerMonitorV2. Use ONE WinForms scaling system here.
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 820);
        MinimumSize = new Size(960, 720);
        DoubleBuffered = true;

        Directory.CreateDirectory(dataDir);
        notes.Text = File.Exists(NotesPath) ? File.ReadAllText(NotesPath) : "Private notes";
        notes.TextChanged += (_, _) => notesDirty = true;
        notes.BackColor = GhostTheme.Surface;
        notes.ForeColor = GhostTheme.Text;
        notes.Font = GhostTheme.Font(11f);

        // Entire workspace uses one TableLayoutPanel. Every band owns its own row,
        // so controls cannot overlap vertically even under DPI scaling.
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = GhostTheme.Canvas,
            Padding = new Padding(24, 22, 24, 18),
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f)); // header
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f)); // gap
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f)); // tabs
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f)); // gap
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f)); // navigation/url
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f)); // gap
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // content
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 14f)); // gap
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f)); // status
        Controls.Add(root);

        // ----- Header -----
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(18, 10, 12, 10),
            BackColor = GhostTheme.Surface
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(header, 0, 0);

        var headingBox = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        headingBox.RowStyles.Add(new RowStyle(SizeType.Percent, 58f));
        headingBox.RowStyles.Add(new RowStyle(SizeType.Percent, 42f));
        var title = new Label
        {
            Text = "GHOSTCURSOR  /  PRIVATE WORKSPACE",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Font = GhostTheme.Font(11f, true),
            ForeColor = GhostTheme.Text,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        var subtitle = new Label
        {
            Text = "Private tools above your presentation · excluded from supported captures",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            Font = GhostTheme.Font(8.7f),
            ForeColor = GhostTheme.Muted,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        headingBox.Controls.Add(title, 0, 0);
        headingBox.Controls.Add(subtitle, 0, 1);
        header.Controls.Add(headingBox, 0, 0);

        var headerActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            Padding = new Padding(8, 2, 0, 0),
            BackColor = Color.Transparent
        };
        header.Controls.Add(headerActions, 1, 0);
        headerActions.Controls.Add(MakeButton("Dashboard", ShowDashboard, false, 126));
        headerActions.Controls.Add(MakeButton("Hide", Hide, false, 86));

        // ----- Tabs -----
        var tabBand = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(16, 9, 16, 9),
            BackColor = GhostTheme.Surface
        };
        root.Controls.Add(tabBand, 0, 2);

        var tabFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        tabBand.Controls.Add(tabFlow);

        // AutoSize + MinimumSize means labels can NEVER collapse to "Brow" / "Not".
        var chatButton = MakeButton("ChatGPT", () => Navigate("https://chatgpt.com"), true, 142);
        var browseButton = MakeButton("Browser", () => Navigate("https://www.bing.com"), false, 142);
        var notesButton = MakeButton("Notes", ShowNotes, false, 142);
        tabFlow.Controls.Add(chatButton);
        tabFlow.Controls.Add(browseButton);
        tabFlow.Controls.Add(notesButton);

        // ----- Navigation + URL -----
        // Fixed, non-auto-sized controls: these labels cannot collapse to partial text.
        var navBand = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(16, 10, 16, 10),
            BackColor = GhostTheme.Surface
        };
        root.Controls.Add(navBand, 0, 4);

        var navGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        navGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        navGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96f));
        navGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
        navGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        navGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146f));
        navBand.Controls.Add(navGrid);

        var backButton = MakeNavButton("Back", () =>
        {
            if (browserReady && browser.CanGoBack) browser.GoBack();
        });
        backButton.Margin = new Padding(0, 0, 10, 0);

        var nextButton = MakeNavButton("Forward", () =>
        {
            if (browserReady && browser.CanGoForward) browser.GoForward();
        });
        nextButton.Margin = new Padding(0, 0, 12, 0);

        var copyButton = MakeNavButton("Copy notes", () =>
        {
            if (notesMode && !string.IsNullOrEmpty(notes.Text)) Clipboard.SetText(notes.Text);
        });
        copyButton.Margin = new Padding(12, 0, 0, 0);

        var urlShell = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = GhostTheme.Raised,
            Margin = new Padding(0),
            Padding = new Padding(14, 11, 14, 11)
        };
        url.Text = "https://chatgpt.com";
        url.Font = GhostTheme.Font(10f);
        url.BackColor = GhostTheme.Raised;
        url.ForeColor = GhostTheme.Text;
        url.AutoSize = false;
        url.Dock = DockStyle.Fill;
        url.TextAlign = HorizontalAlignment.Left;
        urlShell.Controls.Add(url);

        navGrid.Controls.Add(backButton, 0, 0);
        navGrid.Controls.Add(nextButton, 1, 0);
        navGrid.Controls.Add(urlShell, 2, 0);
        navGrid.Controls.Add(copyButton, 3, 0);

        url.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                Navigate(url.Text);
                e.SuppressKeyPress = true;
            }
        };

        // ----- Content -----
        host.BackColor = GhostTheme.Surface;
        host.Padding = new Padding(10);
        host.Controls.Add(browser);
        host.Controls.Add(notes);
        notes.Visible = false;
        root.Controls.Add(host, 0, 6);

        // ----- Status -----
        state.BackColor = GhostTheme.Surface;
        state.ForeColor = GhostTheme.Muted;
        state.Font = GhostTheme.Font(8.7f);
        state.Padding = new Padding(12, 0, 12, 0);
        state.Text = $"{UiBuild}  ·  Private workspace  ·  Capture exclusion initializing";
        root.Controls.Add(state, 0, 8);

        // Tray
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Obsidian dashboard", null, (_, _) => ShowDashboard());
        menu.Items.Add("Show / hide private workspace", null, (_, _) => TogglePanel());
        menu.Items.Add("Enable Ghost Cursor", null, (_, _) => SetGhost(true));
        menu.Items.Add("Restore normal cursor", null, (_, _) => SetGhost(false));
        menu.Items.Add("Silent screenshot + clipboard", null, (_, _) => Screenshot());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit safely", null, (_, _) => ExitSafely());

        tray = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "GhostCursor v0.7.2",
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.DoubleClick += (_, _) => ShowDashboard();

        Shown += async (_, _) =>
        {
            await InitBrowser();
        };
        FormClosing += (_, e) =>
        {
            if (!allowExit)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    private static Button MakeNavButton(string caption, Action click)
    {
        var b = new Button
        {
            Text = caption,
            AutoSize = false,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 44),
            Padding = new Padding(8, 0, 8, 0),
            FlatStyle = FlatStyle.Flat,
            Font = GhostTheme.Font(9.2f, false),
            ForeColor = GhostTheme.Text,
            BackColor = GhostTheme.Raised,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            UseCompatibleTextRendering = false
        };
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = GhostTheme.Border;
        b.FlatAppearance.MouseOverBackColor = ColorTranslator.FromHtml("#343C45");
        b.Click += (_, _) => click();
        return b;
    }

    private static Button MakeButton(string caption, Action click, bool primary, int minimumWidth)
    {
        var b = new Button
        {
            Text = caption,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(minimumWidth, 44),
            Height = 44,
            Padding = new Padding(18, 0, 18, 0),
            Margin = new Padding(0, 0, 12, 0),
            FlatStyle = FlatStyle.Flat,
            Font = GhostTheme.Font(9.5f, primary),
            ForeColor = primary ? GhostTheme.Canvas : GhostTheme.Text,
            BackColor = primary ? GhostTheme.Accent : GhostTheme.Raised,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            UseVisualStyleBackColor = false,
            UseCompatibleTextRendering = false
        };
        b.FlatAppearance.BorderSize = primary ? 0 : 1;
        b.FlatAppearance.BorderColor = GhostTheme.Border;
        b.FlatAppearance.MouseOverBackColor = primary
            ? ColorTranslator.FromHtml("#9CC9B9")
            : ColorTranslator.FromHtml("#343C45");
        b.Click += (_, _) => click();
        return b;
    }

    private string NotesPath => Path.Combine(dataDir, "notes.txt");

    private void SaveNotes()
    {
        if (!notesDirty) return;
        File.WriteAllText(NotesPath, notes.Text);
        notesDirty = false;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        captureReady = Native.Exclude(Handle);
        _ = cursor.Handle;

        bool Reg(int id, uint mods, Keys key) =>
            Native.RegisterHotKey(Handle, id, mods | Native.MOD_NOREPEAT, (uint)key);
        void TryReg(int id, uint mods, Keys key)
        {
            if (Reg(id, mods, key)) registered.Add(id);
        }

        uint baseMods = Native.MOD_CONTROL | Native.MOD_ALT;
        TryReg(HK_HIDE, baseMods, Keys.H);
        TryReg(HK_RESTORE, baseMods, Keys.R);
        TryReg(HK_QUIT, baseMods, Keys.Q);
        TryReg(HK_SHOT, baseMods | Native.MOD_SHIFT, Keys.S);
        TryReg(HK_PANEL, baseMods, Keys.P);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Native.WM_HOTKEY)
        {
            switch (m.WParam.ToInt32())
            {
                case HK_HIDE: SetGhost(true); break;
                case HK_RESTORE: SetGhost(false); break;
                case HK_QUIT: ExitSafely(); break;
                case HK_SHOT: Screenshot(); break;
                case HK_PANEL: TogglePanel(); break;
            }
            return;
        }
        base.WndProc(ref m);
    }

    private async Task InitBrowser()
    {
        if (!captureReady || !cursor.Ready)
        {
            Hide();
            MessageBox.Show("Capture exclusion unavailable. GhostCursor will close.", "GhostCursor");
            ExitSafely();
            return;
        }

        try
        {
            var profile = Path.Combine(dataDir, "WebView2Profile");
            var env = await CoreWebView2Environment.CreateAsync(userDataFolder: profile);
            await browser.EnsureCoreWebView2Async(env);
            browserReady = true;

            browser.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var u) &&
                    (u.Scheme == Uri.UriSchemeHttps || u.Scheme == Uri.UriSchemeHttp))
                    browser.CoreWebView2.Navigate(u.ToString());
            };

            browser.NavigationCompleted += (_, _) =>
            {
                if (browser.Source != null) url.Text = browser.Source.ToString();
            };

            Navigate("https://chatgpt.com");
            state.Text = $"{UiBuild}  ·  Workspace excluded from supported captures  ·  Ctrl+Alt+P to hide";
        }
        catch (Exception ex)
        {
            state.Text = $"{UiBuild}  ·  Browser unavailable: {ex.Message}";
            ShowNotes();
        }
    }

    private void Navigate(string input)
    {
        if (!browserReady) return;
        string destination = input.Trim();
        if (!destination.Contains("://"))
            destination = destination.Contains('.') && !destination.Contains(' ')
                ? "https://" + destination
                : "https://www.bing.com/search?q=" + Uri.EscapeDataString(destination);

        if (!Uri.TryCreate(destination, UriKind.Absolute, out var u) ||
            (u.Scheme != "https" && u.Scheme != "http")) return;

        notesMode = false;
        notes.Visible = false;
        browser.Visible = true;
        browser.BringToFront();
        browser.CoreWebView2.Navigate(u.ToString());
    }

    private void ShowNotes()
    {
        notesMode = true;
        browser.Visible = false;
        notes.Visible = true;
        notes.BringToFront();
        notes.Focus();
    }

    private void ShowDashboard()
    {
        if (!captureReady) return;
        dashboard ??= new DashboardForm(
            () => cursor.GhostEnabled,
            enabled => SetGhost(enabled),
            () => { if (!Visible) TogglePanel(); else Activate(); },
            Screenshot,
            () => SetGhost(false));
        dashboard.Show();
        dashboard.BringToFront();
        dashboard.Activate();
    }

    private void TogglePanel()
    {
        if (!captureReady) return;
        if (Visible)
        {
            SaveNotes();
            Hide();
        }
        else
        {
            Show();
            TopMost = true;
            BringToFront();
            Activate();
        }
    }

    private void SetGhost(bool enabled)
    {
        if (enabled)
        {
            if (!cursor.Enable())
                MessageBox.Show("Cannot enable protected cursor. Normal cursors restored.", "GhostCursor");
        }
        else
        {
            cursor.Disable();
        }
    }

    private void Screenshot()
    {
        try
        {
            Rectangle bounds = SystemInformation.VirtualScreen;
            using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);

            string folder = Path.Combine(dataDir, "Screenshots");
            Directory.CreateDirectory(folder);
            string name = "GhostCursor_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".png";
            bmp.Save(Path.Combine(folder, name), ImageFormat.Png);

            for (int tries = 0; tries < 5; tries++)
            {
                try
                {
                    Clipboard.SetImage(bmp);
                    break;
                }
                catch (System.Runtime.InteropServices.ExternalException) when (tries < 4)
                {
                    Thread.Sleep(40);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Screenshot failed: " + ex.Message, "GhostCursor");
        }
    }

    private void ExitSafely()
    {
        try { SaveNotes(); } catch { }
        cursor.Disable();
        allowExit = true;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (int id in registered)
                Native.UnregisterHotKey(Handle, id);
            registered.Clear();
            dashboard?.Dispose();
            cursor.Dispose();
            tray.Visible = false;
            tray.Dispose();
            Native.Restore();
        }
        base.Dispose(disposing);
    }
}
