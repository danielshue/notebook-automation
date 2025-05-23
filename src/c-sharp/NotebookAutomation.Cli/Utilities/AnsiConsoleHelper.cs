using System;

namespace NotebookAutomation.Cli.Utilities
{
    /// <summary>
    /// Provides helper methods for consistent ANSI-colored console output in CLI commands.
    /// </summary>
    public static class AnsiConsoleHelper
    {
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
    }
}
