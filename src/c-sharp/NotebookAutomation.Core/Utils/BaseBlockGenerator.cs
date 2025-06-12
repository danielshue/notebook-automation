// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides functionality to generate Obsidian Base blocks from a YAML template with dynamic parameters.
/// </summary>

public static class BaseBlockGenerator
{
    /// <summary>
    /// Loads the base block template from the specified path and fills in the placeholders.
    /// </summary>
    /// <param name="templatePath">Path to the YAML template file.</param>
    /// <param name="course">Course name to inject.</param>
    /// <param name="className">Class name to inject.</param>
    /// <param name="module">Module name to inject.</param>
    /// <param name="type">Type to inject.</param>
    /// <returns>The filled-in base block as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the template file does not exist.</exception>
    public static string GenerateBaseBlock(string templatePath, string course, string className, string module, string type)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Base block template not found: {templatePath}");
        }

        string template = File.ReadAllText(templatePath);
        return template
            .Replace("{Course}", course ?? string.Empty)
            .Replace("{Class}", className ?? string.Empty)
            .Replace("{Module}", module ?? string.Empty)
            .Replace("{Type}", type ?? string.Empty);
    }
}