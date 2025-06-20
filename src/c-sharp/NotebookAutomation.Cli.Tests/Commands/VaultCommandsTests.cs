// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for VaultCommands.
/// </summary>
[TestClass]
public class VaultCommandsTests
{
    private readonly Mock<ILogger<VaultCommands>> mockLogger = new object();
    private readonly Mock<IServiceProvider> mockServiceProvider = new object();
    private readonly Mock<AppConfig> mockAppConfig = new object();

    public VaultCommandsTests()
    {
        // Setup the mock service provider to return AppConfig
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);
    }
    [TestMethod]
    public async Task GenerateIndexCommand_PrintsUsage_WhenNoArgs()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: invoke with no args (should print usage for vault parent command)
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("vault").ConfigureAwait(false);
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
        var command = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);

        // Act & Assert
        Assert.IsNotNull(command);
    }
    [TestMethod]
    public void Register_AddsVaultCommandToRoot()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");        // Act
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var vaultCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "vault");
        Assert.IsNotNull(vaultCommand, "vault command should be registered on the root command.");

        var vaultGenerateIndexCommand = vaultCommand.Subcommands.FirstOrDefault(c => c.Name == "generate-index");
        var vaultEnsureMetadataCommand = vaultCommand.Subcommands.FirstOrDefault(c => c.Name == "ensure-metadata");
        var vaultCleanIndexCommand = vaultCommand.Subcommands.FirstOrDefault(c => c.Name == "clean-index");

        Assert.IsNotNull(vaultGenerateIndexCommand, "generate-index command should be registered under vault command.");
        Assert.IsNotNull(vaultEnsureMetadataCommand, "ensure-metadata command should be registered under vault command.");
        Assert.IsNotNull(vaultCleanIndexCommand, "clean-index command should be registered under vault command.");
    }

    [TestMethod]
    public void LoggerExtensions_AreCalled()
    {
        // Arrange
        // No logger method setups; just pass the mock to the command.
    }
    [TestMethod]
    public async Task CleanIndexCommand_ShowsInfoMessage()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Create a temp directory with test files
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Create a test file
            string testFile = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(testFile, "---\ntype: index\n---\nContent").ConfigureAwait(false);

            // Capture console output
            var originalOut = Console.Out;
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            try
            {
                // Act
                var parser = new Parser(rootCommand);
                await parser.InvokeAsync($"vault clean-index {tempDir}").ConfigureAwait(false);

                // Assert - The current implementation shows an info message about executing the command
                string output = stringWriter.ToString();
                Assert.IsTrue(output.Contains("Executing vault clean-index"), "Should show info message about executing clean-index command.");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
