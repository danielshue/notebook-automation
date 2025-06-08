// <copyright file="AnsiConsoleHelperTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli.Tests/Utilities/AnsiConsoleHelperTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable

namespace NotebookAutomation.Cli.Tests.Utilities;

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

    [TestMethod]
    public void WriteInfo_PrintsInfoWithColors()
    {
        AnsiConsoleHelper.WriteInfo("info message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("info message"));
    }

    [TestMethod]
    public void WriteWarning_PrintsWarningWithColors()
    {
        AnsiConsoleHelper.WriteWarning("warn message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("warn message"));
    }

    [TestMethod]
    public void WriteError_PrintsErrorWithColors()
    {
        AnsiConsoleHelper.WriteError("error message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("error message"));
    }

    [TestMethod]
    public void WriteSuccess_PrintsSuccessWithColors()
    {
        AnsiConsoleHelper.WriteSuccess("success message");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("success message"));
    }

    [TestMethod]
    public void WriteHeading_PrintsHeadingWithColors()
    {
        AnsiConsoleHelper.WriteHeading("heading");

        // Give Spectre.Console time to write
        Thread.Sleep(50);

        var output = stringWriter!.ToString();
        Assert.IsTrue(output.Contains("heading"));
    }

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