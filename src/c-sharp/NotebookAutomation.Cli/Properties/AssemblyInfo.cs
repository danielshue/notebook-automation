using System.Runtime.CompilerServices;
using System.Reflection;
using System.Runtime.InteropServices;

/// <summary>
/// Specifies that internal members of the CLI assembly are visible to the test assembly.
/// </summary>
[assembly: InternalsVisibleTo("NotebookAutomation.Cli.Tests")]

/// <summary>
/// Specifies the title of the assembly.
/// </summary>
[assembly: AssemblyTitle("Notebook Automation")]

/// <summary>
/// Provides a short description of the assembly.
/// </summary>
[assembly: AssemblyDescription("Command-line interface for managing course-related content.")]

/// <summary>
/// Specifies the build configuration (e.g., Debug or Release).
/// </summary>
[assembly: AssemblyConfiguration("Release")]

/// <summary>
/// Specifies the company that produced the assembly.
/// </summary>
[assembly: AssemblyCompany("Dan Shue")]

/// <summary>
/// Specifies the product name.
/// </summary>
[assembly: AssemblyProduct("Notebook Automation")]

/// <summary>
/// Specifies copyright information.
/// </summary>
[assembly: AssemblyCopyright("© 2025 Dan Shue")]

/// <summary>
/// Specifies trademark information.
/// </summary>
[assembly: AssemblyTrademark("")]

/// <summary>
/// Specifies the version of the assembly.
/// </summary>
[assembly: AssemblyVersion("1.0.0.0")]

/// <summary>
/// Specifies the file version of the assembly.
/// </summary>
[assembly: AssemblyFileVersion("1.0.0.0")]

/// <summary>
/// Specifies additional version information, such as pre-release or build metadata.
/// </summary>
[assembly: AssemblyInformationalVersion("1.0.0-beta")]

/// <summary>
/// Specifies whether the assembly is visible to COM components.
/// </summary>
[assembly: ComVisible(false)]

/// <summary>
/// Specifies a unique identifier for the assembly when exposed to COM.
/// </summary>
[assembly: Guid("d1234567-d89b-1234-5678-1234567890ab")]
