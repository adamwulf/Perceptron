using PerceptronSimulator.Controls;

namespace PerceptronSimulator;

public class DebugDialog : Form
{
    private const int TITLE_BAR_HEIGHT = 30;
    private const int MIN_NODE_SIZE = 25;
    private const int MAX_NODE_SIZE = 45;
    private const int RESIZE_GRIP_SIZE = 16;

    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private Panel _canvas = null!;

    private bool _isDragging;
    private Point _dragStart;

    // Resize tracking
    private bool _isResizing;
    private Point _resizeStart;
    private Size _sizeAtResizeStart;

    // Data from main form
    private int[] _switchStates = Array.Empty<int>();
    private double[] _weights = Array.Empty<double>();
    private double _bias;
    private double _output;
    private int _gridSize;
    private ConfigKnob.MathRule _mathRule = ConfigKnob.MathRule.PERCEPTRON_CLASSIC;
    private double[]? _hiddenOutputs;

    // Node positions for hit testing
    private Rectangle[] _inputNodeRects = Array.Empty<Rectangle>();
    private Rectangle[] _hiddenNodeRects = Array.Empty<Rectangle>();
    private int _selectedHiddenNode = -1; // -1 = none selected

    // Events for interaction with main form
    public event EventHandler<int>? InputNodeClicked;  // Index of clicked input
    public event EventHandler<(int index, double delta)>? WeightChangeRequested;  // Index and delta

    public DebugDialog()
    {
        InitializeForm();
        InitializeCustomChrome();
        InitializeCanvas();
    }

    private void InitializeForm()
    {
        Text = "Perceptron Debug View";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(10, 10, 10);
        StartPosition = FormStartPosition.Manual;
        Size = new Size(500, 400);
        MinimumSize = new Size(300, 250);
        Font = new Font("Consolas", 9f);
        DoubleBuffered = true;
        ShowInTaskbar = false;
    }

    private void InitializeCustomChrome()
    {
        _titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = TITLE_BAR_HEIGHT,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += TitleBar_MouseUp;

        _titleLabel = new Label
        {
            Text = "THE BRAIN",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += TitleBar_MouseUp;

        _closeButton = new Button
        {
            Text = "X",
            Size = new Size(40, TITLE_BAR_HEIGHT),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 60, 60),
            Font = new Font("Consolas", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 40);
        _closeButton.Click += (s, e) => Hide();

        _titleBar.Controls.Add(_closeButton);
        _titleBar.Controls.Add(_titleLabel);

        _titleBar.Resize += (s, e) =>
        {
            _titleLabel.Location = new Point((_titleBar.Width - _titleLabel.Width) / 2,
                (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2);
        };

        Controls.Add(_titleBar);
    }

    private void InitializeCanvas()
    {
        _canvas = new DoubleBufferedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black
        };
        _canvas.Paint += Canvas_Paint;
        _canvas.MouseDown += Canvas_MouseDown;
        _canvas.MouseMove += Canvas_MouseMove;
        _canvas.MouseUp += Canvas_MouseUp;
        _canvas.KeyDown += Canvas_KeyDown;
        _canvas.PreviewKeyDown += Canvas_PreviewKeyDown;
        Controls.Add(_canvas);
        _canvas.BringToFront();
    }

    private void Canvas_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
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
    }

    private void Canvas_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_selectedHiddenNode < 0 || _selectedHiddenNode >= _weights.Length)
            return;

        double delta = 0;
        switch (e.KeyCode)
        {
            case Keys.Up:
            case Keys.Right:
                delta = 0.05; // Same step as knobs
                break;
            case Keys.Down:
            case Keys.Left:
                delta = -0.05;
                break;
        }

        if (delta != 0)
        {
            WeightChangeRequested?.Invoke(this, (_selectedHiddenNode, delta));
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private bool IsInCanvasResizeGrip(Point canvasPoint)
    {
        // Check if point is in bottom-right corner of canvas (which maps to form's resize grip)
        return canvasPoint.X >= _canvas.Width - RESIZE_GRIP_SIZE &&
               canvasPoint.Y >= _canvas.Height - RESIZE_GRIP_SIZE;
    }

    private void Canvas_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Check resize grip first
            if (IsInCanvasResizeGrip(e.Location))
            {
                _isResizing = true;
                _resizeStart = _canvas.PointToScreen(e.Location);
                _sizeAtResizeStart = Size;
                _canvas.Capture = true;
                return;
            }

            // Check if clicked on an input node
            for (int i = 0; i < _inputNodeRects.Length; i++)
            {
                if (_inputNodeRects[i].Contains(e.Location))
                {
                    InputNodeClicked?.Invoke(this, i);
                    return;
                }
            }

            // Check if clicked on a hidden/weight node
            for (int i = 0; i < _hiddenNodeRects.Length; i++)
            {
                if (_hiddenNodeRects[i].Contains(e.Location))
                {
                    _selectedHiddenNode = i;
                    _canvas.Invalidate();
                    _canvas.Focus(); // Take focus for keyboard input
                    return;
                }
            }

            // Clicked elsewhere - deselect
            if (_selectedHiddenNode != -1)
            {
                _selectedHiddenNode = -1;
                _canvas.Invalidate();
            }
        }
    }

    private void Canvas_MouseMove(object? sender, MouseEventArgs e)
    {
        // Update cursor when over resize grip
        if (IsInCanvasResizeGrip(e.Location))
        {
            _canvas.Cursor = Cursors.SizeNWSE;
        }
        else if (!_isResizing)
        {
            _canvas.Cursor = Cursors.Default;
        }

        // Handle resize dragging
        if (_isResizing)
        {
            Point currentScreen = _canvas.PointToScreen(e.Location);
            int deltaX = currentScreen.X - _resizeStart.X;
            int deltaY = currentScreen.Y - _resizeStart.Y;

            int newWidth = Math.Max(MinimumSize.Width, _sizeAtResizeStart.Width + deltaX);
            int newHeight = Math.Max(MinimumSize.Height, _sizeAtResizeStart.Height + deltaY);

            Size = new Size(newWidth, newHeight);
        }
    }

    private void Canvas_MouseUp(object? sender, MouseEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            _canvas.Capture = false;
            _canvas.Invalidate();
        }
    }

    public void UpdateData(int[] switchStates, double[] weights, double bias, double output, int gridSize,
        ConfigKnob.MathRule mathRule, double[]? hiddenOutputs = null)
    {
        _switchStates = switchStates;
        _weights = weights;
        _bias = bias;
        _output = output;
        _gridSize = gridSize;
        _mathRule = mathRule;
        _hiddenOutputs = hiddenOutputs;
        _canvas?.Invalidate();
    }

    // Helper property: is the network fully connected (all inputs to all hidden)?
    // Must match PerceptronEngine.IsFullyConnected - both 1958 Classic and 1960 Widrow-Hoff use 1-to-1
    private bool IsFullyConnected => _mathRule != ConfigKnob.MathRule.PERCEPTRON_CLASSIC
                                  && _mathRule != ConfigKnob.MathRule.WIDROW_HOFF;

    // Helper to get display name for the math rule
    private string GetMathRuleDisplayName()
    {
        return _mathRule switch
        {
            ConfigKnob.MathRule.PERCEPTRON_CLASSIC => "1958 Classic (1-to-1)",
            ConfigKnob.MathRule.RULE_1958_SUM => "1958+ Sum (fully connected)",
            ConfigKnob.MathRule.RULE_1958_AVG => "1958m Avg (fully connected)",
            ConfigKnob.MathRule.RULE_1958_DIV_SUM => "1958/+ Div-Sum",
            ConfigKnob.MathRule.RULE_1958_DIV_AVG => "1958/m Div-Avg",
            ConfigKnob.MathRule.WIDROW_HOFF => "1960 Widrow-Hoff/LMS (1-to-1)",
            ConfigKnob.MathRule.BACKPROP => "1986 Backprop MLP",
            _ => "Unknown"
        };
    }

    private void Canvas_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (_switchStates.Length == 0 || _weights.Length == 0)
        {
            using var msgBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            g.DrawString("No data - interact with main window", Font, msgBrush, 20, 20);
            DrawResizeGrip(g, _canvas.ClientSize.Width, _canvas.ClientSize.Height);
            return;
        }

        int nodeCount = _switchStates.Length;
        int canvasWidth = _canvas.ClientSize.Width;
        int canvasHeight = _canvas.ClientSize.Height;

        // Initialize node rectangle arrays for hit testing
        _inputNodeRects = new Rectangle[nodeCount];
        _hiddenNodeRects = new Rectangle[nodeCount];

        // Calculate node size based on available space
        // We need room for: nodeCount input/weight pairs + 1 bias node, all vertically
        int totalNodesVertical = nodeCount + 1; // +1 for bias
        int verticalPadding = 50; // top and bottom padding
        int availableHeight = canvasHeight - verticalPadding;

        // Calculate spacing first, then derive node size from that
        int idealSpacing = availableHeight / Math.Max(1, totalNodesVertical);
        int nodeSize = Math.Clamp((int)(idealSpacing * 0.7), MIN_NODE_SIZE, MAX_NODE_SIZE);
        int nodeSpacing = Math.Max(nodeSize + 5, availableHeight / Math.Max(1, totalNodesVertical));

        // Recalculate if nodes would overflow
        int totalRequiredHeight = totalNodesVertical * nodeSpacing;
        if (totalRequiredHeight > availableHeight)
        {
            nodeSpacing = availableHeight / Math.Max(1, totalNodesVertical);
            nodeSize = Math.Clamp((int)(nodeSpacing * 0.7), MIN_NODE_SIZE, MAX_NODE_SIZE);
        }

        // Column positions - distribute evenly across width
        int horizontalPadding = 70;
        int inputX = horizontalPadding;
        int outputX = canvasWidth - horizontalPadding;
        int weightX = (inputX + outputX) / 2;

        // Vertical start position - center the nodes
        int totalHeight = nodeCount * nodeSpacing;
        int startY = (canvasHeight - totalHeight) / 2;

        // Colors
        var activeNodeColor = Color.FromArgb(220, 80, 80); // Red for active (1)
        var inactiveNodeColor = Color.FromArgb(200, 200, 200); // White for inactive
        var activeLineColor = Color.FromArgb(100, 180, 255); // Light blue for active lines
        var inactiveLineColor = Color.FromArgb(60, 60, 60); // Dark gray for inactive
        var positiveValueColor = Color.FromArgb(80, 220, 80); // Green for positive
        var negativeValueColor = Color.FromArgb(255, 220, 80); // Yellow for negative
        var biasColor = Color.FromArgb(180, 120, 220); // Purple for bias

        // Calculate output node Y position (center)
        int outputY = startY + (nodeCount * nodeSpacing) / 2;

        // Draw connections and nodes
        using var activeLinePen = new Pen(activeLineColor, 2);
        using var inactiveLinePen = new Pen(inactiveLineColor, 1);
        using var nodeBorderPen = new Pen(Color.FromArgb(150, 150, 150), 2);
        using var valueFont = new Font("Consolas", Math.Max(7, nodeSize / 4), FontStyle.Bold);
        using var smallFont = new Font("Consolas", 7f);

        // In multi-layer mode, draw all-to-all connections first (behind nodes)
        if (IsFullyConnected)
        {
            using var inactiveMultiLinePen = new Pen(Color.FromArgb(40, 100, 180, 255), 1);
            using var activeMultiLinePen = new Pen(Color.FromArgb(80, 220, 80, 80), 2); // Red for +1 inputs
            for (int i = 0; i < nodeCount; i++)
            {
                int inputY = startY + i * nodeSpacing + nodeSize / 2;
                bool isActiveInput = _switchStates[i] > 0; // +1
                var linePen = isActiveInput ? activeMultiLinePen : inactiveMultiLinePen;
                for (int j = 0; j < nodeCount; j++)
                {
                    int hiddenY = startY + j * nodeSpacing + nodeSize / 2;
                    g.DrawLine(linePen, inputX + nodeSize / 2, inputY, weightX - nodeSize / 2, hiddenY);
                }
            }
        }

        // Draw each input-weight pair
        for (int i = 0; i < nodeCount; i++)
        {
            int y = startY + i * nodeSpacing;
            bool isActive = _switchStates[i] > 0;
            double weight = _weights[i];

            // In multi-layer mode, use hidden outputs instead of simple input*weight
            double hiddenOutput = (IsFullyConnected && _hiddenOutputs != null && i < _hiddenOutputs.Length)
                ? _hiddenOutputs[i] : 0;
            double contribution = IsFullyConnected
                ? hiddenOutput * weight
                : _switchStates[i] * weight;
            bool hasContribution = Math.Abs(contribution) > 0.01;

            // Input value text (left of input node) - show actual values: +1 or -1
            string inputText = isActive ? "+1" : "-1";
            using var inputTextBrush = new SolidBrush(isActive ? positiveValueColor : negativeValueColor);
            var inputTextSize = g.MeasureString(inputText, valueFont);
            g.DrawString(inputText, valueFont, inputTextBrush, inputX - nodeSize / 2 - inputTextSize.Width - 5, y + nodeSize / 2 - inputTextSize.Height / 2);

            // Line from input text to input node - always active since input is always -1 or +1
            g.DrawLine(activeLinePen, inputX - nodeSize / 2 - 5, y + nodeSize / 2, inputX - nodeSize / 2, y + nodeSize / 2);

            // Input node - red for +1, yellow/gold for -1
            _inputNodeRects[i] = new Rectangle(inputX - nodeSize / 2, y, nodeSize, nodeSize);
            using var inputBrush = new SolidBrush(isActive ? activeNodeColor : Color.FromArgb(200, 180, 80));
            g.FillEllipse(inputBrush, _inputNodeRects[i]);
            g.DrawEllipse(nodeBorderPen, _inputNodeRects[i]);

            // Line from input node to weight node (only in single-layer mode)
            if (!IsFullyConnected)
            {
                // Use red for +1 inputs, otherwise blue/gray
                using var activeInputPen = new Pen(Color.FromArgb(220, 80, 80), 2); // Red for +1
                var inputToWeightPen = isActive ? activeInputPen : (hasContribution ? activeLinePen : inactiveLinePen);
                g.DrawLine(inputToWeightPen, inputX + nodeSize / 2, y + nodeSize / 2, weightX - nodeSize / 2, y + nodeSize / 2);
            }

            // Hidden/Weight node
            _hiddenNodeRects[i] = new Rectangle(weightX - nodeSize / 2, y, nodeSize, nodeSize);
            bool hiddenActive = IsFullyConnected ? (hiddenOutput > 0.01) : hasContribution;
            bool isSelected = (i == _selectedHiddenNode);
            using var weightBrush = new SolidBrush(hiddenActive ? Color.FromArgb(80, 80, 80) : Color.FromArgb(40, 40, 40));
            g.FillEllipse(weightBrush, _hiddenNodeRects[i]);
            // Highlight border if selected
            using var hiddenBorderPen = new Pen(isSelected ? Color.FromArgb(100, 200, 255) : Color.FromArgb(150, 150, 150), isSelected ? 3 : 2);
            g.DrawEllipse(hiddenBorderPen, _hiddenNodeRects[i]);

            // Node value inside (if large enough) - only for single-layer mode
            // In multi-layer mode, values are shown outside the node on the output side
            if (nodeSize >= 30 && !IsFullyConnected)
            {
                string nodeText = weight.ToString("0");
                using var nodeTextBrush = new SolidBrush(weight >= 0 ? positiveValueColor : negativeValueColor);
                var nodeTextSize = g.MeasureString(nodeText, smallFont);
                g.DrawString(nodeText, smallFont, nodeTextBrush,
                    weightX - nodeTextSize.Width / 2, y + nodeSize / 2 - nodeTextSize.Height / 2);
            }

            // Line from weight/hidden node to output node
            var outLinePen = hasContribution ? activeLinePen : inactiveLinePen;
            g.DrawLine(outLinePen, weightX + nodeSize / 2, y + nodeSize / 2, outputX - nodeSize / 2, outputY + nodeSize / 2);

            // Contribution value - position immediately to the right of the middle layer node
            if (hasContribution && !IsFullyConnected)
            {
                string contribText = contribution.ToString("+0.0;-0.0");
                using var contribBrush = new SolidBrush(contribution >= 0 ? positiveValueColor : negativeValueColor);
                var contribSize = g.MeasureString(contribText, smallFont);
                // Position immediately to the right of the weight node circle
                float contribX = weightX + nodeSize / 2 + 3;
                float contribY = y + nodeSize / 2 - contribSize.Height / 2;
                g.DrawString(contribText, smallFont, contribBrush, contribX, contribY);
            }

            // In multi-layer mode, show hidden output value immediately to the right of the hidden node
            if (IsFullyConnected && _hiddenOutputs != null && Math.Abs(hiddenOutput) > 0.01)
            {
                string hiddenText = hiddenOutput.ToString("0.0");
                using var hiddenBrush = new SolidBrush(hiddenOutput >= 0 ? positiveValueColor : negativeValueColor);
                var hiddenTextSize = g.MeasureString(hiddenText, smallFont);
                // Position immediately to the right of the hidden node circle
                float hiddenTextX = weightX + nodeSize / 2 + 3;
                float hiddenTextY = y + nodeSize / 2 - hiddenTextSize.Height / 2;
                g.DrawString(hiddenText, smallFont, hiddenBrush, hiddenTextX, hiddenTextY);
            }
        }

        // Bias node (below the weight nodes)
        int biasY = startY + nodeCount * nodeSpacing;
        using var biasBrush = new SolidBrush(biasColor);
        g.FillEllipse(biasBrush, weightX - nodeSize / 2, biasY, nodeSize, nodeSize);
        g.DrawEllipse(nodeBorderPen, weightX - nodeSize / 2, biasY, nodeSize, nodeSize);

        // Bias label
        if (nodeSize >= 30)
        {
            string biasText = _bias.ToString("0");
            using var biasTextBrush = new SolidBrush(_bias >= 0 ? positiveValueColor : negativeValueColor);
            var biasTextSize = g.MeasureString(biasText, smallFont);
            g.DrawString(biasText, smallFont, biasTextBrush,
                weightX - biasTextSize.Width / 2, biasY + nodeSize / 2 - biasTextSize.Height / 2);
        }

        // "BIAS" label to left of bias node
        using var biasLabelBrush = new SolidBrush(biasColor);
        g.DrawString("BIAS", smallFont, biasLabelBrush, weightX - nodeSize / 2 - 35, biasY + nodeSize / 2 - 6);

        // Bias output value to the right of the bias node (same as hidden node outputs)
        if (Math.Abs(_bias) > 0.01)
        {
            string biasOutputText = _bias.ToString("+0.0;-0.0");
            using var biasOutputBrush = new SolidBrush(_bias >= 0 ? positiveValueColor : negativeValueColor);
            var biasOutputSize = g.MeasureString(biasOutputText, smallFont);
            float biasOutputX = weightX + nodeSize / 2 + 3;
            float biasOutputY = biasY + nodeSize / 2 - biasOutputSize.Height / 2;
            g.DrawString(biasOutputText, smallFont, biasOutputBrush, biasOutputX, biasOutputY);
        }

        // Line from bias to output
        bool biasActive = Math.Abs(_bias) > 0.01;
        var biasLinePen = biasActive ? activeLinePen : inactiveLinePen;
        g.DrawLine(biasLinePen, weightX + nodeSize / 2, biasY + nodeSize / 2, outputX - nodeSize / 2, outputY + nodeSize / 2);

        // Output node
        using var outputBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        g.FillEllipse(outputBrush, outputX - nodeSize / 2, outputY, nodeSize, nodeSize);
        g.DrawEllipse(nodeBorderPen, outputX - nodeSize / 2, outputY, nodeSize, nodeSize);

        // Output value inside node (if large enough)
        if (nodeSize >= 30)
        {
            string outText = _output.ToString("0");
            using var outTextBrush = new SolidBrush(_output >= 0 ? positiveValueColor : negativeValueColor);
            var outTextSize = g.MeasureString(outText, smallFont);
            g.DrawString(outText, smallFont, outTextBrush,
                outputX - outTextSize.Width / 2, outputY + nodeSize / 2 - outTextSize.Height / 2);
        }

        // Line from output node to final value (short line)
        g.DrawLine(activeLinePen, outputX + nodeSize / 2, outputY + nodeSize / 2, outputX + nodeSize / 2 + 10, outputY + nodeSize / 2);

        // Final output value
        string finalText = _output.ToString("0.0");
        using var finalBrush = new SolidBrush(_output >= 0 ? positiveValueColor : negativeValueColor);
        using var finalFont = new Font("Consolas", 10f, FontStyle.Bold);
        var finalTextSize = g.MeasureString(finalText, finalFont);
        // Position text so it fits within the canvas
        float textX = Math.Min(outputX + nodeSize / 2 + 12, canvasWidth - finalTextSize.Width - 5);
        g.DrawString(finalText, finalFont, finalBrush, textX, outputY + nodeSize / 2 - finalTextSize.Height / 2);

        // Legend at bottom - show actual math rule
        int legendY = canvasHeight - 20;
        using var legendBrush = new SolidBrush(Color.FromArgb(120, 120, 120));
        string modeText = GetMathRuleDisplayName();
        g.DrawString($"{modeText}  |  Nodes: {nodeCount}  |  Output: {(_output >= 0 ? "+" : "")}{_output:0.0}",
            smallFont, legendBrush, 10, legendY);

        // Draw resize grip in bottom-right corner
        DrawResizeGrip(g, canvasWidth, canvasHeight);
    }

    private void DrawResizeGrip(Graphics g, int width, int height)
    {
        using var gripPen = new Pen(Color.FromArgb(180, 60, 60), 1);
        int x = width - RESIZE_GRIP_SIZE;
        int y = height - RESIZE_GRIP_SIZE;

        // Draw three diagonal lines
        g.DrawLine(gripPen, x + 12, y + 4, x + 4, y + 12);
        g.DrawLine(gripPen, x + 12, y + 7, x + 7, y + 12);
        g.DrawLine(gripPen, x + 12, y + 10, x + 10, y + 12);
    }

    // Window dragging
    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
            if (sender is Label) _dragStart = new Point(_dragStart.X + _titleLabel.Left, _dragStart.Y + _titleLabel.Top);
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Point screenPos = sender is Label ? _titleLabel.PointToScreen(e.Location) : _titleBar.PointToScreen(e.Location);
            Location = new Point(screenPos.X - _dragStart.X, screenPos.Y - _dragStart.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e) => _isDragging = false;

    // Enable resizing via edges
    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        const int HTBOTTOMRIGHT = 17;
        const int HTBOTTOM = 15;
        const int HTRIGHT = 11;
        const int HTLEFT = 10;
        const int HTTOP = 12;
        const int HTTOPLEFT = 13;
        const int HTTOPRIGHT = 14;
        const int HTBOTTOMLEFT = 16;

        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);
            Point pos = PointToClient(new Point(m.LParam.ToInt32()));
            int borderWidth = 8;

            if (pos.X <= borderWidth && pos.Y <= borderWidth)
                m.Result = (IntPtr)HTTOPLEFT;
            else if (pos.X >= ClientSize.Width - borderWidth && pos.Y <= borderWidth)
                m.Result = (IntPtr)HTTOPRIGHT;
            else if (pos.X <= borderWidth && pos.Y >= ClientSize.Height - borderWidth)
                m.Result = (IntPtr)HTBOTTOMLEFT;
            else if (pos.X >= ClientSize.Width - borderWidth && pos.Y >= ClientSize.Height - borderWidth)
                m.Result = (IntPtr)HTBOTTOMRIGHT;
            else if (pos.Y <= borderWidth)
                m.Result = (IntPtr)HTTOP;
            else if (pos.Y >= ClientSize.Height - borderWidth)
                m.Result = (IntPtr)HTBOTTOM;
            else if (pos.X <= borderWidth)
                m.Result = (IntPtr)HTLEFT;
            else if (pos.X >= ClientSize.Width - borderWidth)
                m.Result = (IntPtr)HTRIGHT;

            return;
        }

        base.WndProc(ref m);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Border
        using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }
}

public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
}
