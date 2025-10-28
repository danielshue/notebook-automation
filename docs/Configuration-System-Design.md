# Configuration System Design

Overview of Notebook Automation's hierarchical configuration system.

## Configuration Sources

Configuration is loaded from multiple sources in this order (later sources override earlier ones):

1. **Default Settings** - Built into the application
2. **Configuration Files** - JSON files in various locations
3. **Environment Variables** - OS-level settings
4. **Command-Line Arguments** - Per-execution overrides

## Configuration Locations

1. **Explicit Path**: `--config path/to/config.json`
2. **Current Directory**: `./config.json`
3. **Config Subdirectory**: `./config/appsettings.json`
4. **User Directory**: Platform-specific locations

## Complete Documentation

For detailed configuration information, see:

- [AI Services Configuration](configuration/ai-services.md) - AI provider setup
- [Custom Configuration Tutorial](tutorials/custom-configuration.md) - Creating custom configs
- [Configuration Problems](troubleshooting/configuration-problems.md) - Troubleshooting

## Quick Reference

**View current configuration:**
```bash
na config view
```

**Use environment variables:**
```json
{
  "AIService": {
    "ApiKey": "${OPENAI_API_KEY}"
  }
}
```

**Override configuration:**
```bash
na video-notes -p "file.mp4" --config custom-config.json
```
