namespace PerceptronSimulator;

/// <summary>
/// Bill of Materials dialog showing all components needed to build the perceptron
/// with specific manufacturer part numbers and purchasing information.
/// Uses centralized PartsDatabase for component data.
/// </summary>
public class BOMDialog : Form
{
    private readonly double[] _weights;
    private readonly int _nodeCount;
    private readonly PerceptronSimulator.Controls.ConfigKnob.MathRule _mathRule;

    private DataGridView _bomGrid = null!;
    private Button _closeButton = null!;
    private Button _copyButton = null!;
    private Button _exportButton = null!;
    private Label _totalLabel = null!;

    public BOMDialog(double[] weights, int nodeCount, PerceptronSimulator.Controls.ConfigKnob.MathRule mathRule)
    {
        _weights = weights;
        _nodeCount = nodeCount;
        _mathRule = mathRule;
        InitializeDialog();
        PopulateBOM();
    }

    private void InitializeDialog()
    {
        Text = "Bill of Materials - Perceptron Build";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(35, 35, 35);
        Size = new Size(850, 600);

        // Title
        var titleLabel = new Label
        {
            Text = "BILL OF MATERIALS",
            Font = new Font("Arial", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 220, 100),
            AutoSize = true,
            Location = new Point(20, 15)
        };

        var subtitleLabel = new Label
        {
            Text = $"Perceptron with {_nodeCount} inputs - All parts needed for build",
            Font = new Font("Arial", 9f),
            ForeColor = Color.LightGray,
            AutoSize = true,
            Location = new Point(20, 40)
        };

        // DataGridView for BOM
        _bomGrid = new DataGridView
        {
            Location = new Point(20, 70),
            Size = new Size(ClientSize.Width - 40, ClientSize.Height - 150),
            BackgroundColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.White,
            GridColor = Color.FromArgb(70, 70, 70),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            EnableHeadersVisualStyles = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        // Style the grid
        _bomGrid.DefaultCellStyle.BackColor = Color.FromArgb(45, 45, 45);
        _bomGrid.DefaultCellStyle.ForeColor = Color.White;
        _bomGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 90, 110);
        _bomGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        _bomGrid.DefaultCellStyle.Font = new Font("Consolas", 9f);
        _bomGrid.DefaultCellStyle.Padding = new Padding(5, 3, 5, 3);

        _bomGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
        _bomGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(255, 220, 100);
        _bomGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Arial", 9f, FontStyle.Bold);
        _bomGrid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5);
        _bomGrid.ColumnHeadersHeight = 30;

        // Add columns
        _bomGrid.Columns.Add("Qty", "QTY");
        _bomGrid.Columns.Add("Description", "DESCRIPTION");
        _bomGrid.Columns.Add("Value", "VALUE/SPECS");
        _bomGrid.Columns.Add("Package", "PACKAGE");
        _bomGrid.Columns.Add("PartNumber", "MFR PART NUMBER");
        _bomGrid.Columns.Add("Supplier", "SUPPLIER");
        _bomGrid.Columns.Add("UnitPrice", "UNIT $");

        _bomGrid.Columns["Qty"].Width = 40;
        _bomGrid.Columns["Description"].Width = 150;
        _bomGrid.Columns["Value"].Width = 120;
        _bomGrid.Columns["Package"].Width = 80;
        _bomGrid.Columns["PartNumber"].Width = 150;
        _bomGrid.Columns["Supplier"].Width = 100;
        _bomGrid.Columns["UnitPrice"].Width = 60;

        // Total label
        _totalLabel = new Label
        {
            Font = new Font("Arial", 10f, FontStyle.Bold),
            ForeColor = Color.LimeGreen,
            AutoSize = true,
            Location = new Point(20, ClientSize.Height - 65)
        };

        // Buttons
        _closeButton = new Button
        {
            Text = "Close",
            Size = new Size(80, 30),
            Location = new Point(ClientSize.Width - 100, ClientSize.Height - 45),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        _closeButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
        _closeButton.Click += (s, e) => Close();

        _copyButton = new Button
        {
            Text = "Copy to Clipboard",
            Size = new Size(120, 30),
            Location = new Point(ClientSize.Width - 350, ClientSize.Height - 45),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 80, 60),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        _copyButton.FlatAppearance.BorderColor = Color.FromArgb(80, 100, 80);
        _copyButton.Click += CopyButton_Click;

        _exportButton = new Button
        {
            Text = "Export CSV",
            Size = new Size(100, 30),
            Location = new Point(ClientSize.Width - 220, ClientSize.Height - 45),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 80),
            ForeColor = Color.FromArgb(200, 200, 200)
        };
        _exportButton.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 100);
        _exportButton.Click += ExportButton_Click;

        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(_bomGrid);
        Controls.Add(_totalLabel);
        Controls.Add(_closeButton);
        Controls.Add(_copyButton);
        Controls.Add(_exportButton);
    }

    private void PopulateBOM()
    {
        // Populate BOM based on selected topology
        switch (_mathRule)
        {
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.BACKPROP:
                PopulateBackpropBOM();
                break;

            // All 1958/1960 variants use the same components
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.PERCEPTRON_CLASSIC:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_SUM:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.RULE_1958_DIV_AVG:
            case PerceptronSimulator.Controls.ConfigKnob.MathRule.WIDROW_HOFF:
            default:
                Populate1958BOM();
                break;
        }

        UpdateTotalCost();
    }

    private void Populate1958BOM()
    {
        // Add fixed components from database
        foreach (var (part, qty, _) in PartsDatabase.GetFixedComponents())
        {
            AddComponentToGrid(part, qty);
        }

        // Add per-input components
        foreach (var (part, _) in PartsDatabase.GetPerInputComponents())
        {
            AddComponentToGrid(part, _nodeCount);
        }

        // Add input resistors based on weights (dynamic calculation)
        var resistorGroups = new Dictionary<string, int>();
        foreach (var weight in _weights)
        {
            string valueStr = PartsDatabase.FormatResistorValue(
                PartsDatabase.CalculateInputResistance(weight));

            if (resistorGroups.ContainsKey(valueStr))
                resistorGroups[valueStr]++;
            else
                resistorGroups[valueStr] = 1;
        }

        foreach (var kvp in resistorGroups.OrderBy(k => PartsDatabase.ParseResistorValue(k.Key)))
        {
            var resistor = PartsDatabase.CreateInputResistor(
                PartsDatabase.ParseResistorValue(kvp.Key) > 0
                    ? PartsDatabase.ReferenceResistor / PartsDatabase.ParseResistorValue(kvp.Key)
                    : 0);
            AddComponentToGrid(resistor, kvp.Value);
        }
    }

    private void PopulateBackpropBOM()
    {
        // Backprop is impractical for analog hardware - show explanation
        var note = new PartsDatabase.Component
        {
            Category = "NOTE",
            Description = "Backprop mode is for software simulation only",
            Value = "Not buildable in analog",
            Package = "Digital",
            PartNumber = "N/A",
            Notes = $"Would require {_nodeCount} op-amps for ReLU activation + programmable resistor arrays. Use 1958/1960 mode for analog builds."
        };
        AddComponentToGrid(note, 1);
    }

    private void UpdateTotalCost()
    {
        // Calculate total
        double totalCost = 0;
        foreach (DataGridViewRow row in _bomGrid.Rows)
        {
            int qty = int.Parse(row.Cells["Qty"].Value?.ToString() ?? "0");
            double price = double.Parse(row.Cells["UnitPrice"].Value?.ToString()?.Replace("$", "") ?? "0");
            totalCost += qty * price;
        }

        _totalLabel.Text = $"ESTIMATED TOTAL: ${totalCost:F2} (prices approximate, check suppliers)";
    }

    private void AddComponentToGrid(PartsDatabase.Component part, int quantity)
    {
        _bomGrid.Rows.Add(
            quantity,
            part.Description,
            part.Value,
            part.Package,
            part.PartNumber,
            part.Supplier,
            $"${part.UnitPrice:F2}");
    }

    private void CopyButton_Click(object? sender, EventArgs e)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("QTY\tDESCRIPTION\tVALUE/SPECS\tPACKAGE\tMFR PART NUMBER\tSUPPLIER\tUNIT $");
        sb.AppendLine(new string('-', 100));

        // Data
        foreach (DataGridViewRow row in _bomGrid.Rows)
        {
            var values = new List<string>();
            foreach (DataGridViewCell cell in row.Cells)
            {
                values.Add(cell.Value?.ToString() ?? "");
            }
            sb.AppendLine(string.Join("\t", values));
        }

        sb.AppendLine();
        sb.AppendLine(_totalLabel.Text);

        Clipboard.SetText(sb.ToString());
        MessageBox.Show("BOM copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export Bill of Materials",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = "csv",
            FileName = "perceptron_bom"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("Qty,Description,Value/Specs,Package,Mfr Part Number,Supplier,Unit Price");

            // Data
            foreach (DataGridViewRow row in _bomGrid.Rows)
            {
                var values = new List<string>();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    string val = cell.Value?.ToString() ?? "";
                    // Escape commas and quotes
                    if (val.Contains(",") || val.Contains("\""))
                        val = $"\"{val.Replace("\"", "\"\"")}\"";
                    values.Add(val);
                }
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(dialog.FileName, sb.ToString());
            MessageBox.Show("BOM exported successfully!", "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to export: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
