# Configuration Problems

Troubleshoot configuration-related issues with Notebook Automation.

## Configuration File Locations

### Default Search Order

Notebook Automation looks for configuration in this order:

1. **Command-line specified:** `--config path/to/config.json`
2. **Current directory:** `./config.json`
3. **Config subdirectory:** `./config/appsettings.json`
4. **User directory:** `~/.notebook-automation/config.json`
5. **Environment variables:** Individual settings from environment

### Platform-Specific Locations

**Windows:**
```
C:\Users\{username}\.notebook-automation\config.json
C:\Users\{username}\AppData\Local\NotebookAutomation\config.json
```

**Linux:**
```
~/.notebook-automation/config.json
~/.config/notebook-automation/config.json
```

**macOS:**
```
~/.notebook-automation/config.json
~/Library/Application Support/NotebookAutomation/config.json
```

## Common Configuration Issues

### Issue 1: Configuration File Not Found

**Symptom:**
```
Warning: Configuration file not found, using defaults
```

**Diagnosis:**
```bash
# Check if config file exists
ls -la config.json
cat config.json

# Verify the path
na config view
```

**Solutions:**

**Option 1: Create Config File**
```bash
# Create basic configuration
cat > config.json << EOF
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key-here"
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "/path/to/vault"
  }
}
EOF
```

**Option 2: Specify Config Path**
```bash
# Use --config flag
na video-notes -p "video.mp4" --config "/full/path/to/config.json"
```

**Option 3: Use Default Location**
```bash
# Place config in one of the default locations
mkdir -p ~/.notebook-automation
cp config.json ~/.notebook-automation/
```

### Issue 2: Invalid JSON Syntax

**Symptom:**
```
Error: Failed to parse configuration file
Unexpected character at line 5, column 12
```

**Common JSON Errors:**

**Trailing Commas:**
```json
// ❌ WRONG
{
  "AIService": {
    "Provider": "OpenAI",  // ← Trailing comma
  }
}

// ✅ CORRECT
{
  "AIService": {
    "Provider": "OpenAI"
  }
}
```

**Missing Quotes:**
```json
// ❌ WRONG
{
  AIService: {
    Provider: OpenAI
  }
}

// ✅ CORRECT
{
  "AIService": {
    "Provider": "OpenAI"
  }
}
```

**Unescaped Backslashes (Windows Paths):**
```json
// ❌ WRONG
{
  "Paths": {
    "NotebookVaultFullpathRoot": "C:\Users\MyVault"
  }
}

// ✅ CORRECT (escaped)
{
  "Paths": {
    "NotebookVaultFullpathRoot": "C:\\Users\\MyVault"
  }
}

// ✅ ALSO CORRECT (forward slashes)
{
  "Paths": {
    "NotebookVaultFullpathRoot": "C:/Users/MyVault"
  }
}
```

**Solution: Validate JSON**
```bash
# Use JSON validator
cat config.json | python -m json.tool

# Or online: https://jsonlint.com/
```

### Issue 3: Missing Required Fields

**Symptom:**
```
Error: AI Service provider not configured
Error: API key is required
```

**Minimal Valid Configuration:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here"
  }
}
```

**Complete Configuration Template:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.7
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "/path/to/vault"
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 4000,
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10
  },
  "OneDrive": {
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id"
  },
  "Logging": {
    "MinimumLevel": "Information"
  }
}
```

### Issue 4: API Key Configuration

**Symptoms:**
- "API key not found"
- "Unauthorized" errors
- "Invalid API key format"

**Configuration Methods:**

**Method 1: Configuration File**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-actual-api-key-here"
  }
}
```

**Method 2: Environment Variables**
```bash
# Windows Command Prompt
set OPENAI_API_KEY=sk-your-api-key-here

# Windows PowerShell
$env:OPENAI_API_KEY="sk-your-api-key-here"

# Linux/Mac
export OPENAI_API_KEY="sk-your-api-key-here"

# Make permanent (Linux/Mac)
echo 'export OPENAI_API_KEY="sk-your-api-key-here"' >> ~/.bashrc
source ~/.bashrc
```

**Method 3: User Secrets (Development)**
```bash
# Set using config command
na config update "AIService.ApiKey" "sk-your-api-key-here"

# Verify (shows ***masked***)
na config secrets
```

**Validation:**
```bash
# Check if API key is configured
na config view | grep ApiKey

# Should show: "ApiKey": "***masked***"
```

**Troubleshooting:**
```bash
# Display actual values (for debugging)
na config display-secrets

# Test with explicit config
na video-notes -p "test.mp4" --config config.json --debug
```

### Issue 5: Path Configuration

**Symptoms:**
- "Vault root not found"
- "Invalid path"
- "Access denied to path"

**Windows Path Issues:**

**Problem: Backslash Escaping**
```json
// ❌ WRONG
"NotebookVaultFullpathRoot": "C:\My Vault\Notes"

// ✅ CORRECT (escaped)
"NotebookVaultFullpathRoot": "C:\\My Vault\\Notes"

// ✅ ALSO CORRECT (forward slashes work on Windows)
"NotebookVaultFullpathRoot": "C:/My Vault/Notes"
```

**Problem: Spaces in Paths**
```json
// Quotes handle spaces automatically
"NotebookVaultFullpathRoot": "C:/Users/My Name/My Vault"
```

**Problem: Long Paths**
```powershell
# Enable long paths in Windows (requires admin)
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

**Linux/Mac Path Issues:**

**Problem: Tilde Expansion**
```json
// ❌ Tilde doesn't expand in config
"NotebookVaultFullpathRoot": "~/Documents/Vault"

// ✅ Use full path
"NotebookVaultFullpathRoot": "/home/username/Documents/Vault"
```

**Problem: Relative Paths**
```json
// ❌ Relative paths can cause issues
"NotebookVaultFullpathRoot": "../vault"

// ✅ Use absolute paths
"NotebookVaultFullpathRoot": "/full/path/to/vault"
```

**Verification:**
```bash
# Test path accessibility
ls -la "$(na config view | grep NotebookVaultFullpathRoot | cut -d'"' -f4)"

# Or on Windows
dir "path from config"
```

### Issue 6: OneDrive Configuration

**Symptoms:**
- "OneDrive authentication failed"
- "Client ID not configured"
- "Invalid tenant ID"

**Required OneDrive Settings:**
```json
{
  "OneDrive": {
    "ClientId": "your-app-client-id",
    "TenantId": "common",  // or specific tenant ID
    "Scopes": [
      "Files.ReadWrite.All",
      "Sites.ReadWrite.All"
    ]
  }
}
```

**Getting OneDrive Credentials:**

1. **Register App in Azure Portal:**
   - Go to https://portal.azure.com
   - Navigate to "App registrations"
   - Create new registration
   - Note the "Application (client) ID"

2. **Configure App:**
   - Add redirect URI: `http://localhost`
   - Enable "Public client flows"
   - Add API permissions: Microsoft Graph

3. **Add to Configuration:**
```json
{
  "OneDrive": {
    "ClientId": "12345678-1234-1234-1234-123456789012",
    "TenantId": "common"
  }
}
```

**Troubleshooting:**
```bash
# Test authentication
na refresh-token --debug

# Check configuration
na config view | grep -A 5 "OneDrive"
```

### Issue 7: AI Service Provider Configuration

**Symptoms:**
- "Unknown AI provider"
- "Provider-specific settings missing"
- "Endpoint not configured"

**OpenAI Configuration:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key",
    "Model": "gpt-3.5-turbo",
    "Endpoint": "https://api.openai.com/v1",  // Optional, uses default
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

**Azure OpenAI Configuration:**
```json
{
  "AIService": {
    "Provider": "Azure",
    "ApiKey": "your-azure-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "your-deployment-name",
    "ApiVersion": "2024-02-01"
  }
}
```

**Anthropic Claude Configuration:**
```json
{
  "AIService": {
    "Provider": "Anthropic",
    "ApiKey": "sk-ant-your-api-key",
    "Model": "claude-3-sonnet-20240229"
  }
}
```

**Validation:**
```bash
# Verify provider is set
na config view | grep Provider

# Test with small file
na pdf-notes -p "test.pdf" --debug
```

### Issue 8: Timeout Configuration

**Symptoms:**
- Frequent timeout errors
- Processing stops prematurely
- Inconsistent failures on large files

**Configure Timeouts:**
```json
{
  "Processing": {
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10,
    "DefaultTimeoutMinutes": 15
  }
}
```

**Guidelines:**
- Short videos (< 30 min): 10-15 minutes
- Long videos (> 60 min): 30-60 minutes
- Small PDFs (< 50 pages): 5-10 minutes
- Large PDFs (> 200 pages): 15-30 minutes

**Command-line Override:**
```bash
# Override for single operation
na video-notes -p "long-lecture.mp4" --timeout 60
```

### Issue 9: Logging Configuration

**Symptoms:**
- Too much logging output
- Not enough diagnostic information
- Log files growing too large

**Configure Logging Levels:**
```json
{
  "Logging": {
    "MinimumLevel": "Information",  // Debug, Information, Warning, Error
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/notebook-automation-.log",
          "rollingInterval": "Day",
          "fileSizeLimitBytes": 10485760,  // 10 MB
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
  }
}
```

**Logging Levels:**
- **Debug:** Very detailed, use for troubleshooting
- **Information:** Normal operational messages
- **Warning:** Potential issues
- **Error:** Failures and errors only

**Temporary Debug Mode:**
```bash
# Enable debug without changing config
na pdf-notes -p "file.pdf" --debug
```

## Configuration Validation

### Validate Configuration File

**Check Syntax:**
```bash
# Validate JSON syntax
python -c "import json; json.load(open('config.json'))" && echo "Valid JSON" || echo "Invalid JSON"

# Or using jq (if installed)
jq empty config.json && echo "Valid" || echo "Invalid"
```

**View Current Configuration:**
```bash
# View all settings
na config view

# View specific section
na config view | grep -A 10 "AIService"

# List all available keys
na config list-keys
```

**Test Configuration:**
```bash
# Dry run to test without processing
na pdf-notes -p "test.pdf" --dry-run --verbose

# Debug mode for detailed output
na pdf-notes -p "test.pdf" --debug
```

### Configuration Best Practices

**1. Use Environment Variables for Secrets:**
```bash
# Don't commit API keys to git
# Use environment variables instead
export OPENAI_API_KEY="sk-..."

# Or use .env file (not committed)
echo "OPENAI_API_KEY=sk-..." > .env
source .env
```

**2. Separate Configurations by Environment:**
```
config.development.json
config.staging.json
config.production.json
```

**Usage:**
```bash
na video-notes -p "file.mp4" --config config.production.json
```

**3. Document Custom Settings:**
```json
{
  "_comment": "Configuration for MBA course processing",
  "AIService": {
    "Provider": "OpenAI",
    "_note": "Using gpt-3.5-turbo for cost efficiency"
  }
}
```

**4. Version Control:**
```bash
# Create template without secrets
cp config.json config.template.json

# Remove sensitive data
sed -i 's/"ApiKey": ".*"/"ApiKey": "your-api-key-here"/g' config.template.json

# Commit template, not actual config
git add config.template.json
echo "config.json" >> .gitignore
```

## Troubleshooting Workflow

### Step 1: Verify File Exists and is Valid JSON
```bash
cat config.json | python -m json.tool
```

### Step 2: Check Required Fields
```bash
na config view | grep -E "Provider|ApiKey|NotebookVaultFullpathRoot"
```

### Step 3: Test with Minimal Config
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-test-key"
  }
}
```

### Step 4: Add Settings Incrementally
```bash
# Test after each addition
na pdf-notes -p "test.pdf" --config config.json --dry-run
```

### Step 5: Use Debug Mode
```bash
na pdf-notes -p "test.pdf" --config config.json --debug
```

## Configuration Examples

### Minimal Configuration (OpenAI)
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here"
  }
}
```

### Production Configuration
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.7
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "C:/Users/MyName/Obsidian/Vault"
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 4000,
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10
  },
  "OneDrive": {
    "ClientId": "your-client-id",
    "TenantId": "common"
  },
  "Logging": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/notebook-automation-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### High-Performance Configuration
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 500,
    "Temperature": 0.3
  },
  "Processing": {
    "ChunkSize": 2000,
    "GenerateSummaries": false,
    "VideoProcessingTimeoutMinutes": 60
  },
  "Logging": {
    "MinimumLevel": "Warning"
  }
}
```

## Getting Help

**If configuration issues persist:**

1. **Validate JSON syntax** with online validator
2. **Check logs** for specific error messages
3. **Use debug mode** for detailed diagnostics
4. **Test with minimal config** to isolate issues
5. **Compare with examples** in documentation
6. **File issue on GitHub** with configuration (remove secrets!)

**Related Documentation:**
- [Configuration Guide](../configuration/ai-services.md)
- [Common Issues](common-issues.md)
- [CLI Reference](../cli-reference.md)
- [User Guide](../user-guide/index.md)
