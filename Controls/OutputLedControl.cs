namespace PerceptronSimulator.Controls;

public class OutputLedControl : Control
{
    private bool _isOn;

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

    public string Label { get; set; } = "+";

    public OutputLedControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(40, 50);
        Font = new Font("Consolas", 8f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int ledSize = 20;
        int ledX = (Width - ledSize) / 2;
        int ledY = 5;

        // Glow effect when on
        if (_isOn)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(30, 0, 255, 0));
            g.FillEllipse(glowBrush, ledX - 8, ledY - 8, ledSize + 16, ledSize + 16);
            using var glowBrush2 = new SolidBrush(Color.FromArgb(50, 0, 255, 0));
            g.FillEllipse(glowBrush2, ledX - 4, ledY - 4, ledSize + 8, ledSize + 8);
        }

        // LED body
        Color ledColor = _isOn ? Color.FromArgb(0, 220, 0) : Color.FromArgb(0, 60, 0);
        using (var ledBrush = new SolidBrush(ledColor))
        {
            g.FillEllipse(ledBrush, ledX, ledY, ledSize, ledSize);
        }

        // Highlight
        if (_isOn)
        {
            using var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            g.FillEllipse(highlightBrush, ledX + 4, ledY + 3, 6, 4);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(100, 100, 100), 1))
        {
            g.DrawEllipse(borderPen, ledX, ledY, ledSize, ledSize);
        }

        // Label
        using var labelBrush = new SolidBrush(Color.FromArgb(140, 140, 140));
        var labelSize = g.MeasureString(Label, Font);
        g.DrawString(Label, Font, labelBrush, (Width - labelSize.Width) / 2, ledY + ledSize + 5);
    }
}
