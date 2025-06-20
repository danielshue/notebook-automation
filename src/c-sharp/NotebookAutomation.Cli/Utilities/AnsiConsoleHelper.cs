// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Utilities;

/// <summary>
/// Provides helper methods for consistent ANSI-colored console output in CLI commands.
/// </summary>
/// <remarks>
/// This class includes methods for writing colored messages, managing spinner animations,
/// and integrating Spectre.Console progress displays for batch operations.
/// </remarks>
internal static class AnsiConsoleHelper
{
    private static readonly char[] SpinnerChars = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];
    private static int spinnerIndex = 0;
    private static bool spinnerActive = false;
    private static CancellationTokenSource? spinnerCancellation;
    private static readonly Lock SpinnerLock = new();
    private static string currentSpinnerMessage = string.Empty;

    /// <summary>
    /// Writes a usage/help message to the console with a consistent color scheme.
    /// </summary>
    /// <param name="usage">The usage string (e.g., command syntax).</param>
    /// <param name="description">A short description of the command or usage.</param>
    /// <param name="options">Optional options/help text.</param>
    public static void WriteUsage(string usage, string description, string? options = null)
    {
        Console.WriteLine($"{AnsiColors.OKCYAN}{usage}{AnsiColors.ENDC}");
        Console.WriteLine(string.Empty);
        Console.WriteLine($"{AnsiColors.BOLD}{description}{AnsiColors.ENDC}");
        if (!string.IsNullOrWhiteSpace(options))
        {
            Console.WriteLine(string.Empty);
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
        lock (SpinnerLock)
        {
            if (spinnerActive)
            {
                StopSpinner();
            }

            spinnerActive = true;
            currentSpinnerMessage = message;
            spinnerCancellation = new CancellationTokenSource();
            var token = spinnerCancellation.Token;

            // Write the initial message without a newline
            Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[spinnerIndex]} {currentSpinnerMessage}{AnsiColors.ENDC}");

            // Ensure stdout is flushed immediately so the spinner is visible
            Console.Out.Flush();
            Task.Run(
                () =>
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(100);

                    lock (SpinnerLock)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        try
                        {
                            // Use ANSI escape code to clear the current line and return to beginning
                            Console.Write("\r");

                            // Clear the entire line with spaces
                            Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));                                // Return to start of line again
                            Console.Write("\r");

                            // Print updated spinner and message
                            Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[spinnerIndex]} {currentSpinnerMessage}{AnsiColors.ENDC}");

                            spinnerIndex = (spinnerIndex + 1) % SpinnerChars.Length;
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
        lock (SpinnerLock)
        {
            if (spinnerActive)
            {
                spinnerCancellation?.Cancel();
                spinnerActive = false;

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
        lock (SpinnerLock)
        {
            if (spinnerActive)
            {
                currentSpinnerMessage = message;

                try
                {
                    // Update the message in-place using ANSI escape codes
                    Console.Write("\r");
                    Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));
                    Console.Write("\r");
                    Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[spinnerIndex]} {currentSpinnerMessage}{AnsiColors.ENDC}");
                }
                catch
                {
                    // If console operations fail, just continue silently
                }
            }
        }
    }

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
        return await AnsiConsole.Progress()
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

                return await task(progress).ConfigureAwait(false);
            }).ConfigureAwait(false);
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
        return await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(statusMessage, async ctx =>
            {
                return await taskFunc().ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a void task with a simple Spectre.Console spinner.
    /// </summary>
    /// <param name="taskFunc">The task function to execute with spinner display.</param>
    /// <param name="statusMessage">The status message to display.</param>
    /// <param name="spinnerType">The type of spinner animation to use.</param>
    /// <param name="style">The style to apply to the spinner.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task WithSpinnerAsync(
        Func<Task> taskFunc,
        string statusMessage,
        Spinner? spinnerType = null,
        Style? style = null)
    {
        await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(statusMessage, async ctx =>
            {
                await taskFunc().ConfigureAwait(false);
            }).ConfigureAwait(false);
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
        return await AnsiConsole.Status()
            .AutoRefresh(true)
            .Spinner(spinnerType ?? Spinner.Known.Dots)
            .SpinnerStyle(style ?? new Style(foreground: Color.Blue))
            .StartAsync(initialStatus, async ctx =>
            {
                void UpdateStatus(string newStatus) => ctx.Status(newStatus);
                return await task(UpdateStatus).ConfigureAwait(false);
            }).ConfigureAwait(false);
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

    /// <summary>
    /// Interface for batch progress reporting that integrates with our eventing system.
    /// </summary>

    internal interface IBatchProgressReporter : IDisposable
    {
        /// <summary>
        /// Starts the progress display.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task StopAsync();
    }

    /// <summary>
    /// Internal implementation of batch progress reporter using Spectre.Console.
    /// </summary>

    private class SpectreConsoleBatchProgressReporter : IBatchProgressReporter
    {
        private readonly string description;
        private readonly Action<ProgressTask>? onProgressStarted;
        private ProgressContext? context;
        private ProgressTask? task;
        private readonly CancellationTokenSource cancellationTokenSource = new();

        public SpectreConsoleBatchProgressReporter(string description, Action<ProgressTask>? onProgressStarted)
        {
            this.description = description;
            this.onProgressStarted = onProgressStarted;
        }

        public async Task StartAsync()
        {
            await AnsiConsole.Progress()
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
                    context = ctx;
                    task = ctx.AddTask(description);
                    onProgressStarted?.Invoke(task);

                    try
                    {
                        // Wait until cancelled (progress will be updated via events)
                        while (!cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when StopAsync() is called, just exit gracefully
                    }
                }).ConfigureAwait(false);
        }

        public void UpdateProgress(DocumentProcessingProgressEventArgs args)
        {
            if (task != null)
            {
                // Escape the status text to prevent Spectre.Console from interpreting it as markup
                var safeStatus = args.Status.Replace("[", "[[").Replace("]", "]]");
                task.Description = $"{description}: {safeStatus}";
                if (args.TotalFiles > 0)
                {
                    task.MaxValue = args.TotalFiles;
                    task.Value = args.CurrentFile;
                }
            }
        }

        public void UpdateQueue(QueueChangedEventArgs args)
        {
            if (task != null && args.Queue.Count > 0)
            {
                var completed = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Completed);
                var failed = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Failed);
                var processing = args.Queue.Count(q => q.Status == DocumentProcessingStatus.Processing);

                task.Description = $"{description}: {completed} completed, {failed} failed, {processing} processing";
                task.MaxValue = args.Queue.Count;
                task.Value = completed + failed;
            }
        }

        public async Task StopAsync()
        {
            cancellationTokenSource.Cancel();
            await Task.Delay(100).ConfigureAwait(false); // Give time for the progress to clean up
        }

        public void Dispose()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
        }
    }
}
