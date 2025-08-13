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
