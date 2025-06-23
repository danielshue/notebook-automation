// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Tests.Core.Tools;

[TestClass]
public class MarkdownParserTests
{
    private Mock<ILogger> _mockLogger = null!;
    private MarkdownParser _parser = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new();
        _parser = new MarkdownParser(_mockLogger.Object);
    }

    [TestMethod]
    public void ParseMarkdown_EmptyString_ReturnsEmptyFrontmatterAndContent()
    {
        (Dictionary<string, object> frontmatter, string content) = _parser.ParseMarkdown(string.Empty);
        Assert.AreEqual(0, frontmatter.Count);
        Assert.AreEqual(string.Empty, content);
    }

    [TestMethod]
    public void ParseMarkdown_NoFrontmatter_ReturnsContentOnly()
    {
        string md = "# Title\nContent";
        (Dictionary<string, object> frontmatter, string content) = _parser.ParseMarkdown(md);
        Assert.AreEqual(0, frontmatter.Count);
        Assert.AreEqual(md, content);
    }

    [TestMethod]
    public void ParseMarkdown_WithFrontmatter_ParsesFrontmatterAndContent()
    {
        string md = "---\ntitle: Test\n---\n# Heading\nBody";
        (Dictionary<string, object> frontmatter, string content) = _parser.ParseMarkdown(md);
        Assert.IsTrue(frontmatter.ContainsKey("title"));
        Assert.AreEqual("Test", frontmatter["title"].ToString());
        Assert.IsTrue(content.StartsWith("# Heading"));
    }

    [TestMethod]
    public void CombineMarkdown_CombinesFrontmatterAndContent()
    {
        Dictionary<string, object> frontmatter = new() { { "title", "Test" } };
        string content = "# Heading\nBody";
        string result = _parser.CombineMarkdown(frontmatter, content);
        Assert.IsTrue(result.Contains("title: Test"));
        Assert.IsTrue(result.Contains("# Heading"));
    }

    [TestMethod]
    public async Task WriteFileAsync_WritesFileAndReturnsTrue()
    {
        Dictionary<string, object> frontmatter = new() { { "title", "Test" } };
        string content = "# Heading\nBody";
        string tempFile = Path.GetTempFileName();
        try
        {
            bool result = await _parser.WriteFileAsync(tempFile, frontmatter, content).ConfigureAwait(false);
            Assert.IsTrue(result);
            string written = await File.ReadAllTextAsync(tempFile).ConfigureAwait(false);
            Assert.IsTrue(written.Contains("title: Test"));
            Assert.IsTrue(written.Contains("# Heading"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    [Ignore("Temporarily disabled")]
    public async Task WriteFileAsync_Error_LogsAndReturnsFalse()
    {
        Dictionary<string, object> frontmatter = new() { { "title", "Test" } };
        string content = "# Heading\nBody";

        // Use a path that doesn't exist and can't be created (directory doesn't exist)
        string invalidPath = Path.Combine("/nonexistent/directory/that/does/not/exist", "file.md");
        bool result = await _parser.WriteFileAsync(invalidPath, frontmatter, content).ConfigureAwait(false);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ParseFileAsync_FileNotFound_LogsAndReturnsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        (Dictionary<string, object> frontmatter, string content) = await _parser.ParseFileAsync(path).ConfigureAwait(false);
        Assert.AreEqual(0, frontmatter.Count);
        Assert.AreEqual(string.Empty, content);
    }

    [TestMethod]
    public async Task ParseFileAsync_ValidFile_ParsesContent()
    {
        string tempFile = Path.GetTempFileName();
        string md = "---\ntitle: Test\n---\n# Heading\nBody";
        await File.WriteAllTextAsync(tempFile, md).ConfigureAwait(false);
        try
        {
            (Dictionary<string, object> frontmatter, string content) = await _parser.ParseFileAsync(tempFile).ConfigureAwait(false);
            Assert.IsTrue(frontmatter.ContainsKey("title"));
            Assert.AreEqual("Test", frontmatter["title"].ToString());
            Assert.IsTrue(content.StartsWith("# Heading"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public void ParseHeader_ValidHeader_ReturnsLevelAndTitle()
    {
        (int level, string title) = MarkdownParser.ParseHeader("### My Header");
        Assert.AreEqual(3, level);
        Assert.AreEqual("My Header", title);
    }

    [TestMethod]
    public void ParseHeader_InvalidHeader_ReturnsZeroAndTrimmed()
    {
        (int level, string title) = MarkdownParser.ParseHeader("Not a header");
        Assert.AreEqual(0, level);
        Assert.AreEqual("Not a header", title);
    }

    [TestMethod]
    public void ExtractHeaders_FindsAllHeaders()
    {
        string content = "# H1\nText\n## H2\n### H3";
        List<(int Level, string Title, int LineNumber)> headers = MarkdownParser.ExtractHeaders(content);
        Assert.AreEqual(3, headers.Count);
        Assert.AreEqual((1, "H1", 0), headers[0]);
        Assert.AreEqual((2, "H2", 2), headers[1]);
        Assert.AreEqual((3, "H3", 3), headers[2]);
    }

    /// <summary>
    /// Tests that SanitizeForFilename correctly removes invalid filename characters and applies transformations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies that the filename sanitization logic properly handles invalid characters
    /// commonly found in user input. The method should:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Replace invalid filename characters (: / \ | ? * &lt; &gt; ") with hyphens</description></item>
    /// <item><description>Replace dots with hyphens</description></item>
    /// <item><description>Replace spaces with hyphens</description></item>
    /// <item><description>Convert the entire string to lowercase</description></item>
    /// </list>
    /// <para>
    /// Test input "My:File/Name.txt" should become "my-file-name-txt".
    /// </para>
    /// </remarks>
    [TestMethod]
    public void SanitizeForFilename_RemovesInvalidChars()
    {
        // Arrange
        string input = "My:File/Name.txt";

        // Act
        string sanitized = MarkdownParser.SanitizeForFilename(input);

        // Assert
        Assert.AreEqual("my-file-name-txt", sanitized,
            "Should replace invalid chars (:, /) and dots with hyphens, then convert to lowercase");

        // Verify specific characters are removed
        Assert.IsFalse(sanitized.Contains(':'), "Colon should be replaced with hyphen");
        Assert.IsFalse(sanitized.Contains('/'), "Forward slash should be replaced with hyphen");
        Assert.IsFalse(sanitized.Contains('.'), "Dot should be replaced with hyphen");

        // Verify case conversion - all alphabetic characters should be lowercase
        Assert.IsTrue(sanitized.Where(char.IsLetter).All(char.IsLower),
            "All alphabetic characters should be lowercase");

        // Verify no uppercase letters exist
        Assert.IsFalse(sanitized.Any(char.IsUpper), "Result should not contain any uppercase letters");
    }

    [TestMethod]
    public void SanitizeForFilename_Empty_ReturnsUnnamed()
    {
        string sanitized = MarkdownParser.SanitizeForFilename(string.Empty);
        Assert.AreEqual("unnamed", sanitized);
    }

    /// <summary>
    /// Tests that SanitizeForFilename correctly handles various input scenarios and edge cases.
    /// </summary>
    /// <remarks>
    /// This comprehensive test verifies that the filename sanitization logic properly:
    /// <list type="bullet">
    /// <item><description>Replaces spaces with hyphens</description></item>
    /// <item><description>Replaces dots with hyphens</description></item>
    /// <item><description>Replaces invalid filename characters with hyphens</description></item>
    /// <item><description>Converts to lowercase</description></item>
    /// <item><description>Preserves valid filename characters like underscores</description></item>
    /// </list>
    /// </remarks>
    [TestMethod]
    public void SanitizeForFilename_VariousInputs_ReturnsExpectedResults()
    {
        // Test cases: input -> expected output
        // Note: Path.GetInvalidFileNameChars() typically includes: < > : " | ? * \ /
        var testCases = new Dictionary<string, string>
        {
            { "Simple File", "simple-file" },
            { "File.With.Dots", "file-with-dots" },
            { "File:With|Invalid<Chars>", "file-with-invalid-chars-" }, // : | < > are invalid
            { "UPPERCASE", "uppercase" },
            { "Mixed-Case_File", "mixed-case_file" }, // underscore is valid
            { "   Leading and Trailing   ", "---leading-and-trailing---" },
            { "File\\With\\Backslashes", "file-with-backslashes" }, // backslash is invalid
            { "File/With/Forward/Slashes", "file-with-forward-slashes" }, // forward slash is invalid
            { "File\"With\"Quotes", "file-with-quotes" }, // quotes are invalid
            { "File*With?Wildcards", "file-with-wildcards" }, // * and ? are invalid
            { "ValidFileName123", "validfilename123" }, // should just be lowercased
            { "file-already-valid", "file-already-valid" } // already valid, just lowercased
        };

        foreach (var testCase in testCases)
        {
            string result = MarkdownParser.SanitizeForFilename(testCase.Key);
            Assert.AreEqual(testCase.Value, result,
                $"Input '{testCase.Key}' should produce '{testCase.Value}' but got '{result}'");
        }
    }

    /// <summary>
    /// Tests that SanitizeForFilename always converts alphabetic characters to lowercase.
    /// </summary>
    /// <remarks>
    /// This test specifically validates the lowercase conversion behavior, ensuring that
    /// only alphabetic characters are checked for case (ignoring hyphens, numbers, etc.).
    /// </remarks>
    [TestMethod]
    public void SanitizeForFilename_AlwaysReturnsLowercase()
    {
        // Arrange - Test with mixed case input containing various character types
        var testInputs = new[]
        {
            "UPPERCASE",
            "MixedCASE123",
            "Already-lowercase",
            "Special:Chars/With.UPPERCASE",
            "Numbers123AndUPPERCASE"
        };

        foreach (string input in testInputs)
        {
            // Act
            string result = MarkdownParser.SanitizeForFilename(input);

            // Assert - All alphabetic characters should be lowercase
            Assert.IsTrue(result.Where(char.IsLetter).All(char.IsLower),
                $"Input '{input}' should have all alphabetic characters in lowercase, but got '{result}'");

            // Assert - No uppercase letters should exist
            Assert.IsFalse(result.Any(char.IsUpper),
                $"Input '{input}' should not contain uppercase letters, but got '{result}'");
        }
    }

    /// <summary>
    /// Tests that SanitizeForFilename handles all problematic characters identified in the test cases.
    /// </summary>
    /// <remarks>
    /// This test validates that every character mentioned in the existing test cases is properly
    /// handled by the SanitizeForFilename method, ensuring comprehensive coverage.
    /// </remarks>
    [TestMethod]
    public void SanitizeForFilename_HandlesAllProblematicCharacters()
    {
        // Test each problematic character individually to ensure they're all handled
        var problematicCharacters = new Dictionary<char, string>
        {
            // Characters from existing test cases
            { ':', "colon" },
            { '|', "pipe" },
            { '<', "less-than" },
            { '>', "greater-than" },
            { '\\', "backslash" },
            { '/', "forward-slash" },
            { '"', "quote" },
            { '*', "asterisk" },
            { '?', "question-mark" },

            // Additional problematic characters that should be tested
            { '\t', "tab" },
            { '\n', "newline" },
            { '\r', "carriage-return" },
            { '\0', "null-character" },
        };

        foreach (var kvp in problematicCharacters)
        {
            char problematicChar = kvp.Key;
            string description = kvp.Value;

            // Create test input with the problematic character
            string input = $"test{problematicChar}file";
            string result = MarkdownParser.SanitizeForFilename(input);

            // Assert that the problematic character has been replaced
            Assert.IsFalse(result.Contains(problematicChar),
                $"Character '{problematicChar}' ({description}) should be replaced but was found in result: '{result}'");

            // Assert that the result contains hyphens where the character was
            Assert.IsTrue(result.Contains('-'),
                $"Result should contain hyphens where '{problematicChar}' ({description}) was replaced: '{result}'");
        }
    }

    /// <summary>
    /// Tests edge cases and boundary conditions for SanitizeForFilename.
    /// </summary>
    [TestMethod]
    public void SanitizeForFilename_HandlesEdgeCases()
    {
        // Test cases for edge conditions
        var edgeCases = new Dictionary<string, string>
        {
            // Multiple consecutive invalid characters
            { "file:::name", "file---name" },
            { "file...name", "file---name" },
            { "file   name", "file---name" },

            // Mixed invalid characters
            { "file:|*.txt", "file----txt" },

            // Only invalid characters
            { ":::", "---" },
            { "...", "---" },
            { "   ", "---" },

            // Empty after sanitization scenarios
            { ".", "-" },
            { " ", "-" },

            // Unicode and special characters that should be preserved or handled
            { "file_name", "file_name" }, // underscore should be preserved
            { "file-name", "file-name" }, // hyphen should be preserved
            { "file123", "file123" }, // numbers should be preserved

            // Very long names (test truncation if implemented)
            { new string('a', 300), new string('a', 300).ToLowerInvariant() },
        };

        foreach (var testCase in edgeCases)
        {
            string result = MarkdownParser.SanitizeForFilename(testCase.Key);
            Assert.AreEqual(testCase.Value, result,
                $"Edge case input '{testCase.Key}' should produce '{testCase.Value}' but got '{result}'");
        }
    }

    /// <summary>
    /// Tests null and whitespace-only inputs for SanitizeForFilename.
    /// </summary>
    [TestMethod]
    public void SanitizeForFilename_NullAndWhitespaceInputs_HandlesCorrectly()
    {
        // Test null input
        string? nullInput = null;
        string result = MarkdownParser.SanitizeForFilename(nullInput!);
        Assert.AreEqual("unnamed", result, "Null input should return 'unnamed'");

        // Test empty string
        result = MarkdownParser.SanitizeForFilename(string.Empty);
        Assert.AreEqual("unnamed", result, "Empty string should return 'unnamed'");

        // Test whitespace-only strings
        var whitespaceInputs = new[]
        {
            " ",
            "   ",
            "\t",
            "\n",
            "\r\n",
            " \t \n ",
        };

        foreach (string whitespaceInput in whitespaceInputs)
        {
            result = MarkdownParser.SanitizeForFilename(whitespaceInput);
            Assert.IsTrue(result.All(c => c == '-'),
                $"Whitespace-only input '{whitespaceInput.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r")}' should result in only hyphens, but got '{result}'");
            Assert.IsTrue(result.Length > 0,
                $"Whitespace-only input should not result in empty string, but got '{result}'");
        }
    }
}
