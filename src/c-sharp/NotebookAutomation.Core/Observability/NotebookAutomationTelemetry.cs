// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace NotebookAutomation.Core.Observability;

/// <summary>
/// Provides centralized ActivitySource for tracing operations throughout the Notebook Automation application.
/// </summary>
/// <remarks>
/// <para>
/// The <c>NotebookAutomationTelemetry</c> class provides a centralized way to create and manage tracing activities
/// for debugging and observability purposes. It's particularly useful for tracking complex operations like
/// AI function creation, prompt processing, and semantic kernel operations.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
///   <item><description>Centralized ActivitySource for consistent tracing across the application</description></item>
///   <item><description>Helper methods for common tracing scenarios (AI operations, file processing, etc.)</description></item>
///   <item><description>Automatic tag and attribute management for rich telemetry data</description></item>
///   <item><description>Integration with OpenTelemetry for external observability systems</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// using var activity = NotebookAutomationTelemetry.StartAIOperation("summarize_content");
/// activity?.SetTag("content.length", content.Length);
/// activity?.SetTag("ai.provider", "openai");
/// // ... perform AI operation ...
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Trace an AI summarization operation
/// using var activity = NotebookAutomationTelemetry.StartAIOperation("chunk_summarization");
/// activity?.SetTag("chunk.index", chunkIndex);
/// activity?.SetTag("chunk.size", chunkSize);
/// activity?.SetTag("ai.model", modelName);
///
/// try
/// {
///     var result = await aiService.SummarizeAsync(content);
///     activity?.SetTag("result.length", result.Length);
///     activity?.SetStatus(ActivityStatusCode.Ok);
///     return result;
/// }
/// catch (Exception ex)
/// {
///     activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
///     throw;
/// }
/// </code>
/// </example>
public static class NotebookAutomationTelemetry
{
    /// <summary>
    /// The name of the ActivitySource used for tracing.
    /// </summary>
    public const string ActivitySourceName = "NotebookAutomation.Core";

    /// <summary>
    /// The version of the ActivitySource.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The main ActivitySource for the application.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version);


    /// <summary>
    /// Starts an activity for AI-related operations.
    /// </summary>
    /// <param name="operationName">The name of the AI operation being performed.</param>
    /// <param name="tags">Optional tags to add to the activity.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    /// <example>
    /// <code>
    /// using var activity = NotebookAutomationTelemetry.StartAIOperation("prompt_processing");
    /// activity?.SetTag("prompt.template", templateName);
    /// </code>
    /// </example>
    public static Activity? StartAIOperation(string operationName, params (string Key, object? Value)[] tags)
    {
        var activity = ActivitySource.StartActivity($"ai.{operationName}");

        if (activity != null)
        {
            activity.SetTag("operation.category", "ai");
            activity.SetTag("operation.name", operationName);

            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value?.ToString());
            }
        }

        return activity;
    }


    /// <summary>
    /// Starts an activity for file processing operations.
    /// </summary>
    /// <param name="operationName">The name of the file operation being performed.</param>
    /// <param name="filePath">The path of the file being processed.</param>
    /// <param name="tags">Optional additional tags to add to the activity.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    /// <example>
    /// <code>
    /// using var activity = NotebookAutomationTelemetry.StartFileOperation("extract_metadata", filePath);
    /// activity?.SetTag("file.type", "video");
    /// </code>
    /// </example>
    public static Activity? StartFileOperation(string operationName, string? filePath = null, params (string Key, object? Value)[] tags)
    {
        var activity = ActivitySource.StartActivity($"file.{operationName}");

        if (activity != null)
        {
            activity.SetTag("operation.category", "file");
            activity.SetTag("operation.name", operationName);

            if (!string.IsNullOrEmpty(filePath))
            {
                activity.SetTag("file.path", filePath);
                activity.SetTag("file.extension", Path.GetExtension(filePath));
            }

            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value?.ToString());
            }
        }

        return activity;
    }


    /// <summary>
    /// Starts an activity for Semantic Kernel operations.
    /// </summary>
    /// <param name="operationName">The name of the SK operation being performed.</param>
    /// <param name="functionName">The name of the SK function, if applicable.</param>
    /// <param name="tags">Optional additional tags to add to the activity.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    /// <example>
    /// <code>
    /// using var activity = NotebookAutomationTelemetry.StartSemanticKernelOperation("function_creation", "SummarizeContent");
    /// activity?.SetTag("sk.prompt.length", promptContent.Length);
    /// </code>
    /// </example>
    public static Activity? StartSemanticKernelOperation(string operationName, string? functionName = null, params (string Key, object? Value)[] tags)
    {
        var activity = ActivitySource.StartActivity($"sk.{operationName}");

        if (activity != null)
        {
            activity.SetTag("operation.category", "semantic_kernel");
            activity.SetTag("operation.name", operationName);

            if (!string.IsNullOrEmpty(functionName))
            {
                activity.SetTag("sk.function.name", functionName);
            }

            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value?.ToString());
            }
        }

        return activity;
    }


    /// <summary>
    /// Starts a general activity with the specified name.
    /// </summary>
    /// <param name="activityName">The name of the activity.</param>
    /// <param name="tags">Optional tags to add to the activity.</param>
    /// <returns>The started activity, or null if no listeners are active.</returns>
    /// <example>
    /// <code>
    /// using var activity = NotebookAutomationTelemetry.StartActivity("video_processing");
    /// activity?.SetTag("video.duration", duration);
    /// </code>
    /// </example>
    public static Activity? StartActivity(string activityName, params (string Key, object? Value)[] tags)
    {
        var activity = ActivitySource.StartActivity(activityName);

        if (activity != null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value?.ToString());
            }
        }

        return activity;
    }


    /// <summary>
    /// Sets the status of an activity to error with the specified message.
    /// </summary>
    /// <param name="activity">The activity to set the error status on.</param>
    /// <param name="errorMessage">The error message to record.</param>
    /// <param name="exception">The exception that caused the error, if available.</param>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     // ... operation ...
    /// }
    /// catch (Exception ex)
    /// {
    ///     NotebookAutomationTelemetry.SetError(activity, "Failed to process file", ex);
    ///     throw;
    /// }
    /// </code>
    /// </example>
    public static void SetError(Activity? activity, string errorMessage, Exception? exception = null)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
        activity.SetTag("error.message", errorMessage);

        if (exception != null)
        {
            activity.SetTag("error.type", exception.GetType().Name);
            activity.SetTag("error.stack_trace", exception.StackTrace);
        }
    }


    /// <summary>
    /// Sets the status of an activity to success.
    /// </summary>
    /// <param name="activity">The activity to set the success status on.</param>
    /// <param name="message">Optional success message.</param>
    /// <example>
    /// <code>
    /// var result = await ProcessAsync();
    /// NotebookAutomationTelemetry.SetSuccess(activity, "Processing completed successfully");
    /// </code>
    /// </example>
    public static void SetSuccess(Activity? activity, string? message = null)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Ok, message ?? "Operation completed successfully");

        if (!string.IsNullOrEmpty(message))
        {
            activity.SetTag("success.message", message);
        }
    }


    /// <summary>
    /// Disposes the ActivitySource when the application shuts down.
    /// </summary>
    public static void Dispose()
    {
        ActivitySource?.Dispose();
    }
}
