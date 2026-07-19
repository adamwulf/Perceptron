using PerceptronSimulator.Controls;
using System.Text.Json;

namespace PerceptronSimulator;

public class MainForm : Form
{
    private const int TITLE_BAR_HEIGHT = 30;
    private const int RESIZE_BORDER = 6;

    // Windows messages for resizing
    private const int WM_NCHITTEST = 0x84;
    private const int HTLEFT = 10;
    private const int HTRIGHT = 11;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTBOTTOM = 15;
    private const int HTBOTTOMLEFT = 16;
    private const int HTBOTTOMRIGHT = 17;

    private PerceptronEngine _engine;
    private int _gridSize = 4;
    private bool _weightsDirty = false;

    // Easter egg: linear mode allows non-square node counts (2-10 nodes in a row)
    private bool _isLinearMode = false;
    private int _linearNodeCount = 1;

    // Controls
    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;

    private Panel _switchPanel = null!;
    private Panel _knobPanel = null!;
    private Panel _outputPanel = null!;
    private Panel _controlPanel = null!;
    private Controls.FormulaPlateControl _formulaPlate = null!;

    private List<SwitchControl> _switches = new();
    private List<KnobControl> _knobs = new();
    private KnobControl _biasKnob = null!;
    private Controls.MechanicalPushButton _resetButton = null!;
    private Controls.MechanicalPushButton _printButton = null!;
    private MetalLabelControl _biasLabel = null!;
    private MetalLabelControl _voltageLabel = null!;
    private AnalogMeterControl _meter = null!;
    private OutputLedControl _outputLed = null!;
    private MetalPlateControl _instructionPlate = null!;
    private Controls.MechanicalPushButton _manualButton = null!;
    private PrinterDialog? _printerDialog;

    private Controls.MechanicalPushButton _learnPositiveButton = null!;
    private Controls.MetalLabelControl _learnPositiveLabel = null!;
    private Controls.MechanicalPushButton _learnNegativeButton = null!;
    private Controls.MetalLabelControl _learnNegativeLabel = null!;
    private Controls.MechanicalPushButton _debugButton = null!;
    private Controls.MechanicalPushButton _saveButton = null!;
    private Controls.MechanicalPushButton _loadButton = null!;
    private DebugDialog? _debugDialog;
    private Controls.SettingsKnobControl _gridSizeKnob = null!;
    private Controls.MetalLabelControl _gridSizeLabel = null!;
    private Controls.ConfigKnob _mathDial = null!;
    private Controls.MetalLabelControl _learningRateLabel = null!;
    private Controls.SettingsKnobControl _learningRateKnob = null!;
    private ToolTip _toolTip = null!;
    private Label _versionLabel = null!;
    private Controls.ArrowButton _arrowUp = null!;
    private Controls.ArrowButton _arrowDown = null!;
    private Controls.ArrowButton _arrowLeft = null!;
    private Controls.ArrowButton _arrowRight = null!;
    private Controls.MechanicalPushButton _centerToggleButton = null!;
    private enum CenterButtonState { Off, Red, Yellow }
    private CenterButtonState _centerButtonState = CenterButtonState.Off;

    // SETTINGS TOGGLE: Custom toggle control for collapsing control panel
    private Controls.TogglePlateControl _settingsToggle = null!;

    // TELETYPE TOGGLE: Custom toggle control for opening teletype debug output
    private Controls.TogglePlateControl _teletypeToggle = null!;

    private System.Windows.Forms.Timer _panelAnimationTimer = null!;
    private bool _panelExpanded = true;
    private const int PANEL_COLLAPSED_HEIGHT = 25;
    private const int PANEL_EXPANDED_HEIGHT = 120;
    private int _panelTargetHeight = PANEL_EXPANDED_HEIGHT;
    private const int ANIMATION_STEP = 8;

    // For dragging
    private bool _isDragging;
    private Point _dragStart;

    public MainForm()
    {
        _engine = new PerceptronEngine(_gridSize);
        InitializeForm();
        InitializeCustomChrome();
        InitializeMainContent();
        UpdateFormulaPlate();
        UpdateOutput();
    }

    private void InitializeForm()
    {
        Text = "Perceptron Simulator";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 580);
        Size = new Size(900, 680);
        Font = new Font("Consolas", 9f);
        DoubleBuffered = true;

        // Set application icon (shows in taskbar and Alt+Tab)
        try
        {
            var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("PerceptronSimulator.resources.app.ico");
            if (iconStream != null)
                Icon = new Icon(iconStream);
        }
        catch { }

        // Create tooltip with custom colors
        _toolTip = new ToolTip
        {
            AutoPopDelay = 10000,
            InitialDelay = 500,
            ReshowDelay = 200,
            ShowAlways = true,
            OwnerDraw = true,
            BackColor = Color.FromArgb(35, 35, 35),
            ForeColor = Color.FromArgb(200, 200, 180)
        };
        _toolTip.Draw += (s, e) =>
        {
            e.DrawBackground();
            e.DrawBorder();
            using var brush = new SolidBrush(_toolTip.ForeColor);
            e.Graphics.DrawString(e.ToolTipText, e.Font!, brush, new PointF(2, 2));
        };

        // Panel collapse animation timer
        _panelAnimationTimer = new System.Windows.Forms.Timer
        {
            Interval = 16 // ~60fps
        };
        _panelAnimationTimer.Tick += PanelAnimationTimer_Tick;
    }

    private void InitializeCustomChrome()
    {
        // Title bar
        _titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = TITLE_BAR_HEIGHT,
            BackColor = Color.FromArgb(20, 20, 20)
        };
        _titleBar.MouseDown += TitleBar_MouseDown;
        _titleBar.MouseMove += TitleBar_MouseMove;
        _titleBar.MouseUp += TitleBar_MouseUp;

        // Title label
        _titleLabel = new Label
        {
            Text = "Mark I Perceptron Simulator by Frank Rosenblatt, Cornell, 1958",
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        _titleLabel.MouseDown += TitleBar_MouseDown;
        _titleLabel.MouseMove += TitleBar_MouseMove;
        _titleLabel.MouseUp += TitleBar_MouseUp;

        // Close button
        _closeButton = new Button
        {
            Text = "X",
            Size = new Size(40, TITLE_BAR_HEIGHT),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(200, 60, 60),
            Font = new Font("Consolas", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 40);
        _closeButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 30, 30);
        _closeButton.Click += (s, e) => Close();

        // Video Tutorial link
        var videoLink = new Label
        {
            Text = "▶ Video Tutorial",
            ForeColor = Color.FromArgb(100, 180, 100),
            Font = new Font("Consolas", 8f, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        videoLink.Location = new Point(8, (TITLE_BAR_HEIGHT - videoLink.Height) / 2);
        videoLink.Click += (s, e) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://www.youtube.com/watch?v=l-9ALe3U-Fg", UseShellExecute = true }); }
            catch { }
        };
        videoLink.MouseEnter += (s, e) => videoLink.ForeColor = Color.FromArgb(150, 255, 150);
        videoLink.MouseLeave += (s, e) => videoLink.ForeColor = Color.FromArgb(100, 180, 100);

        // Intro / About link — re-opens the welcome & explanation dialog.
        // Amber tint (matching the Manual/Brain button glow) reads as an
        // interactive affordance rather than dim static text.
        var aboutLink = new Label
        {
            Text = "ⓘ About",
            ForeColor = Color.FromArgb(200, 180, 110),
            Font = new Font("Consolas", 8f, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };
        // Inline fallback position (mirrors videoLink); the Resize handler below
        // repositions it precisely once the title bar has laid out.
        aboutLink.Location = new Point(120, (TITLE_BAR_HEIGHT - aboutLink.Height) / 2);
        aboutLink.Click += (s, e) => ShowIntroductionDialog();
        aboutLink.MouseEnter += (s, e) => aboutLink.ForeColor = Color.FromArgb(255, 220, 120);
        aboutLink.MouseLeave += (s, e) => aboutLink.ForeColor = Color.FromArgb(200, 180, 110);
        _toolTip.SetToolTip(aboutLink, "What is this machine? Show the introduction.");

        _titleBar.Controls.Add(_closeButton);
        _titleBar.Controls.Add(videoLink);
        _titleBar.Controls.Add(aboutLink);
        _titleBar.Controls.Add(_titleLabel);

        Controls.Add(_titleBar);

        // Position title label centered, align video + about links vertically
        _titleBar.Resize += (s, e) =>
        {
            int titleY = (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2;
            _titleLabel.Location = new Point((_titleBar.Width - _titleLabel.Width) / 2, titleY);
            int linkY = titleY + _titleLabel.Height - videoLink.Height;
            videoLink.Location = new Point(8, linkY);
            aboutLink.Location = new Point(videoLink.Right + 14, linkY);
        };
    }

    private void InitializeMainContent()
    {
        Load += MainForm_Load;

        // Main content panel
        var mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Black,
            Padding = new Padding(15)
        };

        // Switch panel (left)
        _switchPanel = new Panel
        {
            BackColor = Color.FromArgb(15, 15, 15),
            BorderStyle = BorderStyle.None,
            AutoScroll = true
        };

        // Knob panel (center)
        _knobPanel = new Panel
        {
            BackColor = Color.FromArgb(15, 15, 15),
            BorderStyle = BorderStyle.None,
            AutoScroll = true
        };

        // Output panel (right)
        _outputPanel = new Panel
        {
            BackColor = Color.FromArgb(15, 15, 15),
            BorderStyle = BorderStyle.None
        };

        // Control panel (bottom)
        _controlPanel = new Panel
        {
            Height = 120,
            BackColor = Color.FromArgb(20, 20, 20),
            Dock = DockStyle.Bottom
        };

        // Formula plate (centered just below title bar)
        _formulaPlate = new Controls.FormulaPlateControl();

        InitializeControlPanel();
        InitializeOutputPanel();
        CreateGrid();

        mainPanel.Controls.Add(_switchPanel);
        mainPanel.Controls.Add(_knobPanel);
        mainPanel.Controls.Add(_outputPanel);

        Controls.Add(_controlPanel);
        Controls.Add(mainPanel);

        mainPanel.Resize += MainPanel_Resize;
        mainPanel.BringToFront();
        _controlPanel.BringToFront();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        // If you want the program to start with the control panel collapsed, uncomment this line:
        // _settingsToggle.Toggle();

        // On the very first launch, welcome the operator with an explanation of
        // what this machine is and how it learns. Re-openable any time via the
        // "ⓘ About" link in the title bar.
        if (IntroductionDialog.ShouldShowOnStartup())
        {
            // Defer until the form is fully shown so the dialog centers correctly.
            BeginInvoke(new Action(ShowIntroductionDialog));
        }
    }

    private void ShowIntroductionDialog()
    {
        bool openManual = false;
        using (var intro = new IntroductionDialog())
        {
            // Record the intent; the intro closes itself, then we open the
            // manual once its ShowDialog returns so the two don't stack.
            intro.OpenManualRequested += (s, e) => openManual = true;
            intro.ShowDialog(this);

            // Honor "Don't show this on startup" only when the user opted in;
            // never un-suppress once they've asked us to stop.
            if (intro.SuppressOnStartup)
                IntroductionDialog.MarkShown();
        }

        if (openManual)
        {
            using var manual = new ManualDialog();
            manual.ShowDialog(this);
        }
    }

    private void MainPanel_Resize(object? sender, EventArgs e)
    {
        var mainPanel = sender as Panel;
        if (mainPanel == null) return;

        int padding = 10;
        int availableWidth = mainPanel.ClientSize.Width - padding * 4;
        int availableHeight = mainPanel.ClientSize.Height - padding * 2;

        // At 8x8+, give more width to knob panel by taking from output panel
        double switchRatio = 0.30;
        double knobRatio = _gridSize >= 8 ? 0.50 : 0.40;

        int switchWidth = (int)(availableWidth * switchRatio);
        int knobWidth = (int)(availableWidth * knobRatio);
        int outputWidth = availableWidth - switchWidth - knobWidth;

        _switchPanel.SetBounds(padding, padding, switchWidth, availableHeight);
        _knobPanel.SetBounds(padding * 2 + switchWidth, padding, knobWidth, availableHeight);
        _outputPanel.SetBounds(padding * 3 + switchWidth + knobWidth, padding, outputWidth, availableHeight);

        ArrangeSwitches();
        ArrangeKnobs();
        ArrangeOutput();
    }

    private void InitializeControlPanel()
    {
        // Reset button - round red button like Load, positioned next to Save
        _resetButton = new Controls.MechanicalPushButton
        {
            LabelText = "RESET",
            Size = new Size(45, 55),
            GlowColor = Color.Empty // Glows red when any switch/knob is non-zero
        };
        _resetButton.ButtonClick += ResetButton_Click;

        // Make button - shows circuit schematic dialog
        _printButton = new Controls.MechanicalPushButton
        {
            LabelText = "MAKE",
            Size = new Size(45, 55),
            GlowColor = Color.FromArgb(100, 180, 100) // Green glow
        };
        _printButton.ButtonClick += PrintButton_Click;

        // Grid size label and knob (positioned by CenterActionButtons)
        _gridSizeLabel = new Controls.MetalLabelControl
        {
            LabelText = "Grid Size",
            Size = new Size(70, 20)
        };
        _toolTip.SetToolTip(_gridSizeLabel, "Number of switches/knobs per row and column (NxN grid)");

        _gridSizeKnob = new Controls.SettingsKnobControl
        {
            MinValue = 1,
            MaxValue = 10,
            Value = _gridSize,
            Step = 1,
            ValueFormat = "0",
            TickValues = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            MajorTickValues = new double[] { 1, 5, 10 },
            MinValuePointsUp = true,
            Size = new Size(75, 78)
        };
        _gridSizeKnob.ValueChanged += GridSizeKnob_ValueChanged;
        _gridSizeKnob.BelowMinimumAttempted += GridSizeKnob_BelowMinimumAttempted;
        _toolTip.SetToolTip(_gridSizeKnob, "Number of switches/knobs per row and column (NxN grid)");

        // Math dial (positioned to the right of Grid Size)
        // No label - hash marks show years (1958, 1960, 1986)
        _mathDial = new Controls.ConfigKnob
        {
            Size = new Size(120, 78)  // Same height as Grid Size for alignment
        };
        _mathDial.RuleChanged += MathDial_RuleChanged;
        _toolTip.SetToolTip(_mathDial, "1958: Classic perceptron\n1958+/m/÷: Extended perceptron variants\n1960: Widrow-Hoff LMS\n1986: Backprop MLP");

        // Learning rate - metal label above knob (positioned by CenterActionButtons)
        _learningRateLabel = new Controls.MetalLabelControl
        {
            LabelText = "RATE",
            Size = new Size(75, 20)
        };
        _toolTip.SetToolTip(_learningRateLabel, "How much to adjust dial weights when learning (higher = faster learning)");

        // Learning rate knob - values -30 to 30, 0 points up, fine-tune step
        _learningRateKnob = new Controls.SettingsKnobControl
        {
            MinValue = -30,
            MaxValue = 30,
            Value = 10.0,
            Step = 0.05,
            ValueFormat = "0.00",
            TickValues = new double[] { -30, -20, -10, 0, 10, 20, 30 },
            MajorTickValues = new double[] { -30, 0, 30 },
            MinValuePointsUp = false, // Zero points up for symmetric range
            Size = new Size(75, 78)
        };
        _learningRateKnob.ValueChanged += (s, e) => _engine.LearningRate = _learningRateKnob.Value;
        _toolTip.SetToolTip(_learningRateKnob, "How much to adjust dial weights when learning (higher = faster learning)");

        // Learn+ label and button
        _learnPositiveLabel = new Controls.MetalLabelControl
        {
            LabelText = "LEARN +",
            Size = new Size(55, 20)
        };
        _toolTip.SetToolTip(_learnPositiveLabel, "Automated learning: adjusts all weights to make the current switch pattern produce a POSITIVE output. See User Manual for details.");
        _learnPositiveButton = new Controls.MechanicalPushButton
        {
            LabelText = "",
            Size = new Size(50, 55),
            GlowColor = Color.Empty // Glow only when switches are on
        };
        _learnPositiveButton.ButtonClick += (s, e) => LearnPattern(true);
        _toolTip.SetToolTip(_learnPositiveButton, "Train: adjust weights so current pattern outputs POSITIVE");

        // Learn- label and button
        _learnNegativeLabel = new Controls.MetalLabelControl
        {
            LabelText = "LEARN -",
            Size = new Size(55, 20)
        };
        _toolTip.SetToolTip(_learnNegativeLabel, "Automated learning: adjusts all weights to make the current switch pattern produce a NEGATIVE output. See User Manual for details.");
        _learnNegativeButton = new Controls.MechanicalPushButton
        {
            LabelText = "",
            Size = new Size(50, 55),
            GlowColor = Color.Empty // Glow only when switches are on
        };
        _learnNegativeButton.ButtonClick += (s, e) => LearnPattern(false);
        _toolTip.SetToolTip(_learnNegativeButton, "Train: adjust weights so current pattern outputs NEGATIVE");

        _debugButton = new Controls.MechanicalPushButton
        {
            LabelText = "BRAIN",
            Size = new Size(45, 55),
            IsSquare = true,
            GlowColor = Color.FromArgb(255, 220, 80) // Yellow glow
        };
        _debugButton.ButtonClick += DebugButton_Click;
        _toolTip.SetToolTip(_debugButton, "Show neural network visualization");

        // Mechanical push buttons for save/load
        _saveButton = new Controls.MechanicalPushButton
        {
            LabelText = "SAVE",
            Size = new Size(45, 55),
            GlowColor = Color.Empty // Glows green when weights are dirty
        };
        _saveButton.ButtonClick += SaveButton_Click;
        _toolTip.SetToolTip(_saveButton, "Save current weights to file");

        _loadButton = new Controls.MechanicalPushButton
        {
            LabelText = "LOAD",
            Size = new Size(45, 55),
            GlowColor = Color.FromArgb(200, 80, 60) // Faint reddish glow
        };
        _loadButton.ButtonClick += LoadButton_Click;
        _toolTip.SetToolTip(_loadButton, "Load weights from file");

        _toolTip.SetToolTip(_printButton, "Show circuit schematic for building this network in hardware");

        // Manual button - near Brain button
        _manualButton = new Controls.MechanicalPushButton
        {
            LabelText = "MAN.",
            Size = new Size(45, 55),
            IsSquare = true,
            GlowColor = Color.FromArgb(255, 220, 80) // Yellow glow like Brain
        };
        _manualButton.ButtonClick += ManualButton_Click;
        _toolTip.SetToolTip(_manualButton, "Open user manual");

        // Version label - bottom left, subdued
        _versionLabel = new Label
        {
            Text = "v. 2025-12-29",
            Font = new Font("Consolas", 7f),
            ForeColor = Color.FromArgb(70, 70, 70),
            BackColor = Color.Transparent,
            AutoSize = true
        };

        // Add Paint event for control panel to draw fold line
        _controlPanel.Paint += ControlPanel_Paint;
        // Note: SETTINGS and TELETYPE are now custom TogglePlateControl - handle their own clicks

        _controlPanel.Controls.AddRange(new Control[] {
            _gridSizeLabel, _gridSizeKnob,
            _mathDial,  // No label - hash marks show years (1958, 1960, 1986)
            _learningRateLabel, _learningRateKnob,
            _learnPositiveLabel, _learnPositiveButton,
            _learnNegativeLabel, _learnNegativeButton,
            _debugButton, _manualButton, _printButton, _resetButton, _saveButton, _loadButton,
            _versionLabel
        });

        // Center the action buttons
        _controlPanel.Resize += (s, e) => CenterActionButtons();
    }

    private void CenterActionButtons()
    {
        // Layout: Labels in a row at top, controls below
        int labelY = 8;
        int labelHeight = 20;
        int controlY = labelY + labelHeight + 5;
        int gap = 8;

        // Grid Size dial aligned with left edge of switches (matching leftmost switch column)
        int gridKnobX = _switchPanel.Left + 15; // Align with leftmost switch
        int gridLabelX = gridKnobX + (_gridSizeKnob.Width - _gridSizeLabel.Width) / 2;
        _gridSizeLabel.Location = new Point(gridLabelX, labelY);
        _gridSizeKnob.Location = new Point(gridKnobX, controlY);

        // Center Learn+ label, Rate label, Learn- label together
        int totalWidth = _learnPositiveLabel.Width + gap +
                         _learningRateLabel.Width + gap +
                         _learnNegativeLabel.Width;
        int startX = (_controlPanel.Width - totalWidth) / 2;

        // Shift to the right at 8x8 and larger (by half a knob width)
        if (_gridSize >= 8)
        {
            int knobWidth = Math.Max(45, 70 - (_gridSize - 6) * 8);
            startX += knobWidth / 2;
        }

        // Math dial evenly spaced between Grid Size and Learn+
        // Grid Size: knob at Y=2, center at Y=30 (from controlY)
        // ConfigKnob: knob at Y=18, center at Y=46 (from mathDialY)
        // To align: mathDialY + 46 = controlY + 30, so mathDialY = controlY - 16
        int gridSizeRight = gridKnobX + _gridSizeKnob.Width;
        int learnPlusLeft = startX;
        int mathDialX = gridSizeRight + (learnPlusLeft - gridSizeRight - _mathDial.Width) / 2;
        _mathDial.Location = new Point(mathDialX, controlY - 16);

        // Position labels in horizontal row
        _learnPositiveLabel.Location = new Point(startX, labelY);
        _learningRateLabel.Location = new Point(startX + _learnPositiveLabel.Width + gap, labelY);
        _learnNegativeLabel.Location = new Point(startX + _learnPositiveLabel.Width + gap + _learningRateLabel.Width + gap, labelY);

        // Position controls below their labels
        _learnPositiveButton.Location = new Point(startX, controlY);
        _learningRateKnob.Location = new Point(startX + _learnPositiveLabel.Width + gap, controlY);
        _learnNegativeButton.Location = new Point(startX + _learnPositiveLabel.Width + gap + _learningRateLabel.Width + gap, controlY);

        // Right side buttons - center vertically
        int rightButtonY = (_controlPanel.Height - _loadButton.Height) / 2;

        // Load button in bottom right
        _loadButton.Location = new Point(_controlPanel.Width - _loadButton.Width - 15, rightButtonY);

        // Save button to the left of Load
        _saveButton.Location = new Point(_loadButton.Left - _saveButton.Width - 5, rightButtonY);

        // Reset button to the left of Save
        _resetButton.Location = new Point(_saveButton.Left - _resetButton.Width - 5, rightButtonY);

        // Print button to the left of Reset
        _printButton.Location = new Point(_resetButton.Left - _printButton.Width - 5, rightButtonY);

        // Brain button to the left of Print
        _debugButton.Location = new Point(_printButton.Left - _debugButton.Width - 5, rightButtonY);

        // Manual button to the left of Brain
        _manualButton.Location = new Point(_debugButton.Left - _manualButton.Width - 5, rightButtonY);

        // Version label - bottom right
        _versionLabel.Location = new Point(_controlPanel.Width - _versionLabel.Width - 5, _controlPanel.Height - _versionLabel.Height - 3);
    }

    private Button CreateFlatButton(string text, int x, int y, int width)
    {
        var btn = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = Font,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);
        return btn;
    }

    private void InitializeOutputPanel()
    {
        _meter = new AnalogMeterControl
        {
            Size = new Size(180, 130)
        };

        _outputLed = new OutputLedControl
        {
            Label = "OUTPUT"
        };
        _outputLed.StateChanged += OutputLed_StateChanged;

        _instructionPlate = new MetalPlateControl
        {
            Size = new Size(190, 160),
            InstructionLines = new[]
            {
                "  OPERATING PROCEDURE",
                "",
                "1. Set switch pattern",
                "2. If output should be +",
                "   press [Learn +]",
                "3. If output should be -",
                "   press [Learn -]",
                "4. Repeat with patterns"
            }
        };

        _settingsToggle = new Controls.TogglePlateControl
        {
            LabelText = "SETTINGS",
            Size = new Size(50, 75),
            ActiveColor = Color.FromArgb(100, 255, 150),
            IsToggled = true  // Start DOWN (panel starts expanded)
        };
        _settingsToggle.ToggleChanged += SettingsToggle_Changed;
        _toolTip.SetToolTip(_settingsToggle, "Toggle control panel visibility");

        _teletypeToggle = new Controls.TogglePlateControl
        {
            LabelText = "TELETYPE",
            Size = new Size(50, 75),
            ActiveColor = Color.FromArgb(100, 255, 150)
        };
        _teletypeToggle.ToggleChanged += TeletypeToggle_Changed;
        _toolTip.SetToolTip(_teletypeToggle, "Toggle vintage teletype debug output");

        _outputPanel.Controls.Add(_meter);
        _outputPanel.Controls.Add(_outputLed);
        _outputPanel.Controls.Add(_formulaPlate);
        _outputPanel.Controls.Add(_instructionPlate);
        _outputPanel.Controls.Add(_settingsToggle);
        _outputPanel.Controls.Add(_teletypeToggle);
    }

    private void ManualButton_Click(object? sender, EventArgs e)
    {
        using var manual = new ManualDialog();
        manual.ShowDialog(this);
    }

    private void DebugButton_Click(object? sender, EventArgs e)
    {
        if (_debugDialog == null || _debugDialog.IsDisposed)
        {
            _debugDialog = new DebugDialog();
            _debugDialog.Location = new Point(Right + 10, Top);

            // Subscribe to brain dialog events
            _debugDialog.InputNodeClicked += DebugDialog_InputNodeClicked;
            _debugDialog.WeightChangeRequested += DebugDialog_WeightChangeRequested;
        }

        if (_debugDialog.Visible)
        {
            _debugDialog.Hide();
        }
        else
        {
            _debugDialog.Show(this);
            UpdateDebugDialog();
        }
    }

    private void DebugDialog_InputNodeClicked(object? sender, int index)
    {
        // Toggle the corresponding switch
        if (index >= 0 && index < _switches.Count)
        {
            _switches[index].IsOn = !_switches[index].IsOn;
            // Switch_StateChanged will be called automatically, which updates output
        }
    }

    private void DebugDialog_WeightChangeRequested(object? sender, (int index, double delta) args)
    {
        // Change the weight of the corresponding knob
        if (args.index >= 0 && args.index < _knobs.Count)
        {
            _knobs[args.index].Value += args.delta;
            // KnobControl's ValueChanged will be called automatically
        }
    }

    private void UpdateDebugDialog()
    {
        if (_debugDialog != null && _debugDialog.Visible)
        {
            // Guard against mismatched state during grid resize
            if (_switches.Count != _engine.Weights.Length)
                return;

            int[] inputs = _switches.Select(s => s.Value).ToArray();
            double output = _engine.CalculateOutput(inputs);
            _debugDialog.UpdateData(inputs, _engine.Weights, _engine.Bias, output, _gridSize,
                _engine.MathRule, _engine.HiddenOutputs);
        }
    }

    private void CreateGrid()
    {
        // Clear existing
        _switches.Clear();
        _knobs.Clear();
        _switchPanel.Controls.Clear();
        _knobPanel.Controls.Clear();

        // In linear mode, use linear node count; otherwise NxN grid
        int count = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;

        // Voltage label plate above switches
        _voltageLabel = new MetalLabelControl
        {
            LabelText = "Off=-1v  On=+1v",
            Size = new Size(110, 16)
        };
        _switchPanel.Controls.Add(_voltageLabel);

        // Create switches
        for (int i = 0; i < count; i++)
        {
            var sw = new SwitchControl();
            sw.StateChanged += Switch_StateChanged;
            _switches.Add(sw);
            _switchPanel.Controls.Add(sw);
        }

        // Arrow buttons for shifting pattern (d-pad style) - pale yellow glow like Brain button
        // These go where the Reset switch used to be (below the switch grid)
        Color paleYellowGlow = Color.FromArgb(255, 220, 80);

        // Arrow buttons: flat wide triangles, same shape rotated
        // Up/Down: 40 wide x 16 tall, Left/Right: 16 wide x 40 tall
        _arrowUp = new Controls.ArrowButton
        {
            Direction = PerceptronSimulator.Controls.ArrowDirection.Up,
            GlowColor = paleYellowGlow,
            Size = new Size(40, 16)
        };
        _arrowUp.ButtonClick += (s, e) => ShiftPattern(0, -1);
        _toolTip.SetToolTip(_arrowUp, "Shift pattern up");
        _switchPanel.Controls.Add(_arrowUp);

        _arrowDown = new Controls.ArrowButton
        {
            Direction = PerceptronSimulator.Controls.ArrowDirection.Down,
            GlowColor = paleYellowGlow,
            Size = new Size(40, 16)
        };
        _arrowDown.ButtonClick += (s, e) => ShiftPattern(0, 1);
        _toolTip.SetToolTip(_arrowDown, "Shift pattern down");
        _switchPanel.Controls.Add(_arrowDown);

        _arrowLeft = new Controls.ArrowButton
        {
            Direction = PerceptronSimulator.Controls.ArrowDirection.Left,
            GlowColor = paleYellowGlow,
            Size = new Size(16, 40)
        };
        _arrowLeft.ButtonClick += (s, e) => ShiftPattern(-1, 0);
        _toolTip.SetToolTip(_arrowLeft, "Shift pattern left");
        _switchPanel.Controls.Add(_arrowLeft);

        _arrowRight = new Controls.ArrowButton
        {
            Direction = PerceptronSimulator.Controls.ArrowDirection.Right,
            GlowColor = paleYellowGlow,
            Size = new Size(16, 40)
        };
        _arrowRight.ButtonClick += (s, e) => ShiftPattern(1, 0);
        _toolTip.SetToolTip(_arrowRight, "Shift pattern right");
        _switchPanel.Controls.Add(_arrowRight);

        // Center toggle button in the middle of the d-pad
        _centerToggleButton = new Controls.MechanicalPushButton
        {
            LabelText = "",
            IsSquare = true,
            _reallySquare_NoJoke = true,
            Size = new Size(46, 47),
            GlowColor = Color.Empty
        };
        _centerToggleButton.ButtonClick += CenterToggleButton_Click;
        _toolTip.SetToolTip(_centerToggleButton, "Toggle all switches");
        _switchPanel.Controls.Add(_centerToggleButton);

        // Create knobs with fine-tune step size
        for (int i = 0; i < count; i++)
        {
            var knob = new KnobControl
            {
                Step = 0.05,
                ValueFormat = "0.00"
            };
            int index = i;
            knob.ValueChanged += (s, e) =>
            {
                _engine.SetWeight(index, knob.Value);
                MarkWeightsDirty();
                UpdateResetButtonGlow();
                UpdateOutput();
            };
            _knobs.Add(knob);
            _knobPanel.Controls.Add(knob);
        }

        // Create bias knob (no Label property, we use metal label instead)
        _biasKnob = new KnobControl
        {
            Step = 0.05,
            ValueFormat = "0.00"
        };
        _biasKnob.ValueChanged += (s, e) =>
        {
            _engine.Bias = _biasKnob.Value;
            MarkWeightsDirty();
            UpdateOutput();
        };
        _knobPanel.Controls.Add(_biasKnob);

        _biasLabel = new MetalLabelControl
        {
            LabelText = "BIAS",
            Size = new Size(50, 18)
        };
        _toolTip.SetToolTip(_biasLabel, "The bias allows the decision boundary to shift away from the origin, which is essential for problems like AND/OR gates where the threshold isn't at zero");
        _knobPanel.Controls.Add(_biasLabel);

        ArrangeSwitches();
        ArrangeKnobs();
    }

    private void ArrangeSwitches()
    {
        int padding = 10;
        int spacing = 5;
        int voltageLabelHeight = 20;

        // In linear mode, arrange with wrapping at 5 columns
        if (_isLinearMode)
        {
            int swWidth = 50;
            int swHeight = 80;
            int linearCols = Math.Min(_linearNodeCount, 5); // Max 5 per row
            int linearRows = (_linearNodeCount + 4) / 5;    // Ceiling division

            int totalWidth = linearCols * swWidth + (linearCols - 1) * spacing;
            int startX = (_switchPanel.ClientSize.Width - totalWidth) / 2;

            _voltageLabel.Location = new Point(
                (_switchPanel.ClientSize.Width - _voltageLabel.Width) / 2,
                padding
            );

            int startY = padding + voltageLabelHeight;

            for (int i = 0; i < _switches.Count; i++)
            {
                int r = i / 5;
                int c = i % 5;
                _switches[i].Location = new Point(
                    startX + c * (swWidth + spacing),
                    startY + r * (swHeight + spacing)
                );
                _switches[i].Size = new Size(swWidth, swHeight);
            }

            // Arrow buttons below switches in linear mode
            int arrowY = startY + linearRows * (swHeight + spacing) + 10;
            int arrowCenterX = _switchPanel.ClientSize.Width / 2;
            int arrowCenterY = arrowY + 28;

            _arrowUp.Location = new Point(arrowCenterX - 20, arrowCenterY - 20 - 16);
            _arrowDown.Location = new Point(arrowCenterX - 20, arrowCenterY + 20);
            _arrowLeft.Location = new Point(arrowCenterX - 20 - 16, arrowCenterY - 20);
            _arrowRight.Location = new Point(arrowCenterX + 20, arrowCenterY - 20);
            _centerToggleButton.Location = new Point(arrowCenterX - 23, arrowCenterY - 26);

            return;
        }

        // Scale down switches for larger grids (7x7 and above)
        int switchWidthGrid, switchHeightGrid;
        if (_gridSize >= 7)
        {
            switchWidthGrid = Math.Max(28, 50 - (_gridSize - 6) * 7);
            switchHeightGrid = Math.Max(38, 80 - (_gridSize - 6) * 12);
        }
        else
        {
            switchWidthGrid = 50;
            switchHeightGrid = 80;
        }

        int cols = _gridSize;
        int totalWidthGrid = cols * switchWidthGrid + (cols - 1) * spacing;
        int startXGrid = (_switchPanel.ClientSize.Width - totalWidthGrid) / 2;

        // Position voltage label at top, centered
        _voltageLabel.Location = new Point(
            (_switchPanel.ClientSize.Width - _voltageLabel.Width) / 2,
            padding
        );

        int startYGrid = padding + voltageLabelHeight;

        for (int i = 0; i < _switches.Count; i++)
        {
            int row = i / _gridSize;
            int col = i % _gridSize;

            _switches[i].Location = new Point(
                startXGrid + col * (switchWidthGrid + spacing),
                startYGrid + row * (switchHeightGrid + spacing)
            );
            _switches[i].Size = new Size(switchWidthGrid, switchHeightGrid);
        }

        // Arrow buttons in d-pad arrangement forming a square, no overlap
        // Up/Down: 40x16, Left/Right: 16x40 (same flat triangle, rotated)
        int arrowYGrid = startYGrid + _gridSize * (switchHeightGrid + spacing) + 10;
        int arrowCenterXGrid = _switchPanel.ClientSize.Width / 2;
        int arrowCenterYGrid = arrowYGrid + 28; // Center of d-pad area

        // Arrows form a 40x40 square with small gap in center
        // Up: top edge of square
        _arrowUp.Location = new Point(arrowCenterXGrid - 20, arrowCenterYGrid - 20 - 16);
        // Down: bottom edge of square
        _arrowDown.Location = new Point(arrowCenterXGrid - 20, arrowCenterYGrid + 20);
        // Left: left edge of square
        _arrowLeft.Location = new Point(arrowCenterXGrid - 20 - 16, arrowCenterYGrid - 20);
        // Right: right edge of square
        _arrowRight.Location = new Point(arrowCenterXGrid + 20, arrowCenterYGrid - 20);
        // Center toggle button
        _centerToggleButton.Location = new Point(arrowCenterXGrid - 23, arrowCenterYGrid - 26);
    }

    private void ArrangeKnobs()
    {
        int padding = 10;
        int spacing = 5;

        // In linear mode, arrange with wrapping at 5 columns
        if (_isLinearMode)
        {
            int kWidth = 70;
            int kHeight = 90;
            int linearCols = Math.Min(_linearNodeCount, 5); // Max 5 per row
            int linearRows = (_linearNodeCount + 4) / 5;    // Ceiling division

            int totalWidth = linearCols * kWidth + (linearCols - 1) * spacing;
            int startX = (_knobPanel.ClientSize.Width - totalWidth) / 2;
            int startY = padding;

            for (int i = 0; i < _knobs.Count; i++)
            {
                int r = i / 5;
                int c = i % 5;
                _knobs[i].Location = new Point(
                    startX + c * (kWidth + spacing),
                    startY + r * (kHeight + spacing)
                );
                _knobs[i].Size = new Size(kWidth, kHeight);
            }

            // Bias knob below the rows
            int biasY = startY + linearRows * (kHeight + spacing) + 5;
            int biasX = (_knobPanel.ClientSize.Width - kWidth) / 2;
            _biasKnob.Location = new Point(biasX, biasY);
            _biasKnob.Size = new Size(kWidth, kHeight);

            _biasLabel.Location = new Point(
                biasX - _biasLabel.Width - 8,
                biasY + (kHeight - _biasLabel.Height) / 2
            );

            return;
        }

        // Scale down knobs for larger grids (7x7 and above)
        int knobWidthGrid, knobHeightGrid;
        if (_gridSize >= 7)
        {
            knobWidthGrid = Math.Max(45, 70 - (_gridSize - 6) * 8);
            knobHeightGrid = Math.Max(58, 90 - (_gridSize - 6) * 10);
        }
        else
        {
            knobWidthGrid = 70;
            knobHeightGrid = 90;
        }

        int cols = _gridSize;
        int totalWidthGrid = cols * knobWidthGrid + (cols - 1) * spacing;
        int startXGrid = (_knobPanel.ClientSize.Width - totalWidthGrid) / 2;

        // Shift knobs to the right at 8x8 and larger
        if (_gridSize >= 8)
        {
            startXGrid += knobWidthGrid / 2;
        }
        int startYGrid = padding;

        for (int i = 0; i < _knobs.Count; i++)
        {
            int row = i / _gridSize;
            int col = i % _gridSize;

            _knobs[i].Location = new Point(
                startXGrid + col * (knobWidthGrid + spacing),
                startYGrid + row * (knobHeightGrid + spacing)
            );
            _knobs[i].Size = new Size(knobWidthGrid, knobHeightGrid);
        }

        // Bias knob at the bottom center
        int biasYGrid = startYGrid + _gridSize * (knobHeightGrid + spacing) + 5;
        int biasXGrid = (_knobPanel.ClientSize.Width - knobWidthGrid) / 2;
        _biasKnob.Location = new Point(biasXGrid, biasYGrid);
        _biasKnob.Size = new Size(knobWidthGrid, knobHeightGrid);

        // BIAS label to the LEFT of bias knob
        _biasLabel.Location = new Point(
            biasXGrid - _biasLabel.Width - 8,
            biasYGrid + (knobHeightGrid - _biasLabel.Height) / 2
        );

        // Bring knob panel to front when shifted right at 8x8+
        if (_gridSize >= 8)
        {
            _knobPanel.BringToFront();
        }
    }

    private void ArrangeOutput()
    {
        int centerX = _outputPanel.ClientSize.Width / 2;
        int y = 10;

        _meter.Location = new Point(centerX - _meter.Width / 2, y);
        y += _meter.Height + 10;

        _outputLed.Location = new Point(centerX - _outputLed.Width / 2, y);
        y += _outputLed.Height + 15;

        _formulaPlate.Location = new Point(centerX - _formulaPlate.Width / 2, y);
        y += _formulaPlate.Height + 8;

        _instructionPlate.Location = new Point(centerX - _instructionPlate.Width / 2, y);
        int spacing = 8;
        y += _instructionPlate.Height + spacing;

        // SETTINGS toggle: left of center under instruction plate
        int toggleSpacing = 10;
        _settingsToggle.Location = new Point(centerX - _settingsToggle.Width - toggleSpacing / 2, y);

        // TELETYPE toggle: right of center, next to SETTINGS toggle
        _teletypeToggle.Location = new Point(centerX + toggleSpacing / 2, y);
    }

    private void Switch_StateChanged(object? sender, EventArgs e)
    {
        UpdateResetButtonGlow();
        UpdateCenterButtonState(CenterButtonState.Red);
        UpdateOutput();
        UpdateLearnButtonGlow(); // Must be after UpdateOutput so LED state is current
    }

    private void OutputLed_StateChanged(object? sender, EventArgs e)
    {
        UpdateLearnButtonGlow();
    }

    private void UpdateLearnButtonGlow()
    {
        // Only glow if at least one switch is on
        bool anySwitchOn = _switches.Any(s => s.IsOn);
        bool outputIsOn = _outputLed.IsOn;

        if (!anySwitchOn)
        {
            // No switches on - neither button should glow
            _learnPositiveButton.GlowColor = Color.Empty;
            _learnNegativeButton.GlowColor = Color.Empty;
            _toolTip.SetToolTip(_learnPositiveButton, "Turn on some switches first to create a pattern");
            _toolTip.SetToolTip(_learnNegativeButton, "Turn on some switches first to create a pattern");
        }
        else if (outputIsOn)
        {
            // Output is positive - only Learn- is useful
            _learnPositiveButton.GlowColor = Color.Empty;
            _learnNegativeButton.GlowColor = Color.FromArgb(255, 220, 80);
            _toolTip.SetToolTip(_learnPositiveButton, "Output is already positive - no positive training needed");
            _toolTip.SetToolTip(_learnNegativeButton, "Train this pattern to produce NEGATIVE output");
        }
        else
        {
            // Output is negative/zero - only Learn+ is useful
            _learnPositiveButton.GlowColor = Color.FromArgb(120, 255, 120);
            _learnNegativeButton.GlowColor = Color.Empty;
            _toolTip.SetToolTip(_learnPositiveButton, "Train this pattern to produce POSITIVE output");
            _toolTip.SetToolTip(_learnNegativeButton, "Output is already negative - no negative training needed");
        }
    }

    private void MarkWeightsDirty()
    {
        _weightsDirty = true;
        UpdateSaveButtonGlow();
    }

    private void MarkWeightsClean()
    {
        _weightsDirty = false;
        UpdateSaveButtonGlow();
    }

    private void UpdateSaveButtonGlow()
    {
        _saveButton.GlowColor = _weightsDirty ? Color.FromArgb(120, 255, 120) : Color.Empty;
        _saveButton.Invalidate();
    }

    private void UpdateOutput()
    {
        // Guard against mismatched state during grid resize
        int expectedCount = _isLinearMode ? _linearNodeCount : _gridSize * _gridSize;
        if (_switches.Count != expectedCount || _engine.Weights.Length != expectedCount)
            return;

        int[] inputs = _switches.Select(s => s.Value).ToArray();
        double output = _engine.CalculateOutput(inputs);

        // Clamp display to meter range
        _meter.Value = Math.Clamp(output, -100, 100);
        _outputLed.IsOn = output > 0;

        // Update tooltip with exact value in microamperes
        double microamperes = output * 1000;
        _toolTip.SetToolTip(_meter, $"Exact output: {microamperes} microamperes ({output})");

        // Update debug dialog if visible
        UpdateDebugDialog();
    }

    private void LearnPattern(bool desiredPositive)
    {
        // Guard against mismatched state during grid resize
        if (_switches.Count != _engine.Weights.Length)
            return;

        int[] inputs = _switches.Select(s => s.Value).ToArray();
        _engine.Learn(inputs, desiredPositive);

        // Update knobs to reflect new weights
        for (int i = 0; i < _knobs.Count && i < _engine.Weights.Length; i++)
        {
            _knobs[i].Value = _engine.Weights[i];
        }
        _biasKnob.Value = _engine.Bias;

        MarkWeightsDirty();
        UpdateOutput();
    }

    private void ShiftPattern(int dx, int dy)
    {
        // In linear mode with wrapping at 5 columns
        if (_isLinearMode)
        {
            int linearCols = 5;
            int linearRows = (_linearNodeCount + 4) / 5;

            // Get current states as 2D grid (padded to full rows)
            bool[,] linearGrid = new bool[linearRows, linearCols];
            for (int i = 0; i < _switches.Count; i++)
            {
                int r = i / linearCols;
                int c = i % linearCols;
                linearGrid[r, c] = _switches[i].IsOn;
            }

            // Create shifted grid
            bool[,] newLinearGrid = new bool[linearRows, linearCols];
            for (int r = 0; r < linearRows; r++)
            {
                for (int c = 0; c < linearCols; c++)
                {
                    int srcRow = r - dy;
                    int srcCol = c - dx;

                    if (srcRow >= 0 && srcRow < linearRows && srcCol >= 0 && srcCol < linearCols)
                    {
                        newLinearGrid[r, c] = linearGrid[srcRow, srcCol];
                    }
                    else
                    {
                        newLinearGrid[r, c] = false;
                    }
                }
            }

            // Apply new states to switches
            for (int i = 0; i < _switches.Count; i++)
            {
                int r = i / linearCols;
                int c = i % linearCols;
                _switches[i].IsOn = newLinearGrid[r, c];
            }

            UpdateResetButtonGlow();
            UpdateOutput();
            UpdateLearnButtonGlow();
            return;
        }

        // Get current states as 2D grid
        bool[,] grid = new bool[_gridSize, _gridSize];
        for (int i = 0; i < _switches.Count; i++)
        {
            int row = i / _gridSize;
            int col = i % _gridSize;
            grid[row, col] = _switches[i].IsOn;
        }

        // Create shifted grid
        bool[,] newGrid = new bool[_gridSize, _gridSize];
        for (int row = 0; row < _gridSize; row++)
        {
            for (int col = 0; col < _gridSize; col++)
            {
                int srcRow = row - dy;
                int srcCol = col - dx;

                // Wrap around or leave off (we'll leave off - pattern shifts out)
                if (srcRow >= 0 && srcRow < _gridSize && srcCol >= 0 && srcCol < _gridSize)
                {
                    newGrid[row, col] = grid[srcRow, srcCol];
                }
                else
                {
                    newGrid[row, col] = false;
                }
            }
        }

        // Apply new states to switches
        for (int i = 0; i < _switches.Count; i++)
        {
            int row = i / _gridSize;
            int col = i % _gridSize;
            _switches[i].IsOn = newGrid[row, col];
        }

        UpdateResetButtonGlow();
        UpdateOutput();
        UpdateLearnButtonGlow();
    }

    private void PrintButton_Click(object? sender, EventArgs e)
    {
        // Gather current network state
        int[] inputs = _switches.Select(s => s.Value).ToArray();
        double[] weights = _engine.Weights.ToArray();
        double bias = _engine.Bias;
        bool[] switchStates = _switches.Select(s => s.IsOn).ToArray();

        // Show modalless so it can stay open while using other dialogs
        var dialog = new PrintSchematicDialog(inputs, weights, bias, switchStates, _gridSize, _isLinearMode, _linearNodeCount, _mathDial.SelectedRule);
        dialog.Show(this);
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        // Reset all switches to OFF
        foreach (var sw in _switches)
        {
            sw.IsOn = false;
        }

        // Reset all weight knobs to 0
        foreach (var knob in _knobs)
        {
            knob.Value = 0;
        }
        _biasKnob.Value = 0;
        _engine.ResetWeights();

        UpdateResetButtonGlow();
        UpdateCenterButtonState(CenterButtonState.Off);
        UpdateOutput();
        UpdateLearnButtonGlow();
    }

    private void CenterToggleButton_Click(object? sender, EventArgs e)
    {
        switch (_centerButtonState)
        {
            case CenterButtonState.Off:
                // Turn all switches ON, go to yellow
                SetAllSwitchesWithoutEvent(true);
                UpdateCenterButtonState(CenterButtonState.Yellow);
                break;

            case CenterButtonState.Red:
                // Toggle ALL switches (each one flips), go to yellow
                ToggleAllSwitchesWithoutEvent();
                UpdateCenterButtonState(CenterButtonState.Yellow);
                break;

            case CenterButtonState.Yellow:
                // Turn all switches OFF, go to off
                SetAllSwitchesWithoutEvent(false);
                UpdateCenterButtonState(CenterButtonState.Off);
                break;
        }

        UpdateResetButtonGlow();
        UpdateOutput();
        UpdateLearnButtonGlow();
    }

    private void SetAllSwitchesWithoutEvent(bool state)
    {
        // Set switches without triggering state changed events
        foreach (var sw in _switches)
        {
            sw.StateChanged -= Switch_StateChanged;
            sw.IsOn = state;
            sw.StateChanged += Switch_StateChanged;
        }
    }

    private void ToggleAllSwitchesWithoutEvent()
    {
        // Toggle each switch individually without triggering state changed events
        foreach (var sw in _switches)
        {
            sw.StateChanged -= Switch_StateChanged;
            sw.IsOn = !sw.IsOn;
            sw.StateChanged += Switch_StateChanged;
        }
    }

    private void UpdateCenterButtonState(CenterButtonState newState)
    {
        // Only update to red if there are any switches on
        if (newState == CenterButtonState.Red && !_switches.Any(s => s.IsOn))
        {
            newState = CenterButtonState.Off;
        }

        _centerButtonState = newState;
        Color paleYellowGlow = Color.FromArgb(255, 220, 80);
        Color redGlow = Color.FromArgb(200, 80, 60);

        _centerToggleButton.GlowColor = newState switch
        {
            CenterButtonState.Off => Color.Empty,
            CenterButtonState.Red => redGlow,
            CenterButtonState.Yellow => paleYellowGlow,
            _ => Color.Empty
        };
    }

    private void UpdateResetButtonGlow()
    {
        // Glow red when any switch is on OR any knob is non-zero
        bool anyOn = _switches.Any(s => s.IsOn);
        bool anyKnobNonZero = _knobs.Any(k => Math.Abs(k.Value) > 0.01);
        bool shouldGlow = anyOn || anyKnobNonZero;
        _resetButton.GlowColor = shouldGlow ? Color.FromArgb(200, 80, 60) : Color.Empty;
    }

    private void GridSizeKnob_ValueChanged(object? sender, EventArgs e)
    {
        int newSize = (int)_gridSizeKnob.Value;

        // If coming from linear mode and going up, exit linear mode
        if (_isLinearMode && newSize > 1)
        {
            _isLinearMode = false;
            _linearNodeCount = 1;
        }

        if (newSize != _gridSize || _isLinearMode)
        {
            _gridSize = newSize;
            _engine.Resize(_isLinearMode ? _linearNodeCount : _gridSize * _gridSize);
            CreateGrid();
            AutoGrowWindow();
            UpdateOutput();
        }
    }

    private void MathDial_RuleChanged(object? sender, EventArgs e)
    {
        _engine.MathRule = _mathDial.SelectedRule;
        UpdateFormulaPlate();
        UpdateOutput();
        UpdateDebugDialog();
    }

    private void UpdateFormulaPlate()
    {
        var rule = _mathDial.SelectedRule;
        _formulaPlate.Line1 = "OUTPUT =";

        var mathRule = rule;
        string formula = mathRule switch
        {
            PerceptronSimulator.Controls.ConfigKnob.MathRule.PERCEPTRON_CLASSIC => "SUM(Switch x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_SUM => "SUM(all Switch x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_AVG => "AVG(all Switch x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_SUM => "SUM(Switch/N x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_AVG => "AVG(Switch/N x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.WIDROW_HOFF => "SUM(Switch x Weight) + Bias",
            PerceptronSimulator.Controls.ConfigKnob.MathRule.BACKPROP => "ReLU(W1*x + b1) -> W2*h + b2",
            _ => "SUM(Switch x Weight) + Bias"
        };
        _formulaPlate.Line2 = formula;
    }

    private void GridSizeKnob_BelowMinimumAttempted(object? sender, EventArgs e)
    {
        // Easter egg: when at grid size 1 and trying to go lower, add linear nodes
        // Nodes wrap at 4 per row, max 25 nodes (equivalent to 5x5 grid)
        if (!_isLinearMode)
        {
            // Enter linear mode with 2 nodes
            _isLinearMode = true;
            _linearNodeCount = 2;
        }
        else if (_linearNodeCount < 25)
        {
            // Add one more node (max 25)
            _linearNodeCount++;
        }
        else
        {
            // Already at max, do nothing
            return;
        }

        _engine.Resize(_linearNodeCount);
        CreateGrid();
        AutoGrowWindow();
        UpdateOutput();
    }

    private void AutoGrowWindow()
    {
        // Calculate required size based on grid
        int spacing = 5;
        int padding = 20;

        int switchWidth, switchHeight, knobWidth, knobHeight;
        int switchPanelWidth, knobPanelWidth, outputPanelWidth;
        int gridHeight;

        if (_isLinearMode)
        {
            // Linear mode: nodes wrap at 5 per row
            switchWidth = 50;
            switchHeight = 80;
            knobWidth = 70;
            knobHeight = 90;

            int cols = Math.Min(_linearNodeCount, 5);
            int rows = (_linearNodeCount + 4) / 5;

            switchPanelWidth = cols * switchWidth + (cols - 1) * spacing + padding * 2;
            knobPanelWidth = cols * knobWidth + (cols - 1) * spacing + padding * 2;
            outputPanelWidth = 220;

            // Height for rows + arrows + bias
            int switchGridHeight = rows * switchHeight + (rows - 1) * spacing + 20 + 80 + padding * 3;
            int knobGridHeight = (rows + 1) * knobHeight + rows * spacing + padding * 3; // +1 for bias
            gridHeight = Math.Max(switchGridHeight, knobGridHeight);
        }
        else
        {
            // Scale down for larger grids (7x7 and above) - must match ArrangeSwitches/ArrangeKnobs
            if (_gridSize >= 7)
            {
                switchWidth = Math.Max(28, 50 - (_gridSize - 6) * 7);
                switchHeight = Math.Max(38, 80 - (_gridSize - 6) * 12);
                knobWidth = Math.Max(45, 70 - (_gridSize - 6) * 8);
                knobHeight = Math.Max(58, 90 - (_gridSize - 6) * 10);
            }
            else
            {
                switchWidth = 50;
                switchHeight = 80;
                knobWidth = 70;
                knobHeight = 90;
            }

            // Calculate panel widths needed
            switchPanelWidth = _gridSize * switchWidth + (_gridSize - 1) * spacing + padding * 2;
            knobPanelWidth = _gridSize * knobWidth + (_gridSize - 1) * spacing + padding * 2;
            outputPanelWidth = _gridSize >= 8 ? 180 : 220;

            // Calculate height needed - use max of switch grid or knob grid
            int switchGridHeight = _gridSize * switchHeight + (_gridSize - 1) * spacing + switchHeight + 20 + padding * 3; // +20 for voltage label, +switchHeight for reset
            int knobGridHeight = _gridSize * knobHeight + (_gridSize - 1) * spacing + knobHeight + padding * 3;
            gridHeight = Math.Max(switchGridHeight, knobGridHeight);
        }

        // Total width = switch panel + knob panel + output panel + margins
        int requiredWidth = switchPanelWidth + knobPanelWidth + outputPanelWidth + 80;
        int requiredHeight = gridHeight + TITLE_BAR_HEIGHT + 60 + 40; // title + control panel + margin

        // Resize to fit grid, but never below minimum
        int newWidth = Math.Max(MinimumSize.Width, requiredWidth);
        int newHeight = Math.Max(MinimumSize.Height, requiredHeight);

        if (newWidth != Width || newHeight != Height)
        {
            // Calculate new position to keep form on screen
            var screen = Screen.FromControl(this).WorkingArea;
            int newX = Left;
            int newY = Top;

            // If form would extend below screen, move it up
            if (Top + newHeight > screen.Bottom)
            {
                newY = Math.Max(screen.Top + 10, screen.Bottom - newHeight);
            }

            // If form would extend past right edge, move it left
            if (Left + newWidth > screen.Right)
            {
                newX = Math.Max(screen.Left, screen.Right - newWidth);
            }

            // Set bounds (position and size) together
            SetBounds(newX, newY, newWidth, newHeight);
        }
    }

    #region Custom Chrome Event Handlers

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
            if (sender is Label)
            {
                _dragStart = new Point(_dragStart.X + _titleLabel.Left, _dragStart.Y + _titleLabel.Top);
            }
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            Point currentScreenPos = PointToScreen(e.Location);
            if (sender is Label)
            {
                currentScreenPos = _titleLabel.PointToScreen(e.Location);
            }
            Location = new Point(currentScreenPos.X - _dragStart.X, currentScreenPos.Y - _dragStart.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    #endregion

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw border
        using var borderPen = new Pen(Color.FromArgb(50, 50, 50), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        // Draw small red diagonal resize indicator in bottom-right corner
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var redPen = new Pen(Color.FromArgb(180, 60, 60), 1);

        int x = Width - 12;
        int y = Height - 12;

        // Three small diagonal lines
        g.DrawLine(redPen, x + 10, y + 4, x + 4, y + 10);
        g.DrawLine(redPen, x + 10, y + 7, x + 7, y + 10);
        g.DrawLine(redPen, x + 10, y + 10, x + 10, y + 10);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            base.WndProc(ref m);

            // Get cursor position relative to form
            Point pos = PointToClient(new Point(m.LParam.ToInt32() & 0xFFFF, m.LParam.ToInt32() >> 16));

            // Check if cursor is on edges for resizing
            bool onLeft = pos.X <= RESIZE_BORDER;
            bool onRight = pos.X >= Width - RESIZE_BORDER;
            bool onTop = pos.Y <= RESIZE_BORDER;
            bool onBottom = pos.Y >= Height - RESIZE_BORDER;

            if (onTop && onLeft)
                m.Result = (IntPtr)HTTOPLEFT;
            else if (onTop && onRight)
                m.Result = (IntPtr)HTTOPRIGHT;
            else if (onBottom && onLeft)
                m.Result = (IntPtr)HTBOTTOMLEFT;
            else if (onBottom && onRight)
                m.Result = (IntPtr)HTBOTTOMRIGHT;
            else if (onLeft)
                m.Result = (IntPtr)HTLEFT;
            else if (onRight)
                m.Result = (IntPtr)HTRIGHT;
            else if (onTop)
                m.Result = (IntPtr)HTTOP;
            else if (onBottom)
                m.Result = (IntPtr)HTBOTTOM;

            return;
        }

        base.WndProc(ref m);
    }

    #region Save/Load

    private class PerceptronData
    {
        public int GridSize { get; set; }
        public double[] Weights { get; set; } = Array.Empty<double>();
        public double Bias { get; set; }
        public double LearningRate { get; set; }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Save Perceptron Settings",
            Filter = "Perceptron Files (*.pcn)|*.pcn|All Files (*.*)|*.*",
            DefaultExt = "pcn",
            FileName = "perceptron_settings"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var data = new PerceptronData
            {
                GridSize = _gridSize,
                Weights = _engine.Weights.ToArray(),
                Bias = _engine.Bias,
                LearningRate = _engine.LearningRate
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dialog.FileName, json);
            MarkWeightsClean();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load Perceptron Settings",
            Filter = "Perceptron Files (*.pcn)|*.pcn|All Files (*.*)|*.*",
            DefaultExt = "pcn"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            string json = File.ReadAllText(dialog.FileName);
            var data = JsonSerializer.Deserialize<PerceptronData>(json);

            if (data == null)
            {
                MessageBox.Show("Invalid save file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Update grid size if different (this will recreate the grid)
            if (data.GridSize != _gridSize)
            {
                _gridSize = data.GridSize;
                _gridSizeKnob.Value = _gridSize;
                _engine.Resize(_gridSize * _gridSize);
                CreateGrid();
                AutoGrowWindow();
            }

            // Load weights
            for (int i = 0; i < data.Weights.Length && i < _engine.Weights.Length; i++)
            {
                _engine.Weights[i] = data.Weights[i];
                if (i < _knobs.Count)
                {
                    _knobs[i].Value = data.Weights[i];
                }
            }

            // Load bias
            _engine.Bias = data.Bias;
            _biasKnob.Value = data.Bias;

            // Load learning rate
            _engine.LearningRate = data.LearningRate;
            _learningRateKnob.Value = data.LearningRate;

            MarkWeightsClean();
            UpdateOutput();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region Control Panel Collapse

    private void SettingsToggle_Changed(object? sender, EventArgs e)
    {
        // Toggle panel collapse state
        // INVERTED: UP (false) = hidden, DOWN (true) = visible
        _panelExpanded = _settingsToggle.IsToggled;  // true = DOWN = visible
        _panelTargetHeight = _panelExpanded ? PANEL_EXPANDED_HEIGHT : PANEL_COLLAPSED_HEIGHT;

        // Start animation
        _panelAnimationTimer.Start();
    }

    private void TeletypeToggle_Changed(object? sender, EventArgs e)
    {
        // Toggle PrinterDialog visibility
        if (_teletypeToggle.IsToggled)
        {
            if (_printerDialog == null || _printerDialog.IsDisposed)
            {
                _printerDialog = new PrinterDialog();
            }
            _printerDialog.Show(this);  // Modalless
            _printerDialog.PrintLine("- - - Teletype Output Active - - -");

            // Connect global debug logger to teletype
            DebugLogger.SetPrinterDialog(_printerDialog);
            DebugLogger.Log("DEBUG", "Debug logger connected to teletype");
        }
        else
        {
            _printerDialog?.Hide();
            DebugLogger.SetPrinterDialog(null);
        }
    }

    // OLD TeletypeToggleSwitch_Paint DELETED - TogglePlateControl paints itself

    private void PanelAnimationTimer_Tick(object? sender, EventArgs e)
    {
        int currentHeight = _controlPanel.Height;

        if (currentHeight == _panelTargetHeight)
        {
            _panelAnimationTimer.Stop();

            // Hide/show controls based on state
            foreach (Control control in _controlPanel.Controls)
            {
                control.Visible = _panelExpanded;
            }

            return;
        }

        // Make controls invisible during animation
        if (currentHeight == PANEL_EXPANDED_HEIGHT && !_panelExpanded)
        {
            foreach (Control control in _controlPanel.Controls)
            {
                control.Visible = false;
            }
        }

        // Animate height
        int diff = _panelTargetHeight - currentHeight;
        int step = Math.Sign(diff) * Math.Min(ANIMATION_STEP, Math.Abs(diff));

        // Adjust both panel and form height
        _controlPanel.Height = currentHeight + step;
        Height += step;

        // Redraw fold line during animation
        _controlPanel.Invalidate();
    }

    private void ControlPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Draw fold line when collapsing
        if (_controlPanel.Height < PANEL_EXPANDED_HEIGHT)
        {
            using var foldPen = new Pen(Color.FromArgb(100, 80, 80, 80), 1);
            foldPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            g.DrawLine(foldPen, 0, _controlPanel.Height - 2, _controlPanel.Width, _controlPanel.Height - 2);
        }
    }


    // OLD paint handlers deleted - TogglePlateControl is now a custom control

    #endregion
}
