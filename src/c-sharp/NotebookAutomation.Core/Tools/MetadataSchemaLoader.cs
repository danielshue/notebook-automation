using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Core.Tools;

/// <summary>
/// Defines a contract for field value resolvers used for dynamic field population in metadata schemas.
/// <remarks>
/// Implementations should provide logic to resolve field values based on field name and optional context.
/// </remarks>
/// <example>
/// <code>
/// public class DateCreatedResolver : IFieldValueResolver
/// {
///     public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
///     {
///         return DateTime.UtcNow;
///     }
/// }
/// </code>
/// </example>
/// </summary>
public interface IFieldValueResolver
{
    object? Resolve(string fieldName, Dictionary<string, object>? context = null);
}

/// <summary>
/// Registry for field value resolvers.
/// </summary>
public class FieldValueResolverRegistry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldValueResolverRegistry"/> class.
    /// <summary>
    /// Provides a centralized, extensible registry for dynamic field value resolvers used in metadata schema processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="FieldValueResolverRegistry"/> enables dynamic registration, lookup, and management of <see cref="IFieldValueResolver"/> implementations. It is designed for extensibility, allowing new resolvers to be added at runtime, including via plugin DLLs. This registry is integral to the metadata schema system, supporting dynamic field population, custom logic injection, and runtime extensibility for metadata processing workflows.
    /// </para>
    /// <para>
    /// Typical usage involves registering resolvers by name and retrieving them for field value resolution. The registry is used by <see cref="MetadataSchemaLoader"/> to support dynamic field population and is exposed via the <see cref="IMetadataSchemaLoader.ResolverRegistry"/> property for integration with other components.
    /// </para>
    /// <para>
    /// <b>Extensibility:</b> Supports plugin-based resolver loading via DLLs, enabling custom field logic without modifying core code.
    /// </para>
    /// <b>Thread Safety:</b> This class is not thread-safe; external synchronization is required for concurrent access.
    /// </remarks>
    /// <example>
    /// <code>
    /// var registry = new FieldValueResolverRegistry();
    /// registry.Register("DateCreatedResolver", new DateCreatedResolver());
    /// var resolver = registry.Get("DateCreatedResolver");
    /// if (resolver != null) {
    ///     var value = resolver.Resolve("date-created");
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <example>
    /// <code>
    /// var registry = new FieldValueResolverRegistry();
    /// registry.Register("DateCreatedResolver", new DateCreatedResolver());
    /// </code>
    /// </example>
    public FieldValueResolverRegistry() { }
    private readonly Dictionary<string, IFieldValueResolver> _resolvers = new();
    private readonly Dictionary<string, IFileTypeMetadataResolver> _fileTypeResolvers = new();

    /// <summary>
    /// Registers a field value resolver with the specified name.
    /// </summary>
    /// <param name="resolverName">The unique name for the resolver.</param>
    /// <param name="resolver">The resolver instance to register.</param>
    /// <remarks>
    /// If a resolver with the same name already exists, it will be replaced.
    /// If the resolver implements <see cref="IFileTypeMetadataResolver"/>, it will also be registered for file type-specific lookups.
    /// </remarks>
    /// <example>
    /// <code>
    /// registry.Register("DateCreatedResolver", new DateCreatedResolver());
    /// </code>
    /// </example>
    public void Register(string resolverName, IFieldValueResolver resolver)
    {
        _resolvers[resolverName] = resolver;
        
        // Also register as file type resolver if applicable
        if (resolver is IFileTypeMetadataResolver fileTypeResolver)
        {
            _fileTypeResolvers[fileTypeResolver.FileType] = fileTypeResolver;
        }
    }

    /// <summary>
    /// Registers a file type-specific metadata resolver.
    /// </summary>
    /// <param name="fileType">The file type this resolver handles.</param>
    /// <param name="resolver">The file type resolver instance to register.</param>
    /// <remarks>
    /// This method also registers the resolver in the general resolver registry using the file type as the key.
    /// </remarks>
    /// <example>
    /// <code>
    /// registry.RegisterFileTypeResolver("pdf", new PdfMetadataResolver());
    /// </code>
    /// </example>
    public void RegisterFileTypeResolver(string fileType, IFileTypeMetadataResolver resolver)
    {
        _fileTypeResolvers[fileType] = resolver;
        _resolvers[fileType] = resolver;
    }

    /// <summary>
    /// Retrieves a registered field value resolver by its name.
    /// </summary>
    /// <param name="resolverName">The name of the resolver to retrieve.</param>
    /// <returns>The <see cref="IFieldValueResolver"/> instance if found; otherwise, <c>null</c>.</returns>
    /// <remarks>
    /// Returns <c>null</c> if no resolver is registered under the specified name.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resolver = registry.Get("DateCreatedResolver");
    /// if (resolver != null) {
    ///     var value = resolver.Resolve("date-created");
    /// }
    /// </code>
    /// </example>
    public IFieldValueResolver? Get(string resolverName)
    {
        _resolvers.TryGetValue(resolverName, out var resolver);
        return resolver;
    }

    /// <summary>
    /// Retrieves a registered file type-specific metadata resolver by file type.
    /// </summary>
    /// <param name="fileType">The file type to get the resolver for.</param>
    /// <returns>The <see cref="IFileTypeMetadataResolver"/> instance if found; otherwise, <c>null</c>.</returns>
    /// <remarks>
    /// Returns <c>null</c> if no file type resolver is registered for the specified file type.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resolver = registry.GetFileTypeResolver("pdf");
    /// if (resolver != null) {
    ///     var metadata = resolver.ExtractMetadata(context);
    /// }
    /// </code>
    /// </example>
    public IFileTypeMetadataResolver? GetFileTypeResolver(string fileType)
    {
        _fileTypeResolvers.TryGetValue(fileType, out var resolver);
        return resolver;
    }

    /// <summary>
    /// Gets all registered file type resolvers.
    /// </summary>
    /// <returns>A dictionary of file type to resolver mappings.</returns>
    /// <remarks>
    /// This method is useful for introspection and debugging of registered resolvers.
    /// </remarks>
    public IReadOnlyDictionary<string, IFileTypeMetadataResolver> GetAllFileTypeResolvers()
    {
        return _fileTypeResolvers;
    }
}

/// <summary>
/// Interface for loading and providing access to the metadata schema.
/// Provides access to template types, universal fields, type mapping, and reserved tags.
/// </summary>
public interface IMetadataSchemaLoader
{
    /// <summary>
    /// Gets the dictionary of template types loaded from the schema, keyed by type name.
    /// <summary>
    /// Defines the contract for metadata schema loaders, providing structured access to template types, universal fields, type mappings, reserved tags, and dynamic field value resolvers.
    /// <summary>
    /// Interface for loading and providing access to the metadata schema.
    /// <para>
    /// <b>YAML Key Case Sensitivity:</b> All top-level YAML keys must use PascalCase (e.g., TemplateTypes, UniversalFields, TypeMapping, ReservedTags) to match C# property names. The deserializer is case-sensitive.
    /// </para>
    /// Provides access to template types, universal fields, type mapping, and reserved tags.
    /// </summary>
    /// </para>
    /// <para>
    /// <b>Integration:</b> Used throughout the metadata automation system to provide schema-driven validation, dynamic field population, and extensible metadata processing. The interface is designed for dependency injection and testability, enabling mock implementations for unit testing.
    /// </para>
    /// <b>Extensibility:</b> Supports plugin-based resolver loading and schema extension for custom workflows.
    /// </remarks>
    /// <example>
    /// <code>
    /// IMetadataSchemaLoader loader = new MetadataSchemaLoader("schema.yaml", logger);
    /// var pdfSchema = loader.TemplateTypes["pdf-reference"];
    /// var value = loader.ResolveFieldValue("pdf-reference", "date-created", new Dictionary<string, object> { ["user"] = "daniel" });
    /// </code>
    /// </example>
    /// <example>
    /// <code>
    /// var pdfSchema = loader.TemplateTypes["pdf-reference"];
    /// </code>
    /// </example>
    Dictionary<string, TemplateTypeSchema> TemplateTypes { get; }

    /// <summary>
    /// Gets the list of universal fields shared by all template types.
    /// </summary>
    /// <remarks>
    /// Universal fields are automatically inherited by all template types during schema loading.
    /// </remarks>
    /// <example>
    /// <code>
    /// var universalFields = loader.UniversalFields;
    /// </code>
    /// </example>
    List<string> UniversalFields { get; }

    /// <summary>
    /// Gets the mapping of template type names to canonical type names.
    /// </summary>
    /// <remarks>
    /// Used to normalize type names and support aliases in the schema.
    /// </remarks>
    /// <example>
    /// <code>
    /// var canonicalType = loader.TypeMapping["pdf-reference"];
    /// </code>
    /// </example>
    Dictionary<string, string> TypeMapping { get; }

    /// <summary>
    /// Gets the list of reserved tags that cannot be used as custom tags.
    /// </summary>
    /// <remarks>
    /// Reserved tags are protected and cannot be overridden or used for custom metadata.
    /// </remarks>
    /// <example>
    /// <code>
    /// var reserved = loader.ReservedTags;
    /// </code>
    /// </example>
    List<string> ReservedTags { get; }

    /// <summary>
    /// Gets the registry of field value resolvers for dynamic field population.
    /// </summary>
    /// <remarks>
    /// The registry allows dynamic registration and lookup of field value resolvers by name.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resolver = loader.ResolverRegistry.Get("DateCreatedResolver");
    /// </code>
    /// </example>
    FieldValueResolverRegistry ResolverRegistry { get; }

    /// <summary>
    /// Resolves the value for a field using its resolver if present, otherwise returns the default value.
    /// </summary>
    /// <param name="templateType">The template type name.</param>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <param name="context">Optional context for resolver.</param>
    /// <returns>The resolved field value or default.</returns>
    object? ResolveFieldValue(string templateType, string fieldName, Dictionary<string, object>? context = null);
}

/// <summary>
/// Loads and provides access to the metadata schema defined in metadata-schema.yaml.
/// Supports recursive inheritance, field defaults, and dynamic resolvers.
/// </summary>
public class MetadataSchemaLoader : IMetadataSchemaLoader
{
    private readonly ILogger<MetadataSchemaLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataSchemaLoader"/> class and loads the schema from the specified path.
    /// <summary>
    /// Loads, processes, and provides structured access to the metadata schema defined in <c>metadata-schema.yaml</c>, supporting recursive inheritance, field defaults, dynamic field value resolvers, and extensible schema-driven automation workflows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="MetadataSchemaLoader"/> class is the central component for schema-driven metadata automation. It parses the YAML schema, resolves inheritance hierarchies, merges universal and base fields, and manages canonical type mappings and reserved tags. <b>Reserved tags are inherited as fields by all template types that include universal fields.</b> The loader integrates with a dynamic registry of field value resolvers, supporting plugin-based extensibility and runtime logic injection for field population.
    /// </para>
    /// <para>
    /// <b>Integration:</b> Used throughout the Notebook Automation system for template validation, dynamic field population, and extensible metadata processing. Designed for dependency injection and testability, with comprehensive logging and error handling.
    /// </para>
    /// <para>
    /// <b>Extensibility:</b> Supports runtime loading of custom field value resolvers via DLL plugins, enabling custom logic for field population without modifying core code.
    /// </para>
    /// <b>Thread Safety:</b> This class is not thread-safe; external synchronization is required for concurrent access.
    /// </remarks>
    /// <example>
    /// <code>
    /// var loader = new MetadataSchemaLoader("schema.yaml", logger);
    /// var pdfSchema = loader.TemplateTypes["pdf-reference"];
    /// var value = loader.ResolveFieldValue("pdf-reference", "date-created", new Dictionary<string, object> { ["user"] = "daniel" });
    /// </code>
    /// </example>
    /// Loads the YAML schema, resolves inheritance, and populates all template types, universal fields, type mappings, and reserved tags.
    /// Throws <see cref="System.ArgumentNullException"/> if <paramref name="logger"/> is null.
    /// </remarks>
    /// <example>
    /// <code>
    /// var loader = new MetadataSchemaLoader("schema.yaml", logger);
    /// </code>
    /// </example>
    public MetadataSchemaLoader(string schemaPath, ILogger<MetadataSchemaLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(PascalCaseNamingConvention.Instance)
            .Build();
        using (var reader = new StreamReader(schemaPath))
        {
            var yaml = reader.ReadToEnd();
            var schema = deserializer.Deserialize<MetadataSchemaConfig>(yaml);
            if (schema.TemplateTypes != null)
            {
                // Recursively resolve inheritance and fields
                foreach (var kvp in schema.TemplateTypes)
                {
                    var typeName = kvp.Key;
                    var typeSchema = kvp.Value;
                    ResolveTemplateType(typeName, typeSchema, schema);
                }
                TemplateTypes = schema.TemplateTypes;
            }
            if (schema.UniversalFields != null)
                UniversalFields = schema.UniversalFields;
            if (schema.TypeMapping != null)
                TypeMapping = schema.TypeMapping;
            if (schema.ReservedTags != null)
                ReservedTags = schema.ReservedTags;
        }
    }

    /// <summary>
    /// Loads all DLLs from the specified directory and registers any <see cref="IFieldValueResolver"/> implementations found.
    /// </summary>
    /// <param name="directoryPath">The directory containing resolver DLLs.</param>
    /// <returns>None.</returns>
    /// <remarks>
    /// <para>This method scans the specified directory for DLL files, loads each assembly, and registers all non-abstract classes implementing <see cref="IFieldValueResolver"/>.</para>
    /// <para>If the directory does not exist, a warning is logged and no action is taken.</para>
    /// <para>Errors during assembly loading or type instantiation are logged as errors.</para>
    /// <para><b>Security:</b> Only trusted DLLs should be placed in the directory. Loading arbitrary DLLs may pose security risks.</para>
    /// <para><b>Performance:</b> Loading many DLLs may impact startup time. This method is intended for extensibility, not frequent invocation.</para>
    /// <para><b>Error Handling:</b> All exceptions are caught and logged; no exceptions are propagated to the caller.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// loader.LoadResolversFromDirectory("./resolvers");
    /// var resolver = loader.ResolverRegistry.Get("DateCreatedResolver");
    /// </code>
    /// </example>
    public void LoadResolversFromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            _logger.LogWarning($"Resolver directory not found: {directoryPath}");
            return;
        }
        var dllFiles = Directory.GetFiles(directoryPath, "*.dll");
        foreach (var dll in dllFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IFieldValueResolver).IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
                    {
                        var resolver = (IFieldValueResolver?)Activator.CreateInstance(type);
                        if (resolver != null)
                        {
                            ResolverRegistry.Register(type.FullName ?? type.Name, resolver);
                            _logger.LogInformation($"Registered resolver: {type.FullName ?? type.Name} from {dll}");
                            
                            // Log additional info for file type resolvers
                            if (resolver is IFileTypeMetadataResolver fileTypeResolver)
                            {
                                _logger.LogInformation($"Registered file type resolver for '{fileTypeResolver.FileType}': {type.FullName ?? type.Name}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load resolver DLL: {dll}");
            }
        }
    }
    /// <summary>
    /// Gets the dictionary of template types loaded from the schema, keyed by type name.
    /// </summary>
    /// <remarks>
    /// Each entry provides the full schema for a template type, including inheritance and field definitions.
    /// </remarks>
    /// <example>
    /// <code>
    /// var pdfSchema = loader.TemplateTypes["pdf-reference"];
    /// </code>
    /// </example>
    public Dictionary<string, TemplateTypeSchema> TemplateTypes { get; private set; } = new();

    /// <summary>
    /// Gets the list of universal fields shared by all template types.
    /// </summary>
    /// <remarks>
    /// Universal fields are automatically inherited by all template types during schema loading.
    /// </remarks>
    /// <example>
    /// <code>
    /// var universalFields = loader.UniversalFields;
    /// </code>
    /// </example>
    public List<string> UniversalFields { get; private set; } = new();

    /// <summary>
    /// Gets the mapping of template type names to canonical type names.
    /// </summary>
    /// <remarks>
    /// Used to normalize type names and support aliases in the schema.
    /// </remarks>
    /// <example>
    /// <code>
    /// var canonicalType = loader.TypeMapping["pdf-reference"];
    /// </code>
    /// </example>
    public Dictionary<string, string> TypeMapping { get; private set; } = new();

    /// <summary>
    /// Gets the list of reserved tags that cannot be used as custom tags.
    /// </summary>
    /// <remarks>
    /// Reserved tags are protected and cannot be overridden or used for custom metadata.
    /// </remarks>
    /// <example>
    /// <code>
    /// var reserved = loader.ReservedTags;
    /// </code>
    /// </example>
    public List<string> ReservedTags { get; private set; } = new();

    /// <summary>
    /// Gets the registry of field value resolvers for dynamic field population.
    /// </summary>
    /// <remarks>
    /// The registry allows dynamic registration and lookup of field value resolvers by name.
    /// </remarks>
    /// <example>
    /// <code>
    /// var resolver = loader.ResolverRegistry.Get("DateCreatedResolver");
    /// </code>
    /// </example>
    public FieldValueResolverRegistry ResolverRegistry { get; } = new();

    /// <summary>
    /// Resolves the value for a field in a template type, using its registered resolver if present, otherwise returns the default value.
    /// </summary>
    /// <param name="templateType">The template type name. Inheritance is automatically handled; base types and universal fields are included.</param>
    /// <param name="fieldName">The field name to resolve. Must exist in the resolved schema after inheritance.</param>
    /// <param name="context">Optional context for the resolver, such as additional metadata or runtime values.</param>
    /// <returns>The resolved field value, or the default value if no resolver is present or the field is not found.</returns>
    /// <remarks>
    /// <para>This method first checks the template type and its inherited fields (including universal fields and base types) for the specified field name.</para>
    /// <para>If a resolver is registered for the field, it is invoked with the provided context; otherwise, the default value from the schema is returned.</para>
    /// <para>If the template type or field does not exist, <c>null</c> is returned. No exceptions are thrown for missing types or fields.</para>
    /// <para>Inheritance is handled recursively, so fields from base types and universal fields are always available.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var value = loader.ResolveFieldValue("pdf-reference", "date-created", new Dictionary<string, object> { ["user"] = "daniel" });
    /// // If a resolver is registered, it is used; otherwise, the default value is returned.
    /// </code>
    /// </example>
    public object? ResolveFieldValue(string templateType, string fieldName, Dictionary<string, object>? context = null)
    {
        if (!TemplateTypes.ContainsKey(templateType)) return null;
        var typeSchema = TemplateTypes[templateType];
        if (!typeSchema.Fields.ContainsKey(fieldName)) return null;
        var fieldSchema = typeSchema.Fields[fieldName];
        if (!string.IsNullOrEmpty(fieldSchema.Resolver))
        {
            // Try both full name and short name for backward compatibility
            var resolver = ResolverRegistry.Get(fieldSchema.Resolver)
                ?? ResolverRegistry.Get(fieldSchema.Resolver.Split('.').Last());
            if (resolver != null)
            {
                return resolver.Resolve(fieldName, context);
            }
        }
        return fieldSchema.Default;
    }

    /// <summary>
    /// Recursively resolves inheritance and fields for a template type, merging base types and universal fields into the schema.
    /// </summary>
    /// <param name="typeName">The name of the template type to resolve.</param>
    /// <param name="typeSchema">The schema object for the template type. This object is mutated to include inherited fields.</param>
    /// <param name="schema">The full metadata schema configuration, including all template types and universal fields.</param>
    /// <remarks>
    /// <para>This method traverses the inheritance hierarchy for the specified template type, merging fields from base types and universal fields.</para>
    /// <para>Fields from base types are added only if they do not already exist in the derived type. Universal fields are always included if not present.</para>
    /// <para>This method is called recursively for each base type, ensuring deep inheritance is handled.</para>
    /// <para>Mutates <paramref name="typeSchema"/> in place; does not return a value.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Example usage during schema loading:
    /// foreach (var kvp in schema.TemplateTypes)
    /// {
    ///     ResolveTemplateType(kvp.Key, kvp.Value, schema);
    /// }
    /// </code>
    /// </example>
    private void ResolveTemplateType(string typeName, TemplateTypeSchema typeSchema, MetadataSchemaConfig schema)
    {
        // Recursively inherit fields from base-types
        if (typeSchema.BaseTypes != null)
        {
            foreach (var baseType in typeSchema.BaseTypes)
            {
                if (baseType == "universal-fields" && schema.UniversalFields != null)
                {
                    foreach (var field in schema.UniversalFields)
                    {
                        // Reserved tags are allowed as fields per user request
                        if (!typeSchema.Fields.ContainsKey(field))
                            typeSchema.Fields[field] = new FieldSchema();
                    }
                }
                else if (schema.TemplateTypes != null && schema.TemplateTypes.TryGetValue(baseType, out var baseSchema))
                {
                    ResolveTemplateType(baseType, baseSchema, schema);
                    foreach (var fieldKvp in baseSchema.Fields)
                    {
                        if (!typeSchema.Fields.ContainsKey(fieldKvp.Key))
                            typeSchema.Fields[fieldKvp.Key] = fieldKvp.Value;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Represents the root configuration for the metadata schema, as loaded from YAML.
/// </summary>
/// <remarks>
/// This class is deserialized from the schema YAML file and provides the top-level structure for all template types, universal fields, type mappings, and reserved tags.
/// </remarks>
/// <example>
/// <code>
/// var config = new MetadataSchemaConfig {
///     TemplateTypes = new Dictionary<string, TemplateTypeSchema>(),
///     UniversalFields = new List<string> { "date-created", "publisher" },
///     TypeMapping = new Dictionary<string, string> { ["pdf-reference"] = "reference" },
///     ReservedTags = new List<string> { "auto-generated-state" }
/// };
/// </code>
/// </example>
public class MetadataSchemaConfig
{
    /// <summary>
    /// Gets or sets the dictionary of template type schemas, keyed by type name.
    /// <summary>
    /// Represents the root configuration for the metadata schema, as loaded from YAML.
    /// <para>
    /// <b>YAML Key Case Sensitivity:</b> All top-level YAML keys must use PascalCase (e.g., TemplateTypes, UniversalFields, TypeMapping, ReservedTags) to match C# property names. The deserializer is case-sensitive.
    /// </para>
    /// </summary>
    /// </remarks>
    public Dictionary<string, TemplateTypeSchema>? TemplateTypes { get; set; }

    /// <summary>
    /// Gets or sets the list of universal fields shared by all template types.
    /// </summary>
    /// <remarks>
    /// Universal fields are inherited by all template types and are always present in their schemas.
    /// </remarks>
    public List<string>? UniversalFields { get; set; }

    /// <summary>
    /// Gets or sets the mapping of template type names to canonical type names.
    /// </summary>
    /// <remarks>
    /// Used to map custom or alias type names to their canonical schema type for normalization.
    /// </remarks>
    public Dictionary<string, string>? TypeMapping { get; set; }

    /// <summary>
    /// Gets or sets the list of reserved tags that cannot be used as custom tags.
    /// </summary>
    /// <remarks>
    /// Reserved tags are protected and cannot be overridden or used for custom metadata.
    /// </remarks>
    public List<string>? ReservedTags { get; set; }
}

/// <summary>
/// Represents the schema for a template type, including canonical type, required fields, inheritance, and field definitions.
/// </summary>
/// <remarks>
/// This class models a single template type in the metadata schema, supporting recursive inheritance, required fields, and custom field definitions.
/// </remarks>
/// <example>
/// <code>
/// var pdfSchema = new TemplateTypeSchema {
///     Type = "reference",
///     RequiredFields = new List<string> { "date-created", "publisher" },
///     BaseTypes = new List<string> { "universal-fields" },
///     Fields = new Dictionary<string, FieldSchema> {
///         ["date-created"] = new FieldSchema { Default = "2025-07-11", Resolver = "DateCreatedResolver" },
///         ["publisher"] = new FieldSchema { Default = "University" }
///     }
/// };
/// </code>
/// </example>
public class TemplateTypeSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TemplateTypeSchema"/> class.
    /// <summary>
    /// Models the schema for a single template type, including canonical type mapping, required fields, inheritance hierarchy, and field definitions for metadata automation workflows.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="TemplateTypeSchema"/> class encapsulates the structure and rules for a template type within the metadata schema system. It supports recursive inheritance from base types and universal fields, enforces required fields, and defines custom field schemas with static defaults and dynamic resolvers. This class is central to schema-driven validation, field population, and extensible metadata processing.
    /// </para>
    /// <para>
    /// <b>Integration:</b> Used by <see cref="MetadataSchemaLoader"/> to build the full schema hierarchy, resolve inheritance, and provide runtime access to template type definitions. Enables extensible automation workflows and custom field logic via resolvers.
    /// </para>
    /// <b>Extensibility:</b> Supports schema extension via inheritance and custom field definitions, enabling flexible metadata modeling for diverse content types.
    /// </remarks>
    /// <example>
    /// <code>
    /// var pdfSchema = new TemplateTypeSchema {
    ///     Type = "reference",
    ///     RequiredFields = new List<string> { "date-created", "publisher" },
    ///     BaseTypes = new List<string> { "universal-fields" },
    ///     Fields = new Dictionary<string, FieldSchema> {
    ///         ["date-created"] = new FieldSchema { Default = "2025-07-11", Resolver = "DateCreatedResolver" },
    ///         ["publisher"] = new FieldSchema { Default = "University" }
    ///     }
    /// };
    /// </code>
    /// </example>
    /// </remarks>
    public TemplateTypeSchema() { }

    /// <summary>
    /// Gets or sets the canonical type name for this template type.
    /// </summary>
    /// <remarks>
    /// Used for normalization and mapping to canonical schema types.
    /// </remarks>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of required fields for this template type.
    /// </summary>
    /// <remarks>
    /// Required fields must be present in any instance of this template type.
    /// </remarks>
    public List<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of base types this template type inherits from.
    /// </summary>
    /// <remarks>
    /// Base types are resolved recursively, allowing inheritance of fields and required fields from other template types or universal fields.
    /// </remarks>
    public List<string>? BaseTypes { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of field schemas for this template type.
    /// </summary>
    /// <remarks>
    /// Each entry defines the schema for a field, including default values and resolver names.
    /// </remarks>
    public Dictionary<string, FieldSchema> Fields { get; set; } = new();
}

/// <summary>
/// Represents metadata for a field in a template type, including default value and resolver name.
/// </summary>
/// <remarks>
/// This class models a single field in a template type schema, supporting static defaults and dynamic value resolution via registered resolvers.
/// </remarks>
/// <example>
/// <code>
/// var field = new FieldSchema {
///     Default = "2025-07-11",
///     Resolver = "DateCreatedResolver"
/// };
/// </code>
/// </example>
public class FieldSchema
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSchema"/> class.
    /// </summary>
    /// <remarks>
    /// Used for field definitions in template schemas, supporting default values and resolvers.
    /// </remarks>
    public FieldSchema() { }

    /// <summary>
    /// Gets or sets the default value for the field.
    /// </summary>
    /// <remarks>
    /// If no resolver is present, this value is used as the field's value.
    /// </remarks>
    public object? Default { get; set; }

    /// <summary>
    /// Gets or sets the resolver name for dynamic field population.
    /// </summary>
    /// <remarks>
    /// If set, the named resolver will be used to compute the field's value at runtime.
    /// </remarks>
    public string? Resolver { get; set; }
}
