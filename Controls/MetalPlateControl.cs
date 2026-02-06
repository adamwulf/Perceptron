namespace PerceptronSimulator.Controls;

public class MetalPlateControl : Control
{
    private string[] _lines = Array.Empty<string>();

    public string[] InstructionLines
    {
        get => _lines;
        set
        {
            _lines = value;
            Invalidate();
        }
    }

    public MetalPlateControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.DoubleBuffer |
                 ControlStyles.ResizeRedraw, true);

        Size = new Size(200, 140);
        Font = new Font("Consolas", 8f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        Rectangle plateRect = new Rectangle(2, 2, Width - 4, Height - 4);

        // Metal plate background with brushed metal effect
        using (var plateBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            plateRect,
            Color.FromArgb(75, 80, 75),
            Color.FromArgb(55, 60, 55),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical))
        {
            g.FillRoundedRectangle(plateBrush, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 4);
        }

        // Add subtle horizontal lines for brushed metal texture
        using (var texturePen = new Pen(Color.FromArgb(15, 255, 255, 255), 1))
        {
            for (int y = plateRect.Y + 4; y < plateRect.Bottom - 4; y += 3)
            {
                g.DrawLine(texturePen, plateRect.X + 4, y, plateRect.Right - 4, y);
            }
        }
        using (var texturePen2 = new Pen(Color.FromArgb(10, 0, 0, 0), 1))
        {
            for (int y = plateRect.Y + 5; y < plateRect.Bottom - 4; y += 3)
            {
                g.DrawLine(texturePen2, plateRect.X + 4, y, plateRect.Right - 4, y);
            }
        }

        // Beveled edge - light top/left
        using (var lightPen = new Pen(Color.FromArgb(100, 120, 100), 2))
        {
            g.DrawLine(lightPen, plateRect.X + 2, plateRect.Y + 1, plateRect.Right - 2, plateRect.Y + 1);
            g.DrawLine(lightPen, plateRect.X + 1, plateRect.Y + 2, plateRect.X + 1, plateRect.Bottom - 2);
        }

        // Beveled edge - dark bottom/right
        using (var darkPen = new Pen(Color.FromArgb(35, 40, 35), 2))
        {
            g.DrawLine(darkPen, plateRect.X + 2, plateRect.Bottom - 1, plateRect.Right - 2, plateRect.Bottom - 1);
            g.DrawLine(darkPen, plateRect.Right - 1, plateRect.Y + 2, plateRect.Right - 1, plateRect.Bottom - 2);
        }

        // Outer border
        using (var borderPen = new Pen(Color.FromArgb(40, 45, 40), 1))
        {
            g.DrawRoundedRectangle(borderPen, plateRect.X, plateRect.Y, plateRect.Width, plateRect.Height, 4);
        }

        // Screw holes in corners
        DrawScrew(g, plateRect.X + 8, plateRect.Y + 8);
        DrawScrew(g, plateRect.Right - 14, plateRect.Y + 8);
        DrawScrew(g, plateRect.X + 8, plateRect.Bottom - 14);
        DrawScrew(g, plateRect.Right - 14, plateRect.Bottom - 14);

        // Draw engraved text
        int textY = plateRect.Y + 20;
        int textX = plateRect.X + 20;

        using var shadowBrush = new SolidBrush(Color.FromArgb(25, 30, 25));
        using var engraveBrush = new SolidBrush(Color.FromArgb(180, 185, 175));
        using var textFont = new Font("Consolas", 7.5f, FontStyle.Bold);

        foreach (var line in _lines)
        {
            // Shadow (engraved effect - dark below)
            g.DrawString(line, textFont, shadowBrush, textX + 1, textY + 1);
            // Light text (raised edge of engraving)
            g.DrawString(line, textFont, engraveBrush, textX, textY);
            textY += 14;
        }
    }

    private void DrawScrew(Graphics g, int x, int y)
    {
        int size = 6;

        // Screw hole shadow
        using (var holeBrush = new SolidBrush(Color.FromArgb(25, 30, 25)))
        {
            g.FillEllipse(holeBrush, x, y, size, size);
        }

        // Screw head
        using (var screwBrush = new SolidBrush(Color.FromArgb(50, 55, 50)))
        {
            g.FillEllipse(screwBrush, x + 1, y + 1, size - 2, size - 2);
        }

        // Screw slot
        using (var slotPen = new Pen(Color.FromArgb(30, 35, 30), 1))
        {
            g.DrawLine(slotPen, x + 1, y + size / 2, x + size - 1, y + size / 2);
        }
    }
}
