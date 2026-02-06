using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PerceptronSimulator;

/// <summary>
/// Vintage 1950s teletype-style debug output window.
/// Displays debug information with authentic dot-matrix printer paper aesthetic:
/// wide alternating blue/white rows and perforated edges.
/// </summary>
public class PrinterDialog : Form
{
    private const int TITLE_BAR_HEIGHT = 30;

    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private Button _fontSmallerButton = null!;
    private Button _fontLargerButton = null!;
    private StripedPrinterTextBox _outputText = null!;

    private bool _isDragging;
    private Point _dragStart;
    private float _fontSize = 10f;  // Current font size

    private static readonly string LogFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Mark1", "Log.txt");

    private static readonly Color TextColor = Color.FromArgb(40, 40, 40);  // Dark gray

    // Embedded typewriter font
    private static PrivateFontCollection? _fontCollection;
    private static Font? _typewriterFont;
    private static List<string> _fontDiag = new() { "font load not started" };

    public PrinterDialog()
    {
        LoadTypewriterFont();
        InitializeForm();
        InitializeCustomChrome();
        InitializeContent();
        EnsureLogDirectory();
    }

    // P/Invoke to register font file with GDI (required for RichTextBox)
    [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
    private static extern int AddFontResourceEx(string lpFileName, uint fl, IntPtr pdv);

    private static string? _tempFontPath;

    /// <summary>
    /// Load the embedded OldNewspaperTypes font (SIL OFL licensed).
    /// Worn, imperfect newsprint serif — nostalgic 1950s newspaper aesthetic.
    /// Extracts to temp file and registers with GDI so RichTextBox can use it.
    /// </summary>
    private static void LoadTypewriterFont()
    {
        if (_typewriterFont != null)
            return;  // Already loaded

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PerceptronSimulator.resources.TT2020StyleE-Regular.ttf";

            _fontDiag = new() { "Loading started..." };

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _fontDiag.Add($"FAIL: resource '{resourceName}' not found");
                _typewriterFont = new Font("Courier New", 9f, FontStyle.Regular);
                return;
            }
            _fontDiag.Add($"Resource found, size={stream.Length} bytes");

            byte[] fontData = new byte[stream.Length];
            stream.Read(fontData, 0, (int)stream.Length);

            _tempFontPath = Path.Combine(Path.GetTempPath(), "TT2020StyleE-Regular.ttf");
            File.WriteAllBytes(_tempFontPath, fontData);
            _fontDiag.Add($"Wrote temp file: {_tempFontPath}, exists={File.Exists(_tempFontPath)}, size={new FileInfo(_tempFontPath).Length}");

            int gdiResult = AddFontResourceEx(_tempFontPath, 0x10, IntPtr.Zero);
            _fontDiag.Add($"AddFontResourceEx returned: {gdiResult} (>0 = success)");

            _fontCollection = new PrivateFontCollection();
            _fontCollection.AddFontFile(_tempFontPath);
            _fontDiag.Add($"GDI+ families count: {_fontCollection.Families.Length}");

            if (_fontCollection.Families.Length == 0)
            {
                _fontDiag.Add("FAIL: No font families loaded");
                _typewriterFont = new Font("Courier New", 9f, FontStyle.Regular);
                return;
            }

            string fontFamilyName = _fontCollection.Families[0].Name;
            _fontDiag.Add($"GDI+ family name: '{fontFamilyName}'");

            _typewriterFont = new Font(_fontCollection.Families[0], 12f, FontStyle.Regular);
            _fontDiag.Add($"Font created: Name='{_typewriterFont.Name}', Family='{_typewriterFont.FontFamily.Name}', Size={_typewriterFont.Size}");

            bool match = _typewriterFont.FontFamily.Name == fontFamilyName;
            _fontDiag.Add($"Font resolved correctly: {match}");
            if (!match)
                _fontDiag.Add($"MISMATCH: requested '{fontFamilyName}' but got '{_typewriterFont.FontFamily.Name}'");
        }
        catch (Exception ex)
        {
            _fontDiag.Add($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            _typewriterFont = new Font("Courier New", 9f, FontStyle.Regular);
        }
    }

    private void InitializeForm()
    {
        Text = "MARK I PRINTER OUTPUT";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(20, 20, 20);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(100, 100);
        Size = new Size(700, 500);
        MinimumSize = new Size(400, 300);
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
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
            Text = "MARK I PERCEPTRON SIMULATOR - TELETYPE OUTPUT",
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
        _closeButton.Click += (s, e) => Hide();

        // Font size buttons (left side of title bar)
        _fontSmallerButton = new Button
        {
            Text = "−",  // Minus sign
            Size = new Size(30, TITLE_BAR_HEIGHT),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Consolas", 14f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Left
        };
        _fontSmallerButton.FlatAppearance.BorderSize = 0;
        _fontSmallerButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
        _fontSmallerButton.Click += FontSmallerButton_Click;

        _fontLargerButton = new Button
        {
            Text = "+",
            Size = new Size(30, TITLE_BAR_HEIGHT),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(20, 20, 20),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Consolas", 14f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Left
        };
        _fontLargerButton.FlatAppearance.BorderSize = 0;
        _fontLargerButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 50, 50);
        _fontLargerButton.Click += FontLargerButton_Click;

        _titleBar.Controls.Add(_closeButton);
        _titleBar.Controls.Add(_titleLabel);
        _titleBar.Controls.Add(_fontLargerButton);
        _titleBar.Controls.Add(_fontSmallerButton);
        Controls.Add(_titleBar);

        // Position title label centered
        _titleBar.Resize += (s, e) =>
        {
            _titleLabel.Location = new Point(
                (_titleBar.Width - _titleLabel.Width) / 2,
                (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2
            );
        };
    }

    private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _isDragging = true;
            _dragStart = e.Location;
            if (sender != _titleBar)
                _dragStart = new Point(
                    _dragStart.X + ((Control)sender!).Left,
                    _dragStart.Y + ((Control)sender!).Top
                );
        }
    }

    private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentPos = PointToScreen(e.Location);
            if (sender != _titleBar)
            {
                var ctrl = (Control)sender!;
                currentPos = new Point(
                    currentPos.X - ctrl.Left,
                    currentPos.Y - ctrl.Top
                );
            }
            Location = new Point(
                currentPos.X - _dragStart.X,
                currentPos.Y - _dragStart.Y
            );
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void FontSmallerButton_Click(object? sender, EventArgs e)
    {
        _fontSize = Math.Max(6f, _fontSize - 1f);  // Minimum 6pt
        UpdateFontSize();
    }

    private void FontLargerButton_Click(object? sender, EventArgs e)
    {
        _fontSize = Math.Min(20f, _fontSize + 1f);  // Maximum 20pt
        UpdateFontSize();
    }

    private void UpdateFontSize()
    {
        if (_fontCollection != null && _fontCollection.Families.Length > 0)
        {
            // Use font family name (not FontFamily object) so GDI can resolve it for RichTextBox
            _outputText.Font = new Font(_fontCollection.Families[0].Name, _fontSize, FontStyle.Regular);
        }
        else
        {
            _outputText.Font = new Font("Courier New", _fontSize, FontStyle.Regular);
        }
    }

    private void InitializeContent()
    {
        // Create striped background panel (draws stripes)
        var stripedPanel = new StripedBackgroundPanel
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(stripedPanel);

        // Teletype text output - transparent over striped background
        _outputText = new StripedPrinterTextBox
        {
            Dock = DockStyle.Fill,
            ForeColor = TextColor,
            BackColor = Color.FromArgb(242, 235, 215), // Match aged cream stripe
            Font = _typewriterFont ?? new Font("Courier New", 10f, FontStyle.Regular),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = true
        };

        // Add padding to keep text away from perforated holes (holes at x=10, size=6)
        stripedPanel.Padding = new Padding(25, 5, 25, 5); // Left/right padding for holes
        stripedPanel.Controls.Add(_outputText);

        // Initial header (typewriter-style with character spacing)
        PrintLine("- - - - - - - - - - - - - - - - - - - - - - - - - - - -");
        PrintLine("    MARK I PERCEPTRON SIMULATOR - TELETYPE OUTPUT");
        PrintLine("    " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PrintLine("    Font: " + _outputText.Font.Name + " " + _outputText.Font.Size + "pt");
        foreach (var d in _fontDiag)
            PrintLine("    > " + d);
        PrintLine("- - - - - - - - - - - - - - - - - - - - - - - - - - - -");
        PrintLine("");
    }

    private void EnsureLogDirectory()
    {
        string? dir = Path.GetDirectoryName(LogFilePath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Print a line to the teletype output and log file.
    /// </summary>
    public void PrintLine(string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => PrintLine(message));
            return;
        }

        // Add to output window — select the newly appended text and apply font
        int start = _outputText.TextLength;
        _outputText.AppendText(message + Environment.NewLine);
        int end = _outputText.TextLength;
        _outputText.Select(start, end - start);
        _outputText.SelectionFont = _typewriterFont ?? _outputText.Font;
        _outputText.SelectionLength = 0;
        _outputText.SelectionStart = end;
        _outputText.ScrollToCaret();

        // Append to log file
        try
        {
            File.AppendAllText(LogFilePath, message + Environment.NewLine);
        }
        catch
        {
            // Silently ignore file errors
        }
    }

    /// <summary>
    /// Print formatted debug information.
    /// </summary>
    public void PrintDebug(string category, string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        PrintLine($"[{timestamp}] [{category}] {message}");
    }

    /// <summary>
    /// Clear the output window (but not the log file).
    /// </summary>
    public void Clear()
    {
        if (InvokeRequired)
        {
            Invoke(Clear);
            return;
        }

        _outputText.Clear();
        PrintLine("- - - OUTPUT CLEARED - - -");
        PrintLine("");
    }

    /// <summary>
    /// Panel that paints striped printer paper background with perforated holes.
    /// </summary>
    private class StripedBackgroundPanel : Panel
    {
        private static readonly Color CreamBar = Color.FromArgb(242, 235, 215);  // Aged yellowed cream
        private static readonly Color BlueBar = Color.FromArgb(215, 225, 235);   // Faded pale blue
        private const int BAR_HEIGHT = 56;
        private const int HOLE_SIZE = 6;
        private const int HOLE_SPACING = 32;  // Every other hole (was 16)
        private const int HOLE_MARGIN = 10;

        public StripedBackgroundPanel()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // DON'T call base.OnPaint - prevents background painting artifacts
            // base.OnPaint(e);

            var g = e.Graphics;
            // Turn off all smoothing/anti-aliasing for crisp edges
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

            // Fill entire background with cream first
            using (var creamBrush = new SolidBrush(CreamBar))
            {
                g.FillRectangle(creamBrush, 0, 0, Width, Height);
            }

            // Now paint blue stripes on top (every other stripe)
            using (var blueBrush = new SolidBrush(BlueBar))
            {
                for (int y = BAR_HEIGHT; y < Height; y += BAR_HEIGHT * 2)
                {
                    g.FillRectangle(blueBrush, 0, y, Width, BAR_HEIGHT);
                }
            }

            // Re-enable anti-aliasing for holes only
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Draw perforated holes on left and right edges
            using (var holeBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
            using (var holeEdgePen = new Pen(Color.FromArgb(100, 100, 100), 1))
            {
                for (int y = HOLE_SPACING / 2; y < Height; y += HOLE_SPACING)
                {
                    // Left edge perforations
                    g.FillEllipse(holeBrush, HOLE_MARGIN - HOLE_SIZE / 2, y - HOLE_SIZE / 2, HOLE_SIZE, HOLE_SIZE);
                    g.DrawEllipse(holeEdgePen, HOLE_MARGIN - HOLE_SIZE / 2, y - HOLE_SIZE / 2, HOLE_SIZE, HOLE_SIZE);

                    // Right edge perforations
                    g.FillEllipse(holeBrush, Width - HOLE_MARGIN - HOLE_SIZE / 2, y - HOLE_SIZE / 2, HOLE_SIZE, HOLE_SIZE);
                    g.DrawEllipse(holeEdgePen, Width - HOLE_MARGIN - HOLE_SIZE / 2, y - HOLE_SIZE / 2, HOLE_SIZE, HOLE_SIZE);
                }
            }
        }
    }

    /// <summary>
    /// RichTextBox with transparent background to show striped panel behind it.
    /// </summary>
    private class StripedPrinterTextBox : RichTextBox
    {
        private const int WM_ERASEBKGND = 0x0014;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x20;

        public StripedPrinterTextBox()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_TRANSPARENT;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Force transparent style
            int style = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, style | WS_EX_TRANSPARENT);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                // Don't erase background - let parent show through
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Don't paint background - let parent show through
        }
    }
}
