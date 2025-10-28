# Common Issues

Quick reference for troubleshooting common problems with Notebook Automation.

## Installation Issues

### CLI Not Found After Installation

**Symptom:** Command `na` not recognized

**Possible Causes:**
- CLI not in system PATH
- Installation directory incorrect
- Permissions issues

**Solutions:**

**Windows:**
```powershell
# Use full path
.\na.exe --version

# Or add to PATH
$env:Path += ";C:\path\to\notebook-automation"

# Verify
na --version
```

**Linux/Mac:**
```bash
# Use full path
./na --version

# Or add to PATH
export PATH=$PATH:/path/to/notebook-automation

# Make executable
chmod +x na

# Verify
na --version
```

### Executable Permission Denied (Linux/Mac)

**Symptom:** "Permission denied" error when running `na`

**Solution:**
```bash
chmod +x na
./na --version
```

### Missing .NET Runtime

**Symptom:** "You must install .NET to run this application"

**Solution:**
```bash
# Install .NET 9.0 Runtime
# Windows: Download from https://dot.net
# Linux: Use package manager
sudo apt-get install dotnet-runtime-9.0

# Mac: Use Homebrew
brew install dotnet
```

## Configuration Issues

### Configuration File Not Found

**Symptom:** "Configuration file not found" error

**Possible Locations:**
- Current directory: `./config.json`
- Config directory: `./config/appsettings.json`
- User directory: `~/.notebook-automation/config.json`

**Solutions:**
```bash
# Specify config file explicitly
na video-notes -p "file.mp4" --config "path/to/config.json"

# Create config file
cat > config.json << EOF
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key"
  }
}
EOF
```

### Invalid Configuration Format

**Symptom:** "Failed to parse configuration" error

**Common Causes:**
- Invalid JSON syntax
- Missing quotes
- Trailing commas
- Wrong data types

**Solution:**
```json
// ❌ WRONG - Trailing comma
{
  "AIService": {
    "Provider": "OpenAI",
  }
}

// ✅ CORRECT
{
  "AIService": {
    "Provider": "OpenAI"
  }
}
```

**Validation:**
```bash
# Use JSON validator
cat config.json | python -m json.tool

# Or online validator: https://jsonlint.com/
```

### Missing API Keys

**Symptom:** "API key not configured" or "Unauthorized" errors

**Solutions:**

**Option 1: Configuration File**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "sk-your-api-key-here"
  }
}
```

**Option 2: Environment Variables**
```bash
# Windows
set OPENAI_API_KEY=sk-your-api-key-here

# Linux/Mac
export OPENAI_API_KEY=sk-your-api-key-here

# Then run command
na video-notes -p "video.mp4"
```

**Option 3: User Secrets (Development)**
```bash
# Set using config command
na config update "AIService.ApiKey" "sk-your-api-key-here"
```

## Processing Issues

### File Not Found

**Symptom:** "File or directory not found" error

**Causes:**
- Incorrect path
- File moved or deleted
- Permission issues

**Solutions:**
```bash
# Use absolute paths
na pdf-notes -p "C:\full\path\to\document.pdf"

# Verify file exists
ls "path/to/file.pdf"  # Linux/Mac
dir "path\to\file.pdf" # Windows

# Check permissions
# Linux/Mac
ls -l "path/to/file.pdf"
# Windows
icacls "path\to\file.pdf"
```

### Processing Timeout

**Symptom:** "Operation timed out" error

**Causes:**
- File too large
- Slow AI API response
- Network issues
- Default timeout too short

**Solutions:**
```bash
# Increase timeout
na video-notes -p "long-video.mp4" --timeout 60

# Or in configuration
{
  "Processing": {
    "VideoProcessingTimeoutMinutes": 60,
    "PdfProcessingTimeoutMinutes": 30
  }
}
```

### AI Summary Generation Failed

**Symptom:** Content processed but no AI summary

**Causes:**
- API rate limiting
- Invalid API key
- Network connectivity
- AI service down

**Solutions:**
```bash
# Check API key
na config view

# Try without AI summary first
na pdf-notes -p "document.pdf" --no-summary

# Check API status
# OpenAI: https://status.openai.com/
# Azure: https://status.azure.com/

# Retry with AI
na pdf-notes -p "document.pdf" --force
```

### Corrupted Output Files

**Symptom:** Generated markdown files are malformed

**Causes:**
- Processing interrupted
- Disk space issues
- Encoding problems

**Solutions:**
```bash
# Delete corrupted file and retry
rm "output-file-pdf.md"
na pdf-notes -p "input.pdf" --force

# Check disk space
df -h  # Linux/Mac
wmic logicaldisk get size,freespace,caption  # Windows

# Use debug mode to identify issue
na pdf-notes -p "input.pdf" --debug
```

## OneDrive Integration Issues

### Authentication Failed

**Symptom:** "OneDrive authentication failed" error

**Solutions:**
```bash
# Refresh authentication token
na refresh-token

# This opens browser for authentication
# Follow prompts to sign in

# Verify authentication
na vault vault-sync "vault" --dry-run
```

### Sync Failures

**Symptom:** Vault sync fails or hangs

**Causes:**
- Network connectivity
- OneDrive permissions
- Token expired

**Solutions:**
```bash
# Refresh token first
na refresh-token

# Try sync with verbose output
na vault vault-sync "vault" --verbose

# Use dry-run to test
na vault vault-sync "vault" --dry-run
```

### Share Link Creation Failed

**Symptom:** "Failed to create share link" error

**Causes:**
- Insufficient OneDrive permissions
- File not in OneDrive
- Organization policy restrictions

**Solutions:**
```bash
# Skip share link creation
na video-notes -p "video.mp4" --no-share-links

# Check file is in OneDrive
# Verify file path includes OneDrive folder

# Contact IT if organization restricts sharing
```

## Output Issues

### No Output Generated

**Symptom:** Command completes but no markdown files created

**Causes:**
- Files already exist (and have AI content)
- Output directory permissions
- Incorrect output path

**Solutions:**
```bash
# Force overwrite
na pdf-notes -p "document.pdf" --force

# Check output directory
ls -la output-directory/

# Use verbose mode to see what's happening
na pdf-notes -p "document.pdf" --verbose

# Specify output directory explicitly
na pdf-notes -p "input.pdf" --overwrite-output-dir "output"
```

### Frontmatter Errors

**Symptom:** "Invalid frontmatter" or YAML parsing errors

**Causes:**
- Special characters in metadata
- Unescaped quotes
- Malformed YAML

**Solutions:**
```bash
# Check frontmatter
head -20 output-file.md

# Fix with tag commands
na tag diagnose-yaml "output-file.md"

# Update frontmatter
na tag update-frontmatter "file.md" "field" "value"
```

### Images Not Extracted

**Symptom:** PDF processed but images missing

**Causes:**
- Forgot `--extract-images` flag
- PDF has no extractable images
- Images are embedded differently

**Solutions:**
```bash
# Use extract-images flag
na pdf-notes -p "document.pdf" --extract-images

# Check if PDF has images
# Open PDF and verify images are present

# Some PDFs have images as part of page scans
# These may not be extractable
```

## Tag Management Issues

### Duplicate Tags

**Symptom:** Same tag appears multiple times in files

**Solution:**
```bash
# Consolidate duplicate tags
na tag consolidate "vault/directory"

# Or single file
na tag consolidate "vault/file.md"
```

### Inconsistent Tag Hierarchy

**Symptom:** Tags not following hierarchy pattern

**Solution:**
```bash
# Add nested tags based on structure
na tag add-nested "vault/directory"

# Check metadata consistency
na tag metadata-check "vault" --verbose
```

### Tag Update Failures

**Symptom:** Tags not updating as expected

**Solutions:**
```bash
# Use debug mode
na tag update-frontmatter "file.md" "field" "value" --debug

# Check file permissions
chmod 644 "file.md"  # Linux/Mac

# Ensure file is not open in editor
```

## Performance Issues

### Slow Processing

**Symptom:** Processing takes much longer than expected

**Causes:**
- Large files
- Slow AI API
- Insufficient resources
- Network latency

**Solutions:**
```bash
# Process without AI for faster base processing
na pdf-notes -p "large-file.pdf" --no-summary

# Increase timeout
na video-notes -p "video.mp4" --timeout 120

# Process smaller batches
na pdf-notes -p "batch-01" --verbose
na pdf-notes -p "batch-02" --verbose

# See: Performance Tuning guide for more details
```

### High Memory Usage

**Symptom:** System running out of memory

**Solutions:**
```bash
# Process smaller batches
# Close other applications
# Increase system RAM if possible

# Use --no-summary to reduce memory
na pdf-notes -p "documents" --no-summary
```

### Rate Limiting

**Symptom:** "Rate limit exceeded" errors from AI service

**Solutions:**
```bash
# Wait and retry
sleep 60
na pdf-notes -p "documents" --retry-failed

# Use --no-summary for bulk processing
na pdf-notes -p "documents" --no-summary

# Upgrade API tier if needed
# Or spread processing over time
```

## Platform-Specific Issues

### Windows

**Long Path Issues:**
```powershell
# Enable long paths in Windows
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
-Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

**PowerShell Execution Policy:**
```powershell
# If scripts are blocked
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Linux

**Dependency Issues:**
```bash
# Install required dependencies
sudo apt-get update
sudo apt-get install -y dotnet-runtime-9.0 libicu-dev
```

### macOS

**Gatekeeper Issues:**
```bash
# If "unidentified developer" error
xattr -d com.apple.quarantine na

# Or allow in System Preferences
# System Preferences > Security & Privacy > General
```

## Error Messages Reference

### "Command not found"

**Meaning:** CLI executable not in PATH or not installed

**Fix:** Install CLI or use full path to executable

### "Failed with code null"

**Meaning:** Process crashed unexpectedly, likely corrupted executable

**Fix:** 
```bash
# Redownload CLI
# Or reload plugin if using Obsidian
```

### "Checksum mismatch"

**Meaning:** Downloaded executable doesn't match expected checksum

**Fix:**
```bash
# Delete and redownload
rm na
# Download fresh copy
```

### "API key not found"

**Meaning:** AI service API key not configured

**Fix:** Add API key to configuration or environment variables

### "Access denied"

**Meaning:** Insufficient file or folder permissions

**Fix:**
```bash
# Linux/Mac
chmod 644 file.md
chmod 755 directory/

# Windows
icacls file.md /grant Users:F
```

### "Invalid JSON"

**Meaning:** Configuration file has syntax errors

**Fix:** Validate and correct JSON syntax

## Debug Mode

### Enabling Debug Output

```bash
# Use debug flag for detailed information
na video-notes -p "video.mp4" --debug

# Use verbose for progress information
na pdf-notes -p "documents" --verbose

# Combine both
na video-notes -p "video.mp4" --verbose --debug
```

### Log Files

**Location:**
- Windows: `C:\Users\{user}\AppData\Local\NotebookAutomation\logs\`
- Linux/Mac: `~/.local/share/NotebookAutomation/logs/`

**Review Logs:**
```bash
# View latest log
tail -f logs/notebook-automation.log

# Search for errors
grep ERROR logs/*.log

# View specific date
cat logs/notebook-automation-2025-01-18.log
```

## Getting Additional Help

### Information to Provide

When reporting issues, include:

1. **Command used:**
   ```bash
   na video-notes -p "file.mp4" --verbose
   ```

2. **Error message:**
   ```
   Copy exact error message
   ```

3. **Version information:**
   ```bash
   na --version
   ```

4. **Configuration (sanitized):**
   ```bash
   na config view  # Remove sensitive data before sharing
   ```

5. **System information:**
   - OS and version
   - .NET version
   - Available memory/disk

### Support Channels

- **Documentation:** Check this guide and related docs
- **GitHub Issues:** https://github.com/danielshue/notebook-automation/issues
- **Discussions:** https://github.com/danielshue/notebook-automation/discussions
- **FAQ:** [Frequently Asked Questions](../getting-started/faq.md)

## Related Documentation

- [Performance Issues](performance-issues.md) - Detailed performance troubleshooting
- [Configuration Problems](configuration-problems.md) - Configuration-specific issues
- [CLI Reference](../cli-reference.md) - Command syntax and options
- [User Guide](../user-guide/index.md) - Complete user documentation
