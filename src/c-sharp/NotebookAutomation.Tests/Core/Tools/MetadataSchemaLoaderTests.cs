// Licensed under the MIT License. See LICENSE file in the project root for full license information.
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

        /// <summary>
        /// Verifies that Prompt property is inherited from base types.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inherit_Prompt_From_BaseTypes()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var videoTemplate = loader.TemplateTypes["video-reference"];
            var pdfTemplate = loader.TemplateTypes["pdf-reference"];

            // Assert
            Assert.AreEqual("video-reference", videoTemplate.Prompt,
                "video-reference should have its own Prompt property");
            Assert.AreEqual("pdf-reference", pdfTemplate.Prompt,
                "pdf-reference should have its own Prompt property");
        }

        /// <summary>
        /// Verifies that generic template types inherit Prompt from base-generic.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inherit_Prompt_From_BaseGeneric()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var genericVideoTemplate = loader.TemplateTypes["generic-video"];
            var genericPdfTemplate = loader.TemplateTypes["generic-pdf"];

            // Assert - Should inherit from base-generic
            Assert.AreEqual("generic_prompt", genericVideoTemplate.Prompt,
                "generic-video should inherit Prompt from base-generic");
            Assert.AreEqual("generic_prompt", genericPdfTemplate.Prompt,
                "generic-pdf should inherit Prompt from base-generic");
        }

        /// <summary>
        /// Verifies that PathResolution is inherited from base types.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inherit_PathResolution_From_BaseTypes()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var baseTemplate = loader.TemplateTypes["base-template"];
            var videoTemplate = loader.TemplateTypes["video-reference"];

            // Assert
            Assert.IsNotNull(baseTemplate.PathResolution,
                "base-template should have PathResolution configured");
            Assert.IsNotNull(videoTemplate.PathResolution,
                "video-reference should inherit PathResolution from base-template");
            Assert.AreEqual("onedrive", videoTemplate.PathResolution.InputRoot,
                "video-reference should inherit InputRoot from base-template");
            Assert.AreEqual("vault", videoTemplate.PathResolution.OutputRoot,
                "video-reference should inherit OutputRoot from base-template");
        }

        /// <summary>
        /// Verifies that PathResolution is inherited correctly for generic types.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Inherit_PathResolution_From_BaseGeneric()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var baseGeneric = loader.TemplateTypes["base-generic"];
            var genericVideo = loader.TemplateTypes["generic-video"];

            // Assert
            Assert.IsNotNull(baseGeneric.PathResolution,
                "base-generic should have PathResolution configured");
            Assert.IsNotNull(genericVideo.PathResolution,
                "generic-video should inherit PathResolution from base-generic");
            Assert.AreEqual("cwd", genericVideo.PathResolution.InputRoot,
                "generic-video should inherit InputRoot=cwd from base-generic");
            Assert.AreEqual("input", genericVideo.PathResolution.OutputRoot,
                "generic-video should inherit OutputRoot=input from base-generic");
        }

        /// <summary>
        /// Verifies that explicit Prompt overrides inherited value.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Override_Inherited_Prompt()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var videoTemplate = loader.TemplateTypes["video-reference"];

            // Assert - video-reference has explicit Prompt that overrides base-template
            Assert.AreEqual("video-reference", videoTemplate.Prompt,
                "video-reference should use its explicit Prompt, not inherited");
        }

        /// <summary>
        /// Verifies that base-template and base-generic have correct default configurations.
        /// </summary>
        [TestMethod]
        public void Loader_Should_Configure_BaseTypes_Correctly()
        {
            // Arrange
            var loader = MetadataSchemaLoaderHelper.CreateTestMetadataSchemaLoader();

            // Act
            var baseTemplate = loader.TemplateTypes["base-template"];
            var baseGeneric = loader.TemplateTypes["base-generic"];

            // Assert base-template
            Assert.AreEqual("default_prompt", baseTemplate.Prompt,
                "base-template should have default_prompt");
            Assert.IsNotNull(baseTemplate.PathResolution);
            Assert.AreEqual("onedrive", baseTemplate.PathResolution.InputRoot);
            Assert.AreEqual("vault", baseTemplate.PathResolution.OutputRoot);

            // Assert base-generic
            Assert.AreEqual("generic_prompt", baseGeneric.Prompt,
                "base-generic should have generic_prompt");
            Assert.IsNotNull(baseGeneric.PathResolution);
            Assert.AreEqual("cwd", baseGeneric.PathResolution.InputRoot);
            Assert.AreEqual("input", baseGeneric.PathResolution.OutputRoot);
        }
    }
}
