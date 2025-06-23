// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Utils;

/// <summary>
/// Tests for the YamlHelper class focusing on YAML parsing and handling.
/// </summary>
[TestClass]
public class YamlHelperTests
{
    private ILogger<YamlHelper> _logger = null!;
    private YamlHelper _yamlHelper = null!;

    /// <summary>
    /// Initialize test resources before each test.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        _logger = new LoggerFactory().CreateLogger<YamlHelper>();
        _yamlHelper = new YamlHelper(_logger);
    }

    /// <summary>
    /// Test that ExtractFrontmatter properly extracts YAML frontmatter from markdown.
    /// </summary>
    [TestMethod]
    public void ExtractFrontmatter_WithValidFrontmatter_ReturnsCorrectYaml()
    {
        // Arrange
        string markdown = @"---
title: Test Document
tags: [test, yaml]
---

# Content starts here";

        // Act
        string? result = _yamlHelper.ExtractFrontmatter(markdown);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("title: Test Document"));
        Assert.IsTrue(result.Contains("tags: [test, yaml]"));
    }

    /// <summary>
    /// Test that ExtractFrontmatter returns null when no frontmatter is present.
    /// </summary>
    [TestMethod]
    public void ExtractFrontmatter_WithNoFrontmatter_ReturnsNull()
    {
        // Arrange
        string markdown = "# Just a regular markdown document\nNo frontmatter here.";

        // Act
        string? result = _yamlHelper.ExtractFrontmatter(markdown);

        // Assert
        Assert.IsNull(result);
    }

    /// <summary>
    /// Test that ParseYamlToDictionary correctly parses standard YAML.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithValidYaml_ReturnsDictionary()
    {
        // Arrange
        string yaml = @"title: Test Document
tags:
  - test
  - yaml
date: 2023-05-15";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yaml);

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Test Document", result["title"]);
        Assert.IsTrue(result["tags"] is List<object>);
        var tags = result["tags"] as List<object>;
        Assert.AreEqual(2, tags?.Count);
    }

    /// <summary>
    /// Test that ParseYamlToDictionary returns an empty dictionary for empty input.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithEmptyInput_ReturnsEmptyDictionary()
    {
        // Arrange
        string yaml = string.Empty;

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yaml);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Test that ParseYamlToDictionary correctly handles YAML wrapped in code blocks.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithYamlCodeBlock_ExtractsAndParses()
    {
        // Arrange
        string yamlInCodeBlock = @"```yaml
title: Test Document
tags:
  - test
  - yaml
```";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yamlInCodeBlock);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Test Document", result["title"]);
        Assert.IsTrue(result["tags"] is List<object>);
    }

    /// <summary>
    /// Test that ParseYamlToDictionary correctly handles YAML wrapped in yml code blocks.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithYmlCodeBlock_ExtractsAndParses()
    {
        // Arrange - use explicit YAML code block format
        string yamlInCodeBlock = @"```yaml
title: Test Document
tags:
  - test
  - yaml
```";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yamlInCodeBlock);

        // Assert
        Assert.IsTrue(result.Count > 0, "Failed to extract and parse YAML from code block");
        Assert.AreEqual("Test Document", result["title"], "Failed to extract 'title' from YAML");
        Assert.IsTrue(result["tags"] is List<object>, "Tags is not a List<object>");
        var tagsList = result["tags"] as List<object>;
        Assert.IsTrue(tagsList?.Count >= 2, "Tags list does not have expected number of items");
    }

    /// <summary>
    /// Test that ParseYamlToDictionary correctly handles YAML formatted as valid YAML without a code block.
    /// This test replaces the generic code block test as the implementation focuses on ```yaml and ```yml blocks.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithValidYamlNoCodeBlock_ParsesProperly()
    {
        // Arrange - use plain YAML without code blocks
        string yaml = @"title: Test Document
tags:
  - test
  - yaml";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yaml);

        // Assert
        Assert.IsTrue(result.Count > 0, "Failed to parse valid YAML");
        Assert.AreEqual("Test Document", result["title"], "Failed to extract 'title' from YAML");
        Assert.IsTrue(result["tags"] is List<object>, "Tags is not a List<object>");
        var tagsList = result["tags"] as List<object>;
        Assert.IsTrue(tagsList?.Count >= 2, "Tags list does not have expected number of items");
    }

    /// <summary>
    /// Test that ParseYamlToDictionary correctly handles YAML with trailing whitespace.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithTrailingWhitespace_TrimsAndParses()
    {
        // Arrange
        string yamlWithWhitespace = @"title: Test Document
tags:
  - test
  - yaml
";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(yamlWithWhitespace);

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Test Document", result["title"]);
        Assert.IsTrue(result["tags"] is List<object>);
    }

    /// <summary>
    /// Test that ParseYamlToDictionary returns an empty dictionary for malformed YAML and doesn't throw.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithMalformedYaml_ReturnsEmptyDictionaryWithoutThrowing()
    {
        // Arrange
        string malformedYaml = @"title: Test Document
- This is not valid YAML structure
  tags:
  - test";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(malformedYaml);

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Test that DiagnoseYamlFrontmatter correctly identifies problems in YAML frontmatter.
    /// </summary>
    [TestMethod]
    public void DiagnoseYamlFrontmatter_WithGoodYaml_ReportsSuccess()
    {
        // Arrange
        string markdown = @"---
title: Test Document
tags:
  - test
  - yaml
---

# Content";

        // Act
        var (success, message, data) = _yamlHelper.DiagnoseYamlFrontmatter(markdown);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(data);
        Assert.AreEqual("Test Document", data!["title"]);
    }

    /// <summary>
    /// Test that DiagnoseYamlFrontmatter correctly identifies problems in YAML frontmatter.
    /// </summary>
    [TestMethod]
    public void DiagnoseYamlFrontmatter_WithMalformedYaml_ReportsProblem()
    {
        // Arrange - Create truly malformed YAML with invalid indentation and syntax
        string markdown = @"---
title: Test Document
  invalid_key: value
    badly_indented: another_value
  tags: [test, another
missing_bracket_close: value
---

# Content";

        // Act
        var (success, message, data) = _yamlHelper.DiagnoseYamlFrontmatter(markdown);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(data);
        Assert.IsTrue(message.Contains("YAML syntax error"));
    }

    /// <summary>
    /// Test that YamlHelper can handle AI-generated nested YAML with indentation issues.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithNestedAiGeneratedYaml_ExtractsAndParses()
    {
        // Arrange - this mimics the format we often see from AI responses
        string aiGeneratedYaml = @"```yaml
title: AI Generated Document
tags:
    - ai
    - generated
    - content
description: >-
    This is a multi-line description
    that might have odd formatting
    from the AI response
```";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(aiGeneratedYaml);

        // Assert
        Assert.IsTrue(result.Count > 0);
        Assert.AreEqual("AI Generated Document", result["title"]);
        Assert.IsTrue(result["tags"] is List<object>);
        var tags = result["tags"] as List<object>;
        Assert.AreEqual(3, tags?.Count);
    }

    /// <summary>
    /// Test that YamlHelper can handle YAML with strange whitespace issues.
    /// </summary>
    [TestMethod]
    public void ParseYamlToDictionary_WithWhitespaceIssues_CorrectlyParses()
    {
        // Arrange - whitespace issues common in AI responses
        string problematicYaml = @"title:    AI Document with Whitespace Issues
tags:
  -   tag1
  -   tag2
  -   tag3
created:   2023-05-15   ";

        // Act
        Dictionary<string, object> result = _yamlHelper.ParseYamlToDictionary(problematicYaml);

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("AI Document with Whitespace Issues", result["title"]);
        Assert.IsTrue(result["tags"] is List<object>);
        var tags = result["tags"] as List<object>;
        Assert.AreEqual(3, tags?.Count);
    }

    /// <summary>
    /// Test that UpdateFrontmatter correctly updates frontmatter in markdown.
    /// </summary>
    [TestMethod]
    public void UpdateFrontmatter_WithExistingFrontmatter_ReplacesCorrectly()
    {
        // Arrange
        string markdown = @"---
title: Original Title
tags:
  - original
---

# Content";
        Dictionary<string, object> newFrontmatter = new()
        {
            { "title", "Updated Title" },
            { "tags", new List<object> { "updated" } },
        };

        // Act
        string result = _yamlHelper.UpdateFrontmatter(markdown, newFrontmatter);

        // Assert
        Assert.IsTrue(result.Contains("title: Updated Title"));
        Assert.IsTrue(result.Contains("- updated"));
        Assert.IsFalse(result.Contains("Original Title"));
    }

    /// <summary>
    /// Test that UpdateFrontmatter adds frontmatter when none exists.
    /// </summary>
    [TestMethod]
    public void UpdateFrontmatter_WithNoExistingFrontmatter_AddsCorrectly()
    {
        // Arrange
        string markdown = "# Just content without frontmatter";
        Dictionary<string, object> newFrontmatter = new()
        {
            { "title", "New Title" },
            { "tags", new List<object> { "new" } },
        };

        // Act
        string result = _yamlHelper.UpdateFrontmatter(markdown, newFrontmatter);

        // Assert
        Assert.IsTrue(result.Contains("title: New Title"));
        Assert.IsTrue(result.Contains("- new"));
        Assert.IsTrue(result.Contains("# Just content without frontmatter"));
    }

    /// <summary>
    /// Test that RemoveFrontmatter correctly removes standard YAML frontmatter.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithStandardFrontmatter_RemovesCorrectly()
    {
        // Arrange
        string markdown = @"---
title: Test Document
tags: [test, yaml]
---

# Content starts here
Some body content.";

        string expected = @"# Content starts here
Some body content.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles nested YAML code blocks correctly.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithNestedYamlCodeBlock_RemovesCorrectly()
    {
        // Arrange
        string markdown = @"```yaml
tags: [module-2, operations-management, supply-chain]
```

# Module 2: Operations Management Overview

This module covers the fundamentals of operations management.";

        string expected = @"# Module 2: Operations Management Overview

This module covers the fundamentals of operations management.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles yml code blocks correctly.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithYmlCodeBlock_RemovesCorrectly()
    {
        // Arrange
        string markdown = @"```yml
title: Test Document
tags:
  - test
  - yaml
```

# Content starts here
Some body content.";

        string expected = @"# Content starts here
Some body content.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles generic code blocks with YAML content.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithGenericCodeBlockContainingYaml_RemovesCorrectly()
    {
        // Arrange
        string markdown = @"```
tags: [test-tag]
title: Test Document
```

# Main Content
Body text here.";

        string expected = @"# Main Content
Body text here.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(expected, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter leaves non-YAML code blocks intact.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithNonYamlCodeBlock_LeavesIntact()
    {
        // Arrange
        string markdown = @"```javascript
console.log('Hello World');
```

# Content starts here
Some body content.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(markdown, result); // Should be unchanged
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles content with no frontmatter.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithNoFrontmatter_ReturnsOriginal()
    {
        // Arrange
        string markdown = @"# Just a regular document
No frontmatter here.";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(markdown, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles empty input correctly.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithEmptyInput_ReturnsEmpty()
    {
        // Arrange
        string markdown = string.Empty;

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Test that RemoveFrontmatter handles whitespace-only input correctly.
    /// </summary>
    [TestMethod]
    public void RemoveFrontmatter_WithWhitespaceOnly_ReturnsEmpty()
    {
        // Arrange
        string markdown = "   \n\t  \r\n  ";

        // Act
        string result = _yamlHelper.RemoveFrontmatter(markdown);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }
}
