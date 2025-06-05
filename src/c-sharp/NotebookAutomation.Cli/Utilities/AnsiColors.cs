namespace NotebookAutomation.Cli.Utilities;
/// <summary>
/// Provides ANSI color codes for CLI console output.
/// </summary>
public static class AnsiColors
{
    /// <summary>
    /// ANSI code for blue background.
    /// </summary>
    public const string BG_BLUE = "\u001b[44m";

    /// <summary>
    /// ANSI code for bold text.
    /// </summary>
    public const string BOLD = "\u001b[1m";

    /// <summary>
    /// ANSI code to reset all attributes (end color formatting).
    /// </summary>
    public const string ENDC = "\u001b[0m";

    /// <summary>
    /// ANSI code for red foreground (errors or failures).
    /// </summary>
    public const string FAIL = "\u001b[91m";

    /// <summary>
    /// ANSI code for grey foreground (muted or less important text).
    /// </summary>
    public const string GREY = "\u001b[90m";

    /// <summary>
    /// ANSI code for magenta foreground (headers or highlights).
    /// </summary>
    public const string HEADER = "\u001b[95m";

    /// <summary>
    /// ANSI code for blue foreground (informational messages).
    /// </summary>
    public const string OKBLUE = "\u001b[94m";

    /// <summary>
    /// ANSI code for cyan foreground (secondary info).
    /// </summary>
    public const string OKCYAN = "\u001b[96m";

    /// <summary>
    /// ANSI code for green foreground (success messages).
    /// </summary>
    public const string OKGREEN = "\u001b[92m";

    /// <summary>
    /// ANSI code for yellow foreground (warnings).
    /// </summary>
    public const string WARNING = "\u001b[93m";
}
