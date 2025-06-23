// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Unit tests for config update with aiservice.* keys.
/// </summary>
[TestClass]
public class ConfigCommands_AiServiceTests
{
    private static AppConfig GetDefaultConfig()
    {
        return new AppConfig
        {
            AiService = new AIServiceConfig
            {
                Provider = "openai",
                OpenAI = new OpenAiProviderConfig { Model = "gpt-4", Endpoint = "https://api.openai.com/v1" },
                Azure = new AzureProviderConfig { Model = string.Empty, Endpoint = string.Empty, Deployment = string.Empty },
                Foundry = new FoundryProviderConfig { Model = string.Empty, Endpoint = string.Empty },
            },
        };
    }

    /// <summary>
    /// Tests updating the AI service provider configuration key
    /// to verify that the provider can be changed from openai to azure.
    /// </summary>
    [TestMethod]
    public void Update_AiService_Provider()
    {
        var config = GetDefaultConfig();
        bool result = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            !.Invoke(null, [config, "aiservice.provider", "azure"]) as bool? ?? false;
        Assert.IsTrue(result);
        Assert.AreEqual("azure", config.AiService.Provider);
    }

    /// <summary>
    /// Tests updating the OpenAI model configuration key
    /// to verify that the OpenAI model can be changed from gpt-4 to gpt-4o.
    /// </summary>
    [TestMethod]
    public void Update_AiService_OpenAI_Model()
    {
        var config = GetDefaultConfig();
        bool result = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.openai.model", "gpt-4o"]) as bool? ?? false; Assert.IsTrue(result);
        Assert.AreEqual("gpt-4o", config.AiService!.OpenAI!.Model);
    }

    /// <summary>
    /// Tests updating the OpenAI endpoint configuration key
    /// to verify that the OpenAI endpoint can be changed to a custom URL.
    /// </summary>
    [TestMethod]
    public void Update_AiService_OpenAI_Endpoint()
    {
        var config = GetDefaultConfig();
        bool result = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.openai.endpoint", "https://custom.openai.com"]) as bool? ?? false; Assert.IsTrue(result);
        Assert.AreEqual("https://custom.openai.com", config.AiService!.OpenAI!.Endpoint);
    }

    /// <summary>
    /// Tests updating the Azure OpenAI model configuration key
    /// to verify that the Azure model can be set correctly.
    /// </summary>
    [TestMethod]
    public void Update_AiService_Azure_Model()
    {
        var config = GetDefaultConfig();
        bool result = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.azure.model", "azure-gpt"]) as bool? ?? false; Assert.IsTrue(result);
        Assert.AreEqual("azure-gpt", config.AiService!.Azure!.Model);
    }

    /// <summary>
    /// Tests updating the Azure OpenAI endpoint and deployment configuration keys
    /// to verify that both Azure endpoint and deployment can be set correctly.
    /// </summary>
    [TestMethod]
    public void Update_AiService_Azure_Endpoint_And_Deployment()
    {
        var config = GetDefaultConfig();
        bool endpointResult = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.azure.endpoint", "https://azure.openai.com"]) as bool? ?? false;
        bool deploymentResult = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.azure.deployment", "my-deployment"]) as bool? ?? false; Assert.IsTrue(endpointResult);
        Assert.IsTrue(deploymentResult);
        Assert.AreEqual("https://azure.openai.com", config.AiService!.Azure!.Endpoint);
        Assert.AreEqual("my-deployment", config.AiService!.Azure!.Deployment);
    }

    /// <summary>
    /// Tests updating the Foundry model and endpoint configuration keys
    /// to verify that both Foundry model and endpoint can be set correctly.
    /// </summary>
    [TestMethod]
    public void Update_AiService_Foundry_Model_And_Endpoint()
    {
        var config = GetDefaultConfig();
        bool modelResult = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.foundry.model", "foundry-llm"]) as bool? ?? false;
        bool endpointResult = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.Invoke(null, [config, "aiservice.foundry.endpoint", "https://foundry.ai"]) as bool? ?? false; Assert.IsTrue(modelResult);
        Assert.IsTrue(endpointResult);
        Assert.AreEqual("foundry-llm", config.AiService!.Foundry!.Model);
        Assert.AreEqual("https://foundry.ai", config.AiService!.Foundry!.Endpoint);
    }
}
