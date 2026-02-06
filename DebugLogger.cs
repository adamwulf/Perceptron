namespace PerceptronSimulator;

/// <summary>
/// Global debug logger that routes debug output to the Teletype (PrinterDialog).
/// Allows any dialog to output debug information without needing a direct reference.
/// </summary>
public static class DebugLogger
{
    private static PrinterDialog? _printerDialog;

    /// <summary>
    /// Set the active printer dialog for debug output.
    /// </summary>
    public static void SetPrinterDialog(PrinterDialog? dialog)
    {
        _printerDialog = dialog;
    }

    /// <summary>
    /// Log a debug message to the teletype output.
    /// </summary>
    public static void Log(string message)
    {
        _printerDialog?.PrintLine(message);
    }

    /// <summary>
    /// Log a debug message with category and timestamp.
    /// </summary>
    public static void Log(string category, string message)
    {
        _printerDialog?.PrintDebug(category, message);
    }
}
