using System.Diagnostics;

namespace PerceptronSimulator;

public class ManualDialog : Form
{
    private const int TITLE_BAR_HEIGHT = 30;

    private Panel _titleBar = null!;
    private Label _titleLabel = null!;
    private Button _closeButton = null!;
    private Panel _contentPanel = null!;
    private PaperPanel _paper = null!;

    // Navigation
    private Label _prevButton = null!;
    private Label _nextButton = null!;
    private Label _pageNumberLabel = null!;

    private bool _isDragging;
    private Point _dragStart;

    // Page definitions with their fake page numbers
    private readonly Dictionary<string, string> _pageNumbers = new()
    {
        { "TOC", "" },
        { "Introduction", "3" },
        { "PressRelease", "5" },
        { "OperatingProcedures", "12" },
        { "MathDial", "19" },
        { "TheAlgorithm", "24" },
        { "The1960Algorithm", "29" },
        { "The1986Algorithm", "34" },
        { "Credits", "39" },
        { "BuildItYourself", "45" }
    };

    public ManualDialog()
    {
        InitializeForm();
        InitializeCustomChrome();
        InitializeContent();
        ShowTableOfContents();
    }

    private void InitializeForm()
    {
        Text = "Operator's Manual";
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(20, 20, 20);
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(620, 750);
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
            Text = "OPERATOR'S MANUAL",
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
    }

    private Action? _prevAction;
    private Action? _nextAction;

    private void SetNavigation(Action? prev, Action? next, string pageNum)
    {
        _prevAction = prev;
        _nextAction = next;

        _prevButton.Visible = prev != null;
        _nextButton.Visible = next != null;

        _pageNumberLabel.Text = string.IsNullOrEmpty(pageNum) ? "" : $"- {pageNum} -";
        _pageNumberLabel.Visible = !string.IsNullOrEmpty(pageNum);

        // Reposition
        int navY = _paper.Bottom + 10;
        _prevButton.Location = new Point(_paper.Left + 20, navY);
        _nextButton.Location = new Point(_paper.Right - _nextButton.Width - 20, navY);
        _pageNumberLabel.Location = new Point(_paper.Left + (_paper.Width - _pageNumberLabel.Width) / 2, navY);
    }

    private void NavigatePrev() => _prevAction?.Invoke();
    private void NavigateNext() => _nextAction?.Invoke();

    private void CreatePageStructure(bool showNav = true)
    {
        _contentPanel.Controls.Clear();

        // Create paper panel
        int paperWidth = _contentPanel.Width - 40;
        int paperHeight = _contentPanel.Height - (showNav ? 70 : 30);

        _paper = new PaperPanel
        {
            Location = new Point(20, 10),
            Size = new Size(paperWidth, paperHeight)
        };
        _contentPanel.Controls.Add(_paper);

        if (showNav)
        {
            // Navigation controls
            _prevButton = new Label
            {
                Text = "<- Prev",
                Font = new Font("Courier New", 10f),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _prevButton.Click += (s, e) => NavigatePrev();
            _prevButton.MouseEnter += (s, e) => _prevButton.ForeColor = Color.FromArgb(100, 150, 255);
            _prevButton.MouseLeave += (s, e) => _prevButton.ForeColor = Color.FromArgb(180, 180, 180);

            _nextButton = new Label
            {
                Text = "Next ->",
                Font = new Font("Courier New", 10f),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Visible = false
            };
            _nextButton.Click += (s, e) => NavigateNext();
            _nextButton.MouseEnter += (s, e) => _nextButton.ForeColor = Color.FromArgb(100, 150, 255);
            _nextButton.MouseLeave += (s, e) => _nextButton.ForeColor = Color.FromArgb(180, 180, 180);

            _pageNumberLabel = new Label
            {
                Text = "",
                Font = new Font("Courier New", 10f),
                ForeColor = Color.FromArgb(180, 180, 180),
                BackColor = Color.Transparent,
                AutoSize = true
            };

            _contentPanel.Controls.Add(_prevButton);
            _contentPanel.Controls.Add(_nextButton);
            _contentPanel.Controls.Add(_pageNumberLabel);
        }
    }

    private void ShowTableOfContents()
    {
        CreatePageStructure(false);

        int y = 25;
        int paperWidth = _paper.Width;
        int rightMargin = 50;
        int pageNumX = paperWidth - rightMargin;

        // Title
        var title = CreateTypewriterLabel("MARK I PERCEPTRON", 14, FontStyle.Bold);
        title.Location = new Point((paperWidth - title.PreferredWidth) / 2, y);
        _paper.Controls.Add(title);
        y += 28;

        var subtitle = CreateTypewriterLabel("OPERATOR'S MANUAL", 12, FontStyle.Bold);
        subtitle.Location = new Point((paperWidth - subtitle.PreferredWidth) / 2, y);
        _paper.Controls.Add(subtitle);
        y += 45;

        var tocHeader = CreateTypewriterLabel("TABLE OF CONTENTS", 11, FontStyle.Bold | FontStyle.Underline);
        tocHeader.Location = new Point(40, y);
        _paper.Controls.Add(tocHeader);
        y += 35;

        // Chapters - varied redaction lengths for authenticity
        y = AddTocEntry("I.", "Introduction", "3", y, pageNumX, true, () => ShowIntroduction(), 0);
        y = AddTocEntry("II.", "Press Release", "5", y, pageNumX, true, () => ShowPressRelease(), 0);
        y = AddTocEntry("III.", "[REDACTED]", "7", y, pageNumX, false, null, 95);
        y = AddTocEntry("IV.", "Basic Perceptron Training", "12", y, pageNumX, true, () => ShowOperatingProcedures(), 0);
        y = AddTocEntry("V.", "Selectable Math Dial", "19", y, pageNumX, true, () => ShowMathDial(), 0);
        y = AddTocEntry("VI.", "The 1958 Algorithm", "24", y, pageNumX, true, () => ShowAlgorithm(), 0);
        y = AddTocEntry("VII.", "The 1960 Algorithm", "29", y, pageNumX, true, () => ShowThe1960Algorithm(), 0);
        y = AddTocEntry("VIII.", "The 1986 Algorithm", "34", y, pageNumX, true, () => ShowThe1986Algorithm(), 0);
        y = AddTocEntry("IX.", "Credits / About", "39", y, pageNumX, true, () => ShowCredits(), 0);
        y = AddTocEntry("X.", "Build It Yourself", "45", y, pageNumX, true, () => ShowBuildItYourself(), 0);
        y = AddTocEntry("XI.", "[REDACTED]", "51", y, pageNumX, false, null, 155);

        y += 35;

        // Classification stamp
        var classified = CreateTypewriterLabel("DECLASSIFIED", 10, FontStyle.Bold);
        classified.ForeColor = Color.FromArgb(150, 60, 60);
        classified.Location = new Point((paperWidth - classified.PreferredWidth) / 2, y);
        _paper.Controls.Add(classified);

        var dateStamp = CreateTypewriterLabel("Authority: EO 12958", 8, FontStyle.Regular);
        dateStamp.ForeColor = Color.FromArgb(120, 80, 80);
        dateStamp.Location = new Point((paperWidth - dateStamp.PreferredWidth) / 2, y + 18);
        _paper.Controls.Add(dateStamp);
    }

    private int AddTocEntry(string number, string title, string page, int y, int pageNumX, bool clickable, Action? onClick, int redactionWidth = 140)
    {
        int leftMargin = 40;
        int titleX = leftMargin + 45;

        var numLabel = CreateTypewriterLabel(number, 10, FontStyle.Regular);
        numLabel.Location = new Point(leftMargin, y);
        _paper.Controls.Add(numLabel);

        Control titleControl;
        int titleWidth;
        if (title == "[REDACTED]")
        {
            // Show partial letter peeking out on one specific redaction (VI)
            bool showPartialLetter = (number == "VI.");
            titleControl = CreateRedactedLabel(redactionWidth, showPartialLetter);
            titleControl.Location = new Point(titleX, y + 2);
            titleWidth = redactionWidth;
        }
        else
        {
            var titleLabel = CreateTypewriterLabel(title, 10, FontStyle.Regular);
            titleLabel.Location = new Point(titleX, y);

            if (clickable && onClick != null)
            {
                titleLabel.ForeColor = Color.FromArgb(40, 40, 120);
                titleLabel.Cursor = Cursors.Hand;
                titleLabel.Click += (s, e) => onClick();
                titleLabel.MouseEnter += (s, e) => titleLabel.Font = new Font(titleLabel.Font, FontStyle.Underline);
                titleLabel.MouseLeave += (s, e) => titleLabel.Font = new Font(titleLabel.Font, FontStyle.Regular);
            }
            titleControl = titleLabel;
            titleWidth = titleLabel.PreferredWidth;
        }
        _paper.Controls.Add(titleControl);

        // Page number - right aligned at pageNumX
        var pageLabel = CreateTypewriterLabel(page, 10, FontStyle.Regular);
        pageLabel.Location = new Point(pageNumX, y);
        _paper.Controls.Add(pageLabel);

        // Dots fill the space between title end and page number start
        int dotsStart = titleX + titleWidth;
        int dotsEnd = pageNumX;
        int gapWidth = dotsEnd - dotsStart;

        if (gapWidth > 5)
        {
            // Fill with enough dots - use extra to ensure full coverage
            string dots = new string('.', 80);
            var dotsLabel = new Label
            {
                Text = dots,
                Font = new Font("Courier New", 10f),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                Location = new Point(dotsStart, y),
                Size = new Size(gapWidth, 20),
                AutoSize = false
            };
            _paper.Controls.Add(dotsLabel);
        }

        return y + 26;
    }

    private Control CreateRedactedLabel(int width, bool showPartialLetter = false)
    {
        if (!showPartialLetter)
        {
            return new Label
            {
                Size = new Size(width, 14),
                BackColor = Color.Black,
                Text = ""
            };
        }

        // Create a panel to hold both the black redaction and a partial letter peeking out
        var container = new Panel
        {
            Size = new Size(width + 8, 16),
            BackColor = Color.Transparent
        };

        var blackBox = new Label
        {
            Size = new Size(width, 14),
            BackColor = Color.Black,
            Text = "",
            Location = new Point(0, 1)
        };
        container.Controls.Add(blackBox);

        // Partial letter peeking out the right side
        var partialLetter = new Label
        {
            Text = "S",
            Font = new Font("Courier New", 10f),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.Transparent,
            AutoSize = true,
            Location = new Point(width - 2, -1)
        };
        container.Controls.Add(partialLetter);
        partialLetter.BringToFront();

        return container;
    }

    private void ShowIntroduction()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("I. INTRODUCTION");

        string content = @"In 1958, Frank Rosenblatt, a research psychologist at the Cornell Aeronautical Laboratory, introduced the Perceptron, the world's first artificial neural network capable of learning from experience.

KEY DEVELOPMENTS IN 1958

The Algorithm: Rosenblatt published his seminal paper, ""The Perceptron: A Probabilistic Model for Information Storage and Organization in the Brain,"" in Psychological Review. It defined the first machine learning algorithm for artificial neurons, using a weighted sum of inputs to make binary classification decisions.

Initial Demonstration: Using a 5-ton IBM 704 computer, Rosenblatt demonstrated the perceptron's ability to ""teach itself"" to distinguish between cards marked on the left versus the right after just 50 trials.

Public Impact: The U.S. Office of Naval Research unveiled the invention in July 1958, leading to sensational New York Times headlines that predicted the ""embryo of an electronic computer"" would soon walk, talk, and be conscious.

THE PERCEPTRON MACHINE (MARK I)

While the 1958 demonstrations used software simulations, Rosenblatt later realized the concept in physical hardware.

Physical Build: The Mark I Perceptron was completed in 1960 at Cornell.

Components: It featured a ""camera eye"" with 400 photocells (a 20x20 pixel array) and used electric motors to adjust potentiometers, which acted as the machine's ""variable weights"".

Capabilities: It was primarily designed for image recognition, successfully learning to identify basic geometric shapes like triangles and squares.";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowTableOfContents(), () => ShowPressRelease(), _pageNumbers["Introduction"]);
    }

    private void ShowPressRelease()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("II. PRESS RELEASE");

        string content = @"New York Times, July 13, 1958, Section E, Page 9.

The Navy last week demonstrated the embryo of an electronic computer named the Perceptron which, when completed in about a year, is expected to be the first non-living mechanism able to ""perceive, recognize and identify its surroundings without human training or control.""

Navy officers demonstrating a preliminary form of the device in Washington said they hesitated to call it a machine because it is so much like a ""human being without life.""

Dr. Frank Rosenblatt, research psychologist at the Cornell Aeronautical Laboratory, Inc., Buffalo, N. Y., designer of the Perceptron, conducted the demonstration. The machine, he said, would be the first electronic device to think as the human brain. Like humans, Perceptron will make mistakes at first, ""but it will grow wiser as it gains experience,"" he said.

The first Perceptron, to cost about $100,000, will have about 1,000 electronic ""association cells"" receiving electrical impulses from an eyelike scanning device with 400 photocells. The human brain has ten billion responsive cells, including 100,000,000 connections with the eye.

DIFFERENCE RECOGNIZED

The concept of the Perceptron was demonstrated on the Weather Bureau's $2,000,000 IBM 704 computer. In one experiment, the 704 computer was shown 100 squares situated at random either on the left or the right side of a field. In 100 trials, it was able to ""say"" correctly ninety-seven times whether a square was situated on the right or left.

Dr. Rosenblatt said that after having seen only thirty to forty squares the device had learned to recognize the difference between right and left, almost the way a child learns.

When fully developed, the Perceptron will be designed to remember images and information it has perceived itself, whereas ordinary computers remember only what is fed into them on punch cards or magnetic tape.

Later Perceptrons, Dr. Rosenblatt said, will be able to recognize people and call out their names. Printed pages, longhand letters and even speech commands are within its reach. Only one more step of development, a difficult step, he said, is needed for the device to hear speech in one language and instantly translate it to speech or writing in another language.

SELF-REPRODUCTION

In principle, Dr. Rosenblatt said, it would be possible to build Perceptrons that could reproduce themselves on an assembly line and which would be ""conscious"" of their existence.

Perceptron, it was pointed out, needs no ""priming."" It is not necessary to introduce it to surroundings and circumstances, record the data involved and then store them for future comparison as is the case with present ""mechanical brains.""

It literally teaches itself to recognize objects the first time it encounters them. It uses a camera-eye lens to scan objects or survey situations, and an electrical impulse system, patterned point-by-point after the human brain does the interpreting.

The Navy said it would use the principle to build the first Perceptron ""thinking machines"" that will be able to read or write.";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowIntroduction(), () => ShowOperatingProcedures(), _pageNumbers["PressRelease"]);
    }

    private void ShowOperatingProcedures()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("IV. BASIC PERCEPTRON TRAINING");

        string content = @"TRAINING THE PERCEPTRON: T vs J RECOGNITION

This procedure demonstrates how to train the Mark I Perceptron to distinguish between ""T"" and ""J"" shapes.

STEP 1: CREATE A ""T"" PATTERN
  - Turn ON switches to form a T shape on the grid
  - The T should have a horizontal bar at top and vertical line down the center

STEP 2: TRAIN FOR POSITIVE OUTPUT
  - We want ""T"" patterns to produce POSITIVE output
  - Click the [Learn +] button
  - The dials will automatically adjust

STEP 3: CREATE A ""J"" PATTERN
  - Use the RESET switch to clear all switches
  - Turn ON switches to form a J shape
  - The J has a horizontal bar at top, vertical line, and curves left at bottom

STEP 4: TRAIN FOR NEGATIVE OUTPUT
  - We want ""J"" patterns to produce NEGATIVE output
  - Click the [Learn -] button
  - The dials will adjust in the opposite direction

STEP 5: VERIFY LEARNING
  - Create the T pattern again
  - Output meter should show POSITIVE (green LED on)
  - Create the J pattern again
  - Output meter should show NEGATIVE (green LED off)

STEP 6: REPEAT AS NEEDED
  - If classification is incorrect, repeat training
  - Try patterns in different positions
  - The perceptron learns to generalize

NOTE: The Learn Rate controls how much the dials adjust with each training step. Higher values learn faster but may be less precise.";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowPressRelease(), () => ShowMathDial(), _pageNumbers["OperatingProcedures"]);
    }

    private void ShowMathDial()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("V. SELECTABLE MATH DIAL");

        string content = @"THE MATH DIAL: NEURAL NETWORK EVOLUTION (1958-1986)

The Math dial allows you to select between different neural network computation rules spanning nearly three decades of AI research. Each position represents a milestone in the development of machine learning.

DIAL POSITIONS (CLOCK FACE)

1 O'CLOCK: ""1958"" - PERCEPTRON CLASSIC
  Frank Rosenblatt's original perceptron (1957-1958).
  - 1-to-1 connectivity: each input connects to one hidden node
  - Formula: OUTPUT = SUM(input[i] x weight[i]) + bias
  - Learning: Only updates when classification is wrong
  - Default setting, matches the original Mark I hardware

2 O'CLOCK: ""1958+"" - 1958_SUM_RULE
  Extended perceptron with full connectivity.
  - All inputs connect to ALL hidden nodes
  - Each hidden node: h[j] = SUM(input[i] x weight[j])
  - Learning: Original perceptron rule
  - Explores what Rosenblatt might have built with more hardware

3 O'CLOCK: ""1958m"" - 1958_AVG_RULE
  Same as 1958+ but averages instead of summing.
  - Each hidden node: h[j] = AVERAGE(input[i] x weight[j])
  - Normalizes output regardless of input count
  - Useful for comparing patterns of different sizes

4 O'CLOCK: ""1958/+"" - 1958_DIV_SUM_RULE
  Divides inputs by N before weighting, then sums.
  - Each hidden node: h[j] = SUM((input[i]/N) x weight[j])
  - Normalizes inputs to prevent large activations
  - Alternative scaling strategy

5 O'CLOCK: ""1958/m"" - 1958_DIV_AVG_RULE
  Divides inputs AND averages output.
  - Each hidden node: h[j] = AVG((input[i]/N) x weight[j])
  - Double normalization for very stable outputs
  - Most conservative of the 1958 variants

6-8 O'CLOCK: (EMPTY)
  Reserved for future expansion.

9 O'CLOCK: ""1960"" - WIDROW-HOFF / LMS RULE
  Bernard Widrow and Ted Hoff's ADALINE system (1960).
  - 1-to-1 connectivity (same topology as 1958 perceptron)
  - Also known as: Delta Rule, Least Mean Squares (LMS)
  - Key difference: Uses continuous error, not binary
  - Formula: w(t+1) = w(t) + eta x (target - output) x input
  - Minimizes Mean Squared Error via gradient descent
  - Updates weights even when ""close"" to correct
  - Historically accurate: ADALINE was single-layer like perceptron

10 O'CLOCK: ""1986"" - BACKPROPAGATION
  Rumelhart, Hinton, and Williams (1986).
  - Full multi-layer network with learned hidden weights
  - Hidden layer uses ReLU activation: h[j] = max(0, z)
  - Backpropagates error gradient through all layers
  - The foundation of modern deep learning

CHOOSING A MATH RULE

For simple demonstrations: Use ""1958"" (classic perceptron)
For exploring connectivity: Try ""1958+"" or ""1958m""
For smooth learning: Use ""1960"" (Widrow-Hoff)
For complex patterns: Use ""1986"" (backpropagation)

NOTE: All 1958 variants use the original perceptron learning rule (update only when wrong). The 1960 and 1986 rules use continuous error for smoother convergence.";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowOperatingProcedures(), () => ShowAlgorithm(), _pageNumbers["MathDial"]);
    }

    private void ShowAlgorithm()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("VI. THE 1958 ALGORITHM");

        string content = "THE PERCEPTRON LEARNING RULE\n\n" +
"The perceptron computes its output using this formula:\n\n" +
"  OUTPUT = SUM( Switch(i) x Weight(i) ) + Bias\n\n" +
"Where:\n" +
"  - Switch(i) = +1 if ON, -1 if OFF\n" +
"  - Weight(i) = dial value (-30 to +30)\n" +
"  - Bias = bias dial value\n" +
"  - SUM = add up all switch*weight products\n\n" +
"THE LEARNING PROCEDURE\n\n" +
"When you press [Learn +] (want POSITIVE output):\n" +
"  - All dials for ON switches: INCREASE by learn rate\n" +
"  - All dials for OFF switches: DECREASE by learn rate\n" +
"  - Bias: INCREASE by learn rate\n\n" +
"When you press [Learn -] (want NEGATIVE output):\n" +
"  - All dials for ON switches: DECREASE by learn rate\n" +
"  - All dials for OFF switches: INCREASE by learn rate\n" +
"  - Bias: DECREASE by learn rate\n\n" +
"IMPORTANT: Learning only occurs when the output is\n" +
"WRONG. If the perceptron already outputs the correct\n" +
"sign (positive or negative), dials are left unchanged.\n\n" +
"WHEN TO PRESS EACH BUTTON\n\n" +
"Only press Learn when the output is WRONG:\n\n" +
"  Pattern   Desired    Current Output   Action\n" +
"  -------   -------    --------------   ------\n" +
"  T         Positive   Green (on)       Nothing needed\n" +
"  T         Positive   Off              Press Learn+\n" +
"  J         Negative   Green (on)       Press Learn-\n" +
"  J         Negative   Off              Nothing needed\n\n" +
"Simple rule: Only press the Learn button when the\n" +
"output is the OPPOSITE of what you want.\n\n" +
"WHY THIS WORKS\n\n" +
"For patterns you want positive (like T):\n" +
"  - Increasing weights for ON switches makes those\n" +
"    inputs contribute MORE to the sum\n" +
"  - Decreasing weights for OFF switches makes those\n" +
"    inputs contribute LESS\n\n" +
"For patterns you want negative (like J):\n" +
"  - The opposite adjustments push the output negative\n\n" +
"Over multiple training examples, the dials converge\n" +
"to values that correctly classify all patterns.\n\n" +
"THE ROLE OF BIAS\n\n" +
"The bias allows the decision boundary to shift away\n" +
"from the origin, which is essential for problems like\n" +
"AND/OR gates where the threshold is not at zero.\n\n" +
"Think of the bias as a 'base level' that the weighted\n" +
"sum must overcome. Without it, the perceptron would be\n" +
"severely limited in what patterns it could learn.\n\n" +
"This procedure was discovered by Frank Rosenblatt in\n" +
"1957 and is guaranteed to find a solution if one exists.";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowMathDial(), () => ShowThe1960Algorithm(), _pageNumbers["TheAlgorithm"]);
    }

    private void ShowThe1960Algorithm()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("VII. THE 1960 ALGORITHM");

        string content = "THE WIDROW-HOFF LEARNING RULE\n" +
"Also known as: The Delta Rule, LMS (Least Mean Squares)\n\n" +
"DISCOVERERS\n\n" +
"In 1960, Bernard Widrow and his graduate student Ted\n" +
"Hoff at Stanford University introduced the ADALINE\n" +
"(Adaptive Linear Neuron) and a new learning rule that\n" +
"would prove more powerful than Rosenblatt's original\n" +
"perceptron algorithm.\n\n" +
"THE KEY INNOVATION: CONTINUOUS ERROR\n\n" +
"Rosenblatt's 1958 rule asks a simple binary question:\n" +
"\"Am I right or wrong?\" If wrong, adjust. If right,\n" +
"do nothing.\n\n" +
"Widrow and Hoff asked a better question: \"How wrong\n" +
"am I?\" Their rule adjusts weights in proportion to\n" +
"the magnitude of the error, not just its sign.\n\n" +
"THE FORMULA\n\n" +
"  w(t+1) = w(t) + eta x (desired - actual) x input\n\n" +
"Where:\n" +
"  w(t)    = current weight value\n" +
"  eta     = learning rate (controls step size)\n" +
"  desired = target output (+1 or -1)\n" +
"  actual  = current computed output\n" +
"  input   = the input value (+1 or -1)\n\n" +
"The error term (desired - actual) is continuous. A\n" +
"large error produces a large adjustment. A small error\n" +
"produces a small adjustment. This is gradient descent\n" +
"on Mean Squared Error (MSE).\n\n" +
"HOW IT DIFFERS FROM 1958\n\n" +
"  1958 Perceptron        1960 Widrow-Hoff\n" +
"  ----------------       ----------------\n" +
"  Binary error           Continuous error\n" +
"  (wrong or right)       (how much wrong)\n" +
"  Fixed step size        Proportional step\n" +
"  Updates only when      Updates even when\n" +
"  wrong                  close to correct\n" +
"  Converges if           Minimizes MSE\n" +
"  separable              (smoother learning)\n\n" +
"MEAN SQUARED ERROR MINIMIZATION\n\n" +
"The Widrow-Hoff rule performs gradient descent on the\n" +
"MSE surface. Imagine a landscape of hills and valleys\n" +
"where elevation represents error. The algorithm always\n" +
"moves downhill toward the lowest point, taking steps\n" +
"proportional to the steepness of the slope.\n\n" +
"HISTORICAL CONTEXT\n\n" +
"The ADALINE found immediate practical application in\n" +
"telephone echo cancellation, where it adaptively\n" +
"filtered echoes from long-distance calls. This was\n" +
"one of the first commercial uses of neural networks.\n\n" +
"Hoff later went on to co-invent the microprocessor\n" +
"at Intel (the 4004 chip in 1971).\n\n" +
"ON THE MATH DIAL\n\n" +
"Position: 9 o'clock, labeled \"1960\"\n" +
"Topology: 1-to-1 connectivity (same as classic 1958)\n" +
"Learning: Continuous error, MSE gradient descent";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowAlgorithm(), () => ShowThe1986Algorithm(), _pageNumbers["The1960Algorithm"]);
    }

    private void ShowThe1986Algorithm()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("VIII. THE 1986 ALGORITHM");

        string content = "BACKPROPAGATION\n" +
"The algorithm that ended the first AI Winter.\n\n" +
"DISCOVERERS\n\n" +
"In 1986, David Rumelhart, Geoffrey Hinton, and Ronald\n" +
"Williams published \"Learning representations by\n" +
"back-propagating errors\" in Nature, demonstrating how\n" +
"to train multi-layer neural networks. (Paul Werbos\n" +
"described the mathematical foundation in his 1974 PhD\n" +
"thesis, but it was the 1986 paper that ignited the\n" +
"field.)\n\n" +
"THE PROBLEM: CREDIT ASSIGNMENT\n\n" +
"The 1958 perceptron and 1960 ADALINE are single-layer\n" +
"networks. Their weights directly connect inputs to the\n" +
"output, so when the output is wrong, it is obvious\n" +
"which weights to blame.\n\n" +
"But what if you add a HIDDEN LAYER between input and\n" +
"output? Now the hidden weights do not directly touch\n" +
"the output. When the network is wrong, how do you\n" +
"know which hidden weights caused the error?\n\n" +
"This is the credit assignment problem, and it blocked\n" +
"progress in neural networks for nearly two decades.\n\n" +
"THE KEY INNOVATION: THE CHAIN RULE\n\n" +
"Backpropagation applies the chain rule of calculus to\n" +
"propagate error backward through the network layers.\n" +
"The error at the output tells us how to adjust the\n" +
"output weights. Those output weight adjustments, in\n" +
"turn, tell us how to adjust the hidden weights.\n\n" +
"ARCHITECTURE\n\n" +
"  INPUT LAYER --> HIDDEN LAYER --> OUTPUT\n" +
"   x[1..N]      h[1..N] (ReLU)    y\n\n" +
"  Forward pass:\n" +
"    z[j] = SUM(W1[j,i] x input[i]) + bias1[j]\n" +
"    h[j] = ReLU(z[j]) = max(0, z[j])\n" +
"    output = SUM(W2[j] x h[j]) + bias\n\n" +
"  Where:\n" +
"    W1[j,i] = weight from input i to hidden node j\n" +
"    W2[j]   = weight from hidden node j to output\n" +
"    ReLU    = Rectified Linear Unit activation\n\n" +
"THE BACKWARD PASS\n\n" +
"  1. Compute output error:\n" +
"     error = desired - actual\n\n" +
"  2. Update output weights:\n" +
"     W2[j] += eta x error x h[j]\n\n" +
"  3. Propagate error to hidden layer:\n" +
"     delta[j] = W2[j] x error x ReLU'(z[j])\n\n" +
"  4. Update hidden weights:\n" +
"     W1[j,i] += eta x delta[j] x input[i]\n\n" +
"  ReLU'(z) = 1 if z > 0, else 0\n" +
"  (The derivative is simply: was the neuron active?)\n\n" +
"WHY IT MATTERS\n\n" +
"Single-layer perceptrons cannot learn XOR or any\n" +
"pattern that is not linearly separable. Minsky and\n" +
"Papert proved this in 1969, triggering the first\n" +
"AI Winter as funding dried up.\n\n" +
"Backpropagation solved this by enabling multi-layer\n" +
"networks where hidden layers can learn intermediate\n" +
"representations. The hidden layer can discover\n" +
"features that make the problem linearly separable\n" +
"at the output layer.\n\n" +
"This breakthrough eventually led to modern deep\n" +
"learning: networks with many hidden layers trained\n" +
"by the same fundamental algorithm.\n\n" +
"ON THE MATH DIAL\n\n" +
"Position: 10 o'clock, labeled \"1986\"\n" +
"Topology: Fully connected, multi-layer (MLP)\n" +
"Activation: ReLU in hidden layer\n" +
"Learning: Full backpropagation through hidden layer";

        AddScrollableContent(content, 75);
        SetNavigation(() => ShowThe1960Algorithm(), () => ShowCredits(), _pageNumbers["The1986Algorithm"]);
    }

    private void ShowCredits()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("VII. CREDITS / ABOUT");

        int y = 85;

        var ack = CreateTypewriterLabel("ACKNOWLEDGMENTS", 10, FontStyle.Bold);
        ack.Location = new Point(35, y);
        _paper.Controls.Add(ack);
        y += 25;

        var content = CreateTypewriterLabel(
@"This simulator was inspired by the excellent
educational video by Welch Labs explaining the
original Mark I Perceptron and its learning
algorithm.", 9, FontStyle.Regular);
        content.Location = new Point(35, y);
        content.AutoSize = true;
        _paper.Controls.Add(content);
        y += 85;

        var videoLabel = CreateTypewriterLabel("Watch the Video:", 9, FontStyle.Bold);
        videoLabel.Location = new Point(35, y);
        _paper.Controls.Add(videoLabel);
        y += 20;

        var videoLink = CreateLinkLabel("https://www.youtube.com/watch?v=l-9ALe3U-Fg");
        videoLink.Location = new Point(35, y);
        _paper.Controls.Add(videoLink);
        y += 30;

        var manualLabel = CreateTypewriterLabel("Original Technical Manual:", 9, FontStyle.Bold);
        manualLabel.Location = new Point(35, y);
        _paper.Controls.Add(manualLabel);
        y += 20;

        var manualLink = CreateLinkLabel("https://apps.dtic.mil/sti/tr/pdf/AD0236965.pdf");
        manualLink.Location = new Point(35, y);
        _paper.Controls.Add(manualLink);
        y += 45;

        var createdLabel = CreateTypewriterLabel("CREATED BY", 10, FontStyle.Bold);
        createdLabel.Location = new Point(35, y);
        _paper.Controls.Add(createdLabel);
        y += 25;

        var author = CreateTypewriterLabel("S. Rives with full use of Claude Code AI", 9, FontStyle.Regular);
        author.Location = new Point(35, y);
        _paper.Controls.Add(author);
        y += 30;

        var pubNote = CreateTypewriterLabel(
@"See, ""Prediction of Atomic Ionization
Potentials I-III Using an Artificial Neural
Network"", Journal of Chemical Information
and Computer Sciences, 1994, 34, 617-620,
for which, S. Rives wrote the associated
software.", 8, FontStyle.Regular);
        pubNote.Location = new Point(35, y);
        pubNote.AutoSize = true;
        _paper.Controls.Add(pubNote);

        // Render journal title in italic overlay
        var journalItalic = new Label
        {
            Text = "Journal of Chemical Information",
            Font = new Font("Courier New", 8f, FontStyle.Italic),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.FromArgb(242, 238, 225),
            AutoSize = true
        };
        // Position over the 3rd line of pubNote (line index 2, ~26px down)
        journalItalic.Location = new Point(40, y + 26);
        _paper.Controls.Add(journalItalic);
        journalItalic.BringToFront();

        var journalItalic2 = new Label
        {
            Text = "and Computer Sciences",
            Font = new Font("Courier New", 8f, FontStyle.Italic),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.FromArgb(242, 238, 225),
            AutoSize = true
        };
        journalItalic2.Location = new Point(40, y + 39);
        _paper.Controls.Add(journalItalic2);
        journalItalic2.BringToFront();

        y += 90;

        var githubLabel = CreateTypewriterLabel("GitHub: ", 9, FontStyle.Regular);
        githubLabel.Location = new Point(35, y);
        _paper.Controls.Add(githubLabel);

        var githubLink = CreateLinkLabel("https://github.com/srives/Perceptron");
        githubLink.Font = new Font("Courier New", 9f);
        githubLink.Location = new Point(35 + githubLabel.Width, y + 2);
        _paper.Controls.Add(githubLink);

        SetNavigation(() => ShowThe1986Algorithm(), () => ShowBuildItYourself(), _pageNumbers["Credits"]);
    }

    private void ShowBuildItYourself()
    {
        CreatePageStructure(true);
        AddTocLinkAndTitle("IX. BUILD IT YOURSELF");

        int y = 85;

        var header = CreateTypewriterLabel("AI-GENERATED SOFTWARE", 10, FontStyle.Bold);
        header.Location = new Point(35, y);
        _paper.Controls.Add(header);
        y += 30;

        var content = CreateTypewriterLabel(
@"This program was built 100% with AI through
prompts as an homage to the pioneers of Neural
Networks.

You can take the exact same prompts I used,
feed them into Claude Code, and have it build
a replica of this program.

Copy the accompanying perceptrons.png to
c:\temp\, then feed the text from prompt.txt
into Claude Code to recreate this program.

I predict, one day, we won't share code, we
will share prompts.", 9, FontStyle.Regular);
        content.Location = new Point(35, y);
        content.AutoSize = true;
        _paper.Controls.Add(content);

        // Buttons near bottom of paper
        int buttonY = _paper.Height - 70;

        var imageButton = CreatePaperButton("View Reference Image");
        imageButton.Location = new Point(35, buttonY);
        imageButton.Click += (s, e) => OpenResourceFile("perceptrons.png");
        _paper.Controls.Add(imageButton);

        var promptButton = CreatePaperButton("View Prompts");
        promptButton.Location = new Point(210, buttonY);
        promptButton.Click += (s, e) => OpenResourceFile("prompt.txt");
        _paper.Controls.Add(promptButton);

        SetNavigation(() => ShowCredits(), null, _pageNumbers["BuildItYourself"]);
    }

    private void AddScrollableContent(string text, int topY)
    {
        var scrollPanel = new Panel
        {
            Location = new Point(5, topY),
            Size = new Size(_paper.Width - 10, _paper.Height - topY - 10),
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        var contentLabel = new Label
        {
            Text = text,
            Font = new Font("Courier New", 9f),
            ForeColor = Color.FromArgb(30, 30, 30),
            BackColor = Color.Transparent,
            Location = new Point(30, 5),
            MaximumSize = new Size(_paper.Width - 80, 0),
            AutoSize = true
        };

        scrollPanel.Controls.Add(contentLabel);
        _paper.Controls.Add(scrollPanel);
    }

    private void OpenResourceFile(string filename)
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            string resourceName = $"PerceptronSimulator.resources.{filename}";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                MessageBox.Show($"Resource not found: {resourceName}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Text files: show in a scrollable dialog directly from the embedded resource
            if (filename.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(stream);
                string text = reader.ReadToEnd();
                ShowTextResourceDialog(filename, text);
                return;
            }

            // Binary files (images, etc.): extract to temp and open with default app
            string tempPath = Path.Combine(Path.GetTempPath(), filename);
            using (var fileStream = File.Create(tempPath))
            {
                stream.CopyTo(fileStream);
            }

            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private Form? _textResourceDialog;

    private void ShowTextResourceDialog(string title, string text)
    {
        if (_textResourceDialog != null && !_textResourceDialog.IsDisposed)
        {
            _textResourceDialog.BringToFront();
            return;
        }

        const int titleBarHeight = 30;
        bool isDragging = false;
        Point dragStart = Point.Empty;

        var dlg = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.None,
            BackColor = Color.FromArgb(20, 20, 20),
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(700, 600),
            ShowInTaskbar = false
        };
        dlg.Paint += (s, e) =>
        {
            using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 1);
            e.Graphics.DrawRectangle(borderPen, 0, 0, dlg.Width - 1, dlg.Height - 1);
        };

        // Title bar
        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = titleBarHeight,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isDragging = true; dragStart = e.Location; } };
        titleBar.MouseMove += (s, e) => { if (isDragging) { var p = titleBar.PointToScreen(e.Location); dlg.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y); } };
        titleBar.MouseUp += (s, e) => isDragging = false;

        var titleLabel = new Label
        {
            Text = title.ToUpper(),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Consolas", 10f, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.Transparent
        };
        titleLabel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { isDragging = true; dragStart = new Point(e.X + titleLabel.Left, e.Y + titleLabel.Top); } };
        titleLabel.MouseMove += (s, e) => { if (isDragging) { var p = titleLabel.PointToScreen(e.Location); dlg.Location = new Point(p.X - dragStart.X, p.Y - dragStart.Y); } };
        titleLabel.MouseUp += (s, e) => isDragging = false;

        var closeButton = new Button
        {
            Text = "X",
            Size = new Size(40, titleBarHeight),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 60, 60),
            Font = new Font("Consolas", 12f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 40);
        closeButton.Click += (s, e) => dlg.Close();

        titleBar.Controls.Add(closeButton);
        titleBar.Controls.Add(titleLabel);
        titleBar.Resize += (s, e) =>
        {
            titleLabel.Location = new Point(
                (titleBar.Width - titleLabel.Width) / 2,
                (titleBarHeight - titleLabel.Height) / 2);
        };

        var textBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            Text = text,
            ReadOnly = true,
            BackColor = Color.FromArgb(35, 35, 35),
            ForeColor = Color.FromArgb(200, 200, 180),
            Font = new Font("Courier New", 9f),
            BorderStyle = BorderStyle.None,
            WordWrap = true
        };

        dlg.Controls.Add(textBox);
        dlg.Controls.Add(titleBar);
        _textResourceDialog = dlg;
        dlg.Show(this);
    }

    private Button CreatePaperButton(string text)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(160, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(215, 210, 200),
            ForeColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Courier New", 9f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(120, 115, 105);
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(195, 190, 180);
        return btn;
    }

    private LinkLabel CreateLinkLabel(string url)
    {
        var link = new LinkLabel
        {
            Text = url,
            AutoSize = true,
            Font = new Font("Courier New", 8f),
            LinkColor = Color.FromArgb(40, 40, 120),
            VisitedLinkColor = Color.FromArgb(80, 40, 120)
        };
        link.Click += (s, e) =>
        {
            try { Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true }); }
            catch { }
        };
        return link;
    }

    private void AddTocLinkAndTitle(string title)
    {
        var tocLink = CreateTypewriterLabel("[TABLE OF CONTENTS]", 8, FontStyle.Regular);
        tocLink.Location = new Point(35, 12);
        tocLink.ForeColor = Color.FromArgb(40, 40, 120);
        tocLink.Cursor = Cursors.Hand;
        tocLink.Click += (s, e) => ShowTableOfContents();
        tocLink.MouseEnter += (s, e) => tocLink.Font = new Font(tocLink.Font, FontStyle.Underline);
        tocLink.MouseLeave += (s, e) => tocLink.Font = new Font(tocLink.Font, FontStyle.Regular);
        _paper.Controls.Add(tocLink);

        var titleLabel = CreateTypewriterLabel(title, 12, FontStyle.Bold);
        titleLabel.Location = new Point(35, 40);
        _paper.Controls.Add(titleLabel);
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

    private void VideoTutorial_Click(object? sender, EventArgs e)
    {
        try { Process.Start(new ProcessStartInfo { FileName = "https://www.youtube.com/watch?v=l-9ALe3U-Fg", UseShellExecute = true }); }
        catch { }
    }
    #endregion

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }
}

public class PaperPanel : Panel
{
    public PaperPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        BackColor = Color.FromArgb(242, 238, 225);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.FillRectangle(new SolidBrush(Color.FromArgb(242, 238, 225)), ClientRectangle);

        var rand = new Random(42);
        for (int i = 0; i < 600; i++)
        {
            int gray = rand.Next(210, 245);
            using var brush = new SolidBrush(Color.FromArgb(25, gray, gray, gray));
            g.FillRectangle(brush, rand.Next(Width), rand.Next(Height), 1, 1);
        }

        // Draw diagonal "DECLASSIFIED" watermark with subtext
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using var watermarkFont = new Font("Courier New", 42f, FontStyle.Bold);
        using var subtextFont = new Font("Courier New", 10f, FontStyle.Regular);
        using var watermarkBrush = new SolidBrush(Color.FromArgb(40, 140, 60, 60)); // Visible red-brown watermark
        using var subtextBrush = new SolidBrush(Color.FromArgb(95, 140, 60, 60)); // Darker subtext

        var state = g.Save();
        g.TranslateTransform(Width / 2f, Height / 2f);
        g.RotateTransform(-35f); // Diagonal angle

        string watermarkText = "DECLASSIFIED";
        string subtextLine = "Declassified in 2005 as per the";
        string subtextLine2 = "1995 Executive Order 12958 3.4";

        var textSize = g.MeasureString(watermarkText, watermarkFont);
        var subSize1 = g.MeasureString(subtextLine, subtextFont);
        var subSize2 = g.MeasureString(subtextLine2, subtextFont);

        float mainY = -textSize.Height / 2 - 10;
        g.DrawString(watermarkText, watermarkFont, watermarkBrush, -textSize.Width / 2, mainY);
        g.DrawString(subtextLine, subtextFont, subtextBrush, -subSize1.Width / 2, mainY + textSize.Height - 5);
        g.DrawString(subtextLine2, subtextFont, subtextBrush, -subSize2.Width / 2, mainY + textSize.Height + subSize1.Height - 10);

        g.Restore(state);

        using var edgeBrush = new SolidBrush(Color.FromArgb(12, 0, 0, 0));
        g.FillRectangle(edgeBrush, 0, 0, 2, Height);
        g.FillRectangle(edgeBrush, Width - 2, 0, 2, Height);
        g.FillRectangle(edgeBrush, 0, 0, Width, 2);
        g.FillRectangle(edgeBrush, 0, Height - 2, Width, 2);

        using var borderPen = new Pen(Color.FromArgb(180, 175, 165), 1);
        g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);

        base.OnPaint(e);
    }
}
