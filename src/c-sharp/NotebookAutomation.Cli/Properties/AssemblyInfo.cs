using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// Specifies that internal members of the CLI assembly are visible to the test assembly.
/// </summary>
[assembly: InternalsVisibleTo("NotebookAutomation.Cli.Tests")]

/// <summary>
/// Provides a short description of the assembly.
/// </summary>
[assembly: AssemblyDescription("Command-line interface for managing course-related content.")]

/// <summary>
/// Specifies whether the assembly is visible to COM components.
/// </summary>
[assembly: ComVisible(false)]

/// <summary>
/// Specifies a unique identifier for the assembly when exposed to COM.
/// </summary>
[assembly: Guid("d1234567-d89b-1234-5678-1234567890ab")]
