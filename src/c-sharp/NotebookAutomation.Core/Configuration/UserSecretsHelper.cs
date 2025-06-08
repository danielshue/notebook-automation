// <copyright file="UserSecretsHelper.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Core/Configuration/UserSecretsHelper.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides convenient access to user secrets in the application.
/// </summary>
/// <remarks>
/// The <c>UserSecretsHelper</c> class simplifies the retrieval of sensitive information
/// stored in user secrets, such as API keys and client credentials. It leverages the
/// application's configuration system to access these secrets securely.
/// </remarks>
/// <param name="configuration">The configuration to use for accessing user secrets.</param>
public class UserSecretsHelper(IConfiguration configuration)
{
    private readonly IConfiguration configuration = configuration;

    /// <summary>
    /// Gets an OpenAI API key from user secrets, if available.
    /// </summary>
    /// <returns>The API key if found in user secrets; otherwise, null.</returns>
    public string? GetOpenAIApiKey()
    {
        return configuration["UserSecrets:OpenAI:ApiKey"];
    }

    /// <summary>
    /// Gets a Microsoft Graph client ID from user secrets, if available.
    /// </summary>
    /// <returns>The client ID if found in user secrets; otherwise, null.</returns>
    public string? GetMicrosoftGraphClientId()
    {
        return configuration["UserSecrets:Microsoft:ClientId"];
    }

    /// <summary>
    /// Gets a Microsoft Graph tenant ID from user secrets, if available.
    /// </summary>
    /// <returns>The tenant ID if found in user secrets; otherwise, null.</returns>
    public string? GetMicrosoftGraphTenantId()
    {
        return configuration["UserSecrets:Microsoft:TenantId"];
    }

    /// <summary>
    /// Gets any user secret by key.
    /// </summary>
    /// <param name="key">The key of the user secret to get.</param>
    /// <returns>The value if found; otherwise, null.</returns>
    public string? GetSecret(string key)
    {
        return configuration[$"UserSecrets:{key}"];
    }

    /// <summary>
    /// Determines whether a specific user secret exists.
    /// </summary>
    /// <param name="key">The key of the user secret to check.</param>
    /// <returns>True if the user secret exists; otherwise, false.</returns>
    public bool HasSecret(string key)
    {
        return !string.IsNullOrEmpty(configuration[$"UserSecrets:{key}"]);
    }
}
