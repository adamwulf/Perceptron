namespace PerceptronSimulator;

/// <summary>
/// Models a real 830-point breadboard with electrical connectivity.
/// Tracks which holes are connected and provides coordinate mapping.
/// </summary>
public class BreadboardModel
{
    // Standard breadboard dimensions
    public const int COLUMNS = 63;  // a-bc-... columns (5-hole groups)
    public const int ROWS_ABOVE_CENTER = 5;  // Rows A-E above center channel
    public const int ROWS_BELOW_CENTER = 5;  // Rows F-J below center channel
    public const int HOLE_SPACING = 10;  // 0.1" = 2.54mm ≈ 10px at this scale

    private readonly int _xOffset;
    private readonly int _yOffset;

    public BreadboardModel(int xOffset, int yOffset)
    {
        _xOffset = xOffset;
        _yOffset = yOffset;
    }

    /// <summary>
    /// Get pixel coordinates for a specific hole.
    /// </summary>
    /// <param name="row">Row letter: 'A'-'E' above center, 'F'-'J' below center</param>
    /// <param name="col">Column number 1-63</param>
    public Point GetHolePosition(char row, int col)
    {
        int x = _xOffset + (col - 1) * HOLE_SPACING;

        int rowIndex = char.ToUpper(row) - 'A';
        int y;

        if (rowIndex < ROWS_ABOVE_CENTER)
        {
            // Rows A-E (above center)
            y = _yOffset + rowIndex * HOLE_SPACING;
        }
        else
        {
            // Rows F-J (below center, skip center channel)
            int centerChannelHeight = 10;
            y = _yOffset + (ROWS_ABOVE_CENTER * HOLE_SPACING) + centerChannelHeight +
                ((rowIndex - ROWS_ABOVE_CENTER) * HOLE_SPACING);
        }

        return new Point(x, y);
    }

    /// <summary>
    /// Get the Y coordinate for power rails.
    /// </summary>
    public int GetPowerRailY(bool isTop)
    {
        if (isTop)
            return _yOffset - 30;  // +5V rail at top
        else
            return _yOffset + (ROWS_ABOVE_CENTER + ROWS_BELOW_CENTER) * HOLE_SPACING + 40;  // GND rail at bottom
    }

    /// <summary>
    /// Get the Y coordinate for the center channel.
    /// </summary>
    public int GetCenterChannelY()
    {
        return _yOffset + (ROWS_ABOVE_CENTER * HOLE_SPACING);
    }

    /// <summary>
    /// Check if two holes are electrically connected.
    /// On a breadboard, vertical columns of 5 holes are connected.
    /// </summary>
    public bool AreHolesConnected(char row1, int col1, char row2, int col2)
    {
        // Must be same column
        if (col1 != col2) return false;

        int r1 = char.ToUpper(row1) - 'A';
        int r2 = char.ToUpper(row2) - 'A';

        // Must be on same side of center channel
        bool both_above = r1 < ROWS_ABOVE_CENTER && r2 < ROWS_ABOVE_CENTER;
        bool both_below = r1 >= ROWS_ABOVE_CENTER && r2 >= ROWS_ABOVE_CENTER;

        return both_above || both_below;
    }

    /// <summary>
    /// Draw a jumper wire between two holes.
    /// </summary>
    public void DrawJumper(Graphics g, Pen pen, char fromRow, int fromCol, char toRow, int toCol,
        int arcHeight = 0)
    {
        Point from = GetHolePosition(fromRow, fromCol);
        Point to = GetHolePosition(toRow, toCol);

        if (arcHeight == 0)
        {
            // Straight line
            g.DrawLine(pen, from, to);
        }
        else
        {
            // Curved jumper wire (more realistic)
            Point[] points = new Point[4];
            points[0] = from;
            points[1] = new Point(from.X, from.Y - arcHeight);
            points[2] = new Point(to.X, to.Y - arcHeight);
            points[3] = to;

            g.DrawCurve(pen, points, 0.5f);
        }

        // Draw connection dots at endpoints
        g.FillEllipse(Brushes.Black, from.X - 2, from.Y - 2, 4, 4);
        g.FillEllipse(Brushes.Black, to.X - 2, to.Y - 2, 4, 4);
    }

    /// <summary>
    /// Draw a component spanning multiple holes.
    /// </summary>
    public Rectangle GetComponentBounds(char row, int colStart, int colEnd, int height = 15)
    {
        Point start = GetHolePosition(row, colStart);
        Point end = GetHolePosition(row, colEnd);

        int width = end.X - start.X;
        return new Rectangle(start.X, start.Y - height / 2, width, height);
    }
}
