namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Options for configuring the behavior of OneDrive operations via the command-line interface.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The OneDriveCliOptions class provides a structured way to configure how OneDrive operations
    /// behave when invoked through the command-line interface. These options control behaviors such as:
    /// <list type="bullet">
    ///   <item><description>Whether operations should run in dry-run mode (simulating changes without actually making them)</description></item>
    ///   <item><description>Whether to display verbose output during operations</description></item>
    ///   <item><description>Whether to force operations even if they might overwrite existing content</description></item>
    ///   <item><description>Whether to retry operations on failure</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These options are typically set based on command-line arguments provided by the user,
    /// and then passed to the OneDriveService for use during operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example of setting up options based on command-line arguments:
    /// <code>
    /// var options = new OneDriveCliOptions
    /// {
    ///     DryRun = context.ParseResult.GetValueForOption(dryRunOption),
    ///     Verbose = context.ParseResult.GetValueForOption(verboseOption),
    ///     Force = context.ParseResult.GetValueForOption(forceOption),
    ///     Retry = true  // Always retry by default
    /// };
    /// oneDriveService.SetCliOptions(options);
    /// </code>
    /// </example>
    public class OneDriveCliOptions
    {
        /// <summary>
        /// Gets or sets whether to simulate operations without making actual changes.
        /// </summary>
        /// <value>
        /// <c>true</c> to run in dry-run mode (simulating but not performing operations);
        /// <c>false</c> to perform actual operations.
        /// </value>
        /// <remarks>
        /// When DryRun is enabled, the OneDriveService will log what would have happened
        /// but won't actually modify, upload, or download any files. This is useful for
        /// verifying what operations would be performed without risking any data changes.
        /// </remarks>
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets whether to display detailed, verbose output during operations.
        /// </summary>
        /// <value>
        /// <c>true</c> to display verbose output; <c>false</c> for standard output.
        /// </value>
        /// <remarks>
        /// When Verbose is enabled, the OneDriveService will log additional details about
        /// each operation, including file sizes, paths, timestamps, and more. This is useful
        /// for debugging or for understanding exactly what the service is doing.
        /// </remarks>
        public bool Verbose { get; set; }

        /// <summary>
        /// Gets or sets whether to force operations even if they would overwrite existing content.
        /// </summary>
        /// <value>
        /// <c>true</c> to force operations; <c>false</c> to prompt or skip when conflicts occur.
        /// </value>
        /// <remarks>
        /// When Force is enabled, the OneDriveService will overwrite existing files without
        /// prompting for confirmation. If Force is disabled, the service might skip conflicting
        /// operations or prompt for confirmation, depending on the specific implementation.
        /// </remarks>
        public bool Force { get; set; }

        /// <summary>
        /// Gets or sets whether to retry failed operations.
        /// </summary>
        /// <value>
        /// <c>true</c> to retry failed operations; <c>false</c> to fail immediately.
        /// </value>
        /// <remarks>
        /// <para>
        /// When Retry is enabled, the OneDriveService will attempt to retry operations that
        /// fail due to transient errors, like network issues or rate limiting. The specific
        /// retry strategy (number of retries, delays) is determined by the service implementation.
        /// </para>
        /// <para>
        /// This is particularly useful for operations that are likely to succeed on retry,
        /// such as uploads or downloads that might fail due to temporary network issues.
        /// </para>
        /// </remarks>
        public bool Retry { get; set; }
    }
}
