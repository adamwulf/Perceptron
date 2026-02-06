namespace PerceptronSimulator.Controls;

public class SettingsKnobControl : Control
{
    private double _value;
    private bool _isDragging;
    private Point _lastMousePos;
    private bool _isHovered;

    // Auto-repeat click functionality
    private System.Windows.Forms.Timer? _repeatTimer;
    private int _repeatDirection; // +1 for increase, -1 for decrease

    public event EventHandler? ValueChanged;
    public event EventHandler? BelowMinimumAttempted;

    public double MinValue { get; set; } = 0;
    public double MaxValue { get; set; } = 10;
    public double Step { get; set; } = 1;
    public string ValueFormat { get; set; } = "0";
    public double[] TickValues { get; set; } = Array.Empty<double>();
    public double[] MajorTickValues { get; set; } = Array.Empty<double>();
    public bool MinValuePointsUp { get; set; } = false; // When true, minimum value points straight up

    /// <summary>
    /// When true, clicking increments/decrements the knob with auto-repeat.
    /// Left-click = increase (Shift+Left = decrease)
    /// Right-click = decrease (Shift+Right = increase)
    /// When false, only drag behavior is active.
    /// </summary>
    public bool KnobClick { get; set; } = false;

    public double Value
    {
        get => _value;
        set
        {
            double clamped = Math.Clamp(value, MinValue, MaxValue);
            // Snap to step
            clamped = Math.Round(clamped / Step) * Step;
            clamped = Math.Clamp(clamped, MinValue, MaxValue);

            if (Math.Abs(_value - clamped) > 0.0001)
            {
                _value = clamped;
                Invalidate();
                Update(); // Force immediate repaint so knob visually turns right away
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public SettingsKnobControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        Size = new Size(50, 65);
        Cursor = Cursors.Hand;
        Font = new Font("Consolas", 7f);
        TabStop = false;

        // Initialize auto-repeat timer (250ms interval)
        _repeatTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _repeatTimer.Tick += RepeatTimer_Tick;
    }

    private void RepeatTimer_Tick(object? sender, EventArgs e)
    {
        // Check if at minimum and trying to decrease
        if (_repeatDirection < 0 && Math.Abs(_value - MinValue) < 0.0001)
        {
            BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Value += _repeatDirection * Step;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int knobY = 2;
        int knobSize = Math.Min(Width - 8, Height - 22);
        int knobX = (Width - knobSize) / 2;

        // Draw tick marks around the knob
        DrawTickMarks(g, knobX + knobSize / 2, knobY + knobSize / 2, knobSize / 2 + 2);

        // Knob shadow
        using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 20, 20)))
        {
            g.FillEllipse(shadowBrush, knobX + 2, knobY + 2, knobSize, knobSize);
        }

        // Knob body gradient
        using (var knobPath = new System.Drawing.Drawing2D.GraphicsPath())
        {
            knobPath.AddEllipse(knobX, knobY, knobSize, knobSize);
            using var knobBrush = new System.Drawing.Drawing2D.PathGradientBrush(knobPath)
            {
                CenterColor = Color.FromArgb(90, 90, 90),
                SurroundColors = new[] { Color.FromArgb(40, 40, 40) },
                CenterPoint = new PointF(knobX + knobSize * 0.4f, knobY + knobSize * 0.3f)
            };
            g.FillEllipse(knobBrush, knobX, knobY, knobSize, knobSize);
        }

        // Knob border - highlight when focused or hovered
        Color borderColor = Focused ? Color.FromArgb(100, 140, 180) :
                           (_isHovered ? Color.FromArgb(120, 120, 120) : Color.FromArgb(80, 80, 80));
        using (var borderPen = new Pen(borderColor, Focused ? 3 : 2))
        {
            g.DrawEllipse(borderPen, knobX, knobY, knobSize, knobSize);
        }

        // Indicator line
        double normalizedValue = (MaxValue > MinValue) ? (_value - MinValue) / (MaxValue - MinValue) : 0;
        double angle;
        if (MinValuePointsUp)
        {
            // Min value points up (-90), sweeps clockwise 270 degrees to max
            angle = -90 + normalizedValue * 270;
        }
        else
        {
            // Standard: min at -225, max at +45
            angle = -225 + normalizedValue * 270;
        }
        double radians = angle * Math.PI / 180;

        int centerX = knobX + knobSize / 2;
        int centerY = knobY + knobSize / 2;
        int lineLength = knobSize / 2 - 4;

        int lineEndX = centerX + (int)(Math.Cos(radians) * lineLength);
        int lineEndY = centerY + (int)(Math.Sin(radians) * lineLength);

        using (var indicatorPen = new Pen(Color.FromArgb(220, 220, 220), 2))
        {
            indicatorPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            indicatorPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            g.DrawLine(indicatorPen, centerX, centerY, lineEndX, lineEndY);
        }

        // Center dot
        int dotSize = 6;
        using (var dotBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
        {
            g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
        }

        // Value text
        string valueText = _value.ToString(ValueFormat);
        using (var textBrush = new SolidBrush(Color.FromArgb(180, 180, 180)))
        {
            var textSize = g.MeasureString(valueText, Font);
            g.DrawString(valueText, Font, textBrush, (Width - textSize.Width) / 2, knobY + knobSize + 3);
        }
    }

    private void DrawTickMarks(Graphics g, int centerX, int centerY, int radius)
    {
        using var tickPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var majorTickPen = new Pen(Color.FromArgb(110, 110, 110), 1);

        // Draw all ticks
        foreach (var tickValue in TickValues)
        {
            if (tickValue < MinValue || tickValue > MaxValue) continue;

            bool isMajor = MajorTickValues.Contains(tickValue);
            double normalizedValue = (MaxValue > MinValue) ? (tickValue - MinValue) / (MaxValue - MinValue) : 0;
            double angle;
            if (MinValuePointsUp)
            {
                angle = -90 + normalizedValue * 270;
            }
            else
            {
                angle = -225 + normalizedValue * 270;
            }
            double radians = angle * Math.PI / 180;

            int innerRadius = radius + 2;
            int outerRadius = radius + (isMajor ? 7 : 4);

            int x1 = centerX + (int)(Math.Cos(radians) * innerRadius);
            int y1 = centerY + (int)(Math.Sin(radians) * innerRadius);
            int x2 = centerX + (int)(Math.Cos(radians) * outerRadius);
            int y2 = centerY + (int)(Math.Sin(radians) * outerRadius);

            g.DrawLine(isMajor ? majorTickPen : tickPen, x1, y1, x2, y2);
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

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus(); // Make this knob active for keyboard input

        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastMousePos = e.Location;
            Capture = true;

            // KnobClick feature: click-and-hold auto-repeat
            if (KnobClick)
            {
                bool shiftHeld = (ModifierKeys & Keys.Shift) != 0;
                _repeatDirection = shiftHeld ? -1 : 1;

                // Check if at minimum and trying to decrease
                if (_repeatDirection < 0 && Math.Abs(_value - MinValue) < 0.0001)
                {
                    BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Value += _repeatDirection * Step;
                }
                _repeatTimer?.Start();
            }
        }
        else if (e.Button == MouseButtons.Right && KnobClick)
        {
            // KnobClick feature: right-click decreases (or increases with Shift)
            bool shiftHeld = (ModifierKeys & Keys.Shift) != 0;
            _repeatDirection = shiftHeld ? 1 : -1;

            // Check if at minimum and trying to decrease
            if (_repeatDirection < 0 && Math.Abs(_value - MinValue) < 0.0001)
            {
                BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Value += _repeatDirection * Step;
            }
            _repeatTimer?.Start();
            Capture = true;
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_isDragging)
        {
            int deltaY = _lastMousePos.Y - e.Y;
            int deltaX = e.X - _lastMousePos.X;
            double delta = (deltaY + deltaX) * Step * 0.5;

            // Check if at minimum and trying to decrease
            if (delta < -0.0001 && Math.Abs(_value - MinValue) < 0.0001)
            {
                BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Value += delta;
            }
            _lastMousePos = e.Location;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _repeatTimer?.Stop();
        _isDragging = false;
        Capture = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e.Delta < 0 && Math.Abs(_value - MinValue) < 0.0001)
        {
            BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Value += e.Delta > 0 ? Step : -Step;
        }
        base.OnMouseWheel(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        // Tell WinForms that arrow keys are input keys, not navigation keys
        switch (keyData)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
                return true;
        }
        return base.IsInputKey(keyData);
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        // Mark arrow keys as input keys so they don't trigger navigation
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Down:
            case Keys.Left:
            case Keys.Right:
                e.IsInputKey = true;
                break;
        }
        base.OnPreviewKeyDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Right:
                Value += Step;
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Down:
            case Keys.Left:
                // Check if at minimum before attempting decrease
                if (Math.Abs(_value - MinValue) < 0.0001)
                {
                    BelowMinimumAttempted?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Value -= Step;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
        }
        base.OnKeyDown(e);
    }

    protected override void OnGotFocus(EventArgs e)
    {
        Invalidate();
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(EventArgs e)
    {
        Invalidate();
        base.OnLostFocus(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _repeatTimer?.Stop();
            _repeatTimer?.Dispose();
            _repeatTimer = null;
        }
        base.Dispose(disposing);
    }
}
