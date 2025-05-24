using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Core.Configuration;
using System.IO;
using System.Text.Json;

namespace NotebookAutomation.Core.Tests
{
    [TestClass]
    public class UserSecretsTests
    {
        [TestMethod]
        public void ConfigurationSetup_ShouldLoadUserSecrets()
        {
            // Arrange
            var tempSecretFile = Path.GetTempFileName();
            try
            {
                // Create a temporary user secrets file with test data
                var secretsJson = JsonSerializer.Serialize(new
                {
                    UserSecrets = new
                    {
                        OpenAI = new
                        {
                            ApiKey = "test-openai-key"
                        },
                        Microsoft = new
                        {
                            ClientId = "test-client-id",
                            TenantId = "test-tenant-id"
                        }
                    }
                });
                File.WriteAllText(tempSecretFile, secretsJson);

                // Create test configuration with custom secret file
                var configBuilder = new ConfigurationBuilder()
                    .AddJsonFile(tempSecretFile); // Simulate user secrets
                
                var config = configBuilder.Build();

                // Act
                var helper = new UserSecretsHelper(config);

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
            var config = new ConfigurationBuilder().Build();
            
            // Act
            var helper = new UserSecretsHelper(config);
            
            // Assert - should not throw exceptions
            Assert.IsNull(helper.GetOpenAIApiKey());
            Assert.IsNull(helper.GetMicrosoftGraphClientId());
            Assert.IsNull(helper.GetMicrosoftGraphTenantId());
            Assert.IsFalse(helper.HasSecret("AnyKey"));
        }
    }
}
