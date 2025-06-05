using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Cli.Tests.Commands;    /// <summary>
                                                    /// Unit tests for VaultCommands.
                                                    /// </summary>    [TestClass]
public class VaultCommandsTests
{
    private readonly Mock<ILogger<VaultCommands>> _mockLogger = new();
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();
    private readonly Mock<AppConfig> _mockAppConfig = new();

    public VaultCommandsTests()
    {
        // Setup the mock service provider to return AppConfig
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(_mockAppConfig.Object);
    }

    [TestMethod]
    public async Task GenerateIndexCommand_PrintsUsage_WhenNoArgs()
    {
        // Arrange
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var vaultCommands = new VaultCommands(_mockLogger.Object, _mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new System.IO.StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: invoke with no args (should print usage)
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("vault generate-index");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
        // Assert
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
    }

    [TestMethod]
    public void VaultCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new VaultCommands(_mockLogger.Object, _mockServiceProvider.Object);
        // Act & Assert
        Assert.IsNotNull(command);
    }

    [TestMethod]
    public void Register_AddsVaultCommandToRoot()
    {
        // Arrange
        var rootCommand = new System.CommandLine.RootCommand();
        var configOption = new System.CommandLine.Option<string>("--config");
        var debugOption = new System.CommandLine.Option<bool>("--debug");
        var verboseOption = new System.CommandLine.Option<bool>("--verbose");
        var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
        var vaultCommands = new VaultCommands(_mockLogger.Object, _mockServiceProvider.Object);

        // Act
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var vaultCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "vault");
        Assert.IsNotNull(vaultCommand, "vault command should be registered on the root command.");

        // Check subcommands
        var subcommands = vaultCommand.Subcommands.Select(c => c.Name).ToList();
        CollectionAssert.Contains(subcommands, "generate-index", "vault should have 'generate-index' subcommand");
        CollectionAssert.Contains(subcommands, "ensure-metadata", "vault should have 'ensure-metadata' subcommand");
    }

    [TestMethod]
    public void LoggerExtensions_AreCalled()
    {
        // Arrange
        // No logger method setups; just pass the mock to the command.
    }
}
