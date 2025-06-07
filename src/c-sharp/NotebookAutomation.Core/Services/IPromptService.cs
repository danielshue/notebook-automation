// <copyright file="IPromptService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Services/IPromptService.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
#nullable enable

namespace NotebookAutomation.Core.Services;

/// <summary>
/// Interface for services that manage and process prompt templates.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides methods for loading, processing, and substituting variables in prompt templates.
/// Implementations should support:
/// <list type="bullet">
/// <item><description>Loading templates from a configured directory</description></item>
/// <item><description>Substituting variables in templates</description></item>
/// <item><description>Generating prompts with substituted variables</description></item>
/// <item><description>Asynchronous processing of templates</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var promptService = serviceProvider.GetService&lt;IPromptService&gt;();
/// var template = await promptService.LoadTemplateAsync("welcome_message");
/// var prompt = promptService.SubstituteVariables(template, new Dictionary&lt;string, string&gt; { { "name", "John" } });
/// Console.WriteLine(prompt);
/// </code>
/// </example>
public interface IPromptService
{
    /// <summary>
    /// Loads a template from the configured prompts directory.
    /// </summary>
    /// <param name="templateName">Name of the template to load, without file extension.</param>
    /// <returns>The template content as a string.</returns>
    /// <remarks>
    /// <para>
    /// This method retrieves the content of a template file from the configured prompts directory.
    /// If the template does not exist, an exception is thrown.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = await promptService.LoadTemplateAsync("welcome_message");
    /// Console.WriteLine(template);
    /// </code>
    /// </example>
    Task<string> LoadTemplateAsync(string templateName);

    /// <summary>
    /// Substitutes variables in a template string.
    /// </summary>
    /// <param name="template">Template with placeholders in the format {{variable_name}}.</param>
    /// <param name="variables">Dictionary of variables to substitute.</param>
    /// <returns>The template with variables substituted.</returns>
    /// <remarks>
    /// <para>
    /// This method replaces placeholders in the template string with values from the provided dictionary.
    /// If a placeholder does not have a corresponding value, it remains unchanged.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var template = "Hello, {{name}}!";
    /// var result = promptService.SubstituteVariables(template, new Dictionary&lt;string, string&gt; { { "name", "John" } });
    /// Console.WriteLine(result); // Outputs: "Hello, John!"
    /// </code>
    /// </example>
    string SubstituteVariables(string template, Dictionary<string, string>? variables);

    /// <summary>
    /// Gets a prompt with variables substituted.
    /// </summary>
    /// <param name="templateName">Name of the template to load, without file extension.</param>
    /// <param name="variables">Dictionary of variables to substitute.</param>
    /// <returns>The prompt with variables substituted.</returns>
    /// <remarks>
    /// <para>
    /// This method combines template loading and variable substitution to generate a complete prompt.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var prompt = await promptService.GetPromptAsync("welcome_message", new Dictionary&lt;string, string&gt; { { "name", "John" } });
    /// Console.WriteLine(prompt);
    /// </code>
    /// </example>
    Task<string> GetPromptAsync(string templateName, Dictionary<string, string>? variables);

    /// <summary>
    /// Processes template with variables asynchronously.
    /// </summary>
    /// <param name="template">The template string with placeholders.</param>
    /// <param name="variables">Dictionary of variable names and values.</param>
    /// <returns>The template with variables substituted.</returns>
    /// <remarks>
    /// <para>
    /// This method performs variable substitution in the provided template string asynchronously.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await promptService.ProcessTemplateAsync("Hello, {{name}}!", new Dictionary&lt;string, string&gt; { { "name", "John" } });
    /// Console.WriteLine(result); // Outputs: "Hello, John!"
    /// </code>
    /// </example>
    Task<string> ProcessTemplateAsync(string template, Dictionary<string, string>? variables);
}
