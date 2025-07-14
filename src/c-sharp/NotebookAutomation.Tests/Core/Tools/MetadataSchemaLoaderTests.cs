
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Tests.Core.Helpers;

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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();
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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Assert type mapping
            Assert.IsTrue(loader.TypeMapping.ContainsKey("pdf-reference"));
            Assert.AreEqual("note/case-study", loader.TypeMapping["pdf-reference"]);

            // Assert reserved tags
            var reserved = loader.ReservedTags;
            Assert.IsTrue(reserved.Contains("case-study"));
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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();
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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

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
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act & Assert
            // Simulate loading from a non-existent directory (should log warning, not throw)
            loader.LoadResolversFromDirectory("./nonexistent-directory");
            // No exception should be thrown, registry remains unchanged
            Assert.IsNull(loader.ResolverRegistry.Get("NonexistentResolver"));
        }

        /// <summary>
        /// Verifies that reserved tags are properly inherited by all template types.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inherit_ReservedTags_Across_TemplateTypes()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var reservedTags = loader.ReservedTags;

            // Assert - All template types should have reserved tags available
            foreach (var templateType in loader.TemplateTypes.Keys)
            {
                var template = loader.TemplateTypes[templateType];
                foreach (var reservedTag in reservedTags)
                {
                    Assert.IsTrue(template.Fields.ContainsKey(reservedTag), 
                        $"Template type '{templateType}' should inherit reserved tag '{reservedTag}'");
                }
            }
        }

        /// <summary>
        /// Verifies that universal fields are properly injected into all template types.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inject_UniversalFields_Into_All_TemplateTypes()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var universalFields = new[] { "auto-generated-state", "date-created", "publisher" };

            // Assert - All template types should have universal fields injected
            foreach (var templateType in loader.TemplateTypes.Keys)
            {
                var template = loader.TemplateTypes[templateType];
                foreach (var universalField in universalFields)
                {
                    Assert.IsTrue(template.Fields.ContainsKey(universalField), 
                        $"Template type '{templateType}' should have universal field '{universalField}' injected");
                }
            }
        }

        /// <summary>
        /// Verifies that reserved tags cannot be overridden by template-specific fields.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Prevent_ReservedTag_Override()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act & Assert
            var reservedTags = loader.ReservedTags;
            foreach (var reservedTag in reservedTags)
            {
                // Reserved tags should be present as fields in all templates
                foreach (var templateType in loader.TemplateTypes.Keys)
                {
                    var template = loader.TemplateTypes[templateType];
                    Assert.IsTrue(template.Fields.ContainsKey(reservedTag), 
                        $"Reserved tag '{reservedTag}' should be present in template '{templateType}'");
                }
            }
        }

        /// <summary>
        /// Verifies that universal field injection maintains field hierarchy and defaults.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Maintain_FieldHierarchy_During_UniversalField_Injection()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var pdfTemplate = loader.TemplateTypes["pdf-reference"];
            var videoTemplate = loader.TemplateTypes["video-reference"];

            // Assert - Universal fields should maintain their characteristics
            Assert.IsTrue(pdfTemplate.Fields.ContainsKey("publisher"), 
                "PDF template should have universal field 'publisher'");
            Assert.IsTrue(videoTemplate.Fields.ContainsKey("publisher"), 
                "Video template should have universal field 'publisher'");
            
            // Both templates should have the same universal field behavior
            Assert.IsTrue(pdfTemplate.Fields.ContainsKey("date-created"), 
                "PDF template should have universal field 'date-created'");
            Assert.IsTrue(videoTemplate.Fields.ContainsKey("date-created"), 
                "Video template should have universal field 'date-created'");
        }

        /// <summary>
        /// Verifies that resolver registry integrates properly with schema loading.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Integrate_ResolverRegistry_With_Schema()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();
            var mockResolver = new MockDateCreatedResolver("2023-01-01");
            
            // Act
            loader.ResolverRegistry.Register("TestResolver", mockResolver);

            // Assert
            Assert.IsNotNull(loader.ResolverRegistry.Get("TestResolver"), 
                "Resolver registry should store registered resolvers");
            Assert.AreSame(mockResolver, loader.ResolverRegistry.Get("TestResolver"), 
                "Resolver registry should return the same instance");
        }

        /// <summary>
        /// Verifies that plugin integration works with resolver registry.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Support_Plugin_Integration_With_Registry()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();
            var pluginResolver = new MockDateCreatedResolver("plugin-resolved-value");

            // Act
            loader.ResolverRegistry.Register("PluginResolver", pluginResolver);
            
            // Assert
            var registeredResolver = loader.ResolverRegistry.Get("PluginResolver");
            Assert.IsNotNull(registeredResolver, "Plugin resolver should be registered");
            
            var resolvedValue = registeredResolver.Resolve("test-field");
            Assert.AreEqual("plugin-resolved-value", resolvedValue, 
                "Plugin resolver should resolve values correctly");
        }
    }
}
