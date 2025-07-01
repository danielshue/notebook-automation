// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Unit tests for VaultCommands.
/// </summary>
[TestClass]
public class VaultCommandsTests
{
    private readonly Mock<ILogger<VaultCommands>> mockLogger = new Mock<ILogger<VaultCommands>>();
    private readonly Mock<IServiceProvider> mockServiceProvider = new Mock<IServiceProvider>();
    private readonly Mock<AppConfig> mockAppConfig = new Mock<AppConfig>();

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

    [TestMethod]
    public async Task SyncDirsCommand_PrintsUsage_WhenMissingRequiredArguments()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        
        // Setup mock services for sync-dirs command
        var mockSyncProcessor = new Mock<IVaultFolderSyncProcessor>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetRequiredService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: invoke sync-dirs with missing required arguments
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault sync-dirs").ConfigureAwait(false);

            // Assert - Should fail due to missing required argument
            Assert.AreNotEqual(0, result, "Command should fail when required arguments are missing");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public async Task SyncDirsCommand_ExecutesSuccessfully_WithValidArguments()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        
        // Setup mock services for sync-dirs command
        var mockSyncProcessor = new Mock<IVaultFolderSyncProcessor>();
        var mockResult = new VaultFolderSyncResult
        {
            Success = true,
            TotalFolders = 5,
            SynchronizedFolders = 5,
            CreatedVaultFolders = 3,
            SkippedFolders = 2,
            FailedFolders = 0
        };
        
        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(mockResult);
        
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetRequiredService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault sync-dirs MBA/Finance /test/vault --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault sync-dirs"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            
            // Verify the processor was called with correct parameters (bidirectional defaults to true)
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync("MBA/Finance", "/test/vault", true, true), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public async Task SyncDirsCommand_ExecutesBidirectional_WithBidirectionalFlag()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        
        // Setup mock services for sync-dirs command
        var mockSyncProcessor = new Mock<IVaultFolderSyncProcessor>();
        var mockResult = new VaultFolderSyncResult
        {
            Success = true,
            TotalFolders = 8,
            SynchronizedFolders = 8,
            CreatedVaultFolders = 3,
            CreatedOneDriveFolders = 2,
            SkippedFolders = 3,
            FailedFolders = 0
        };
        
        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(mockResult);
        
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetRequiredService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault sync-dirs MBA/Finance /test/vault --bidirectional --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault bidirectional sync-dirs"), "Should show bidirectional execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            Assert.IsTrue(output.Contains("Created 3 new vault directories"), "Should show vault directory creation count");
            Assert.IsTrue(output.Contains("Created 2 new OneDrive directories"), "Should show OneDrive directory creation count");
            
            // Verify the processor was called with bidirectional = true
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync("MBA/Finance", "/test/vault", true, true), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [TestMethod]
    public async Task SyncDirsCommand_ExecutesSuccessfully_WithUnidirectionalFlag()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        
        // Setup mock services for sync-dirs command
        var mockSyncProcessor = new Mock<IVaultFolderSyncProcessor>();
        var mockResult = new VaultFolderSyncResult
        {
            Success = true,
            TotalFolders = 3,
            SynchronizedFolders = 3,
            CreatedVaultFolders = 2,
            CreatedOneDriveFolders = 0, // No OneDrive folders created in unidirectional mode
            SkippedFolders = 1,
            FailedFolders = 0
        };
        
        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(mockResult);
        
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetRequiredService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        
        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act - Test with --unidirectional flag
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault sync-dirs MBA/Finance /test/vault --unidirectional --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault sync-dirs"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            
            // Verify the processor was called with bidirectional = false due to --unidirectional flag
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync("MBA/Finance", "/test/vault", true, false), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
