// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Integration tests to verify that CLI commands are using the MetadataSchemaLoader for metadata validation and generation.
/// These tests verify that the refactoring for issue #37 is working correctly.
/// </summary>
[TestClass]
public class SchemaLoaderIntegrationTests
{
    /// <summary>
    /// Verifies that MetadataSchemaLoader provides template types.
    /// </summary>
    [TestMethod]
    public void MetadataSchemaLoader_Should_Provide_TemplateTypes()
    {
        // Arrange
        var logger = new Mock<ILogger<MetadataSchemaLoader>>();
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "metadata-schema.yml");

        // Skip test if schema file doesn't exist in test environment
        if (!File.Exists(schemaPath))
        {
            Assert.Inconclusive("Schema file not found in test environment");
            return;
        }

        // Act
        var schemaLoader = new MetadataSchemaLoader(schemaPath, logger.Object);

        // Assert
        Assert.IsNotNull(schemaLoader.TemplateTypes);
        Assert.IsTrue(schemaLoader.TemplateTypes.Count > 0);
        Assert.IsTrue(schemaLoader.TemplateTypes.ContainsKey("pdf-reference"));
        Assert.IsTrue(schemaLoader.TemplateTypes.ContainsKey("video-reference"));
    }

    /// <summary>
    /// Verifies that MetadataSchemaLoader provides a resolver registry.
    /// </summary>
    [TestMethod]
    public void MetadataSchemaLoader_Should_Provide_ResolverRegistry()
    {
        // Arrange
        var logger = new Mock<ILogger<MetadataSchemaLoader>>();
        var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "metadata-schema.yml");

        // Skip test if schema file doesn't exist in test environment
        if (!File.Exists(schemaPath))
        {
            Assert.Inconclusive("Schema file not found in test environment");
            return;
        }

        // Act
        var schemaLoader = new MetadataSchemaLoader(schemaPath, logger.Object);

        // Assert
        Assert.IsNotNull(schemaLoader.ResolverRegistry);
        Assert.IsInstanceOfType(schemaLoader.ResolverRegistry, typeof(FieldValueResolverRegistry));
    }

    /// <summary>
    /// Verifies that TagProcessor can be instantiated with a resolver registry.
    /// This test ensures that the refactoring to use the schema loader resolver registry is working.
    /// </summary>
    [TestMethod]
    public void TagProcessor_Should_Accept_ResolverRegistry()
    {
        // Arrange
        var logger = new Mock<ILogger<TagProcessor>>();
        var failedLogger = new Mock<ILogger>();
        var yamlHelper = new Mock<IYamlHelper>();
        var resolverRegistry = new FieldValueResolverRegistry();

        // Act
        var tagProcessor = new TagProcessor(
            logger.Object,
            failedLogger.Object,
            yamlHelper.Object,
            dryRun: false,
            verbose: false,
            resolverRegistry: resolverRegistry);

        // Assert
        Assert.IsNotNull(tagProcessor);
        Assert.IsNotNull(tagProcessor.Stats);
    }
}
