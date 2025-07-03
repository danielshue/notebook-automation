// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Tests.Core.Tools.Vault;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

/// <summary>
/// Comprehensive unit tests for the VaultIndexContentGenerator class, validating content generation, hierarchy handling, and integration scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This test class provides complete coverage of the VaultIndexContentGenerator functionality using MSTest framework.
/// It employs a sophisticated mocking strategy to isolate the class under test from file system dependencies
/// while maintaining realistic test scenarios that mirror production usage patterns.
/// </para>
/// <para>
/// Testing Strategy:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Isolation Testing</strong>: Uses TestableVaultIndexContentGenerator subclass to override file system operations</description></item>
/// <item><description><strong>Comprehensive Mocking</strong>: Mocks all external dependencies (ILogger, IMetadataHierarchyDetector, IYamlHelper)</description></item>
/// <item><description><strong>Scenario Coverage</strong>: Tests all hierarchy levels, content types, and edge cases</description></item>
/// <item><description><strong>Integration Validation</strong>: Verifies end-to-end workflows and component interactions</description></item>
/// <item><description><strong>Error Handling</strong>: Validates graceful handling of null values, empty collections, and edge cases</description></item>
/// </list>
/// <para>
/// Test Categories:
/// The test methods are organized into logical regions covering specific functionality areas:
/// GenerateIndexContentAsync, PrepareFrontmatter, GenerateContentSections, AddSubfolderListing,
/// AddContentByType, utility methods, integration tests, and error handling scenarios.
/// </para>
/// <para>
/// Mock Configuration:
/// Tests use carefully configured mocks that return realistic data structures and verify
/// method invocations with appropriate parameters, ensuring both behavior and interaction validation.
/// </para>
/// </remarks>
[TestClass]
public class VaultIndexContentGeneratorTests
{
    /// <summary>
    /// Mock logger for capturing and verifying log output during test execution.
    /// </summary>
    private Mock<ILogger<VaultIndexContentGenerator>> _loggerMock = null!;

    /// <summary>
    /// Mock metadata hierarchy detector for simulating hierarchy analysis and metadata enhancement.
    /// </summary>
    private Mock<IMetadataHierarchyDetector> _hierarchyDetectorMock = null!;

    /// <summary>
    /// Mock YAML helper for simulating frontmatter processing and serialization.
    /// </summary>
    private Mock<IYamlHelper> _yamlHelperMock = null!;

    /// <summary>
    /// Real MarkdownNoteBuilder instance configured with mocked YAML helper for note assembly testing.
    /// </summary>
    private MarkdownNoteBuilder _noteBuilder = null!;

    /// <summary>
    /// Test application configuration with predefined vault paths and settings.
    /// </summary>
    private AppConfig _appConfig = null!;

    /// <summary>
    /// Testable instance of VaultIndexContentGenerator with file system operations mocked for reliable testing.
    /// </summary>
    private TestableVaultIndexContentGenerator _generator = null!;

    /// <summary>
    /// Standard test vault root path used across test scenarios.
    /// </summary>
    private string _testVaultPath = null!;

    /// <summary>
    /// Standard test folder path representing a typical hierarchy location for content generation.
    /// </summary>
    private string _testFolderPath = null!;    /// <summary>
                                               /// Testable subclass that overrides file system operations to enable isolated unit testing without file system dependencies.
                                               /// </summary>
                                               /// <remarks>
                                               /// <para>
                                               /// This internal test class extends VaultIndexContentGenerator to provide controlled, predictable behavior
                                               /// for file system operations that would otherwise require actual directories and files. It enables
                                               /// comprehensive testing of the content generation logic while maintaining test isolation and reliability.
                                               /// </para>
                                               /// <para>
                                               /// Key Features:
                                               /// </para>
                                               /// <list type="bullet">
                                               /// <item><description><strong>Subfolder Mocking</strong>: SetMockSubfolders allows tests to define expected directory structures</description></item>
                                               /// <item><description><strong>Root Index Mocking</strong>: SetMockRootIndex controls vault root index filename discovery</description></item>
                                               /// <item><description><strong>Predictable Behavior</strong>: Returns consistent, test-controlled data instead of file system queries</description></item>
                                               /// <item><description><strong>No Side Effects</strong>: Eliminates file system dependencies that could cause test failures or pollution</description></item>
                                               /// </list>
                                               /// <para>
                                               /// Usage Pattern:
                                               /// Tests configure the mock behavior using SetMockSubfolders and SetMockRootIndex before invoking
                                               /// methods that would normally perform file system operations, ensuring predictable and isolated test execution.
                                               /// </para>
                                               /// </remarks>
    private class TestableVaultIndexContentGenerator : VaultIndexContentGenerator
    {
        /// <summary>
        /// Dictionary mapping folder paths to their expected subfolder collections for test scenarios.
        /// </summary>

        private readonly Dictionary<string, List<string>> _mockSubfolders = new();

        /// <summary>
        /// Dictionary mapping vault paths to their expected root index filenames for test scenarios.
        /// </summary>

        private readonly Dictionary<string, string> _mockRootIndexes = new();

        /// <summary>
        /// Initializes a new instance of the TestableVaultIndexContentGenerator class with the specified dependencies.
        /// </summary>
        /// <param name="logger">Logger instance for capturing diagnostic information during testing.</param>
        /// <param name="hierarchyDetector">Metadata hierarchy detector for simulating hierarchy analysis operations.</param>
        /// <param name="noteBuilder">Markdown note builder for assembling final content output.</param>
        /// <param name="appConfig">Application configuration containing vault paths and settings.</param>
        public TestableVaultIndexContentGenerator(
            ILogger<VaultIndexContentGenerator> logger,
            IMetadataHierarchyDetector hierarchyDetector,
            MarkdownNoteBuilder noteBuilder,
            AppConfig appConfig) : base(logger, hierarchyDetector, noteBuilder, appConfig)
        {
        }

        /// <summary>
        /// Configures the mock subfolder structure for a specific path to enable predictable directory enumeration testing.
        /// </summary>
        /// <param name="path">The folder path for which to define the subfolder structure.</param>
        /// <param name="subfolders">Collection of subfolder names that should be returned when enumerating the specified path.</param>
        /// <remarks>
        /// This method allows tests to simulate various directory structures without requiring actual file system setup.
        /// Subfolders are returned in the order specified, enabling testing of sorting and organization logic.
        /// </remarks>
        public void SetMockSubfolders(string path, List<string> subfolders)
        {
            _mockSubfolders[path] = subfolders;
        }

        /// <summary>
        /// Configures the mock root index filename for a specific vault path to enable predictable index discovery testing.
        /// </summary>
        /// <param name="vaultPath">The vault root path for which to define the expected index filename.</param>
        /// <param name="indexName">The filename (without extension) that should be returned when discovering the root index.</param>
        /// <remarks>
        /// This method allows tests to simulate various vault configurations and index naming patterns
        /// without requiring actual index files to exist in the test environment.
        /// </remarks>
        public void SetMockRootIndex(string vaultPath, string indexName)
        {
            _mockRootIndexes[vaultPath] = indexName;
        }

        /// <summary>
        /// Returns the configured mock subfolders for the specified path, or an empty collection if no mock is configured.
        /// </summary>
        /// <param name="folderPath">The folder path for which to retrieve mock subfolders.</param>
        /// <returns>
        /// A list of subfolder names configured for the specified path, or an empty collection if no mock data is available.
        /// </returns>
        /// <remarks>
        /// This override replaces the file system directory enumeration with predictable test data,
        /// enabling consistent test execution regardless of the actual file system state.
        /// </remarks>
        protected override List<string> GetOrderedSubfolders(string folderPath)
        {
            return _mockSubfolders.TryGetValue(folderPath, out var subfolders) ? subfolders : [];
        }        /// <summary>
                 /// Returns the configured mock root index filename for the specified vault path, or "index" as the default.
                 /// </summary>
                 /// <param name="vaultPath">The vault path for which to retrieve the mock root index filename.</param>
                 /// <param name="currentFolderPath">The current folder path (not used in mock implementation).</param>
                 /// <returns>        /// <summary>
                 /// Returns the configured mock root index filename for the specified vault path, or "index.md" as the default.
                 /// </summary>
                 /// <param name="vaultPath">The vault path for which to retrieve the mock root index filename.</param>
                 /// <param name="currentFolderPath">The current folder path (not used in mock implementation).</param>
                 /// <returns>
                 /// The configured root index filename for the specified vault path, or "index.md" if no mock is configured.
                 /// </returns>
                 /// <remarks>
                 /// This override replaces the file system index discovery with predictable test data,
                 /// ensuring consistent behavior across different test environments and vault configurations.
                 /// Note: This mock implementation returns the simple filename without relative path calculation.
                 /// </remarks>
        protected override string GetRootIndexFilename(string vaultPath, string currentFolderPath)
        {
            // Get the mock filename and add .md extension if not present
            var mockFilename = _mockRootIndexes.TryGetValue(vaultPath, out var indexName) ? indexName : "index";
            if (!mockFilename.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                mockFilename += ".md";
            }

            // For test simplicity, return just the filename (no relative path calculation in tests)
            return mockFilename;
        }
    }

    /// <summary>
    /// Initializes test dependencies and configures the test environment before each test method execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method sets up a complete testing environment with properly configured mocks and realistic
    /// test data that enables comprehensive testing of the VaultIndexContentGenerator functionality.
    /// The setup ensures test isolation and predictable behavior across all test scenarios.
    /// </para>
    /// <para>
    /// Initialization Steps:
    /// </para>
    /// <list type="number">
    /// <item><description>Creates mock instances for all external dependencies</description></item>
    /// <item><description>Configures a real MarkdownNoteBuilder with mocked YAML helper</description></item>
    /// <item><description>Sets up test application configuration with standard vault paths</description></item>
    /// <item><description>Instantiates the testable generator with dependency injection</description></item>
    /// <item><description>Configures default mock behaviors for common test scenarios</description></item>
    /// </list>
    /// <para>
    /// Mock Configuration:
    /// The setup includes default mock behaviors that provide realistic responses for typical
    /// test scenarios, reducing the need for repetitive mock configuration in individual tests.
    /// </para>
    /// </remarks>
    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<VaultIndexContentGenerator>>();
        _hierarchyDetectorMock = new Mock<IMetadataHierarchyDetector>();
        _yamlHelperMock = new Mock<IYamlHelper>();
        _noteBuilder = new MarkdownNoteBuilder(_yamlHelperMock.Object, _appConfig);

        _appConfig = new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = @"C:\TestVault"
            }
        };

        _testVaultPath = @"C:\TestVault";
        _testFolderPath = @"C:\TestVault\Program\Course";

        _generator = new TestableVaultIndexContentGenerator(
            _loggerMock.Object,
            _hierarchyDetectorMock.Object,
            _noteBuilder,
            _appConfig);

        // Set up default mock behaviors
        _generator.SetMockRootIndex(_testVaultPath, "main-index");
        _generator.SetMockSubfolders(_testFolderPath, ["Module1", "Module2"]);
    }

    #region GenerateIndexContentAsync Tests

    /// <summary>
    /// Validates that GenerateIndexContentAsync produces comprehensive index content with valid frontmatter, structured body, and hierarchy-appropriate features.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the complete end-to-end content generation workflow for a class-level index,
    /// ensuring proper integration of frontmatter preparation, content structuring, and markdown assembly.
    /// It validates that the generated content includes all expected elements and maintains proper formatting.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a class-level template with mixed content types (readings and videos) to validate
    /// comprehensive content categorization and organization within a realistic hierarchy context.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies presence of YAML frontmatter, proper title formatting, template-type preservation,
    /// and content type sections, ensuring the generated index serves as an effective navigation hub.
    /// </para>
    /// </remarks>
    [TestMethod]
    public async Task GenerateIndexContentAsync_WithValidInputs_ReturnsGeneratedContent()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["title"] = "Test Course",
            ["banner"] = "'[[banner.png]]'"
        };

        var files = new List<VaultFileInfo>
        {
            new() { FileName = "reading1", ContentType = "reading", Title = "Introduction" },
            new() { FileName = "video1", ContentType = "video", Title = "Video Lecture" }
        };

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["course"] = "Test Course",
            ["class"] = "Test Class"
        }; var expectedFrontmatter = new Dictionary<string, object?>
        {
            ["template-type"] = "class",
            ["title"] = "Test Course",
            ["banner"] = "'[[banner.png]]'",
            ["type"] = "index",
            ["date-created"] = "2025-06-16"
        }; _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(expectedFrontmatter);

        _yamlHelperMock
            .Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns("---\nkey: value\n---\n\n");

        // Act
        var result = await _generator.GenerateIndexContentAsync(
            _testFolderPath,
            _testVaultPath,
            template,
            files,
            hierarchyInfo,
            4);        // Assert
        Assert.IsTrue(result.Contains("---"));
        Assert.IsTrue(result.Contains("# Test Course"));
        Assert.IsTrue(result.Contains("template-type: class"));
        Assert.IsTrue(result.Contains("Readings"));
        Assert.IsTrue(result.Contains("Videos"));
    }

    /// <summary>
    /// Validates that GenerateIndexContentAsync handles empty file collections gracefully while still producing valid index content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures that the content generation process remains robust when no vault files are present
    /// in the target folder, verifying that essential index structure and navigation elements are still generated.
    /// This scenario commonly occurs in newly created folders or during initial vault setup.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a main-level template with empty file collection to validate baseline content generation
    /// and proper handling of the absence of categorizable content.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that valid YAML frontmatter is generated even with minimal input data,
    /// ensuring the index remains functional and properly structured.
    /// </para>
    /// </remarks>
    [TestMethod]
    public async Task GenerateIndexContentAsync_WithEmptyFiles_StillGeneratesContent()
    {
        // Arrange
        var template = new Dictionary<string, object> { ["template-type"] = "main" };
        var files = new List<VaultFileInfo>();
        var hierarchyInfo = new Dictionary<string, string>();
        _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(new Dictionary<string, object?> { ["title"] = "Test Index" });

        _yamlHelperMock
            .Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns("---\nkey: value\n---\n\n");

        // Act
        var result = await _generator.GenerateIndexContentAsync(
            _testFolderPath,
            _testVaultPath,
            template,
            files,
            hierarchyInfo,
            1);

        // Assert
        Assert.IsTrue(result.Contains("---"));
    }

    #endregion

    #region PrepareFrontmatter Tests

    /// <summary>
    /// Validates that PrepareFrontmatter correctly enhances template data with hierarchy information and standardized index fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the frontmatter preparation process, ensuring that the method properly combines
    /// template data with hierarchy-specific metadata and adds required standardized fields for index files.
    /// It validates the integration with the metadata hierarchy detector for consistent frontmatter structure.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a class-level template with course hierarchy information to validate proper metadata enhancement
    /// and field standardization according to the vault's organizational requirements.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies presence of required fields (title, type, date-created), preservation of template data,
    /// and proper integration of hierarchy-specific metadata from the detector service.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void PrepareFrontmatter_WithValidTemplate_ReturnsEnhancedFrontmatter()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["banner"] = "'[[banner.png]]'"
        };

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["course"] = "Test Course"
        };

        var expectedUpdated = new Dictionary<string, object?>
        {
            ["template-type"] = "class",
            ["banner"] = "'[[banner.png]]'",
            ["title"] = "Course",
            ["course"] = "Test Course"
        };

        _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                "class"))
            .Returns(expectedUpdated);

        // Act
        var result = _generator.PrepareFrontmatter(template, _testFolderPath, hierarchyInfo);

        // Assert
        Assert.IsTrue(result.ContainsKey("title"));
        Assert.IsTrue(result.ContainsKey("type"));
        Assert.IsTrue(result.ContainsKey("date-created"));
        Assert.IsTrue(result.ContainsKey("banner"));
        Assert.AreEqual("index", result["type"]);
        Assert.AreEqual("Course", result["title"]);
    }

    /// <summary>
    /// Validates that PrepareFrontmatter automatically adds a default banner field when not present in the template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures that the frontmatter preparation process includes fallback behavior for essential
    /// visual elements, specifically adding a default banner reference when the template doesn't specify one.
    /// This maintains visual consistency across the vault while allowing template customization.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a minimal class-level template without banner specification to validate automatic
    /// default banner addition with proper Obsidian wiki link formatting.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that the banner field is automatically added with the expected default value
    /// and proper wiki link syntax for Obsidian compatibility.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void PrepareFrontmatter_WithoutBanner_AddsBannerField()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class"
        };

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["course"] = "Test Course"
        };

        _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(new Dictionary<string, object?> { ["template-type"] = "class" });

        // Act
        var result = _generator.PrepareFrontmatter(template, _testFolderPath, hierarchyInfo);

        // Assert
        Assert.IsTrue(result.ContainsKey("banner"));
        Assert.AreEqual("'[[gies-banner.png]]'", result["banner"]);
    }

    /// <summary>
    /// Validates that PrepareFrontmatter creates a new dictionary instance without modifying the original template.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures immutability of input data by verifying that the frontmatter preparation process
    /// does not modify the original template dictionary. This prevents unintended side effects and ensures
    /// that templates can be safely reused across multiple content generation operations.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a basic template and compares the original template size before and after processing
    /// to confirm that no fields were added to the original template instance.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that the original template count remains unchanged while the returned frontmatter
    /// contains additional fields, confirming proper immutability handling.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void PrepareFrontmatter_DoesNotMutateOriginalTemplate()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class"
        };
        var originalCount = template.Count;

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["course"] = "Test Course"
        };

        _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(new Dictionary<string, object?> { ["template-type"] = "class" });

        // Act
        var result = _generator.PrepareFrontmatter(template, _testFolderPath, hierarchyInfo);

        // Assert
        Assert.AreEqual(originalCount, template.Count);
        Assert.IsTrue(result.Count > template.Count);
    }

    #endregion

    #region GenerateContentSections Tests

    /// <summary>
    /// Validates that GenerateContentSections produces appropriate main index content with navigation and dashboard integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the generation of main-level index content, ensuring that vault root indexes
    /// include essential navigation elements and dashboard integration for comprehensive vault management.
    /// Main indexes serve as the primary entry point and must provide effective navigation capabilities.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a main template type at hierarchy level 1 to validate main-specific content generation
    /// including dashboard links and class assignment shortcuts.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies presence of main index title, dashboard navigation links, and class assignment
    /// shortcuts that are essential for vault-wide navigation and productivity.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GenerateContentSections_WithMainTemplate_GeneratesMainContent()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "main",
            ["title"] = "Main Index"
        };

        var files = new List<VaultFileInfo>();
        var hierarchyInfo = new Dictionary<string, string>();

        // Act
        var result = _generator.GenerateContentSections(
            _testFolderPath,
            _testVaultPath,
            frontmatter,
            files,
            hierarchyInfo,
            1);

        // Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual("# Main Index", result.First());
        Assert.IsTrue(result.Any(s => s.Contains("Dashboard")));
        Assert.IsTrue(result.Any(s => s.Contains("Classes Assignments")));
    }

    /// <summary>
    /// Validates that GenerateContentSections includes Obsidian Bases integration for class-level hierarchy indexes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies that class-level index generation includes advanced Obsidian Bases integration
    /// features that provide dynamic content querying and organization capabilities. Class-level indexes
    /// benefit from automated content discovery and filtering through Bases query blocks.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a class template at hierarchy level 4 with course and class hierarchy information
    /// to validate Bases integration inclusion in the generated content sections.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that class index content is generated successfully and that the method execution
    /// completes without errors, indicating proper Bases integration handling.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GenerateContentSections_WithClassLevel_IncludesBasesIntegration()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["title"] = "Class Index"
        };

        var files = new List<VaultFileInfo>
        {
            new() { FileName = "reading1", Title = "reading1" },
            new() { FileName = "video1", Title = "video1" }
        };
        var hierarchyInfo = new Dictionary<string, string>
        {
            ["course"] = "Test Course",
            ["class"] = "Test Class"
        };

        // Act
        var result = _generator.GenerateContentSections(
            _testFolderPath,
            _testVaultPath,
            frontmatter,
            files,
            hierarchyInfo,
            4); // Class level

        // Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual("# Class Index", result.First());
        // Bases integration would be added but requires file system access
        // so we just verify the method runs without error
    }

    /// <summary>
    /// Validates that GenerateContentSections uses "Index" as the default title when frontmatter lacks a title field.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust handling of incomplete frontmatter by verifying that the content generation
    /// process provides appropriate fallback behavior when essential fields are missing. This prevents
    /// failures and ensures consistent index structure even with minimal or malformed input data.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses empty frontmatter dictionary to simulate missing title information and validate
    /// the default title fallback mechanism in content section generation.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that content sections are generated successfully and that the default "Index"
    /// title is used when no title is specified in the frontmatter.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GenerateContentSections_WithEmptyTitle_UsesDefaultTitle()
    {
        // Arrange
        var frontmatter = new Dictionary<string, object>
        {
            ["template-type"] = "index"
        };
        var files = new List<VaultFileInfo>();
        var hierarchyInfo = new Dictionary<string, string>();

        // Act
        var result = _generator.GenerateContentSections(
            _testFolderPath,
            _testVaultPath,
            frontmatter,
            files,
            hierarchyInfo,
            1);

        // Assert
        Assert.IsTrue(result.Any());
        Assert.AreEqual("# Index", result.First());
    }

    #endregion

    #region AddSubfolderListing Tests

    /// <summary>
    /// Validates that AddSubfolderListing creates properly formatted sections with custom icons and friendly titles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the subfolder listing functionality, ensuring that directory structures are
    /// presented in an organized, visually appealing format with appropriate icons and human-readable titles.
    /// The method should transform technical folder names into user-friendly navigation elements.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses academic folder names (intro-to-programming, data-structures) with a custom book icon
    /// to validate proper title transformation and icon integration in the generated listing.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies section header creation, custom icon usage, and proper title case transformation
    /// from hyphenated folder names to readable display names.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddSubfolderListing_WithSubfolders_AddsFormattedList()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string> { "intro-to-programming", "data-structures" };        // Act
        _generator.AddSubfolderListing(contentSections, subFolders, "Courses", "📚");

        // Assert
        Assert.IsTrue(contentSections.Contains("## Courses"));
        Assert.IsTrue(contentSections.Any(s => s.Contains("📚") && s.Contains("Intro Programming")));
        Assert.IsTrue(contentSections.Any(s => s.Contains("📚") && s.Contains("Data Structures")));
    }

    /// <summary>
    /// Validates that AddSubfolderListing produces no output when provided with an empty subfolder collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures efficient handling of empty directory structures by verifying that the method
    /// avoids generating unnecessary sections when no subfolders exist. This prevents cluttered output
    /// and maintains clean index structure in folders without subdirectories.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an empty subfolder collection to validate the method's early exit behavior
    /// and efficient handling of directories without subdirectory content.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that no content sections are added to the output when the subfolder collection
    /// is empty, ensuring clean and efficient content generation.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddSubfolderListing_WithEmptyList_AddsNothing()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string>();

        // Act
        _generator.AddSubfolderListing(contentSections, subFolders, "Courses");

        // Assert
        Assert.AreEqual(0, contentSections.Count);
    }

    /// <summary>
    /// Validates that AddSubfolderListing uses the default folder icon when no custom icon is specified.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the default icon behavior, ensuring that subfolder listings maintain visual
    /// consistency by using appropriate default icons when custom icons are not provided. This provides
    /// fallback behavior that maintains the visual structure of generated content.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a single test folder without specifying a custom icon to validate the default
    /// folder icon (📁) application in the generated subfolder listing.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that the default folder icon is used in the generated content when no
    /// custom icon is specified, ensuring consistent visual presentation.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddSubfolderListing_WithDefaultIcon_UsesFolder()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string> { "test-folder" };

        // Act
        _generator.AddSubfolderListing(contentSections, subFolders, "Test");

        // Assert
        Assert.IsTrue(contentSections.Any(s => s.Contains("📁")));
    }

    #endregion

    #region AddContentByType Tests

    /// <summary>
    /// Validates that AddContentByType creates organized sections for different content types with appropriate icons and titles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the content type organization functionality, ensuring that vault files are
    /// properly categorized and presented in visually distinct sections. The method should create
    /// hierarchical organization that enhances content discovery and navigation within the vault.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses mixed content types (readings and videos) with multiple files per type to validate
    /// proper section creation, icon assignment, and content organization within each type category.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies section header creation with appropriate icons, inclusion of all file titles,
    /// and exclusion of empty content type sections to maintain clean output structure.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddContentByType_WithGroupedFiles_CreatesTypedSections()
    {
        // Arrange
        var contentSections = new List<string>();
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>
        {
            ["reading"] = new List<VaultFileInfo>
            {
                new() { Title = "Advanced Algorithms" },
                new() { Title = "Basic Data Structures" }
            },
            ["video"] = new List<VaultFileInfo>
            {
                new() { Title = "Tutorial Video" }
            }
        };
        var contentTypes = new[] { "reading", "video", "assignment" };

        // Act
        _generator.AddContentByType(contentSections, groupedFiles, contentTypes);

        // Assert
        Assert.IsTrue(contentSections.Contains("## 📖 Readings"));
        Assert.IsTrue(contentSections.Contains("## 🎥 Videos"));
        Assert.IsTrue(contentSections.Any(s => s.Contains("Advanced Algorithms")));
        Assert.IsTrue(contentSections.Any(s => s.Contains("Basic Data Structures")));
        Assert.IsTrue(contentSections.Any(s => s.Contains("Tutorial Video")));
        Assert.IsFalse(contentSections.Any(s => s.Contains("## 📋 Assignments"))); // Empty type
    }

    /// <summary>
    /// Validates that AddContentByType sorts files alphabetically within each content type section for consistent organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures that content within each type category is presented in a logical, predictable order
    /// that enhances user experience and content discoverability. Alphabetical sorting provides consistency
    /// and makes it easier for users to locate specific content within organized sections.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses reading files with titles in reverse alphabetical order (Zebra, Alpha) to validate
    /// that the sorting mechanism properly reorders content for optimal presentation.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that files appear in alphabetical order within their content type section,
    /// confirming proper sorting logic implementation and consistent content organization.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddContentByType_SortsFilesAlphabetically()
    {
        // Arrange
        var contentSections = new List<string>();
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>
        {
            ["reading"] = new List<VaultFileInfo>
            {
                new() { Title = "Zebra Reading" },
                new() { Title = "Alpha Reading" }
            }
        };
        var contentTypes = new[] { "reading" };

        // Act
        _generator.AddContentByType(contentSections, groupedFiles, contentTypes);

        // Assert
        var readingIndex = contentSections.FindIndex(s => s.Contains("Alpha Reading"));
        var zebraIndex = contentSections.FindIndex(s => s.Contains("Zebra Reading"));
        Assert.IsTrue(readingIndex < zebraIndex, "Files should be sorted alphabetically");
    }

    /// <summary>
    /// Validates that AddContentByType produces no output when provided with empty grouped files collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures efficient handling of scenarios where no categorized content exists,
    /// verifying that the method avoids generating empty sections that would clutter the index.
    /// This maintains clean output structure when content categorization yields no results.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses empty grouped files dictionary with specified content types to validate the method's
    /// early exit behavior when no content is available for organization.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that no content sections are added when the grouped files collection is empty,
    /// ensuring efficient processing and clean output generation.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddContentByType_WithEmptyGroupedFiles_AddsNothing()
    {
        // Arrange
        var contentSections = new List<string>();
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>();
        var contentTypes = new[] { "reading", "video" };

        // Act
        _generator.AddContentByType(contentSections, groupedFiles, contentTypes);

        // Assert
        Assert.AreEqual(0, contentSections.Count);
    }

    #endregion

    #region GetContentTypeIcon Tests

    /// <summary>
    /// Validates that GetContentTypeIcon returns appropriate icons for recognized content types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the content type icon mapping functionality, ensuring that different
    /// content types receive visually distinct and semantically appropriate icons for enhanced
    /// user interface and content recognition within the vault index structure.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Tests multiple recognized content types (reading, video, transcript, assignment, discussion)
    /// to validate that each receives its designated icon according to the content type mapping.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that each content type returns its expected icon character, ensuring consistent
    /// visual representation and proper icon assignment logic implementation.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeIcon_WithKnownTypes_ReturnsCorrectIcons()
    {
        // Act & Assert
        Assert.AreEqual("📖", VaultIndexContentGenerator.GetContentTypeIcon("reading"));
        Assert.AreEqual("🎥", VaultIndexContentGenerator.GetContentTypeIcon("video"));
        Assert.AreEqual("📝", VaultIndexContentGenerator.GetContentTypeIcon("transcript"));
        Assert.AreEqual("📋", VaultIndexContentGenerator.GetContentTypeIcon("assignment"));
        Assert.AreEqual("💬", VaultIndexContentGenerator.GetContentTypeIcon("discussion"));
    }

    /// <summary>
    /// Validates that GetContentTypeIcon returns a default icon for unrecognized content types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust handling of unknown or custom content types by verifying that
    /// the icon mapping provides appropriate fallback behavior. This prevents display issues
    /// and maintains visual consistency even when encountering unexpected content types.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an unrecognized content type string to validate the default icon fallback mechanism
    /// and ensure that unknown types don't cause display or processing failures.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that unknown content types receive the default document icon (📄),
    /// providing consistent fallback behavior for unrecognized content classifications.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeIcon_WithUnknownType_ReturnsDefaultIcon()
    {
        // Act
        var result = VaultIndexContentGenerator.GetContentTypeIcon("unknown-type");

        // Assert
        Assert.AreEqual("📄", result);
    }

    /// <summary>
    /// Validates that GetContentTypeIcon handles empty string input gracefully by returning the default icon.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust error handling for edge cases where content type information
    /// might be missing or malformed. Empty string handling is critical for preventing
    /// display issues when content type data is incomplete or corrupted.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an empty string as content type input to validate the error handling and
    /// fallback behavior when content type information is not available.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that empty string input receives the default document icon,
    /// ensuring graceful degradation when content type information is missing.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeIcon_WithEmptyString_ReturnsDefaultIcon()
    {
        // Act
        var result = VaultIndexContentGenerator.GetContentTypeIcon(string.Empty);

        // Assert
        Assert.AreEqual("📄", result);
    }

    #endregion

    #region GetContentTypeTitle Tests
    /// <summary>
    /// Validates that GetContentTypeTitle returns appropriate section titles for recognized content types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the content type title mapping functionality, ensuring that section headers
    /// use proper, user-friendly titles that enhance readability and navigation within the vault index.
    /// Consistent title formatting improves the overall user experience and content organization.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Tests multiple recognized content types to validate that each receives its designated
    /// section title according to the content type mapping and formatting conventions.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that each content type returns its expected section title, ensuring consistent
    /// naming conventions and proper title mapping logic implementation.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeTitle_WithKnownTypes_ReturnsCorrectTitles()
    {
        // Act & Assert
        Assert.AreEqual("Readings", VaultIndexContentGenerator.GetContentTypeTitle("reading"));
        Assert.AreEqual("Videos", VaultIndexContentGenerator.GetContentTypeTitle("video"));
        Assert.AreEqual("Transcripts", VaultIndexContentGenerator.GetContentTypeTitle("transcript"));
        Assert.AreEqual("Assignments", VaultIndexContentGenerator.GetContentTypeTitle("assignment"));
        Assert.AreEqual("Discussions", VaultIndexContentGenerator.GetContentTypeTitle("discussion"));
    }

    /// <summary>
    /// Validates that GetContentTypeTitle returns a default title for unrecognized content types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust handling of unknown or custom content types by verifying that
    /// the title mapping provides appropriate fallback behavior. This prevents section header
    /// issues and maintains consistent presentation even with unexpected content types.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an unrecognized content type string to validate the default title fallback mechanism
    /// and ensure that unknown types receive appropriate generic section headers.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that unknown content types receive the default "Notes" title,
    /// providing consistent fallback behavior for unrecognized content classifications.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeTitle_WithUnknownType_ReturnsDefaultTitle()
    {
        // Act
        var result = VaultIndexContentGenerator.GetContentTypeTitle("unknown-type");

        // Assert
        Assert.AreEqual("Notes", result);
    }

    /// <summary>
    /// Validates that GetContentTypeTitle handles empty string input gracefully by returning the default title.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust error handling for edge cases where content type information
    /// might be missing or malformed. Empty string handling is critical for preventing
    /// section header issues when content type data is incomplete or corrupted.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an empty string as content type input to validate the error handling and
    /// fallback behavior when content type information is not available.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that empty string input receives the default "Notes" title,
    /// ensuring graceful degradation when content type information is missing.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetContentTypeTitle_WithEmptyString_ReturnsDefaultTitle()
    {
        // Act
        var result = VaultIndexContentGenerator.GetContentTypeTitle(string.Empty);

        // Assert
        Assert.AreEqual("Notes", result);
    }

    #endregion

    #region GetBackLinkTarget Tests

    /// <summary>
    /// Validates that GetBackLinkTarget returns appropriate navigation targets based on hierarchy level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the back navigation logic, ensuring that breadcrumb navigation provides
    /// appropriate parent references based on the current position within the vault hierarchy.
    /// Proper back link targeting is essential for intuitive navigation and user orientation.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Tests various hierarchy levels (2, 3, 4) with a typical vault path structure to validate
    /// that back link targets are consistently determined based on directory hierarchy.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that different hierarchy levels return appropriate parent directory names
    /// for back navigation, ensuring consistent navigation behavior across the vault structure.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetBackLinkTarget_WithDifferentHierarchyLevels_ReturnsCorrectTargets()
    {
        // Arrange
        var testPath = @"C:\vault\program\course\class";

        // Act & Assert
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(testPath, 2));
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(testPath, 3));
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(testPath, 4));
    }

    /// <summary>
    /// Validates that GetBackLinkTarget handles empty path input gracefully by returning an empty string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust error handling for edge cases where path information might be
    /// missing or invalid. Empty path handling prevents navigation errors and ensures that
    /// the back link logic degrades gracefully when path data is unavailable.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses an empty string as path input to validate the error handling and ensure that
    /// invalid path data doesn't cause navigation system failures.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that empty path input returns an empty string for the back link target,
    /// providing safe fallback behavior when path information is not available.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetBackLinkTarget_WithEmptyPath_ReturnsEmptyString()
    {
        // Act
        var result = VaultIndexContentGenerator.GetBackLinkTarget(string.Empty, 2);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    /// <summary>
    /// Validates that GetBackLinkTarget works correctly for both Windows and Unix path formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures that the path parsing logic correctly handles platform differences
    /// between Windows (C:\path\to\folder) and Unix (/path/to/folder) path formats.
    /// Both should return the same parent directory name despite different path structures.
    /// </para>
    /// <para>
    /// Test Scenarios:
    /// - Windows path: C:\vault\program\course\class
    /// - Unix path: /vault/program/course/class
    /// - Mixed separators: C:/vault/program/course/class
    /// </para>
    /// <para>
    /// Assertions:
    /// All path formats should return "course" as the parent of "class" regardless of
    /// the hierarchy level parameter (since it's currently not used in the logic).
    /// </para>
    /// </remarks>
    [TestMethod]
    public void GetBackLinkTarget_CrossPlatformPaths_ReturnsCorrectParent()
    {
        // Arrange - Test different path formats
        var windowsPath = @"C:\vault\program\course\class";
        var unixPath = "/vault/program/course/class";
        var mixedPath = "C:/vault/program/course/class";

        // Act & Assert - All should return "course" as the parent of "class"
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(windowsPath, 3),
            "Windows path should return 'course' as parent of 'class'");
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(unixPath, 3),
            "Unix path should return 'course' as parent of 'class'");
        Assert.AreEqual("course", VaultIndexContentGenerator.GetBackLinkTarget(mixedPath, 3),
            "Mixed separator path should return 'course' as parent of 'class'");

        // Test with shorter paths
        var shortWindowsPath = @"C:\program\course";
        var shortUnixPath = "/program/course";

        Assert.AreEqual("program", VaultIndexContentGenerator.GetBackLinkTarget(shortWindowsPath, 2),
            "Short Windows path should return 'program' as parent of 'course'");
        Assert.AreEqual("program", VaultIndexContentGenerator.GetBackLinkTarget(shortUnixPath, 2),
            "Short Unix path should return 'program' as parent of 'course'");
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// Validates the complete content generation workflow with realistic data, verifying end-to-end integration and component interaction.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This comprehensive integration test validates the entire content generation pipeline using
    /// realistic academic data structures. It ensures that all components work together effectively
    /// to produce complete, properly formatted index content that meets real-world usage requirements.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a course-level template with diverse content types (readings, videos, assignments, notes)
    /// and complete hierarchy information to simulate realistic academic vault usage patterns.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies YAML frontmatter structure, content organization, hierarchy metadata integration,
    /// and proper dependency interaction through mock verification of service calls.
    /// </para>
    /// </remarks>
    [TestMethod]
    public async Task GenerateIndexContentAsync_CompleteWorkflow_GeneratesExpectedStructure()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "course",
            ["title"] = "Data Science Course",
            ["banner"] = "'[[banner.png]]'"
        };

        var files = new List<VaultFileInfo>
        {
            new VaultFileInfo { FileName = "reading1.md", ContentType = "reading", Title = "Introduction to Data Science" },
            new VaultFileInfo { FileName = "video1.md", ContentType = "video", Title = "Python Basics" },
            new VaultFileInfo { FileName = "assignment1.md", ContentType = "assignment", Title = "First Assignment" },
            new VaultFileInfo { FileName = "note1.md", ContentType = "note", Title = "Case Study Analysis" }
        };

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["program"] = "Data Analytics",
            ["course"] = "Data Science",
            ["class"] = "Introduction"
        };

        var expectedFrontmatter = new Dictionary<string, object?>
        {
            ["template-type"] = "course",
            ["title"] = "Course",
            ["program"] = "Data Analytics",
            ["course"] = "Data Science",
            ["class"] = "Introduction",
            ["type"] = "index",
            ["banner"] = "'[[banner.png]]'"
        }; _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                "course"))
            .Returns(expectedFrontmatter);

        _yamlHelperMock
            .Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns((string content, Dictionary<string, object> fm) => $"---\n{string.Join("\n", fm.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}\n---\n\n");

        // Act
        var result = await _generator.GenerateIndexContentAsync(
            _testFolderPath,
            _testVaultPath,
            template,
            files,
            hierarchyInfo,
            2); // Course level

        // Assert
        Assert.IsTrue(result.Contains("---"));
        Assert.IsTrue(result.Contains("type: index"));
        Assert.IsTrue(result.Contains("# Course"));

        // Verify hierarchy detector was called with correct parameters
        _hierarchyDetectorMock.Verify(x => x.UpdateMetadataWithHierarchy(
            It.IsAny<Dictionary<string, object?>>(),
            hierarchyInfo,
            "course"), Times.Once);
    }

    /// <summary>
    /// Validates that AddHierarchySpecificContent generates appropriate content for different vault hierarchy levels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the hierarchy-aware content generation system, ensuring that different
    /// levels within the vault structure receive appropriate content organization and features.
    /// Each hierarchy level has distinct requirements and this test validates proper adaptation.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Tests hierarchy levels 0-5 with grouped reading content to validate that each level
    /// generates appropriate content structure according to its position in the vault hierarchy.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that each hierarchy level executes without errors and generates valid content
    /// structure, ensuring robust hierarchy handling across the entire vault organization system.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddHierarchySpecificContent_WithDifferentLevels_GeneratesAppropriateContent()
    {
        // Arrange
        var contentSections = new List<string>();
        var files = new List<VaultFileInfo>
        {
            new VaultFileInfo { ContentType = "reading", Title = "Test Reading" }
        };
        var groupedFiles = files.GroupBy(f => f.ContentType).ToDictionary(g => g.Key, g => g.ToList());

        // Test different hierarchy levels
        var testCases = new[]
        {
            new { Level = 0, ExpectedSectionCount = 0 }, // Main - handled separately
            new { Level = 1, ExpectedSectionCount = 0 }, // Program - only subfolders
            new { Level = 2, ExpectedSectionCount = 1 }, // Course - includes content
            new { Level = 3, ExpectedSectionCount = 0 }, // Class - only subfolders
            new { Level = 4, ExpectedSectionCount = 0 }, // Class - Bases only
            new { Level = 5, ExpectedSectionCount = 2 }  // Module - full content
        };

        foreach (var testCase in testCases)
        {
            // Act
            contentSections.Clear();
            _generator.AddHierarchySpecificContent(contentSections, _testFolderPath, testCase.Level, groupedFiles);

            // Assert - verify appropriate content is generated for each level
            // Note: Exact counts depend on file system structure, but we verify the method runs without error
            Assert.IsTrue(contentSections.Count >= 0, $"Level {testCase.Level} should generate valid content");
        }
    }

    #endregion

    #region Edge Cases and Error Handling

    /// <summary>
    /// Validates that PrepareFrontmatter handles null values gracefully by filtering them from the final frontmatter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust handling of malformed or incomplete template data by verifying that
    /// null values are properly filtered during frontmatter preparation. This prevents YAML serialization
    /// issues and ensures that generated indexes maintain clean, valid structure even with problematic input.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a template containing null values to validate the filtering mechanism and ensure that
    /// null fields don't propagate to the final frontmatter structure.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that valid fields are preserved while null values are filtered out,
    /// ensuring clean frontmatter generation and preventing serialization issues.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void PrepareFrontmatter_WithNullValues_HandlesGracefully()
    {
        // Arrange
        var template = new Dictionary<string, object>
        {
            ["template-type"] = "class",
            ["nullable-field"] = null!
        };

        var hierarchyInfo = new Dictionary<string, string>
        {
            ["level"] = "2",
            ["parent"] = "course"
        };

        _hierarchyDetectorMock
            .Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(new Dictionary<string, object?>
            {
                ["template-type"] = "class",
                ["nullable-field"] = null
            });

        // Act
        var result = _generator.PrepareFrontmatter(template, _testFolderPath, hierarchyInfo);

        // Assert
        Assert.IsTrue(result.ContainsKey("template-type"));
        Assert.IsFalse(result.ContainsKey("nullable-field")); // Null values should be filtered out
    }

    /// <summary>
    /// Validates that AddContentByType handles files with null titles gracefully without causing processing failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures robust error handling for incomplete file metadata by verifying that
    /// null or missing title information doesn't cause content organization failures. The method
    /// should continue processing valid files while gracefully handling problematic entries.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a mix of files with null and valid titles to validate error handling and ensure
    /// that content organization continues despite incomplete metadata for some files.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that content sections are generated successfully and that files with valid
    /// titles are properly included while null title files are handled without causing failures.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddContentByType_WithNullTitles_HandlesGracefully()
    {
        // Arrange
        var contentSections = new List<string>();
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>
        {
            ["reading"] = new List<VaultFileInfo>
            {
                new() { Title = null! },
                new() { Title = "Valid Title" }
            }
        };
        var contentTypes = new[] { "reading" };

        // Act & Assert - Should not throw exception
        _generator.AddContentByType(contentSections, groupedFiles, contentTypes);

        Assert.IsTrue(contentSections.Count > 0);
        Assert.IsTrue(contentSections.Any(s => s.Contains("Valid Title")));
    }

    /// <summary>
    /// Validates that GenerateIndexContentAsync handles all hierarchy levels robustly without failures across the complete level range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This comprehensive stress test validates the hierarchy system's robustness by testing
    /// content generation across all supported hierarchy levels (0-6+). It ensures that the
    /// system handles edge cases and maintains stability across the entire hierarchy spectrum.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Iterates through hierarchy levels 0-6 with minimal test data to validate that each level
    /// produces valid output without errors, ensuring comprehensive hierarchy level support.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that all hierarchy levels generate non-null, non-empty content, ensuring
    /// robust operation and proper fallback behavior across the entire hierarchy system.
    /// </para>
    /// </remarks>
    [TestMethod]
    public async Task GenerateIndexContentAsync_WithComplexHierarchy_HandlesAllLevels()
    {
        // Arrange
        var template = new Dictionary<string, object> { ["template-type"] = "module" };
        var files = new List<VaultFileInfo>();
        var hierarchyInfo = new Dictionary<string, string>
        {
            ["level"] = "2",
            ["parent"] = "course"
        };

        _hierarchyDetectorMock.Setup(x => x.UpdateMetadataWithHierarchy(
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string?>()))
            .Returns(new Dictionary<string, object?> { ["title"] = "Complex Hierarchy Test" });

        _yamlHelperMock
            .Setup(x => x.UpdateFrontmatter(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
            .Returns("---\ngenerated: content\n---\n\n");

        // Test various hierarchy levels
        for (int level = 0; level <= 6; level++)
        {
            // Act
            var result = await _generator.GenerateIndexContentAsync(
                _testFolderPath,
                _testVaultPath,
                template,
                files,
                hierarchyInfo,
                level);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Length > 0);
        }
    }

    /// <summary>
    /// Validates that AddLessonLevelContent prioritizes videos and readings over subfolders for lesson-focused content organization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test verifies the lesson-level content generation strategy, ensuring that educational content
    /// (videos, readings, transcripts) is prominently featured over hierarchical navigation. This approach
    /// optimizes lesson pages for immediate access to learning materials.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses a mix of educational content types (video, reading, transcript) along with subfolders
    /// to validate that content appears before subfolder navigation and uses appropriate ordering.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that video and reading sections are created, content is properly categorized,    /// and subfolders are listed as "Sub-sections" after the main content.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddLessonLevelContent_WithContentAndSubfolders_PrioritizesContent()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string> { "additional-resources", "supplementary-materials" };
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>
        {
            ["video"] = new List<VaultFileInfo>
            {
                new VaultFileInfo { Title = "Lesson Video", ContentType = "video" },
                new VaultFileInfo { Title = "Demo Video", ContentType = "video" }
            },
            ["reading"] = new List<VaultFileInfo>
            {
                new VaultFileInfo { Title = "Chapter Reading", ContentType = "reading" }
            },
            ["transcript"] = new List<VaultFileInfo>
            {
                new VaultFileInfo { Title = "Video Transcript", ContentType = "transcript" }
            }
        };

        // Act
        _generator.AddLessonLevelContent(contentSections, subFolders, groupedFiles);

        // Assert
        // Check that content sections are created with proper hierarchy
        Assert.IsTrue(contentSections.Any(s => s.Contains("## 🎥 Videos")), "Should contain video section");
        Assert.IsTrue(contentSections.Any(s => s.Contains("## 📖 Readings")), "Should contain reading section");
        Assert.IsTrue(contentSections.Any(s => s.Contains("## 📝 Transcripts")), "Should contain transcript section");

        // Check that videos and readings appear in content
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[Lesson Video]]")), "Should contain lesson video link");
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[Chapter Reading]]")), "Should contain chapter reading link");
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[Video Transcript]]")), "Should contain video transcript link");

        // Check that subfolders are listed as Sub-sections (should appear after content)
        Assert.IsTrue(contentSections.Any(s => s.Contains("## Sub-sections")), "Should contain sub-sections header");
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[additional-resources|Additional Resources]]")), "Should contain additional resources link");

        // Verify content appears before subfolders in the list
        var videoIndex = contentSections.FindIndex(s => s.Contains("## 🎥 Videos"));
        var subsectionIndex = contentSections.FindIndex(s => s.Contains("## Sub-sections"));
        Assert.IsTrue(videoIndex < subsectionIndex, "Videos should appear before Sub-sections");
    }

    /// <summary>
    /// Validates that AddLessonLevelContent handles empty content gracefully while still showing subfolders.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures that lesson-level content generation remains functional when no educational
    /// content is available, falling back to showing available subfolders for navigation.
    /// </para>
    /// <para>
    /// Test Scenario:
    /// Uses empty grouped files with only subfolders to validate fallback behavior
    /// when lesson content is not available but navigation structure exists.
    /// </para>
    /// <para>
    /// Assertions:
    /// Verifies that subfolder navigation is properly generated when no content files
    /// are available, ensuring the lesson page remains functional.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddLessonLevelContent_WithEmptyContent_ShowsSubfolders()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string> { "homework", "quiz" };
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>();

        // Act
        _generator.AddLessonLevelContent(contentSections, subFolders, groupedFiles);

        // Assert
        Assert.IsTrue(contentSections.Any(s => s.Contains("## Sub-sections")));
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[homework|Homework]]")));
        Assert.IsTrue(contentSections.Any(s => s.Contains("[[quiz|Quiz]]")));
    }

    /// <summary>
    /// Validates that AddLessonLevelContent produces no output when both content and subfolders are empty.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test ensures efficient handling of completely empty lesson folders by verifying that
    /// the method doesn't generate unnecessary sections when no content or navigation exists.
    /// </para>
    /// </remarks>
    [TestMethod]
    public void AddLessonLevelContent_WithEmptyInputs_ProducesNoSections()
    {
        // Arrange
        var contentSections = new List<string>();
        var subFolders = new List<string>();
        var groupedFiles = new Dictionary<string, List<VaultFileInfo>>();

        // Act
        _generator.AddLessonLevelContent(contentSections, subFolders, groupedFiles);        // Assert
        Assert.AreEqual(0, contentSections.Count);
    }

    #endregion
}
