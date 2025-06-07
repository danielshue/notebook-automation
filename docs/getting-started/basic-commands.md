# Basic Commands

Learn the essential commands for using Notebook Automation effectively.

## Command Structure

All commands follow this basic structure:

```powershell
.\na.exe [command] [options] [arguments]
```

## Core Commands

### Process Command

Process files or directories to extract metadata and generate summaries.

**Basic syntax:**

```powershell
.\na.exe process <path> [options]
```

**Examples:**

Process a single file:

```powershell
.\na.exe process "documents/lecture-notes.md"
```

Process a directory recursively:

```powershell
.\na.exe process "documents/" --recursive
```

Process with custom output directory:

```powershell
.\na.exe process "documents/" --output "results/"
```

**Options:**

- `--recursive, -r`: Process directories recursively
- `--output, -o <path>`: Specify output directory
- `--config, -c <path>`: Use custom configuration file
- `--verbose, -v`: Enable verbose logging
- `--dry-run`: Preview operations without executing

### Configuration Commands

Manage application configuration and settings.

**Initialize configuration:**

```powershell
.\na.exe config init
```

**Validate configuration:**

```powershell
.\na.exe config validate
```

**Show current configuration:**

```powershell
.\na.exe config show
```

**Set configuration values:**

```powershell
.\na.exe config set <key> <value>
```

Examples:

```powershell
.\na.exe config set "AIService.Provider" "OpenAI"
.\na.exe config set "Output.BasePath" "./results"
```

### Info Commands

Get information about the application and processed files.

**Show version:**

```powershell
.\na.exe --version
```

**Show help:**

```powershell
.\na.exe --help
.\na.exe process --help  # Command-specific help
```

**Show processing statistics:**

```powershell
.\na.exe info stats --path "output/"
```

## Common Usage Patterns

### Processing Academic Notebooks

Process a semester's worth of notes:

```powershell
.\na.exe process "semester-notes/" --recursive --output "processed-notes/" --verbose
```

### Batch Processing with Custom Config

Process multiple directories with specific settings:

```powershell
.\na.exe process "course1/" --config "academic-config.json" --output "results/course1/"
.\na.exe process "course2/" --config "academic-config.json" --output "results/course2/"
```

### Preview Operations

Check what will be processed without executing:

```powershell
.\na.exe process "documents/" --recursive --dry-run
```

## Tips and Best Practices

### File Paths

- Use quotes around paths containing spaces
- Use forward slashes (/) or double backslashes (\\\\) on Windows
- Relative paths are resolved from the current working directory

### Output Organization

- Use descriptive output directory names
- Include timestamps in output paths for version control:

  ```powershell
  .\na.exe process "notes/" --output "results/$(Get-Date -Format 'yyyy-MM-dd-HHmm')/"
  ```

### Configuration Management

- Keep different configuration files for different use cases
- Use version control to track configuration changes
- Validate configuration before large batch operations

### Performance Optimization

- Use `--verbose` for troubleshooting, but avoid it for large batches
- Process smaller batches if memory usage becomes an issue
- Configure AI service rate limits appropriately

## Next Steps

- [Configuration Guide](../configuration/index.md) - Set up AI services and customize behavior
- [User Guide](../user-guide/index.md) - Advanced usage scenarios
- [Troubleshooting](../troubleshooting/index.md) - Common issues and solutions
