namespace NotebookAutomation.Cli.Utilities
{
    /// <summary>
    /// Provides helper methods for consistent ANSI-colored console output in CLI commands.
    /// </summary>
    public static class AnsiConsoleHelper
    {
        private static readonly char[] SpinnerChars = ['⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏'];
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
        }        /// <summary>
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
                Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");

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
                                Console.Write(new string(' ', Console.WindowWidth > 1 ? Console.WindowWidth - 1 : 80));
                                // Return to start of line again
                                Console.Write("\r");
                                // Print updated spinner and message
                                Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");

                                _spinnerIndex = (_spinnerIndex + 1) % SpinnerChars.Length;
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
        }        /// <summary>
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
        }        /// <summary>
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
                        Console.Write($"{AnsiColors.OKBLUE}{SpinnerChars[_spinnerIndex]} {_currentSpinnerMessage}{AnsiColors.ENDC}");
                    }
                    catch
                    {
                        // If console operations fail, just continue silently
                    }
                }
            }
        }
    }
}
