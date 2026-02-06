namespace PerceptronSimulator.Controls;

/// <summary>
/// 1950s-style toggle switch with metal plate, vertical text, mounting divots, and rocker switch.
/// Scalable - adjusts font and proportions based on control size.
/// </summary>
public class TogglePlateControl : Control
{
    private bool _isToggled = false;
    private string _labelText = "TOGGLE";
    private Color _activeColor = Color.FromArgb(100, 255, 150);  // Green glow when active
    private Color _inactiveColor = Color.FromArgb(85, 90, 85);   // Subdued gray-green

    public TogglePlateControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(50, 75);  // Default size
        Cursor = Cursors.Hand;
        BackColor = Color.Transparent;
    }

    [System.ComponentModel.Category("Appearance")]
    public string LabelText
    {
        get => _labelText;
        set { _labelText = value; Invalidate(); }
    }

    [System.ComponentModel.Category("Appearance")]
    public bool IsToggled
    {
        get => _isToggled;
        set { _isToggled = value; Invalidate(); }
    }

    [System.ComponentModel.Category("Appearance")]
    public Color ActiveColor
    {
        get => _activeColor;
        set { _activeColor = value; Invalidate(); }
    }

    [System.ComponentModel.Category("Appearance")]
    public Color InactiveColor
    {
        get => _inactiveColor;
        set { _inactiveColor = value; Invalidate(); }
    }

    public event EventHandler? ToggleChanged;

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Toggle();
    }

    public void Toggle()
    {
        _isToggled = !_isToggled;
        Invalidate();
        ToggleChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int width = ClientSize.Width;
        int height = ClientSize.Height;

        // Calculate scaled dimensions
        int plateX = 2;
        int plateY = 2;
        int plateWidth = width - 4;
        int plateHeight = height - 4;
        int cornerRadius = Math.Max(2, width / 12);

        // Rounded rectangle for wall plate
        using var platePath = new System.Drawing.Drawing2D.GraphicsPath();
        platePath.AddArc(plateX, plateY, cornerRadius, cornerRadius, 180, 90);
        platePath.AddArc(plateX + plateWidth - cornerRadius, plateY, cornerRadius, cornerRadius, 270, 90);
        platePath.AddArc(plateX + plateWidth - cornerRadius, plateY + plateHeight - cornerRadius, cornerRadius, cornerRadius, 0, 90);
        platePath.AddArc(plateX, plateY + plateHeight - cornerRadius, cornerRadius, cornerRadius, 90, 90);
        platePath.CloseFigure();

        // Dark metal gradient
        using var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(plateX, plateY, plateWidth, plateHeight),
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(50, 55, 50),
            45f);
        g.FillPath(plateBrush, platePath);

        // Plate border
        using var plateBorderPen = new Pen(Color.FromArgb(90, 95, 90), 1);
        g.DrawPath(plateBorderPen, platePath);

        // Mounting holes (divots) - top and bottom
        int holeRadius = Math.Max(2, width / 16);
        int topHoleY = plateY + holeRadius + 4;
        int bottomHoleY = plateY + plateHeight - holeRadius - 4;
        int holeX = plateX + plateWidth / 2;

        using var holeBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(holeX - holeRadius, topHoleY - holeRadius, holeRadius * 2, holeRadius * 2),
            Color.FromArgb(25, 25, 25),
            Color.FromArgb(45, 45, 45),
            45f);
        g.FillEllipse(holeBrush, holeX - holeRadius, topHoleY - holeRadius, holeRadius * 2, holeRadius * 2);
        g.FillEllipse(holeBrush, holeX - holeRadius, bottomHoleY - holeRadius, holeRadius * 2, holeRadius * 2);

        // Hole shadows
        using var holeShadowPen = new Pen(Color.FromArgb(15, 15, 15), 1);
        g.DrawEllipse(holeShadowPen, holeX - holeRadius, topHoleY - holeRadius, holeRadius * 2, holeRadius * 2);
        g.DrawEllipse(holeShadowPen, holeX - holeRadius, bottomHoleY - holeRadius, holeRadius * 2, holeRadius * 2);

        // Switch opening (recessed slot)
        int slotWidth = Math.Max(14, width / 3);
        int slotHeight = Math.Max(35, height / 2);
        int slotX = plateX + (plateWidth - slotWidth) / 2;
        int slotY = plateY + (plateHeight - slotHeight) / 2;

        using var recessBrush = new SolidBrush(Color.FromArgb(25, 25, 25));
        g.FillRectangle(recessBrush, slotX, slotY, slotWidth, slotHeight);

        // Inner shadow gradient
        using var shadowBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(slotX, slotY, slotWidth / 2, slotHeight),
            Color.FromArgb(100, 0, 0, 0),
            Color.FromArgb(0, 0, 0, 0),
            0f);
        g.FillRectangle(shadowBrush, slotX, slotY, slotWidth / 2, slotHeight);

        // Toggle paddle (rocker switch)
        int paddleWidth = Math.Max(10, slotWidth - 4);
        int paddleHeight = Math.Max(20, slotHeight / 2);
        int paddleX = slotX + (slotWidth - paddleWidth) / 2;

        // Paddle position based on toggle state
        int paddleY = _isToggled
            ? slotY + 2  // Up position
            : slotY + slotHeight - paddleHeight - 2;  // Down position

        // Paddle gradient (subtle 3D effect)
        using var paddleBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(paddleX, paddleY, paddleWidth, paddleHeight),
            Color.FromArgb(90, 90, 85),
            Color.FromArgb(60, 60, 55),
            90f);
        g.FillRectangle(paddleBrush, paddleX, paddleY, paddleWidth, paddleHeight);

        // Paddle border
        using var paddleBorderPen = new Pen(Color.FromArgb(110, 110, 105), 1);
        g.DrawRectangle(paddleBorderPen, paddleX, paddleY, paddleWidth, paddleHeight);

        // Vertical label text - engraved metal appearance (no glow)
        float fontSize = Math.Max(4f, width / 11f);  // Scale font with control size
        using var labelFont = new Font("Arial", fontSize, FontStyle.Bold);
        using var textShadowBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        // Always use subdued metal color - labels are engraved, not lit up
        using var labelBrush = new SolidBrush(Color.FromArgb(130, 135, 130));

        // Measure actual letter height
        var letterSize = g.MeasureString("M", labelFont);
        int letterHeight = (int)Math.Ceiling(letterSize.Height);

        // Calculate available space for text
        int textStartY = topHoleY + holeRadius + 6;  // Start below top mounting hole
        int textEndY = bottomHoleY - holeRadius - 4;  // End above bottom mounting hole
        int availableHeight = textEndY - textStartY;

        // Calculate spacing to fit all letters within available space
        int totalTextHeight = letterHeight * _labelText.Length;
        int letterSpacing = totalTextHeight > availableHeight
            ? availableHeight / _labelText.Length  // Compress if too tall
            : letterHeight;  // Use natural letter height if there's room

        int textX = plateX + 4;

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        for (int i = 0; i < _labelText.Length; i++)
        {
            string letter = _labelText[i].ToString();
            int letterY = textStartY + i * letterSpacing;

            // Shadow (embossed effect)
            g.DrawString(letter, labelFont, textShadowBrush, textX + 1, letterY + 1);
            // Main letter - always subdued metal color
            g.DrawString(letter, labelFont, labelBrush, textX, letterY);
        }
    }
}
