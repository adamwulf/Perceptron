using System.Diagnostics;

namespace PerceptronSimulator;

/// <summary>
/// A welcome / introduction dialog that explains what the Mark I Perceptron
/// machine does: a short history of Rosenblatt's 1958 invention, an intuitive
/// explanation of how a perceptron learns, a quick tour of this simulator's
/// controls, and links out to Wikipedia and other further-reading resources.
///
/// Shown automatically on first launch (see <see cref="ShouldShowOnStartup"/> /
/// <see cref="MarkShown"/>) and re-openable at any time from the title bar.
/// Styled to match <see cref="ManualDialog"/>'s vintage "aged paper" aesthetic.
/// </summary>
public class IntroductionDialog : Form
{
    private const int TITLE_BAR_HEIGHT = 30;

    // Links for context and further reading.
    private const string LINK_WIKI_PERCEPTRON = "https://en.wikipedia.org/wiki/Perceptron";
    private const string LINK_WIKI_ROSENBLATT = "https://en.wikipedia.org/wiki/Frank_Rosenblatt";
    private const string LINK_WIKI_MARK1 = "https://en.wikipedia.org/wiki/Perceptron#Mark_I_Perceptron_machine";
    private const string LINK_ORIGINAL_PAPER = "https://apps.dtic.mil/sti/tr/pdf/AD0236965.pdf";
    private const string LINK_BUILD_VIDEO = "https://www.youtube.com/watch?v=l-9ALe3U-Fg";

    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private Panel _contentPanel = null!;
    private PaperPanel _paper = null!;
    private CheckBox _dontShowAgain = null!;

    private bool _isDragging;
    private Point _dragStart;

    /// <summary>Raised when the user clicks the "Read the full Manual" button.</summary>
    public event EventHandler? OpenManualRequested;

    public IntroductionDialog()
    {
        InitializeForm();
        InitializeCustomChrome();
        InitializeContent();
        BuildIntroduction();
    }

    private void InitializeForm()
    {
        Text = "Welcome";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(20, 20, 20);
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(640, 760);
        Font = new Font("Courier New", 10f);
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
            Text = "WELCOME TO THE PERCEPTRON",
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
            _titleLabel.Location = new Point((_titleBar.Width - _titleLabel.Width) / 2, (TITLE_BAR_HEIGHT - _titleLabel.Height) / 2);
        };

        Controls.Add(_titleBar);
    }

    private void InitializeContent()
    {
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(20, 20, 20)
        };

        Controls.Add(_contentPanel);
        _contentPanel.BringToFront();

        // "Don't show this again" lives in the dark strip below the paper.
        // The dialog is a fixed size, so a manual bottom-left Location suffices.
        _dontShowAgain = new CheckBox
        {
            Text = "Don't show this on startup",
            ForeColor = Color.FromArgb(150, 150, 150),
            BackColor = Color.FromArgb(20, 20, 20),
            Font = new Font("Consolas", 9f),
            AutoSize = true
        };
    }

    private void BuildIntroduction()
    {
        _contentPanel.Controls.Clear();

        int paperWidth = _contentPanel.Width - 40;
        int paperHeight = _contentPanel.Height - 45;

        _paper = new PaperPanel
        {
            Location = new Point(20, 10),
            Size = new Size(paperWidth, paperHeight),
            // Friendly welcome page: no "DECLASSIFIED" watermark behind the body text.
            ShowWatermark = false
        };
        _contentPanel.Controls.Add(_paper);

        int y = 22;

        // --- Header -------------------------------------------------------
        var title = CreateTypewriterLabel("THE MARK I PERCEPTRON", 15, FontStyle.Bold);
        title.Location = new Point((paperWidth - title.PreferredWidth) / 2, y);
        _paper.Controls.Add(title);
        y += 30;

        var subtitle = CreateTypewriterLabel("A Machine That Learns — Frank Rosenblatt, 1958", 10, FontStyle.Regular);
        subtitle.ForeColor = Color.FromArgb(80, 80, 80);
        subtitle.Location = new Point((paperWidth - subtitle.PreferredWidth) / 2, y);
        _paper.Controls.Add(subtitle);
        y += 34;

        // Thin rule under the header.
        var rule = new Panel
        {
            BackColor = Color.FromArgb(120, 110, 95),
            Location = new Point(40, y),
            Size = new Size(paperWidth - 80, 1)
        };
        _paper.Controls.Add(rule);
        y += 14;

        // --- Scrollable body ---------------------------------------------
        var scrollPanel = new Panel
        {
            Location = new Point(5, y),
            Size = new Size(paperWidth - 10, paperHeight - y - 10),
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        _paper.Controls.Add(scrollPanel);

        int contentWidth = paperWidth - 80;
        int innerY = 6;

        innerY = AddSectionHeader(scrollPanel, "WHAT IS THIS MACHINE?", innerY);
        innerY = AddBody(scrollPanel,
            "This is a working recreation of the Perceptron — the world's first " +
            "artificial neural network that could learn from experience. In 1958, " +
            "Cornell psychologist Frank Rosenblatt introduced the algorithm and " +
            "demonstrated it on an IBM 704, showing that a machine could \"teach " +
            "itself\" to tell two kinds of patterns apart, simply by being shown " +
            "examples and corrected when it was wrong. The press called it the embryo " +
            "of a computer that would one day walk, talk, and be conscious. Rosenblatt " +
            "later built the idea into physical hardware — the Mark I Perceptron, " +
            "completed around 1960 with a 400-photocell \"camera eye\" and motor-driven " +
            "weights.",
            contentWidth, innerY);

        innerY = AddSectionHeader(scrollPanel, "HOW A PERCEPTRON WORKS", innerY);
        innerY = AddBody(scrollPanel,
            "A perceptron is surprisingly simple. It takes several inputs, gives each one a " +
            "\"weight\" (how much that input matters), adds them all up, and fires a YES or " +
            "NO based on whether the total crosses a threshold.",
            contentWidth, innerY);
        innerY = AddBody(scrollPanel,
            "  •  INPUTS — the switches on the left. Each is ON (+1) or OFF (−1).\n" +
            "  •  WEIGHTS — the knobs in the middle. Each turns an input up or down.\n" +
            "  •  OUTPUT — the meter and LED on the right show the machine's answer.",
            contentWidth, innerY);
        innerY = AddBody(scrollPanel,
            "It LEARNS by adjusting its own weights: show it a pattern, tell it the right " +
            "answer with LEARN+ or LEARN−, and every time it guesses wrong it nudges the " +
            "weights a little closer. After enough corrections, it settles on a set of " +
            "weights that gets the answer right — no one programmed the rule; it found " +
            "the rule itself. That single idea grew into the neural networks behind modern AI.",
            contentWidth, innerY);

        innerY = AddSectionHeader(scrollPanel, "TRY IT IN 30 SECONDS", innerY);
        innerY = AddBody(scrollPanel,
            "  1.  Flip a few input switches ON.\n" +
            "  2.  Press LEARN+ to teach the machine that this pattern means YES.\n" +
            "  3.  Change the switches, and press LEARN− to teach it a NO pattern.\n" +
            "  4.  Repeat a few times — watch the knobs (weights) move on their own and " +
            "the meter learn to separate the two patterns.",
            contentWidth, innerY);
        innerY = AddBody(scrollPanel,
            "Open THE BRAIN to watch the network think in real time, or the full " +
            "MANUAL for the whole story — including the different learning rules from " +
            "1958 to 1986 and how to build a real one from hardware.",
            contentWidth, innerY);

        // --- Further reading (external links) ----------------------------
        innerY = AddSectionHeader(scrollPanel, "HISTORY & FURTHER READING", innerY);
        innerY = AddLinkRow(scrollPanel, "Perceptron (Wikipedia)", LINK_WIKI_PERCEPTRON, innerY);
        innerY = AddLinkRow(scrollPanel, "Frank Rosenblatt (Wikipedia)", LINK_WIKI_ROSENBLATT, innerY);
        innerY = AddLinkRow(scrollPanel, "The Mark I Perceptron machine", LINK_WIKI_MARK1, innerY);
        innerY = AddLinkRow(scrollPanel, "Rosenblatt's original 1958 report (PDF)", LINK_ORIGINAL_PAPER, innerY);
        innerY = AddLinkRow(scrollPanel, "Watch a hardware perceptron being built", LINK_BUILD_VIDEO, innerY);
        innerY += 8;

        // --- Open-the-manual button --------------------------------------
        var manualButton = CreatePaperButton("READ THE FULL MANUAL  →");
        manualButton.Location = new Point(30, innerY);
        manualButton.Click += (s, e) =>
        {
            OpenManualRequested?.Invoke(this, EventArgs.Empty);
            Close();
        };
        scrollPanel.Controls.Add(manualButton);
        innerY += manualButton.Height + 12;

        // --- Footer strip: don't-show-again ------------------------------
        _dontShowAgain.Location = new Point(24, _contentPanel.Height - 28);
        _contentPanel.Controls.Add(_dontShowAgain);
        _dontShowAgain.BringToFront();
    }

    private int AddSectionHeader(Panel parent, string text, int y)
    {
        var header = CreateTypewriterLabel(text, 11, FontStyle.Bold | FontStyle.Underline);
        header.Location = new Point(30, y + 8);
        parent.Controls.Add(header);
        return y + 8 + header.PreferredHeight + 6;
    }

    private int AddBody(Panel parent, string text, int width, int y)
    {
        var body = new Label
        {
            Text = text,
            Font = new Font("Courier New", 9.5f),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.Transparent,
            Location = new Point(30, y),
            MaximumSize = new Size(width, 0),
            AutoSize = true
        };
        parent.Controls.Add(body);
        return y + body.PreferredHeight + 10;
    }

    private int AddLinkRow(Panel parent, string caption, string url, int y)
    {
        var bullet = CreateTypewriterLabel("•", 9, FontStyle.Regular);
        bullet.Location = new Point(30, y);
        parent.Controls.Add(bullet);

        var link = new LinkLabel
        {
            Text = caption,
            AutoSize = true,
            Font = new Font("Courier New", 9.5f),
            LinkColor = Color.FromArgb(40, 40, 120),
            ActiveLinkColor = Color.FromArgb(120, 40, 40),
            VisitedLinkColor = Color.FromArgb(80, 40, 120),
            BackColor = Color.Transparent,
            Location = new Point(48, y)
        };
        var target = url;
        link.LinkClicked += (s, e) => OpenUrl(target);
        parent.Controls.Add(link);

        return y + link.PreferredHeight + 6;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
        catch { /* best effort: nothing we can do if no browser is available */ }
    }

    private Label CreateTypewriterLabel(string text, float size, FontStyle style)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Courier New", size, style),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.Transparent,
            AutoSize = true
        };
    }

    private Button CreatePaperButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(14, 6, 14, 6),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(215, 210, 200),
            ForeColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Courier New", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(120, 115, 105);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(195, 190, 180);
        return btn;
    }

    #region Window Dragging
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
            Point screenPos = sender is Label ? _titleLabel.PointToScreen(e.Location) : PointToScreen(e.Location);
            Location = new Point(screenPos.X - _dragStart.X, screenPos.Y - _dragStart.Y);
        }
    }

    private void TitleBar_MouseUp(object? sender, MouseEventArgs e) => _isDragging = false;
    #endregion

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    // ------------------------------------------------------------------
    // First-run persistence
    // ------------------------------------------------------------------

    private static string FlagFilePath =>
        Path.Combine(Application.UserAppDataPath, "intro_shown.flag");

    /// <summary>
    /// True the very first time the app runs (before the intro has ever been
    /// shown-and-dismissed with "don't show again"). Any failure reading the
    /// flag errs on the side of showing the intro.
    /// </summary>
    public static bool ShouldShowOnStartup()
    {
        try { return !File.Exists(FlagFilePath); }
        catch { return true; }
    }

    /// <summary>
    /// Records that the intro has been shown so it will not appear on the next
    /// startup. Called when the user checks "Don't show this on startup".
    /// </summary>
    public static void MarkShown()
    {
        try
        {
            Directory.CreateDirectory(Application.UserAppDataPath);
            File.WriteAllText(FlagFilePath, "1");
        }
        catch { /* non-fatal: worst case the intro shows again next time */ }
    }

    /// <summary>True if the user asked not to see the intro on startup again.</summary>
    public bool SuppressOnStartup => _dontShowAgain.Checked;
}
