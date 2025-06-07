// <copyright file="DebugPromptTemplateService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/DebugPromptTemplateService.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests;

[TestClass]
internal class DebugPromptTemplateServiceTests
{
    [TestMethod]
    public void Debug_PromptsDirectorySearch()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<PromptTemplateService>>();
        var yamlHelperMock = new Mock<IYamlHelper>();

        yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(content =>
            {
                if (content.StartsWith("---"))
                {
                    int endIndex = content.IndexOf("---", 3);
                    if (endIndex > 0)
                    {
                        return content[(endIndex + 3)..].Trim();
                    }
                }

                return content;
            });

        AppConfig config = new();

        // Act
        PromptTemplateService service = new(loggerMock.Object, yamlHelperMock.Object, config);

        // Debug: check what directory the service found
        string promptsDirectory = service.PromptsDirectory;

        Console.WriteLine($"Base Directory: {AppContext.BaseDirectory}");
        Console.WriteLine($"Prompts Directory: {promptsDirectory}");

        // Check potential search paths
        string baseDirectory = AppContext.BaseDirectory;

        string[] searchPaths =
        [
            Path.Combine(baseDirectory, "prompts"),
            Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "..\\..\\..")), "prompts"),
            Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "../../../../..")), "prompts"),
            Path.Combine(Path.GetFullPath(Path.Combine(baseDirectory, "../../../../../..")), "prompts")
        ];

        foreach (string path in searchPaths)
        {
            Console.WriteLine($"Search path: {path} - Exists: {Directory.Exists(path)}");
            if (Directory.Exists(path))
            {
                string chunkFile = Path.Combine(path, "chunk_summary_prompt.md");
                Console.WriteLine($"  chunk_summary_prompt.md exists: {File.Exists(chunkFile)}");
            }
        }

        // Try to load the template
        var result = service.LoadTemplateAsync("chunk_summary_prompt").Result;
        Console.WriteLine($"Template length: {result.Length}");
        Console.WriteLine($"Template starts with: {result[..Math.Min(100, result.Length)]}");
    }
}
