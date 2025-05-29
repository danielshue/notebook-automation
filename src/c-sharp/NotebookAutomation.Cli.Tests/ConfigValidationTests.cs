using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
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
        public void RequireOpenAi_ReturnsFalse_WhenApiKeyMissing()
        {
            // Arrange
            var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            var config = new AppConfig
            {
                AiService = new AIServiceConfig
                {
                    Provider = "openai"
                }
            };

            // Act
            var result = ConfigValidation.RequireOpenAi(config);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }

        [TestMethod]
        public void RequireOpenAi_ReturnsTrue_WhenApiKeyPresent()
        {
            // Arrange
            var original = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", "test-key");
            var config = new AppConfig
            {
                AiService = new AIServiceConfig
                {
                    Provider = "openai"
                }
            };

            // Act
            var result = ConfigValidation.RequireOpenAi(config);

            // Assert
            Assert.IsTrue(result);

            // Cleanup
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", original);
        }
        [TestMethod]
        public void RequireAllPaths_ReturnsFalse_WhenPathsIsNull()
        {
            var config = new AppConfig { Paths = null! };
            var result = ConfigValidation.RequireAllPaths(config, out var missing);
            Assert.IsFalse(result);
            Assert.IsTrue(missing.Count > 0);
        }

        [TestMethod]
        public void RequireAllPaths_ReturnsFalse_WhenAllFieldsMissingOrWhitespace()
        {
            var config = new AppConfig
            {
                Paths = new PathsConfig
                {
                    OnedriveFullpathRoot = " ",
                    NotebookVaultFullpathRoot = null,
                    MetadataFile = "",
                    OnedriveResourcesBasepath = null,
                    LoggingDir = ""
                }
            };
            var result = ConfigValidation.RequireAllPaths(config, out var missing);
            Assert.IsFalse(result);
            Assert.AreEqual(5, missing.Count);
        }

        [TestMethod]
        public void RequireMicrosoftGraph_ReturnsFalse_WhenMicrosoftGraphIsNull()
        {
            var config = new AppConfig { MicrosoftGraph = null! };
            var result = ConfigValidation.RequireMicrosoftGraph(config);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void RequireMicrosoftGraph_ReturnsFalse_WhenScopesIsNullOrEmpty()
        {
            var config1 = new AppConfig { MicrosoftGraph = new MicrosoftGraphConfig { ClientId = "id", ApiEndpoint = "ep", Authority = "auth", Scopes = null! } };
            var config2 = new AppConfig { MicrosoftGraph = new MicrosoftGraphConfig { ClientId = "id", ApiEndpoint = "ep", Authority = "auth", Scopes = new List<string>() } };
            Assert.IsFalse(ConfigValidation.RequireMicrosoftGraph(config1));
            Assert.IsFalse(ConfigValidation.RequireMicrosoftGraph(config2));
        }

        [TestMethod]
        public void RequireOpenAi_ReturnsFalse_WhenAiServiceIsNull()
        {
            var config = new AppConfig { AiService = null! };
            var result = ConfigValidation.RequireOpenAi(config);
            Assert.IsFalse(result);
        }

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
            var result = ConfigValidation.RequireAllPaths(config, out var missing);
            Assert.IsFalse(result);
            CollectionAssert.Contains(missing, "paths.onedrive_fullpath_root");
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
    }
}
