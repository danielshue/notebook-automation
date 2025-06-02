// Module: ConfigurationExtensions.cs
// Extension methods for working with IConfiguration and ConfigurationBuilder.
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Provides extension methods for IConfiguration and ConfigurationBuilder.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds an object as a configuration source.
        /// </summary>
        /// <param name="configurationBuilder">The configuration builder to add to.</param>
        /// <param name="obj">The object to serialize as configuration values.</param>
        /// <returns>The same configuration builder.</returns>
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
}