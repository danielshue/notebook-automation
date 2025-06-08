// <copyright file="VersionCommandsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli.Tests/Commands/VersionCommandsTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for VersionCommands.
/// </summary>
[TestClass]
public class VersionCommandsTests
{
    [TestMethod]
    public void VersionCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new VersionCommands();

        // Act & Assert
        Assert.IsNotNull(command);
    }

    [TestMethod]
    public void Register_AddsVersionCommandToRoot()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var versionCommands = new VersionCommands();

        // Act
        VersionCommands.Register(rootCommand);

        // Assert
        var versionCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "version");
        Assert.IsNotNull(versionCommand, "version command should be registered on the root command.");
    }
}