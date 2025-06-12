// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using NotebookAutomation.Cli.Commands;
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Models;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Tools.Vault;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for VaultCommands focusing on the handling of relative paths
/// and their resolution against the configured vault root.
/// </summary>
/// <remarks>
/// Tests the proper handling of relative paths when executing vault commands, ensuring they
/// are correctly resolved based on the configured vault root path instead of the current
/// working directory. These tests validate the fix for path handling issues when commands
/// are executed with paths that are relative to the vault root rather than absolute paths.
/// </remarks>
[TestClass]
public class VaultCommandsRelativePathTests
{
    private readonly Mock<ILogger<VaultCommands>> _mockLogger = new();
    private readonly Mock<IServiceProvider> _mockServiceProvider = new();
    private readonly Mock<AppConfig> _appConfig = new();
    private readonly Mock<PathsConfig> _pathsConfig = new();
    private string _tempDir = string.Empty;
    private string _vaultRoot = string.Empty;

    /// <summary>
    /// Set up test environment, creating a temporary directory structure
    /// and configuring mocks for testing path resolution.
    /// </summary>
    [TestInitialize]
    public void Setup()
    {
        // Create a temporary directory for tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"VaultTest_{Guid.NewGuid()}");
        _vaultRoot = Path.Combine(_tempDir, "VaultRoot");
        Directory.CreateDirectory(_vaultRoot);

        // Create test nested structure in vault root
        var testStructure = Path.Combine(_vaultRoot, "Value Chain Management", "Operations Management");
        Directory.CreateDirectory(testStructure);

        // Set up AppConfig with proper vault root path
        _pathsConfig.SetupGet(p => p.NotebookVaultFullpathRoot).Returns(_vaultRoot);
        _appConfig.SetupGet(a => a.Paths).Returns(_pathsConfig.Object);        // Setup ServiceProvider to return AppConfig
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(_appConfig.Object);

        // Can't mock extension method directly
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(AppConfig)))
            .Returns(_appConfig.Object);
    }

    /// <summary>
    /// Clean up temporary test directories after tests run.
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Tests that relative paths are properly resolved against the configured vault root,
    /// not the current working directory.
    /// </summary>
    [TestMethod]
    public async Task ExecuteVaultCommand_WithRelativePath_ResolvesAgainstVaultRoot()
    {
        // Arrange
        var vaultCommands = new VaultCommands(_mockLogger.Object, _mockServiceProvider.Object);        // Create dependencies for the test processor
        var mockBatchLogger = new Mock<ILogger<VaultIndexBatchProcessor>>();
        var mockProcessor = new Mock<IVaultIndexProcessor>();
        var mockHierarchyDetector = new Mock<IMetadataHierarchyDetector>();
        // Create our test processor with a custom function to intercept the path
        string? capturedPath = null;
        var testProcessor = new TestVaultIndexBatchProcessor(
            mockBatchLogger.Object,
            mockProcessor.Object,
            mockHierarchyDetector.Object,
            (path, vp, f, d, t) =>
            {
                capturedPath = path;
                return Task.FromResult(new BatchProcessingResult { Success = true });
            });

        // Set up the service provider to return our test processor
        var mockVaultRootContext = new Mock<VaultRootContextService>();
        var mockScope = new Mock<IServiceScope>();
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScopeServiceProvider = new Mock<IServiceProvider>(); mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(VaultRootContextService)))
            .Returns(mockVaultRootContext.Object);
        // Using GetService instead of GetRequiredService
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(VaultRootContextService)))
            .Returns(mockVaultRootContext.Object);
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(VaultIndexBatchProcessor)))
            .Returns(testProcessor);
        // Using GetService instead of GetRequiredService
        mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(VaultIndexBatchProcessor)))
            .Returns(testProcessor);

        mockScope.Setup(s => s.ServiceProvider).Returns(mockScopeServiceProvider.Object);
        mockScopeFactory.Setup(f => f.CreateScope()).Returns(mockScope.Object); _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);
        // Using GetService instead of GetRequiredService
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockScopeFactory.Object);

        // Define paths for testing
        string relativePath = "Value Chain Management\\Operations Management";
        string expectedFullPath = Path.Combine(_vaultRoot, "Value Chain Management", "Operations Management");        // Create a helper method to invoke private ExecuteVaultCommandAsync
        MethodInfo? methodInfo = typeof(VaultCommands).GetMethod("ExecuteVaultCommandAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        Assert.IsNotNull(methodInfo, "ExecuteVaultCommandAsync method not found");

        await (Task)methodInfo.Invoke(vaultCommands, new object?[]
        {
            "generate-index",
            relativePath,
            null, // configPath
            false, // debug
            false, // verbose
            false, // dryRun
            false, // force
            null, // vaultRoot
            null // templateTypes
        })!;

        // Assert
        // Verify the path was resolved correctly (with platform-agnostic comparison)
        Assert.IsNotNull(capturedPath, "The path was not captured during processing");

        Assert.IsTrue(
            string.Equals(
                Path.GetFullPath(capturedPath!),
                Path.GetFullPath(expectedFullPath),
                OperatingSystem.IsWindows() ?
                    StringComparison.OrdinalIgnoreCase :
                    StringComparison.Ordinal
            ),
            $"Expected path: {expectedFullPath}, but got: {capturedPath}"
        );        // Verify logger calls about resolving path
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Using path relative to vault root")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }    /// <summary>
         /// Modified VaultIndexBatchProcessor for testing purposes
         /// </summary>

    public class TestVaultIndexBatchProcessor : VaultIndexBatchProcessor
    {
        private readonly Func<string, string, bool, bool, string[]?, Task<BatchProcessingResult>> _processAsyncFunc;

        public TestVaultIndexBatchProcessor(
            ILogger<VaultIndexBatchProcessor> logger,
            IVaultIndexProcessor processor,
            IMetadataHierarchyDetector hierarchyDetector,
            Func<string, string, bool, bool, string[]?, Task<BatchProcessingResult>> processAsyncFunc)
            : base(logger, processor, hierarchyDetector)
        {
            _processAsyncFunc = processAsyncFunc;
        }

        /// <summary>
        /// Override the base class method to capture the path for testing
        /// </summary>

        public override async Task<VaultIndexBatchResult> GenerateIndexesAsync(
            string vaultPath,
            bool dryRun = false,
            List<string>? templateTypes = null,
            bool forceOverwrite = false,
            string? vaultRoot = null)
        {
            // Call the process function to capture the path
            await _processAsyncFunc(vaultPath, vaultRoot ?? string.Empty, forceOverwrite, dryRun, templateTypes?.ToArray());

            // Create a minimal result for the test
            return new VaultIndexBatchResult
            {
                Success = true,
                ProcessedFolders = 0,
                SkippedFolders = 1,
                TotalFolders = 1
            };
        }
    }
}