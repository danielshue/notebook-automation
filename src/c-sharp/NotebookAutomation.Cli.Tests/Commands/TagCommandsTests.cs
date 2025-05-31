using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.CommandLine.Parsing;
using System.CommandLine.Invocation;


namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for TagCommands.
    /// </summary>
    [TestClass]
    public class TagCommandsTests
    {

        /// <summary>
        /// Verifies that the 'tag clean-index' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task CleanIndexCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag clean-index");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Verifies that the 'tag consolidate' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task ConsolidateCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag consolidate");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Verifies that the 'tag restructure' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task RestructureCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag restructure");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Verifies that the 'tag add-example' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task AddExampleCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag add-example");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Verifies that the 'tag metadata-check' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task MetadataCheckCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag metadata-check");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }

        /// <summary>
        /// Verifies that the 'tag update-frontmatter' command prints usage/help when required arguments are missing.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task UpdateFrontmatterCommand_PrintsUsage_WhenArgsMissing()
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
                // Only provide the subcommand, missing all required arguments
                await parser.InvokeAsync("tag update-frontmatter");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when required args are missing.");
        }

        /// <summary>
        /// Verifies that the 'tag diagnose-yaml' command prints usage/help when no arguments are provided.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [TestMethod]
        public async Task DiagnoseYamlCommand_PrintsUsage_WhenNoArgs()
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
                await parser.InvokeAsync("tag diagnose-yaml");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage"), "Should print usage/help when no args provided.");
        }
    }
}
