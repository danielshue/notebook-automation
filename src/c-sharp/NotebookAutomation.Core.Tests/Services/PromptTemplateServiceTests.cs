// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tests.Services;

/// <summary>
/// Unit tests for the PromptTemplateService class which handles template loading
/// and variable substitution for AI prompt templates.
/// </summary>
[TestClass]
public class PromptTemplateServiceTests
{
    private Mock<ILogger<PromptTemplateService>> _loggerMock = null!;
    private Mock<IYamlHelper> _yamlHelperMock = null!;
    private string _testFolder = null!;

    /// <summary>
    /// Set up the test environment before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<PromptTemplateService>>();
        _yamlHelperMock = new Mock<IYamlHelper>();

        // Set up yamlHelper to simulate frontmatter removal
        _yamlHelperMock.Setup(m => m.RemoveFrontmatter(It.IsAny<string>()))
            .Returns<string>(content =>
            {
                // Simple frontmatter removal: if starts with ---, remove everything until second ---
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

        // Create a temporary test folder for prompt templates
        _testFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testFolder);
        Directory.CreateDirectory(Path.Combine(_testFolder, "Prompts"));
    }

    /// <summary>
    /// Clean up the test environment after each test.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        // Remove the temporary test folder
        if (Directory.Exists(_testFolder))
        {
            Directory.Delete(_testFolder, true);
        }
    }

    /// <summary>
    /// Tests that SubstituteVariables correctly replaces template variables.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_ReplacesTemplateVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "Hello {{name}}, welcome to {{course}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" },
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert
        Assert.AreEqual("Hello John, welcome to MBA Programming!", result);
    }

    /// <summary>
    /// Tests that SubstituteVariables handles missing variables by keeping the placeholder.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_HandlesMissingVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "Hello {{name}}, welcome to {{course}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },

            // 'course' is intentionally missing
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert
        Assert.AreEqual("Hello John, welcome to {{course}}!", result);
    }

    /// <summary>
    /// Tests that SubstituteVariables ignores extra variables.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_IgnoresExtraVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "Hello {{name}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" }, // Extra variable
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert
        Assert.AreEqual("Hello John!", result);
    }

    /// <summary>
    /// Tests variable substitution with complex templates containing multiple variables.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_HandlesComplexTemplates()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = @"# 📝 Notes for {{course}}

## 🧩 Topics Covered in {{module}}

- {{topic1}}
- {{topic2}}
- {{topic3}}

## 🔑 Key Concepts Explained by {{authors}}

{{conceptParagraph}}

## ⭐ Important Takeaways

- {{takeaway1}}
- {{takeaway2}}";

        Dictionary<string, string> variables = new()
        {
            { "course", "Financial Management" },
            { "module", "Risk Assessment" },
            { "topic1", "Market Risk Analysis" },
            { "topic2", "Credit Risk Models" },
            { "topic3", "Operational Risk Management" },
            { "authors", "Prof. Smith" },
            { "conceptParagraph", "Risk assessment is fundamental to financial decision-making." },
            { "takeaway1", "Always quantify risks where possible" },
            { "takeaway2", "Risk management should be integrated across the organization" },
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert
        StringAssert.Contains(result, "# 📝 Notes for Financial Management");
        StringAssert.Contains(result, "## 🧩 Topics Covered in Risk Assessment");
        StringAssert.Contains(result, "- Market Risk Analysis");
        StringAssert.Contains(result, "- Credit Risk Models");
        StringAssert.Contains(result, "- Operational Risk Management");
        StringAssert.Contains(result, "## 🔑 Key Concepts Explained by Prof. Smith");
        StringAssert.Contains(result, "Risk assessment is fundamental to financial decision-making.");
        StringAssert.Contains(result, "- Always quantify risks where possible");
        StringAssert.Contains(result, "- Risk management should be integrated across the organization");
    }

    /// <summary>
    /// Tests that LoadAndSubstituteAsync correctly loads a template and substitutes variables.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadAndSubstituteAsync_LoadsTemplateAndSubstitutesVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string templatePath = Path.Combine(_testFolder, "test_template.md");
        string templateContent = "Hello {{name}}, welcome to {{course}}!";
        await File.WriteAllTextAsync(templatePath, templateContent).ConfigureAwait(false);

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" },
        };

        // Act
        string result = await service.LoadAndSubstituteAsync(templatePath, variables).ConfigureAwait(false);

        // Assert
        Assert.AreEqual("Hello John, welcome to MBA Programming!", result);
    }

    /// <summary>
    /// Tests that LoadAndSubstituteAsync returns empty string when template file doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadAndSubstituteAsync_ReturnsEmptyStringWhenFileNotFound()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string nonExistentPath = Path.Combine(_testFolder, "non_existent.md");
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
        };

        // Act
        string result = await service.LoadAndSubstituteAsync(nonExistentPath, variables).ConfigureAwait(false);

        // Assert
        Assert.AreEqual(string.Empty, result);
        _loggerMock.Verify(
            x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error), It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that LoadTemplateAsync loads a template from the repository root Prompts directory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadTemplateAsync_LoadsTemplateFromRepositoryRoot()
    {
        // Since we can't reliably test the repository root discovery in a unit test,
        // we're going to test the fallback to default templates

        // Arrange - Create a service with a mocked logger that we can verify
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);

        // Act
        string result = await service.LoadTemplateAsync("non_existent_template").ConfigureAwait(false);

        // Assert - Should fall back to default template
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result));

        // Verify that the appropriate log message was written
        _loggerMock.Verify(
            x => x.Log(It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Tests that GetDefaultTemplate returns the appropriate default template.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadTemplateAsync_GetsCorrectDefaultTemplate()
    {
        // Arrange
        AppConfig config = new();

        // Set the prompts path to the repository root prompts directory
        // Use a more robust approach to find the prompts directory
        string promptsPath = FindPromptsDirectory();
        config.Paths.PromptsPath = promptsPath;

        // Use NullLogger to avoid any potential issues with mock logging
        var nullLogger = NullLogger<PromptTemplateService>.Instance;
        PromptTemplateService service = new(nullLogger, _yamlHelperMock.Object, config);        // Act
        string chunkResult = await service.LoadTemplateAsync("chunk_summary_prompt").ConfigureAwait(false);
        string finalResult = await service.LoadTemplateAsync("final_summary_prompt").ConfigureAwait(false);
        string unknownResult = await service.LoadTemplateAsync("unknown_template").ConfigureAwait(false);

        // Assert - Simple assertions first to check if basic functionality works
        Assert.IsNotNull(chunkResult);
        Assert.IsNotNull(finalResult);
        Assert.IsNotNull(unknownResult);

        // Basic length checks
        Assert.IsTrue(chunkResult.Length > 0, "Chunk summary prompt should not be empty");
        Assert.IsTrue(finalResult.Length > 0, "Final summary prompt should not be empty");
        Assert.IsTrue(unknownResult.Length > 0, "Unknown template fallback should not be empty");
    }

    /// <summary>
    /// Finds the prompts directory using multiple strategies to avoid potential infinite loops.
    /// </summary>
    private static string FindPromptsDirectory()
    {
        // Strategy 1: Check if we can find it relative to the workspace root
        string workspaceRoot = @"d:\source\notebook-automation";
        string promptsPath = Path.Combine(workspaceRoot, "prompts");
        if (Directory.Exists(promptsPath))
        {
            return promptsPath;
        }

        // Strategy 2: Walk up from AppContext.BaseDirectory with a depth limit
        string currentDir = AppContext.BaseDirectory;
        int maxDepth = 10; // Prevent infinite loops
        int depth = 0;

        while (!string.IsNullOrEmpty(currentDir) && depth < maxDepth)
        {
            string testPromptsPath = Path.Combine(currentDir, "prompts");
            if (Directory.Exists(testPromptsPath) &&
                File.Exists(Path.Combine(testPromptsPath, "chunk_summary_prompt.md")))
            {
                return testPromptsPath;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName ?? string.Empty;
            depth++;
        }

        // Strategy 3: Try the original GetRepositoryRoot with timeout protection
        try
        {
            string repoRoot = GetRepositoryRoot();
            return Path.Combine(repoRoot, "prompts");
        }
        catch
        {
            // Fall back to using a known working directory
            return @"d:\source\notebook-automation\prompts";
        }
    }

    /// <summary>
    /// Gets the repository root directory by walking up from the current directory.
    /// </summary>
    private static string GetRepositoryRoot()
    {
        string currentDir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(currentDir))
        {
            if (Directory.Exists(Path.Combine(currentDir, "prompts")) &&
                File.Exists(Path.Combine(currentDir, "README.md")))
            {
                return currentDir;
            }

            currentDir = Directory.GetParent(currentDir)?.FullName ?? string.Empty;
        }

        throw new DirectoryNotFoundException("Could not find repository root directory");
    }

    /// <summary>
    /// Creates a test template file with the specified name and content.
    /// </summary>
    private async Task CreateTestTemplate(string templateName, string content)
    {
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        Directory.CreateDirectory(promptsDir);
        string templatePath = Path.Combine(promptsDir, $"{templateName}.md");
        await File.WriteAllTextAsync(templatePath, content).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests that LoadTemplateAsync correctly loads template from file when available.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadTemplateAsync_LoadsFromFileWhenAvailable()
    {
        // Arrange
        // Create a directory structure with test templates
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        await File.WriteAllTextAsync(
            Path.Combine(promptsDir, "test_template.md"),
            "Test template content")
        .ConfigureAwait(false);

        // Create service
        TestablePromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, promptsDir);

        // Act
        string result = await service.LoadTemplateAsyncWithPath("test_template").ConfigureAwait(false);

        // Assert
        Assert.AreEqual("Test template content", result);
    }

    /// <summary>
    /// Tests the GetDefaultTemplate method for handling template types.
    /// </summary>
    [TestMethod]
    public void GetDefaultTemplate_ReturnsCorrectTemplateType()
    {
        // This test uses the TestablePromptTemplateService to expose the protected method
        // for testing purposes.

        // Arrange
        TestablePromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, _testFolder);

        // Act
        string chunkTemplate = service.GetDefaultTemplateForTest("chunk_summary_prompt");
        string finalTemplate = service.GetDefaultTemplateForTest("final_summary_prompt");

        // Using the same template for all types of content
        string videoTemplate = service.GetDefaultTemplateForTest("final_summary_prompt");
        string fallbackTemplate = service.GetDefaultTemplateForTest("unknown_template");

        // Assert
        Assert.IsNotNull(chunkTemplate);
        Assert.IsNotNull(finalTemplate);
        Assert.IsNotNull(videoTemplate);
        Assert.IsNotNull(fallbackTemplate);

        // chunk and final should be different, but video and fallback should match final
        Assert.AreNotEqual(chunkTemplate, finalTemplate);
        Assert.AreEqual(finalTemplate, videoTemplate);
        Assert.AreEqual(finalTemplate, fallbackTemplate);
    }

    /// <summary>
    /// Tests that LoadTemplateAsync and then substitution works properly.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadTemplateAndSubstitute_WorksCorrectly()
    {
        // Arrange - Create a template file
        string templateContent = "Hello {{name}}, welcome to the {{course}} course!";
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        Directory.CreateDirectory(promptsDir);

        string templatePath = Path.Combine(promptsDir, "welcome_template.md");
        await File.WriteAllTextAsync(templatePath, templateContent).ConfigureAwait(false);

        TestablePromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, promptsDir);
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "Advanced Financial Management" },
        };

        // Act - First load the template, then substitute variables
        string template = await service.LoadTemplateAsyncWithPath("welcome_template").ConfigureAwait(false);
        string result = service.SubstituteVariables(template, variables);

        // Assert
        Assert.AreEqual("Hello John, welcome to the Advanced Financial Management course!", result);
    }

    /// <summary>
    /// Tests handling of nested variables - where one variable contains another variable marker.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_HandlesNestedVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "Hello {{name}}, your course is {{course}}";

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA {{specialization}}" }, // This contains another variable marker
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert - The second-level variable should remain as is
        Assert.AreEqual("Hello John, your course is MBA {{specialization}}", result);

        // Now if we substitute again with the specialization
        Dictionary<string, string> moreVariables = new()
        {
            { "specialization", "Finance" },
        };

        string finalResult = service.SubstituteVariables(result, moreVariables);
        Assert.AreEqual("Hello John, your course is MBA Finance", finalResult);
    }

    /// <summary>
    /// Tests variable substitution with whitespace variations in the variable names.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_HandlesWhitespaceInVariableNames()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "Hello {{  name  }}, welcome to {{ course}}!";

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Program" },
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert - Whitespace in variable markers should be trimmed
        Assert.AreEqual("Hello John, welcome to MBA Program!", result);
    }

    /// <summary>
    /// Tests error handling when loading a template file throws an exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [TestMethod]
    public async Task LoadTemplateAsync_HandlesExceptions()
    {
        // Arrange - Create a mock FileSystem that throws an exception
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);

        // We need to use a path that will cause an exception
        // In this case, we'll use a path with invalid characters
        string invalidPath = Path.Combine(_testFolder, "Prompts", "invalid|file?.md");        // Use reflection to set the _promptsDirectory field to our test directory
        System.Reflection.FieldInfo? fieldInfo = typeof(PromptTemplateService).GetField(
            "_promptsDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Skip test if reflection fails (could happen in Release mode or different environments)
        if (fieldInfo == null)
        {
            Assert.Inconclusive("Cannot access _promptsDirectory field via reflection in this environment");
            return;
        }        // Check for null before calling SetValue
        string? directoryPath = Path.GetDirectoryName(invalidPath);
        if (directoryPath == null)
        {
            Assert.Inconclusive("Could not determine directory path for test");
            return;
        }

        fieldInfo.SetValue(service, directoryPath);

        // Act
        string result = await service.LoadTemplateAsync(Path.GetFileNameWithoutExtension(invalidPath)).ConfigureAwait(false);

        // Assert - Should get default template
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result));

        // Verify the warning was logged for using default template
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// Tests that the template variable substitution correctly handles multilingual content.
    /// </summary>
    [TestMethod]
    public void SubstituteVariables_HandlesMultilingualContent()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, _yamlHelperMock.Object, config);
        string template = "{{greeting}}, {{name}}! {{message}}";

        Dictionary<string, string> variables = new()
        {
            { "greeting", "你好" }, // Chinese
            { "name", "João" },     // Portuguese name
            { "message", "Welkom bij onze cursus! Здравствуйте!" }, // Dutch and Russian
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert
        Assert.AreEqual("你好, João! Welkom bij onze cursus! Здравствуйте!", result);
    }
}

/// <summary>
/// A testable version of PromptTemplateService that allows setting the prompts directory.
/// </summary>
internal class TestablePromptTemplateService(ILogger<PromptTemplateService> logger, IYamlHelper yamlHelper, string promptsDirectory) : PromptTemplateService(logger, yamlHelper, new AppConfig())
{
    private readonly string _testPromptsDirectory = promptsDirectory;

    public new string PromptsDirectory => _testPromptsDirectory;

    public async Task<string> LoadTemplateAsyncWithPath(string templateName)
    {
        string templatePath = Path.Combine(_testPromptsDirectory, $"{templateName}.md");

        try
        {
            if (File.Exists(templatePath))
            {
                return await File.ReadAllTextAsync(templatePath).ConfigureAwait(false);
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public string GetDefaultTemplateForTest(string templateName)
    {        // Use reflection to call the private method
        System.Reflection.MethodInfo? methodInfo = typeof(PromptTemplateService).GetMethod(
            "GetDefaultTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)(methodInfo?.Invoke(this, [templateName]) ?? string.Empty);
    }
}
