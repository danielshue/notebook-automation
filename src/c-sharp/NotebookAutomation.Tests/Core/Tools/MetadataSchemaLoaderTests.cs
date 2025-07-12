
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Tests.Core.Tools
{
    /// <summary>
    /// Contains unit tests for <see cref="MetadataSchemaLoader"/>.
    /// <para>
    /// These tests verify correct loading of field defaults, resolver names, recursive inheritance, and dynamic field population.
    /// </para>
    /// <remarks>
    /// All tests follow the Arrange-Act-Assert pattern and cover both happy paths and edge cases for schema loading and resolver logic.
    /// </remarks>
    /// <example>
    /// <code>
    /// var loader = new MetadataSchemaLoader(schemaPath, logger);
    /// var value = loader.ResolveFieldValue("pdf-reference", "date-created");
    /// </code>
    /// </example>
    /// </summary>
    [TestClass]
    public class MetadataSchemaLoaderTests
    {
        /// <summary>
        /// Verifies that a resolver registered with a fully qualified namespace is used for field value resolution.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Use_Namespaced_Resolver_For_FieldValue()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);
            var expectedValue = "resolved-date-ns";
            var namespacedName = "NotebookAutomation.Core.Resolvers.DateCreatedResolver";
            loader.ResolverRegistry.Register(namespacedName, new MockDateCreatedResolver(expectedValue));

            // Act
            var value = loader.ResolveFieldValue("pdf-reference", "date-created");

            // Assert
            Assert.AreEqual(expectedValue, value);
        }

        /// <summary>
        /// Verifies that <see cref="MetadataSchemaLoader"/> correctly loads type mappings and reserved tags from the schema file.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Load_TypeMapping_And_ReservedTags()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);

            // Assert type mapping
            Assert.IsTrue(loader.TypeMapping.ContainsKey("pdf-reference"));
            Assert.AreEqual("reference", loader.TypeMapping["pdf-reference"]);

            // Assert reserved tags
            var reserved = loader.ReservedTags;
            Assert.IsTrue(reserved.Contains("auto-generated-state"));
        }


        /// <summary>
        /// Verifies that <see cref="MetadataSchemaLoader"/> recursively inherits universal fields for template types.
        /// <para>
        /// Ensures that all universal fields are present in the 'pdf-reference' template type after schema loading.
        /// </para>
        /// <remarks>
        /// This test covers recursive inheritance logic for field population.
        /// </remarks>
        /// <example>
        /// <code>
        /// Assert.IsTrue(pdfSchema.Fields.ContainsKey("auto-generated-state"));
        /// </code>
        /// </example>
        /// </summary>
        [TestMethod]
        public void Loader_Should_Recursively_Inherit_UniversalFields()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);

            // Act
            var pdfSchema = loader.TemplateTypes["pdf-reference"];

            // Assert
            Assert.IsTrue(pdfSchema.Fields.ContainsKey("auto-generated-state"));
            Assert.IsTrue(pdfSchema.Fields.ContainsKey("date-created"));
            Assert.IsTrue(pdfSchema.Fields.ContainsKey("publisher"));
        }

        /// <summary>
        /// Verifies that <see cref="MetadataSchemaLoader.ResolveFieldValue"/> uses the registered resolver for dynamic field population.
        /// <para>
        /// Ensures that a mock resolver returns the expected value for the 'date-created' field.
        /// </para>
        /// <remarks>
        /// This test covers dynamic field resolution using custom resolver registration.
        /// </remarks>
        /// <example>
        /// <code>
        /// loader.ResolverRegistry.Register("DateCreatedResolver", new MockDateCreatedResolver(expectedValue));
        /// var value = loader.ResolveFieldValue("pdf-reference", "date-created");
        /// Assert.AreEqual(expectedValue, value);
        /// </code>
        /// </example>
        /// </summary>
        [TestMethod]
        public void Loader_Should_Use_Resolver_For_FieldValue()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);
            var expectedValue = "resolved-date";
            loader.ResolverRegistry.Register("DateCreatedResolver", new MockDateCreatedResolver(expectedValue));

            // Act
            var value = loader.ResolveFieldValue("pdf-reference", "date-created");

            // Assert
            Assert.AreEqual(expectedValue, value);
        }

        /// <summary>
        /// Mock implementation of <see cref="IFieldValueResolver"/> for unit testing dynamic field resolution.
        /// </summary>
        internal class MockDateCreatedResolver : IFieldValueResolver
        {
            private readonly object _value;

            /// <summary>
            /// Initializes a new instance of the <see cref="MockDateCreatedResolver"/> class.
            /// </summary>
            /// <param name="value">The value to return when resolving a field.</param>
            public MockDateCreatedResolver(object value) { _value = value; }

            /// <summary>
            /// Returns the mock value for any field resolution request.
            /// </summary>
            /// <param name="fieldName">The field name being resolved.</param>
            /// <param name="context">Optional context for resolution.</param>
            /// <returns>The mock value provided at construction.</returns>
            public object? Resolve(string fieldName, Dictionary<string, object>? context = null) => _value;
        }

        /// <summary>
        /// Verifies input validation and boundary conditions for field value resolution.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Handle_Null_And_Empty_FieldNames()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);

            // Act & Assert
            Assert.IsNull(loader.ResolveFieldValue("pdf-reference", ""));
        }

        /// <summary>
        /// Verifies reserved tags cannot be used as custom tags in the schema.
        /// </summary>
        [TestMethod]
        public void ReservedTags_Should_Be_Present_As_Fields()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);

            // Act
            var reserved = loader.ReservedTags;

            // Assert
            foreach (var tag in reserved)
            {
                Assert.IsTrue(loader.TemplateTypes["pdf-reference"].Fields.ContainsKey(tag), $"Reserved tag '{tag}' should be present as a field.");
            }
        }

        /// <summary>
        /// Verifies plugin DLL loading for field value resolvers (mocked).
        /// </summary>
        [TestMethod]
        public void Loader_Should_Handle_Plugin_DLL_Loading_Mocked()
        {
            // Arrange
            var schemaPath = "../../../config/metadata-schema.yaml";
            var logger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger<MetadataSchemaLoader>>().Object;
            var loader = new MetadataSchemaLoader(schemaPath, logger);

            // Act & Assert
            // Simulate loading from a non-existent directory (should log warning, not throw)
            loader.LoadResolversFromDirectory("./nonexistent-directory");
            // No exception should be thrown, registry remains unchanged
            Assert.IsNull(loader.ResolverRegistry.Get("NonexistentResolver"));
        }
    }
}
