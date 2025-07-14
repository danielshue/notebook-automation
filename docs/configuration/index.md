# Configuration

Configure Notebook Automation for your specific needs and environment.

## Overview

Notebook Automation uses a flexible configuration system that supports:
- JSON configuration files
- Environment variables
- Command-line arguments
- User secrets for sensitive data

## Configuration Files

### Main Configuration

The primary configuration file is `config/config.json`:

```json
{
  "NotebookSettings": {
    "InputDirectory": "input",
    "OutputDirectory": "output",
    "BackupDirectory": "backup",
    "TemplateDirectory": "templates"
  },
  "ProcessingOptions": {
    "EnableMetadataExtraction": true,
    "EnableTagGeneration": true,
    "EnableContentSummary": true,
    "ParallelProcessing": false
  },
  "LoggingSettings": {
    "LogLevel": "Warning",
    "LogToFile": true,
    "LogFilePath": "logs/notebook-automation.log",
    "MaxFileSizeMB": 50,
    "RetainedFileCount": 7
  }
}
```

### Example Configurations

See the `config/` directory for example configurations:
- `config.json` - Basic configuration
- `example-config-with-timeout.json` - Configuration with timeout settings

### Metadata Schema Configuration

The metadata system is configured using the unified `metadata-schema.yml` file, which defines template types, universal fields, type mappings, and reserved tags:

```yaml
# NOTE: All top-level keys must use PascalCase for C# compatibility
TemplateTypes:
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - comprehension
      - status
      - completion-date
      - authors
      - tags
    Fields:
      publisher:
        Default: University of Illinois at Urbana-Champaign
      status:
        Default: unread
      comprehension:
        Default: 0
      date-created:
        Resolver: DateCreatedResolver
      title:
        Default: "PDF Note"
      tags:
        Default: [pdf, reference]
      page-count:
        Resolver: PdfPageCountResolver

UniversalFields:
  - auto-generated-state
  - date-created
  - publisher

ReservedTags:
  - auto-generated-state
  - case-study
  - video
  - pdf
```

**Key Features:**
- **Template Types**: Define structured schemas for different content types
- **Universal Fields**: Fields automatically inherited by all template types
- **Reserved Tags**: Protected system tags that cannot be overridden
- **Field Resolvers**: Dynamic field population through registered resolvers
- **Type Mapping**: Canonical type normalization for consistent processing

For detailed schema configuration, see the [Metadata Schema Configuration Guide](../metadata-schema-configuration.md).

## Environment Variables

Override configuration settings using environment variables:

```powershell
# Core settings
$env:NOTEBOOK_INPUT_DIR = "C:\MyNotebooks"
$env:NOTEBOOK_OUTPUT_DIR = "C:\ProcessedNotebooks"

# Processing options
$env:NOTEBOOK_ENABLE_METADATA = "true"
$env:NOTEBOOK_ENABLE_TAGS = "true"
$env:NOTEBOOK_PARALLEL_PROCESSING = "false"

# Logging
$env:NOTEBOOK_LOG_LEVEL = "Debug"
$env:NOTEBOOK_LOG_FILE = "logs/debug.log"
```

## Logging Configuration

Notebook Automation provides comprehensive logging capabilities with configurable verbosity levels and automatic log file management.

### Log Levels

The application supports the following log levels:

- **Debug/Verbose**: Detailed diagnostic information (only shown when `--debug` or `--verbose` flags are used)
- **Information**: General application flow information
- **Warning**: Potentially harmful situations (default for production)
- **Error**: Error events that allow the application to continue
- **Critical**: Critical failures that may cause the application to terminate

### Production vs Debug Mode

**Production Mode (Default)**:
```powershell
# Only warnings, errors, and critical messages are shown in console
NotebookAutomation.exe process-pdfs --input "Documents/"

# Enable verbose mode for detailed console output
NotebookAutomation.exe process-pdfs --input "Documents/" --verbose
```

**Debug Mode**:
```powershell
# Shows all log levels including debug information in console
NotebookAutomation.exe process-pdfs --input "Documents/" --debug
```

### Rolling Log Files

Log files are automatically managed with size-based rolling:

- **Default filename**: `notebook-automation.log` (consistent across runs)
- **Rolling behavior**: When file reaches maximum size, creates `.1`, `.2`, etc.
- **Automatic cleanup**: Old log files are automatically deleted when limit is reached

### Configuration Options

Add to your `config.json`:

```json
{
  "logging": {
    "max_file_size_mb": 50,
    "retained_file_count": 7
  },
  "paths": {
    "logging_dir": "./logs"
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `max_file_size_mb` | Maximum size of each log file before rolling | 50 MB |
| `retained_file_count` | Number of old log files to keep | 7 |
| `logging_dir` | Directory where log files are stored | `./logs` |

### Example Log Files

```
logs/
├── notebook-automation.log      # Current log file
├── notebook-automation.log.1    # Previous log file  
├── notebook-automation.log.2    # Older log file
└── notebook-automation.log.3    # Oldest retained file
```

### Environment Variables

Override logging settings with environment variables:

```powershell
# Set log levels
$env:DEBUG = "true"
$env:VERBOSE = "true"

# Override file settings  
$env:LOGGING_MAX_FILE_SIZE_MB = "100"
$env:LOGGING_RETAINED_FILE_COUNT = "14"
```

## User Secrets

Store sensitive configuration data securely using .NET User Secrets:

```powershell
# Initialize user secrets
dotnet user-secrets init --project src/c-sharp/NotebookAutomation.Core

# Add API keys or sensitive settings
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
dotnet user-secrets set "Azure:ConnectionString" "your-connection-string"
```

## Command Line Arguments

Override any configuration setting from the command line:

```powershell
# Basic usage with config overrides
NotebookAutomation.exe --input "C:\Notes" --output "C:\Processed" --log-level Debug

# Processing options
NotebookAutomation.exe --enable-metadata --disable-tags --parallel
```

## Configuration Hierarchy

Settings are applied in the following order (later sources override earlier ones):

1. Default configuration values
2. Configuration files (`config/config.json`)
3. Environment variables
4. User secrets
5. Command-line arguments

## Template Configuration

Customize output templates by modifying files in the `templates/` directory:

### Block Templates

Edit `config/BaseBlockTemplate.yaml` to customize how notebook blocks are processed:

```yaml
template:
  header: |
    ## {block_type}: {title}
    **Created:** {timestamp}

  content: |
    {content}

  footer: |
    ---
    *Generated by Notebook Automation*
```

### Custom Templates

Create custom templates for specific notebook types:

```yaml
# templates/course-template.yaml
course_template:
  metadata_section: |
    # Course: {course_name}
    **Code:** {course_code}
    **Instructor:** {instructor}
    **Semester:** {semester}

  assignment_section: |
    ## Assignment: {assignment_title}
    **Due Date:** {due_date}
    **Points:** {points}
```

## Banner Configuration

Configure custom banners for generated markdown files using the `banners` section in `config.json`:

### Basic Banner Configuration

```json
{
  "banners": {
    "enabled": true,
    "default": "gies-banner.png",
    "format": "image",
    "template_banners": {
      "main": "main-banner.png",
      "program": "program-banner.png",
      "course": "course-banner.png"
    },
    "filename_patterns": {
      "*index*": "index-banner.png",
      "*assignment*": "assignment-banner.png"
    }
  }
}
```

### Banner Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `enabled` | Global enable/disable for banner functionality | `true` |
| `default` | Default banner content when no specific match is found | `"gies-banner.png"` |
| `format` | Banner format type: `image`, `text`, `markdown`, `html` | `"image"` |
| `template_banners` | Banner content by template type | See defaults |
| `filename_patterns` | Banner content by filename patterns (wildcards supported) | `{}` |

### Banner Selection Priority

The system selects banners in the following order (first match wins):

1. **Existing banner**: If frontmatter already contains a banner, it's preserved
2. **Filename patterns**: Matches filename against wildcard patterns
3. **Template types**: Matches `template-type` frontmatter field
4. **Default fallback**: Uses default banner for backward compatibility

### Filename Pattern Examples

```json
{
  "filename_patterns": {
    "*index*": "index-banner.png",        // Matches: index.md, course-index.md, etc.
    "assignment-*": "assignment-banner.png", // Matches: assignment-1.md, assignment-final.md
    "*.course.*": "course-banner.png",    // Matches: mba.course.md, intro.course.notes.md
    "main": "main-banner.png"             // Exact match: main.md
  }
}
```

### Banner Format Types

- **`image`**: Obsidian-style image references (default behavior)
- **`text`**: Plain text content
- **`markdown`**: Markdown formatted content  
- **`html`**: HTML formatted content

### Disabling Banners

To disable banner functionality entirely:

```json
{
  "banners": {
    "enabled": false
  }
}
```

## Validation

The configuration system includes built-in validation:

- **Required fields** - Essential settings must be provided
- **Type checking** - Values must match expected data types
- **Path validation** - Directory paths must exist or be creatable
- **Dependency checks** - Related settings are validated together

## Troubleshooting Configuration

### Common Issues

**Configuration not loading:**
- Check file path and permissions
- Verify JSON syntax using a validator
- Review log output for specific error messages

**Environment variables not recognized:**
- Ensure proper naming convention (NOTEBOOK_SETTING_NAME)
- Check for typos in variable names
- Verify variables are set in the correct scope

**User secrets not accessible:**
- Confirm user secrets are initialized for the project
- Check that secret names match configuration keys
- Verify the user secrets store is accessible

### Debug Configuration

Enable detailed configuration logging:

```json
{
  "Logging": {
    "LogLevel": {
      "NotebookAutomation.Configuration": "Debug"
    }
  }
}
```

## Advanced Configuration

### Custom Configuration Providers

Extend the configuration system with custom providers:

```csharp
public class DatabaseConfigurationProvider : ConfigurationProvider
{
    public override void Load()
    {
        // Load configuration from database
        var settings = LoadFromDatabase();
        Data = settings.ToDictionary(s => s.Key, s => s.Value);
    }
}
```

### Configuration Validation Attributes

Use data annotations for configuration validation:

```csharp
public class NotebookSettings
{
    [Required]
    [DirectoryExists]
    public string InputDirectory { get; set; }

    [Range(1, 100)]
    public int MaxConcurrentFiles { get; set; }
}
```

## See Also

- [Getting Started](../getting-started/index.md) - Initial setup and basic usage
- [User Guide](../user-guide/index.md) - Comprehensive usage documentation
- [Troubleshooting](../troubleshooting/index.md) - Common issues and solutions
- [Developer Guide](../developer-guide/index.md) - Development and contribution guidelines
