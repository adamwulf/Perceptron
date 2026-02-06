namespace PerceptronSimulator.Controls;

public class FlatNumericUpDown : Control
{
    private int _value = 4;
    private int _minimum = 1;
    private int _maximum = 10;
    private readonly Label _valueLabel;
    private readonly Button _upButton;
    private readonly Button _downButton;

    public int Value
    {
        get => _value;
        set
        {
            _value = Math.Clamp(value, _minimum, _maximum);
            _valueLabel.Text = _value.ToString();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; Value = Math.Clamp(_value, _minimum, _maximum); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = value; Value = Math.Clamp(_value, _minimum, _maximum); }
    }

    public event EventHandler? ValueChanged;

    public FlatNumericUpDown()
    {
        BackColor = Color.FromArgb(40, 40, 40);

        _valueLabel = new Label
        {
            Text = _value.ToString(),
            ForeColor = Color.FromArgb(180, 180, 180),
            BackColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Consolas", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(1, 1),
            Size = new Size(30, 20)
        };

        _upButton = CreateSpinButton("\u25B2", true); // ▲
        _downButton = CreateSpinButton("\u25BC", false); // ▼

        Controls.Add(_valueLabel);
        Controls.Add(_upButton);
        Controls.Add(_downButton);

        Size = new Size(50, 22);
    }

    private Button CreateSpinButton(string text, bool isUp)
    {
        var btn = new Button
        {
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.FromArgb(140, 140, 140),
            Font = new Font("Consolas", 5f),
            Size = new Size(18, 11),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
        btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 60, 60);
        btn.Click += (s, e) => Value += isUp ? 1 : -1;
        return btn;
    }

    private void UpdateLayout()
    {
        if (_valueLabel == null || _upButton == null || _downButton == null)
            return;

        int buttonWidth = 18;
        _valueLabel.Location = new Point(1, 1);
        _valueLabel.Size = new Size(Width - buttonWidth - 2, Height - 2);
        _upButton.Location = new Point(Width - buttonWidth - 1, 1);
        _upButton.Size = new Size(buttonWidth, Height / 2 - 1);
        _downButton.Location = new Point(Width - buttonWidth - 1, Height / 2);
        _downButton.Size = new Size(buttonWidth, Height / 2 - 1);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayout();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var borderPen = new Pen(Color.FromArgb(70, 70, 70), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }
}
