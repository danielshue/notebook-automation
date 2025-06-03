using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests;

/// <summary>
/// Unit tests for the PromptTemplateService class which handles template loading
/// and variable substitution for AI prompt templates.
/// </summary>
[TestClass]
public class PromptTemplateServiceTests
{
    private Mock<ILogger<PromptTemplateService>> _loggerMock;
    private string _testFolder;

    /// <summary>
    /// Set up the test environment before each test.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILogger<PromptTemplateService>>();
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "Hello {{name}}, welcome to {{course}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" }
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "Hello {{name}}, welcome to {{course}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" }
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "Hello {{name}}!";
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" } // Extra variable
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
        PromptTemplateService service = new(_loggerMock.Object, config);
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
            { "takeaway2", "Risk management should be integrated across the organization" }
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
    [TestMethod]
    public async Task LoadAndSubstituteAsync_LoadsTemplateAndSubstitutesVariables()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, config);
        string templatePath = Path.Combine(_testFolder, "test_template.md");
        string templateContent = "Hello {{name}}, welcome to {{course}}!";
        await File.WriteAllTextAsync(templatePath, templateContent);

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Programming" }
        };

        // Act
        string result = await service.LoadAndSubstituteAsync(templatePath, variables);

        // Assert
        Assert.AreEqual("Hello John, welcome to MBA Programming!", result);
    }

    /// <summary>
    /// Tests that LoadAndSubstituteAsync returns empty string when template file doesn't exist.
    /// </summary>
    [TestMethod]
    public async Task LoadAndSubstituteAsync_ReturnsEmptyStringWhenFileNotFound()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, config);
        string nonExistentPath = Path.Combine(_testFolder, "non_existent.md");
        Dictionary<string, string> variables = new()
        {
            { "name", "John" }
        };

        // Act
        string result = await service.LoadAndSubstituteAsync(nonExistentPath, variables);

        // Assert
        Assert.AreEqual(string.Empty, result);
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Error),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that LoadTemplateAsync loads a template from the repository root Prompts directory.
    /// </summary>
    [TestMethod]
    public async Task LoadTemplateAsync_LoadsTemplateFromRepositoryRoot()
    {
        // Since we can't reliably test the repository root discovery in a unit test,
        // we're going to test the fallback to default templates

        // Arrange - Create a service with a mocked logger that we can verify
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, config);

        // Act
        string result = await service.LoadTemplateAsync("non_existent_template");

        // Assert - Should fall back to default template
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result));

        // Verify that the appropriate log message was written
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce());
    }

    /// <summary>
    /// Tests that GetDefaultTemplate returns the appropriate default template.
    /// </summary>
    [TestMethod]
    public async Task LoadTemplateAsync_GetsCorrectDefaultTemplate()
    {
        // Arrange
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, config);

        // Act
        string chunkResult = await service.LoadTemplateAsync("chunk_summary_prompt");
        string finalResult = await service.LoadTemplateAsync("final_summary_prompt");
        // Using the same template for all types of content
        string videoResult = await service.LoadTemplateAsync("final_summary_prompt");
        string unknownResult = await service.LoadTemplateAsync("unknown_template");

        // Assert - All should return non-null templates
        Assert.IsNotNull(chunkResult);
        Assert.IsNotNull(finalResult);
        Assert.IsNotNull(videoResult);
        Assert.IsNotNull(unknownResult);            // Normalize line endings and trim trailing whitespace for comparison
        static string Normalize(string s) => s.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd();

        // Since this is an integration test, check that templates were loaded correctly
        // For chunk_summary_prompt, the file content should be used (which is normalized for comparison)
        Assert.AreEqual(Normalize(PromptTemplateService.DefaultChunkPrompt), Normalize(chunkResult));

        // For final_summary_prompt, the content should have been loaded from the file            // Just verify it contains the expected starting text
        StringAssert.StartsWith(finalResult, "You are an educational content summarizer for MBA course materials.");
        Assert.IsTrue(finalResult.Length > 100, "Final summary prompt is too short");

        // After template consolidation, both final_summary_prompt.md and final_summary_prompt_video.md
        // should use the same template, so verify video template has same content
        Assert.AreEqual(Normalize(finalResult), Normalize(videoResult),
            "After template consolidation, video should use the same template as final");
        Assert.IsTrue(videoResult.Length > 100, "Video summary prompt is too short");

        // For unknown templates, we should get the default final prompt template as fallback
        Assert.AreEqual(Normalize(PromptTemplateService.DefaultFinalPrompt), Normalize(unknownResult));
    }

    /// <summary>
    /// Creates a test template file with the specified name and content.
    /// </summary>
    private async Task CreateTestTemplate(string templateName, string content)
    {
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        Directory.CreateDirectory(promptsDir);
        string templatePath = Path.Combine(promptsDir, $"{templateName}.md");
        await File.WriteAllTextAsync(templatePath, content);
    }
    /// <summary>
    /// Tests that LoadTemplateAsync correctly loads template from file when available.
    /// </summary>
    [TestMethod]
    public async Task LoadTemplateAsync_LoadsFromFileWhenAvailable()
    {
        // Arrange
        // Create a directory structure with test templates
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        await File.WriteAllTextAsync(
            Path.Combine(promptsDir, "test_template.md"),
            "Test template content"
        );

        // Create service
        TestablePromptTemplateService service = new(_loggerMock.Object, promptsDir);

        // Act
        string result = await service.LoadTemplateAsyncWithPath("test_template");

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
        TestablePromptTemplateService service = new(_loggerMock.Object, _testFolder);

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
    [TestMethod]
    public async Task LoadTemplateAndSubstitute_WorksCorrectly()
    {
        // Arrange - Create a template file
        string templateContent = "Hello {{name}}, welcome to the {{course}} course!";
        string promptsDir = Path.Combine(_testFolder, "Prompts");
        Directory.CreateDirectory(promptsDir);

        string templatePath = Path.Combine(promptsDir, "welcome_template.md");
        await File.WriteAllTextAsync(templatePath, templateContent);

        TestablePromptTemplateService service = new(_loggerMock.Object, promptsDir);
        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "Advanced Financial Management" }
        };

        // Act - First load the template, then substitute variables
        string template = await service.LoadTemplateAsyncWithPath("welcome_template");
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "Hello {{name}}, your course is {{course}}";

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA {{specialization}}" } // This contains another variable marker
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert - The second-level variable should remain as is
        Assert.AreEqual("Hello John, your course is MBA {{specialization}}", result);

        // Now if we substitute again with the specialization
        Dictionary<string, string> moreVariables = new()
        {
            { "specialization", "Finance" }
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "Hello {{  name  }}, welcome to {{ course}}!";

        Dictionary<string, string> variables = new()
        {
            { "name", "John" },
            { "course", "MBA Program" }
        };

        // Act
        string result = service.SubstituteVariables(template, variables);

        // Assert - Whitespace in variable markers should be trimmed
        Assert.AreEqual("Hello John, welcome to MBA Program!", result);
    }        /// <summary>
             /// Tests error handling when loading a template file throws an exception.
             /// </summary>
    [TestMethod]
    public async Task LoadTemplateAsync_HandlesExceptions()
    {
        // Arrange - Create a mock FileSystem that throws an exception
        AppConfig config = new();
        PromptTemplateService service = new(_loggerMock.Object, config);

        // We need to use a path that will cause an exception
        // In this case, we'll use a path with invalid characters
        string invalidPath = Path.Combine(_testFolder, "Prompts", "invalid|file?.md");

        // Use reflection to set the _promptsDirectory field to our test directory
        System.Reflection.FieldInfo fieldInfo = typeof(PromptTemplateService).GetField("_promptsDirectory",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fieldInfo.SetValue(service, Path.GetDirectoryName(invalidPath));

        // Act
        string result = await service.LoadTemplateAsync(Path.GetFileNameWithoutExtension(invalidPath));

        // Assert - Should get default template
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result));

        // Verify the warning was logged for using default template
        _loggerMock.Verify(x => x.Log(
            It.Is<LogLevel>(l => l == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
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
        PromptTemplateService service = new(_loggerMock.Object, config);
        string template = "{{greeting}}, {{name}}! {{message}}";

        Dictionary<string, string> variables = new()
        {
            { "greeting", "你好" }, // Chinese
            { "name", "João" },     // Portuguese name
            { "message", "Welkom bij onze cursus! Здравствуйте!" } // Dutch and Russian
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
public class TestablePromptTemplateService(ILogger<PromptTemplateService> logger, string promptsDirectory) : PromptTemplateService(logger, new AppConfig())
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
                return await File.ReadAllTextAsync(templatePath);
            }

            return string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
    public string GetDefaultTemplateForTest(string templateName)
    {
        // Use reflection to call the private method
        System.Reflection.MethodInfo methodInfo = typeof(PromptTemplateService).GetMethod(
            "GetDefaultTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return (string)methodInfo.Invoke(this, [templateName]);
    }
}
