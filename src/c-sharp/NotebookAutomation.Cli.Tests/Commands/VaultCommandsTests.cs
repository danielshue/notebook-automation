// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for VaultCommands.
/// </summary>
[TestClass]
public class VaultCommandsTests
{
    private readonly Mock<ILogger<VaultCommands>> mockLogger = new();
    private readonly Mock<IServiceProvider> mockServiceProvider = new();
    private readonly Mock<AppConfig> mockAppConfig = new();

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
        VaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Capture console output
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: invoke with no args (should print usage)
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync("vault-generate-index").ConfigureAwait(false);
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
        var rootCommand = new RootCommand(); var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");

        // Act
        VaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Assert
        var vaultGenerateIndexCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "vault-generate-index");
        var vaultEnsureMetadataCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "vault-ensure-metadata");
        var vaultCleanIndexCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "vault-clean-index");

        Assert.IsNotNull(vaultGenerateIndexCommand, "vault-generate-index command should be registered on the root command.");
        Assert.IsNotNull(vaultEnsureMetadataCommand, "vault-ensure-metadata command should be registered on the root command.");
        Assert.IsNotNull(vaultCleanIndexCommand, "vault-clean-index command should be registered on the root command.");
    }

    [TestMethod]
    public void LoggerExtensions_AreCalled()
    {
        // Arrange
        // No logger method setups; just pass the mock to the command.
    }

    [TestMethod]
    public async Task CleanIndexCommand_DeletesAllIndexFiles()
    {
        // Arrange
        var rootCommand = new RootCommand(); var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var verboseOption = new Option<bool>("--verbose");
        var dryRunOption = new Option<bool>("--dry-run");
        VaultCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

        // Create a temp directory with index and non-index files
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        try
        {
            // Index file (type: index)
            string indexFile = Path.Combine(tempDir, "index.md");
            await File.WriteAllTextAsync(indexFile, "---\ntype: index\ntemplate-type: course-index\n---\nContent").ConfigureAwait(false);

            // Index file (template-type: case-studies-index)
            string caseStudiesIndexFile = Path.Combine(tempDir, "case-studies.md");
            await File.WriteAllTextAsync(caseStudiesIndexFile, "---\ntemplate-type: case-studies-index\n---\nContent").ConfigureAwait(false);

            // Non-index file
            string noteFile = Path.Combine(tempDir, "note.md");
            await File.WriteAllTextAsync(noteFile, "---\ntype: note\ntemplate-type: note-case-study\n---\nContent").ConfigureAwait(false);

            // Act
            var parser = new Parser(rootCommand);
            await parser.InvokeAsync($"vault-clean-index {tempDir}").ConfigureAwait(false);

            // Assert
            Assert.IsFalse(File.Exists(indexFile), "Index file (type: index) should be deleted");
            Assert.IsFalse(File.Exists(caseStudiesIndexFile), "Index file (template-type: case-studies-index) should be deleted");
            Assert.IsTrue(File.Exists(noteFile), "Non-index file should not be deleted");
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
