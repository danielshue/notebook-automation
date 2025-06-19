# Troubleshooting

Solutions to common issues and debugging guidance.

## Common Issues

### Installation Problems

#### .NET SDK Not Found

**Problem:** `dotnet` command not recognized or wrong version installed.

**Solution:**
1. Download and install [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
2. Restart your terminal/command prompt
3. Verify installation: `dotnet --version`

#### Package Restore Failures

**Problem:** Build fails with package restore errors.

**Solutions:**
```bash
# Clear NuGet caches
dotnet nuget locals all --clear

# Restore with verbose output
dotnet restore --verbosity detailed

# Force package reinstallation
dotnet restore --force
```

### Configuration Issues

#### Configuration File Not Found

**Problem:** Application can't locate `config/config.json`.

**Solutions:**
- Verify the file exists in the `config/` directory
- Check file permissions (must be readable)
- Use absolute path if running from different directory
- Set environment variable: `NOTEBOOK_CONFIG_PATH=C:\path\to\config.json`

#### Invalid JSON Configuration

**Problem:** JSON parsing errors in configuration files.

**Solutions:**
1. Validate JSON syntax using [jsonlint.com](https://jsonlint.com)
2. Check for common issues:
   - Missing commas between properties
   - Trailing commas (not allowed in JSON)
   - Unescaped quotes in string values
   - Missing closing brackets/braces

#### Environment Variables Not Working

**Problem:** Environment variables are not being recognized.

**Solutions:**
- Use correct naming convention: `NOTEBOOK_SETTING_NAME`
- Restart application after setting variables
- Check variable scope (user vs system vs process)
- Verify with: `echo $env:NOTEBOOK_SETTING_NAME` (PowerShell)

### Processing Issues

#### Input Directory Not Found

**Problem:** Application can't access the specified input directory.

**Solutions:**
- Verify directory exists and is accessible
- Check permissions (read access required)
- Use absolute paths to avoid ambiguity
- Ensure directory is not locked by another process

#### Out of Memory Errors

**Problem:** Application crashes with `OutOfMemoryException`.

**Solutions:**
- Process files in smaller batches
- Enable garbage collection: `GC.Collect()`
- Increase available memory
- Check for memory leaks in custom code

#### Slow Performance

**Problem:** Processing takes longer than expected.

**Solutions:**
- Enable parallel processing if safe to do so
- Check disk I/O performance
- Monitor CPU and memory usage
- Optimize file filtering to exclude unnecessary files

### Build and Development Issues

#### Build Failures

**Problem:** `dotnet build` fails with compilation errors.

**Solutions:**
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Restore packages first
dotnet restore
dotnet build

# Build specific project
dotnet build src/c-sharp/NotebookAutomation.Core
```

#### Test Failures

**Problem:** Unit tests fail unexpectedly.

**Solutions:**
- Run tests with verbose output: `dotnet test --logger "console;verbosity=detailed"`
- Check test dependencies and setup
- Verify test data files exist and are accessible
- Run individual test methods: `dotnet test --filter "TestMethodName"`

#### Code Formatting Issues

**Problem:** Code doesn't meet formatting standards.

**Solutions:**
```bash
# Auto-format code
dotnet format src/c-sharp/NotebookAutomation.sln

# Check formatting without fixing
dotnet format --verify-no-changes

# Fix specific formatting rules
dotnet format --diagnostics IDE0001
```

### Runtime Errors

#### File Access Denied

**Problem:** `UnauthorizedAccessException` when processing files.

**Solutions:**
- Run application as administrator (if necessary)
- Check file/directory permissions
- Ensure files are not locked by other applications
- Use `File.SetAttributes()` to modify read-only files if needed

#### Path Too Long

**Problem:** `PathTooLongException` on Windows systems.

**Solutions:**
- Enable long path support in Windows 10/11
- Use shorter directory structures
- Move files closer to root directory
- Use relative paths where possible

#### Invalid File Format

**Problem:** Application fails to process certain notebook files.

**Solutions:**
- Verify file is a valid notebook format (.ipynb)
- Check file encoding (should be UTF-8)
- Validate JSON structure of notebook file
- Use backup files if originals are corrupted

### Logging and Debugging

#### Enable Debug Logging

Add to `config/config.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "NotebookAutomation": "Trace"
    }
  }
}
```

#### Capture Detailed Logs

```bash
# Run with verbose logging
NotebookAutomation.exe --log-level Trace --log-file debug.log

# Enable console logging
NotebookAutomation.exe --log-console
```

#### Debug Configuration Loading

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Extensions.Configuration": "Debug"
    }
  }
}
```

## Error Messages and Solutions

### Common Error Messages

#### "Could not load file or assembly"

**Error:** Assembly loading failures.

**Solutions:**
- Verify all dependencies are installed
- Check .NET version compatibility
- Ensure assembly is not corrupted
- Try running `dotnet restore` to reinstall packages

#### "Access to the path is denied"

**Error:** File system permission issues.

**Solutions:**
- Check file/directory permissions
- Run as administrator if required
- Ensure path exists and is accessible
- Verify no other process is using the file

#### "The process cannot access the file because it is being used by another process"

**Error:** File locking conflicts.

**Solutions:**
- Close other applications that might be using the file
- Use `using` statements for proper file disposal
- Implement retry logic with delays
- Check for zombie processes holding file locks

## Getting Help

### Before Asking for Help

1. **Check the logs** - Enable debug logging and review output
2. **Reproduce the issue** - Create minimal test case
3. **Check permissions** - Verify file and directory access
4. **Try clean build** - Remove obj/bin directories and rebuild
5. **Update dependencies** - Ensure all packages are current

### Information to Include

When reporting issues, include:
- **Error message** - Full exception details and stack trace
- **Configuration** - Relevant configuration settings (sanitized)
- **Environment** - OS version, .NET version, hardware specs
- **Steps to reproduce** - Minimal sequence to trigger the issue
- **Log files** - Recent log entries with debug level enabled

### Community Resources

- **GitHub Issues** - Report bugs and feature requests
- **Discussions** - Ask questions and share solutions
- **Documentation** - Check other sections of this documentation
- **Stack Overflow** - Search for similar issues with .NET/C#

## See Also

- [Configuration](../configuration/index.md) - Detailed configuration options
- [User Guide](../user-guide/index.md) - Comprehensive usage guide
- [Developer Guide](../developer-guide/index.md) - Development guidelines
- [Getting Started](../getting-started/index.md) - Initial setup instructions
