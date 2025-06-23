// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests for the ServiceRegistration class.
/// </summary>
[TestClass]
public class ServiceRegistrationTests
{
    [TestMethod]
    public void AddNotebookAutomationServices_RegistersCoreServices()
    {
        // Arrange
        ServiceCollection services = new();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"paths:logging_dir", Path.GetTempPath()}
            })
            .Build();        // Act
        services.AddNotebookAutomationServices(config);
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert: Key services should be resolvable
        var appConfig = provider.GetService<AppConfig>();
        Assert.IsNotNull(appConfig, "AppConfig should be registered and resolvable");

        var loggingService = provider.GetService<LoggingService>();
        Assert.IsNotNull(loggingService, "LoggingService should be registered and resolvable");

        var promptService = provider.GetService<PromptTemplateService>();
        Assert.IsNotNull(promptService, "PromptTemplateService should be registered and resolvable");

        var aiSummarizer = provider.GetService<IAISummarizer>();
        Assert.IsNotNull(aiSummarizer, "IAISummarizer should be registered and resolvable");
    }
    [TestMethod]
    public void AddNotebookAutomationServices_ThrowsOnNullArguments()
    {
        IServiceCollection? services = null;
        IConfiguration? config = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            ServiceRegistration.AddNotebookAutomationServices(services!, new ConfigurationBuilder().Build()));
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            ServiceRegistration.AddNotebookAutomationServices(new ServiceCollection(), config!));
    }
}
