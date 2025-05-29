using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for config update-key with aiservice.* keys.
    /// </summary>
    [TestClass]
    public class ConfigCommands_AiServiceTests
    {
        private AppConfig GetDefaultConfig()
        {
            return new AppConfig
            {
                AiService = new AIServiceConfig
                {
                    Provider = "openai",
                    OpenAI = new OpenAiProviderConfig { Model = "gpt-4", Endpoint = "https://api.openai.com/v1" },
                    Azure = new AzureProviderConfig { Model = "", Endpoint = "", Deployment = "" },
                    Foundry = new FoundryProviderConfig { Model = "", Endpoint = "" }
                }
            };
        }

        [TestMethod]
        public void Update_AiService_Provider()
        {
            var config = GetDefaultConfig();
            bool result = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.provider", "azure" }) as bool? ?? false;
            Assert.IsTrue(result);
            Assert.AreEqual("azure", config.AiService.Provider);
        }

        [TestMethod]
        public void Update_AiService_OpenAI_Model()
        {
            var config = GetDefaultConfig();
            bool result = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.openai.model", "gpt-4o" }) as bool? ?? false;
            Assert.IsTrue(result);
            Assert.AreEqual("gpt-4o", config.AiService.OpenAI.Model);
        }

        [TestMethod]
        public void Update_AiService_OpenAI_Endpoint()
        {
            var config = GetDefaultConfig();
            bool result = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.openai.endpoint", "https://custom.openai.com" }) as bool? ?? false;
            Assert.IsTrue(result);
            Assert.AreEqual("https://custom.openai.com", config.AiService.OpenAI.Endpoint);
        }

        [TestMethod]
        public void Update_AiService_Azure_Model()
        {
            var config = GetDefaultConfig();
            bool result = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.azure.model", "azure-gpt" }) as bool? ?? false;
            Assert.IsTrue(result);
            Assert.AreEqual("azure-gpt", config.AiService.Azure.Model);
        }

        [TestMethod]
        public void Update_AiService_Azure_Endpoint_And_Deployment()
        {
            var config = GetDefaultConfig();
            bool endpointResult = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.azure.endpoint", "https://azure.openai.com" }) as bool? ?? false;
            bool deploymentResult = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.azure.deployment", "my-deployment" }) as bool? ?? false;
            Assert.IsTrue(endpointResult);
            Assert.IsTrue(deploymentResult);
            Assert.AreEqual("https://azure.openai.com", config.AiService.Azure.Endpoint);
            Assert.AreEqual("my-deployment", config.AiService.Azure.Deployment);
        }

        [TestMethod]
        public void Update_AiService_Foundry_Model_And_Endpoint()
        {
            var config = GetDefaultConfig();
            bool modelResult = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.foundry.model", "foundry-llm" }) as bool? ?? false;
            bool endpointResult = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                !.Invoke(null, new object[] { config, "aiservice.foundry.endpoint", "https://foundry.ai" }) as bool? ?? false;
            Assert.IsTrue(modelResult);
            Assert.IsTrue(endpointResult);
            Assert.AreEqual("foundry-llm", config.AiService.Foundry.Model);
            Assert.AreEqual("https://foundry.ai", config.AiService.Foundry.Endpoint);
        }
    }
}
