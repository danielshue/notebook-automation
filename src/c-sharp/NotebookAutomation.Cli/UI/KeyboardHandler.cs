// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Handles keyboard shortcuts and special key combinations in chat mode.
/// </summary>
public class KeyboardHandler
{
    private readonly ILogger<KeyboardHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public KeyboardHandler(ILogger<KeyboardHandler> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Event raised when Ctrl+C is pressed.
    /// </summary>
    public event EventHandler? CancelRequested;

    /// <summary>
    /// Event raised when Ctrl+D is pressed (end of input).
    /// </summary>
    public event EventHandler? EndOfInputRequested;

    /// <summary>
    /// Event raised when Ctrl+L is pressed (clear screen).
    /// </summary>
    public event EventHandler? ClearScreenRequested;

    /// <summary>
    /// Event raised when Up arrow is pressed (history navigation).
    /// </summary>
    public event EventHandler<HistoryNavigationEventArgs>? HistoryNavigationRequested;

    /// <summary>
    /// Event raised when Tab is pressed (auto-complete).
    /// </summary>
    public event EventHandler<AutoCompleteEventArgs>? AutoCompleteRequested;

    /// <summary>
    /// Process a key press and raise appropriate events.
    /// </summary>
    /// <param name="keyInfo">The key press information.</param>
    /// <returns>True if the key was handled, false otherwise.</returns>
    public bool ProcessKey(ConsoleKeyInfo keyInfo)
    {
        // Handle Ctrl key combinations
        if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
        {
            return ProcessControlKey(keyInfo);
        }

        // Handle special keys without modifiers
        return keyInfo.Key switch
        {
            ConsoleKey.UpArrow => HandleHistoryNavigation(HistoryDirection.Previous),
            ConsoleKey.DownArrow => HandleHistoryNavigation(HistoryDirection.Next),
            ConsoleKey.Tab => HandleAutoComplete(keyInfo),
            ConsoleKey.Escape => HandleEscape(),
            _ => false
        };
    }

    /// <summary>
    /// Process Ctrl+key combinations.
    /// </summary>
    private bool ProcessControlKey(ConsoleKeyInfo keyInfo)
    {
        switch (keyInfo.Key)
        {
            case ConsoleKey.C:
                logger.LogDebug("Ctrl+C detected - requesting cancel");
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case ConsoleKey.D:
                logger.LogDebug("Ctrl+D detected - requesting end of input");
                EndOfInputRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case ConsoleKey.L:
                logger.LogDebug("Ctrl+L detected - requesting clear screen");
                ClearScreenRequested?.Invoke(this, EventArgs.Empty);
                return true;

            case ConsoleKey.R:
                logger.LogDebug("Ctrl+R detected - reverse search (not implemented)");
                return false; // Could implement history search

            case ConsoleKey.A:
                // Move to beginning of line - handled by Console
                return false;

            case ConsoleKey.E:
                // Move to end of line - handled by Console
                return false;

            case ConsoleKey.K:
                // Kill to end of line (could implement)
                return false;

            case ConsoleKey.U:
                // Kill to beginning of line (could implement)
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Handle history navigation.
    /// </summary>
    private bool HandleHistoryNavigation(HistoryDirection direction)
    {
        var args = new HistoryNavigationEventArgs(direction);
        HistoryNavigationRequested?.Invoke(this, args);
        return args.Handled;
    }

    /// <summary>
    /// Handle auto-complete request.
    /// </summary>
    private bool HandleAutoComplete(ConsoleKeyInfo keyInfo)
    {
        var isShift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
        var args = new AutoCompleteEventArgs(isShift ? AutoCompleteDirection.Previous : AutoCompleteDirection.Next);
        AutoCompleteRequested?.Invoke(this, args);
        return args.Handled;
    }

    /// <summary>
    /// Handle Escape key.
    /// </summary>
    private bool HandleEscape()
    {
        // Could cancel current operation or clear input
        logger.LogDebug("Escape key pressed");
        return false;
    }

    /// <summary>
    /// Read a line of input with keyboard handling support.
    /// </summary>
    /// <param name="history">Command history for navigation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The input line, or null if cancelled.</returns>
    public async Task<string?> ReadLineAsync(
        IReadOnlyList<string>? history = null,
        CancellationToken cancellationToken = default)
    {
        var buffer = new StringBuilder();
        var historyIndex = history?.Count ?? 0;
        var cursorPosition = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!Console.KeyAvailable)
            {
                await Task.Delay(10, cancellationToken);
                continue;
            }

            var key = Console.ReadKey(intercept: true);

            // Check for cancellation via Ctrl+C
            if (key is { Key: ConsoleKey.C, Modifiers: ConsoleModifiers.Control })
            {
                CancelRequested?.Invoke(this, EventArgs.Empty);
                return null;
            }

            // Check for end of input via Ctrl+D
            if (key is { Key: ConsoleKey.D, Modifiers: ConsoleModifiers.Control })
            {
                if (buffer.Length == 0)
                {
                    EndOfInputRequested?.Invoke(this, EventArgs.Empty);
                    return null;
                }
            }

            // Check for clear screen via Ctrl+L
            if (key is { Key: ConsoleKey.L, Modifiers: ConsoleModifiers.Control })
            {
                ClearScreenRequested?.Invoke(this, EventArgs.Empty);
                continue;
            }

            // Handle Enter
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }

            // Handle Backspace
            if (key.Key == ConsoleKey.Backspace)
            {
                if (cursorPosition > 0)
                {
                    buffer.Remove(cursorPosition - 1, 1);
                    cursorPosition--;
                    RedrawLine(buffer.ToString(), cursorPosition);
                }

                continue;
            }

            // Handle Delete
            if (key.Key == ConsoleKey.Delete)
            {
                if (cursorPosition < buffer.Length)
                {
                    buffer.Remove(cursorPosition, 1);
                    RedrawLine(buffer.ToString(), cursorPosition);
                }

                continue;
            }

            // Handle Home
            if (key.Key == ConsoleKey.Home)
            {
                cursorPosition = 0;
                Console.CursorLeft = 0;
                continue;
            }

            // Handle End
            if (key.Key == ConsoleKey.End)
            {
                cursorPosition = buffer.Length;
                Console.CursorLeft = buffer.Length;
                continue;
            }

            // Handle Left Arrow
            if (key.Key == ConsoleKey.LeftArrow && cursorPosition > 0)
            {
                cursorPosition--;
                Console.CursorLeft--;
                continue;
            }

            // Handle Right Arrow
            if (key.Key == ConsoleKey.RightArrow && cursorPosition < buffer.Length)
            {
                cursorPosition++;
                Console.CursorLeft++;
                continue;
            }

            // Handle Up Arrow (history)
            if (key.Key == ConsoleKey.UpArrow && history != null && historyIndex > 0)
            {
                historyIndex--;
                buffer.Clear();
                buffer.Append(history[historyIndex]);
                cursorPosition = buffer.Length;
                RedrawLine(buffer.ToString(), cursorPosition);
                continue;
            }

            // Handle Down Arrow (history)
            if (key.Key == ConsoleKey.DownArrow && history != null)
            {
                if (historyIndex < history.Count - 1)
                {
                    historyIndex++;
                    buffer.Clear();
                    buffer.Append(history[historyIndex]);
                    cursorPosition = buffer.Length;
                    RedrawLine(buffer.ToString(), cursorPosition);
                }
                else if (historyIndex == history.Count - 1)
                {
                    historyIndex++;
                    buffer.Clear();
                    cursorPosition = 0;
                    RedrawLine(string.Empty, cursorPosition);
                }

                continue;
            }

            // Handle regular character input
            if (!char.IsControl(key.KeyChar))
            {
                buffer.Insert(cursorPosition, key.KeyChar);
                cursorPosition++;

                if (cursorPosition == buffer.Length)
                {
                    Console.Write(key.KeyChar);
                }
                else
                {
                    RedrawLine(buffer.ToString(), cursorPosition);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Redraw the current line with cursor positioning.
    /// </summary>
    private static void RedrawLine(string content, int cursorPosition)
    {
        var currentLeft = Console.CursorLeft;

        // Move to start of line content, clear, and redraw
        Console.CursorLeft = 0;
        Console.Write(new string(' ', Math.Max(currentLeft, content.Length + 1)));
        Console.CursorLeft = 0;
        Console.Write(content);
        Console.CursorLeft = cursorPosition;
    }
}

/// <summary>
/// Direction for history navigation.
/// </summary>
public enum HistoryDirection
{
    /// <summary>Navigate to previous (older) history entry.</summary>
    Previous,

    /// <summary>Navigate to next (newer) history entry.</summary>
    Next
}

/// <summary>
/// Direction for auto-complete navigation.
/// </summary>
public enum AutoCompleteDirection
{
    /// <summary>Navigate to next suggestion.</summary>
    Next,

    /// <summary>Navigate to previous suggestion.</summary>
    Previous
}

/// <summary>
/// Event arguments for history navigation.
/// </summary>
public class HistoryNavigationEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryNavigationEventArgs"/> class.
    /// </summary>
    /// <param name="direction">Navigation direction.</param>
    public HistoryNavigationEventArgs(HistoryDirection direction)
    {
        Direction = direction;
    }

    /// <summary>
    /// Gets the navigation direction.
    /// </summary>
    public HistoryDirection Direction { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the event was handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the history entry to display.
    /// </summary>
    public string? HistoryEntry { get; set; }
}

/// <summary>
/// Event arguments for auto-complete.
/// </summary>
public class AutoCompleteEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoCompleteEventArgs"/> class.
    /// </summary>
    /// <param name="direction">Navigation direction.</param>
    public AutoCompleteEventArgs(AutoCompleteDirection direction)
    {
        Direction = direction;
    }

    /// <summary>
    /// Gets the navigation direction.
    /// </summary>
    public AutoCompleteDirection Direction { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the event was handled.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// Gets or sets the completion text.
    /// </summary>
    public string? CompletionText { get; set; }
}
