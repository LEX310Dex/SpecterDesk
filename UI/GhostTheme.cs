using System.Drawing;
using System.Windows.Forms;

namespace GhostCursorV6.UI;

internal static class GhostTheme
{
    internal static readonly Color Canvas = ColorTranslator.FromHtml("#101216");
    internal static readonly Color Surface = ColorTranslator.FromHtml("#1B2025");
    internal static readonly Color Raised = ColorTranslator.FromHtml("#252B32");
    internal static readonly Color Border = ColorTranslator.FromHtml("#30363C");
    internal static readonly Color Text = ColorTranslator.FromHtml("#F1F3F4");
    internal static readonly Color Muted = ColorTranslator.FromHtml("#98A2AD");
    internal static readonly Color Accent = ColorTranslator.FromHtml("#7FB4A0");
    internal static readonly Color AccentSurface = ColorTranslator.FromHtml("#23433B");

    internal static Font Font(float points = 10f, bool bold = false) =>
        new("Segoe UI", points, bold ? FontStyle.Bold : FontStyle.Regular);

    internal static Button Button(string label, bool primary = false, int width = 122)
    {
        var b = new Button
        {
            Text = label,
            Width = width,
            Height = 37,
            Font = Font(9f, primary),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Raised,
            ForeColor = primary ? Canvas : Text,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 8),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = primary ? 0 : 1;
        b.FlatAppearance.BorderColor = Border;
        b.FlatAppearance.MouseOverBackColor = primary ? ColorTranslator.FromHtml("#9CC9B9") : ColorTranslator.FromHtml("#343C45");
        return b;
    }

    internal static Label Label(string value, float size = 10f, bool bold = false, Color? color = null) =>
        new() { Text = value, Font = Font(size, bold), ForeColor = color ?? Text, AutoSize = true, BackColor = Color.Transparent };

    internal static Panel Card() => new() { BackColor = Surface, Padding = new Padding(19), Margin = new Padding(0, 0, 0, 12) };
}
