namespace NotebookAutomation.Core.Models;

/// <summary>
/// Options for vault index generation.
/// </summary>
public class VaultIndexOptions
{
    /// <summary>
    /// Gets or sets whether to perform a dry run without creating files.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to force overwrite existing index files.
    /// </summary>
    public bool ForceOverwrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the specific depth level to process (null for all levels).
    /// </summary>
    public int? Depth { get; set; }
}
