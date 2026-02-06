namespace PerceptronSimulator.Controls;

public class MechanicalPushButton : Control
{
    private bool _isPressed;
    private bool _isHovered;
    private Color _glowColor = Color.Empty;

    public event EventHandler? ButtonClick;

    public string LabelText { get; set; } = "";

    /// <summary>
    /// If true, draws a square button. If false, draws a round button.
    /// </summary>
    public bool IsSquare { get; set; } = false;

    /// <summary>
    /// If true, doesn't reserve label space even when LabelText is empty - truly square button.
    /// </summary>
    public bool _reallySquare_NoJoke { get; set; } = false;

    /// <summary>
    /// Optional glow color for the button interior. Set to Color.Empty for no glow.
    /// </summary>
    public Color GlowColor
    {
        get => _glowColor;
        set
        {
            if (_glowColor != value)
            {
                _glowColor = value;
                Invalidate();
            }
        }
    }

    public MechanicalPushButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(50, 55);
        Cursor = Cursors.Hand;
        Font = new Font("Consolas", 7f, FontStyle.Bold);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int labelHeight = (_reallySquare_NoJoke || string.IsNullOrEmpty(LabelText)) ? 0 : 16;
        int buttonY = labelHeight > 0 ? labelHeight + 2 : 4;
        int buttonWidth = Width - 8;
        int buttonHeight = Height - labelHeight - (labelHeight > 0 ? 8 : 8);
        int buttonSize = Math.Min(buttonWidth, buttonHeight);
        int buttonX = (Width - (IsSquare ? buttonWidth : buttonSize)) / 2;

        // Draw metal plate label above button
        if (!string.IsNullOrEmpty(LabelText))
        {
            Rectangle plateRect = new Rectangle(2, 0, Width - 4, labelHeight);

            // Metal plate background
            using (var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                plateRect,
                Color.FromArgb(65, 70, 65),
                Color.FromArgb(45, 50, 45),
                System.Drawing.Drawing2D.LinearGradientMode.Vertical))
            {
                g.FillRoundedRectangle(plateBrush, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 2);
            }

            // Plate border
            using (var borderPen = new Pen(Color.FromArgb(35, 40, 35), 1))
            {
                g.DrawRoundedRectangle(borderPen, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 2);
            }

            // Corner divots (screw holes)
            DrawDivot(g, plateRect.X + 3, plateRect.Y + 3);
            DrawDivot(g, plateRect.Right - 5, plateRect.Y + 3);
            DrawDivot(g, plateRect.X + 3, plateRect.Bottom - 5);
            DrawDivot(g, plateRect.Right - 5, plateRect.Bottom - 5);

            // Label text
            var textSize = g.MeasureString(LabelText, Font);
            float textX = (Width - textSize.Width) / 2;
            float textY = (labelHeight - textSize.Height) / 2;

            // Shadow
            using (var shadowBrush = new SolidBrush(Color.FromArgb(25, 30, 25)))
            {
                g.DrawString(LabelText, Font, shadowBrush, textX + 1, textY + 1);
            }
            // Text
            using (var textBrush = new SolidBrush(Color.FromArgb(120, 125, 120)))
            {
                g.DrawString(LabelText, Font, textBrush, textX, textY);
            }
        }

        if (IsSquare)
        {
            DrawSquareButton(g, buttonX, buttonY, buttonWidth, buttonHeight);
        }
        else
        {
            DrawRoundButton(g, buttonX, buttonY, buttonSize);
        }
    }

    private void DrawSquareButton(Graphics g, int x, int y, int width, int height)
    {
        int cornerRadius = 4;
        int pressOffset = _isPressed ? 2 : 0;

        // Button housing (recessed area)
        Rectangle housingRect = new Rectangle(x - 2, y, width + 4, height + 4);
        using (var housingBrush = new SolidBrush(Color.FromArgb(15, 15, 15)))
        {
            g.FillRoundedRectangle(housingBrush, housingRect.X, housingRect.Y, housingRect.Width, housingRect.Height, cornerRadius + 2);
        }

        // Button bezel (outer frame)
        Rectangle bezelRect = new Rectangle(x, y + 2, width, height);
        using (var bezelBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            bezelRect,
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(35, 40, 35),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(bezelBrush, bezelRect.X, bezelRect.Y, bezelRect.Width, bezelRect.Height, cornerRadius);
        }

        // Button face
        Rectangle faceRect = new Rectangle(
            x + 3,
            y + 4 + pressOffset,
            width - 6,
            height - 6);

        // Base button color
        Color baseTop = _isPressed ? Color.FromArgb(40, 40, 40) :
                        (_isHovered ? Color.FromArgb(70, 70, 70) : Color.FromArgb(55, 55, 55));
        Color baseBottom = _isPressed ? Color.FromArgb(25, 25, 25) :
                           (_isHovered ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35));

        // Draw base button
        using (var faceBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            faceRect,
            baseTop,
            baseBottom,
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(faceBrush, faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height, cornerRadius - 1);
        }

        // Draw glow effect if color is set
        if (GlowColor != Color.Empty)
        {
            DrawGlowEffect(g, faceRect, cornerRadius - 1);
        }

        // Button highlight (top edge shine) - only when not pressed
        if (!_isPressed)
        {
            using (var highlightPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
            {
                g.DrawLine(highlightPen, faceRect.X + cornerRadius, faceRect.Y + 1,
                          faceRect.Right - cornerRadius, faceRect.Y + 1);
            }
        }

        // Button border
        using (var borderPen = new Pen(Color.FromArgb(20, 20, 20), 1))
        {
            g.DrawRoundedRectangle(borderPen, faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height, cornerRadius - 1);
        }
    }

    private void DrawGlowEffect(Graphics g, Rectangle faceRect, int cornerRadius)
    {
        // Create an uneven glow to simulate light under plastic
        // Brighter in center-bottom, dimmer at edges

        int glowInset = 4;
        Rectangle glowRect = new Rectangle(
            faceRect.X + glowInset,
            faceRect.Y + glowInset,
            faceRect.Width - glowInset * 2,
            faceRect.Height - glowInset * 2);

        if (glowRect.Width <= 0 || glowRect.Height <= 0) return;

        // Create path for glow area
        using (var glowPath = new System.Drawing.Drawing2D.GraphicsPath())
        {
            glowPath.AddRectangle(glowRect);

            // Radial-ish gradient from center-bottom
            using (var glowBrush = new System.Drawing.Drawing2D.PathGradientBrush(glowPath))
            {
                // Intensity varies with press/hover state
                int alpha = _isPressed ? 60 : (_isHovered ? 100 : 80);

                glowBrush.CenterColor = Color.FromArgb(alpha, GlowColor);
                glowBrush.SurroundColors = new[] { Color.FromArgb(10, GlowColor) };

                // Offset center point slightly down and to one side for natural look
                glowBrush.CenterPoint = new PointF(
                    glowRect.X + glowRect.Width * 0.45f,
                    glowRect.Y + glowRect.Height * 0.6f);

                g.FillRectangle(glowBrush, glowRect);
            }
        }

        // Add a subtle secondary highlight spot
        int spotSize = Math.Min(glowRect.Width, glowRect.Height) / 3;
        Rectangle spotRect = new Rectangle(
            glowRect.X + glowRect.Width / 4,
            glowRect.Y + glowRect.Height / 3,
            spotSize,
            spotSize);

        if (spotRect.Width > 2)
        {
            using (var spotPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                spotPath.AddEllipse(spotRect);
                using (var spotBrush = new System.Drawing.Drawing2D.PathGradientBrush(spotPath))
                {
                    int spotAlpha = _isPressed ? 30 : (_isHovered ? 50 : 40);
                    spotBrush.CenterColor = Color.FromArgb(spotAlpha, GlowColor);
                    spotBrush.SurroundColors = new[] { Color.FromArgb(0, GlowColor) };
                    g.FillEllipse(spotBrush, spotRect);
                }
            }
        }
    }

    private void DrawRoundButton(Graphics g, int x, int y, int size)
    {
        // Button housing (recessed area)
        Rectangle housingRect = new Rectangle(x - 3, y, size + 6, size + 6);
        using (var housingBrush = new SolidBrush(Color.FromArgb(15, 15, 15)))
        {
            g.FillEllipse(housingBrush, housingRect);
        }

        // Button bezel (outer ring)
        int bezelInset = 2;
        Rectangle bezelRect = new Rectangle(x - 1, y + bezelInset, size + 2, size + 2);
        using (var bezelBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            bezelRect,
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(35, 40, 35),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillEllipse(bezelBrush, bezelRect);
        }

        // Button face
        int pressOffset = _isPressed ? 2 : 0;
        Rectangle faceRect = new Rectangle(
            x + 2,
            y + bezelInset + 2 + pressOffset,
            size - 4,
            size - 4);

        // Button gradient - dark gray like square button
        Color faceTop = _isPressed ? Color.FromArgb(40, 40, 40) :
                        (_isHovered ? Color.FromArgb(70, 70, 70) : Color.FromArgb(55, 55, 55));
        Color faceBottom = _isPressed ? Color.FromArgb(25, 25, 25) :
                           (_isHovered ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35));

        using (var faceBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            faceRect,
            faceTop,
            faceBottom,
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillEllipse(faceBrush, faceRect);
        }

        // Draw glow effect if color is set (like square button)
        if (GlowColor != Color.Empty)
        {
            DrawRoundGlowEffect(g, faceRect);
        }

        // Button highlight (top edge shine)
        if (!_isPressed)
        {
            using (var highlightPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
            {
                g.DrawArc(highlightPen, faceRect.X + 2, faceRect.Y + 1, faceRect.Width - 4, faceRect.Height / 2, 180, 180);
            }
        }

        // Button border
        using (var borderPen = new Pen(Color.FromArgb(20, 20, 20), 1))
        {
            g.DrawEllipse(borderPen, faceRect);
        }
    }

    private void DrawRoundGlowEffect(Graphics g, Rectangle faceRect)
    {
        // Create glow inside the circular button face
        int glowInset = 4;
        Rectangle glowRect = new Rectangle(
            faceRect.X + glowInset,
            faceRect.Y + glowInset,
            faceRect.Width - glowInset * 2,
            faceRect.Height - glowInset * 2);

        if (glowRect.Width <= 0 || glowRect.Height <= 0) return;

        using (var glowPath = new System.Drawing.Drawing2D.GraphicsPath())
        {
            glowPath.AddEllipse(glowRect);

            using (var glowBrush = new System.Drawing.Drawing2D.PathGradientBrush(glowPath))
            {
                int alpha = _isPressed ? 60 : (_isHovered ? 100 : 80);

                glowBrush.CenterColor = Color.FromArgb(alpha, GlowColor);
                glowBrush.SurroundColors = new[] { Color.FromArgb(10, GlowColor) };

                // Offset center slightly for natural look
                glowBrush.CenterPoint = new PointF(
                    glowRect.X + glowRect.Width * 0.45f,
                    glowRect.Y + glowRect.Height * 0.55f);

                g.FillEllipse(glowBrush, glowRect);
            }
        }

        // Secondary highlight spot
        int spotSize = Math.Min(glowRect.Width, glowRect.Height) / 3;
        Rectangle spotRect = new Rectangle(
            glowRect.X + glowRect.Width / 4,
            glowRect.Y + glowRect.Height / 3,
            spotSize,
            spotSize);

        if (spotRect.Width > 2)
        {
            using (var spotPath = new System.Drawing.Drawing2D.GraphicsPath())
            {
                spotPath.AddEllipse(spotRect);
                using (var spotBrush = new System.Drawing.Drawing2D.PathGradientBrush(spotPath))
                {
                    int spotAlpha = _isPressed ? 30 : (_isHovered ? 50 : 40);
                    spotBrush.CenterColor = Color.FromArgb(spotAlpha, GlowColor);
                    spotBrush.SurroundColors = new[] { Color.FromArgb(0, GlowColor) };
                    g.FillEllipse(spotBrush, spotRect);
                }
            }
        }
    }

    private void DrawDivot(Graphics g, int x, int y)
    {
        int size = 2;
        // Dark hole
        using (var holeBrush = new SolidBrush(Color.FromArgb(25, 30, 25)))
        {
            g.FillEllipse(holeBrush, x, y, size, size);
        }
        // Subtle highlight on bottom edge
        using (var highlightBrush = new SolidBrush(Color.FromArgb(75, 80, 75)))
        {
            g.FillRectangle(highlightBrush, x, y + size, size, 1);
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
            ButtonClick?.Invoke(this, EventArgs.Empty);
        }
        base.OnMouseUp(e);
    }
}
