// <copyright file="OneDriveCliOptionsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core.Tests/Services/OneDriveCliOptionsTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="OneDriveCliOptions"/> class.
/// </summary>
[TestClass]
public class OneDriveCliOptionsTests
{
    [TestMethod]
    public void Properties_DefaultValues_AreFalse()
    {
        OneDriveCliOptions options = new();
        Assert.IsFalse(options.DryRun);
        Assert.IsFalse(options.Verbose);
        Assert.IsFalse(options.Force);
        Assert.IsFalse(options.Retry);
    }

    [TestMethod]
    public void Properties_CanBeSetAndGet()
    {
        OneDriveCliOptions options = new()
        {
            DryRun = true,
            Verbose = true,
            Force = true,
            Retry = true,
        };
        Assert.IsTrue(options.DryRun);
        Assert.IsTrue(options.Verbose);
        Assert.IsTrue(options.Force);
        Assert.IsTrue(options.Retry);
    }

    [TestMethod]
    public void Properties_CanBeChangedIndividually()
    {
        OneDriveCliOptions options = new()
        {
            DryRun = true,
        };
        Assert.IsTrue(options.DryRun);
        options.Verbose = true;
        Assert.IsTrue(options.Verbose);
        options.Force = true;
        Assert.IsTrue(options.Force);
        options.Retry = true;
        Assert.IsTrue(options.Retry);
    }
}
