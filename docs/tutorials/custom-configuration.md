# Custom Configuration Tutorial

Learn how to create custom configurations for different use cases, environments, and workflows.

## Tutorial Overview

**What You'll Learn:**
- Creating environment-specific configurations
- Customizing AI behavior
- Optimizing for different content types
- Managing multiple configuration files
- Advanced configuration techniques

**Time Required:** 30 minutes

## Part 1: Understanding Configuration Hierarchy

### Configuration Precedence

Notebook Automation loads configuration in this order (later overrides earlier):

1. Default application settings
2. Configuration file (config.json)
3. Environment variables
4. Command-line arguments

### Basic Configuration Structure

```json
{
  "AIService": { },
  "Paths": { },
  "Processing": { },
  "OneDrive": { },
  "Logging": { }
}
```

## Part 2: Environment-Specific Configurations

### Scenario: Development, Staging, Production

**Create Three Config Files:**

**config.development.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 500,
    "Temperature": 0.3
  },
  "Processing": {
    "GenerateSummaries": false,
    "ChunkSize": 2000
  },
  "Logging": {
    "MinimumLevel": "Debug"
  },
  "_environment": "development"
}
```

**config.staging.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.5
  },
  "Processing": {
    "GenerateSummaries": true,
    "ChunkSize": 4000
  },
  "Logging": {
    "MinimumLevel": "Information"
  },
  "_environment": "staging"
}
```

**config.production.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-4",
    "MaxTokens": 1500,
    "Temperature": 0.7
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 4000
  },
  "Logging": {
    "MinimumLevel": "Warning"
  },
  "_environment": "production"
}
```

**Usage:**
```bash
# Development (fast, no AI, debug logging)
na pdf-notes -p "test.pdf" --config config.development.json

# Staging (standard AI, normal logging)
na pdf-notes -p "test.pdf" --config config.staging.json

# Production (best quality, minimal logging)
na pdf-notes -p "file.pdf" --config config.production.json
```

## Part 3: Content-Type Specific Configurations

### Configuration for Academic Papers

**config.academic-papers.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.3,
    "CustomPrompts": {
      "SummaryPrompt": "Provide an academic summary focusing on: research question, methodology, key findings, and implications. Be concise and scholarly."
    }
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 6000
  },
  "Metadata": {
    "DefaultTags": ["research", "academic", "paper"],
    "ExtractCitations": true
  }
}
```

**Usage:**
```bash
na pdf-notes -p "research-papers" --config config.academic-papers.json
```

### Configuration for Lecture Videos

**config.lectures.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1500,
    "Temperature": 0.5,
    "CustomPrompts": {
      "SummaryPrompt": "Create a student-friendly summary with: main topics covered, key concepts explained, important takeaways. Use clear, accessible language."
    }
  },
  "Processing": {
    "VideoProcessingTimeoutMinutes": 45,
    "GenerateSummaries": true,
    "ExtractTimestamps": true
  },
  "Metadata": {
    "DefaultTags": ["lecture", "video", "education"]
  }
}
```

**Usage:**
```bash
na video-notes -p "lectures" --config config.lectures.json
```

### Configuration for Quick Batch Processing

**config.fast-batch.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 500
  },
  "Processing": {
    "GenerateSummaries": false,
    "ExtractMetadata": true,
    "ChunkSize": 2000,
    "VideoProcessingTimeoutMinutes": 15,
    "PdfProcessingTimeoutMinutes": 5
  },
  "Logging": {
    "MinimumLevel": "Warning"
  }
}
```

**Usage:**
```bash
# Fast extraction, no AI summaries
na pdf-notes -p "large-batch" --config config.fast-batch.json
```

## Part 4: Advanced Customizations

### Multi-Provider Configuration

**Scenario:** Use different providers for different tasks

**config.multi-provider.json:**
```json
{
  "AIProviders": {
    "Default": {
      "Provider": "OpenAI",
      "Model": "gpt-3.5-turbo",
      "ApiKey": "${OPENAI_API_KEY}"
    },
    "HighQuality": {
      "Provider": "OpenAI",
      "Model": "gpt-4",
      "ApiKey": "${OPENAI_API_KEY}"
    },
    "Fast": {
      "Provider": "Anthropic",
      "Model": "claude-3-haiku-20240307",
      "ApiKey": "${ANTHROPIC_API_KEY}"
    }
  },
  "Processing": {
    "DefaultProvider": "Default"
  }
}
```

### Custom Templates

**config.custom-templates.json:**
```json
{
  "Templates": {
    "PdfNoteTemplate": "templates/custom-pdf-template.md",
    "VideoNoteTemplate": "templates/custom-video-template.md",
    "MetadataTemplate": "templates/custom-metadata.yaml"
  },
  "Formatting": {
    "DateFormat": "yyyy-MM-dd",
    "TimeFormat": "HH:mm:ss",
    "UseMarkdownLinks": true
  }
}
```

### Organization-Specific Settings

**config.organization.json:**
```json
{
  "Organization": {
    "Name": "Acme University",
    "DefaultVault": "/shared/knowledge-base"
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "/shared/knowledge-base",
    "TemplatesPath": "/shared/templates",
    "OutputPath": "/shared/processed"
  },
  "OneDrive": {
    "ClientId": "org-client-id",
    "TenantId": "org-tenant-id",
    "SharedDriveRoot": "/shared/source-content"
  },
  "Metadata": {
    "DefaultTags": ["acme-university", "knowledge-base"],
    "AddOrganizationMetadata": true,
    "OrganizationPrefix": "ACME"
  }
}
```

## Part 5: Configuration Management

### Using Environment Variables

**Create .env file:**
```bash
# .env (not committed to git)
OPENAI_API_KEY=sk-your-actual-key
AZURE_OPENAI_KEY=your-azure-key
ANTHROPIC_API_KEY=sk-ant-your-key

VAULT_ROOT=/path/to/vault
OUTPUT_DIR=/path/to/output
```

**Load in shell:**
```bash
# Linux/Mac
source .env

# Windows PowerShell
Get-Content .env | ForEach-Object {
  $var = $_.Split('=')
  [Environment]::SetEnvironmentVariable($var[0], $var[1])
}
```

**Reference in config:**
```json
{
  "AIService": {
    "ApiKey": "${OPENAI_API_KEY}"
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "${VAULT_ROOT}"
  }
}
```

### Configuration Validation Script

**validate-config.sh:**
```bash
#!/bin/bash

CONFIG_FILE=$1

echo "Validating configuration: $CONFIG_FILE"

# Check JSON syntax
if python -m json.tool "$CONFIG_FILE" >/dev/null 2>&1; then
  echo "✅ Valid JSON syntax"
else
  echo "❌ Invalid JSON syntax"
  exit 1
fi

# Check required fields
PROVIDER=$(jq -r '.AIService.Provider' "$CONFIG_FILE")
if [ "$PROVIDER" != "null" ]; then
  echo "✅ AI Provider configured: $PROVIDER"
else
  echo "⚠️  No AI Provider configured"
fi

# Check API key (shouldn't be in file for production)
APIKEY=$(jq -r '.AIService.ApiKey' "$CONFIG_FILE")
if [ "$APIKEY" != "null" ] && [ "$APIKEY" != "\${OPENAI_API_KEY}" ]; then
  echo "⚠️  API key in config file (security risk!)"
else
  echo "✅ API key using environment variable"
fi

echo "Validation complete!"
```

**Usage:**
```bash
chmod +x validate-config.sh
./validate-config.sh config.production.json
```

## Part 6: Practical Examples

### Example 1: MBA Program Configuration

**config.mba-program.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.6
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 4000,
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 15
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "./vault/MBA-Program"
  },
  "Metadata": {
    "DefaultTags": ["mba", "business", "education"],
    "CoursePrefix": "MBA",
    "SemesterFormat": "Fall-2025"
  },
  "Logging": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/mba-processing-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Example 2: Research Lab Configuration

**config.research-lab.json:**
```json
{
  "AIService": {
    "Provider": "Azure",
    "Endpoint": "https://research-lab.openai.azure.com/",
    "DeploymentName": "gpt-4-deployment",
    "ApiVersion": "2024-02-01",
    "MaxTokens": 2000,
    "Temperature": 0.3
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ExtractCitations": true,
    "ChunkSize": 6000
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "/lab-shared/knowledge-base",
    "TemplatesPath": "/lab-shared/templates"
  },
  "Metadata": {
    "DefaultTags": ["research", "lab", "academic"],
    "LabName": "AI Research Lab",
    "RequireDOI": true
  }
}
```

### Example 3: Personal Learning Configuration

**config.personal-learning.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 800,
    "Temperature": 0.7,
    "CustomPrompts": {
      "SummaryPrompt": "Create a study guide summary with key points, important concepts, and study questions."
    }
  },
  "Processing": {
    "GenerateSummaries": true,
    "GenerateQuestions": true,
    "ChunkSize": 3000
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "~/Documents/MyLearning"
  },
  "Metadata": {
    "DefaultTags": ["personal", "learning", "self-study"],
    "AddStudyMetadata": true
  }
}
```

## Part 7: Configuration Templates

### Template: Minimal Configuration

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo"
  }
}
```

### Template: Complete Configuration

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "${OPENAI_API_KEY}",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.7,
    "TopP": 1.0,
    "FrequencyPenalty": 0.0,
    "PresencePenalty": 0.0
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "./vault",
    "TemplatesPath": "./templates",
    "OutputPath": "./output"
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "CreateCrossLinks": true,
    "UseHierarchicalTags": true,
    "ChunkSize": 4000,
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10,
    "DefaultTimeoutMinutes": 15
  },
  "OneDrive": {
    "ClientId": "${ONEDRIVE_CLIENT_ID}",
    "TenantId": "common",
    "Scopes": ["Files.ReadWrite.All", "Sites.ReadWrite.All"]
  },
  "Logging": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/notebook-automation-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      },
      {
        "Name": "Console",
        "Args": {
          "restrictedToMinimumLevel": "Information"
        }
      }
    ]
  },
  "Metadata": {
    "DefaultTags": [],
    "AddProcessingMetadata": true,
    "DateFormat": "yyyy-MM-dd",
    "TimeFormat": "HH:mm:ss"
  }
}
```

## Best Practices

**1. Use Environment Variables for Secrets:**
```json
"ApiKey": "${OPENAI_API_KEY}"  // ✅ Good
"ApiKey": "sk-actual-key"      // ❌ Bad
```

**2. Create Config Templates:**
```bash
# Keep template in version control
config.template.json

# Copy and customize
cp config.template.json config.json
# Add to .gitignore
echo "config.json" >> .gitignore
```

**3. Document Custom Settings:**
```json
{
  "_comment": "Production configuration for MBA program processing",
  "_updated": "2025-01-18",
  "AIService": {
    "_note": "Using gpt-4 for high quality summaries",
    "Model": "gpt-4"
  }
}
```

**4. Validate Before Use:**
```bash
# Always validate
cat config.json | python -m json.tool
na config view --config config.json
```

**5. Version Control Strategy:**
```bash
# Commit templates and examples
git add config.template.json
git add config.example-*.json

# Never commit actual configs with secrets
# Add to .gitignore
config.json
config.*.json
!config.template.json
!config.example-*.json
```

## Troubleshooting

**Configuration Not Loading:**
```bash
# Specify explicitly
na pdf-notes -p "file.pdf" --config /full/path/to/config.json

# Check current config
na config view
```

**Environment Variables Not Expanding:**
```bash
# Set before running
export OPENAI_API_KEY="sk-your-key"
na pdf-notes -p "file.pdf" --config config.json

# Or inline (Linux/Mac)
OPENAI_API_KEY="sk-key" na pdf-notes -p "file.pdf"
```

**JSON Syntax Errors:**
```bash
# Validate
python -m json.tool config.json

# Common errors:
# - Trailing commas
# - Missing quotes
# - Unescaped backslashes (Windows paths)
```

## Next Steps

- [AI Services Configuration](../configuration/ai-services.md)
- [Configuration Problems](../troubleshooting/configuration-problems.md)
- [Getting Started Tutorial](getting-started-tutorial.md)

## Summary

You've learned:
- ✅ Creating environment-specific configurations
- ✅ Content-type specific settings
- ✅ Using environment variables for secrets
- ✅ Configuration validation and management
- ✅ Real-world configuration examples
- ✅ Best practices for configuration files

**You're ready to customize Notebook Automation for any use case!**
