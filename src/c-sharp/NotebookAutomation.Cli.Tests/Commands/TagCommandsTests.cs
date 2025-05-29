using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for TagCommands.
    /// </summary>
    [TestClass]
    public class TagCommandsTests
    {
        /// <summary>
        /// Verifies that the 'tag add-nested' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task AddNestedCommand_PrintsUsage_WhenNoArgs()
        {
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var tagCommands = new TagCommands();
            tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            var originalOut = Console.Out;
            var stringWriter = new System.IO.StringWriter();
            Console.SetOut(stringWriter);
            try
            {
                var parser = new System.CommandLine.Parsing.Parser(rootCommand);
                await parser.InvokeAsync("tag add-nested");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }
        /// <summary>
        /// Tests that the TagCommands class can be instantiated successfully.
        /// </summary>
        [TestMethod]
        public void TagCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new TagCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        /// <summary>
        /// Tests that the Register method adds the tag command and its subcommands to the root command.
        /// </summary>
        [TestMethod]
        public void Register_AddsTagCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var tagCommands = new TagCommands();

            // Act
            tagCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var tagCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "tag");
            Assert.IsNotNull(tagCommand, "tag command should be registered on the root command.");

            // Check subcommands
            var subcommands = tagCommand.Subcommands.Select(c => c.Name).ToList();
            CollectionAssert.Contains(subcommands, "add-nested", "tag should have 'add-nested' subcommand");
            CollectionAssert.Contains(subcommands, "clean-index", "tag should have 'clean-index' subcommand");
            CollectionAssert.Contains(subcommands, "consolidate", "tag should have 'consolidate' subcommand");
            CollectionAssert.Contains(subcommands, "restructure", "tag should have 'restructure' subcommand");
            CollectionAssert.Contains(subcommands, "add-example", "tag should have 'add-example' subcommand");
            CollectionAssert.Contains(subcommands, "metadata-check", "tag should have 'metadata-check' subcommand");
            CollectionAssert.Contains(subcommands, "update-frontmatter", "tag should have 'update-frontmatter' subcommand");
            CollectionAssert.Contains(subcommands, "diagnose-yaml", "tag should have 'diagnose-yaml' subcommand");
        }
    }
}
