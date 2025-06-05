using NotebookAutomation.Core.Models;

using Spectre.Console;

namespace NotebookAutomation.Cli.Utilities;

/// <summary>
/// Provides helper methods for consistent ANSI-colored console output in CLI commands.
/// </summary>
/// <remarks>
/// This class includes methods for writing colored messages, managing spinner animations,
/// and integrating Spectre.Console progress displays for batch operations.
/// </remarks>
public static class AnsiConsoleHelper
{
    private static readonly char[] _spinnerChars = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];
    private static int _spinnerIndex = 0;
    private static bool _spinnerActive = false;
    private static CancellationTokenSource? _spinnerCancellation;
    private static readonly Lock _spinnerLock = new();
    private static string _currentSpinnerMessage = string.Empty;

    /// <summary>
    /// Writes a usage/help message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="usage">The usage string (e.g., command syntax).</param>
    /// <param name="description">A short description of the command or usage.</param>
    /// <param name="options">Optional options/help text.</param>
    public static void WriteUsage(string usage, string description, string? options = null)
    {
        Console.WriteLine($"{AnsiColors.OKCYAN}{usage}{AnsiColors.ENDC}");
        Console.WriteLine("");
        Console.WriteLine($"{AnsiColors.BOLD}{description}{AnsiColors.ENDC}");
        if (!string.IsNullOrWhiteSpace(options))
        {
            Console.WriteLine("");
            Console.WriteLine($"{AnsiColors.OKBLUE}Options:{AnsiColors.ENDC}");
            Console.WriteLine(options);
        }
    }

    /// <summary>
    /// Writes a status/info message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteInfo(string message)
    {
        Console.WriteLine($"{AnsiColors.OKBLUE}{message}{AnsiColors.ENDC}");
    }

    /// <summary>
    /// Writes a warning message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="message">The warning message to write.</param>
    public static void WriteWarning(string message)
    {
        Console.WriteLine($"{AnsiColors.WARNING}{message}{AnsiColors.ENDC}");
    }

    /// <summary>
    /// Writes an error message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="message">The error message to write.</param>
    public static void WriteError(string message)
    {
        Console.WriteLine($"{AnsiColors.FAIL}{message}{AnsiColors.ENDC}");
    }

    /// <summary>
    /// Writes a success message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="message">The success message to write.</param>
    public static void WriteSuccess(string message)
    {
        Console.WriteLine($"{AnsiColors.OKGREEN}{message}{AnsiColors.ENDC}");
    }

    /// <summary>
    /// Writes a heading to the console with a consistent color scheme.
    /// </summary>
    /// <param name="heading">The heading text to write.</param>
    internal static void WriteHeading(string heading)
    {
        Console.WriteLine($"{AnsiColors.HEADER}{heading}{AnsiColors.ENDC}");
    }

    /// <summary>
    /// Writes a key-value pair to the console with a consistent color scheme.
    /// </summary>
    /// <param name="key">The key to display.</param>
    /// <param name="value">The value to display.</param>
    internal static void WriteKeyValue(string key, string value)
    {
        Console.WriteLine($"{AnsiColors.OKCYAN}{key}:{AnsiColors.ENDC} {value}");
    }

    /// <summary>
    /// Starts a spinner animation with a message to indicate ongoing processing.
    /// </summary>
    /// <param name="message">The message to display alongside the spinner.</param>
    public static void StartSpinner(string message)
    {
        lock (_spinnerLock)
        {
            if (_spinnerActive)
            {
                StopSpinner();
            }

            _spinnerActive = true;
            _currentSpinnerMessage = message;
            _spinnerCancellation = new CancellationTokenSource();
            var token = _spinnerCancellation.Token;

            // Write the initial message without a newline
            Console.Write($"{AnsiColors.OKBLUE}{_spinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");

            // Ensure stdout is flushed immediately so the spinner is visible
            Console.Out.Flush(); Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(100);

                    lock (_spinnerLock)
                    {
                        if (token.IsCancellationRequested)
                            break;

                        try
                        {
                            // Use ANSI escape code to clear the current line and return to beginning
                            Console.Write("\r");
                            // Clear the entire line with spaces
                            Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));                                // Return to start of line again
                            Console.Write("\r");
                            // Print updated spinner and message
                            Console.Write($"{AnsiColors.OKBLUE}{_spinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");

                            _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;
                        }
                        catch (Exception)
                        {
                            // Swallow any console errors and continue
                        }
                    }

                    Thread.Sleep(100);
                }
            }, token);
        }
    }

    /// <summary>
    /// Stops the spinner animation and clears the line.
    /// </summary>
    public static void StopSpinner()
    {
        lock (_spinnerLock)
        {
            if (_spinnerActive)
            {
                _spinnerCancellation?.Cancel();
                _spinnerActive = false;

                try
                {
                    // Clear the spinner line using ANSI escape codes
                    Console.Write("\r");
                    Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));
                    Console.Write("\r");
                }
                catch
                {
                    // If console operations fail, just add a line break
                }

                // Always write a new line to ensure clean state for next output
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Updates the spinner message while keeping the animation running.
    /// </summary>
    /// <param name="message">The new message to display.</param>
    public static void UpdateSpinnerMessage(string message)
    {
        lock (_spinnerLock)
        {
            if (_spinnerActive)
            {
                _currentSpinnerMessage = message;

                try
                {
                    // Update the message in-place using ANSI escape codes
                    Console.Write("\r");
                    Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));
                    Console.Write("\r");
                    Console.Write($"{AnsiColors.OKBLUE}{_spinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");
                }
                catch
                {
                    // If console operations fail, just continue silently
                }
            }
        }
    }

    #region Spectre.Console Async Progress Support

    /// <summary>
    /// Executes a task with a Spectre.Console async progress display for batch processing operations.
    /// </summary>
    /// <typeparam name="T">The return type of the task.</typeparam>
    /// <param name="task">The task to execute with progress display.</param>
    /// <param name="description">Description of the operation being performed.</param>
    /// <param name="progressUpdater">Callback to update progress based on queue state.</param>
    /// <returns>The result of the executed task.</returns>
    public static async Task<T> WithProgressAsync<T>(
        Func<IProgress<(string status, int current, int total)>, Task<T>> task,
        string description,
        Action<ProgressTask>? progressUpdater = null)
    {
        return await Spectre.Console.AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),    // Task description
                new ProgressBarColumn(),        // Progress bar
                new PercentageColumn(),         // Percentage
                new RemainingTimeColumn(),      // Remaining time
                new SpinnerColumn(),            // Spinner
            })
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask(description);
                progressUpdater?.Invoke(progressTask);

                var progress = new Progress<(string status, int current, int total)>(update =>
                {
                    progressTask.Description = update.status;
                    if (update.total > 0)
                    {
                        progressTask.MaxValue = update.total;
                        progressTask.Value = update.current;
                    }
                });

                return await task(progress);
            });
    }

    /// <summary>
    /// Executes a task with a simple Spectre.Console spinner.
    /// </summary>
    /// <typeparam name="T">The return type of the task.</typeparam>
    /// <param name="taskFunc">The task function to execute with spinner display.</param>
    /// <param name="statusMessage">The status message to display.</param>
    /// <param name="spinnerType">The type of spinner animation to use.</param>
    /// <param name="style">The style to apply to the spinner.</param>
    /// <returns>The result of the executed task.</returns>
    public static async Task<T> WithSpinnerAsync<T>(
        Func<Task<T>> taskFunc,
        string statusMessage,
        Spinner? spinnerType = null,
        Style? style = null)
    {
        return await Spectre.Console.AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(statusMessage, async ctx =>
            {
                return await taskFunc();
            });
    }

    /// <summary>
    /// Executes a void task with a simple Spectre.Console spinner.
    /// </summary>
    /// <param name="taskFunc">The task function to execute with spinner display.</param>
    /// <param name="statusMessage">The status message to display.</param>
    /// <param name="spinnerType">The type of spinner animation to use.</param>
    /// <param name="style">The style to apply to the spinner.</param>
    public static async Task WithSpinnerAsync(
        Func<Task> taskFunc,
        string statusMessage,
        Spinner? spinnerType = null,
        Style? style = null)
    {
        await Spectre.Console.AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(statusMessage, async ctx =>
            {
                await taskFunc();
            });
    }

    /// <summary>
    /// Executes a task with a Spectre.Console status display that can be dynamically updated.
    /// </summary>
    /// <typeparam name="T">The return type of the task.</typeparam>
    /// <param name="task">The task to execute with status display.</param>
    /// <param name="initialStatus">The initial status message.</param>
    /// <param name="statusUpdater">Callback to update status during execution.</param>
    /// <param name="spinnerType">The type of spinner animation to use.</param>
    /// <param name="style">The style to apply to the status display.</param>
    /// <returns>The result of the executed task.</returns>
    public static async Task<T> WithStatusAsync<T>(
        Func<Action<string>, Task<T>> task,
        string initialStatus,
        Spinner? spinnerType = null,
        Style? style = null)
    {
        return await Spectre.Console.AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(initialStatus, async ctx =>
            {
                void UpdateStatus(string newStatus) => ctx.Status(newStatus);
                return await task(UpdateStatus);
            });
    }

    /// <summary>
    /// Creates a progress reporter that integrates with batch processor events.
    /// </summary>
    /// <param name="description">Description of the batch operation.</param>
    /// <param name="onProgressStarted">Callback when progress display starts.</param>
    /// <returns>A progress reporter that can be used with batch processor events.</returns>
    public static IBatchProgressReporter CreateBatchProgressReporter(
        string description,
        Action<ProgressTask>? onProgressStarted = null)
    {
        return new SpectreConsoleBatchProgressReporter(description, onProgressStarted);
    }

    #endregion

    #region Internal Progress Reporter Implementation

    /// <summary>
    /// Interface for batch progress reporting that integrates with our eventing system.
    /// </summary>
    public interface IBatchProgressReporter : IDisposable
    {
        /// <summary>
        /// Starts the progress display.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Updates progress based on processing progress event.
        /// </summary>
        /// <param name="args">Progress event arguments.</param>
        void UpdateProgress(DocumentProcessingProgressEventArgs args);

        /// <summary>
        /// Updates display based on queue changed event.
        /// </summary>
        /// <param name="args">Queue changed event arguments.</param>
        void UpdateQueue(QueueChangedEventArgs args);

        /// <summary>
        /// Stops the progress display.
        /// </summary>
        Task StopAsync();
    }

    /// <summary>
    /// Internal implementation of batch progress reporter using Spectre.Console.
    /// </summary>
    private class SpectreConsoleBatchProgressReporter : IBatchProgressReporter
    {
        private readonly string _description;
        private readonly Action<ProgressTask>? _onProgressStarted;
        private ProgressContext? _context;
        private ProgressTask? _task;
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public SpectreConsoleBatchProgressReporter(string description, Action<ProgressTask>? onProgressStarted)
        {
            _description = description;
            _onProgressStarted = onProgressStarted;
        }
        public async Task StartAsync()
        {
            await Spectre.Console.AnsiConsole.Progress()
                .AutoRefresh(true)
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn(),            // Spinner
                })
                .StartAsync(async ctx =>
                {
                    _context = ctx;
                    _task = ctx.AddTask(_description);
                    _onProgressStarted?.Invoke(_task);

                    try
                    {
                        // Wait until cancelled (progress will be updated via events)
                        while (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await Task.Delay(100, _cancellationTokenSource.Token);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when StopAsync() is called, just exit gracefully
                    }
                });
        }
        public void UpdateProgress(DocumentProcessingProgressEventArgs args)
        {
            if (_task != null)
            {
                // Escape the status text to prevent Spectre.Console from interpreting it as markup
                var safeStatus = args.Status.Replace("[", "[[").Replace("]", "]]");
                _task.Description = $"{_description}: {safeStatus}";
                if (args.TotalFiles > 0)
                {
                    _task.MaxValue = args.TotalFiles;
                    _task.Value = args.CurrentFile;
                }
            }
        }

        public void UpdateQueue(QueueChangedEventArgs args)
        {
            if (_task != null && args.Queue.Count > 0)
            {
                var completed = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Completed);
                var failed = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Failed);
                var processing = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Processing);

                _task.Description = $"{_description}: {completed} completed, {failed} failed, {processing} processing";
                _task.MaxValue = args.Queue.Count;
                _task.Value = completed + failed;
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            await Task.Delay(100); // Give time for the progress to clean up
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    #endregion
}
