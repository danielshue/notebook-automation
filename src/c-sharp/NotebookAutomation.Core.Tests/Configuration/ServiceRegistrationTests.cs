using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tests.Configuration
{
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
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

            // Act
            services.AddNotebookAutomationServices(config);
            var provider = services.BuildServiceProvider();

            // Assert: Key services should be resolvable
            Assert.IsNotNull(provider.GetService<NotebookAutomation.Core.Configuration.AppConfig>());
            Assert.IsNotNull(provider.GetService<NotebookAutomation.Core.Configuration.LoggingService>());
            Assert.IsNotNull(provider.GetService<NotebookAutomation.Core.Services.PromptTemplateService>());
            Assert.IsNotNull(provider.GetService<NotebookAutomation.Core.Services.AISummarizer>());
        }

        [TestMethod]
        public void AddNotebookAutomationServices_ThrowsOnNullArguments()
        {
            IServiceCollection services = null;
            IConfiguration config = null;
            Assert.ThrowsException<ArgumentNullException>(() =>
                ServiceRegistration.AddNotebookAutomationServices(services, new ConfigurationBuilder().Build()));
            Assert.ThrowsException<ArgumentNullException>(() =>
                ServiceRegistration.AddNotebookAutomationServices(new ServiceCollection(), config));
        }
    }
}
