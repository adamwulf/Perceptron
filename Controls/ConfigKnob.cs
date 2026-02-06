namespace PerceptronSimulator.Controls;

/// <summary>
/// Math dial control with generous margins to prevent label clipping.
/// Discrete positions at clock hours for different neural network rules.
/// </summary>
public class ConfigKnob : Control
{
    public enum MathRule
    {
        PERCEPTRON_CLASSIC = 0,     // 1 o'clock: "1958"
        RULE_1958_SUM = 1,          // 2 o'clock: "1958+"
        RULE_1958_AVG = 2,          // 3 o'clock: "1958m"
        RULE_1958_DIV_SUM = 3,      // 4 o'clock: "1958/+"
        RULE_1958_DIV_AVG = 4,      // 5 o'clock: "1958/m"
        WIDROW_HOFF = 5,            // 9 o'clock: "1960"
        BACKPROP = 6                // 10 o'clock: "1986"
    }

    private static readonly (int clockHour, MathRule rule, string label)[] Positions = new[]
    {
        (1, MathRule.PERCEPTRON_CLASSIC, "1958"),
        (2, MathRule.RULE_1958_SUM, "1958+"),
        (3, MathRule.RULE_1958_AVG, "1958m"),
        (4, MathRule.RULE_1958_DIV_SUM, "1958/+"),
        (8, MathRule.RULE_1958_DIV_AVG, "1958/m"),
        (9, MathRule.WIDROW_HOFF, "1960"),
        (10, MathRule.BACKPROP, "1986")
    };

    private MathRule _selectedRule = MathRule.PERCEPTRON_CLASSIC;
    private bool _isDragging;
    private Point _lastMousePos;
    private bool _isHovered;
    private float _dragAccumulator;

    public event EventHandler? RuleChanged;

    public MathRule SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (_selectedRule != value)
            {
                _selectedRule = value;
                Invalidate();
                RuleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public ConfigKnob()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        Size = new Size(120, 78);  // Match Grid Size height, extra width for labels
        Cursor = Cursors.Hand;
        Font = new Font("Consolas", 6.5f);
        TabStop = false;
    }

    private static float ClockHourToAngle(int hour)
    {
        return 90f - (hour * 30f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // Knob size matches Grid Size dial (56px)
        // Position knob lower in control to leave room at top for 1 o'clock label
        int knobSize = 56;
        int knobX = (Width - knobSize) / 2;
        int knobY = 18;  // Extra space at top for 1 o'clock label
        int centerX = knobX + knobSize / 2;
        int centerY = knobY + knobSize / 2;

        // Draw tick marks and labels around the knob
        DrawTickMarksAndLabels(g, centerX, centerY, knobSize / 2 + 3);

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

        // Knob border
        Color borderColor = Focused ? Color.FromArgb(100, 140, 180) :
                           (_isHovered ? Color.FromArgb(120, 120, 120) : Color.FromArgb(80, 80, 80));
        using (var borderPen = new Pen(borderColor, Focused ? 3 : 2))
        {
            g.DrawEllipse(borderPen, knobX, knobY, knobSize, knobSize);
        }

        // Indicator line from center to edge pointing to selected position
        int clockHour = GetClockHourForRule(_selectedRule);
        float angle = ClockHourToAngle(clockHour);
        double radians = angle * Math.PI / 180;

        int lineLength = knobSize / 2 - 2;
        int lineEndX = centerX + (int)(Math.Cos(radians) * lineLength);
        int lineEndY = centerY - (int)(Math.Sin(radians) * lineLength);

        using (var indicatorPen = new Pen(Color.FromArgb(220, 220, 220), 2))
        {
            indicatorPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            indicatorPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            g.DrawLine(indicatorPen, centerX, centerY, lineEndX, lineEndY);
        }

        // Center dot
        int dotSize = 8;
        using (var dotBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
        {
            g.FillEllipse(dotBrush, centerX - dotSize / 2, centerY - dotSize / 2, dotSize, dotSize);
        }
    }

    private void DrawTickMarksAndLabels(Graphics g, int centerX, int centerY, int radius)
    {
        using var tickPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var activeTickPen = new Pen(Color.FromArgb(140, 140, 100), 2);
        using var labelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        using var activeLabelBrush = new SolidBrush(Color.FromArgb(180, 180, 140));
        using var labelFont = new Font("Consolas", 6.5f);

        foreach (var (clockHour, rule, label) in Positions)
        {
            float angle = ClockHourToAngle(clockHour);
            double radians = angle * Math.PI / 180;

            bool isActive = rule == _selectedRule;

            // Tick mark - shorter at 3 and 9 o'clock to avoid label collision
            int innerRadius = radius + 2;
            int outerRadius = (clockHour == 3 || clockHour == 9) ? radius + 7 : radius + 12;

            int x1 = centerX + (int)(Math.Cos(radians) * innerRadius);
            int y1 = centerY - (int)(Math.Sin(radians) * innerRadius);
            int x2 = centerX + (int)(Math.Cos(radians) * outerRadius);
            int y2 = centerY - (int)(Math.Sin(radians) * outerRadius);

            g.DrawLine(isActive ? activeTickPen : tickPen, x1, y1, x2, y2);

            // Label further out from tick
            int labelRadius = radius + 18;
            int labelX = centerX + (int)(Math.Cos(radians) * labelRadius);
            int labelY = centerY - (int)(Math.Sin(radians) * labelRadius);

            var labelSize = g.MeasureString(label, labelFont);
            labelX -= (int)(labelSize.Width / 2);
            labelY -= (int)(labelSize.Height / 2);

            g.DrawString(label, labelFont, isActive ? activeLabelBrush : labelBrush, labelX, labelY);
        }

    }

    private static int GetClockHourForRule(MathRule rule)
    {
        foreach (var (clockHour, r, _) in Positions)
        {
            if (r == rule) return clockHour;
        }
        return 1;
    }

    private void MoveToNextPosition(int direction)
    {
        var orderedPositions = Positions.OrderBy(p => p.clockHour).ToList();
        int currentIndex = orderedPositions.FindIndex(p => p.rule == _selectedRule);

        if (currentIndex >= 0)
        {
            int newIndex = currentIndex + direction;
            if (newIndex >= 0 && newIndex < orderedPositions.Count)
            {
                SelectedRule = orderedPositions[newIndex].rule;
            }
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
        Focus();
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _lastMousePos = e.Location;
            _dragAccumulator = 0;
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
            _dragAccumulator += (deltaY + deltaX) * 0.1f;

            if (_dragAccumulator > 1)
            {
                MoveToNextPosition(1);
                _dragAccumulator = 0;
            }
            else if (_dragAccumulator < -1)
            {
                MoveToNextPosition(-1);
                _dragAccumulator = 0;
            }

            _lastMousePos = e.Location;
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _isDragging = false;
        Capture = false;
        base.OnMouseUp(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        MoveToNextPosition(e.Delta > 0 ? 1 : -1);
        base.OnMouseWheel(e);
    }

    protected override bool IsInputKey(Keys keyData)
    {
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
                MoveToNextPosition(1);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
            case Keys.Down:
            case Keys.Left:
                MoveToNextPosition(-1);
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
}
