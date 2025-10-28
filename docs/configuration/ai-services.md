# AI Services Configuration

Configure AI service providers for content summarization and analysis in Notebook Automation.

## Overview

Notebook Automation supports multiple AI service providers for generating summaries, extracting insights, and enhancing content. This guide covers setup and configuration for each supported provider.

## Supported AI Providers

### OpenAI

**Models Supported:**
- GPT-4 (most capable, slower, more expensive)
- GPT-3.5-turbo (fast, cost-effective, recommended for most users)
- GPT-4-turbo (balanced performance and cost)

**Configuration:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000,
    "Temperature": 0.7,
    "Endpoint": "https://api.openai.com/v1"
  }
}
```

**Getting an API Key:**
1. Sign up at https://platform.openai.com/
2. Navigate to API keys section
3. Create new secret key
4. Copy key (shown only once!)
5. Add to configuration

**Pricing (as of 2025):**
- GPT-3.5-turbo: ~$0.002 per 1K tokens
- GPT-4: ~$0.03 per 1K tokens
- GPT-4-turbo: ~$0.01 per 1K tokens

### Azure OpenAI

**Configuration:**
```json
{
  "AIService": {
    "Provider": "Azure",
    "ApiKey": "your-azure-api-key",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "your-deployment-name",
    "ApiVersion": "2024-02-01",
    "Model": "gpt-35-turbo"
  }
}
```

**Setup Steps:**
1. Create Azure OpenAI resource in Azure Portal
2. Deploy a model (e.g., gpt-35-turbo)
3. Get endpoint URL and API key from Azure Portal
4. Note your deployment name
5. Add to configuration

**Advantages:**
- Enterprise security and compliance
- Private endpoint options
- SLA guarantees
- Integration with Azure services

### Anthropic Claude

**Configuration:**
```json
{
  "AIService": {
    "Provider": "Anthropic",
    "ApiKey": "sk-ant-your-api-key-here",
    "Model": "claude-3-sonnet-20240229",
    "MaxTokens": 1000,
    "Temperature": 0.7
  }
}
```

**Models:**
- claude-3-opus (most capable)
- claude-3-sonnet (balanced)
- claude-3-haiku (fast and economical)

**Getting an API Key:**
1. Sign up at https://console.anthropic.com/
2. Navigate to API keys
3. Create new key
4. Add to configuration

## Configuration Parameters

### Required Parameters

**Provider:**
- **Type:** String
- **Options:** "OpenAI", "Azure", "Anthropic"
- **Description:** AI service provider to use

**ApiKey:**
- **Type:** String
- **Format:** Provider-specific
- **Description:** Authentication key for API access
- **Security:** Never commit to version control!

### Model Selection

**Model:**
- **Type:** String
- **Default:** Provider-specific
- **Description:** Specific AI model to use

**Model Selection Guide:**

| Use Case | Recommended Model | Reason |
|----------|------------------|---------|
| Large batch processing | gpt-3.5-turbo | Fast, cost-effective |
| Important academic papers | gpt-4 | Most accurate summaries |
| General course materials | gpt-3.5-turbo | Good balance |
| Quick content extraction | claude-3-haiku | Very fast |
| Deep analysis | claude-3-opus | Best reasoning |

### Response Configuration

**MaxTokens:**
- **Type:** Integer
- **Default:** 1000
- **Range:** 100-4000 (depends on model)
- **Description:** Maximum length of AI response

**Guidelines:**
- Short summaries: 500 tokens
- Standard summaries: 1000 tokens
- Detailed analysis: 2000 tokens
- Comprehensive summaries: 4000 tokens

**Temperature:**
- **Type:** Float
- **Default:** 0.7
- **Range:** 0.0-2.0
- **Description:** Controls randomness in responses

**Guidelines:**
- Factual summaries: 0.3-0.5 (more deterministic)
- Creative writing: 0.7-0.9 (more varied)
- Consistent output: 0.0-0.3 (most consistent)

### Optional Parameters

**Endpoint:**
- **Type:** String
- **Default:** Provider default
- **Description:** Custom API endpoint (for Azure or self-hosted)

**TopP:**
- **Type:** Float
- **Default:** 1.0
- **Range:** 0.0-1.0
- **Description:** Alternative to temperature for controlling randomness

**FrequencyPenalty:**
- **Type:** Float
- **Default:** 0.0
- **Range:** -2.0-2.0
- **Description:** Penalizes word repetition

**PresencePenalty:**
- **Type:** Float
- **Default:** 0.0
- **Range:** -2.0-2.0
- **Description:** Encourages topic diversity

## Configuration Examples

### Development Configuration (Fast and Cheap)
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-key",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 500,
    "Temperature": 0.3
  }
}
```

### Production Configuration (High Quality)
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-key",
    "Model": "gpt-4",
    "MaxTokens": 2000,
    "Temperature": 0.5,
    "TopP": 0.9
  }
}
```

### Enterprise Configuration (Azure)
```json
{
  "AIService": {
    "Provider": "Azure",
    "ApiKey": "azure-key",
    "Endpoint": "https://mycompany.openai.azure.com/",
    "DeploymentName": "gpt-4-deployment",
    "ApiVersion": "2024-02-01",
    "Model": "gpt-4",
    "MaxTokens": 1500,
    "Temperature": 0.7
  }
}
```

### Cost-Optimized Configuration
```json
{
  "AIService": {
    "Provider": "Anthropic",
    "ApiKey": "sk-ant-your-key",
    "Model": "claude-3-haiku-20240307",
    "MaxTokens": 500,
    "Temperature": 0.3
  }
}
```

## Environment-Specific Configuration

### Using Environment Variables

**Configuration file:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo"
  }
}
```

**Set API key via environment:**
```bash
# Windows
set OPENAI_API_KEY=sk-your-key

# Linux/Mac
export OPENAI_API_KEY=sk-your-key

# Make permanent (Linux/Mac)
echo 'export OPENAI_API_KEY=sk-your-key' >> ~/.bashrc
```

### Multiple Environments

**Structure:**
```
config/
├── config.development.json
├── config.staging.json
└── config.production.json
```

**Usage:**
```bash
# Development
na pdf-notes -p "file.pdf" --config config.development.json

# Production
na pdf-notes -p "file.pdf" --config config.production.json
```

## Security Best Practices

### Never Commit API Keys

**Use .gitignore:**
```bash
# Add to .gitignore
echo "config.json" >> .gitignore
echo ".env" >> .gitignore

# Create template without keys
cp config.json config.template.json
# Manually remove API keys from template
```

### Use Environment Variables

**Create .env file (not committed):**
```bash
OPENAI_API_KEY=sk-your-actual-key
AZURE_OPENAI_KEY=your-azure-key
```

**Load in scripts:**
```bash
# Linux/Mac
source .env

# Windows PowerShell
Get-Content .env | ForEach-Object {
  $var = $_.Split('=')
  [Environment]::SetEnvironmentVariable($var[0], $var[1])
}
```

### Use User Secrets (Development)

```bash
# Store secrets securely
na config update "AIService.ApiKey" "sk-your-key"

# View masked
na config secrets

# Delete when done
na config update "AIService.ApiKey" ""
```

### Rotate Keys Regularly

- Change API keys every 90 days
- Immediately rotate if compromised
- Use different keys for dev/prod
- Monitor usage for anomalies

## Testing Configuration

### Validate Configuration

```bash
# View current configuration (API keys masked)
na config view

# Test with small file
na pdf-notes -p "test.pdf" --dry-run --debug

# Check API connectivity
na pdf-notes -p "small-test.pdf" --verbose
```

### Troubleshoot Issues

**API Key Not Found:**
```bash
# Check configuration
na config view | grep ApiKey

# Should show masked value
# "ApiKey": "***masked***"
```

**Authentication Failed:**
```bash
# Verify key is valid
# Check provider's dashboard
# Ensure key has correct permissions
```

**Model Not Available:**
```bash
# Verify model name
# Check provider's model list
# Ensure deployment exists (Azure)
```

## Cost Management

### Monitor Usage

**Track API Calls:**
- Check provider's usage dashboard
- Set up billing alerts
- Monitor per-project usage

**Estimate Costs:**
```
Average cost per file:
- Small PDF (10 pages): $0.001-0.003
- Large PDF (100 pages): $0.01-0.03
- Short video (30 min): $0.02-0.05
- Long video (120 min): $0.10-0.20
```

### Optimize Costs

**1. Use Appropriate Model:**
```json
// For bulk processing
"Model": "gpt-3.5-turbo"  // 10x cheaper than gpt-4

// For important content only
"Model": "gpt-4"
```

**2. Limit Token Usage:**
```json
"MaxTokens": 500  // Shorter summaries = lower cost
```

**3. Disable AI When Not Needed:**
```bash
# Extract content without AI
na pdf-notes -p "documents" --no-summary
```

**4. Batch Processing:**
```bash
# Process in batches to monitor costs
na pdf-notes -p "batch-01" --verbose
# Check costs before continuing
na pdf-notes -p "batch-02" --verbose
```

### Cost-Saving Strategies

**Two-Pass Approach:**
```bash
# Pass 1: Extract all content (no AI)
na pdf-notes -p "all-files" --no-summary

# Pass 2: Add AI to important files only
na pdf-notes -p "critical-file.pdf" --force
```

**Selective Processing:**
```bash
# Identify files that need AI
# Process only those with AI enabled
# Leave others with base content only
```

## Advanced Configuration

### Custom Prompts

**Configure prompt templates:**
```json
{
  "AIService": {
    "CustomPrompts": {
      "SummaryPrompt": "Provide a concise summary focusing on key concepts...",
      "QuestionPrompt": "Generate study questions based on this content..."
    }
  }
}
```

### Timeout Configuration

```json
{
  "AIService": {
    "TimeoutSeconds": 120,
    "RetryAttempts": 3,
    "RetryDelaySeconds": 5
  }
}
```

### Rate Limiting

```json
{
  "AIService": {
    "MaxRequestsPerMinute": 60,
    "MaxConcurrentRequests": 3
  }
}
```

## Troubleshooting

### Common Issues

**Issue:** "API key not configured"
**Solution:** Add API key to configuration or environment variable

**Issue:** "Rate limit exceeded"
**Solution:** Reduce processing speed or upgrade API tier

**Issue:** "Model not found"
**Solution:** Verify model name and availability

**Issue:** "Timeout during API call"
**Solution:** Increase timeout or check network connectivity

### Debug Mode

```bash
# Enable debug logging
na pdf-notes -p "file.pdf" --debug

# Check logs for API-related errors
tail -f logs/notebook-automation.log | grep "API"
```

## Related Documentation

- [Configuration Problems](../troubleshooting/configuration-problems.md)
- [Common Issues](../troubleshooting/common-issues.md)
- [Performance Tuning](../user-guide/performance-tuning.md)
- [Getting Started](../getting-started/quick-start.md)
