namespace PerceptronSimulator;

/// <summary>
/// Dialog showing a hobbyist breadboard layout with explicit wiring
/// that can be used to physically build the perceptron circuit.
/// Includes hoverable components that show detailed part information.
/// </summary>
public class POCBreadboardDialog : Form
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

    private Panel _breadboardPanel = null!;
    private Panel _detailPanel = null!;
    private Button _saveButton = null!;
    private Button _bomButton = null!;
    private ComboBox _powerSupplySelector = null!;

    // DEBUG TOGGLE: CheckBox on breadboard dialog to show hit regions and coordinates
    private CheckBox _debugToggle = null!;
    private bool _debugMode = false;

    // Power supply configuration
    private enum PowerSupplyMode
    {
        OffBoard,           // Just show +5V/GND rails (supply off-board)
        OnBoard_Battery,    // 9V battery + 7805 on breadboard
        OnBoard_USB,        // USB 5V direct (no regulator needed)
        OnBoard_BenchSupply // Lab bench supply (just wires to rails)
    }
    private PowerSupplyMode _powerMode = PowerSupplyMode.OffBoard;

    // TODO: Future network type support (for 1960 Widrow-Hoff, 1986 Backprop)
    // Currently implements: 1958 Perceptron (inverting summing amplifier)
    // Future: Add network type parameter and generate appropriate schematics/breadboards

    // Component tracking for click selection
    private List<ComponentRegion> _componentRegions = new();
    private ComponentRegion? _selectedComponent = null;  // Changed from hover to click selection

    // Breadboard geometry tracking for grid coordinate conversion
    private int _bbLeft = 0;
    private int _bbTop = 0;

    // Breadboard geometry - ACCURATE for real circuit building
    private int _holeSpacing = 20;  // Larger spacing for detailed view
    private int _holeSize = 6;      // Larger holes for visibility
    private int _detailInputCount = 4;  // Show 4 inputs in detail (scalable to 6)

    // Breadboard physical dimensions (CRITICAL - must be accurate)
    private int _boardMarginTop = 50;     // Space above main component area
    private int _boardMarginBottom = 50;  // Space below main component area


    // Colors
    private static readonly Color BreadboardColor = Color.FromArgb(245, 245, 235);
    private static readonly Color HoleColor = Color.FromArgb(40, 40, 40);
    private static readonly Color PowerRailRed = Color.FromArgb(200, 60, 60);
    private static readonly Color PowerRailBlue = Color.FromArgb(60, 60, 200);
    private static readonly Color WireRed = Color.FromArgb(220, 50, 50);
    private static readonly Color WireBlack = Color.FromArgb(30, 30, 30);
    private static readonly Color WireYellow = Color.FromArgb(220, 200, 50);
    private static readonly Color WireGreen = Color.FromArgb(50, 180, 50);
    private static readonly Color WireBlue = Color.FromArgb(50, 100, 220);
    private static readonly Color WireOrange = Color.FromArgb(240, 140, 40);

    // Resistor color band colors
    private static readonly Color[] BandColors = {
        Color.Black, Color.Brown, Color.Red, Color.Orange, Color.Yellow,
        Color.Green, Color.Blue, Color.Purple, Color.Gray, Color.White
    };
    private static readonly Color GoldColor = Color.FromArgb(212, 175, 55);
    private static readonly Color SilverColor = Color.FromArgb(192, 192, 192);

    public POCBreadboardDialog(int[] inputs, double[] weights, double bias, bool[] switchStates,
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
        Text = "Breadboard Layout";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(20, 20, 20);
        DoubleBuffered = true;
        ShowInTaskbar = false;

        int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        int dialogWidth = Math.Max(1100, 500 + nodeCount * 50);
        int dialogHeight = Math.Max(850, 550 + nodeCount * 10);
        Size = new Size(Math.Min(dialogWidth, 1500), Math.Min(dialogHeight, 1000));
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
            Text = "BREADBOARD LAYOUT",
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

        // Detail panel (shows component info on hover) - use double buffering
        _detailPanel = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(50, 50, 45),
            Location = new Point(ClientSize.Width - 275, contentTop),
            Size = new Size(260, ClientSize.Height - contentTop - 50),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = true
        };
        _detailPanel.Paint += DetailPanel_Paint;

        // Main breadboard panel - use double buffering to prevent flicker
        _breadboardPanel = new DoubleBufferedPanel
        {
            BackColor = Color.FromArgb(60, 60, 55),
            Location = new Point(15, contentTop),
            Size = new Size(ClientSize.Width - 310, ClientSize.Height - contentTop - 50),
            BorderStyle = BorderStyle.FixedSingle
        };
        _breadboardPanel.Paint += BreadboardPanel_Paint;
        _breadboardPanel.MouseClick += BreadboardPanel_MouseClick;  // CLICK to select, not hover

        _saveButton = new Button
        {
            Text = "Save Image",
            Size = new Size(100, 30),
            Location = new Point(ClientSize.Width - 120, ClientSize.Height - 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        _saveButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _saveButton.Click += SaveButton_Click;

        // BOM button
        _bomButton = new Button
        {
            Text = "Bill of Materials",
            Size = new Size(110, 30),
            Location = new Point(15, ClientSize.Height - 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(80, 100, 120),
            ForeColor = Color.FromArgb(220, 220, 220)
        };
        _bomButton.FlatAppearance.BorderColor = Color.FromArgb(100, 120, 140);
        _bomButton.Click += BOMButton_Click;

        // Power supply selector
        var powerLabel = new Label
        {
            Text = "Power Supply:",
            AutoSize = true,
            ForeColor = Color.FromArgb(200, 200, 200),
            Location = new Point(250, ClientSize.Height - 35)
        };

        _powerSupplySelector = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(200, 25),
            Location = new Point(340, ClientSize.Height - 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(70, 70, 70),
            ForeColor = Color.FromArgb(220, 220, 220)
        };
        _powerSupplySelector.Items.AddRange(new object[]
        {
            "Off-board (9V battery + 7805)",
            "On-board (battery + regulator on breadboard)",
            "USB 5V direct (no regulator)",
            "Bench supply (wire to rails only)"
        });
        _powerSupplySelector.SelectedIndex = 0;  // Default to off-board
        _powerSupplySelector.SelectedIndexChanged += PowerSupplySelector_Changed;

        // Debug toggle
        _debugToggle = new CheckBox
        {
            Text = "Debug",
            AutoSize = true,
            ForeColor = Color.FromArgb(255, 220, 100),
            Location = new Point(560, ClientSize.Height - 35),
            Checked = false
        };
        _debugToggle.CheckedChanged += DebugToggle_Changed;

        Controls.Add(_breadboardPanel);
        Controls.Add(_detailPanel);
        Controls.Add(_saveButton);
        Controls.Add(_bomButton);
        Controls.Add(powerLabel);
        Controls.Add(_powerSupplySelector);
        Controls.Add(_debugToggle);
    }

    private void DebugToggle_Changed(object? sender, EventArgs e)
    {
        _debugMode = _debugToggle.Checked;
        _breadboardPanel.Invalidate();  // Redraw with/without debug visuals
    }

    private void PowerSupplySelector_Changed(object? sender, EventArgs e)
    {
        // Update power mode and re-render breadboard
        _powerMode = (PowerSupplyMode)_powerSupplySelector.SelectedIndex;
        _breadboardPanel.Invalidate();  // Trigger repaint
    }

    private void BOMButton_Click(object? sender, EventArgs e)
    {
        int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        using var bomDialog = new BOMDialog(_weights, nodeCount, _mathRule);
        bomDialog.ShowDialog(this);
    }

    private void BreadboardPanel_MouseClick(object? sender, MouseEventArgs e)
    {
        // Hit test: Find which component was clicked
        var clickedComponent = _componentRegions.FirstOrDefault(r => r.Bounds.Contains(e.Location));

        if (clickedComponent != null)
        {
            var oldSelected = _selectedComponent;
            _selectedComponent = clickedComponent;

            // DEBUG: Output to teletype
            DebugLogger.Log("CLICK", "=== COMPONENT CLICKED ===");
            DebugLogger.Log("CLICK", $"Component: {clickedComponent.Name}");
            DebugLogger.Log("CLICK", $"Type: {clickedComponent.Type}");
            DebugLogger.Log("CLICK", $"Grid Position: {clickedComponent.GridPosition}");
            DebugLogger.Log("CLICK", $"Pixel Position: X={clickedComponent.Bounds.X}, Y={clickedComponent.Bounds.Y}");
            DebugLogger.Log("CLICK", $"Size: W={clickedComponent.Bounds.Width}, H={clickedComponent.Bounds.Height}");
            DebugLogger.Log("CLICK", $"Has WirePaths: {clickedComponent.WirePaths != null}");
            if (clickedComponent.WirePaths != null)
            {
                DebugLogger.Log("CLICK", $"WirePath count: {clickedComponent.WirePaths.Count}");
                for (int i = 0; i < clickedComponent.WirePaths.Count; i++)
                {
                    DebugLogger.Log("CLICK", $"  Path {i}: {clickedComponent.WirePaths[i].Length} points");
                    if (clickedComponent.WirePaths[i].Length > 0)
                    {
                        var firstPt = clickedComponent.WirePaths[i][0];
                        var lastPt = clickedComponent.WirePaths[i][clickedComponent.WirePaths[i].Length - 1];
                        string gridStart = GetGridPosition(firstPt);
                        string gridEnd = GetGridPosition(lastPt);
                        DebugLogger.Log("CLICK", $"    From {gridStart} ({firstPt.X},{firstPt.Y}) to {gridEnd} ({lastPt.X},{lastPt.Y})");
                    }
                }
            }

            // Invalidate detail panel to show new selection
            _detailPanel.Invalidate();

            // FIXED: Invalidate entire breadboard panel to show wire path highlighting
            // (Wire paths extend far beyond component bounds, so we need to repaint everything)
            _breadboardPanel.Invalidate();
        }
    }

    private void DetailPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var titleFont = new Font("Arial", 10f, FontStyle.Bold);
        using var labelFont = new Font("Arial", 8f);
        using var smallFont = new Font("Arial", 7f);
        using var whiteBrush = new SolidBrush(Color.White);
        using var grayBrush = new SolidBrush(Color.LightGray);
        using var yellowBrush = new SolidBrush(Color.FromArgb(255, 220, 100));

        int panelWidth = _detailPanel.ClientSize.Width;
        int y = 10;

        g.DrawString("COMPONENT DETAIL", titleFont, yellowBrush, 10, y);
        y += 25;

        if (_selectedComponent == null)
        {
            g.DrawString("Click on a component", labelFont, grayBrush, 10, y);
            g.DrawString("to see details.", labelFont, grayBrush, 10, y + 15);
            y += 45;

            // Draw default info
            g.DrawString("Power Requirements:", labelFont, whiteBrush, 10, y);
            y += 18;
            g.DrawString("  +5V DC @ 50mA typical", smallFont, grayBrush, 10, y);
            y += 15;
            g.DrawString($"  9V battery + {PartsDatabase.VoltageRegulator5V.PartNumber}", smallFont, grayBrush, 10, y);
            return;
        }

        var comp = _selectedComponent;

        // Component name
        g.DrawString(comp.Name, titleFont, whiteBrush, 10, y);
        y += 22;

        // Grid position
        if (!string.IsNullOrEmpty(comp.GridPosition))
        {
            g.DrawString($"Position: {comp.GridPosition}", labelFont, yellowBrush, 10, y);
            y += 18;
        }

        // Subsystem
        if (!string.IsNullOrEmpty(comp.Subsystem))
        {
            g.DrawString($"Subsystem: {comp.Subsystem}", labelFont, new SolidBrush(Color.FromArgb(150, 200, 255)), 10, y);
            y += 18;
        }

        // Contextual description (what it does in THIS circuit)
        if (!string.IsNullOrEmpty(comp.ContextualDescription))
        {
            g.DrawString("FUNCTION IN CIRCUIT:", labelFont, yellowBrush, 10, y);
            y += 16;

            // Word wrap the description
            var descLines = WrapText(comp.ContextualDescription, panelWidth - 25, smallFont, g);
            foreach (var line in descLines)
            {
                g.DrawString(line, smallFont, grayBrush, 10, y);
                y += 13;
            }
            y += 8;
        }

        // Draw component image
        y = DrawComponentImage(g, 10, y, panelWidth - 20, comp);
        y += 15;

        // Specifications
        g.DrawString("SPECIFICATIONS:", labelFont, yellowBrush, 10, y);
        y += 18;

        foreach (var spec in comp.Specs)
        {
            g.DrawString($"  {spec}", smallFont, grayBrush, 10, y);
            y += 14;
        }

        // Purchase info
        if (!string.IsNullOrEmpty(comp.PartNumber))
        {
            y += 8;
            g.DrawString("WHERE TO BUY:", labelFont, yellowBrush, 10, y);
            y += 16;
            g.DrawString($"Part: {comp.PartNumber}", smallFont, whiteBrush, 10, y);
            y += 14;

            // Add retailer suggestions based on component type
            string[] retailers = GetRetailers(comp.Type);
            foreach (var retailer in retailers)
            {
                g.DrawString($"  {retailer}", smallFont, grayBrush, 10, y);
                y += 12;
            }
        }
    }

    private string[] GetRetailers(ComponentType type)
    {
        return type switch
        {
            ComponentType.Resistor => new[] { $"{PartsDatabase.Resistor10k.Supplier}", $"~${PartsDatabase.Resistor10k.UnitPrice:F2} each or kit ~$12" },
            ComponentType.LED => new[] { $"{PartsDatabase.LEDGreen3mm.Supplier}", $"~${PartsDatabase.LEDGreen3mm.UnitPrice:F2} each or kit ~$8" },
            ComponentType.OpAmp => new[] { $"DigiKey: {PartsDatabase.OpAmpLM358.PartNumber}", $"~${PartsDatabase.OpAmpLM358.UnitPrice:F2} each" },
            ComponentType.Switch => new[] { $"{PartsDatabase.SwitchSPDT.Supplier}", $"Part: {PartsDatabase.SwitchSPDT.PartNumber}", $"~${PartsDatabase.SwitchSPDT.UnitPrice:F2} each" },
            ComponentType.Battery => new[] { $"{PartsDatabase.Battery9V.Manufacturer} {PartsDatabase.Battery9V.PartNumber}", $"~${PartsDatabase.Battery9V.UnitPrice:F2} each" },
            ComponentType.VoltageRegulator => new[] { $"DigiKey: {PartsDatabase.VoltageRegulator5V.PartNumber}", $"~${PartsDatabase.VoltageRegulator5V.UnitPrice:F2} each" },
            ComponentType.Capacitor => new[] { $"{PartsDatabase.CapCeramic100nF.Supplier}", $"~${PartsDatabase.CapCeramic100nF.UnitPrice:F2} each" },
            ComponentType.Breadboard => new[] { $"{PartsDatabase.Breadboard830.PartNumber}", $"{PartsDatabase.Breadboard830.Supplier}", $"~${PartsDatabase.Breadboard830.UnitPrice:F2} each" },
            _ => new[] { "DigiKey, Mouser, Amazon" }
        };
    }

    private int DrawComponentImage(Graphics g, int x, int y, int width, ComponentRegion comp)
    {
        int imageHeight = 80;

        // Draw component based on type
        switch (comp.Type)
        {
            case ComponentType.Resistor:
                DrawResistorDetail(g, x, y, width, imageHeight, comp.Value);
                break;
            case ComponentType.LED:
                DrawLEDDetail(g, x, y, width, imageHeight, comp.LedColor);
                break;
            case ComponentType.OpAmp:
                DrawOpAmpDetail(g, x, y, width, imageHeight);
                break;
            case ComponentType.Switch:
                DrawSwitchDetail(g, x, y, width, imageHeight);
                break;
            case ComponentType.Battery:
                DrawBatteryDetail(g, x, y, width, imageHeight);
                break;
            case ComponentType.VoltageRegulator:
                DrawRegulatorDetail(g, x, y, width, imageHeight);
                break;
            case ComponentType.Capacitor:
                DrawCapacitorDetail(g, x, y, width, imageHeight, comp.Value);
                break;
            case ComponentType.Breadboard:
                DrawBreadboardDetail(g, x, y, width, imageHeight);
                break;
        }

        return y + imageHeight;
    }

    private void DrawResistorDetail(Graphics g, int x, int y, int width, int height, double ohms)
    {
        // Draw realistic resistor with color bands
        int bodyWidth = 120;
        int bodyHeight = 35;
        int startX = x + (width - bodyWidth - 40) / 2;
        int startY = y + (height - bodyHeight) / 2;

        // Leads
        using var leadPen = new Pen(Color.Silver, 2);
        g.DrawLine(leadPen, startX - 20, startY + bodyHeight / 2, startX, startY + bodyHeight / 2);
        g.DrawLine(leadPen, startX + bodyWidth, startY + bodyHeight / 2, startX + bodyWidth + 20, startY + bodyHeight / 2);

        // Body (tan/beige)
        using var bodyBrush = new SolidBrush(Color.FromArgb(210, 180, 140));
        using var bodyPen = new Pen(Color.FromArgb(150, 120, 80), 1);

        // Rounded ends
        g.FillEllipse(bodyBrush, startX - 5, startY, 20, bodyHeight);
        g.FillEllipse(bodyBrush, startX + bodyWidth - 15, startY, 20, bodyHeight);
        g.FillRectangle(bodyBrush, startX + 5, startY, bodyWidth - 10, bodyHeight);

        g.DrawEllipse(bodyPen, startX - 5, startY, 20, bodyHeight);
        g.DrawEllipse(bodyPen, startX + bodyWidth - 15, startY, 20, bodyHeight);

        // Color bands
        var bands = GetResistorColorBands(ohms);
        int bandWidth = 12;
        int bandSpacing = 18;
        int bandStart = startX + 20;

        for (int i = 0; i < bands.Length; i++)
        {
            int bx = bandStart + i * bandSpacing;
            if (i == bands.Length - 1) bx += 10; // Tolerance band spaced further

            using var bandBrush = new SolidBrush(bands[i]);
            g.FillRectangle(bandBrush, bx, startY + 3, bandWidth, bodyHeight - 6);
            g.DrawRectangle(Pens.Black, bx, startY + 3, bandWidth, bodyHeight - 6);
        }

        // Value label
        using var labelFont = new Font("Arial", 9f, FontStyle.Bold);
        string valueStr = FormatResistance(ohms);
        var size = g.MeasureString(valueStr, labelFont);
        g.DrawString(valueStr, labelFont, Brushes.White, x + (width - size.Width) / 2, y + height - 18);
    }

    private Color[] GetResistorColorBands(double ohms)
    {
        // Calculate 4-band resistor colors
        if (ohms < 1) ohms = 1;
        if (ohms > 99000000) ohms = 99000000;

        int multiplier = 0;
        double value = ohms;

        while (value >= 100)
        {
            value /= 10;
            multiplier++;
        }
        while (value < 10 && multiplier > 0)
        {
            value *= 10;
            multiplier--;
        }

        int firstDigit = (int)(value / 10) % 10;
        int secondDigit = (int)value % 10;

        return new Color[]
        {
            BandColors[Math.Clamp(firstDigit, 0, 9)],
            BandColors[Math.Clamp(secondDigit, 0, 9)],
            BandColors[Math.Clamp(multiplier, 0, 9)],
            GoldColor // 5% tolerance
        };
    }

    private void DrawLEDDetail(Graphics g, int x, int y, int width, int height, Color ledColor)
    {
        int centerX = x + width / 2;
        int centerY = y + height / 2 - 5;

        // LED dome (epoxy body)
        using var domePath = new System.Drawing.Drawing2D.GraphicsPath();
        domePath.AddArc(centerX - 20, centerY - 15, 40, 40, 180, 180);
        domePath.AddLine(centerX - 20, centerY + 5, centerX - 20, centerY + 20);
        domePath.AddLine(centerX - 20, centerY + 20, centerX + 20, centerY + 20);
        domePath.AddLine(centerX + 20, centerY + 20, centerX + 20, centerY + 5);

        using var ledBrush = new System.Drawing.Drawing2D.PathGradientBrush(domePath)
        {
            CenterColor = Color.FromArgb(200, ledColor),
            SurroundColors = new[] { Color.FromArgb(100, ledColor) }
        };
        g.FillPath(ledBrush, domePath);
        g.DrawPath(Pens.Black, domePath);

        // Highlight
        using var highlightBrush = new SolidBrush(Color.FromArgb(100, Color.White));
        g.FillEllipse(highlightBrush, centerX - 8, centerY - 10, 10, 8);

        // Legs
        using var legPen = new Pen(Color.Silver, 2);
        g.DrawLine(legPen, centerX - 8, centerY + 20, centerX - 8, centerY + 40);
        g.DrawLine(legPen, centerX + 8, centerY + 20, centerX + 8, centerY + 35);

        // Flat edge indicator (cathode side)
        g.FillRectangle(Brushes.DarkGray, centerX + 15, centerY, 5, 25);

        // Labels
        using var smallFont = new Font("Arial", 7f);
        g.DrawString("(+) Anode", smallFont, Brushes.White, centerX - 35, centerY + 42);
        g.DrawString("(-) Cathode", smallFont, Brushes.White, centerX + 5, centerY + 35);
    }

    private void DrawOpAmpDetail(Graphics g, int x, int y, int width, int height)
    {
        int chipX = x + (width - 100) / 2;
        int chipY = y + 5;

        // DIP-8 package
        using var bodyBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        g.FillRectangle(bodyBrush, chipX, chipY, 100, 50);
        g.DrawRectangle(Pens.Gray, chipX, chipY, 100, 50);

        // Notch
        g.FillEllipse(Brushes.Gray, chipX + 45, chipY - 3, 10, 8);

        // Pin 1 dot
        g.FillEllipse(Brushes.White, chipX + 8, chipY + 8, 5, 5);

        // Chip markings
        using var markFont = new Font("Arial", 8f, FontStyle.Bold);
        g.DrawString(PartsDatabase.OpAmpLM358.PartNumber, markFont, Brushes.White, chipX + 25, chipY + 18);
        g.DrawString("DUAL OP-AMP", new Font("Arial", 6f), Brushes.Gray, chipX + 15, chipY + 32);

        // Pins
        using var pinPen = new Pen(Color.Silver, 2);
        string[] pinNames = { "OUT1", "IN1-", "IN1+", "GND", "IN2+", "IN2-", "OUT2", "V+" };
        for (int i = 0; i < 4; i++)
        {
            // Left pins
            g.DrawLine(pinPen, chipX - 10, chipY + 8 + i * 12, chipX, chipY + 8 + i * 12);
            // Right pins
            g.DrawLine(pinPen, chipX + 100, chipY + 8 + i * 12, chipX + 110, chipY + 8 + i * 12);
        }

        // Pin numbers
        using var tinyFont = new Font("Arial", 6f);
        for (int i = 0; i < 4; i++)
        {
            g.DrawString($"{i + 1}", tinyFont, Brushes.LightGray, chipX + 3, chipY + 5 + i * 12);
            g.DrawString($"{8 - i}", tinyFont, Brushes.LightGray, chipX + 90, chipY + 5 + i * 12);
        }
    }

    private void DrawSwitchDetail(Graphics g, int x, int y, int width, int height)
    {
        int swX = x + (width - 60) / 2;
        int swY = y + 10;

        // Toggle switch body
        using var bodyBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        g.FillRectangle(bodyBrush, swX, swY, 60, 40);
        g.DrawRectangle(Pens.Black, swX, swY, 60, 40);

        // Toggle lever slot
        g.FillRectangle(Brushes.DarkGray, swX + 20, swY + 5, 20, 30);

        // Toggle lever
        using var leverBrush = new SolidBrush(Color.Silver);
        g.FillRectangle(leverBrush, swX + 23, swY + 8, 14, 12);

        // Terminals
        using var termPen = new Pen(Color.Silver, 3);
        g.DrawLine(termPen, swX + 15, swY + 40, swX + 15, swY + 55);
        g.DrawLine(termPen, swX + 30, swY + 40, swX + 30, swY + 55);
        g.DrawLine(termPen, swX + 45, swY + 40, swX + 45, swY + 55);

        // Labels
        using var smallFont = new Font("Arial", 7f);
        g.DrawString("+V", smallFont, Brushes.Red, swX + 8, swY + 57);
        g.DrawString("OUT", smallFont, Brushes.White, swX + 22, swY + 57);
        g.DrawString("-V", smallFont, Brushes.Blue, swX + 40, swY + 57);
    }

    private void DrawBatteryDetail(Graphics g, int x, int y, int width, int height)
    {
        int batX = x + (width - 80) / 2;
        int batY = y + 10;

        // 9V battery body
        using var bodyBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        g.FillRectangle(bodyBrush, batX, batY, 80, 50);
        g.DrawRectangle(Pens.Gray, batX, batY, 80, 50);

        // Top connector area
        g.FillRectangle(Brushes.DarkGray, batX + 25, batY - 10, 30, 15);

        // Snap connectors
        g.FillEllipse(Brushes.Silver, batX + 30, batY - 8, 10, 10);
        g.FillRectangle(Brushes.Silver, batX + 42, batY - 6, 8, 8);

        // Label
        using var labelFont = new Font("Arial", 12f, FontStyle.Bold);
        g.DrawString("9V", labelFont, Brushes.Yellow, batX + 28, batY + 18);

        // + and - markings
        using var smallFont = new Font("Arial", 8f, FontStyle.Bold);
        g.DrawString("+", smallFont, Brushes.Red, batX + 31, batY + 2);
        g.DrawString("-", smallFont, Brushes.Black, batX + 46, batY + 2);
    }

    private void DrawRegulatorDetail(Graphics g, int x, int y, int width, int height)
    {
        int regX = x + (width - 60) / 2;
        int regY = y + 15;

        // TO-220 package body
        using var bodyBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        using var tabBrush = new SolidBrush(Color.Silver);

        // Metal tab (heatsink)
        g.FillRectangle(tabBrush, regX - 5, regY, 70, 8);
        g.FillEllipse(tabBrush, regX + 25, regY - 2, 10, 12);

        // Plastic body
        g.FillRectangle(bodyBrush, regX, regY + 8, 60, 35);
        g.DrawRectangle(Pens.Gray, regX, regY + 8, 60, 35);

        // Markings
        using var markFont = new Font("Arial", 8f, FontStyle.Bold);
        g.DrawString(PartsDatabase.VoltageRegulator5V.PartNumber, markFont, Brushes.White, regX + 5, regY + 15);
        g.DrawString("+5V REG", new Font("Arial", 6f), Brushes.Gray, regX + 8, regY + 28);

        // Pins
        using var pinPen = new Pen(Color.Silver, 3);
        g.DrawLine(pinPen, regX + 12, regY + 43, regX + 12, regY + 58);
        g.DrawLine(pinPen, regX + 30, regY + 43, regX + 30, regY + 58);
        g.DrawLine(pinPen, regX + 48, regY + 43, regX + 48, regY + 58);

        // Pin labels
        using var tinyFont = new Font("Arial", 6f);
        g.DrawString("IN", tinyFont, Brushes.White, regX + 7, regY + 60);
        g.DrawString("GND", tinyFont, Brushes.White, regX + 22, regY + 60);
        g.DrawString("OUT", tinyFont, Brushes.White, regX + 42, regY + 60);
    }

    private void DrawCapacitorDetail(Graphics g, int x, int y, int width, int height, double value)
    {
        int capX = x + (width - 50) / 2;
        int capY = y + 10;

        // Electrolytic capacitor (cylindrical)
        using var bodyBrush = new SolidBrush(Color.FromArgb(20, 20, 80));
        using var stripeBrush = new SolidBrush(Color.FromArgb(180, 180, 200));

        // Body
        g.FillEllipse(bodyBrush, capX, capY, 50, 20);
        g.FillRectangle(bodyBrush, capX, capY + 10, 50, 35);
        g.FillEllipse(bodyBrush, capX, capY + 35, 50, 20);

        // Negative stripe
        g.FillRectangle(stripeBrush, capX + 40, capY + 5, 8, 45);

        // Minus signs on stripe
        using var minusFont = new Font("Arial", 6f);
        g.DrawString("-", minusFont, Brushes.Black, capX + 42, capY + 15);
        g.DrawString("-", minusFont, Brushes.Black, capX + 42, capY + 30);

        // Top marking (+ side)
        g.DrawString("+", new Font("Arial", 10f, FontStyle.Bold), Brushes.Red, capX + 5, capY - 2);

        // Value
        using var labelFont = new Font("Arial", 7f);
        string valStr = value >= 1 ? $"{value:0}uF" : $"{value * 1000:0}nF";
        g.DrawString(valStr, labelFont, Brushes.White, capX + 8, capY + 22);

        // Leads
        using var leadPen = new Pen(Color.Silver, 2);
        g.DrawLine(leadPen, capX + 15, capY + 55, capX + 15, capY + 70);
        g.DrawLine(leadPen, capX + 35, capY + 55, capX + 35, capY + 70);
    }

    private void DrawBreadboardDetail(Graphics g, int x, int y, int width, int height)
    {
        int bbX = x + 10;
        int bbY = y + 5;
        int bbW = width - 20;
        int bbH = height - 10;

        // Breadboard body
        using var bbBrush = new SolidBrush(BreadboardColor);
        g.FillRectangle(bbBrush, bbX, bbY, bbW, bbH);
        g.DrawRectangle(Pens.Gray, bbX, bbY, bbW, bbH);

        // Power rails
        using var redPen = new Pen(PowerRailRed, 2);
        using var bluePen = new Pen(PowerRailBlue, 2);
        g.DrawLine(redPen, bbX + 5, bbY + 8, bbX + bbW - 5, bbY + 8);
        g.DrawLine(bluePen, bbX + 5, bbY + 15, bbX + bbW - 5, bbY + 15);

        // Center channel
        using var channelBrush = new SolidBrush(Color.FromArgb(200, 200, 190));
        g.FillRectangle(channelBrush, bbX + 5, bbY + bbH / 2 - 2, bbW - 10, 4);

        // Holes (simplified)
        using var holeBrush = new SolidBrush(HoleColor);
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 15; col++)
            {
                g.FillEllipse(holeBrush, bbX + 10 + col * 14, bbY + 22 + row * 8, 3, 3);
                g.FillEllipse(holeBrush, bbX + 10 + col * 14, bbY + bbH - 35 + row * 8, 3, 3);
            }
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save Breadboard Layout",
            Filter = "PNG Image (*.png)|*.png|All Files (*.*)|*.*",
            DefaultExt = "png",
            FileName = "perceptron_breadboard"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            using var bitmap = new Bitmap(_breadboardPanel.Width, _breadboardPanel.Height);
            _breadboardPanel.DrawToBitmap(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            MessageBox.Show("Breadboard layout saved successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BreadboardPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        _componentRegions.Clear();

        int nodeCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        int panelWidth = _breadboardPanel.ClientSize.Width;
        int panelHeight = _breadboardPanel.ClientSize.Height;

        using var titleFont = new Font("Arial", 12f, FontStyle.Bold);
        using var labelFont = new Font("Arial", 8f);
        using var smallFont = new Font("Arial", 7f);
        using var tinyFont = new Font("Arial", 6f);
        using var textBrush = new SolidBrush(Color.White);

        // Title
        g.DrawString("PERCEPTRON - BREADBOARD BUILD GUIDE", titleFont, textBrush, 15, 8);

        // Layout - maximize breadboard size
        int bbLeft = 20;
        int bbTop = 45;
        int bbWidth = panelWidth - 40;
        int bbHeight = panelHeight - 120;  // More space for breadboard

        // Store for grid coordinate conversion
        _bbLeft = bbLeft;
        _bbTop = bbTop;

        // Click instruction
        g.DrawString("Click on components for details", smallFont, Brushes.Gray, 15, 28);

        // Draw main breadboard
        DrawBreadboard(g, bbLeft, bbTop, bbWidth, bbHeight);
        // NOTE: Breadboard itself NOT registered - it would capture all clicks
        // Only individual components (switches, resistors, ICs, LEDs) are clickable

        // Power supply section (conditional based on user selection)
        if (_powerMode == PowerSupplyMode.OnBoard_Battery)
        {
            // TODO: Draw battery + regulator with hole-based placement
            // For now, show the schematic overlay
            DrawPowerSupplySection(g, bbLeft, bbTop, bbWidth, smallFont, tinyFont);
        }
        else if (_powerMode == PowerSupplyMode.OffBoard)
        {
            // Off-board power - just show a note
            g.DrawString("Power Supply: Off-board (9V battery + LM7805)",
                smallFont, Brushes.Gray, bbLeft + 5, bbTop + 5);
        }
        else if (_powerMode == PowerSupplyMode.OnBoard_USB)
        {
            g.DrawString("Power Supply: USB 5V direct",
                smallFont, Brushes.Gray, bbLeft + 5, bbTop + 5);
        }
        else if (_powerMode == PowerSupplyMode.OnBoard_BenchSupply)
        {
            g.DrawString("Power Supply: Bench supply (connect +5V and GND to rails)",
                smallFont, Brushes.Gray, bbLeft + 5, bbTop + 5);
        }

        // === CIRCUIT TOPOLOGY SELECTION ===
        // Draw breadboard layout based on selected math rule
        switch (_mathRule)
        {
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.BACKPROP:
                DrawBackpropBreadboard(g, bbLeft, bbTop, bbWidth, bbHeight, nodeCount, smallFont, tinyFont);
                break;

            // All 1958/1960 variants use the same inverting summing amplifier topology
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.PERCEPTRON_CLASSIC:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.WIDROW_HOFF:
            default:
                Draw1958Breadboard(g, bbLeft, bbTop, bbWidth, bbHeight, nodeCount, smallFont, tinyFont);
                break;
        }

        // Decoupling capacitors (TODO: Convert to hole-based placement)
        // DrawDecouplingCapacitors(g, opAmpX, opAmpY + 80, tinyFont);

        // Parts list at bottom
        DrawPartsListCompact(g, bbLeft, bbTop + bbHeight + 5, bbWidth, nodeCount, labelFont, smallFont);

        // DEBUG MODE: Show hit regions and coordinates
        if (_debugMode)
        {
            using var debugPen = new Pen(Color.FromArgb(120, Color.Cyan), 2);
            using var debugFont = new Font("Courier New", 6f);
            using var debugBrush = new SolidBrush(Color.FromArgb(200, Color.Blue));

            foreach (var region in _componentRegions)
            {
                // Draw hit region rectangle
                g.DrawRectangle(debugPen, region.Bounds);

                // Draw coordinate label
                string coords = $"({region.Bounds.X},{region.Bounds.Y}) {region.Bounds.Width}×{region.Bounds.Height}";
                g.DrawString(coords, debugFont, debugBrush, region.Bounds.X, region.Bounds.Y - 10);
            }
        }

        // Highlight selected component
        if (_selectedComponent != null)
        {
            DebugLogger.Log("PAINT", "=== Drawing highlights ===");
            DebugLogger.Log("PAINT", $"Selected: {_selectedComponent.Name}");

            using var highlightPen = new Pen(Color.Yellow, 3);
            g.DrawRectangle(highlightPen, _selectedComponent.Bounds);

            // Highlight wire paths for selected component
            if (_selectedComponent.WirePaths != null && _selectedComponent.WirePaths.Count > 0)
            {
                DebugLogger.Log("PAINT", $"Drawing {_selectedComponent.WirePaths.Count} wire paths");

                // Draw glowing highlight path
                using var glowPen = new Pen(Color.FromArgb(150, 255, 255, 100), 10);  // Semi-transparent yellow glow
                using var pathPen = new Pen(Color.FromArgb(255, 255, 220, 80), 6);   // Bright yellow core

                foreach (var path in _selectedComponent.WirePaths)
                {
                    if (path.Length >= 2)
                    {
                        DebugLogger.Log("PAINT", $"  Path: {path.Length} points from ({path[0].X},{path[0].Y}) to ({path[path.Length-1].X},{path[path.Length-1].Y})");
                        // Draw glow first (wider, behind)
                        g.DrawLines(glowPen, path);
                        // Draw core on top (narrower, brighter)
                        g.DrawLines(pathPen, path);
                    }
                }
            }
            else
            {
                DebugLogger.Log("PAINT", "No wire paths (WirePaths null or empty)");
            }
        }
    }

    private void Draw1958Breadboard(Graphics g, int bbLeft, int bbTop, int bbWidth, int bbHeight, int nodeCount,
        Font smallFont, Font tinyFont)
    {
        // === HOLE-BASED COMPONENT PLACEMENT ===
        // Show DETAILED view of 3-6 inputs (scalable, buildable example)

        int sumBusCol = 25;  // Column 25 for summing bus
        int showInputs = Math.Min(_detailInputCount, nodeCount);  // Show 4 inputs in detail
        int startRow = 3;  // Start at row D (gives room for power connections above)
        int rowSpacing = 4;  // Each input occupies 4 rows (more spacing for clarity)

        // Draw input channels with proper hole placement and ALL required connections
        for (int i = 0; i < showInputs; i++)
        {
            int row = startRow + (i * rowSpacing);
            bool isOn = i < _switchStates.Length && _switchStates[i];
            double weight = i < _weights.Length ? _weights[i] : 0;

            DrawInputChannelHoleBased(g, bbLeft, bbTop, bbHeight, i + 1, row, isOn, weight, sumBusCol, smallFont);
        }

        // Note about additional inputs
        if (nodeCount > showInputs)
        {
            using var darkBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
            Point msgPos = GetHolePos(bbLeft, bbTop, startRow + showInputs * rowSpacing + 2, 5);
            g.DrawString($"... pattern repeats for {nodeCount - showInputs} more inputs",
                smallFont, darkBrush, msgPos.X, msgPos.Y);
            g.DrawString($"(Detailed view shows {showInputs} inputs for clarity)",
                tinyFont, Brushes.Gray, msgPos.X, msgPos.Y + 12);
        }

        // Op-amp placement (straddles center channel)
        int opAmpCenterRow = startRow + (showInputs * rowSpacing) / 2;
        int opAmpCol = 30;  // Column 30 for op-amp (more central, was 42)
        DrawOpAmpHoleBased(g, bbLeft, bbTop, bbHeight, opAmpCenterRow, opAmpCol, smallFont, tinyFont);

        // Summing bus (already defined above at column 25)
        int firstInputRow = startRow;
        int lastInputRow = startRow + ((showInputs - 1) * rowSpacing);
        DrawSummingBusHoleBased(g, bbLeft, bbTop, sumBusCol, firstInputRow, lastInputRow,
            opAmpCenterRow - 1, opAmpCol, smallFont);

        // Output section
        int outputRow = opAmpCenterRow - 2;
        int outputCol = 40;  // Column 40 for output (tighter, was 52)
        DrawOutputSectionHoleBased(g, bbLeft, bbTop, bbHeight, outputRow, outputCol,
            opAmpCenterRow - 2, opAmpCol, smallFont);

        // Decoupling capacitors (TODO: Convert to hole-based placement)
        // DrawDecouplingCapacitors(g, opAmpX, opAmpY + 80, tinyFont);
    }

    private void DrawBackpropBreadboard(Graphics g, int bbLeft, int bbTop, int bbWidth, int bbHeight, int nodeCount,
        Font smallFont, Font tinyFont)
    {
        // Backprop is impractical for analog breadboard implementation
        using var textBrush = new SolidBrush(Color.FromArgb(200, 200, 200));
        using var yellowBrush = new SolidBrush(Color.FromArgb(255, 220, 100));

        int y = bbTop + 20;
        g.DrawString("BACKPROP MODE - SOFTWARE SIMULATION ONLY", smallFont, yellowBrush, bbLeft + 20, y);
        y += 25;

        var notes = new[] {
            "Backpropagation networks are impractical to build in analog hardware:",
            "",
            $"• Requires {nodeCount} op-amps for hidden layer ReLU activation",
            $"• {nodeCount}×{nodeCount} = {nodeCount * nodeCount} weighted connections in first layer",
            "• Hidden weights (W¹) learned internally - can't be set with physical knobs",
            "• Would need programmable resistor arrays or digital potentiometers",
            "• Circuit complexity grows as O(N²) with input count",
            "",
            "RECOMMENDATION:",
            "• Switch to 1958/1960 mode for buildable analog circuit",
            "• Or implement digitally using microcontroller/FPGA",
            "",
            "The original 1958 perceptron was analog because backprop wasn't",
            "invented yet. Modern MLPs are digital for good reason!"
        };

        foreach (var note in notes)
        {
            g.DrawString(note, tinyFont, textBrush, bbLeft + 30, y);
            y += 14;
        }
    }

    private void DrawPowerSupplySection(Graphics g, int x, int y, int width, Font labelFont, Font smallFont)
    {
        // Title - dark text on light breadboard
        using var darkLabelBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        g.DrawString("POWER SUPPLY", labelFont, darkLabelBrush, x + 5, y + 5);

        // 9V Battery
        int batX = x + 10;
        int batY = y + 25;
        DrawBatterySymbol(g, batX, batY);
        RegisterComponent(new Rectangle(batX - 5, batY - 5, 45, 35), "9V Battery",
            ComponentType.Battery, 9, Color.Black,
            new[] { "9V alkaline or rechargeable", "Standard snap connector", "~500mAh capacity",
                "Provides raw power input", "Replace when < 7V" },
            "Duracell 9V / Energizer 9V");

        // Wire from battery to regulator
        using var redPen = new Pen(WireRed, 2);
        using var blackPen = new Pen(WireBlack, 2);
        g.DrawLine(redPen, batX + 40, batY + 8, batX + 60, batY + 8);
        g.DrawLine(blackPen, batX + 40, batY + 22, batX + 60, batY + 22);

        // LM7805 Voltage Regulator
        int regX = batX + 65;
        int regY = batY - 5;
        DrawRegulatorSymbol(g, regX, regY);
        RegisterComponent(new Rectangle(regX - 5, regY - 5, 55, 50), "LM7805 Voltage Regulator",
            ComponentType.VoltageRegulator, 5, Color.Black,
            new[] { "Input: 7-35V DC", "Output: +5V DC regulated", "Max current: 1A", "TO-220 package",
                "Add heatsink if >100mA", "Essential for stable operation" },
            "LM7805CT / L7805CV");

        // Output capacitor
        int capX = regX + 55;
        DrawSmallCapacitor(g, capX, regY + 10, 10);
        RegisterComponent(new Rectangle(capX - 5, regY + 5, 25, 30), "Filter Capacitor (10uF)",
            ComponentType.Capacitor, 10, Color.Blue,
            new[] { "10uF electrolytic", "16V or higher rating", "Smooths regulator output",
                "Observe polarity (+/-)", "Place close to regulator" },
            "10uF 16V Electrolytic");

        // Wire to power rails
        g.DrawLine(redPen, capX + 20, regY + 15, x + width - 20, regY + 15);
        using var darkRedBrush = new SolidBrush(Color.FromArgb(150, 30, 30));
        g.DrawString("+5V to rail", smallFont, darkRedBrush, capX + 30, regY + 2);

        g.DrawLine(blackPen, regX + 25, regY + 40, regX + 25, y + 70);
        g.DrawLine(blackPen, regX + 25, y + 70, x + width - 20, y + 70);
        using var darkBlueBrush = new SolidBrush(Color.FromArgb(30, 30, 120));
        g.DrawString("GND to rail", smallFont, darkBlueBrush, capX + 30, y + 58);

        // Power rail connections shown
        using var railPen = new Pen(Color.FromArgb(150, 255, 0, 0), 3);
        g.DrawLine(railPen, x + width - 25, regY + 15, x + width - 25, y + 20);
        using var gndPen = new Pen(Color.FromArgb(150, 0, 0, 255), 3);
        g.DrawLine(gndPen, x + width - 25, y + 70, x + width - 25, y + 35);
    }

    private void DrawBatterySymbol(Graphics g, int x, int y)
    {
        using var pen = new Pen(Color.Black, 2);

        // Battery symbol
        g.DrawLine(pen, x, y + 15, x + 10, y + 15);
        g.DrawLine(pen, x + 10, y + 5, x + 10, y + 25);
        g.DrawLine(pen, x + 15, y + 10, x + 15, y + 20);
        g.DrawLine(pen, x + 20, y + 5, x + 20, y + 25);
        g.DrawLine(pen, x + 25, y + 10, x + 25, y + 20);
        g.DrawLine(pen, x + 25, y + 15, x + 40, y + 15);

        // Labels
        using var tinyFont = new Font("Arial", 6f);
        g.DrawString("+", tinyFont, Brushes.Red, x + 8, y - 2);
        g.DrawString("-", tinyFont, Brushes.Blue, x + 25, y + 26);
        g.DrawString("9V", tinyFont, Brushes.Black, x + 12, y + 8);
    }

    private void DrawRegulatorSymbol(Graphics g, int x, int y)
    {
        // TO-220 package representation
        using var bodyBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        g.FillRectangle(bodyBrush, x, y + 8, 45, 25);
        g.DrawRectangle(Pens.Gray, x, y + 8, 45, 25);

        // Tab
        g.FillRectangle(Brushes.Silver, x, y, 45, 10);

        // Label
        using var tinyFont = new Font("Arial", 6f);
        g.DrawString("7805", tinyFont, Brushes.White, x + 10, y + 15);

        // Pins
        using var pinPen = new Pen(Color.Silver, 2);
        g.DrawLine(pinPen, x + 10, y + 33, x + 10, y + 42);
        g.DrawLine(pinPen, x + 22, y + 33, x + 22, y + 42);
        g.DrawLine(pinPen, x + 35, y + 33, x + 35, y + 42);

        // Pin labels
        g.DrawString("IN", tinyFont, Brushes.Red, x + 5, y + 43);
        g.DrawString("G", tinyFont, Brushes.Gray, x + 19, y + 43);
        g.DrawString("OUT", tinyFont, Brushes.Green, x + 28, y + 43);
    }

    private void DrawSmallCapacitor(Graphics g, int x, int y, double value)
    {
        using var bodyBrush = new SolidBrush(Color.FromArgb(40, 40, 100));
        g.FillEllipse(bodyBrush, x, y, 15, 20);
        g.DrawEllipse(Pens.Gray, x, y, 15, 20);

        // Stripe
        g.FillRectangle(Brushes.LightGray, x + 11, y + 2, 3, 16);

        using var tinyFont = new Font("Arial", 5f);
        g.DrawString("+", tinyFont, Brushes.Red, x + 2, y - 2);
    }

    private void DrawBreadboard(Graphics g, int x, int y, int width, int height)
    {
        // Scalable breadboard - sized for detailed component view
        int holeSpacing = _holeSpacing;  // Use instance variable
        int holeSize = _holeSize;        // Use instance variable
        int margin = 20;

        // Main breadboard body - tan/cream color
        using var bbBrush = new SolidBrush(Color.FromArgb(230, 215, 190));
        g.FillRectangle(bbBrush, x, y, width, height);

        using var borderPen = new Pen(Color.FromArgb(120, 110, 90), 2);
        g.DrawRectangle(borderPen, x, y, width, height);

        using var holeBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        using var tinyFont = new Font("Arial", 7f);
        using var labelBrush = new SolidBrush(Color.FromArgb(80, 80, 80));

        // Calculate grid based on available space (scalable for detail view)
        int numCols = Math.Min(63, (width - margin * 2) / holeSpacing);  // Standard breadboard = 63 columns

        // === TOP POWER RAILS ===
        int topRailY = y + 15;
        using var redStripeBrush = new SolidBrush(Color.FromArgb(255, 220, 220));
        using var blueStripeBrush = new SolidBrush(Color.FromArgb(220, 220, 255));

        // +5V rail (red)
        g.FillRectangle(redStripeBrush, x + margin, topRailY, width - margin * 2, 6);
        using var redPen = new Pen(Color.FromArgb(200, 60, 60), 1);
        g.DrawLine(redPen, x + margin, topRailY, x + width - margin, topRailY);
        g.DrawString("+5V", tinyFont, new SolidBrush(Color.FromArgb(180, 40, 40)), x + 5, topRailY - 2);

        // GND rail (blue)
        g.FillRectangle(blueStripeBrush, x + margin, topRailY + 10, width - margin * 2, 6);
        using var bluePen = new Pen(Color.FromArgb(60, 60, 200), 1);
        g.DrawLine(bluePen, x + margin, topRailY + 10, x + width - margin, topRailY + 10);
        g.DrawString("GND", tinyFont, new SolidBrush(Color.FromArgb(40, 40, 180)), x + 5, topRailY + 8);

        // Rail holes in clusters of 5 with 1-space gaps (dense, realistic power distribution)
        int clusterSpacing = 1; // Minimal gap between clusters for better power access
        for (int cluster = 0; cluster * (5 + clusterSpacing) < numCols; cluster++)
        {
            int clusterStartCol = cluster * (5 + clusterSpacing);
            // Draw 5 holes in this cluster
            for (int i = 0; i < 5 && (clusterStartCol + i) < numCols; i++)
            {
                int hx = x + margin + (clusterStartCol + i) * holeSpacing;
                g.FillEllipse(holeBrush, hx, topRailY + 1, holeSize, holeSize);
                g.FillEllipse(holeBrush, hx, topRailY + 11, holeSize, holeSize);
            }
        }

        // === MAIN COMPONENT AREA ===
        int mainAreaY = topRailY + 30;
        int mainAreaHeight = height - 80;

        // Center channel for IC straddling
        int centerY = mainAreaY + mainAreaHeight / 2;
        using var channelBrush = new SolidBrush(Color.FromArgb(200, 190, 170));
        g.FillRectangle(channelBrush, x + margin - 5, centerY - 5, width - margin * 2 + 10, 10);

        // Draw hole grid (component area)
        int rowsAbove = (centerY - mainAreaY - 10) / holeSpacing + 1; // +1 for additional row before center channel
        int rowsBelow = (mainAreaY + mainAreaHeight - centerY - 10) / holeSpacing + 1; // +1 for additional row below Z

        // Holes above center channel
        for (int row = 0; row < rowsAbove; row++)
        {
            int hy = mainAreaY + row * holeSpacing;
            for (int col = 0; col < numCols; col++)
            {
                int hx = x + margin + col * holeSpacing;
                g.FillEllipse(holeBrush, hx, hy, holeSize, holeSize);
            }
        }

        // Holes below center channel
        for (int row = 0; row < rowsBelow; row++)
        {
            int hy = centerY + 10 + row * holeSpacing;
            for (int col = 0; col < numCols; col++)
            {
                int hx = x + margin + col * holeSpacing;
                g.FillEllipse(holeBrush, hx, hy, holeSize, holeSize);
            }
        }

        // Vertical row labels (A, B, C, D, E above center; F, G, H, I, J below)
        using var rowLabelFont = new Font("Arial", 7f, FontStyle.Bold);
        using var rowLabelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

        // Labels for rows above center channel
        for (int row = 0; row < rowsAbove; row++)
        {
            int hy = mainAreaY + row * holeSpacing;
            int rowNum = row;
            string label = rowNum < 26 ? ((char)('A' + rowNum)).ToString() : (rowNum + 1).ToString();
            g.DrawString(label, rowLabelFont, rowLabelBrush, x + 3, hy - 3);
        }

        // Labels for rows below center channel
        for (int row = 0; row < rowsBelow; row++)
        {
            int hy = centerY + 10 + row * holeSpacing;
            int rowNum = rowsAbove + row;
            string label = rowNum < 26 ? ((char)('A' + rowNum)).ToString() : (rowNum + 1).ToString();
            g.DrawString(label, rowLabelFont, rowLabelBrush, x + 3, hy - 3);
        }

        // === BOTTOM POWER RAILS ===
        int bottomRailY = y + height - 25;

        // +5V rail
        g.FillRectangle(redStripeBrush, x + margin, bottomRailY, width - margin * 2, 6);
        g.DrawLine(redPen, x + margin, bottomRailY, x + width - margin, bottomRailY);
        g.DrawString("+5V", tinyFont, new SolidBrush(Color.FromArgb(180, 40, 40)), x + 5, bottomRailY - 2);

        // GND rail
        g.FillRectangle(blueStripeBrush, x + margin, bottomRailY + 10, width - margin * 2, 6);
        g.DrawLine(bluePen, x + margin, bottomRailY + 10, x + width - margin, bottomRailY + 10);
        g.DrawString("GND", tinyFont, new SolidBrush(Color.FromArgb(40, 40, 180)), x + 5, bottomRailY + 8);

        // Bottom rail holes in clusters of 5 (like a real breadboard)
        for (int cluster = 0; cluster * (5 + clusterSpacing) < numCols; cluster++)
        {
            int clusterStartCol = cluster * (5 + clusterSpacing);
            // Draw 5 holes in this cluster
            for (int i = 0; i < 5 && (clusterStartCol + i) < numCols; i++)
            {
                int hx = x + margin + (clusterStartCol + i) * holeSpacing;
                g.FillEllipse(holeBrush, hx, bottomRailY + 1, holeSize, holeSize);
                g.FillEllipse(holeBrush, hx, bottomRailY + 11, holeSize, holeSize);
            }
        }

        // Row numbering (every 5 holes) - makes it easier for makers
        using var rowNumFont = new Font("Arial", 6f, FontStyle.Bold);
        using var rowNumBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        for (int col = 4; col < numCols; col += 5)  // Start at 5 (col index 4)
        {
            int hx = x + margin + col * holeSpacing;
            int rowNum = col + 1;  // Display as 1-based (5, 10, 15, 20...)
            string numText = rowNum.ToString();
            var textSize = g.MeasureString(numText, rowNumFont);

            // Draw number above the component area
            g.DrawString(numText, rowNumFont, rowNumBrush, hx - textSize.Width / 2, mainAreaY - 15);

            // Draw number below the component area (above bottom rails)
            g.DrawString(numText, rowNumFont, rowNumBrush, hx - textSize.Width / 2, bottomRailY - 15);
        }
    }

    #region Component Drawing Helpers (Reusable Graphics)

    private void DrawMiniSwitch(Graphics g, int x, int y, bool isOn)
    {
        // REDESIGNED: Show the 3 pins extending into holes (SPDT = 3 terminals)
        // x, y is the COMMON terminal position (center pin)
        // Plus terminal is above (y - _holeSpacing), GND below (y + _holeSpacing)

        using var bodyBrush = new SolidBrush(Color.FromArgb(70, 70, 70));
        using var toggleBrush = new SolidBrush(isOn ? Color.LightGray : Color.DarkGray);
        using var pinBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
        using var pinPen = new Pen(Color.FromArgb(120, 120, 120), 1.5f);
        using var pinLabelFont = new Font("Arial", 6f, FontStyle.Bold);
        using var pinLabelBrush = new SolidBrush(Color.FromArgb(255, 100, 100));  // Red for switch pins

        // Small switch body (centered between pins)
        int bodyWidth = 22;
        int bodyHeight = _holeSpacing + 6;  // Spans from top to bottom pin
        int bodyX = x - bodyWidth / 2;
        int bodyY = y - _holeSpacing - 3;

        g.FillRectangle(bodyBrush, bodyX, bodyY, bodyWidth, bodyHeight);
        g.DrawRectangle(Pens.Black, bodyX, bodyY, bodyWidth, bodyHeight);

        // Draw toggle position indicator
        int toggleY = isOn ? bodyY + 3 : bodyY + bodyHeight - 9;
        g.FillRectangle(toggleBrush, bodyX + 6, toggleY, 10, 6);

        // Draw THREE PINS extending to holes
        // Pin 1: Plus terminal (top, to +5V rail)
        int pin1Y = y - _holeSpacing;
        g.FillRectangle(pinBrush, bodyX + 4, pin1Y - 1, 6, bodyY - pin1Y + 4);
        g.DrawRectangle(pinPen, bodyX + 4, pin1Y - 1, 6, bodyY - pin1Y + 4);
        g.DrawString("+", pinLabelFont, pinLabelBrush, bodyX - 8, pin1Y - 3);

        // Pin 2: Common terminal (center, output)
        g.FillRectangle(pinBrush, bodyX - 8, y - 1, bodyX - (bodyX - 8), 3);
        g.DrawRectangle(pinPen, bodyX - 8, y - 1, bodyX - (bodyX - 8), 3);
        g.DrawString("C", pinLabelFont, pinLabelBrush, bodyX - 8, y - 10);

        // Pin 3: GND terminal (bottom, to ground rail)
        int pin3Y = y + _holeSpacing;
        g.FillRectangle(pinBrush, bodyX + 12, bodyY + bodyHeight - 2, 6, pin3Y - (bodyY + bodyHeight) + 2);
        g.DrawRectangle(pinPen, bodyX + 12, bodyY + bodyHeight - 2, 6, pin3Y - (bodyY + bodyHeight) + 2);
        g.DrawString("−", pinLabelFont, pinLabelBrush, bodyX - 8, pin3Y - 3);
    }

    private void DrawMiniLED(Graphics g, int x, int y, Color color, bool isOn)
    {
        // REDESIGNED: Show anode and cathode pins extending into holes
        // x, y is the ANODE position (top hole)
        // Cathode is one row below (y + _holeSpacing)

        using var pinBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
        using var pinPen = new Pen(Color.FromArgb(120, 120, 120), 1.5f);
        using var pinLabelFont = new Font("Arial", 6f, FontStyle.Bold);
        using var pinLabelBrush = new SolidBrush(Color.FromArgb(100, 255, 100));  // Green for LED pins

        // LED body (centered between anode and cathode)
        int ledSize = 10;
        int ledX = x - ledSize / 2;
        int ledY = y + _holeSpacing / 2 - ledSize / 2;

        // Glow effect if lit
        if (isOn)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(100, color));
            g.FillEllipse(glowBrush, ledX - 6, ledY - 6, ledSize + 12, ledSize + 12);
        }

        // LED body
        using var ledBrush = new SolidBrush(isOn ? color : Color.FromArgb(60, 60, 60));
        g.FillEllipse(ledBrush, ledX, ledY, ledSize, ledSize);
        g.DrawEllipse(Pens.Black, ledX, ledY, ledSize, ledSize);

        // Anode pin (longer, top) - extends from LED to hole above
        g.FillRectangle(pinBrush, x - 1, y, 3, ledY - y);
        g.DrawRectangle(pinPen, x - 1, y, 3, ledY - y);
        g.DrawString("+", pinLabelFont, pinLabelBrush, x + 4, y - 2);

        // Cathode pin (shorter, bottom) - extends from LED to hole below
        int cathodeY = y + _holeSpacing;
        g.FillRectangle(pinBrush, x - 1, ledY + ledSize, 3, cathodeY - (ledY + ledSize));
        g.DrawRectangle(pinPen, x - 1, ledY + ledSize, 3, cathodeY - (ledY + ledSize));
        g.DrawString("−", pinLabelFont, pinLabelBrush, x + 4, cathodeY - 2);

        // Flat edge on cathode side (polarity indicator)
        using var flatPen = new Pen(Color.Black, 2f);
        g.DrawLine(flatPen, ledX + 2, ledY + ledSize, ledX + ledSize - 2, ledY + ledSize);
    }

    private void DrawMiniResistorWithBands(Graphics g, int x, int y, double ohms, Font font)
    {
        // REALISTIC SIZE: Resistor spans 2 columns (normal component size)
        // Use jumper wires for longer distances
        int resistorWidth = 2 * _holeSpacing;  // 40px for 2 columns (was 80px - too long!)
        int bodyWidth = resistorWidth - 10;    // Leave room for leads
        int leadLength = 5;

        // Body (centered)
        using var bodyBrush = new SolidBrush(Color.FromArgb(210, 180, 140));
        g.FillRectangle(bodyBrush, x + leadLength, y - 5, bodyWidth, 10);

        // Leads (scaled to match hole spacing)
        using var leadPen = new Pen(Color.Silver, 2);
        g.DrawLine(leadPen, x, y, x + leadLength, y);
        g.DrawLine(leadPen, x + leadLength + bodyWidth, y, x + resistorWidth, y);

        // Color bands (scaled to new body width)
        var bands = GetResistorColorBands(ohms);
        int bandWidth = 6;
        int bandSpacing = (bodyWidth - 4 * bandWidth) / 5;  // Evenly spaced
        int[] bandX = {
            x + leadLength + bandSpacing,
            x + leadLength + bandSpacing * 2 + bandWidth,
            x + leadLength + bandSpacing * 3 + bandWidth * 2,
            x + leadLength + bodyWidth - bandWidth - bandSpacing  // Tolerance band at end
        };
        for (int i = 0; i < bands.Length; i++)
        {
            using var bandBrush = new SolidBrush(bands[i]);
            g.FillRectangle(bandBrush, bandX[i], y - 4, bandWidth, 8);
        }
    }

    private void DrawLM358Component(Graphics g, int x, int y, Font labelFont, Font smallFont)
    {
        // REDESIGNED: Pins MUST align with breadboard holes (spacing = _holeSpacing)
        // Priority: ELECTRICAL CORRECTNESS - chip must be insertable into breadboard

        using var pinBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
        using var pinPen = new Pen(Color.FromArgb(120, 120, 120), 1.5f);
        using var pinLabelBrush = new SolidBrush(Color.FromArgb(255, 255, 100));  // Yellow for visibility
        using var pinLabelFont = new Font("Arial", 7f, FontStyle.Bold);

        // Pin spacing MUST match breadboard hole spacing
        int pinSpacing = _holeSpacing;  // 20 pixels = breadboard hole spacing
        int chipHeight = 3 * pinSpacing + 8;  // Span 4 pins (3 gaps + margins)
        int chipWidth = 60;
        int chipX = x + 15;  // Offset for pin leads
        int chipY = y;

        // Draw pins FIRST (must align with breadboard holes!)
        // Left side pins (pins 1-4) - each pin at exact hole position
        string[] leftPinLabels = { "1", "2", "3", "4" };
        for (int i = 0; i < 4; i++)
        {
            int pinY = y + i * pinSpacing;  // FIXED: Use hole spacing, not 12px
            // Thicker, more visible pin lead extending into hole
            g.FillRectangle(pinBrush, x - 2, pinY - 1, chipX - x + 4, 3);
            g.DrawRectangle(pinPen, x - 2, pinY - 1, chipX - x + 4, 3);
        }

        // Right side pins (pins 5-8) - each pin at exact hole position
        string[] rightPinLabels = { "8", "7", "6", "5" };  // Top to bottom
        for (int i = 0; i < 4; i++)
        {
            int pinY = y + i * pinSpacing;  // FIXED: Use hole spacing, not 12px
            int pinX = chipX + chipWidth;
            // Thicker, more visible pin lead extending into hole
            g.FillRectangle(pinBrush, pinX - 2, pinY - 1, (x + 90) - pinX + 4, 3);
            g.DrawRectangle(pinPen, pinX - 2, pinY - 1, (x + 90) - pinX + 4, 3);
        }

        // Chip body (taller to accommodate hole-spaced pins)
        using var bodyBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
        g.FillRectangle(bodyBrush, chipX, chipY - 4, chipWidth, chipHeight);
        g.DrawRectangle(Pens.Gray, chipX, chipY - 4, chipWidth, chipHeight);

        // Notch at top center (pin 1 indicator)
        g.FillEllipse(Brushes.DarkGray, chipX + chipWidth / 2 - 4, chipY - 6, 8, 5);

        // Pin numbers ON the chip body at each pin position
        for (int i = 0; i < 4; i++)
        {
            int pinY = y + i * pinSpacing;
            // Left side pin numbers
            g.DrawString(leftPinLabels[i], pinLabelFont, pinLabelBrush, chipX + 3, pinY - 4);
            // Right side pin numbers
            g.DrawString(rightPinLabels[i], pinLabelFont, pinLabelBrush, chipX + chipWidth - 12, pinY - 4);
        }

        // Part number label (centered)
        using var partFont = new Font("Arial", 6f);
        g.DrawString("LM358N", partFont, Brushes.White, chipX + 15, chipY + chipHeight / 2 - 3);

        // NOTE: Power connections now drawn by DrawOpAmpHoleBased with accurate coordinates
        // Old hardcoded connections removed - they were creating "wires to nowhere"

        RegisterComponent(new Rectangle(x - 15, y - 10, 130, 80), $"{PartsDatabase.OpAmpLM358.PartNumber} {PartsDatabase.OpAmpLM358.Description}",
            ComponentType.OpAmp, 0, Color.Black,
            new[] { PartsDatabase.OpAmpLM358.Description, "Supply: 3-32V single / +/-1.5-16V dual",
                "Pin 1: OUT1, Pin 2: IN1-, Pin 3: IN1+", "Pin 4: GND, Pin 8: V+",
                "Used as summing amplifier", PartsDatabase.OpAmpLM358.Package },
            PartsDatabase.OpAmpLM358.PartNumber);
    }

    // OLD DrawSummingWires REMOVED
    // Replaced by DrawSummingBusHoleBased (hole-based, electrically accurate)

    // OLD DrawOutputSection and DrawDecouplingCapacitors REMOVED
    // DrawOutputSection replaced by DrawOutputSectionHoleBased
    // DrawDecouplingCapacitors commented out, not currently used

    private void DrawMiniGround(Graphics g, int x, int y)
    {
        using var pen = new Pen(Color.Black, 1.5f);
        g.DrawLine(pen, x - 6, y, x + 6, y);
        g.DrawLine(pen, x - 4, y + 3, x + 4, y + 3);
        g.DrawLine(pen, x - 2, y + 6, x + 2, y + 6);
    }

    private void DrawPartsListCompact(Graphics g, int x, int y, int width, int nodeCount,
        Font labelFont, Font smallFont)
    {
        using var headerBrush = new SolidBrush(Color.FromArgb(255, 220, 100));
        using var textBrush = new SolidBrush(Color.White);

        g.DrawString("SHOPPING LIST:", labelFont, headerBrush, x, y);

        string[] items = {
            $"{nodeCount}x {PartsDatabase.SwitchSPDT.Description}",
            $"{nodeCount}x {PartsDatabase.LEDGreen3mm.Description}",
            $"{nodeCount + 2}x Resistors (see values)",
            $"1x {PartsDatabase.OpAmpLM358.PartNumber} Op-Amp",
            $"1x {PartsDatabase.LEDGreen5mm.Description}",
            $"1x {PartsDatabase.Battery9V.Value} + {PartsDatabase.BatterySnap.Description}",
            $"1x {PartsDatabase.VoltageRegulator5V.PartNumber} Regulator",
            $"2x Capacitors ({PartsDatabase.CapCeramic100nF.Value}, {PartsDatabase.CapElectrolytic10uF.Value})",
            $"1x {PartsDatabase.Breadboard830.Description}",
            PartsDatabase.JumperWireKit.Description
        };

        int col = 0;
        int row = 0;
        int colWidth = width / 3;

        foreach (var item in items)
        {
            g.DrawString(item, smallFont, textBrush, x + col * colWidth, y + 18 + row * 13);
            row++;
            if (row > 3) { row = 0; col++; }
        }
    }

    private void RegisterComponent(Rectangle bounds, string name, ComponentType type,
        double value, Color color, string[] specs, string partNumber, List<Point[]>? wirePaths = null,
        string subsystem = "", string contextualDesc = "", int? inputNum = null, double? weight = null)
    {
        // Calculate grid position from center of bounds
        Point centerPos = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        string gridPos = GetGridPosition(centerPos);

        // Generate contextual description if not provided
        if (string.IsNullOrEmpty(contextualDesc))
        {
            contextualDesc = GenerateContextualDescription(type, name, value, inputNum, weight);
        }

        _componentRegions.Add(new ComponentRegion
        {
            Bounds = bounds,
            Name = name,
            Type = type,
            Value = value,
            LedColor = color,
            Specs = specs,
            PartNumber = partNumber,
            WirePaths = wirePaths,
            GridPosition = gridPos,
            Subsystem = subsystem,
            ContextualDescription = contextualDesc
        });
    }

    /// <summary>
    /// Register a wire as a clickable component with start and end grid positions.
    /// </summary>
    private void RegisterWire(Point start, Point end, string name, Color wireColor, string subsystem, string contextualDesc)
    {
        // Create hit region along the wire path (thickened for easier clicking)
        int thickness = 8;  // Hit region thickness
        Rectangle wireBounds = new Rectangle(
            Math.Min(start.X, end.X) - thickness,
            Math.Min(start.Y, end.Y) - thickness,
            Math.Abs(end.X - start.X) + thickness * 2,
            Math.Abs(end.Y - start.Y) + thickness * 2
        );

        string gridPosStart = GetGridPosition(start);
        string gridPosEnd = GetGridPosition(end);
        string gridPos = $"{gridPosStart} to {gridPosEnd}";

        _componentRegions.Add(new ComponentRegion
        {
            Bounds = wireBounds,
            Name = name,
            Type = ComponentType.Wire,
            Value = 0,
            LedColor = wireColor,
            Specs = new[] { $"From: {gridPosStart}", $"To: {gridPosEnd}", $"Wire color: {wireColor.Name}" },
            PartNumber = "22AWG Jumper Wire",
            GridPosition = gridPos,
            Subsystem = subsystem,
            ContextualDescription = contextualDesc
        });
    }

    /// <summary>
    /// Generate contextual description explaining what this component does in THIS circuit.
    /// </summary>
    private string GenerateContextualDescription(ComponentType type, string name, double value, int? inputNum = null, double? weight = null)
    {
        switch (type)
        {
            case ComponentType.Resistor:
                if (name.StartsWith("R") && inputNum.HasValue && weight.HasValue)
                {
                    // Input resistor
                    double resistance = PartsDatabase.CalculateInputResistance(weight.Value);
                    string resStr = FormatResistance(resistance);
                    return $"Sets weight for Input {inputNum}. Value {resStr} calculated from weight {weight.Value:+0.00;-0.00}. " +
                           $"In inverting summing amp: Vout contribution = -Rf/Rin × Vin = -{PartsDatabase.ReferenceResistor}/{resistance:F0} × Vin. " +
                           $"Higher weight → lower resistance → stronger signal.";
                }
                else if (name.StartsWith("Rf"))
                {
                    // Feedback resistor
                    return $"Feedback resistor sets overall gain of summing amplifier. Value: {FormatResistance(PartsDatabase.ReferenceResistor)}. " +
                           $"Output = -Rf × Σ(Vin/Rin). Determines output voltage range and scaling factor.";
                }
                else if (name.Contains("LED"))
                {
                    // LED current limiting resistor
                    return $"Current limiting resistor for output LED. Limits LED current to safe operating range (~20mA). " +
                           $"Value: {FormatResistance(470)}. Prevents LED burnout.";
                }
                break;

            case ComponentType.Switch:
                if (inputNum.HasValue)
                {
                    return $"SPDT switch selects input {inputNum} polarity. " +
                           $"Up position: +1V (positive input). Down position: -1V (negative input). " +
                           $"Allows bipolar input encoding for pattern recognition.";
                }
                break;

            case ComponentType.OpAmp:
                return $"LM358N dual op-amp configured as inverting summing amplifier. " +
                       $"Pin 2 (IN-) receives all weighted inputs through resistors. " +
                       $"Pin 3 (IN+) tied to ground for inverting configuration. " +
                       $"Pin 1 (OUT) produces inverted weighted sum: Vout = -Σ(Vin×Weight) + Bias. " +
                       $"This implements the perceptron's weighted sum calculation in analog hardware.";

            case ComponentType.LED:
                if (name.Contains("Output"))
                {
                    return $"Output indicator LED shows perceptron decision. " +
                           $"ON (lit) = positive output (pattern recognized). " +
                           $"OFF (dark) = negative output (pattern rejected). " +
                           $"Provides visual feedback of network classification.";
                }
                break;

            case ComponentType.Wire:
                return $"Electrical connection routing signals between components. " +
                       $"Follows breadboard hole connectivity for proper electrical continuity.";
        }

        return "Component in circuit.";
    }

    private Color GetWireColor(int index)
    {
        Color[] colors = { WireRed, WireBlue, WireYellow, WireGreen, WireOrange,
            Color.Purple, Color.Cyan, Color.Magenta, Color.Brown, Color.Pink };
        return colors[(index - 1) % colors.Length];
    }

    /// <summary>
    /// Word wrap text to fit within specified width.
    /// </summary>
    private List<string> WrapText(string text, int maxWidth, Font font, Graphics g)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        string currentLine = "";

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var size = g.MeasureString(testLine, font);

            if (size.Width > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    /// <summary>
    /// Convert pixel position to breadboard grid coordinates (row, column).
    /// Returns string like "D-15" for row D, column 15.
    /// </summary>
    private string GetGridPosition(Point pixelPos)
    {
        // Calculate which hole this pixel position is closest to
        int dx = pixelPos.X - _bbLeft - 30;  // Account for board margins
        int dy = pixelPos.Y - _bbTop - _boardMarginTop;

        int col = (dx + _holeSpacing / 2) / _holeSpacing;
        int row = (dy + _holeSpacing / 2) / _holeSpacing;

        // Convert row to letter (A, B, C, ...)
        char rowLetter = (char)('A' + row);

        return $"{rowLetter}-{col}";
    }

    private string FormatResistance(double ohms)
    {
        if (ohms >= 1000000)
            return $"{ohms / 1000000:0.#}M";
        else if (ohms >= 1000)
            return $"{ohms / 1000:0.##}k";
        else
            return $"{ohms:0}R";
    }

    // ACCURATE: Get actual breadboard area dimensions
    private Rectangle GetBoardArea(int bbLeft, int bbTop, int bbWidth, int bbHeight)
    {
        return new Rectangle(
            bbLeft + 20,  // Left margin
            bbTop + _boardMarginTop,  // Top margin (below power rails)
            bbWidth - 40,  // Width (accounting for margins)
            bbHeight - _boardMarginTop - _boardMarginBottom  // Height (between rails)
        );
    }

    // ACCURATE: Get power rail positions WITHIN the breadboard
    private int GetTopPowerRailY(int bbTop)
    {
        return bbTop + 15;  // Fixed position for top rails
    }

    private int GetBottomPowerRailY(int bbTop, int bbHeight)
    {
        return bbTop + bbHeight - 25;  // Fixed position for bottom rails
    }

    // ACCURATE: Get hole position in pixels (within visible breadboard area)
    private Point GetHolePos(int bbLeft, int bbTop, int row, int col)
    {
        int x = bbLeft + 20 + (col - 1) * _holeSpacing;
        int y = bbTop + _boardMarginTop + (row * _holeSpacing);
        return new Point(x, y);
    }

    // ACCURATE: Get power rail hole position
    private Point GetPowerRailHole(int bbLeft, int bbTop, int bbHeight, bool isTop, bool isPlus, int col)
    {
        int x = bbLeft + 20 + (col - 1) * _holeSpacing;
        int y;

        if (isTop)
        {
            y = GetTopPowerRailY(bbTop) + (isPlus ? 1 : 11);  // +5V or GND on top rail
        }
        else
        {
            y = GetBottomPowerRailY(bbTop, bbHeight) + (isPlus ? 1 : 11);  // +5V or GND on bottom rail
        }

        return new Point(x, y);
    }

    #endregion

    #region Hole-Based Circuit Layout (Electrically Accurate)

    // Helper: Draw jumper wire between holes (FIXED - simple straight lines for debugging)
    private void DrawJumperWire(Graphics g, Pen pen, Point from, Point to, int arcHeight = 5, bool drawDots = false)
    {
        // SIMPLIFIED: Just draw straight line for now to debug connections
        // TODO: Add proper arc routing later once connections are verified
        g.DrawLine(pen, from, to);

        // Optional connection dots (only when requested)
        if (drawDots)
        {
            g.FillEllipse(Brushes.Black, from.X - 2, from.Y - 2, 4, 4);
            g.FillEllipse(Brushes.Black, to.X - 2, to.Y - 2, 4, 4);
        }
    }

    // Helper: Draw single connection dot at a point
    private void DrawConnectionDot(Graphics g, Point pos, int size = 4)
    {
        // Draw larger, more visible connection dot with subtle glow
        using var glowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        g.FillEllipse(glowBrush, pos.X - size, pos.Y - size, size * 2, size * 2);
        g.FillEllipse(Brushes.Black, pos.X - size / 2, pos.Y - size / 2, size, size);
    }

    #endregion

    #region 1958 Perceptron Circuit Layout (Inverting Summing Amplifier)

    // Draw input channel with hole-based placement (ELECTRICALLY ACCURATE)
    private void DrawInputChannelHoleBased(Graphics g, int bbLeft, int bbTop, int bbHeight, int inputNum, int row,
        bool isOn, double weight, int sumBusCol, Font smallFont)  // sumBusCol now a PARAMETER
    {
        // Component placement columns (using realistic breadboard spacing)
        int switchCol = 5;      // Column 5: Switch common terminal
        int switchPlusCol = 3;  // Column 3: Switch +5V terminal
        int switchGndCol = 2;   // Column 2: All switches share ground column (realistic ground bus)
        int resistorCol = 15;   // Column 15: Resistor input
        // sumBusCol passed as parameter - no longer hardcoded!

        // Wire colors - THICKER for visibility (3-4px as requested)
        Color wireColor = GetWireColor(inputNum);
        using var wirePen = new Pen(wireColor, 4f);      // Was 1.5f
        using var redPen = new Pen(WireRed, 4f);         // Was 1.5f
        using var blackPen = new Pen(WireBlack, 4f);     // Was 1.5f

        // Calculate hole positions
        Point switchCommonPos = GetHolePos(bbLeft, bbTop, row, switchCol);
        Point switchPlusPos = GetHolePos(bbLeft, bbTop, row - 1, switchPlusCol);  // +5V terminal above
        Point switchGndPos = GetHolePos(bbLeft, bbTop, row + 1, switchGndCol);    // GND terminal below
        Point resistorInPos = GetHolePos(bbLeft, bbTop, row, resistorCol);
        Point resistorOutPos = GetHolePos(bbLeft, bbTop, row, resistorCol + 2);  // 2 columns (was 4)
        Point sumBusPos = GetHolePos(bbLeft, bbTop, row, sumBusCol);

        // REQUIRED: Switch +5V terminal to TOP power rail (ACTUAL visible rail)
        Point plusRail = GetPowerRailHole(bbLeft, bbTop, bbHeight, true, true, switchPlusCol);
        g.DrawLine(redPen, switchPlusPos, plusRail);
        DrawConnectionDot(g, switchPlusPos);

        // REQUIRED: Switch GND terminal to TOP ground rail via shared ground bus
        Point gndRail = GetPowerRailHole(bbLeft, bbTop, bbHeight, true, false, switchGndCol);
        using var thinBlackPen = new Pen(WireBlack, 2f);  // Thinner for ground bus (less visual weight)
        g.DrawLine(thinBlackPen, switchGndPos, gndRail);
        // Note: All switches share column 2 for ground, creating a vertical ground bus (realistic)

        // Draw switch (visual representation at hole position)
        DrawMiniSwitch(g, switchCommonPos.X - 12, switchCommonPos.Y, isOn);
        RegisterComponent(new Rectangle(switchCommonPos.X - 20, switchCommonPos.Y - 15, 50, 35),
            $"Switch {inputNum} (SPDT)", ComponentType.Switch, 0, Color.Gray,
            new[] { "SPDT toggle switch", $"Plus terminal: row {(char)('A' + row - 1)}, col {switchPlusCol}",
                $"Common terminal: row {(char)('A' + row)}, col {switchCol}",
                $"GND terminal: row {(char)('A' + row + 1)}, col {switchGndCol}" },
            "SPDT Mini Toggle");

        // Jumper: Switch common → Resistor input
        DrawJumperWire(g, wirePen, switchCommonPos, resistorInPos, 3, false);
        DrawConnectionDot(g, switchCommonPos);

        // Draw resistor at hole position
        double resistorValue = PartsDatabase.CalculateInputResistance(weight);
        DrawMiniResistorWithBands(g, resistorInPos.X, resistorInPos.Y, resistorValue, smallFont);

        // Capture wire path for this input resistor (for highlighting when selected)
        var wirePath = new List<Point[]>
        {
            new[] { switchCommonPos, resistorInPos },      // Switch → Resistor
            new[] { resistorOutPos, sumBusPos }            // Resistor → Summing bus
        };

        // EXACT hit region matching DrawMiniResistorWithBands drawing
        int resistorWidth = 2 * _holeSpacing;  // 40px total span (2 columns)
        // Resistor drawn from x to x+40, y-5 to y+5 (height 10px)
        RegisterComponent(new Rectangle(resistorInPos.X, resistorInPos.Y - 5, resistorWidth, 10),
            $"R{inputNum}: {FormatResistance(resistorValue)}", ComponentType.Resistor, resistorValue, Color.Tan,
            new[] { $"Value: {FormatResistance(resistorValue)}", $"Weight: {weight:+0.00;-0.00}",
                $"Spans row {(char)('A' + row)}, cols {resistorCol}-{resistorCol + 2}" },  // FIXED: +2 not +4
            $"{FormatResistance(resistorValue)} 1/4W",
            wirePath,  // Wire path for highlighting
            $"Input {inputNum}",  // Subsystem
            "",  // Auto-generate contextual description
            inputNum,  // Input number for description generation
            weight);  // Weight for description generation

        // Jumper: Resistor output → Summing bus
        DrawJumperWire(g, wirePen, resistorOutPos, sumBusPos, 5, false);
    }

    // NEW: Draw op-amp with hole-based placement (ELECTRICALLY ACCURATE)
    private void DrawOpAmpHoleBased(Graphics g, int bbLeft, int bbTop, int bbHeight, int centerRow, int startCol,
        Font smallFont, Font tinyFont)
    {
        // LM358 DIP-8 straddles center channel
        // Pins 1-4 on left (rows before center), pins 5-8 on right (rows after center)

        Point pin1Pos = GetHolePos(bbLeft, bbTop, centerRow - 2, startCol);     // OUT1
        Point pin2Pos = GetHolePos(bbLeft, bbTop, centerRow - 1, startCol);     // IN1-
        Point pin3Pos = GetHolePos(bbLeft, bbTop, centerRow, startCol);         // IN1+
        Point pin4Pos = GetHolePos(bbLeft, bbTop, centerRow + 1, startCol);     // GND
        Point pin5Pos = GetHolePos(bbLeft, bbTop, centerRow + 1, startCol + 7); // IN2+
        Point pin6Pos = GetHolePos(bbLeft, bbTop, centerRow, startCol + 7);     // IN2-
        Point pin7Pos = GetHolePos(bbLeft, bbTop, centerRow - 1, startCol + 7); // OUT2
        Point pin8Pos = GetHolePos(bbLeft, bbTop, centerRow - 2, startCol + 7); // V+

        // Draw IC body centered on pins
        int icX = pin1Pos.X - 5;
        int icY = pin1Pos.Y - 5;
        DrawLM358Component(g, icX, icY, smallFont, tinyFont);

        // REQUIRED power connections - op-amp won't work without these
        using var redPen = new Pen(WireRed, 4f);      // Thicker wires for visibility
        using var blackPen = new Pen(WireBlack, 4f);  // Thicker wires for visibility

        // Pin 8 (V+) to ACTUAL top +5V rail
        Point pin8Rail = GetPowerRailHole(bbLeft, bbTop, bbHeight, true, true, startCol + 7);
        g.DrawLine(redPen, pin8Pos, pin8Rail);
        DrawConnectionDot(g, pin8Pos);

        // Pin 4 (GND) to ACTUAL bottom ground rail
        Point pin4Rail = GetPowerRailHole(bbLeft, bbTop, bbHeight, false, false, startCol);
        g.DrawLine(blackPen, pin4Pos, pin4Rail);
        DrawConnectionDot(g, pin4Pos);

        // Pin 3 (IN+, non-inverting) to ACTUAL bottom ground rail (inverting config)
        Point pin3Rail = GetPowerRailHole(bbLeft, bbTop, bbHeight, false, false, startCol);
        g.DrawLine(blackPen, pin3Pos, pin3Rail);
        DrawConnectionDot(g, pin3Pos);
    }

    // NEW: Draw summing bus with hole-based placement
    private void DrawSummingBusHoleBased(Graphics g, int bbLeft, int bbTop, int sumBusCol,
        int firstRow, int lastRow, int opAmpPinRow, int opAmpCol, Font smallFont)
    {
        using var busPen = new Pen(WireYellow, 5f);  // Thicker for summing bus visibility

        // Draw vertical summing bus (yellow wire)
        Point busTop = GetHolePos(bbLeft, bbTop, firstRow, sumBusCol);
        Point busBottom = GetHolePos(bbLeft, bbTop, lastRow, sumBusCol);

        g.DrawLine(busPen, busTop, busBottom);

        // Draw single connection dot at each input junction on the bus
        int rowSpacing = 4;  // Must match main paint method's rowSpacing (was hardcoded to 3 - BUG!)
        for (int i = 0; i < (lastRow - firstRow) / rowSpacing + 1; i++)
        {
            int row = firstRow + (i * rowSpacing);
            if (row <= lastRow)
            {
                Point junctionPos = GetHolePos(bbLeft, bbTop, row, sumBusCol);
                DrawConnectionDot(g, junctionPos, 5);  // Slightly larger for bus junction
            }
        }

        // Jumper: Summing bus to op-amp pin 2 (IN1-)
        Point opAmpPin2 = GetHolePos(bbLeft, bbTop, opAmpPinRow, opAmpCol);
        Point busMidpoint = GetHolePos(bbLeft, bbTop, (firstRow + lastRow) / 2, sumBusCol);
        DrawJumperWire(g, busPen, busMidpoint, opAmpPin2, 10, false);
        DrawConnectionDot(g, opAmpPin2);  // Dot only at op-amp pin

        // Feedback resistor: Pin 1 to Pin 2 (spans 2 columns like all resistors)
        Point opAmpPin1 = GetHolePos(bbLeft, bbTop, opAmpPinRow - 1, opAmpCol);
        int fbResCol = sumBusCol + 5;
        Point fbResIn = GetHolePos(bbLeft, bbTop, opAmpPinRow - 3, fbResCol);
        Point fbResOut = GetHolePos(bbLeft, bbTop, opAmpPinRow - 3, fbResCol + 2);  // 2 columns (was 4)

        // Draw feedback path (no intermediate dots)
        DrawJumperWire(g, busPen, opAmpPin2, fbResIn, 5, false);
        DrawMiniResistorWithBands(g, fbResIn.X, fbResIn.Y, PartsDatabase.ReferenceResistor, smallFont);

        // Capture wire path for feedback resistor (for highlighting when selected)
        var fbWirePath = new List<Point[]>
        {
            new[] { opAmpPin2, fbResIn },      // Op-amp IN- → Resistor input
            new[] { fbResOut, opAmpPin1 }      // Resistor output → Op-amp OUT
        };

        // Register feedback resistor as clickable component
        int fbResWidth = 2 * _holeSpacing;  // 40px total span (2 columns)
        RegisterComponent(new Rectangle(fbResIn.X - 5, fbResIn.Y - 8, fbResWidth + 10, 18),
            $"Rf: {FormatResistance(PartsDatabase.ReferenceResistor)}", ComponentType.Resistor,
            PartsDatabase.ReferenceResistor, Color.Tan,
            new[] { "Feedback resistor", $"Value: {FormatResistance(PartsDatabase.ReferenceResistor)}",
                "Sets gain = -Rf/Rin", "Determines output voltage range" },
            $"{FormatResistance(PartsDatabase.ReferenceResistor)} 1/4W",
            fbWirePath,  // Wire path for highlighting
            "Feedback Loop",  // Subsystem
            "",  // Auto-generate contextual description
            null,  // No input number
            null);  // No weight

        DrawJumperWire(g, busPen, fbResOut, opAmpPin1, 5, false);
        DrawConnectionDot(g, opAmpPin1);  // Dot at op-amp output pin
    }

    // NEW: Draw output section with hole-based placement (ELECTRICALLY ACCURATE)
    private void DrawOutputSectionHoleBased(Graphics g, int bbLeft, int bbTop, int bbHeight, int row, int startCol,
        int opAmpOutputRow, int opAmpCol, Font smallFont)
    {
        using var greenPen = new Pen(WireGreen, 4f);
        using var blackPen = new Pen(WireBlack, 4f);

        // Op-amp pin 1 (output)
        Point opAmpOut = GetHolePos(bbLeft, bbTop, opAmpOutputRow, opAmpCol);

        // Current limiting resistor position (2-column span to match graphic)
        Point resistorIn = GetHolePos(bbLeft, bbTop, row, startCol);
        Point resistorOut = GetHolePos(bbLeft, bbTop, row, startCol + 2);  // 2 columns (was 4)

        // Jumper: Op-amp output → Resistor input
        DrawJumperWire(g, greenPen, opAmpOut, resistorIn, 10, false);
        DrawConnectionDot(g, opAmpOut);  // Dot only at op-amp output pin

        // Draw current limiting resistor
        double ledResValue = PartsDatabase.Resistor470R.Resistance ?? 470;
        DrawMiniResistorWithBands(g, resistorIn.X, resistorIn.Y, ledResValue, smallFont);

        // Output LED position (need to calculate before registering resistor)
        Point ledAnodePos = GetHolePos(bbLeft, bbTop, row, startCol + 6);
        Point ledCathodePos = GetHolePos(bbLeft, bbTop, row + 1, startCol + 6);

        // Capture wire path for LED resistor (for highlighting when selected)
        var ledResWirePath = new List<Point[]>
        {
            new[] { opAmpOut, resistorIn },      // Op-amp OUT → Resistor input
            new[] { resistorOut, ledAnodePos }   // Resistor output → LED anode
        };

        RegisterComponent(new Rectangle(resistorIn.X - 5, resistorIn.Y - 8, 55, 18),
            $"R-LED: {FormatResistance(ledResValue)}", ComponentType.Resistor, ledResValue, Color.Tan,
            new[] { "LED current limiter", $"{FormatResistance(ledResValue)}, 1/4W",
                $"Placed at row {(char)('A' + row)}, cols {startCol}-{startCol + 4}" },
            $"{FormatResistance(ledResValue)} 1/4W Resistor",
            ledResWirePath,  // Wire paths for highlighting
            "Output",  // Subsystem
            "",  // Auto-generate contextual description
            null,  // No input number
            null);  // No weight

        // Jumper: Resistor → LED anode
        DrawJumperWire(g, greenPen, resistorOut, ledAnodePos, 0, false);

        // Jumper: LED cathode → ACTUAL bottom ground rail (need before registering LED)
        Point ledGndRail = GetPowerRailHole(bbLeft, bbTop, bbHeight, false, false, startCol + 6);

        // Draw output LED
        DrawMiniLED(g, ledAnodePos.X, ledAnodePos.Y, Color.LimeGreen, true);

        // Capture wire path for LED (cathode to ground)
        var ledWirePath = new List<Point[]>
        {
            new[] { ledCathodePos, ledGndRail }  // LED cathode → Ground rail
        };

        RegisterComponent(new Rectangle(ledAnodePos.X - 8, ledAnodePos.Y - 8, 20, 20),
            "Output LED (Green)", ComponentType.LED, 0, Color.LimeGreen,
            new[] { "5mm green LED", $"Anode at row {(char)('A' + row)}, col {startCol + 6}",
                $"Cathode at row {(char)('A' + row + 1)}, col {startCol + 6}",
                "Indicates positive output" },
            "Green LED 5mm",
            ledWirePath,  // Wire path for highlighting
            "Output",  // Subsystem
            "",  // Auto-generate contextual description
            null,  // No input number
            null);  // No weight

        // Draw cathode ground connection
        g.DrawLine(blackPen, ledCathodePos, ledGndRail);
        DrawConnectionDot(g, ledCathodePos);
    }

    #endregion
}

public enum ComponentType
{
    Resistor,
    LED,
    OpAmp,
    Switch,
    Battery,
    VoltageRegulator,
    Capacitor,
    Breadboard,
    Wire
}

public class ComponentRegion
{
    public Rectangle Bounds { get; set; }
    public string Name { get; set; } = "";
    public ComponentType Type { get; set; }
    public double Value { get; set; }
    public Color LedColor { get; set; }
    public string[] Specs { get; set; } = Array.Empty<string>();
    public string PartNumber { get; set; } = "";
    public List<Point[]>? WirePaths { get; set; }  // Signal path(s) for this component (for highlighting)
    public string GridPosition { get; set; } = "";  // Breadboard grid position (e.g., "D-15" or "D-5 to D-15" for wires)
    public string ContextualDescription { get; set; } = "";  // What this component does in THIS circuit
    public string Subsystem { get; set; } = "";  // Which subsystem this belongs to (Input, Summing, Feedback, Output, Power)
}
