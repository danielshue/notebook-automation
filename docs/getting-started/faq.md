# Frequently Asked Questions

Common questions and answers about Notebook Automation.

## General Questions

### What file formats are supported?

Notebook Automation supports:

- **Markdown files** (.md, .markdown)
- **HTML files** (.html, .htm)
- **Text files** (.txt)
- **Rich text files** (.rtf)

### What AI services can I use?

Currently supported AI providers:

- **OpenAI** (GPT-3.5, GPT-4, GPT-4o)
- **Azure OpenAI** (same models via Azure)
- **IBM Foundry** (various models)

### How much does it cost to run?

Costs depend on your AI service usage:

- **OpenAI**: Pay per token (input/output)
- **Azure OpenAI**: Similar to OpenAI with Azure pricing
- **IBM Foundry**: Based on your IBM Cloud plan

Typical costs for processing academic notebooks range from $0.01-$0.10 per document.

## Installation and Setup

### Do I need to install .NET separately?

For **pre-built binaries**: No, they include the .NET runtime.

For **building from source**: Yes, you need the .NET 9.0 SDK.

### Can I run this on macOS/Linux?

Yes! Notebook Automation is cross-platform and runs on:

- Windows (x64, ARM64)
- macOS (Intel, Apple Silicon)
- Linux (x64, ARM64)

### Where should I store my configuration files?

Common locations:

- **Project-specific**: In your project directory
- **User-wide**: `~/.config/notebook-automation/` (Linux/macOS) or `%APPDATA%\\NotebookAutomation\\` (Windows)
- **Portable**: Alongside the executable

## Usage Questions

### How do I process large directories efficiently?

Best practices for large-scale processing:

1. **Use batch processing** with smaller directories
2. **Configure rate limits** for AI services
3. **Enable progress reporting** with `--verbose`
4. **Monitor memory usage** and restart if needed

Example:

```powershell
# Process subdirectories separately
.\na.exe process "notes/week1/" --output "results/week1/"
.\na.exe process "notes/week2/" --output "results/week2/"
```

### Can I customize the output format?

Yes, through configuration:

- **Metadata format**: JSON, YAML, or XML
- **Summary style**: Academic, technical, or casual
- **Output structure**: Flat or hierarchical

### How do I handle API rate limits?

Configure rate limiting in your `config.json`:

```json
{
  "AIService": {
    "RateLimit": {
      "RequestsPerMinute": 50,
      "DelayBetweenRequests": 1000
    }
  }
}
```

## Troubleshooting

### Why am I getting "API key not found" errors?

Check these steps:

1. **Verify API key** is set in configuration
2. **Check environment variables** if using them
3. **Validate configuration** with `.\na.exe config validate`
4. **Test connectivity** to the AI service

### The tool is running slowly. How can I speed it up?

Performance optimization tips:

1. **Reduce AI model complexity** (use faster models)
2. **Increase rate limits** if your plan allows
3. **Process smaller batches**
4. **Use SSD storage** for temporary files
5. **Close unnecessary applications**

### Some files are being skipped. Why?

Common reasons files are skipped:

- **Unsupported format**: Check the supported file types
- **File access issues**: Ensure read permissions
- **Size limits**: Very large files may be skipped
- **Encoding issues**: Non-UTF8 files may cause problems

Check the logs for specific error messages.

## Configuration Questions

### How do I switch between AI providers?

Update your configuration file:

```json
{
  "AIService": {
    "Provider": "OpenAI",  // or "AzureOpenAI", "Foundry"
    "OpenAI": {
      "ApiKey": "your-key-here"
    }
  }
}
```

### Can I use multiple configurations?

Yes! Use the `--config` parameter:

```powershell
.\na.exe process "docs/" --config "openai-config.json"
.\na.exe process "docs/" --config "azure-config.json"
```

### How do I secure my API keys?

Security best practices:

1. **Use environment variables** instead of config files
2. **Use Azure Key Vault** or similar secret management
3. **Restrict file permissions** on configuration files
4. **Don't commit** API keys to version control

## Advanced Usage

### Can I integrate this into CI/CD pipelines?

Yes! Example GitHub Actions workflow:

```yaml
- name: Process Documentation
  run: |
    .\na.exe process "docs/" --config "ci-config.json" --output "processed-docs/"
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
```

### How do I customize the processing templates?

Edit the template files in your configuration directory:

- **Metadata extraction**: Modify `metadata-template.yaml`
- **Summary generation**: Edit `summary-template.md`
- **Output formatting**: Customize `output-template.json`

### Can I extend the tool with custom processors?

Currently, the tool doesn't support plugins, but you can:

1. **Fork the repository** and add custom processors
2. **Use the API** to build custom integrations
3. **Process outputs** with additional tools

## Getting Help

### Where can I report bugs?

- **GitHub Issues**: [Project Issues Page](https://github.com/your-repo/notebook-automation/issues)
- **Discussions**: [GitHub Discussions](https://github.com/your-repo/notebook-automation/discussions)

### How do I request new features?

1. **Search existing issues** to avoid duplicates
2. **Create a feature request** with detailed requirements
3. **Provide use cases** and examples
4. **Consider contributing** if you can implement it

### Is commercial support available?

Currently, support is community-based through:

- GitHub Issues and Discussions
- Documentation and guides
- Community contributions

For enterprise needs, consider forking the project or contacting the maintainers.

## Still Need Help?

If your question isn't answered here:

1. Check the [Troubleshooting Guide](../troubleshooting/index.md)
2. Browse the [User Guide](../user-guide/index.md)
3. Review the [API Documentation](../api/index.md)
4. Search or create an issue on GitHub
