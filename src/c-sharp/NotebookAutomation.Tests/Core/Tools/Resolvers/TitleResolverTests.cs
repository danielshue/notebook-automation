// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Tools.Resolvers;

[TestClass]
public class TitleResolverTests
{
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
