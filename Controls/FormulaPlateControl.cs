namespace PerceptronSimulator.Controls;

public class FormulaPlateControl : Control
{
    private string _line1 = "OUTPUT =";
    private string _line2 = "SUM(Switch x Weight) + Bias";

    public string Line1
    {
        get => _line1;
        set
        {
            if (_line1 != value)
            {
                _line1 = value;
                Invalidate();
            }
        }
    }

    public string Line2
    {
        get => _line2;
        set
        {
            if (_line2 != value)
            {
                _line2 = value;
                Invalidate();
            }
        }
    }

    public FormulaPlateControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(190, 38);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle plateRect = new Rectangle(0, 0, Width - 1, Height - 1);

        // Metal plate background
        using (var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            plateRect,
            Color.FromArgb(70, 75, 70),
            Color.FromArgb(50, 55, 50),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(plateBrush, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Subtle brushed texture
        using (var texturePen = new Pen(Color.FromArgb(12, 255, 255, 255), 1))
        {
            for (int y = plateRect.Y + 3; y < plateRect.Bottom - 3; y += 2)
            {
                g.DrawLine(texturePen, plateRect.X + 8, y, plateRect.Right - 8, y);
            }
        }

        // Beveled edge - light top
        using (var lightPen = new Pen(Color.FromArgb(90, 95, 90), 1))
        {
            g.DrawLine(lightPen, plateRect.X + 2, plateRect.Y + 1, plateRect.Right - 2, plateRect.Y + 1);
        }

        // Beveled edge - dark bottom
        using (var darkPen = new Pen(Color.FromArgb(35, 40, 35), 1))
        {
            g.DrawLine(darkPen, plateRect.X + 2, plateRect.Bottom - 1, plateRect.Right - 2, plateRect.Bottom - 1);
        }

        // Border
        using (var borderPen = new Pen(Color.FromArgb(40, 45, 40), 1))
        {
            g.DrawRoundedRectangle(borderPen, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 3);
        }

        // Screw holes in corners
        DrawScrew(g, plateRect.X + 3, plateRect.Y + 3);
        DrawScrew(g, plateRect.Right - 6, plateRect.Y + 3);
        DrawScrew(g, plateRect.X + 3, plateRect.Bottom - 6);
        DrawScrew(g, plateRect.Right - 6, plateRect.Bottom - 6);

        // Engraved text - two lines
        using var titleFont = new Font("Consolas", 7f, FontStyle.Bold);
        using var formulaFont = new Font("Consolas", 6.5f, FontStyle.Bold);

        var line1Size = g.MeasureString(_line1, titleFont);
        var line2Size = g.MeasureString(_line2, formulaFont);

        float line1X = (Width - line1Size.Width) / 2;
        float line2X = (Width - line2Size.Width) / 2;
        float line1Y = 6;
        float line2Y = line1Y + line1Size.Height - 2;

        // Shadow (engraved effect)
        using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 35, 30)))
        {
            g.DrawString(_line1, titleFont, shadowBrush, line1X + 1, line1Y + 1);
            g.DrawString(_line2, formulaFont, shadowBrush, line2X + 1, line2Y + 1);
        }
        // Light text
        using (var textBrush = new SolidBrush(Color.FromArgb(140, 145, 140)))
        {
            g.DrawString(_line1, titleFont, textBrush, line1X, line1Y);
            g.DrawString(_line2, formulaFont, textBrush, line2X, line2Y);
        }
    }

    private void DrawScrew(Graphics g, int x, int y)
    {
        int size = 3;

        using (var holeBrush = new SolidBrush(Color.FromArgb(25, 30, 25)))
        {
            g.FillEllipse(holeBrush, x, y, size, size);
        }

        using (var screwBrush = new SolidBrush(Color.FromArgb(60, 65, 60)))
        {
            g.FillEllipse(screwBrush, x, y, size - 1, size - 1);
        }
    }
}
