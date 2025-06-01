using System;
using System.IO;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// Unit tests for LoggingService, ConfigProvider, ConfigurationExtensions, and ConfigurationSetup.
    /// </summary>
    [TestClass]
    public class LoggingServiceAndConfigProviderTests
    {
        [TestMethod]
        public void ConfigurationExtensions_AddObject_ThrowsOnNull()
        {
            var builder = new ConfigurationBuilder();
            Assert.ThrowsException<ArgumentNullException>(() => builder.AddObject(null));
        }

        [TestMethod]
        public void ConfigProvider_GetService_ThrowsOnUnregisteredType()
        {
            var appConfig = new AppConfig { Paths = new PathsConfig { LoggingDir = Path.GetTempPath() } };
            var loggingService = new LoggingService(appConfig, debug: false);
            var provider = new ConfigProvider(loggingService, appConfig);
            // Try to get a type that is not registered
            Assert.ThrowsException<InvalidOperationException>(() => provider.GetService<IDisposable>());
        }
        [TestMethod]
        public void LoggingService_SingletonAndFactoryMethods_Work()
        {
            var logger1 = LoggingService.Instance.Logger;
            var logger2 = LoggingService.Instance.FailedLogger;
            Assert.IsNotNull(logger1);
            Assert.IsNotNull(logger2);
            var logger3 = LoggingService.CreateLogger("TestCategory");
            Assert.IsNotNull(logger3);
            var logger4 = LoggingService.CreateLogger<LoggingServiceAndConfigProviderTests>();
            Assert.IsNotNull(logger4);
            var failedLogger = LoggingService.CreateFailedLogger();
            Assert.IsNotNull(failedLogger);
            var minLevelDebug = LoggingService.GetMinLogLevel(true);
            var minLevelInfo = LoggingService.GetMinLogLevel(false);
            Assert.AreEqual(LogLevel.Debug, minLevelDebug);
            Assert.AreEqual(LogLevel.Information, minLevelInfo);
            var asmName = LoggingService.GetAssemblyName();
            Assert.IsFalse(string.IsNullOrEmpty(asmName));
        }

        [TestMethod]
        public void LoggingService_ConstructorWithAppConfig_Works()
        {
            var appConfig = new AppConfig { Paths = new PathsConfig { LoggingDir = Path.GetTempPath() } };
            var service = new LoggingService(appConfig, debug: true);
            Assert.IsNotNull(service.Logger);
            Assert.IsNotNull(service.FailedLogger);
        }

        [TestMethod]
        public void ConfigProvider_CreateAndGetService_Works()
        {
            var appConfig = new AppConfig { Paths = new PathsConfig { LoggingDir = Path.GetTempPath() } };
            var loggingService = new LoggingService(appConfig, debug: true);
            var provider = new ConfigProvider(loggingService, appConfig);
            Assert.IsNotNull(provider.AppConfig);
            Assert.IsNotNull(provider.LoggingService);
            Assert.IsNotNull(provider.ServiceProvider);
            Assert.IsNotNull(provider.Logger);
            Assert.IsNotNull(provider.FailedLogger);
            var logger = provider.GetLogger<LoggingServiceAndConfigProviderTests>();
            Assert.IsNotNull(logger);
            var appConfig2 = provider.GetService<AppConfig>();
            Assert.IsNotNull(appConfig2);
        }

        [TestMethod]
        public void ConfigProvider_CreateStatic_Works()
        {
            var provider = ConfigProvider.Create(null, true);
            Assert.IsNotNull(provider);
            Assert.IsNotNull(provider.AppConfig);
            Assert.IsNotNull(provider.LoggingService);
            Assert.IsNotNull(provider.ServiceProvider);
        }

        [TestMethod]
        public void ConfigProvider_InitializeDefaultConfiguration_SetsDefaults()
        {
            var appConfig = new AppConfig();
            var loggingService = new LoggingService(appConfig, debug: false);
            var provider = new ConfigProvider(loggingService, appConfig);
            // Use reflection to call private method
            var method = typeof(ConfigProvider).GetMethod("InitializeDefaultConfiguration", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(provider, null);
            Assert.IsNotNull(provider.AppConfig.Paths.LoggingDir);
        }

        [TestMethod]
        public void ConfigurationExtensions_AddObject_Works()
        {
            var builder = new ConfigurationBuilder();
            var obj = new { Foo = "Bar", Nested = new { Baz = 42 } };
            builder.AddObject(obj);
            var config = builder.Build();
            Assert.AreEqual("Bar", config["Foo"]);
            Assert.AreEqual("42", config["Nested:Baz"]);
        }

        [TestMethod]
        public void ConfigurationSetup_BuildConfiguration_Works()
        {
            var config = ConfigurationSetup.BuildConfiguration(environment: "Development");
            Assert.IsNotNull(config);
            var config2 = ConfigurationSetup.BuildConfiguration<LoggingServiceAndConfigProviderTests>(environment: "Development");
            Assert.IsNotNull(config2);
        }
    }
}
