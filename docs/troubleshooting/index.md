# Troubleshooting

Common issues and solutions for Notebook Automation.

## Quick Diagnostic Steps

If you're experiencing issues, start with these diagnostic steps:

1. **Check version and installation**:

   ```bash
   na.exe --version
   ```

2. **Validate configuration**:

   ```bash
   na.exe config validate
   ```

3. **Test with verbose logging**:

   ```bash
   na.exe process "test-file.md" --verbose
   ```

4. **Check log files** in your configured log directory

## Common Issues

### Installation and Setup

#### Issue: "Command not found" or "na.exe is not recognized"

**Symptoms:**

- Error when running `na.exe` commands
- "Command not found" on Linux/macOS
- "is not recognized as an internal or external command" on Windows

**Solutions:**

1. **Verify installation location**:

   ```bash
   # Check if file exists
   ls -la na.exe  # Linux/macOS
   dir na.exe     # Windows
   ```

2. **Add to PATH** (if using portable installation):

   ```bash
   # Windows (PowerShell)
   $env:PATH += ";C:\path\to\notebook-automation"

   # Linux/macOS
   export PATH="$PATH:/path/to/notebook-automation"
   ```

3. **Use full path**:

   ```bash
   /full/path/to/na.exe process "document.md"
   ```

4. **Check permissions** (Linux/macOS):

   ```bash
   chmod +x na
   ```

#### Issue: ".NET runtime not found"

**Symptoms:**

- "The framework 'Microsoft.NETCore.App', version 'X.X.X' was not found"
- Application fails to start

**Solutions:**

1. **Install .NET Runtime**:
   - Download from [Microsoft .NET](https://dotnet.microsoft.com/download)
   - Install the version specified in the error message

2. **Use self-contained deployment**:
   - Download the self-contained release (includes runtime)
   - No separate .NET installation required

### Configuration Issues

#### Issue: "Configuration file not found"

**Symptoms:**

- "Could not find configuration file" error
- Default configuration not loading

**Solutions:**

1. **Initialize configuration**:

   ```bash
   na.exe config init
   ```

2. **Specify configuration path**:

   ```bash
   na.exe process "file.md" --config "/path/to/config.json"
   ```

3. **Check working directory**:

   ```bash
   # Ensure you're in the correct directory
   pwd  # Linux/macOS
   Get-Location  # Windows PowerShell
   ```

#### Issue: "Invalid configuration format"

**Symptoms:**

- JSON parsing errors
- "Unexpected character" errors
- Configuration validation failures

**Solutions:**

1. **Validate JSON syntax**:
   - Use online JSON validators
   - Check for missing commas, brackets, or quotes

2. **Reset to defaults**:

   ```bash
   na.exe config init --force
   ```

3. **Use configuration wizard**:

   ```bash
   na.exe config wizard
   ```

### AI Service Issues

#### Issue: "API key not found" or "Unauthorized"

**Symptoms:**

- HTTP 401 Unauthorized errors
- "API key not configured" messages
- Authentication failures

**Solutions:**

1. **Verify API key**:

   ```bash
   na.exe config show
   # Check if API key is set and not empty
   ```

2. **Set API key**:

   ```bash
   na.exe config set "AIService.OpenAI.ApiKey" "your-key-here"
   ```

3. **Use environment variables**:

   ```bash
   export OPENAI_API_KEY="your-key-here"
   na.exe process "file.md"
   ```

4. **Check key validity**:
   - Test key with AI service directly
   - Verify key hasn't expired
   - Check account status and billing

#### Issue: "Rate limit exceeded"

**Symptoms:**

- HTTP 429 errors
- "Too many requests" messages
- Processing stops or slows significantly

**Solutions:**

1. **Reduce rate limit**:

   ```bash
   na.exe config set "AIService.RateLimit.RequestsPerMinute" "30"
   ```

2. **Increase delay between requests**:

   ```bash
   na.exe config set "AIService.RateLimit.DelayBetweenRequests" "2000"
   ```

3. **Process smaller batches**:

   ```bash
   # Process files in smaller groups
   na.exe process "batch1/" --output "results1/"
   na.exe process "batch2/" --output "results2/"
   ```

4. **Upgrade API plan** (if applicable)

#### Issue: "Model not available" or "Deployment not found"

**Symptoms:**

- "Model 'gpt-4' not found" errors
- Azure OpenAI deployment errors
- Service-specific model errors

**Solutions:**

1. **Check available models**:

   ```bash
   na.exe models list --provider OpenAI
   ```

2. **Use available model**:

   ```bash
   na.exe config set "AIService.OpenAI.Model" "gpt-3.5-turbo"
   ```

3. **For Azure OpenAI, check deployment name**:

   ```bash
   na.exe config set "AIService.AzureOpenAI.DeploymentName" "your-deployment"
   ```

### Processing Issues

#### Issue: Files not being processed

**Symptoms:**

- No output generated
- "No files found" messages
- Empty results

**Solutions:**

1. **Check file patterns**:

   ```bash
   # Verify files exist
   na.exe process "docs/" --dry-run --verbose
   ```

2. **Review include/exclude filters**:

   ```bash
   # Remove filters temporarily
   na.exe process "docs/" --include "*" --recursive
   ```

3. **Check file permissions**:

   ```bash
   # Ensure files are readable
   ls -la docs/  # Linux/macOS
   ```

4. **Verify file formats**:
   - Ensure files have supported extensions
   - Check file encoding (UTF-8 recommended)

#### Issue: Poor quality output or extraction

**Symptoms:**

- Incomplete metadata extraction
- Poor quality summaries
- Missing information

**Solutions:**

1. **Increase analysis level**:

   ```bash
   na.exe config set "Processing.AnalysisLevel" "deep"
   ```

2. **Use better AI model**:

   ```bash
   na.exe config set "AIService.OpenAI.Model" "gpt-4"
   ```

3. **Adjust content structure**:
   - Ensure documents have clear headings
   - Use consistent formatting
   - Include relevant metadata in frontmatter

4. **Review templates**:
   - Customize extraction templates
   - Adjust prompt engineering

#### Issue: Processing is very slow

**Symptoms:**

- Long processing times
- High memory usage
- Unresponsive application

**Solutions:**

1. **Optimize rate limiting**:

   ```bash
   na.exe config set "AIService.RateLimit.RequestsPerMinute" "60"
   ```

2. **Use faster AI model**:

   ```bash
   na.exe config set "AIService.OpenAI.Model" "gpt-3.5-turbo"
   ```

3. **Reduce analysis depth**:

   ```bash
   na.exe config set "Processing.AnalysisLevel" "basic"
   ```

4. **Process smaller batches**:

   ```bash
   # Process in chunks
   na.exe process "docs/batch1/" --output "results/batch1/"
   ```

5. **Monitor system resources**:
   - Check available memory
   - Monitor CPU usage
   - Ensure sufficient disk space

### Output Issues

#### Issue: Output files not created

**Symptoms:**

- No files in output directory
- "Permission denied" errors
- "Directory not found" errors

**Solutions:**

1. **Check output directory permissions**:

   ```bash
   # Ensure directory is writable
   ls -ld output/  # Linux/macOS
   ```

2. **Create output directory**:

   ```bash
   mkdir -p output/
   ```

3. **Use absolute paths**:

   ```bash
   na.exe process "docs/" --output "/full/path/to/output/"
   ```

#### Issue: Corrupted or invalid output

**Symptoms:**

- Malformed JSON/YAML
- Truncated files
- Invalid characters

**Solutions:**

1. **Check disk space**:

   ```bash
   df -h  # Linux/macOS
   ```

2. **Validate output format**:

   ```bash
   na.exe config set "Processing.OutputFormat" "json"
   ```

3. **Enable output validation**:

   ```bash
   na.exe process "docs/" --validate-output
   ```

## Performance Troubleshooting

### Memory Issues

**Symptoms:**

- "Out of memory" errors
- Application crashes
- System becomes unresponsive

**Solutions:**

1. **Process smaller batches**
2. **Reduce analysis level** to `basic`
3. **Increase system virtual memory**
4. **Monitor memory usage** with system tools

### Network Issues

**Symptoms:**

- Timeout errors
- Connection failures
- Intermittent API errors

**Solutions:**

1. **Check internet connectivity**
2. **Configure proxy settings** if behind corporate firewall
3. **Increase timeout values**:

   ```bash
   na.exe config set "AIService.TimeoutSeconds" "120"
   ```

## Getting Help

### Collecting Diagnostic Information

When reporting issues, include:

1. **Version information**:

   ```bash
   na.exe --version
   ```

2. **Configuration** (remove sensitive information):

   ```bash
   na.exe config show
   ```

3. **Error logs** from the log directory

4. **Command that failed** with `--verbose` flag

5. **System information**:
   - Operating system and version
   - .NET version
   - Available memory and disk space

### Log Analysis

Enable detailed logging for troubleshooting:

```bash
# Set debug logging
na.exe config set "Logging.Level" "Debug"

# Process with verbose output
na.exe process "problem-file.md" --verbose
```

**Log files location:**

- Default: `./logs/` directory
- Check configuration: `na.exe config show | grep LogDirectory`

### Community Support

- **GitHub Issues**: [Report bugs and issues](https://github.com/your-repo/notebook-automation/issues)
- **Discussions**: [Community discussions](https://github.com/your-repo/notebook-automation/discussions)
- **Documentation**: [Official documentation](../index.md)

### Professional Support

For enterprise support needs:

- Review the [Developer Guide](../developer-guide/index.md) for customization options
- Consider forking the project for custom modifications
- Contact maintainers for consulting services

## Prevention Tips

### Regular Maintenance

1. **Keep software updated**
2. **Monitor log files** for warnings
3. **Validate configuration** periodically
4. **Test with sample files** before large batches
5. **Backup configuration files**

### Best Practices

1. **Use version control** for configuration files
2. **Document custom configurations**
3. **Test changes** in development environments
4. **Monitor API usage** and costs
5. **Implement proper error handling** in scripts
