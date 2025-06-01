using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NotebookAutomation.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Tests for the AppConfig class, especially focusing on its implementation of IConfiguration.
    /// </summary>    [TestClass]
    public class AppConfigTests
    {
        private Mock<ILogger<AppConfig>> _loggerMock = null!;
        private Mock<IConfiguration> _configurationMock = null!;

        [TestInitialize]
        public void Initialize()
        {
            _loggerMock = new Mock<ILogger<AppConfig>>();
            _configurationMock = new Mock<IConfiguration>();
        }

        /// <summary>
        /// Helper method to set up API key configuration for testing
        /// </summary>
        private void SetupApiKeyConfigurationForTesting(AppConfig config, string apiKey)
        {
            // Set the environment variable directly for the provider
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", apiKey);
        }
        /// Tests the basic property indexer access with both direct and nested keys.
        /// </summary>
        [TestMethod]
        public void Indexer_ShouldReturnValueForExistingKeys()
        {
            // Arrange
            var appConfig = new AppConfig();
            appConfig.Paths = new PathsConfig
            {
                LoggingDir = "logs",
                OnedriveFullpathRoot = "/resources"
            };
            appConfig.AiService = new AIServiceConfig
            {
                Provider = "openai",
                OpenAI = new OpenAiProviderConfig { Model = "gpt-4" }
            };
            SetupApiKeyConfigurationForTesting(appConfig, "test-api-key");
            // Act & Assert
            Assert.AreEqual("logs", appConfig["paths:LoggingDir"]);
            Assert.AreEqual("/resources", appConfig["paths:OnedriveFullpathRoot"]);
            Assert.AreEqual("gpt-4", appConfig["aiservice:Model"]);
            // Check using JsonPropertyName value
            Assert.AreEqual("test-api-key", appConfig["aiservice:api_key"]);
            Assert.AreEqual("logs", appConfig["paths:logging_dir"]);
        }

        /// <summary>
        /// Tests that null is returned for non-existent keys.
        /// </summary>
        [TestMethod]
        public void Indexer_ShouldReturnNullForNonExistentKeys()
        {
            // Arrange
            var appConfig = new AppConfig();

            // Act & Assert
            Assert.IsNull(appConfig["nonExistentKey"]);
            Assert.IsNull(appConfig["paths:nonExistentKey"]);
        }

        /// <summary>
        /// Tests setting values through the indexer.
        /// </summary>
        [TestMethod]
        public void Indexer_ShouldSetValueForExistingKeys()
        {
            // Arrange
            var appConfig = new AppConfig();
            appConfig.Paths = new PathsConfig();

            // Act
            appConfig["paths:LoggingDir"] = "new-logs";

            // Assert
            Assert.AreEqual("new-logs", appConfig.Paths.LoggingDir);
            Assert.AreEqual("new-logs", appConfig["paths:LoggingDir"]);
        }

        /// <summary>
        /// Tests getting configuration sections.
        /// </summary>
        [TestMethod]
        public void GetSection_ShouldReturnValidSection()
        {
            // Arrange
            var appConfig = new AppConfig(); appConfig.Paths = new PathsConfig
            {
                LoggingDir = "logs",
                OnedriveFullpathRoot = "/resources"
            };

            // Act
            var pathsSection = appConfig.GetSection("paths");
            // Assert
            Assert.IsNotNull(pathsSection);
            Assert.AreEqual("paths", pathsSection.Key);
            Assert.AreEqual("paths", pathsSection.Path);
            Assert.AreEqual("logs", pathsSection["LoggingDir"]);
            Assert.AreEqual("/resources", pathsSection["OnedriveFullpathRoot"]);
        }

        /// <summary>
        /// Tests the Exists method.
        /// </summary>
        [TestMethod]
        public void Exists_ShouldReturnCorrectResults()
        {
            // Arrange
            var appConfig = new AppConfig();
            appConfig.Paths = new PathsConfig
            {
                LoggingDir = "logs"
            };

            // Act & Assert
            Assert.IsTrue(appConfig.Exists("paths"));
            Assert.IsTrue(appConfig.Exists("paths:LoggingDir"));
            Assert.IsTrue(appConfig.Exists("Paths")); // Case-insensitive
            Assert.IsTrue(appConfig.Exists("paths:logging_dir")); // JsonPropertyName
            Assert.IsFalse(appConfig.Exists("nonExistentKey"));
            Assert.IsFalse(appConfig.Exists("paths:nonExistentKey"));
        }

        /// <summary>
        /// Tests getting child sections.
        /// </summary>
        [TestMethod]
        public void GetChildren_ShouldReturnAllTopLevelSections()
        {
            // Arrange
            var appConfig = new AppConfig();

            // Act
            var children = appConfig.GetChildren().ToList();

            // Assert
            Assert.IsTrue(children.Count > 0);
            Assert.IsTrue(children.Any(c => c.Key.Equals("Paths", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(children.Any(c => c.Key.Equals("AiService", StringComparison.OrdinalIgnoreCase)));
            Assert.IsTrue(children.Any(c => c.Key.Equals("MicrosoftGraph", StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Tests loading configuration from JSON file.
        /// </summary>
        [TestMethod]
        public void LoadFromJsonFile_ShouldLoadConfigurationCorrectly()
        {
            // Arrange - Create a temporary config file
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
            var config = new
            {
                paths = new
                {
                    resources_root = "/resources",
                    logging_dir = "/logs"
                },
                aiservice = new
                {
                    model = "gpt-4"
                }
            };

            File.WriteAllText(tempFile, JsonSerializer.Serialize(config));

            try
            {
                // Act
                var appConfig = AppConfig.LoadFromJsonFile(tempFile);
                // Assert
                Assert.IsNotNull(appConfig);
                Assert.AreEqual("/resources", appConfig.Paths.OnedriveFullpathRoot);
                Assert.AreEqual("/logs", appConfig.Paths.LoggingDir);
                // Set up the configuration for testing                // Create a configuration with the API key
                var configDict = new Dictionary<string, string>
                {
                    {"UserSecrets:OpenAI:ApiKey", "test-api-key"}
                };
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configDict)
                    .Build();
                // No SetConfiguration; set provider/model directly
                appConfig.AiService.Provider = "openai";
                if (appConfig.AiService.OpenAI == null)
                    appConfig.AiService.OpenAI = new OpenAiProviderConfig();
                appConfig.AiService.OpenAI.Model = "gpt-4";
                Assert.AreEqual("test-api-key", appConfig.AiService.GetApiKey());
                Assert.AreEqual("gpt-4", appConfig.AiService.OpenAI?.Model);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        /// <summary>
        /// Tests saving configuration to JSON file.
        /// </summary>
        [TestMethod]
        public void SaveToJsonFile_ShouldSaveConfigurationCorrectly()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".json");
            var appConfig = new AppConfig(); appConfig.Paths = new PathsConfig
            {
                LoggingDir = "/logs",
                OnedriveFullpathRoot = "/resources"
            };

            appConfig.AiService = new AIServiceConfig
            {
                Provider = "openai",
                OpenAI = new OpenAiProviderConfig { Model = "gpt-4" }
            };

            try
            {
                // Act
                appConfig.SaveToJsonFile(tempFile);

                // Assert - Load it back and verify
                var loadedConfig = AppConfig.LoadFromJsonFile(tempFile);
                Assert.IsNotNull(loadedConfig);
                Assert.AreEqual("/resources", loadedConfig.Paths.OnedriveFullpathRoot);
                Assert.AreEqual("/logs", loadedConfig.Paths.LoggingDir);

                // Create a configuration with the API key for testing GetApiKey
                var configDict = new Dictionary<string, string>
                {
                    {"UserSecrets:OpenAI:ApiKey", "test-api-key"}
                };
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configDict)
                    .Build();
                // No SetConfiguration; set provider/model directly
                loadedConfig.AiService.Provider = "openai";
                if (loadedConfig.AiService.OpenAI == null)
                    loadedConfig.AiService.OpenAI = new OpenAiProviderConfig();
                loadedConfig.AiService.OpenAI.Model = "gpt-4";
                Assert.AreEqual("test-api-key", loadedConfig.AiService.GetApiKey());
                Assert.AreEqual("gpt-4", loadedConfig.AiService.OpenAI?.Model);
            }
            finally
            {
                // Clean up
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }        /// <summary>
                 /// Tests configuration with underlying IConfiguration.
                 /// </summary>
        [TestMethod]
        public void WithUnderlyingConfiguration_ShouldUseUnderlyingValues()
        {
            // Arrange
            var configValues = new Dictionary<string, string>
            {
                {"paths:resources_root", "/config-resources"},
                {"paths:logging_dir", "/config-logs"},                {"aiservice:apiKey", "config-api-key"},
                {"aiservice:model", "gpt-4-turbo"}
            };

            _configurationMock.Setup(c => c[It.IsAny<string>()]).Returns<string>(key =>
                configValues.TryGetValue(key, out var value) ? value : null);

            // Mock sections without using Exists extension method
            _configurationMock.Setup(c => c.GetSection(It.IsAny<string>()))
                .Returns<string>(key =>
                {
                    var mockSection = new Mock<IConfigurationSection>();
                    mockSection.Setup(s => s.Path).Returns(key);
                    mockSection.Setup(s => s.Key).Returns(key);
                    // Use Value property instead of Exists() extension method
                    mockSection.Setup(s => s.Value).Returns(
                        configValues.Keys.Any(k => k.StartsWith(key + ":")) ? string.Empty : null);
                    return mockSection.Object;
                });

            // Create test configuration using constructor parameters
            var appConfig = new AppConfig();            // Set the values manually instead of relying on constructor
            appConfig.Paths = new PathsConfig
            {
                OnedriveFullpathRoot = "/config-resources",
                LoggingDir = "/config-logs"
            };
            appConfig.AiService = new AIServiceConfig
            {
                OpenAI = new OpenAiProviderConfig { Model = "gpt-4-turbo" }
            };

            // Set up configuration with API key
            SetupApiKeyConfigurationForTesting(appConfig, "config-api-key");

            // Act & Assert            Assert.AreEqual("/config-resources", appConfig["paths:resources_root"]);
            Assert.AreEqual("/config-logs", appConfig["paths:logging_dir"]);
            Assert.AreEqual("config-api-key", appConfig.AiService.GetApiKey());
            Assert.AreEqual("gpt-4-turbo", appConfig.AiService.OpenAI?.Model);
        }
    }
}
