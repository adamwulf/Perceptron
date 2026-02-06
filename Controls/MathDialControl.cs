namespace PerceptronSimulator.Controls;

/// <summary>
/// Math dial control with discrete positions at clock positions.
/// Each position corresponds to a different neural network learning rule.
/// </summary>
public class MathDialControl : Control
{
    public enum MathRule
    {
        /// <summary>1 o'clock: Original Rosenblatt perceptron (1-to-1 connectivity)</summary>
        PERCEPTRON_CLASSIC = 0,     // "1958"

        /// <summary>2 o'clock: Fully connected, sum of (input × weight + bias)</summary>
        RULE_1958_SUM = 1,          // "1958+"

        /// <summary>3 o'clock: Fully connected, average of (input × weight + bias)</summary>
        RULE_1958_AVG = 2,          // "1958m"

        /// <summary>4 o'clock: Fully connected, sum of ((input/N) × weight + bias)</summary>
        RULE_1958_DIV_SUM = 3,      // "1958/+"

        /// <summary>5 o'clock: Fully connected, average of ((input/N) × weight + bias)</summary>
        RULE_1958_DIV_AVG = 4,      // "1958/m"

        /// <summary>9 o'clock: Widrow-Hoff / LMS / Delta rule (1960)</summary>
        WIDROW_HOFF = 5,            // "1960"

        /// <summary>10 o'clock: Backpropagation MLP (1986)</summary>
        BACKPROP = 6                // "1986"
    }

    // Position definitions: clock hour -> rule mapping
    private static readonly (int clockHour, MathRule rule, string label)[] Positions = new[]
    {
        (1, MathRule.PERCEPTRON_CLASSIC, "1958"),
        (2, MathRule.RULE_1958_SUM, "1958+"),
        (3, MathRule.RULE_1958_AVG, "1958m"),
        (4, MathRule.RULE_1958_DIV_SUM, "1958/+"),
        (5, MathRule.RULE_1958_DIV_AVG, "1958/m"),
        (9, MathRule.WIDROW_HOFF, "1960"),
        (10, MathRule.BACKPROP, "1986")
    };

    private MathRule _selectedRule = MathRule.PERCEPTRON_CLASSIC;
    private bool _isDragging;
    private Point _lastMousePos;
    private bool _isHovered;
    private float _dragAccumulator;

    public event EventHandler? RuleChanged;

    /// <summary>
    /// Extra padding around the dial for longer hash mark labels.
    /// Default is 0. Set higher (e.g., 20-30) when labels need more room.
    /// This reduces the knob size to make room for labels without clipping.
    /// </summary>
    public int LabelPadding { get; set; } = 0;

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

    public MathDialControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);

        Size = new Size(90, 95);
        Cursor = Cursors.Hand;
        Font = new Font("Consolas", 7f);
        TabStop = false;
    }

    /// <summary>Convert clock hour (1-12) to angle in degrees (standard math: 0° = right)</summary>
    private static float ClockHourToAngle(int hour)
    {
        // 12 o'clock = 90°, 3 o'clock = 0°, 6 o'clock = -90°, 9 o'clock = 180°
        return 90f - (hour * 30f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        // LabelPadding adds extra space for longer hash mark labels
        int horizontalPad = 48 + LabelPadding * 2;  // Labels on both sides
        int verticalPad = 25 + LabelPadding;
        int knobSize = Math.Min(Width - horizontalPad, Height - verticalPad);
        int knobX = (Width - knobSize) / 2;
        int knobY = 18 + LabelPadding / 2;  // Extra top margin for 1 o'clock label
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

        // Knob border - highlight when focused or hovered
        Color borderColor = Focused ? Color.FromArgb(100, 140, 180) :
                           (_isHovered ? Color.FromArgb(120, 120, 120) : Color.FromArgb(80, 80, 80));
        using (var borderPen = new Pen(borderColor, Focused ? 3 : 2))
        {
            g.DrawEllipse(borderPen, knobX, knobY, knobSize, knobSize);
        }

        // Indicator line pointing to selected position
        int clockHour = GetClockHourForRule(_selectedRule);
        float angle = ClockHourToAngle(clockHour);
        double radians = angle * Math.PI / 180;

        int lineLength = knobSize / 2 - 4;
        int lineEndX = centerX + (int)(Math.Cos(radians) * lineLength);
        int lineEndY = centerY - (int)(Math.Sin(radians) * lineLength); // Negative because Y is inverted

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

        // No label below knob - the hash marks show the value
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

            // Tick mark
            int innerRadius = radius + 2;
            int outerRadius = radius + 7;

            int x1 = centerX + (int)(Math.Cos(radians) * innerRadius);
            int y1 = centerY - (int)(Math.Sin(radians) * innerRadius);
            int x2 = centerX + (int)(Math.Cos(radians) * outerRadius);
            int y2 = centerY - (int)(Math.Sin(radians) * outerRadius);

            g.DrawLine(isActive ? activeTickPen : tickPen, x1, y1, x2, y2);

            // Label outside the tick (radius + 10 keeps labels within control bounds)
            int labelRadius = radius + 10;
            int labelX = centerX + (int)(Math.Cos(radians) * labelRadius);
            int labelY = centerY - (int)(Math.Sin(radians) * labelRadius);

            var labelSize = g.MeasureString(label, labelFont);
            labelX -= (int)(labelSize.Width / 2);
            labelY -= (int)(labelSize.Height / 2);

            g.DrawString(label, labelFont, isActive ? activeLabelBrush : labelBrush, labelX, labelY);
        }

        // Draw small dots for empty positions (6, 7, 8 o'clock)
        using var dotBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        foreach (int hour in new[] { 6, 7, 8 })
        {
            float angle = ClockHourToAngle(hour);
            double radians = angle * Math.PI / 180;
            int dotRadius = radius + 4;
            int dotX = centerX + (int)(Math.Cos(radians) * dotRadius);
            int dotY = centerY - (int)(Math.Sin(radians) * dotRadius);
            g.FillEllipse(dotBrush, dotX - 2, dotY - 2, 4, 4);
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

    private static string GetLabelForRule(MathRule rule)
    {
        foreach (var (_, r, label) in Positions)
        {
            if (r == rule) return label;
        }
        return "1958";
    }

    private MathRule? GetRuleAtClockHour(int hour)
    {
        foreach (var (clockHour, rule, _) in Positions)
        {
            if (clockHour == hour) return rule;
        }
        return null;
    }

    private void MoveToNextPosition(int direction)
    {
        // Get ordered list of valid positions
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

            // Move to next/prev position when accumulated enough drag
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
