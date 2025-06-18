// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Configuration;

[TestClass]
public class UserSecretsTests
{
    [TestMethod]
    public void ConfigurationSetup_ShouldLoadUserSecrets()
    {
        // Arrange
        string tempSecretFile = Path.GetTempFileName();
        try
        {
            // Create a temporary user secrets file with test data
            string secretsJson = JsonSerializer.Serialize(new
            {
                UserSecrets = new
                {
                    OpenAI = new
                    {
                        ApiKey = "test-openai-key",
                    },
                    Microsoft = new
                    {
                        ClientId = "test-client-id",
                        TenantId = "test-tenant-id",
                    },
                },
            });
            File.WriteAllText(tempSecretFile, secretsJson);

            // Create test configuration with custom secret file
            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .AddJsonFile(tempSecretFile); // Simulate user secrets

            IConfigurationRoot config = configBuilder.Build();

            // Act
            UserSecretsHelper helper = new(config);

            // Assert
            Assert.AreEqual("test-openai-key", helper.GetOpenAIApiKey());
            Assert.AreEqual("test-client-id", helper.GetMicrosoftGraphClientId());
            Assert.AreEqual("test-tenant-id", helper.GetMicrosoftGraphTenantId());
            Assert.IsTrue(helper.HasSecret("OpenAI:ApiKey"));
            Assert.IsFalse(helper.HasSecret("NonExistentKey"));
        }
        finally
        {
            // Clean up
            if (File.Exists(tempSecretFile))
            {
                File.Delete(tempSecretFile);
            }
        }
    }

    [TestMethod]
    public void UserSecretsHelper_ShouldHandleNullConfiguration()
    {
        // Create an empty configuration
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        // Act
        UserSecretsHelper helper = new(config);

        // Assert - should not throw exceptions
        Assert.IsNull(helper.GetOpenAIApiKey());
        Assert.IsNull(helper.GetMicrosoftGraphClientId());
        Assert.IsNull(helper.GetMicrosoftGraphTenantId());
        Assert.IsFalse(helper.HasSecret("AnyKey"));
    }
}
