// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Utilities;

/// <summary>
/// Unit tests for the BaseBlockGenerator class.
/// </summary>
[TestClass]
public class BaseBlockGeneratorTests
{
    private string _tempFilePath = null!;

    /// <summary>
    /// Initializes test setup before each test method.
    /// </summary>
    [TestInitialize]
    public void TestInitialize()
    {
        _tempFilePath = Path.GetTempFileName();
    }

    /// <summary>
    /// Cleans up after each test method.
    /// </summary>
    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    /// <summary>
    /// Tests that GenerateBaseBlock replaces all placeholders correctly.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithAllPlaceholders_ReplacesCorrectly()
    {
        // Arrange
        const string templateContent = @"
Course: {Course}
Class: {Class}
Module: {Module}
Type: {Type}
";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(
            _tempFilePath,
            "Mathematics",
            "Algebra",
            "Linear Equations",
            "Lesson");

        // Assert
        Assert.IsTrue(result.Contains("Course: Mathematics"));
        Assert.IsTrue(result.Contains("Class: Algebra"));
        Assert.IsTrue(result.Contains("Module: Linear Equations"));
        Assert.IsTrue(result.Contains("Type: Lesson"));
        Assert.IsFalse(result.Contains("{Course}"));
        Assert.IsFalse(result.Contains("{Class}"));
        Assert.IsFalse(result.Contains("{Module}"));
        Assert.IsFalse(result.Contains("{Type}"));
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles null parameters correctly.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithNullParameters_ReplacesWithEmptyString()
    {
        // Arrange
        const string templateContent = "Course: {Course}, Class: {Class}, Module: {Module}, Type: {Type}";
        File.WriteAllText(_tempFilePath, templateContent);        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, null!, null!, null!, null!);

        // Assert
        Assert.AreEqual("Course: , Class: , Module: , Type: ", result);
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles empty parameters correctly.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithEmptyParameters_ReplacesWithEmptyString()
    {
        // Arrange
        const string templateContent = "Course: {Course}, Class: {Class}, Module: {Module}, Type: {Type}";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, "", "", "", "");

        // Assert
        Assert.AreEqual("Course: , Class: , Module: , Type: ", result);
    }

    /// <summary>
    /// Tests that GenerateBaseBlock throws FileNotFoundException for non-existent template.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithNonExistentTemplate_ThrowsFileNotFoundException()
    {
        // Arrange
        const string nonExistentPath = "non-existent-template.yaml";

        // Act & Assert
        var exception = Assert.ThrowsException<FileNotFoundException>(() =>
            BaseBlockGenerator.GenerateBaseBlock(nonExistentPath, "course", "class", "module", "type"));

        Assert.IsTrue(exception.Message.Contains(nonExistentPath));
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles template without placeholders.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithoutPlaceholders_ReturnsOriginalContent()
    {
        // Arrange
        const string templateContent = "This is a template without any placeholders.";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, "course", "class", "module", "type");

        // Assert
        Assert.AreEqual(templateContent, result);
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles partial placeholders correctly.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithPartialPlaceholders_ReplacesOnlyExistingOnes()
    {
        // Arrange
        const string templateContent = "Course: {Course}, Module: {Module}, Some text without placeholders.";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(
            _tempFilePath,
            "Science",
            "Biology",
            "Cell Structure",
            "Lab");

        // Assert
        Assert.IsTrue(result.Contains("Course: Science"));
        Assert.IsTrue(result.Contains("Module: Cell Structure"));
        Assert.IsTrue(result.Contains("Some text without placeholders."));
        Assert.IsFalse(result.Contains("{Course}"));
        Assert.IsFalse(result.Contains("{Module}"));
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles multiple occurrences of same placeholder.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithMultiplePlaceholderOccurrences_ReplacesAll()
    {
        // Arrange
        const string templateContent = "Course: {Course}, Again: {Course}, Type: {Type}, Also Type: {Type}";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, "Physics", "class", "module", "Video");

        // Assert
        Assert.AreEqual("Course: Physics, Again: Physics, Type: Video, Also Type: Video", result);
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles special characters in parameters.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        const string templateContent = "Course: {Course}, Class: {Class}";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(
            _tempFilePath,
            "C# & .NET",
            "Advanced Programming (Part 1)",
            "module",
            "type");

        // Assert
        Assert.IsTrue(result.Contains("Course: C# & .NET"));
        Assert.IsTrue(result.Contains("Class: Advanced Programming (Part 1)"));
    }

    /// <summary>
    /// Tests that GenerateBaseBlock handles empty template file.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_WithEmptyTemplate_ReturnsEmptyString()
    {
        // Arrange
        File.WriteAllText(_tempFilePath, string.Empty);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, "course", "class", "module", "type");

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Tests that GenerateBaseBlock preserves formatting and whitespace.
    /// </summary>
    [TestMethod]
    public void GenerateBaseBlock_PreservesFormattingAndWhitespace()
    {
        // Arrange
        const string templateContent = @"
Base:
  course: {Course}
  class:   {Class}

  module: {Module}
    type: {Type}
";
        File.WriteAllText(_tempFilePath, templateContent);

        // Act
        string result = BaseBlockGenerator.GenerateBaseBlock(_tempFilePath, "Math", "Geometry", "Triangles", "Exercise");

        // Assert
        Assert.IsTrue(result.Contains("  course: Math"));
        Assert.IsTrue(result.Contains("  class:   Geometry"));
        Assert.IsTrue(result.Contains("  module: Triangles"));
        Assert.IsTrue(result.Contains("    type: Exercise"));
        // Check that line breaks are preserved
        StringAssert.Contains(result, "\n");
    }
}
