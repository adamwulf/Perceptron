namespace PerceptronSimulator.Controls;

public enum ArrowDirection
{
    Up,
    Down,
    Left,
    Right
}

public class ArrowButton : Control
{
    private bool _isPressed;
    private bool _isHovered;

    public event EventHandler? ButtonClick;

    public ArrowDirection Direction { get; set; } = ArrowDirection.Up;

    /// <summary>
    /// Glow color for backlit plastic effect. Default is pale yellow like Brain button.
    /// </summary>
    public Color GlowColor { get; set; } = Color.FromArgb(255, 220, 80);

    public ArrowButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);

        BackColor = Color.Transparent;
        Size = new Size(26, 26);
        Cursor = Cursors.Hand;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int pressOffset = _isPressed ? 1 : 0;
        int margin = 2;

        // Get triangle points for different layers
        Point[] housingTriangle = GetTrianglePoints(Width, Height, 0, 0);
        Point[] bezelTriangle = GetTrianglePoints(Width - margin, Height - margin, margin / 2, margin / 2);
        Point[] faceTriangle = GetTrianglePoints(Width - margin * 3, Height - margin * 3, margin + margin / 2, margin + margin / 2 + pressOffset);

        // Button housing (recessed dark area)
        using (var housingBrush = new SolidBrush(Color.FromArgb(15, 15, 15)))
        {
            g.FillPolygon(housingBrush, housingTriangle);
        }

        // Button bezel (outer frame)
        using (var bezelBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, Width, Height),
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(35, 40, 35),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillPolygon(bezelBrush, bezelTriangle);
        }

        // Button face - dark gray like Brain button
        Color baseTop = _isPressed ? Color.FromArgb(40, 40, 40) :
                        (_isHovered ? Color.FromArgb(70, 70, 70) : Color.FromArgb(55, 55, 55));
        Color baseBottom = _isPressed ? Color.FromArgb(25, 25, 25) :
                           (_isHovered ? Color.FromArgb(50, 50, 50) : Color.FromArgb(35, 35, 35));

        using (var faceBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, Width, Height),
            baseTop,
            baseBottom,
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillPolygon(faceBrush, faceTriangle);
        }

        // Draw glow effect (backlit plastic look)
        if (GlowColor != Color.Empty)
        {
            DrawGlowEffect(g, faceTriangle);
        }

        // Top highlight when not pressed
        if (!_isPressed)
        {
            using (var highlightPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
            {
                // Draw highlight along top edge of triangle
                if (Direction == ArrowDirection.Up)
                {
                    g.DrawLine(highlightPen, faceTriangle[1].X + 4, faceTriangle[1].Y - 1,
                              faceTriangle[2].X - 4, faceTriangle[2].Y - 1);
                }
            }
        }

        // Button border
        using (var borderPen = new Pen(Color.FromArgb(20, 20, 20), 1))
        {
            g.DrawPolygon(borderPen, faceTriangle);
        }
    }

    private void DrawGlowEffect(Graphics g, Point[] faceTriangle)
    {
        // Create glow inside the triangle face
        var bounds = GetPolygonBounds(faceTriangle);
        if (bounds.Width <= 4 || bounds.Height <= 4) return;

        int inset = 3;
        Point[] glowTriangle = GetTrianglePoints(
            bounds.Width - inset * 2,
            bounds.Height - inset * 2,
            bounds.X + inset,
            bounds.Y + inset);

        using (var glowPath = new System.Drawing.Drawing2D.GraphicsPath())
        {
            glowPath.AddPolygon(glowTriangle);

            using (var glowBrush = new System.Drawing.Drawing2D.PathGradientBrush(glowPath))
            {
                int alpha = _isPressed ? 60 : (_isHovered ? 100 : 80);

                glowBrush.CenterColor = Color.FromArgb(alpha, GlowColor);
                glowBrush.SurroundColors = new[] { Color.FromArgb(10, GlowColor) };

                // Offset center for natural look
                var center = GetTriangleCenter(glowTriangle);
                glowBrush.CenterPoint = new PointF(center.X, center.Y + bounds.Height * 0.1f);

                g.FillPolygon(glowBrush, glowTriangle);
            }
        }
    }

    private PointF GetTriangleCenter(Point[] triangle)
    {
        float cx = (triangle[0].X + triangle[1].X + triangle[2].X) / 3f;
        float cy = (triangle[0].Y + triangle[1].Y + triangle[2].Y) / 3f;
        return new PointF(cx, cy);
    }

    private Rectangle GetPolygonBounds(Point[] points)
    {
        int minX = points.Min(p => p.X);
        int minY = points.Min(p => p.Y);
        int maxX = points.Max(p => p.X);
        int maxY = points.Max(p => p.Y);
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    private Point[] GetTrianglePoints(int w, int h, int offsetX, int offsetY)
    {
        // Create flatter, wider triangles (less pointy)
        // For up/down: wide base, short height
        // For left/right: tall base, short width
        switch (Direction)
        {
            case ArrowDirection.Up:
                return new Point[]
                {
                    new Point(w / 2 + offsetX, offsetY),
                    new Point(offsetX, h + offsetY),
                    new Point(w + offsetX, h + offsetY)
                };
            case ArrowDirection.Down:
                return new Point[]
                {
                    new Point(w / 2 + offsetX, h + offsetY),
                    new Point(offsetX, offsetY),
                    new Point(w + offsetX, offsetY)
                };
            case ArrowDirection.Left:
                return new Point[]
                {
                    new Point(offsetX, h / 2 + offsetY),
                    new Point(w + offsetX, offsetY),
                    new Point(w + offsetX, h + offsetY)
                };
            case ArrowDirection.Right:
                return new Point[]
                {
                    new Point(w + offsetX, h / 2 + offsetY),
                    new Point(offsetX, offsetY),
                    new Point(offsetX, h + offsetY)
                };
            default:
                return new Point[] { new Point(offsetX, offsetY) };
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
