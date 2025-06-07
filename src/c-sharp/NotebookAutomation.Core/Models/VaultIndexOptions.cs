// <copyright file="VaultIndexOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Models/VaultIndexOptions.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Models;

/// <summary>
/// Options for vault index generation.
/// </summary>
public class VaultIndexOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to perform a dry run without creating files.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets whether to force overwrite existing index files.
    /// </summary>
    public bool ForceOverwrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the specific depth level to process (null for all levels).
    /// </summary>
    public int? Depth { get; set; }
}
