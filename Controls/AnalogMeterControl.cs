namespace PerceptronSimulator.Controls;

public class AnalogMeterControl : Control
{
    private double _value;
    private double _displayValue;
    private System.Windows.Forms.Timer? _animationTimer;

    public double MinValue { get; set; } = -100;
    public double MaxValue { get; set; } = 100;

    public double Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, MinValue, MaxValue);
            StartAnimation();
        }
    }

    public AnalogMeterControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(200, 150);
        Font = new Font("Consolas", 9f);

        _animationTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    private void StartAnimation()
    {
        _animationTimer?.Start();
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        double diff = _value - _displayValue;
        if (Math.Abs(diff) < 0.5)
        {
            _displayValue = _value;
            _animationTimer?.Stop();
        }
        else
        {
            _displayValue += diff * 0.15;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Meter background (vintage look)
        Rectangle meterRect = new Rectangle(5, 5, Width - 10, Height - 10);

        using (var bgBrush = new SolidBrush(Color.FromArgb(25, 25, 25)))
        {
            g.FillRoundedRectangle(bgBrush, meterRect.X, meterRect.Y, meterRect.Width, meterRect.Height, 8);
        }

        // Inner face (slightly lighter)
        Rectangle faceRect = new Rectangle(15, 15, Width - 30, Height - 50);
        using (var faceBrush = new SolidBrush(Color.FromArgb(35, 35, 35)))
        {
            g.FillRoundedRectangle(faceBrush, faceRect.X, faceRect.Y, faceRect.Width, faceRect.Height, 6);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(70, 70, 70), 2))
        {
            g.DrawRoundedRectangle(borderPen, meterRect.X, meterRect.Y, meterRect.Width, meterRect.Height, 8);
        }

        int centerX = Width / 2;
        int centerY = Height - 35;
        int radius = Math.Min(Width, Height) - 70;

        // Draw scale arc and ticks
        DrawScale(g, centerX, centerY, radius);

        // Draw needle
        DrawNeedle(g, centerX, centerY, radius);

        // Center pivot
        using (var pivotBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
        {
            g.FillEllipse(pivotBrush, centerX - 6, centerY - 6, 12, 12);
        }
        using (var pivotHighlight = new SolidBrush(Color.FromArgb(90, 90, 90)))
        {
            g.FillEllipse(pivotHighlight, centerX - 3, centerY - 3, 6, 6);
        }

        // No label - removed MICROAMPERES text
    }

    private void DrawScale(Graphics g, int centerX, int centerY, int radius)
    {
        using var tickPen = new Pen(Color.FromArgb(100, 100, 100), 1);
        using var majorTickPen = new Pen(Color.FromArgb(140, 140, 140), 2);
        using var textBrush = new SolidBrush(Color.FromArgb(160, 160, 160));
        using var scaleFont = new Font("Consolas", 8f);

        // Draw arc
        using (var arcPen = new Pen(Color.FromArgb(80, 80, 80), 1))
        {
            g.DrawArc(arcPen, centerX - radius, centerY - radius, radius * 2, radius * 2, -150, 120);
        }

        // Draw ticks and labels
        for (int i = -100; i <= 100; i += 10)
        {
            double normalizedValue = (i - MinValue) / (MaxValue - MinValue);
            double angle = -150 + normalizedValue * 120;
            double radians = angle * Math.PI / 180;

            bool isMajor = i % 50 == 0;
            int innerRadius = radius - (isMajor ? 15 : 10);
            int outerRadius = radius;

            int x1 = centerX + (int)(Math.Cos(radians) * innerRadius);
            int y1 = centerY + (int)(Math.Sin(radians) * innerRadius);
            int x2 = centerX + (int)(Math.Cos(radians) * outerRadius);
            int y2 = centerY + (int)(Math.Sin(radians) * outerRadius);

            g.DrawLine(isMajor ? majorTickPen : tickPen, x1, y1, x2, y2);

            // Draw numbers for major ticks
            if (isMajor)
            {
                string text = i.ToString();
                var textSize = g.MeasureString(text, scaleFont);
                int textRadius = innerRadius - 12;
                int textX = centerX + (int)(Math.Cos(radians) * textRadius) - (int)(textSize.Width / 2);
                int textY = centerY + (int)(Math.Sin(radians) * textRadius) - (int)(textSize.Height / 2);
                g.DrawString(text, scaleFont, textBrush, textX, textY);
            }
        }
    }

    private void DrawNeedle(Graphics g, int centerX, int centerY, int radius)
    {
        double normalizedValue = (_displayValue - MinValue) / (MaxValue - MinValue);
        double angle = -150 + normalizedValue * 120;
        double radians = angle * Math.PI / 180;

        int needleLength = radius - 5;
        int needleEndX = centerX + (int)(Math.Cos(radians) * needleLength);
        int needleEndY = centerY + (int)(Math.Sin(radians) * needleLength);

        // Needle shadow
        using (var shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), 4))
        {
            g.DrawLine(shadowPen, centerX + 1, centerY + 1, needleEndX + 1, needleEndY + 1);
        }

        // Needle body (red)
        using (var needlePen = new Pen(Color.FromArgb(200, 60, 60), 2))
        {
            needlePen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            needlePen.EndCap = System.Drawing.Drawing2D.LineCap.Triangle;
            g.DrawLine(needlePen, centerX, centerY, needleEndX, needleEndY);
        }

        // Counter-weight (small tail)
        int tailLength = 15;
        int tailX = centerX - (int)(Math.Cos(radians) * tailLength);
        int tailY = centerY - (int)(Math.Sin(radians) * tailLength);
        using (var tailPen = new Pen(Color.FromArgb(200, 60, 60), 4))
        {
            tailPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            tailPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            g.DrawLine(tailPen, centerX, centerY, tailX, tailY);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
