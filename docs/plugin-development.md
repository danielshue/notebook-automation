# Plugin Development Guide

This guide provides comprehensive documentation for developing plugins for the Notebook Automation toolkit, focusing on the new metadata schema system, resolver extension points, and configuration options.

## Overview

The Notebook Automation toolkit supports plugin-based extensibility through a registry system that allows dynamic registration of custom field value resolvers and metadata extraction logic. Plugins can be loaded at runtime from DLL files, enabling custom functionality without modifying the core codebase.

## Plugin Architecture

### Core Components

The plugin system is built around several key interfaces and components:

- **`IFieldValueResolver`**: Interface for custom field value resolution logic
- **`IFileTypeMetadataResolver`**: Interface for file type-specific metadata extraction
- **`FieldValueResolverRegistry`**: Central registry for resolver registration and lookup
- **`MetadataSchemaLoader`**: Schema loader with integrated resolver registry

### Plugin Types

1. **Field Value Resolvers**: Custom logic for populating specific fields
2. **File Type Resolvers**: Specialized metadata extraction for specific file types
3. **Hybrid Resolvers**: Resolvers that implement both interfaces for comprehensive functionality

## Creating Field Value Resolvers

### Basic Resolver Implementation

Implement the `IFieldValueResolver` interface for custom field population logic:

```csharp
using NotebookAutomation.Core.Tools;

public class CustomDateResolver : IFieldValueResolver
{
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        return fieldName switch
        {
            "custom-date" => DateTime.UtcNow.ToString("yyyy-MM-dd"),
            "last-modified" => GetLastModifiedDate(context),
            _ => null
        };
    }
    
    private string GetLastModifiedDate(Dictionary<string, object>? context)
    {
        if (context?.TryGetValue("filePath", out var pathObj) == true && 
            pathObj is string filePath && File.Exists(filePath))
        {
            return File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd");
        }
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}
```

### Advanced Resolver with Context

Use the context parameter to access additional information:

```csharp
public class UserContextResolver : IFieldValueResolver
{
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        return fieldName switch
        {
            "user-name" => GetUserName(context),
            "user-role" => GetUserRole(context),
            "processing-timestamp" => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            _ => null
        };
    }
    
    private string GetUserName(Dictionary<string, object>? context)
    {
        if (context?.TryGetValue("user", out var userObj) == true)
        {
            return userObj?.ToString() ?? "unknown";
        }
        return Environment.UserName;
    }
    
    private string GetUserRole(Dictionary<string, object>? context)
    {
        // Custom logic to determine user role
        return context?.TryGetValue("role", out var roleObj) == true 
            ? roleObj?.ToString() ?? "user"
            : "user";
    }
}
```

## Creating File Type Resolvers

### File Type-Specific Metadata Extraction

Implement `IFileTypeMetadataResolver` for specialized file type handling:

```csharp
using NotebookAutomation.Core.Tools.Resolvers;

public class CustomFileTypeResolver : IFileTypeMetadataResolver
{
    public string FileType => "custom"; // File type this resolver handles
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        return fieldName switch
        {
            "file-size" => GetFileSize(context),
            "file-hash" => GetFileHash(context),
            "custom-metadata" => ExtractCustomMetadata(context),
            _ => null
        };
    }
    
    public Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context)
    {
        var metadata = new Dictionary<string, object>();
        
        if (context?.TryGetValue("filePath", out var pathObj) == true && 
            pathObj is string filePath && File.Exists(filePath))
        {
            var fileInfo = new FileInfo(filePath);
            metadata["file-size"] = fileInfo.Length;
            metadata["file-extension"] = fileInfo.Extension;
            metadata["file-created"] = fileInfo.CreationTime.ToString("yyyy-MM-dd");
            
            // Custom metadata extraction logic
            metadata["custom-properties"] = ExtractCustomProperties(filePath);
        }
        
        return metadata;
    }
    
    private long GetFileSize(Dictionary<string, object>? context)
    {
        if (context?.TryGetValue("filePath", out var pathObj) == true && 
            pathObj is string filePath && File.Exists(filePath))
        {
            return new FileInfo(filePath).Length;
        }
        return 0;
    }
    
    private string GetFileHash(Dictionary<string, object>? context)
    {
        if (context?.TryGetValue("filePath", out var pathObj) == true && 
            pathObj is string filePath && File.Exists(filePath))
        {
            using var stream = File.OpenRead(filePath);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }
        return string.Empty;
    }
    
    private Dictionary<string, object> ExtractCustomProperties(string filePath)
    {
        // Implement custom property extraction logic
        return new Dictionary<string, object>
        {
            ["custom-property-1"] = "value1",
            ["custom-property-2"] = "value2"
        };
    }
}
```

## Plugin Registration

### Manual Registration

Register resolvers programmatically in your application:

```csharp
// Create schema loader
var schemaLoader = new MetadataSchemaLoader("config/metadata-schema.yml", logger);

// Register field value resolver
schemaLoader.ResolverRegistry.Register("CustomDateResolver", new CustomDateResolver());

// Register file type resolver
schemaLoader.ResolverRegistry.RegisterFileTypeResolver("custom", new CustomFileTypeResolver());

// Register hybrid resolver (both interfaces)
var hybridResolver = new HybridResolver();
schemaLoader.ResolverRegistry.Register("HybridResolver", hybridResolver);
```

### Automatic Plugin Loading

Load plugins automatically from a directory:

```csharp
// Load all resolver plugins from directory
schemaLoader.LoadResolversFromDirectory("./plugins");

// This will automatically register all IFieldValueResolver implementations found in DLL files
```

### Plugin Directory Structure

Organize plugins in a dedicated directory:

```
plugins/
├── CustomDateResolver.dll
├── FileTypeResolvers.dll
├── AdvancedResolvers.dll
└── ThirdPartyPlugins.dll
```

## Configuration Integration

### Schema Configuration

Configure resolvers in the `metadata-schema.yml` file:

```yaml
TemplateTypes:
  custom-document:
    BaseTypes:
      - universal-fields
    Type: note/custom
    RequiredFields:
      - custom-date
      - file-hash
    Fields:
      custom-date:
        Resolver: CustomDateResolver
      file-hash:
        Resolver: CustomFileTypeResolver
      user-name:
        Resolver: UserContextResolver
      last-modified:
        Resolver: CustomDateResolver
```

### Application Configuration

Configure plugin loading in your application settings:

```json
{
  "MetadataSchemaPath": "config/metadata-schema.yml",
  "PluginConfiguration": {
    "PluginDirectory": "./plugins",
    "LoadPluginsOnStartup": true,
    "EnablePluginLogging": true
  },
  "ResolverSettings": {
    "DefaultTimeout": 30000,
    "EnableContextLogging": false,
    "CacheResolverResults": true
  }
}
```

## Advanced Plugin Features

### Context-Aware Resolvers

Use context information for intelligent field resolution:

```csharp
public class IntelligentResolver : IFieldValueResolver
{
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null) return null;
        
        return fieldName switch
        {
            "intelligent-title" => GenerateIntelligentTitle(context),
            "content-type" => DetermineContentType(context),
            "processing-priority" => CalculateProcessingPriority(context),
            _ => null
        };
    }
    
    private string GenerateIntelligentTitle(Dictionary<string, object> context)
    {
        // Use AI or heuristics to generate intelligent titles
        var fileName = context.TryGetValue("fileName", out var fn) ? fn?.ToString() : "Unknown";
        var contentType = context.TryGetValue("contentType", out var ct) ? ct?.ToString() : "document";
        
        return $"{CleanFileName(fileName)} ({contentType})";
    }
    
    private string DetermineContentType(Dictionary<string, object> context)
    {
        // Analyze file content to determine type
        if (context.TryGetValue("filePath", out var pathObj) && pathObj is string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "case-study",
                ".mp4" => "video",
                ".md" => "reading",
                _ => "document"
            };
        }
        return "document";
    }
    
    private int CalculateProcessingPriority(Dictionary<string, object> context)
    {
        // Calculate priority based on context
        var fileSize = context.TryGetValue("fileSize", out var fs) && fs is long size ? size : 0;
        var contentType = context.TryGetValue("contentType", out var ct) ? ct?.ToString() : "";
        
        return contentType switch
        {
            "video" => fileSize > 100_000_000 ? 1 : 2, // Large videos: low priority
            "pdf" => 3, // PDFs: medium priority
            "reading" => 4, // Readings: high priority
            _ => 2
        };
    }
    
    private string CleanFileName(string fileName)
    {
        // Clean and format file name
        return fileName.Replace("_", " ").Replace("-", " ");
    }
}
```

### Async Resolvers

For computationally expensive operations, implement async patterns:

```csharp
public class AsyncResolver : IFieldValueResolver
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AsyncResolver> _logger;
    
    public AsyncResolver(HttpClient httpClient, ILogger<AsyncResolver> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        return fieldName switch
        {
            "external-metadata" => GetExternalMetadata(context),
            "ai-summary" => GenerateAISummary(context),
            _ => null
        };
    }
    
    private string GetExternalMetadata(Dictionary<string, object>? context)
    {
        try
        {
            // Perform async operation synchronously (be careful with deadlocks)
            var result = GetExternalMetadataAsync(context).GetAwaiter().GetResult();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get external metadata");
            return "Error retrieving metadata";
        }
    }
    
    private async Task<string> GetExternalMetadataAsync(Dictionary<string, object>? context)
    {
        if (context?.TryGetValue("externalId", out var idObj) == true && 
            idObj is string externalId)
        {
            var response = await _httpClient.GetStringAsync($"https://api.example.com/metadata/{externalId}");
            return response;
        }
        return "No external ID provided";
    }
    
    private string GenerateAISummary(Dictionary<string, object>? context)
    {
        // Implement AI-powered summarization
        // This would typically be an async operation
        return "AI-generated summary placeholder";
    }
}
```

## Plugin Best Practices

### Error Handling

Implement robust error handling in resolvers:

```csharp
public class RobustResolver : IFieldValueResolver
{
    private readonly ILogger<RobustResolver> _logger;
    
    public RobustResolver(ILogger<RobustResolver> logger)
    {
        _logger = logger;
    }
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            return fieldName switch
            {
                "risky-operation" => PerformRiskyOperation(context),
                "safe-fallback" => GetSafeFallback(context),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving field {FieldName}", fieldName);
            return GetDefaultValue(fieldName);
        }
    }
    
    private object? PerformRiskyOperation(Dictionary<string, object>? context)
    {
        // Potentially risky operation
        // Always wrap in try-catch
        return "result";
    }
    
    private object GetSafeFallback(Dictionary<string, object>? context)
    {
        // Always provide safe fallback values
        return "safe-default";
    }
    
    private object GetDefaultValue(string fieldName)
    {
        return fieldName switch
        {
            "risky-operation" => "error-fallback",
            "safe-fallback" => "default-value",
            _ => "unknown-field"
        };
    }
}
```

### Performance Considerations

Optimize resolver performance:

```csharp
public class OptimizedResolver : IFieldValueResolver
{
    private readonly ConcurrentDictionary<string, object> _cache = new();
    private readonly ILogger<OptimizedResolver> _logger;
    
    public OptimizedResolver(ILogger<OptimizedResolver> logger)
    {
        _logger = logger;
    }
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        // Use caching for expensive operations
        var cacheKey = GenerateCacheKey(fieldName, context);
        
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }
        
        var result = fieldName switch
        {
            "expensive-operation" => PerformExpensiveOperation(context),
            "cached-lookup" => PerformCachedLookup(context),
            _ => null
        };
        
        // Cache result if not null
        if (result != null)
        {
            _cache.TryAdd(cacheKey, result);
        }
        
        return result;
    }
    
    private string GenerateCacheKey(string fieldName, Dictionary<string, object>? context)
    {
        var contextHash = context?.GetHashCode() ?? 0;
        return $"{fieldName}:{contextHash}";
    }
    
    private object PerformExpensiveOperation(Dictionary<string, object>? context)
    {
        // Simulate expensive operation
        Thread.Sleep(100);
        return "expensive-result";
    }
    
    private object PerformCachedLookup(Dictionary<string, object>? context)
    {
        // Lookup operation that benefits from caching
        return "cached-result";
    }
}
```

### Testing Plugins

Create comprehensive tests for your plugins:

```csharp
[TestFixture]
public class CustomResolverTests
{
    private CustomDateResolver _resolver;
    private ILogger<CustomDateResolver> _logger;
    
    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<CustomDateResolver>>().Object;
        _resolver = new CustomDateResolver(_logger);
    }
    
    [Test]
    public void Resolve_CustomDate_ReturnsValidDate()
    {
        // Arrange
        var context = new Dictionary<string, object>();
        
        // Act
        var result = _resolver.Resolve("custom-date", context);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<string>());
        Assert.That(DateTime.TryParse(result.ToString(), out _), Is.True);
    }
    
    [Test]
    public void Resolve_UnknownField_ReturnsNull()
    {
        // Arrange
        var context = new Dictionary<string, object>();
        
        // Act
        var result = _resolver.Resolve("unknown-field", context);
        
        // Assert
        Assert.That(result, Is.Null);
    }
    
    [Test]
    public void Resolve_WithContext_UsesContextInformation()
    {
        // Arrange
        var context = new Dictionary<string, object>
        {
            ["filePath"] = "/path/to/test/file.txt"
        };
        
        // Act
        var result = _resolver.Resolve("last-modified", context);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<string>());
    }
}
```

## Deployment and Distribution

### Plugin Packaging

Package plugins as NuGet packages or standalone DLLs:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <PackageId>NotebookAutomation.CustomResolvers</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Name</Authors>
    <Description>Custom resolvers for Notebook Automation</Description>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="NotebookAutomation.Core" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Installation Instructions

Provide clear installation instructions:

```bash
# Install via NuGet
dotnet add package NotebookAutomation.CustomResolvers

# Or copy DLL to plugins directory
cp CustomResolvers.dll /path/to/notebook-automation/plugins/

# Update configuration
# Add resolver configuration to metadata-schema.yml
```

## Security Considerations

### Plugin Security

- **Validate inputs**: Always validate context data and field names
- **Sanitize outputs**: Ensure resolver outputs are safe for consumption
- **Limit permissions**: Run plugins with minimal required permissions
- **Audit plugins**: Review plugin code before deployment

### Example Security Measures

```csharp
public class SecureResolver : IFieldValueResolver
{
    private readonly HashSet<string> _allowedFields = new()
    {
        "safe-field-1",
        "safe-field-2"
    };
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        // Validate field name
        if (!_allowedFields.Contains(fieldName))
        {
            throw new ArgumentException($"Field '{fieldName}' is not allowed");
        }
        
        // Validate context
        if (context != null && !ValidateContext(context))
        {
            throw new ArgumentException("Invalid context provided");
        }
        
        return fieldName switch
        {
            "safe-field-1" => GetSafeValue1(context),
            "safe-field-2" => GetSafeValue2(context),
            _ => null
        };
    }
    
    private bool ValidateContext(Dictionary<string, object> context)
    {
        // Implement context validation logic
        foreach (var kvp in context)
        {
            if (kvp.Key.Contains("..") || kvp.Key.Contains("/"))
            {
                return false; // Prevent path traversal
            }
        }
        return true;
    }
    
    private object GetSafeValue1(Dictionary<string, object>? context)
    {
        // Implement safe value retrieval
        return "safe-value-1";
    }
    
    private object GetSafeValue2(Dictionary<string, object>? context)
    {
        // Implement safe value retrieval
        return "safe-value-2";
    }
}
```

## Troubleshooting

### Common Issues

1. **Plugin not loaded**: Check plugin directory path and DLL compatibility
2. **Resolver not found**: Verify resolver is registered with correct name
3. **Context not available**: Ensure context is properly passed to resolver
4. **Performance issues**: Implement caching and optimize resolver logic

### Debugging Tips

```csharp
public class DebuggingResolver : IFieldValueResolver
{
    private readonly ILogger<DebuggingResolver> _logger;
    
    public DebuggingResolver(ILogger<DebuggingResolver> logger)
    {
        _logger = logger;
    }
    
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        _logger.LogDebug("Resolving field: {FieldName}", fieldName);
        _logger.LogDebug("Context keys: {ContextKeys}", 
            context?.Keys.ToList() ?? new List<string>());
        
        try
        {
            var result = ResolveInternal(fieldName, context);
            _logger.LogDebug("Resolved {FieldName} to: {Result}", fieldName, result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving field {FieldName}", fieldName);
            throw;
        }
    }
    
    private object? ResolveInternal(string fieldName, Dictionary<string, object>? context)
    {
        // Actual resolution logic
        return fieldName switch
        {
            "debug-field" => "debug-value",
            _ => null
        };
    }
}
```

For more information, see the [Metadata Schema Configuration Guide](metadata-schema-configuration.md) and [API Reference](../api/index.md).