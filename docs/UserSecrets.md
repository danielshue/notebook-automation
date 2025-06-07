---
auto-generated-state: writable
date-created: 2025-06-06
publisher: University of Illinois at Urbana-Champaign
tags: ''
---

# Using User Secrets in Notebook Automation

User secrets provide a way to store sensitive information like API keys outside of your configuration files. This ensures that sensitive data doesn't get committed to source control and is only available on your development machine.

## Setting Up User Secrets

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2025 or VS Code with C# extension

### Initialize User Secrets

To initialize user secrets for the project:

```powershell
cd src/c-sharp/NotebookAutomation.Core
dotnet user-secrets init
```

You can also initialize user secrets for the CLI project:

```powershell
cd src/c-sharp/NotebookAutomation.Cli
dotnet user-secrets init
```

This will create a `UserSecretsId` entry in the project file if it doesn't already exist.

### Adding Secrets

Add your sensitive API keys and other secrets using the following commands:

```powershell
dotnet user-secrets set "UserSecrets:OpenAI:ApiKey" "your-openai-api-key"
dotnet user-secrets set "UserSecrets:Microsoft:ClientId" "your-microsoft-client-id"
dotnet user-secrets set "UserSecrets:Microsoft:TenantId" "your-microsoft-tenant-id"
```

### Viewing Current Secrets

To list all of the secrets stored for the project:

```powershell
dotnet user-secrets list
```

### Removing Secrets

To remove a specific secret:

```powershell
dotnet user-secrets remove "UserSecrets:OpenAI:ApiKey"
```

To clear all secrets:

```powershell
dotnet user-secrets clear
```

## Secret Storage Location

User secrets are stored in your user profile in a JSON file:

- Windows: `%APPDATA%\Microsoft\UserSecrets\<user_secrets_id>\secrets.json`
- macOS/Linux: `~/.microsoft/usersecrets/<user_secrets_id>/secrets.json`

Where `<user_secrets_id>` is the value specified in your project file.

## How Secrets are Used in the Application

The application loads user secrets during the configuration setup stage with this priority:

1. User secrets (in development environment)
2. Configuration files (config.json)
3. Environment variables

This allows you to:

1. Keep sensitive information out of source control
2. Have different API keys for different environments
3. Override configuration values for local development

### Accessing API Keys in Code

The `AIServiceConfig` class has been updated to retrieve API keys from user secrets. You can access the API key using the `GetApiKey()` method:

```csharp
// Example usage
var apiKey = appConfig.AiService.GetApiKey();
```

The `GetApiKey()` method first checks for the API key in user secrets, then falls back to environment variables and other configured sources.

## Secret Format

The secrets.json file uses a simple JSON format:

```json
{
  "UserSecrets": {
    "OpenAI": {
      "ApiKey": "your-api-key-here"
    },
    "Microsoft": {
      "ClientId": "your-client-id",
      "TenantId": "your-tenant-id"
    }
  }
}
```

## Important Notes

- User secrets are designed for development only. For production environments, use Azure Key Vault or environment variables.
- The user secrets file is not encrypted, so ensure your user profile is secured appropriately.
- Remember to add sensitive keys to your .gitignore file if storing them in other locations.

## For Testing

When writing tests that need API keys, you can set up a mock configuration:

```csharp
// Example of setting up test configuration
var configDict = new Dictionary<string, string>
{
    {"UserSecrets:OpenAI:ApiKey", "test-api-key"}
};
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(configDict)
    .Build();
config.AiService.SetConfiguration(configuration);
```

This approach allows your tests to run without requiring actual API keys while still testing the correct flow.

## Reference Documentation

For more information on user secrets, see Microsoft's documentation:
- [Safe storage of app secrets in development in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
