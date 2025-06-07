# Configuration

Learn how to configure Notebook Automation for optimal performance and customization.

## Overview

Configuration in Notebook Automation is flexible and hierarchical, allowing you to customize behavior at multiple levels:

- **Global configuration** - System-wide defaults
- **Project configuration** - Project-specific settings
- **Command-line options** - Runtime overrides

## Configuration File Structure

Configuration files use JSON format and follow this basic structure:

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "your-api-key",
      "Model": "gpt-4",
      "MaxTokens": 2000
    },
    "RateLimit": {
      "RequestsPerMinute": 50,
      "DelayBetweenRequests": 1000
    }
  },
  "Processing": {
    "OutputFormat": "json",
    "SummaryStyle": "academic",
    "AnalysisLevel": "standard"
  },
  "Paths": {
    "OutputDirectory": "./output",
    "TemplateDirectory": "./templates",
    "LogDirectory": "./logs"
  },
  "Logging": {
    "Level": "Information",
    "EnableFileLogging": true,
    "EnableConsoleLogging": true
  }
}
```

## Quick Setup

### 1. Initialize Configuration

Create a default configuration file:

```bash
na.exe config init
```

This creates a `config.json` file in your current directory with default settings.

### 2. Configure AI Service

Choose and configure your AI provider:

**For OpenAI:**

```bash
na.exe config set "AIService.Provider" "OpenAI"
na.exe config set "AIService.OpenAI.ApiKey" "your-openai-api-key"
```

**For Azure OpenAI:**

```bash
na.exe config set "AIService.Provider" "AzureOpenAI"
na.exe config set "AIService.AzureOpenAI.ApiKey" "your-azure-key"
na.exe config set "AIService.AzureOpenAI.Endpoint" "https://your-resource.openai.azure.com/"
```

**For IBM Foundry:**

```bash
na.exe config set "AIService.Provider" "Foundry"
na.exe config set "AIService.Foundry.ApiKey" "your-foundry-key"
na.exe config set "AIService.Foundry.ProjectId" "your-project-id"
```

### 3. Validate Configuration

Check that your configuration is correct:

```bash
na.exe config validate
```

### 4. Test Processing

Run a test to ensure everything works:

```bash
na.exe process "sample-document.md" --verbose
```

## Configuration Sections

### AI Service Configuration

Configure AI providers and their specific settings.

#### OpenAI Configuration

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4",
      "MaxTokens": 2000,
      "Temperature": 0.3,
      "Organization": "org-..."
    }
  }
}
```

**Available Models:**

- `gpt-4` - Most capable, higher cost
- `gpt-4-turbo` - Faster, cost-effective
- `gpt-3.5-turbo` - Fastest, lowest cost

#### Azure OpenAI Configuration

```json
{
  "AIService": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "ApiKey": "your-key",
      "Endpoint": "https://your-resource.openai.azure.com/",
      "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  }
}
```

#### IBM Foundry Configuration

```json
{
  "AIService": {
    "Provider": "Foundry",
    "Foundry": {
      "ApiKey": "your-key",
      "ProjectId": "your-project-id",
      "Model": "meta-llama/llama-2-70b-chat",
      "ServiceUrl": "https://us-south.ml.cloud.ibm.com"
    }
  }
}
```

### Processing Configuration

Control how documents are processed and analyzed.

```json
{
  "Processing": {
    "OutputFormat": "json",
    "SummaryStyle": "academic",
    "AnalysisLevel": "standard",
    "IncludeSourceText": false,
    "GenerateSummaries": true,
    "ExtractKeywords": true,
    "AnalyzeSentiment": false,
    "DetectTopics": true
  }
}
```

**Options:**

- **OutputFormat**: `json`, `yaml`, `xml`
- **SummaryStyle**: `academic`, `technical`, `casual`
- **AnalysisLevel**: `basic`, `standard`, `deep`

### Path Configuration

Specify directories for input, output, and resources.

```json
{
  "Paths": {
    "OutputDirectory": "./output",
    "TemplateDirectory": "./templates",
    "LogDirectory": "./logs",
    "ConfigDirectory": "./config",
    "TempDirectory": "./temp"
  }
}
```

### Rate Limiting

Configure API rate limiting to avoid service limits.

```json
{
  "AIService": {
    "RateLimit": {
      "RequestsPerMinute": 50,
      "DelayBetweenRequests": 1000,
      "MaxRetries": 3,
      "BackoffMultiplier": 2.0
    }
  }
}
```

### Logging Configuration

Control logging behavior and output.

```json
{
  "Logging": {
    "Level": "Information",
    "EnableFileLogging": true,
    "EnableConsoleLogging": true,
    "LogFormat": "json",
    "MaxLogFileSize": "10MB",
    "MaxLogFiles": 5
  }
}
```

**Log Levels**: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

## Configuration Management

### Environment Variables

Use environment variables for sensitive information:

```bash
# Set API key via environment variable
export NOTEBOOK_AUTOMATION_OPENAI_API_KEY="your-key"

# Override configuration path
export NOTEBOOK_AUTOMATION_CONFIG_PATH="/path/to/config.json"
```

### Multiple Configurations

Manage different configurations for different scenarios:

```bash
# Development configuration
na.exe process "docs/" --config "dev-config.json"

# Production configuration
na.exe process "docs/" --config "prod-config.json"

# Testing configuration
na.exe process "docs/" --config "test-config.json"
```

### Configuration Inheritance

Configurations can inherit from base configurations:

```json
{
  "extends": "./base-config.json",
  "AIService": {
    "OpenAI": {
      "Model": "gpt-3.5-turbo"
    }
  }
}
```

## Security Best Practices

### API Key Management

1. **Use environment variables** instead of configuration files:

   ```bash
   export OPENAI_API_KEY="your-key"
   ```

2. **Use secret management services**:
   - Azure Key Vault
   - AWS Secrets Manager
   - HashiCorp Vault

3. **Restrict file permissions**:

   ```bash
   chmod 600 config.json  # Unix/Linux
   ```

### Configuration Validation

Always validate configuration before production use:

```bash
# Validate configuration
na.exe config validate --config "prod-config.json"

# Test connectivity
na.exe config test --config "prod-config.json"

# Show resolved configuration
na.exe config show --config "prod-config.json"
```

## Advanced Configuration

### Custom Templates

Configure custom templates for metadata extraction and summary generation:

```json
{
  "Templates": {
    "MetadataExtraction": "./templates/metadata-template.yaml",
    "SummaryGeneration": "./templates/summary-template.md",
    "OutputFormatting": "./templates/output-template.json"
  }
}
```

### Processing Rules

Define rules for different file types or content patterns:

```json
{
  "ProcessingRules": [
    {
      "Pattern": "lecture-*.md",
      "SummaryStyle": "academic",
      "AnalysisLevel": "deep",
      "ExtractCourseInfo": true
    },
    {
      "Pattern": "api-*.html",
      "SummaryStyle": "technical",
      "AnalysisLevel": "standard",
      "ExtractApiInfo": true
    }
  ]
}
```

### Output Customization

Customize output structure and content:

```json
{
  "Output": {
    "Structure": "hierarchical",
    "IncludeTimestamps": true,
    "IncludeProcessingStats": true,
    "CompressOutput": false,
    "GenerateIndex": true
  }
}
```

## Configuration Examples

### Academic Research Configuration

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "OpenAI": {
      "Model": "gpt-4",
      "Temperature": 0.2
    }
  },
  "Processing": {
    "SummaryStyle": "academic",
    "AnalysisLevel": "deep",
    "ExtractCitations": true,
    "AnalyzeMethodology": true
  }
}
```

### Fast Processing Configuration

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "OpenAI": {
      "Model": "gpt-3.5-turbo",
      "MaxTokens": 1000
    },
    "RateLimit": {
      "RequestsPerMinute": 100
    }
  },
  "Processing": {
    "AnalysisLevel": "basic",
    "GenerateSummaries": false
  }
}
```

### Enterprise Configuration

```json
{
  "AIService": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
      "DeploymentName": "gpt-4-enterprise",
      "ApiVersion": "2024-02-15-preview"
    }
  },
  "Security": {
    "EnableAuditLogging": true,
    "RequireAuthentication": true,
    "DataRetention": "30d"
  },
  "Monitoring": {
    "EnableMetrics": true,
    "MetricsEndpoint": "https://metrics.company.com",
    "AlertOnErrors": true
  }
}
```

## Troubleshooting Configuration

### Common Issues

**"Configuration file not found"**

```bash
# Check current directory
na.exe config show

# Specify config path explicitly
na.exe config show --config "/full/path/to/config.json"
```

**"Invalid API key"**

```bash
# Validate API key
na.exe config validate

# Test connectivity
na.exe config test
```

**"Rate limit exceeded"**

```bash
# Reduce rate limit in configuration
na.exe config set "AIService.RateLimit.RequestsPerMinute" "30"
```

### Configuration Debugging

Enable detailed logging to troubleshoot configuration issues:

```bash
# Enable debug logging
na.exe config set "Logging.Level" "Debug"

# Process with verbose output
na.exe process "test.md" --verbose --config "debug-config.json"
```

## Next Steps

- [AI Service Setup](ai-services.md) - Detailed AI service configuration
- [User Secrets](user-secrets.md) - Secure credential management
- [Performance Tuning](../user-guide/performance-tuning.md) - Optimize configuration for performance
