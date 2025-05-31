using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Utilities;
using System.IO;

namespace NotebookAutomation.Cli.Tests.Utilities
{
    /// <summary>
    /// Unit tests for <see cref="AnsiConsoleHelper"/>.
    /// </summary>
    [TestClass]
    public class AnsiConsoleHelperTests
    {
        private StringWriter _stringWriter = null!;
        private TextWriter _originalOut = null!;

        [TestInitialize]
        public void Setup()
        {
            _stringWriter = new StringWriter();
            _originalOut = Console.Out;
            Console.SetOut(_stringWriter);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.SetOut(_originalOut);
            _stringWriter.Dispose();
        }

        [TestMethod]
        public void WriteUsage_PrintsUsageWithColors()
        {
            AnsiConsoleHelper.WriteUsage("usage", "desc", "opts");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("usage"));
            Assert.IsTrue(output.Contains("desc"));
            Assert.IsTrue(output.Contains("opts"));
        }

        [TestMethod]
        public void WriteInfo_PrintsInfoWithColors()
        {
            AnsiConsoleHelper.WriteInfo("info message");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("info message"));
        }

        [TestMethod]
        public void WriteWarning_PrintsWarningWithColors()
        {
            AnsiConsoleHelper.WriteWarning("warn message");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("warn message"));
        }

        [TestMethod]
        public void WriteError_PrintsErrorWithColors()
        {
            AnsiConsoleHelper.WriteError("error message");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("error message"));
        }

        [TestMethod]
        public void WriteSuccess_PrintsSuccessWithColors()
        {
            AnsiConsoleHelper.WriteSuccess("success message");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("success message"));
        }

        [TestMethod]
        public void WriteHeading_PrintsHeadingWithColors()
        {
            AnsiConsoleHelper.WriteHeading("heading");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("heading"));
        }

        [TestMethod]
        public void WriteKeyValue_PrintsKeyValueWithColors()
        {
            AnsiConsoleHelper.WriteKeyValue("key", "value");
            var output = _stringWriter.ToString();
            Assert.IsTrue(output.Contains("key:"));
            Assert.IsTrue(output.Contains("value"));
        }
    }
}
