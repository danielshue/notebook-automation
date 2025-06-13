// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Utilities;

/// <summary>
/// Provides ANSI color codes for CLI console output.
/// </summary>
/// <remarks>
/// These constants can be used to format text output in the console with various colors and styles.
/// ANSI escape codes are widely supported in modern terminal emulators.
/// </remarks>
internal static class AnsiColors
{
    /// <summary>
    /// ANSI code for blue background.
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.BG_BLUE + "Text with blue background" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string BGBLUE = "\u001b[44m";

    /// <summary>
    /// ANSI code for bold text.
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.BOLD + "Bold text" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string BOLD = "\u001b[1m";

    /// <summary>
    /// ANSI code to reset all attributes (end color formatting).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine("Normal text" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string ENDC = "\u001b[0m";

    /// <summary>
    /// ANSI code for red foreground (errors or failures).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.FAIL + "Error message" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string FAIL = "\u001b[91m";

    /// <summary>
    /// ANSI code for grey foreground (muted or less important text).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.GREY + "Muted text" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string GREY = "\u001b[90m";

    /// <summary>
    /// ANSI code for magenta foreground (headers or highlights).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.HEADER + "Header text" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string HEADER = "\u001b[95m";

    /// <summary>
    /// ANSI code for blue foreground (informational messages).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.OKBLUE + "Info message" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string OKBLUE = "\u001b[94m";

    /// <summary>
    /// ANSI code for cyan foreground (secondary info).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.OKCYAN + "Secondary info" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string OKCYAN = "\u001b[96m";

    /// <summary>
    /// ANSI code for green foreground (success messages).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.OKGREEN + "Success message" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string OKGREEN = "\u001b[92m";

    /// <summary>
    /// ANSI code for yellow foreground (warnings).
    /// </summary>
    /// <example>
    /// <code>
    /// Console.WriteLine(AnsiColors.WARNING + "Warning message" + AnsiColors.ENDC);
    /// </code>
    /// </example>
    public const string WARNING = "\u001b[93m";
}
