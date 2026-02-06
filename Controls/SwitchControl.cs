namespace PerceptronSimulator.Controls;

public class SwitchControl : Control
{
    private bool _isOn;
    private bool _isHovered;

    public event EventHandler? StateChanged;

    public bool IsOn
    {
        get => _isOn;
        set
        {
            if (_isOn != value)
            {
                _isOn = value;
                Invalidate();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public int Value => _isOn ? 1 : -1;

    public SwitchControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(50, 80);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Scale all dimensions based on control size
        float scaleX = Width / 50f;
        float scaleY = Height / 80f;
        float scale = Math.Min(scaleX, scaleY);

        int ledSize = Math.Max(6, (int)(12 * scale));
        int ledX = (Width - ledSize) / 2;
        int ledY = Math.Max(2, (int)(4 * scale));

        // Draw LED
        using (var ledBrush = new SolidBrush(_isOn ? Color.FromArgb(255, 60, 60) : Color.FromArgb(60, 20, 20)))
        {
            g.FillEllipse(ledBrush, ledX, ledY, ledSize, ledSize);
        }

        // LED glow effect when on
        if (_isOn)
        {
            int glowPad = Math.Max(2, (int)(3 * scale));
            using var glowBrush = new SolidBrush(Color.FromArgb(40, 255, 100, 100));
            g.FillEllipse(glowBrush, ledX - glowPad, ledY - glowPad, ledSize + glowPad * 2, ledSize + glowPad * 2);
        }

        // LED border
        using (var ledBorder = new Pen(Color.FromArgb(100, 100, 100), 1))
        {
            g.DrawEllipse(ledBorder, ledX, ledY, ledSize, ledSize);
        }

        // Switch track
        int trackWidth = Math.Max(12, (int)(24 * scale));
        int trackHeight = Math.Max(22, (int)(44 * scale));
        int trackX = (Width - trackWidth) / 2;
        int trackY = ledY + ledSize + Math.Max(4, (int)(8 * scale));
        int cornerRadius = Math.Max(2, (int)(4 * scale));

        using (var trackBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
        {
            g.FillRoundedRectangle(trackBrush, trackX, trackY, trackWidth, trackHeight, cornerRadius);
        }

        using (var trackBorder = new Pen(_isHovered ? Color.FromArgb(100, 100, 100) : Color.FromArgb(70, 70, 70), 1))
        {
            g.DrawRoundedRectangle(trackBorder, trackX, trackY, trackWidth, trackHeight, cornerRadius);
        }

        // Switch toggle
        int togglePad = Math.Max(2, (int)(3 * scale));
        int toggleHeight = Math.Max(9, (int)(18 * scale));
        int toggleY = _isOn ? trackY + togglePad : trackY + trackHeight - toggleHeight - togglePad;

        // Toggle color: off is darker (shadowed), on is medium gray
        Color toggleColor = _isOn ? Color.FromArgb(100, 100, 100) : Color.FromArgb(70, 70, 70);
        using (var toggleBrush = new SolidBrush(toggleColor))
        {
            g.FillRoundedRectangle(toggleBrush, trackX + togglePad, toggleY, trackWidth - togglePad * 2, toggleHeight, Math.Max(1, cornerRadius - 1));
        }

        // Toggle highlight (subtle)
        int hlPad = Math.Max(1, (int)(2 * scale));
        int highlightAlpha = _isOn ? 20 : 40;
        using (var highlightBrush = new SolidBrush(Color.FromArgb(highlightAlpha, 255, 255, 255)))
        {
            g.FillRectangle(highlightBrush, trackX + togglePad + hlPad, toggleY + hlPad, trackWidth - togglePad * 2 - hlPad * 2, Math.Max(2, (int)(3 * scale)));
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnClick(EventArgs e)
    {
        IsOn = !IsOn;
        base.OnClick(e);
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics g, Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = CreateRoundedRectanglePath(x, y, width, height, radius);
        g.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics g, Pen pen, int x, int y, int width, int height, int radius)
    {
        using var path = CreateRoundedRectanglePath(x, y, width, height, radius);
        g.DrawPath(pen, path);
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(int x, int y, int width, int height, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int diameter = radius * 2;

        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}
