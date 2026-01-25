// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Manages accessibility options for chat mode including high contrast,
/// screen reader support, and reduced motion.
/// </summary>
public class AccessibilityOptions
{
    private readonly ILogger<AccessibilityOptions> logger;
    private CopilotAccessibilityConfig config;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessibilityOptions"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="config">Accessibility configuration.</param>
    public AccessibilityOptions(
        ILogger<AccessibilityOptions> logger,
        CopilotAccessibilityConfig? config = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.config = config ?? new CopilotAccessibilityConfig();

        // Auto-detect system accessibility settings
        DetectSystemSettings();
    }

    /// <summary>
    /// Gets or sets a value indicating whether high contrast mode is enabled.
    /// </summary>
    public bool HighContrast
    {
        get => config.HighContrast;
        set => config = config with { HighContrast = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether reduced motion is enabled.
    /// </summary>
    public bool ReducedMotion
    {
        get => config.ReducedMotion;
        set => config = config with { ReducedMotion = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether progress announcements are enabled.
    /// </summary>
    public bool AnnounceProgress
    {
        get => config.AnnounceProgress;
        set => config = config with { AnnounceProgress = value };
    }

    /// <summary>
    /// Gets or sets a value indicating whether screen reader mode is enabled.
    /// </summary>
    public bool ScreenReaderMode { get; set; }

    /// <summary>
    /// Gets the current accessibility configuration.
    /// </summary>
    public CopilotAccessibilityConfig Configuration => config;

    /// <summary>
    /// Detect system-level accessibility settings.
    /// </summary>
    private void DetectSystemSettings()
    {
        try
        {
            // Check for Windows high contrast
            if (OperatingSystem.IsWindows())
            {
                DetectWindowsSettings();
            }

            // Check for terminal-specific settings
            DetectTerminalSettings();

            logger.LogDebug(
                "Detected accessibility settings: HighContrast={HighContrast}, ReducedMotion={ReducedMotion}",
                HighContrast,
                ReducedMotion);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect system accessibility settings");
        }
    }

    /// <summary>
    /// Detect Windows-specific accessibility settings.
    /// </summary>
    private void DetectWindowsSettings()
    {
        // Check HIGH_CONTRAST environment indicator
        var highContrastEnv = Environment.GetEnvironmentVariable("HIGH_CONTRAST");
        if (!string.IsNullOrEmpty(highContrastEnv) &&
            (highContrastEnv == "1" || highContrastEnv.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            config = config with { HighContrast = true };
            logger.LogInformation("High contrast mode detected from environment");
        }

        // Check for screen reader presence
        var screenReader = Environment.GetEnvironmentVariable("SCREEN_READER");
        if (!string.IsNullOrEmpty(screenReader) &&
            (screenReader == "1" || screenReader.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            ScreenReaderMode = true;
            logger.LogInformation("Screen reader mode detected from environment");
        }
    }

    /// <summary>
    /// Detect terminal-specific accessibility settings.
    /// </summary>
    private void DetectTerminalSettings()
    {
        // Check for NO_COLOR (https://no-color.org/)
        var noColor = Environment.GetEnvironmentVariable("NO_COLOR");
        if (noColor != null)
        {
            config = config with { HighContrast = true };
            logger.LogInformation("NO_COLOR environment variable set, enabling high contrast");
        }

        // Check for TERM_PROGRAM specific accessibility
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        if (!string.IsNullOrEmpty(termProgram))
        {
            logger.LogDebug("Running in terminal: {TermProgram}", termProgram);
        }

        // Check for reduced motion preference
        var prefersReducedMotion = Environment.GetEnvironmentVariable("PREFERS_REDUCED_MOTION");
        if (!string.IsNullOrEmpty(prefersReducedMotion) &&
            (prefersReducedMotion == "1" || prefersReducedMotion.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            config = config with { ReducedMotion = true };
            logger.LogInformation("Reduced motion preference detected");
        }
    }

    /// <summary>
    /// Format text for current accessibility settings.
    /// </summary>
    /// <param name="text">The text to format.</param>
    /// <param name="style">The intended style (e.g., "prompt", "response", "error").</param>
    /// <returns>Formatted text appropriate for current settings.</returns>
    public string FormatText(string text, string style)
    {
        if (HighContrast || ScreenReaderMode)
        {
            // Strip any ANSI codes for high contrast / screen reader mode
            return StripAnsiCodes(text);
        }

        return text;
    }

    /// <summary>
    /// Get console colors for the current accessibility settings.
    /// </summary>
    /// <param name="element">The UI element (e.g., "prompt", "response", "error").</param>
    /// <returns>Foreground and background colors.</returns>
    public (ConsoleColor Foreground, ConsoleColor Background) GetColors(string element)
    {
        if (HighContrast)
        {
            return element.ToLowerInvariant() switch
            {
                "prompt" => (ConsoleColor.White, ConsoleColor.Black),
                "response" => (ConsoleColor.White, ConsoleColor.Black),
                "error" => (ConsoleColor.Yellow, ConsoleColor.Black),
                "warning" => (ConsoleColor.Yellow, ConsoleColor.Black),
                "info" => (ConsoleColor.White, ConsoleColor.Black),
                "tool" => (ConsoleColor.Cyan, ConsoleColor.Black),
                "success" => (ConsoleColor.Green, ConsoleColor.Black),
                _ => (ConsoleColor.White, ConsoleColor.Black)
            };
        }

        // Default colors for normal mode
        return element.ToLowerInvariant() switch
        {
            "prompt" => (ConsoleColor.Cyan, Console.BackgroundColor),
            "response" => (ConsoleColor.Green, Console.BackgroundColor),
            "error" => (ConsoleColor.Red, Console.BackgroundColor),
            "warning" => (ConsoleColor.Yellow, Console.BackgroundColor),
            "info" => (ConsoleColor.Gray, Console.BackgroundColor),
            "tool" => (ConsoleColor.Magenta, Console.BackgroundColor),
            "success" => (ConsoleColor.Green, Console.BackgroundColor),
            _ => (Console.ForegroundColor, Console.BackgroundColor)
        };
    }

    /// <summary>
    /// Announce a message for screen readers.
    /// </summary>
    /// <param name="message">The message to announce.</param>
    /// <param name="priority">Announcement priority.</param>
    public void Announce(string message, AnnouncementPriority priority = AnnouncementPriority.Normal)
    {
        if (!AnnounceProgress && priority != AnnouncementPriority.Critical)
        {
            return;
        }

        if (ScreenReaderMode)
        {
            // For screen readers, we output plain text that can be read
            Console.WriteLine($"[{priority}] {message}");
        }

        logger.LogDebug("Announcement ({Priority}): {Message}", priority, message);
    }

    /// <summary>
    /// Get a spinner/progress indicator appropriate for current settings.
    /// </summary>
    /// <returns>Spinner frames or null if reduced motion is enabled.</returns>
    public string[]? GetSpinnerFrames()
    {
        if (ReducedMotion)
        {
            // For reduced motion, return a static indicator
            return ["..."];
        }

        // Standard spinner frames
        return ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    }

    /// <summary>
    /// Write text with appropriate formatting for accessibility.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="element">The UI element type.</param>
    /// <param name="newLine">Whether to add a newline.</param>
    public void Write(string text, string element = "default", bool newLine = true)
    {
        var (foreground, background) = GetColors(element);
        var formattedText = FormatText(text, element);

        var previousForeground = Console.ForegroundColor;
        var previousBackground = Console.BackgroundColor;

        try
        {
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;

            if (newLine)
            {
                Console.WriteLine(formattedText);
            }
            else
            {
                Console.Write(formattedText);
            }
        }
        finally
        {
            Console.ForegroundColor = previousForeground;
            Console.BackgroundColor = previousBackground;
        }
    }

    /// <summary>
    /// Strip ANSI escape codes from text.
    /// </summary>
    private static string StripAnsiCodes(string text)
    {
        // Simple regex to remove ANSI escape sequences
        return System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\x1B\[[0-9;]*[mK]",
            string.Empty);
    }

    /// <summary>
    /// Create a summary of current accessibility settings for display.
    /// </summary>
    /// <returns>Human-readable summary.</returns>
    public string GetSettingsSummary()
    {
        var settings = new List<string>();

        if (HighContrast)
        {
            settings.Add("High Contrast");
        }

        if (ReducedMotion)
        {
            settings.Add("Reduced Motion");
        }

        if (ScreenReaderMode)
        {
            settings.Add("Screen Reader");
        }

        if (AnnounceProgress)
        {
            settings.Add("Progress Announcements");
        }

        return settings.Count > 0
            ? string.Join(", ", settings)
            : "Default";
    }
}

/// <summary>
/// Priority levels for accessibility announcements.
/// </summary>
public enum AnnouncementPriority
{
    /// <summary>Low priority - may be skipped.</summary>
    Low,

    /// <summary>Normal priority.</summary>
    Normal,

    /// <summary>High priority - important updates.</summary>
    High,

    /// <summary>Critical - always announced.</summary>
    Critical
}
