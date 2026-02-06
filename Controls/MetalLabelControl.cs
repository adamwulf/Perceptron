namespace PerceptronSimulator.Controls;

public class MetalLabelControl : Control
{
    private string _text = "";

    public string LabelText
    {
        get => _text;
        set
        {
            _text = value;
            Invalidate();
        }
    }

    /// <summary>
    /// When true, uses more subdued colors for a less prominent appearance.
    /// </summary>
    public bool Subdued { get; set; } = false;

    /// <summary>
    /// Optional custom text color. When set, overrides the default engraved text color.
    /// </summary>
    public Color? CustomTextColor { get; set; } = null;

    public MetalLabelControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(55, 24);
        Font = new Font("Consolas", 8f, FontStyle.Bold);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle plateRect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Metal plate background
        using (var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            plateRect,
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(50, 55, 50),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(plateBrush, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Subtle brushed texture
        using (var texturePen = new Pen(Color.FromArgb(12, 255, 255, 255), 1))
        {
            for (int y = plateRect.Y + 3; y < plateRect.Bottom - 3; y += 2)
            {
                g.DrawLine(texturePen, plateRect.X + 8, y, plateRect.Right - 8, y);
            }
        }

        // Beveled edge - light top
        using (var lightPen = new Pen(Color.FromArgb(90, 95, 90), 1))
        {
            g.DrawLine(lightPen, plateRect.X + 2, plateRect.Y + 1, plateRect.Right - 2, plateRect.Y + 1);
        }

        // Beveled edge - dark bottom
        using (var darkPen = new Pen(Color.FromArgb(35, 40, 35), 1))
        {
            g.DrawLine(darkPen, plateRect.X + 2, plateRect.Bottom - 1, plateRect.Right - 2, plateRect.Bottom - 1);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(40, 45, 40), 1))
        {
            g.DrawRoundedRectangle(borderPen, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Screw holes in corners
        DrawScrew(g, plateRect.X + 3, plateRect.Y + 3);
        DrawScrew(g, plateRect.Right - 6, plateRect.Y + 3);
        DrawScrew(g, plateRect.X + 3, plateRect.Bottom - 6);
        DrawScrew(g, plateRect.Right - 6, plateRect.Bottom - 6);

        // Engraved text
        var textSize = g.MeasureString(_text, Font);
        float textX = (Width - textSize.Width) / 2;
        float textY = (Height - textSize.Height) / 2;

        // Shadow (engraved effect)
        var shadowColor = Subdued ? Color.FromArgb(25, 30, 25) : Color.FromArgb(30, 35, 30);
        using (var shadowBrush = new SolidBrush(shadowColor))
        {
            g.DrawString(_text, Font, shadowBrush, textX + 1, textY + 1);
        }
        // Light text - use custom color if set, otherwise default engraved style
        var textColor = CustomTextColor ?? (Subdued ? Color.FromArgb(85, 90, 85) : Color.FromArgb(130, 135, 130));
        using (var textBrush = new SolidBrush(textColor))
        {
            g.DrawString(_text, Font, textBrush, textX, textY);
        }
    }

    private void DrawScrew(Graphics g, int x, int y)
    {
        int size = 3;

        // Screw hole shadow
        using (var holeBrush = new SolidBrush(Color.FromArgb(25, 30, 25)))
        {
            g.FillEllipse(holeBrush, x, y, size, size);
        }

        // Screw head highlight
        using (var screwBrush = new SolidBrush(Color.FromArgb(60, 65, 60)))
        {
            g.FillEllipse(screwBrush, x, y, size - 1, size - 1);
        }
    }
}
