// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Handles multi-line input in chat mode, supporting continuation characters
/// and block input modes.
/// </summary>
public class MultiLineInputHandler
{
    private readonly ILogger<MultiLineInputHandler> logger;
    private readonly KeyboardHandler keyboardHandler;

    /// <summary>
    /// Character used to continue input on next line.
    /// </summary>
    public const char ContinuationChar = '\\';

    /// <summary>
    /// Delimiter for block input mode (triple backticks).
    /// </summary>
    public const string BlockDelimiter = "```";

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiLineInputHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="keyboardHandler">Keyboard handler for input.</param>
    public MultiLineInputHandler(
        ILogger<MultiLineInputHandler> logger,
        KeyboardHandler keyboardHandler)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.keyboardHandler = keyboardHandler ?? throw new ArgumentNullException(nameof(keyboardHandler));
    }

    /// <summary>
    /// Read potentially multi-line input from the user.
    /// </summary>
    /// <remarks>
    /// Supports:
    /// - Line continuation with trailing backslash (\)
    /// - Block input mode with triple backticks (```)
    /// - Shift+Enter for soft line breaks (when supported by terminal)
    /// </remarks>
    /// <param name="prompt">The prompt to display.</param>
    /// <param name="history">Optional command history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete input, or null if cancelled.</returns>
    public async Task<string?> ReadInputAsync(
        string prompt,
        IReadOnlyList<string>? history = null,
        CancellationToken cancellationToken = default)
    {
        var lines = new List<string>();
        var isBlockMode = false;
        var isFirstLine = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            // Display appropriate prompt
            var currentPrompt = GetPrompt(isFirstLine, isBlockMode, prompt);
            Console.Write(currentPrompt);

            // Read a line
            var line = await keyboardHandler.ReadLineAsync(
                isFirstLine ? history : null,
                cancellationToken);

            if (line == null)
            {
                // Cancelled
                return null;
            }

            isFirstLine = false;

            // Check for block mode toggle
            if (line.Trim() == BlockDelimiter)
            {
                if (!isBlockMode)
                {
                    // Entering block mode
                    isBlockMode = true;
                    logger.LogDebug("Entering multi-line block mode");
                    continue;
                }
                else
                {
                    // Exiting block mode
                    isBlockMode = false;
                    logger.LogDebug("Exiting multi-line block mode");
                    break;
                }
            }

            // In block mode, add line as-is
            if (isBlockMode)
            {
                lines.Add(line);
                continue;
            }

            // Check for line continuation
            if (line.EndsWith(ContinuationChar))
            {
                // Remove the continuation character and add the line
                lines.Add(line[..^1]);
                continue;
            }

            // Normal line - add and complete input
            lines.Add(line);
            break;
        }

        // Join lines with newlines
        var result = string.Join(Environment.NewLine, lines);
        logger.LogDebug("Read {LineCount} lines of input", lines.Count);
        return result;
    }

    /// <summary>
    /// Parse multi-line input that may have been entered with explicit newlines.
    /// </summary>
    /// <param name="input">The raw input string.</param>
    /// <returns>Processed input with proper line handling.</returns>
    public static string ProcessInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var lines = new List<string>();
        var currentInput = input;

        // Handle escaped newlines (literal \n in input)
        currentInput = currentInput.Replace("\\n", "\n");

        // Handle continuation characters
        var rawLines = currentInput.Split('\n');
        var pendingLine = new StringBuilder();

        foreach (var line in rawLines)
        {
            if (line.EndsWith(ContinuationChar))
            {
                pendingLine.Append(line[..^1]);
            }
            else
            {
                pendingLine.Append(line);
                lines.Add(pendingLine.ToString());
                pendingLine.Clear();
            }
        }

        // Add any remaining pending line
        if (pendingLine.Length > 0)
        {
            lines.Add(pendingLine.ToString());
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Check if the input appears to be multi-line.
    /// </summary>
    /// <param name="input">The input to check.</param>
    /// <returns>True if the input contains newlines.</returns>
    public static bool IsMultiLine(string input)
    {
        return input.Contains('\n') || input.Contains('\r');
    }

    /// <summary>
    /// Get the line count of multi-line input.
    /// </summary>
    /// <param name="input">The input to count.</param>
    /// <returns>The number of lines.</returns>
    public static int GetLineCount(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return 0;
        }

        return input.Split('\n').Length;
    }

    /// <summary>
    /// Get the appropriate prompt for the current state.
    /// </summary>
    private static string GetPrompt(bool isFirstLine, bool isBlockMode, string basePrompt)
    {
        if (isFirstLine)
        {
            return basePrompt;
        }

        if (isBlockMode)
        {
            return "  ... ";
        }

        return "  > ";
    }

    /// <summary>
    /// Validate that multi-line input is well-formed.
    /// </summary>
    /// <param name="input">The input to validate.</param>
    /// <returns>Validation result with any errors.</returns>
    public static MultiLineValidationResult Validate(string input)
    {
        var errors = new List<string>();

        // Check for unclosed block delimiters
        var blockCount = 0;
        foreach (var line in input.Split('\n'))
        {
            if (line.Trim() == BlockDelimiter)
            {
                blockCount++;
            }
        }

        if (blockCount % 2 != 0)
        {
            errors.Add("Unclosed code block (missing closing ```)");
        }

        // Check for trailing continuation character
        if (input.TrimEnd().EndsWith(ContinuationChar))
        {
            errors.Add("Input ends with continuation character without following line");
        }

        return new MultiLineValidationResult(errors.Count == 0, errors);
    }
}

/// <summary>
/// Result of multi-line input validation.
/// </summary>
public record MultiLineValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
