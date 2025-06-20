// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.TestDoubles;

[TestClass]
public class DebugPromptTemplateServiceTests
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

        AppConfig config = new object();

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
