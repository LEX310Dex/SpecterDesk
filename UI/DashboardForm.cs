using System;
using System.Drawing;
using System.Windows.Forms;

namespace GhostCursorV6.UI;

internal sealed class DashboardForm : Form
{
    private readonly Func<bool> cursorEnabled;
    private readonly Action<bool> toggleCursor;
    private readonly Action openWorkspace;
    private readonly Action capture;
    private readonly Action restore;

    private readonly Label stateLabel;
    private readonly Button cursorButton;
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 350 };

    internal DashboardForm(
        Func<bool> cursorEnabled,
        Action<bool> toggleCursor,
        Action openWorkspace,
        Action capture,
        Action restore)
    {
        this.cursorEnabled = cursorEnabled;
        this.toggleCursor = toggleCursor;
        this.openWorkspace = openWorkspace;
        this.capture = capture;
        this.restore = restore;

        Text = "GhostCursor  /  Control Center";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 760);
        MinimumSize = new Size(780, 660);
        BackColor = GhostTheme.Canvas;
        ForeColor = GhostTheme.Text;
        Font = GhostTheme.Font(10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        Shown += (_, _) =>
        {
            if (!Native.Exclude(Handle))
            {
                Hide();
                MessageBox.Show(
                    "Capture exclusion was unavailable. Dashboard hidden for safety.",
                    "GhostCursor");
            }
        };

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = GhostTheme.Canvas,
            Padding = Padding.Empty
        };
        Controls.Add(scroll);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            BackColor = GhostTheme.Canvas,
            Padding = new Padding(32, 30, 32, 28),
            Margin = Padding.Empty
        };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        scroll.Controls.Add(main);

        void AddBlock(Control control, int bottomMargin = 18)
        {
            int row = main.RowCount++;
            main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            control.Dock = DockStyle.Top;
            control.Margin = new Padding(0, 0, 0, bottomMargin);
            main.Controls.Add(control, 0, row);
        }

        // Header: pure auto-size layout, no absolute coordinates.
        var header = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = GhostTheme.Canvas,
            Margin = Padding.Empty,
            Padding = new Padding(2, 0, 2, 4)
        };
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        header.Controls.Add(MakeText("GHOSTCURSOR", 9.5f, true, GhostTheme.Accent, new Padding(0, 0, 0, 8)), 0, 0);
        header.Controls.Add(MakeText("Your desktop. Your rules.", 22f, true, GhostTheme.Text, new Padding(0, 0, 0, 8)), 0, 1);
        header.Controls.Add(MakeText(
            "Private controls for what you see locally and what supported screen captures receive.",
            9.5f, false, GhostTheme.Muted, Padding.Empty), 0, 2);
        AddBlock(header, 24);

        // Cursor status card.
        var cursorCard = MakeCard();
        cursorCard.RowCount = 3;
        cursorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cursorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cursorCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        cursorCard.Controls.Add(MakeSectionTitle("CURSOR EXCLUSION"), 0, 0);

        var cursorStateRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 8),
            Padding = Padding.Empty
        };
        cursorStateRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cursorStateRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        stateLabel = MakeText("Checking…", 13f, true, GhostTheme.Text, new Padding(0, 7, 16, 0));
        stateLabel.Dock = DockStyle.Fill;
        stateLabel.TextAlign = ContentAlignment.MiddleLeft;

        cursorButton = MakeActionButton("Enable", true, 128);
        cursorButton.Click += (_, _) => this.toggleCursor(!this.cursorEnabled());
        cursorStateRow.Controls.Add(stateLabel, 0, 0);
        cursorStateRow.Controls.Add(cursorButton, 1, 0);
        cursorCard.Controls.Add(cursorStateRow, 0, 1);
        cursorCard.Controls.Add(MakeText(
            "Your replacement pointer stays visible locally while supported capture outputs omit it.",
            9.2f, false, GhostTheme.Muted, Padding.Empty), 0, 2);
        AddBlock(cursorCard);

        // Quick actions card.
        var actionsCard = MakeCard();
        actionsCard.RowCount = 4;
        for (int i = 0; i < 4; i++) actionsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        actionsCard.Controls.Add(MakeSectionTitle("QUICK ACTIONS"), 0, 0);
        actionsCard.Controls.Add(MakeText(
            "Private workspace",
            12f, true, GhostTheme.Text, new Padding(0, 6, 0, 4)), 0, 1);
        actionsCard.Controls.Add(MakeText(
            "ChatGPT, Browser, and Notes remain available above your presentation without crowding the control center.",
            9.2f, false, GhostTheme.Muted, new Padding(0, 0, 0, 14)), 0, 2);

        var actionFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        var openButton = MakeActionButton("Open workspace", true, 168);
        openButton.Click += (_, _) => this.openWorkspace();
        var shotButton = MakeActionButton("Capture screen", false, 158);
        shotButton.Click += (_, _) => this.capture();
        actionFlow.Controls.Add(openButton);
        actionFlow.Controls.Add(shotButton);
        actionsCard.Controls.Add(actionFlow, 0, 3);
        AddBlock(actionsCard);

        // Shortcuts card: two real columns, one row per shortcut.
        var shortcutsCard = MakeCard();
        shortcutsCard.RowCount = 2;
        shortcutsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shortcutsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        shortcutsCard.Controls.Add(MakeSectionTitle("KEYBOARD SHORTCUTS"), 0, 0);

        var shortcutGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0),
            Padding = Padding.Empty
        };
        shortcutGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f));
        shortcutGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 4; i++) shortcutGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddShortcut(shortcutGrid, 0, "Ctrl + Alt + H", "Enable cursor exclusion");
        AddShortcut(shortcutGrid, 1, "Ctrl + Alt + R", "Restore normal Windows cursor");
        AddShortcut(shortcutGrid, 2, "Ctrl + Alt + P", "Show or hide private workspace");
        AddShortcut(shortcutGrid, 3, "Ctrl + Shift + Alt + S", "Screenshot and copy to clipboard");
        shortcutsCard.Controls.Add(shortcutGrid, 0, 1);
        AddBlock(shortcutsCard);

        // Footer / recovery row.
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = GhostTheme.Canvas,
            Margin = Padding.Empty,
            Padding = new Padding(2, 0, 2, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var note = MakeText(
            "Capture exclusion depends on the receiving application's capture method.",
            8.5f, false, GhostTheme.Muted, new Padding(0, 10, 18, 0));
        note.Dock = DockStyle.Fill;
        note.TextAlign = ContentAlignment.MiddleLeft;

        var restoreButton = MakeActionButton("Restore cursor", false, 144);
        restoreButton.Click += (_, _) => this.restore();
        footer.Controls.Add(note, 0, 0);
        footer.Controls.Add(restoreButton, 1, 0);
        AddBlock(footer, 0);

        refresh.Tick += (_, _) => UpdateState();
        refresh.Start();
        UpdateState();
    }

    private static TableLayoutPanel MakeCard()
    {
        var card = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            BackColor = GhostTheme.Surface,
            Padding = new Padding(22, 20, 22, 20),
            Margin = Padding.Empty
        };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        return card;
    }

    private static Label MakeSectionTitle(string text) =>
        MakeText(text, 8.8f, true, GhostTheme.Muted, Padding.Empty);

    private static Label MakeText(string text, float size, bool bold, Color color, Padding margin)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = GhostTheme.Font(size, bold),
            ForeColor = color,
            BackColor = Color.Transparent,
            Margin = margin,
            UseCompatibleTextRendering = false
        };
    }

    private static Button MakeActionButton(string text, bool primary, int width)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = width,
            Height = 44,
            MinimumSize = new Size(width, 44),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? GhostTheme.Accent : GhostTheme.Raised,
            ForeColor = primary ? GhostTheme.Canvas : GhostTheme.Text,
            Font = GhostTheme.Font(9.2f, primary),
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0, 0, 12, 0),
            UseVisualStyleBackColor = false,
            UseCompatibleTextRendering = false
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = GhostTheme.Border;
        button.FlatAppearance.MouseOverBackColor = primary
            ? ColorTranslator.FromHtml("#9CC9B9")
            : ColorTranslator.FromHtml("#343C45");
        return button;
    }

    private static void AddShortcut(TableLayoutPanel grid, int row, string keys, string description)
    {
        var keyLabel = MakeText(keys, 9.2f, true, GhostTheme.Text, new Padding(0, 5, 20, 7));
        var descLabel = MakeText(description, 9.2f, false, GhostTheme.Muted, new Padding(0, 5, 0, 7));
        keyLabel.Dock = DockStyle.Fill;
        descLabel.Dock = DockStyle.Fill;
        keyLabel.TextAlign = ContentAlignment.MiddleLeft;
        descLabel.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(keyLabel, 0, row);
        grid.Controls.Add(descLabel, 1, row);
    }

    private void UpdateState()
    {
        bool enabled = cursorEnabled();
        stateLabel.Text = enabled
            ? "Enabled  ·  Local pointer active"
            : "Disabled  ·  Normal Windows cursor";
        stateLabel.ForeColor = enabled ? GhostTheme.Accent : GhostTheme.Text;
        cursorButton.Text = enabled ? "Disable" : "Enable";
        cursorButton.BackColor = enabled ? GhostTheme.Raised : GhostTheme.Accent;
        cursorButton.ForeColor = enabled ? GhostTheme.Text : GhostTheme.Canvas;
        cursorButton.FlatAppearance.BorderSize = enabled ? 1 : 0;
        cursorButton.FlatAppearance.BorderColor = GhostTheme.Border;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) refresh.Dispose();
        base.Dispose(disposing);
    }
}
