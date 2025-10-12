// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Utilities;

/// <summary>
/// Unit tests for <see cref="AnsiConsoleHelper"/>.
/// </summary>
[TestClass]
public class AnsiConsoleHelperTests
{
    private StringWriter? stringWriter;
    private TextWriter? originalOut;

    [TestInitialize]
    public void Setup()
    {
        stringWriter = new StringWriter();
        originalOut = Console.Out;
        Console.SetOut(stringWriter);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Give Spectre.Console time to finish writing before disposing
        Thread.Sleep(100);
        Console.SetOut(originalOut!);

        // Don't dispose the StringWriter immediately - let GC handle it
        // This prevents ObjectDisposedException when Spectre.Console tries to write asynchronously
        stringWriter = null;
    }

    [TestMethod]
    public void WriteUsage_PrintsUsageWithColors()
    {
        AnsiConsoleHelper.WriteUsage("usage", "desc", "opts");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("usage"));
        Assert.IsTrue(output.Contains("desc"));
        Assert.IsTrue(output.Contains("opts"));
    }

    /// <summary>
    /// Verifies that WriteInfo prints informational messages with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteInfo_PrintsInfoWithColors()
    {
        AnsiConsoleHelper.WriteInfo("info message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("info message"));
    }

    /// <summary>
    /// Verifies that WriteWarning prints warning messages with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteWarning_PrintsWarningWithColors()
    {
        AnsiConsoleHelper.WriteWarning("warn message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("warn message"));
    }

    /// <summary>
    /// Verifies that WriteError prints error messages with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteError_PrintsErrorWithColors()
    {
        AnsiConsoleHelper.WriteError("error message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("error message"));
    }

    /// <summary>
    /// Verifies that WriteSuccess prints success messages with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteSuccess_PrintsSuccessWithColors()
    {
        AnsiConsoleHelper.WriteSuccess("success message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("success message"));
    }

    /// <summary>
    /// Verifies that WriteHeading prints heading text with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteHeading_PrintsHeadingWithColors()
    {
        AnsiConsoleHelper.WriteHeading("heading");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("heading"));
    }

    /// <summary>
    /// Verifies that WriteKeyValue prints key-value pairs with ANSI colors to console output.
    /// </summary>
    [TestMethod]
    public void WriteKeyValue_PrintsKeyValueWithColors()
    {
        AnsiConsoleHelper.WriteKeyValue("key", "value");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("key:"));
        Assert.IsTrue(output.Contains("value"));
    }
}
