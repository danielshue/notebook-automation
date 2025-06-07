// <copyright file="VaultFileInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/VaultFileInfo.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Represents information about a file within a vault for index generation.
/// </summary>
public class VaultFileInfo
{
    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative path from the vault root.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly title for display.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the content type (reading, video, transcript, etc.).
    /// </summary>
    public string ContentType { get; set; } = "note";

    /// <summary>
    /// Gets or sets the course name.
    /// </summary>
    public string? Course { get; set; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Gets or sets the full file path.
    /// </summary>
    public string FullPath { get; set; } = string.Empty;
}
