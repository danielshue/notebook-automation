using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Cli.Commands;

namespace NotebookAutomation.Cli.Tests
{
    /// <summary>
    /// Unit tests for ConfigValidation static helpers.
    /// </summary>
    [TestClass]
    public class ConfigValidationTests
    {
        [TestMethod]
        public void RequireAllPaths_ReturnsTrue_WhenAllPathsPresent()
        {
            var config = new AppConfig
            {
                Paths = new PathsConfig
                {
                    OnedriveFullpathRoot = "C:/resources",
                    NotebookVaultFullpathRoot = "C:/vault",
                    MetadataFile = "C:/meta/metadata.json",
                    OnedriveResourcesBasepath = "C:/onedrive",
                    LoggingDir = "C:/logs"
                }
            };
            var result = ConfigValidation.RequireAllPaths(config, out var missing);
            Assert.IsTrue(result);
            Assert.AreEqual(0, missing.Count);
        }

        [TestMethod]
        public void RequireAllPaths_ReturnsFalse_AndListsMissing_WhenSomePathsMissing()
        {
            var config = new AppConfig
            {
                Paths = new PathsConfig
                {
                    OnedriveFullpathRoot = "",
                    NotebookVaultFullpathRoot = null,
                    MetadataFile = "meta.json",
                    OnedriveResourcesBasepath = "basepath",
                    LoggingDir = null
                }
            };
            var result = ConfigValidation.RequireAllPaths(config, out var missing); Assert.IsFalse(result); CollectionAssert.Contains(missing, "paths.onedrive_fullpath_root");
            CollectionAssert.Contains(missing, "paths.notebook_vault_fullpath_root");
            CollectionAssert.Contains(missing, "paths.logging_dir");
            CollectionAssert.DoesNotContain(missing, "paths.metadata_file");
            CollectionAssert.DoesNotContain(missing, "paths.onedrive_resources_basepath");
        }

        [TestMethod]
        public void RequireMicrosoftGraph_ReturnsFalse_WhenMissingValues()
        {
            var config = new AppConfig
            {
                MicrosoftGraph = new MicrosoftGraphConfig
                {
                    ClientId = null,
                    ApiEndpoint = "",
                    Authority = null,
                    Scopes = new List<string>()
                }
            };
            var result = ConfigValidation.RequireMicrosoftGraph(config);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RequireMicrosoftGraph_ReturnsTrue_WhenAllValuesPresent()
        {
            var config = new AppConfig
            {
                MicrosoftGraph = new MicrosoftGraphConfig
                {
                    ClientId = "id",
                    ApiEndpoint = "endpoint",
                    Authority = "authority",
                    Scopes = new List<string> { "scope1" }
                }
            };
            var result = ConfigValidation.RequireMicrosoftGraph(config);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RequireOpenAi_ReturnsFalse_WhenMissingValues()
        {
            var config = new AppConfig
            {
                AiService = new AIServiceConfig
                {
                    Model = null
                }
            };

            // No need to set up API key configuration as we want it to be null
            var result = ConfigValidation.RequireOpenAi(config);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RequireOpenAi_ReturnsTrue_WhenAllValuesPresent()
        {
            var config = new AppConfig
            {
                AiService = new AIServiceConfig
                {
                    Model = "gpt-4"
                }
            };

            // Set up API key for testing
            var configDict = new Dictionary<string, string>
            {
                {"UserSecrets:OpenAI:ApiKey", "key"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();
            config.AiService.SetConfiguration(configuration);
            var result = ConfigValidation.RequireOpenAi(config);
            Assert.IsTrue(result);
        }
    }
}
