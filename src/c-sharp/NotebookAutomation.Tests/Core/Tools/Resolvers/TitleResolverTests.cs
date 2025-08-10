// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

/// <summary>
/// Unit tests for <see cref="TitleResolver"/> verifying title extraction logic from file path context.
/// </summary>
/// <remarks>
/// These tests follow the Arrange-Act-Assert pattern and validate:
/// - FriendlyTitleHelper integration for human-readable titles
/// - Fallback to the internal path key (<c>_internal_path</c>) when <c>filePath</c> is not provided
/// - Null behavior when no context is available
/// </remarks>
[TestClass]
public class TitleResolverTests
{
    /// <summary>
    /// Ensures the resolver uses FriendlyTitleHelper against <c>filePath</c> to produce a cleaned title.
    /// </summary>
    /// <remarks>
    /// Expect stop-words removed (e.g., "to") and numeric prefixes stripped.
    /// </remarks>
    [TestMethod]
    public void Resolve_WithFilePath_UsesFriendlyTitleHelper()
    {
        // Arrange
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TitleResolver>();
        var resolver = new TitleResolver(logger);
        var context = new Dictionary<string, object>
        {
            ["filePath"] = "C:/vault/Program/Course/Class/01-introduction-to-cvp.mp4"
        };

        // Act
        var value = resolver.Resolve("title", context)?.ToString();

        // Assert
        Assert.IsNotNull(value);
        // FriendlyTitleHelper removes common words like 'to'
        Assert.AreEqual("Introduction CVP", value);
    }

    /// <summary>
    /// Verifies the resolver can fall back to the internal context key <c>_internal_path</c> when <c>filePath</c> is missing.
    /// </summary>
    /// <remarks>
    /// Leading sequence numbers should be removed and words normalized (e.g., "02-" prefix).
    /// </remarks>
    [TestMethod]
    public void Resolve_WithInternalPath_Works()
    {
        // Arrange
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TitleResolver>();
        var resolver = new TitleResolver(logger);
        var context = new Dictionary<string, object>
        {
            ["_internal_path"] = "C:/vault/Docs/Module/lesson_02-roi-analysis.pdf"
        };

        // Act
        var value = resolver.Resolve("title", context)?.ToString();

        // Assert
        Assert.IsNotNull(value);
        // FriendlyTitleHelper removes leading numbers like "02"
        Assert.AreEqual("ROI Analysis", value);
    }

    /// <summary>
    /// Confirms the resolver returns <c>null</c> when no context is provided.
    /// </summary>
    [TestMethod]
    public void Resolve_WithoutContext_ReturnsNull()
    {
        // Arrange
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TitleResolver>();
        var resolver = new TitleResolver(logger);

        // Act
        var value = resolver.Resolve("title", null);

        // Assert
        Assert.IsNull(value);
    }
}
