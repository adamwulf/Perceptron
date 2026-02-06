namespace PerceptronSimulator;

/// <summary>
/// Dialog that displays a buildable circuit schematic for the current
/// forward-propagation network using op-amps, resistors, and standard components.
/// </summary>
public class PrintSchematicDialog : Form
{
    private const int TITLE_BAR_HEIGHT = 30;

    private readonly int[] _inputs;
    private readonly double[] _weights;
    private readonly double _bias;
    private readonly bool[] _switchStates;
    private readonly int _gridSize;
    private readonly bool _isLinearMode;
    private readonly int _linearNodeCount;
    private readonly PerceptronSimulator.Controls.ConfigKnob.MathRule _mathRule;

    // Custom chrome
    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private bool _isDragging;
    private Point _dragStart;

    private Panel _schematicPanel = null!;
    private Button _saveButton = null!;
    private Button _pocButton = null!;
    private Button _kicadButton = null!;

    public PrintSchematicDialog(int[] inputs, double[] weights, double bias, bool[] switchStates,
        int gridSize, bool isLinearMode, int linearNodeCount, PerceptronSimulator.Controls.ConfigKnob.MathRule mathRule)
    {
        _inputs = inputs;
        _weights = weights;
        _bias = bias;
        _switchStates = switchStates;
        _gridSize = gridSize;
        _isLinearMode = isLinearMode;
        _linearNodeCount = linearNodeCount;
        _mathRule = mathRule;

        InitializeForm();
        InitializeCustomChrome();
        InitializeContent();
    }

    private void InitializeForm()
    {
        Text = "Circuit Schematic";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(20, 20, 20);
        DoubleBuffered = true;
        ShowInTaskbar = false;

        // Size based on number of inputs
        int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        int dialogWidth = Math.Max(1200, 500 + nodeCount * 50);
        int dialogHeight = Math.Max(800, 550 + nodeCount * 25);
        Size = new Size(Math.Min(dialogWidth, 1600), Math.Min(dialogHeight, 1100));
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
            Text = "CIRCUIT SCHEMATIC",
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
        _closeButton.Click += (s, e) => Close();

        _titleBar.Controls.Add(_closeButton);
        _titleBar.Controls.Add(_titleLabel);

        _titleBar.Resize += (s, e) =>
        {
            _titleLabel.Location = new Point((_titleBar.Width - _titleLabel.Width) / 2,
                (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2);
        };

        Controls.Add(_titleBar);
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
            if (sender is Label) _dragStart = _titleLabel.Location + (Size)e.Location;
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Point currentPos = PointToScreen(e.Location);
            if (sender is Label) currentPos = PointToScreen(_titleLabel.Location + (Size)e.Location);
            Location = new Point(currentPos.X - _dragStart.X, currentPos.Y - _dragStart.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void InitializeContent()
    {
        int contentTop = TITLE_BAR_HEIGHT + 10;

        // Schematic panel - white background like engineering paper
        _schematicPanel = new Panel
        {
            BackColor = Color.White,
            Location = new Point(15, contentTop),
            Size = new Size(ClientSize.Width - 30, ClientSize.Height - contentTop - 50),
            BorderStyle = BorderStyle.FixedSingle
        };
        _schematicPanel.Paint += SchematicPanel_Paint;

        // Save image button
        _saveButton = new Button
        {
            Text = "Save Image",
            Size = new Size(100, 30),
            Location = new Point(ClientSize.Width - 125, ClientSize.Height - 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        _saveButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _saveButton.Click += SaveButton_Click;

        // POC button - shows breadboard layout
        _pocButton = new Button
        {
            Text = "Breadboard",
            Size = new Size(90, 30),
            Location = new Point(15, ClientSize.Height - 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 120, 80),
            ForeColor = Color.FromArgb(220, 220, 220)
        };
        _pocButton.FlatAppearance.BorderColor = Color.FromArgb(100, 140, 100);
        _pocButton.Click += POCButton_Click;

        // KiCad export button
        _kicadButton = new Button
        {
            Text = "Export KiCad",
            Size = new Size(110, 30),
            Location = new Point(110, ClientSize.Height - 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(100, 80, 120),
            ForeColor = Color.FromArgb(220, 220, 220)
        };
        _kicadButton.FlatAppearance.BorderColor = Color.FromArgb(120, 100, 140);
        _kicadButton.Click += KiCadButton_Click;

        Controls.Add(_schematicPanel);
        Controls.Add(_saveButton);
        Controls.Add(_pocButton);
        Controls.Add(_kicadButton);
    }

    private void POCButton_Click(object? sender, EventArgs e)
    {
        // Show modalless so it can stay open alongside other dialogs
        var pocDialog = new POCBreadboardDialog(_inputs, _weights, _bias, _switchStates,
            _gridSize, _isLinearMode, _linearNodeCount, _mathRule);
        pocDialog.Show(this);
    }

    private void KiCadButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export to KiCad Schematic",
            Filter = "KiCad Schematic (*.kicad_sch)|*.kicad_sch|All Files (*.*)|*.*",
            DefaultExt = "kicad_sch",
            FileName = "perceptron_circuit"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
            var exporter = new KiCadExporter(_inputs, _weights, _bias, _switchStates, nodeCount);
            exporter.ExportToFile(dialog.FileName);

            MessageBox.Show(
                "KiCad schematic exported successfully!\n\n" +
                "You can now:\n" +
                "1. Open the .kicad_sch file in KiCad\n" +
                "2. Run SPICE simulation (Tools → Simulator)\n" +
                "3. Verify the circuit operates correctly",
                "Export Successful",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save Circuit Schematic",
            Filter = "PNG Image (*.png)|*.png|All Files (*.*)|*.*",
            DefaultExt = "png",
            FileName = "perceptron_circuit"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var bitmap = new Bitmap(_schematicPanel.Width, _schematicPanel.Height);
            _schematicPanel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Schematic saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SchematicPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        int panelWidth = _schematicPanel.ClientSize.Width;
        int panelHeight = _schematicPanel.ClientSize.Height;

        using var linePen = new Pen(Color.Black, 1.5f);
        using var thinPen = new Pen(Color.Black, 1f);
        using var textBrush = new SolidBrush(Color.Black);
        using var redBrush = new SolidBrush(Color.FromArgb(200, 50, 50));
        using var blueBrush = new SolidBrush(Color.FromArgb(50, 50, 200));
        using var greenBrush = new SolidBrush(Color.FromArgb(50, 150, 50));
        using var titleFont = new Font("Arial", 12f, FontStyle.Bold);
        using var labelFont = new Font("Arial", 8f);
        using var smallFont = new Font("Arial", 7f);
        using var tinyFont = new Font("Arial", 6f);

        // Draw grid (faint)
        using var gridPen = new Pen(Color.FromArgb(230, 230, 230), 0.5f);
        for (int x = 0; x < panelWidth; x += 20)
            g.DrawLine(gridPen, x, 0, x, panelHeight);
        for (int y = 0; y < panelHeight; y += 20)
            g.DrawLine(gridPen, 0, y, panelWidth, y);

        // Draw circuit based on selected topology
        switch (_mathRule)
        {
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.BACKPROP:
                DrawBackpropCircuit(g, panelWidth, panelHeight, nodeCount, titleFont, labelFont, smallFont, tinyFont,
                    textBrush, redBrush, blueBrush, greenBrush, linePen, thinPen);
                break;

            // All 1958/1960 variants use the same inverting summing amplifier topology
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.PERCEPTRON_CLASSIC:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.WIDROW_HOFF:
            default:
                Draw1958Circuit(g, panelWidth, panelHeight, nodeCount, titleFont, labelFont, smallFont, tinyFont,
                    textBrush, redBrush, blueBrush, greenBrush, linePen, thinPen);
                break;
        }
    }

    private void Draw1958Circuit(Graphics g, int panelWidth, int panelHeight, int nodeCount,
        Font titleFont, Font labelFont, Font smallFont, Font tinyFont,
        SolidBrush textBrush, SolidBrush redBrush, SolidBrush blueBrush, SolidBrush greenBrush,
        Pen linePen, Pen thinPen)
    {
        // Title
        string title = "PERCEPTRON - ANALOG SUMMING CIRCUIT (1958)";
        g.DrawString(title, titleFont, textBrush, 20, 10);

        // Subtitle with component summary
        string rfValue = PartsDatabase.FormatResistorValue(PartsDatabase.ReferenceResistor);
        string subtitle = $"Inputs: {nodeCount} | Feedback Rf: {rfValue} | Supply: +5V (9V battery + {PartsDatabase.VoltageRegulator5V.PartNumber})";
        g.DrawString(subtitle, labelFont, textBrush, 20, 30);

        // Layout zones - increased margins to prevent overlap
        int leftMargin = 160;
        int topMargin = 55;
        int bottomMargin = 130;

        // Calculate spacing
        int availableHeight = panelHeight - topMargin - bottomMargin;
        int nodeSpacing = Math.Min(40, availableHeight / (nodeCount + 3));
        int startY = topMargin + 25;

        // Power supply section (far left, below title)
        DrawPowerSupply(g, 15, 50, linePen, textBrush, labelFont, smallFont);

        // Op-amp position (center-right) - need enough room for comparator, LED, and ground on right
        // Ensure minimum distance from left margin to prevent overlap with switches/power supply
        int opAmpX = Math.Max(panelWidth - 400, leftMargin + 400);
        int opAmpY = startY + (nodeCount * nodeSpacing) / 2;

        // Draw each input channel
        int sumBusX = opAmpX - 80;

        // Track min/max Y for continuous vertical bus
        int minY = startY;
        int maxY = startY + (nodeCount - 1) * nodeSpacing;

        for (int i = 0; i < nodeCount; i++)
        {
            int y = startY + i * nodeSpacing;
            bool isOn = i < _switchStates.Length && _switchStates[i];
            double weight = i < _weights.Length ? _weights[i] : 0;

            // Input label - position to the left of switch
            string inputLabel = $"IN{i + 1}";
            g.DrawString(inputLabel, tinyFont, textBrush, leftMargin - 35, y - 5);

            // SPDT Switch
            // For inverting summer: positive weights need swapped switch wiring
            // because the amplifier inverts. Swapping the switch effectively negates
            // the input, and (-input) through inverting amp = +input contribution.
            bool swapSwitch = weight > 0.01;  // Positive weights need swapped wiring
            int switchX = leftMargin + 5;
            DrawSPDTSwitch(g, switchX, y, isOn, swapSwitch, linePen, thinPen, textBrush, tinyFont);

            // Calculate resistor value based on weight magnitude
            // For inverting summer: Vout = -Rf * Σ(Vin/Rin)
            // Rin = Rf/|weight| (use PartsDatabase for calculation)
            double resistorValue = PartsDatabase.CalculateInputResistance(weight);
            string resistorLabel = FormatResistance(resistorValue);

            // Mark swapped switches for clarity
            bool needsSwapNote = swapSwitch;

            // Input resistor (resistor drawing is 40px wide)
            int resistorX = switchX + 55;

            // Connection line from switch output to resistor input
            g.DrawLine(linePen, switchX + 40, y, resistorX, y);

            DrawResistor(g, resistorX, y, resistorLabel, weight, linePen, textBrush, tinyFont, needsSwapNote);

            // Connection line from resistor output to summing bus (resistor ends at resistorX + 40)
            g.DrawLine(linePen, resistorX + 40, y, sumBusX, y);

            // Connection dot at summing bus junction
            g.FillEllipse(Brushes.Black, sumBusX - 2, y - 2, 4, 4);
        }

        // Draw continuous vertical summing bus (from highest input to lowest, extending to opAmpY)
        int busTopY = Math.Min(minY, opAmpY);
        int busBottomY = Math.Max(maxY, opAmpY);
        g.DrawLine(linePen, sumBusX, busTopY, sumBusX, busBottomY);

        // Bias input (below the main inputs)
        int biasY = startY + nodeCount * nodeSpacing + 20;
        if (Math.Abs(_bias) > 0.01)
        {
            g.DrawString("BIAS", tinyFont, textBrush, leftMargin - 30, biasY - 5);

            // Bias voltage source
            DrawVoltageSource(g, leftMargin + 20, biasY, _bias, linePen, textBrush, tinyFont);

            // Bias resistor (Rf for unity gain on bias)
            double biasResistor = PartsDatabase.ReferenceResistor;
            int biasResX = leftMargin + 70;
            DrawResistor(g, biasResX, biasY, FormatResistance(biasResistor), _bias, linePen, textBrush, tinyFont, _bias < 0);

            // Connect bias resistor output (at biasResX + 40) to summing bus
            g.DrawLine(linePen, biasResX + 40, biasY, sumBusX, biasY);

            // Connection dot at summing bus junction
            g.FillEllipse(Brushes.Black, sumBusX - 2, biasY - 2, 4, 4);

            // Extend vertical bus if bias extends below
            busBottomY = Math.Max(busBottomY, biasY);
            g.DrawLine(linePen, sumBusX, Math.Min(maxY, opAmpY), sumBusX, busBottomY);
        }

        // Summing junction node
        g.FillEllipse(Brushes.Black, sumBusX - 3, opAmpY - 3, 6, 6);

        // Line from summing bus to op-amp inverting input (- is at top, offset from center)
        int invInputY = opAmpY - 8; // Inverting input is above center
        g.DrawLine(linePen, sumBusX, opAmpY, sumBusX, invInputY);
        g.DrawLine(linePen, sumBusX, invInputY, opAmpX, invInputY);

        // Op-amp U1 (summing amplifier) - LM358 dual op-amp, section A
        // Draw op-amp first so we know exact output position
        DrawOpAmp(g, opAmpX, opAmpY, "U1A", PartsDatabase.OpAmpLM358.PartNumber, linePen, textBrush, labelFont, smallFont);
        int opAmpOutX = opAmpX + 50; // Op-amp output point (tip of triangle, size=50)

        // Feedback resistor (Rf = 10kΩ) - connects from op-amp output back to summing junction
        int feedbackY = opAmpY - 45;
        int fbResistorX = sumBusX + 20; // Where resistor starts

        // Vertical from summing node up to feedback level
        g.DrawLine(linePen, sumBusX, opAmpY, sumBusX, feedbackY);
        // Horizontal line TO the resistor (resistor draws its own lead-in from x to x+5)
        g.DrawLine(linePen, sumBusX, feedbackY, fbResistorX, feedbackY);
        // The resistor (40px wide, from fbResistorX to fbResistorX+40)
        string rfLabel = PartsDatabase.FormatResistorValue(PartsDatabase.ReferenceResistor) + " (Rf)";
        DrawResistor(g, fbResistorX, feedbackY, rfLabel, 1, linePen, textBrush, tinyFont, false);
        // Horizontal line FROM the resistor to the vertical drop
        g.DrawLine(linePen, fbResistorX + 40, feedbackY, opAmpOutX + 5, feedbackY);
        // Vertical from feedback level down to output
        g.DrawLine(linePen, opAmpOutX + 5, feedbackY, opAmpOutX + 5, opAmpY);
        // Short horizontal to connect to output point
        g.DrawLine(linePen, opAmpOutX, opAmpY, opAmpOutX + 5, opAmpY);

        // Non-inverting input to virtual ground (2.5V via voltage divider)
        // + input is below center
        int nonInvY = opAmpY + 8;
        g.DrawLine(linePen, opAmpX, nonInvY, opAmpX - 25, nonInvY);
        DrawVirtualGround(g, opAmpX - 25, nonInvY, linePen, textBrush, tinyFont);

        // Output from U1 to comparator
        int compX = opAmpOutX + 100; // Comparator position
        int compInvY = opAmpY - 8; // Comparator inverting input (above center)

        // Draw line from U1 output, then to comparator inverting input
        g.DrawLine(linePen, opAmpOutX + 5, opAmpY, opAmpOutX + 40, opAmpY);
        g.DrawLine(linePen, opAmpOutX + 40, opAmpY, opAmpOutX + 40, compInvY);
        g.DrawLine(linePen, opAmpOutX + 40, compInvY, compX, compInvY);

        // Comparator stage (U1B) - LM358 dual op-amp, section B used as comparator
        DrawOpAmp(g, compX, opAmpY, "U1B", PartsDatabase.OpAmpLM358.PartNumber, linePen, textBrush, labelFont, smallFont, false);

        // Comparator non-inverting input to ground/Vref (below center)
        int compNonInvY = opAmpY + 8;
        g.DrawLine(linePen, compX, compNonInvY, compX - 25, compNonInvY);
        g.DrawLine(linePen, compX - 25, compNonInvY, compX - 25, compNonInvY + 30);
        DrawGround(g, compX - 25, compNonInvY + 30, linePen);
        g.DrawString("Vref", tinyFont, textBrush, compX - 48, compNonInvY + 8);

        // Comparator output point
        int compOutX = compX + 50; // Op-amp output is at x + 50 (size=50)

        // Pull-up resistor on comparator output
        // Resistor is 25px tall with 5px leads on each end, centered at opAmpY - 50
        // So resistor spans from opAmpY - 33 (bottom lead end) to opAmpY - 67 (top lead end)
        int pullupX = compOutX + 10;
        int pullupResY = opAmpY - 50;
        int resistorHalfHeight = 12; // half of 25
        int leadLength = 5;

        // Horizontal from comparator output
        g.DrawLine(linePen, compOutX, opAmpY, pullupX, opAmpY);
        // Vertical from output level TO resistor bottom lead (not through it)
        g.DrawLine(linePen, pullupX, opAmpY, pullupX, pullupResY + resistorHalfHeight + leadLength);
        // The resistor (draws its own leads) - pull-up resistor from database
        string pullupValue = PartsDatabase.FormatResistorValue(PartsDatabase.Resistor4k7.Resistance ?? 4700);
        DrawResistorVertical(g, pullupX, pullupResY, pullupValue, linePen, textBrush, tinyFont);
        // Vertical FROM resistor top lead to +5V
        g.DrawLine(linePen, pullupX, pullupResY - resistorHalfHeight - leadLength, pullupX, opAmpY - 90);
        g.DrawString("+5V", tinyFont, redBrush, pullupX - 8, opAmpY - 105);

        // Output LED circuit - connect from pull-up junction
        int ledX = compOutX + 60;
        g.DrawLine(linePen, compOutX + 10, opAmpY, ledX, opAmpY);

        // Current limiting resistor for LED (resistor is 40px wide)
        string ledResValue = PartsDatabase.FormatResistorValue(PartsDatabase.Resistor470R.Resistance ?? 470);
        DrawResistor(g, ledX, opAmpY, ledResValue, 1, linePen, textBrush, tinyFont, false);

        // Connect resistor output to LED input
        int ledPosX = ledX + 50; // LED starts 10px after resistor ends (resistor end = ledX+40)
        g.DrawLine(linePen, ledX + 40, opAmpY, ledPosX, opAmpY);

        // LED (has internal 10px lead-in line)
        DrawLED(g, ledPosX, opAmpY, linePen, greenBrush, textBrush, tinyFont);

        // LED to ground (LED is 15px wide, plus internal lead-out)
        int ledEndX = ledPosX + 25; // LED output point
        g.DrawLine(linePen, ledEndX, opAmpY, ledEndX + 15, opAmpY);
        g.DrawLine(linePen, ledEndX + 15, opAmpY, ledEndX + 15, opAmpY + 25);
        DrawGround(g, ledEndX + 15, opAmpY + 25, linePen);

        // Digital display (voltmeter) tapping from U1 output
        int meterX = opAmpOutX + 20;
        int meterY = opAmpY + 70;
        g.DrawLine(thinPen, opAmpOutX + 5, opAmpY, opAmpOutX + 5, opAmpY + 10);
        g.DrawLine(thinPen, opAmpOutX + 5, opAmpY + 10, meterX, meterY - 15);
        DrawVoltmeter(g, meterX - 25, meterY, linePen, textBrush, labelFont, smallFont);

        // Calculate and show output value
        double sum = 0;
        for (int i = 0; i < Math.Min(_inputs.Length, _weights.Length); i++)
        {
            sum += _inputs[i] * _weights[i];
        }
        sum += _bias;
        // Inverting amplifier inverts the sign
        double outputVoltage = -sum; // Simplified - actual scaling depends on voltage levels
        g.DrawString($"≈ {sum:+0.0;-0.0;0}V", smallFont, textBrush, meterX - 20, meterY + 45);

        // Notes section - positioned at bottom with adequate spacing
        int noteY = panelHeight - 115;
        string rfValueNote = PartsDatabase.FormatResistorValue(PartsDatabase.ReferenceResistor);
        g.DrawString("NOTES:", labelFont, textBrush, 20, noteY);
        g.DrawString($"• +5V supply from 9V battery + {PartsDatabase.VoltageRegulator5V.PartNumber}; add 0.1µF/10µF caps", smallFont, textBrush, 20, noteY + 14);
        g.DrawString($"• Rin = {rfValueNote}/|weight| (BOM has E24 values)", smallFont, textBrush, 20, noteY + 26);
        using var orangeBrush2 = new SolidBrush(Color.OrangeRed);
        g.DrawString("• Resistors marked * have SWAPPED switch wiring (positive weights)", smallFont, orangeBrush2, 20, noteY + 38);
        g.DrawString("• Vref = 2.5V from 10k/10k voltage divider", smallFont, textBrush, 20, noteY + 50);

        // Component list - positioned to the right
        int listX = panelWidth - 200;
        g.DrawString("COMPONENTS:", labelFont, textBrush, listX, noteY);
        g.DrawString($"• {nodeCount}x {PartsDatabase.SwitchSPDT.Description}", smallFont, textBrush, listX, noteY + 14);
        g.DrawString($"• {nodeCount + 5} Resistors", smallFont, textBrush, listX, noteY + 26);
        g.DrawString($"• 1x {PartsDatabase.OpAmpLM358.PartNumber} dual op-amp", smallFont, textBrush, listX, noteY + 38);
        g.DrawString($"• 1x {PartsDatabase.VoltageRegulator5V.PartNumber} + LED + caps", smallFont, textBrush, listX, noteY + 50);
    }

    private void DrawPowerSupply(Graphics g, int x, int y, Pen pen, Brush textBrush, Font labelFont, Font smallFont)
    {
        using var redBrush = new SolidBrush(Color.Red);

        g.DrawString("POWER SUPPLY", labelFont, textBrush, x, y - 5);

        // Draw compact power supply block
        int boxY = y + 12;
        g.DrawRectangle(pen, x, boxY, 90, 35);

        // Labels inside/near box
        g.DrawString("9V → 7805 → +5V", smallFont, textBrush, x + 3, boxY + 5);
        g.DrawString("GND (0V)", smallFont, textBrush, x + 3, boxY + 18);

        // Output indicators
        g.DrawString("+5V", smallFont, redBrush, x + 95, boxY + 3);
        g.DrawLine(pen, x + 90, boxY + 8, x + 115, boxY + 8);

        g.DrawString("GND", smallFont, textBrush, x + 95, boxY + 18);
        g.DrawLine(pen, x + 90, boxY + 23, x + 115, boxY + 23);
    }

    private void DrawSPDTSwitch(Graphics g, int x, int y, bool isOn, bool swapWiring, Pen pen, Pen thinPen, Brush textBrush, Font font)
    {
        // SPDT switch: common in middle, voltage rails on top/bottom contacts
        // swapWiring: when true, swap which contact gets +5V vs 0V
        // This is needed for positive weights because the inverting amplifier inverts.
        int switchHeight = 18;

        using var redBrush = new SolidBrush(Color.FromArgb(180, 0, 0));
        using var orangeBrush = new SolidBrush(Color.OrangeRed);

        // Top contact
        g.DrawLine(thinPen, x, y - switchHeight / 2, x + 12, y - switchHeight / 2);
        g.FillEllipse(Brushes.Black, x + 10, y - switchHeight / 2 - 2, 4, 4);

        // Bottom contact
        g.DrawLine(thinPen, x, y + switchHeight / 2, x + 12, y + switchHeight / 2);
        g.FillEllipse(Brushes.Black, x + 10, y + switchHeight / 2 - 2, 4, 4);

        // Common terminal
        g.FillEllipse(Brushes.Black, x + 23, y - 2, 4, 4);
        g.DrawLine(pen, x + 27, y, x + 40, y);

        // Switch arm
        bool connectToTop = isOn;
        if (connectToTop)
        {
            g.DrawLine(pen, x + 14, y - switchHeight / 2, x + 25, y);
        }
        else
        {
            g.DrawLine(pen, x + 14, y + switchHeight / 2, x + 25, y);
        }

        // Compact voltage labels on left side only (no overlap with resistor)
        string topLabel = swapWiring ? "0" : "5";
        string bottomLabel = swapWiring ? "5" : "0";
        g.DrawString(topLabel, font, swapWiring ? textBrush : redBrush, x - 8, y - switchHeight / 2 - 4);
        g.DrawString(bottomLabel, font, swapWiring ? redBrush : textBrush, x - 8, y + switchHeight / 2 - 6);

        // Mark swapped switches with small asterisk after resistor (handled in main draw)
    }

    private void DrawResistor(Graphics g, int x, int y, string value, double weight, Pen pen, Brush textBrush, Font font, bool needsInverter)
    {
        // Standard resistor symbol (rectangular box - IEC style)
        int width = 40;
        int height = 10;

        g.DrawLine(pen, x, y, x + 5, y);
        g.DrawRectangle(pen, x + 5, y - height / 2, width - 10, height);
        g.DrawLine(pen, x + width - 5, y, x + width, y);

        // Value label
        var valueSize = g.MeasureString(value, font);
        g.DrawString(value, font, textBrush, x + (width - valueSize.Width) / 2, y - height / 2 - 12);

        // Mark negative weights with asterisk
        if (needsInverter)
        {
            using var orangeBrush = new SolidBrush(Color.OrangeRed);
            g.DrawString("*", font, orangeBrush, x + width - 5, y - height / 2 - 12);
        }
    }

    private void DrawResistorVertical(Graphics g, int x, int y, string value, Pen pen, Brush textBrush, Font font)
    {
        // Vertical resistor
        int width = 10;
        int height = 25;

        g.DrawLine(pen, x, y - height / 2 - 5, x, y - height / 2);
        g.DrawRectangle(pen, x - width / 2, y - height / 2, width, height);
        g.DrawLine(pen, x, y + height / 2, x, y + height / 2 + 5);

        // Value label
        g.DrawString(value, font, textBrush, x + 8, y - 5);
    }

    private void DrawOpAmp(Graphics g, int x, int y, string refDes, string partNum, Pen pen, Brush textBrush, Font labelFont, Font smallFont, bool showPartNumber = true)
    {
        // Op-amp triangle symbol
        int size = 50;
        var points = new Point[]
        {
            new Point(x, y - size / 2),      // Top
            new Point(x, y + size / 2),      // Bottom
            new Point(x + size, y)           // Output point
        };
        g.DrawPolygon(pen, points);

        // Inverting input (-)
        g.DrawString("-", labelFont, textBrush, x + 5, y - 12);
        // Non-inverting input (+)
        g.DrawString("+", labelFont, textBrush, x + 5, y + 2);

        // Reference designator and part number (only if showPartNumber is true)
        g.DrawString(refDes, smallFont, textBrush, x + 15, y - 5);
        if (showPartNumber)
        {
            g.DrawString(partNum, smallFont, textBrush, x + 10, y + size / 2 + 2);
        }

        // Power pins (implied, shown as labels) - single +5V supply with GND
        using var redBrush = new SolidBrush(Color.Red);
        g.DrawString("+5V", smallFont, redBrush, x + size / 2 - 7, y - size / 2 - 12);
        g.DrawString("GND", smallFont, textBrush, x + size / 2 - 8, y + size / 2 + 2);
    }

    private void DrawLED(Graphics g, int x, int y, Pen pen, Brush fillBrush, Brush textBrush, Font font)
    {
        // LED symbol: triangle with line (cathode bar) and arrows
        int size = 15;

        // Triangle (anode side)
        var points = new Point[]
        {
            new Point(x, y - size / 2),
            new Point(x, y + size / 2),
            new Point(x + size, y)
        };
        g.FillPolygon(fillBrush, points);
        g.DrawPolygon(pen, points);

        // Cathode bar
        g.DrawLine(pen, x + size, y - size / 2, x + size, y + size / 2);

        // Light arrows
        using var arrowPen = new Pen(Color.Green, 1);
        g.DrawLine(arrowPen, x + size / 2, y - size / 2 - 3, x + size / 2 + 8, y - size / 2 - 10);
        g.DrawLine(arrowPen, x + size / 2 + 5, y - size / 2 - 3, x + size / 2 + 13, y - size / 2 - 10);

        // Small arrowheads
        g.DrawLine(arrowPen, x + size / 2 + 8, y - size / 2 - 10, x + size / 2 + 5, y - size / 2 - 8);
        g.DrawLine(arrowPen, x + size / 2 + 13, y - size / 2 - 10, x + size / 2 + 10, y - size / 2 - 8);

        // Label
        g.DrawString("LED", font, textBrush, x, y + size / 2 + 2);

        // Connection lines
        g.DrawLine(pen, x - 10, y, x, y);
        g.DrawLine(pen, x + size, y, x + size + 10, y);
    }

    private void DrawVoltmeter(Graphics g, int x, int y, Pen pen, Brush textBrush, Font labelFont, Font smallFont)
    {
        // Digital voltmeter symbol (rectangle with V)
        int width = 50;
        int height = 35;

        g.DrawRectangle(pen, x, y, width, height);
        g.DrawString("DVM", smallFont, textBrush, x + 15, y + 3);

        // Display area
        g.FillRectangle(Brushes.Black, x + 5, y + 15, width - 10, 15);
    }

    private void DrawVoltageSource(Graphics g, int x, int y, double voltage, Pen pen, Brush textBrush, Font font)
    {
        // Voltage source symbol (circle with + and -)
        int size = 20;
        g.DrawEllipse(pen, x - size / 2, y - size / 2, size, size);

        // + and - signs
        if (voltage >= 0)
        {
            g.DrawString("+", font, textBrush, x - 4, y - size / 2 - 2);
            g.DrawString("-", font, textBrush, x - 3, y + size / 2 - 8);
        }
        else
        {
            g.DrawString("-", font, textBrush, x - 3, y - size / 2 - 2);
            g.DrawString("+", font, textBrush, x - 4, y + size / 2 - 8);
        }

        // Voltage value
        g.DrawString($"{Math.Abs(voltage):0.0}V", font, textBrush, x + size / 2 + 2, y - 5);

        // Connection lines
        g.DrawLine(pen, x - size / 2 - 10, y, x - size / 2, y);
        g.DrawLine(pen, x + size / 2, y, x + size / 2 + 10, y);
    }

    private void DrawGround(Graphics g, int x, int y, Pen pen)
    {
        // Standard ground symbol (three horizontal lines)
        g.DrawLine(pen, x, y, x, y + 5);
        g.DrawLine(pen, x - 8, y + 5, x + 8, y + 5);
        g.DrawLine(pen, x - 5, y + 9, x + 5, y + 9);
        g.DrawLine(pen, x - 2, y + 13, x + 2, y + 13);
    }

    private void DrawVirtualGround(Graphics g, int x, int y, Pen pen, Brush textBrush, Font font)
    {
        // Virtual ground: voltage divider creating 2.5V (mid-rail) reference
        // Compact representation

        // Connection point at 2.5V
        g.FillEllipse(Brushes.Black, x - 2, y - 2, 4, 4);

        // Line down to divider box
        g.DrawLine(pen, x, y, x, y + 5);

        // Simplified divider box with label
        g.DrawRectangle(pen, x - 12, y + 5, 24, 25);
        g.DrawString("Vref", font, textBrush, x - 9, y + 8);
        g.DrawString("2.5V", font, textBrush, x - 9, y + 18);

        // Ground below
        g.DrawLine(pen, x, y + 30, x, y + 35);
        g.DrawLine(pen, x - 5, y + 35, x + 5, y + 35);
        g.DrawLine(pen, x - 3, y + 38, x + 3, y + 38);
        g.DrawLine(pen, x - 1, y + 41, x + 1, y + 41);
    }

    private string FormatResistance(double ohms)
    {
        if (ohms >= 1000000)
            return $"{ohms / 1000000:0.#}MΩ";
        else if (ohms >= 1000)
            return $"{ohms / 1000:0.#}kΩ";
        else
            return $"{ohms:0}Ω";
    }

    private void DrawBackpropCircuit(Graphics g, int panelWidth, int panelHeight, int nodeCount,
        Font titleFont, Font labelFont, Font smallFont, Font tinyFont,
        SolidBrush textBrush, SolidBrush redBrush, SolidBrush blueBrush, SolidBrush greenBrush,
        Pen linePen, Pen thinPen)
    {
        // Title
        string title = "MULTI-LAYER PERCEPTRON - BACKPROPAGATION (1986)";
        g.DrawString(title, titleFont, textBrush, 20, 10);

        // Architecture explanation
        g.DrawString("ARCHITECTURE: Input → Hidden Layer (ReLU) → Output", labelFont, textBrush, 20, 35);

        int y = 65;
        g.DrawString("ANALOG IMPLEMENTATION NOTES:", labelFont, new SolidBrush(Color.FromArgb(180, 100, 50)), 20, y);
        y += 20;

        var notes = new[] {
            "• Backprop networks are impractical to build in analog hardware",
            "• Would require N op-amps for hidden layer ReLU activation",
            $"• This network: {nodeCount} inputs × {nodeCount} hidden nodes = {nodeCount * nodeCount} connections",
            "• Hidden weights (W¹) are learned internally - not shown on physical knobs",
            "• Knobs show output weights (W²) only",
            "",
            "PRACTICAL RECOMMENDATION:",
            "• Use 1958/1960 modes for buildable analog circuits",
            "• Use Backprop mode for software simulation only",
            "• Digital implementation (microcontroller) recommended for MLP"
        };

        foreach (var note in notes)
        {
            g.DrawString(note, smallFont, textBrush, 30, y);
            y += 14;
        }

        // Conceptual block diagram
        y += 20;
        g.DrawString("CONCEPTUAL TOPOLOGY:", labelFont, blueBrush, 20, y);
        y += 25;

        int blockY = y;
        int blockSpacing = 200;

        // Input layer
        g.DrawRectangle(linePen, 50, blockY, 80, 60);
        g.DrawString("INPUTS", smallFont, textBrush, 60, blockY + 10);
        g.DrawString($"({nodeCount})", tinyFont, textBrush, 70, blockY + 28);
        g.DrawString("±1V", tinyFont, textBrush, 75, blockY + 42);

        // Hidden layer
        g.DrawRectangle(linePen, 50 + blockSpacing, blockY, 80, 60);
        g.DrawString("HIDDEN", smallFont, textBrush, 55 + blockSpacing, blockY + 10);
        g.DrawString($"({nodeCount})", tinyFont, textBrush, 65 + blockSpacing, blockY + 28);
        g.DrawString("ReLU", tinyFont, textBrush, 70 + blockSpacing, blockY + 42);

        // Output layer
        g.DrawRectangle(linePen, 50 + blockSpacing * 2, blockY, 80, 60);
        g.DrawString("OUTPUT", smallFont, textBrush, 55 + blockSpacing * 2, blockY + 10);
        g.DrawString("(1)", tinyFont, textBrush, 75 + blockSpacing * 2, blockY + 28);
        g.DrawString("Linear", tinyFont, textBrush, 65 + blockSpacing * 2, blockY + 42);

        // Arrows
        using var arrowPen = new Pen(Color.Black, 2f);
        arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor;
        g.DrawLine(arrowPen, 135, blockY + 30, 245, blockY + 30);
        g.DrawString("W¹", tinyFont, textBrush, 180, blockY + 15);
        g.DrawLine(arrowPen, 335, blockY + 30, 445, blockY + 30);
        g.DrawString("W²", tinyFont, textBrush, 380, blockY + 15);
    }
}
