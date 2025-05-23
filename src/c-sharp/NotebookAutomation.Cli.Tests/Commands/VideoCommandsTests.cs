using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for VideoCommands.
    /// </summary>
    [TestClass]
    public class VideoCommandsTests
    {
        [TestMethod]
        public void VideoCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new VideoCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsVideoMetaCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var verboseOption = new System.CommandLine.Option<bool>("--verbose");
            var dryRunOption = new System.CommandLine.Option<bool>("--dry-run");
            var videoCommands = new VideoCommands();

            // Act
            videoCommands.Register(rootCommand, configOption, debugOption, verboseOption, dryRunOption);

            // Assert
            var videoMetaCommand = rootCommand.Children.FirstOrDefault(c => c.Name == "video-meta");
            Assert.IsNotNull(videoMetaCommand, "video-meta command should be registered on the root command.");
        }
    }
}
