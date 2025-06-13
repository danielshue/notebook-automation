// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Configuration;

/// <summary>
/// Provides extension methods for IConfiguration and ConfigurationBuilder.
/// </summary>
/// <remarks>
/// This class includes utility methods to simplify the process of adding custom objects
/// as configuration sources and converting objects into key-value pairs for configuration.
/// It is designed to handle both simple and complex objects, supporting nested properties
/// and ensuring compatibility with the IConfiguration interface.
/// </remarks>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds an object as a configuration source.
    /// </summary>
    /// <param name="configurationBuilder">The configuration builder to add to.</param>
    /// <param name="obj">The object to serialize as configuration values.</param>
    /// <returns>The same configuration builder.</returns>
    /// <remarks>
    /// This method serializes the provided object into key-value pairs and adds them
    /// to the configuration builder as an in-memory collection.
    /// </remarks>
    public static IConfigurationBuilder AddObject(this IConfigurationBuilder configurationBuilder, object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        var dictionary = ConvertToDictionary(obj);
        return configurationBuilder.AddInMemoryCollection(dictionary);
    }

    /// <summary>
    /// Converts an object to a dictionary of key-value pairs.
    /// </summary>
    /// <param name="obj">The object to convert.</param>
    /// <returns>A dictionary of key-value pairs.</returns>
    /// <remarks>
    /// This method recursively converts complex objects into nested dictionaries,
    /// using colon-separated keys for nested properties.
    /// </remarks>
    private static IDictionary<string, string?> ConvertToDictionary(object obj)
    {
        var dictionary = new Dictionary<string, string?>();

        if (obj == null)
        {
            return dictionary;
        }

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);

            if (value != null)
            {
                // Handle complex objects by recursively converting them
                if (!property.PropertyType.IsPrimitive &&
                    property.PropertyType != typeof(string) &&
                    property.PropertyType != typeof(DateTime) &&
                    !property.PropertyType.IsEnum)
                {
                    var nestedDictionary = ConvertToDictionary(value);
                    foreach (var kvp in nestedDictionary)
                    {
                        dictionary[$"{property.Name}:{kvp.Key}"] = kvp.Value;
                    }
                }
                else
                {
                    // Simple values are added directly
                    dictionary[property.Name] = value.ToString();
                }
            }
        }

        return dictionary;
    }
}
