namespace PerceptronSimulator.Controls;

public class MetalPlateButton : Control
{
    private string _text = "";
    private bool _isHovered;
    private bool _isPressed;

    public string LabelText
    {
        get => _text;
        set
        {
            _text = value;
            Invalidate();
        }
    }

    public event EventHandler? PlateClick;

    public MetalPlateButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(130, 28);
        Font = new Font("Consolas", 8f, FontStyle.Bold);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle plateRect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Adjust colors based on state
        int colorOffset = _isPressed ? -10 : (_isHovered ? 10 : 0);

        // Metal plate background
        using (var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            plateRect,
            Color.FromArgb(70 + colorOffset, 75 + colorOffset, 70 + colorOffset),
            Color.FromArgb(50 + colorOffset, 55 + colorOffset, 50 + colorOffset),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(plateBrush, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Subtle brushed texture
        using (var texturePen = new Pen(Color.FromArgb(12, 255, 255, 255), 1))
        {
            for (int y = plateRect.Y + 4; y < plateRect.Bottom - 4; y += 2)
            {
                g.DrawLine(texturePen, plateRect.X + 10, y, plateRect.Right - 10, y);
            }
        }

        // Beveled edge - light top (inverted when pressed)
        using (var lightPen = new Pen(Color.FromArgb(_isPressed ? 50 : 90, _isPressed ? 55 : 95, _isPressed ? 50 : 90), 1))
        {
            g.DrawLine(lightPen, plateRect.X + 2, plateRect.Y + 1, plateRect.Right - 2, plateRect.Y + 1);
        }

        // Beveled edge - dark bottom
        using (var darkPen = new Pen(Color.FromArgb(_isPressed ? 70 : 35, _isPressed ? 75 : 40, _isPressed ? 70 : 35), 1))
        {
            g.DrawLine(darkPen, plateRect.X + 2, plateRect.Bottom - 1, plateRect.Right - 2, plateRect.Bottom - 1);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(_isHovered ? 60 : 40, _isHovered ? 65 : 45, _isHovered ? 60 : 40), 1))
        {
            g.DrawRoundedRectangle(borderPen, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Screw holes in corners
        DrawScrew(g, plateRect.X + 4, plateRect.Y + 4);
        DrawScrew(g, plateRect.Right - 8, plateRect.Y + 4);
        DrawScrew(g, plateRect.X + 4, plateRect.Bottom - 8);
        DrawScrew(g, plateRect.Right - 8, plateRect.Bottom - 8);

        // Engraved text
        var textSize = g.MeasureString(_text, Font);
        float textX = (Width - textSize.Width) / 2;
        float textY = (Height - textSize.Height) / 2;

        // Shadow (engraved effect)
        using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 35, 30)))
        {
            g.DrawString(_text, Font, shadowBrush, textX + 1, textY + 1);
        }
        // Light text
        int textBrightness = _isHovered ? 160 : 140;
        using (var textBrush = new SolidBrush(Color.FromArgb(textBrightness, textBrightness + 5, textBrightness)))
        {
            g.DrawString(_text, Font, textBrush, textX, textY);
        }
    }

    private void DrawScrew(Graphics g, int x, int y)
    {
        int size = 4;

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

    protected override void OnMouseEnter(EventArgs e)
    {
        _isHovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _isHovered = false;
        _isPressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isPressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_isPressed && e.Button == MouseButtons.Left)
        {
            _isPressed = false;
            Invalidate();
            PlateClick?.Invoke(this, EventArgs.Empty);
        }
        base.OnMouseUp(e);
    }
}
