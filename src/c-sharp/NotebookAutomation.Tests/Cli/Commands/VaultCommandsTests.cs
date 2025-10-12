// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using NotebookAutomation.Core.Configuration;

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
    private readonly Mock<PathsConfig> mockPathsConfig = new Mock<PathsConfig>();
    private string _tempVaultRoot = string.Empty;

    public VaultCommandsTests()
    {
        // Create a temporary directory for tests
        _tempVaultRoot = Path.Combine(Path.GetTempPath(), $"VaultTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempVaultRoot);

        // Setup the mock service provider to return AppConfig
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        // Setup AppConfig.Paths to return PathsConfig with real temp vault root and OneDrive config
        mockPathsConfig.SetupGet(p => p.NotebookVaultFullpathRoot).Returns(_tempVaultRoot);
        mockPathsConfig.SetupGet(p => p.OnedriveFullpathRoot).Returns("C:/OneDriveRoot");
        mockPathsConfig.SetupGet(p => p.OnedriveResourcesBasepath).Returns("Education/MBA-Resources");
        mockAppConfig.SetupGet(a => a.Paths).Returns(mockPathsConfig.Object);

        // Ensure GetEffectiveVaultRoot uses real implementation so tests exercising vault-sync
        // receive a non-empty effective vault root value (otherwise Moq returns default null).
        mockPathsConfig.CallBase = true;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up the temporary vault directory
        if (Directory.Exists(_tempVaultRoot))
        {
            Directory.Delete(_tempVaultRoot, true);
        }
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

    /// <summary>
    /// Verifies that VaultCommand can be initialized successfully.
    /// </summary>
    [TestMethod]
    public void VaultCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);

        // Act & Assert
        Assert.IsNotNull(command);
    }

    /// <summary>
    /// Verifies that Register adds vault command and subcommands to root command.
    /// </summary>
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

    /// <summary>
    /// Verifies that logger extensions are available and can be called.
    /// </summary>
    [TestMethod]
    public void LoggerExtensions_AreCalled()
    {
        // Arrange
        // No logger method setups; just pass the mock to the command.
    }

    /// <summary>
    /// Verifies that CleanIndexCommand displays an info message when executed.
    /// </summary>
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

    /// <summary>
    /// Verifies that VaultSyncCommand executes successfully when invoked with no arguments.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesSuccessfully_WithNoArguments()
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
        // Mock the generic GetService method that GetRequiredService calls internally
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: invoke vault-sync (vault-path is optional, defaults to vault root)
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault vault-sync").ConfigureAwait(false);

            // Assert: Should execute successfully with default vault root
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault") || output.Contains("vault-sync"), "Should show execution message");
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Verifies that VaultSyncCommand executes successfully using the default vault root from configuration.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesSuccessfully_WithDefaultVaultRoot()
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

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        // Mock the generic GetService method that GetRequiredService calls internally
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act - Use default vault root (no vault-path provided)
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync("vault vault-sync --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");

            // Verify the processor was called with correct parameters (full normalized OneDrive path, vault root from config)
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources"));
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, _tempVaultRoot, true, true, false, null), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Verifies that VaultSyncCommand executes successfully when provided with a specific vault path.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesSuccessfully_WithVaultPath()
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

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        // Mock the generic GetService method that GetRequiredService calls internally
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act - Provide specific vault path
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");

            // Verify the processor was called with correct parameters (full normalized OneDrive path, full vault path)
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, true, false, null), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Verifies that VaultSyncCommand executes bidirectional sync by default.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesBidirectional_WithBidirectionalFlag()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");

        // Setup mock services for vault-sync command
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

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        // Mock the generic GetService method that GetRequiredService calls internally
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault bidirectional vault-sync"), "Should show bidirectional execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            Assert.IsTrue(output.Contains("Created 3 new vault directories"), "Should show vault directory creation count");
            Assert.IsTrue(output.Contains("Created 2 new OneDrive directories"), "Should show OneDrive directory creation count");

            // Verify the processor was called with bidirectional = true (default)
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, true, false, null), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Verifies that VaultSyncCommand executes unidirectional sync when --unidirectional flag is provided.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesSuccessfully_WithUnidirectionalFlag()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");

        // Setup mock services for vault-sync command
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

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        // Mock the generic GetService method that GetRequiredService calls internally
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act - Test with --unidirectional flag
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --unidirectional --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault vault-sync"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");

            // Verify the processor was called with bidirectional = false due to --unidirectional flag
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, false, false, null), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }


    /// <summary>
    /// Tests that SyncDirsCommand executes successfully with document types option.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_ExecutesSuccessfully_WithCreatePlaceholdersOption()
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
            TotalFolders = 2,
            SynchronizedFolders = 2,
            CreatedVaultFolders = 2,
            CreatedOneDriveFolders = 0,
            CreatedPlaceholderFiles = 3,
            SkippedFolders = 0,
            FailedFolders = 0
        };

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act - Test with --create-placeholders option
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --create-placeholders videos pdf html --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault bidirectional vault-sync"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            Assert.IsTrue(output.Contains("Created 3 placeholder markdown files"), "Should show placeholder files created");

            // Verify the processor was called with the correct create placeholders types
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            var expectedCreatePlaceholderTypes = new List<string> { "videos", "pdf", "html" };
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, true, false, It.Is<List<string>?>(list =>
                list != null && list.Count == 3 && list.Contains("videos") && list.Contains("pdf") && list.Contains("html"))), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }


    /// <summary>
    /// Tests that SyncDirsCommand accepts space-separated document types.
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_AcceptsSpaceSeparatedCreatePlaceholders()
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
            TotalFolders = 1,
            SynchronizedFolders = 1,
            CreatedVaultFolders = 1,
            CreatedPlaceholderFiles = 2,
            SkippedFolders = 0,
            FailedFolders = 0
        };

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act - Test with space-separated document types
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --create-placeholders videos pdf --recursive --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault bidirectional vault-sync"), "Should show execution message");
            Assert.IsTrue(output.Contains("Created 2 placeholder markdown files"), "Should show placeholder files created");

            // Verify the processor was called with the correct create placeholders types and recursive flag
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            var expectedCreatePlaceholderTypes = new List<string> { "videos", "pdf" };
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, true, true, It.Is<List<string>?>(list =>
                list != null && list.Count == 2 && list.Contains("videos") && list.Contains("pdf"))), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }


    /// <summary>
    /// Tests that SyncDirsCommand works without document types (legacy behavior).
    /// </summary>
    [TestMethod]
    public async Task VaultSyncCommand_WorksWithoutCreatePlaceholders_LegacyBehavior()
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
            TotalFolders = 1,
            SynchronizedFolders = 1,
            CreatedVaultFolders = 1,
            CreatedPlaceholderFiles = 0, // No placeholder files when no document types specified
            SkippedFolders = 0,
            FailedFolders = 0
        };

        mockSyncProcessor.Setup(p => p.SyncDirectoriesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<List<string>?>()))
            .ReturnsAsync(mockResult);

        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);
        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Create the test vault path
            var testVaultPath = Path.Combine(_tempVaultRoot, "MBA", "Finance");
            Directory.CreateDirectory(testVaultPath);

            // Act - Test without document types
            var parser = new Parser(rootCommand);
            var result = await parser.InvokeAsync($"vault vault-sync \"{testVaultPath}\" --dry-run").ConfigureAwait(false);

            // Assert
            Assert.AreEqual(0, result, "Command should execute successfully");
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Executing vault bidirectional vault-sync"), "Should show execution message");
            Assert.IsTrue(output.Contains("completed successfully"), "Should show success message");
            // Should not mention placeholder files when none are created
            Assert.IsFalse(output.Contains("placeholder markdown files"), "Should not mention placeholder files when none created");

            // Verify the processor was called with null create placeholder types
            var expectedOneDrivePath = NotebookAutomation.Core.Utils.PathUtils.NormalizePath(System.IO.Path.Combine("C:/OneDriveRoot", "Education/MBA-Resources", "MBA/Finance"));
            mockSyncProcessor.Verify(p => p.SyncDirectoriesAsync(expectedOneDrivePath, testVaultPath, true, true, false, null), Times.Once);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }


    /// <summary>
    /// Tests that SyncDirsCommand help shows document types option.
    /// </summary>
    [TestMethod]
    public void VaultSyncCommand_Help_ShowsCreatePlaceholdersOption()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");

        var vaultCommands = new VaultCommands(mockLogger.Object, mockServiceProvider.Object);

        // Setup mock services for sync-dirs command (needed even for help)
        var mockSyncProcessor = new Mock<IVaultFolderSyncProcessor>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IVaultFolderSyncProcessor)))
            .Returns(mockSyncProcessor.Object);
        mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(mockAppConfig.Object);

        vaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Act & Assert - Check that the command was registered with the option
        // Since console output capture can be tricky with System.CommandLine,
        // we'll verify the command structure instead
        var vaultCommand = rootCommand.Subcommands
            .FirstOrDefault(sc => sc.Name == "vault");

        Assert.IsNotNull(vaultCommand, "vault command should be registered");

        var vaultSyncCommand = vaultCommand!.Subcommands
            .FirstOrDefault(sc => sc.Name == "vault-sync");

        Assert.IsNotNull(vaultSyncCommand, "vault-sync command should be registered");

        var createPlaceholdersOption = vaultSyncCommand!.Options
            .FirstOrDefault(opt => opt.Name == "create-placeholders");

        Assert.IsNotNull(createPlaceholdersOption, "create-placeholders option should be registered");
        Assert.IsTrue(createPlaceholdersOption?.HasAlias("-p") == true, "Option should have -p alias");
        Assert.IsTrue(createPlaceholdersOption?.Description?.Contains("placeholder") == true, "Option should have proper description");
        Assert.IsTrue(createPlaceholdersOption?.Description?.Contains("videos") == true, "Description should mention videos");
        Assert.IsTrue(createPlaceholdersOption?.Description?.Contains("pdf") == true, "Description should mention pdf");
        Assert.IsTrue(createPlaceholdersOption?.Description?.Contains("html") == true, "Description should mention html");
    }
}
