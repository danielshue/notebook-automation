using System.IO;

using Moq;

using NotebookAutomation.Core.Tools.MarkdownGeneration;

namespace NotebookAutomation.Core.Tests.Tools;

[TestClass]
public class MarkdownParserTests
{
    private Mock<ILogger> _mockLogger;
    private MarkdownParser _parser;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _parser = new MarkdownParser(_mockLogger.Object);
    }

    [TestMethod]
    public void ParseMarkdown_EmptyString_ReturnsEmptyFrontmatterAndContent()
    {
        (Dictionary<string, object> frontmatter, string content) = _parser.ParseMarkdown("");
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
            bool result = await _parser.WriteFileAsync(tempFile, frontmatter, content);
            Assert.IsTrue(result);
            string written = await File.ReadAllTextAsync(tempFile);
            Assert.IsTrue(written.Contains("title: Test"));
            Assert.IsTrue(written.Contains("# Heading"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [TestMethod]
    public async Task WriteFileAsync_Error_LogsAndReturnsFalse()
    {
        Dictionary<string, object> frontmatter = new() { { "title", "Test" } };
        string content = "# Heading\nBody";
        // Use an invalid path to force an error
        string invalidPath = Path.Combine(Path.GetTempPath(), "?invalid:file.md");
        bool result = await _parser.WriteFileAsync(invalidPath, frontmatter, content);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ParseFileAsync_FileNotFound_LogsAndReturnsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".md");
        (Dictionary<string, object> frontmatter, string content) = await _parser.ParseFileAsync(path);
        Assert.AreEqual(0, frontmatter.Count);
        Assert.AreEqual(string.Empty, content);
    }

    [TestMethod]
    public async Task ParseFileAsync_ValidFile_ParsesContent()
    {
        string tempFile = Path.GetTempFileName();
        string md = "---\ntitle: Test\n---\n# Heading\nBody";
        await File.WriteAllTextAsync(tempFile, md);
        try
        {
            (Dictionary<string, object> frontmatter, string content) = await _parser.ParseFileAsync(tempFile);
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

    [TestMethod]
    public void SanitizeForFilename_RemovesInvalidChars()
    {
        string input = "My:File/Name.txt";
        string sanitized = MarkdownParser.SanitizeForFilename(input);
        Assert.IsFalse(sanitized.Contains(':'));
        Assert.IsFalse(sanitized.Contains('/'));
        Assert.IsFalse(sanitized.Contains('.'));
        Assert.IsTrue(sanitized.Contains("my-file-name-txt"));
    }

    [TestMethod]
    public void SanitizeForFilename_Empty_ReturnsUnnamed()
    {
        string sanitized = MarkdownParser.SanitizeForFilename("");
        Assert.AreEqual("unnamed", sanitized);
    }
}
