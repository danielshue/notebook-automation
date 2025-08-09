// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the current local date in yyyy-MM-dd format for date-created or similar fields.
/// </summary>
/// <remarks>
/// <para>
/// <b>Precedence:</b> Values produced by resolvers are merged by <see cref="IMetadataPipeline"/> with precedence
/// (schema defaults < resolvers < content/frontmatter < explicit overrides). Returning a concrete value here supplies
/// a sensible default that can be overridden by frontmatter or user edits.
/// </para>
/// <example>
/// <code>
/// var value = (string?)resolver.Resolve("date-created");
/// // e.g., "2025-08-08"
/// </code>
/// </example>
/// </remarks>
public class DateCreatedResolver(ILogger<DateCreatedResolver> logger) : IFieldValueResolver
{
    private readonly ILogger<DateCreatedResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Resolves the current local date in <c>yyyy-MM-dd</c> format.
    /// </summary>
    /// <param name="fieldName">The name of the field being resolved (e.g., <c>"date-created"</c>).</param>
    /// <param name="context">Optional resolver context. This resolver does not use context.</param>
    /// <returns>A string containing the formatted local date.</returns>
    /// <remarks>
    /// Uses <see cref="DateTime.Now"/> to obtain the local date and logs a debug message with the resolved value.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        var value = DateTime.Now.ToString("yyyy-MM-dd");
        _logger.LogDebug("Resolved {Field} to {Value}", fieldName, value);
        return value;
    }
}
