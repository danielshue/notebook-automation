using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace NotebookAutomation.Core.Configuration
{
    /// <summary>
    /// Helper class to set up application configuration with various sources, including user secrets.
    /// </summary>
    public static class ConfigurationSetup
    {
        /// <summary>
        /// Creates a standard configuration with support for config files and user secrets.
        /// </summary>
        /// <param name="environment">The current environment (development, production, etc.)</param>
        /// <param name="userSecretsId">Optional user secrets ID. If null, will attempt to use assembly-defined ID.</param>
        /// <param name="configPath">Optional path to the config file. If null, will search for config.json in standard locations.</param>
        /// <returns>A configured IConfiguration instance.</returns>
        public static IConfiguration BuildConfiguration(
            string environment = "Development",
            string? userSecretsId = null,
            string? configPath = null)
        {
            // Find the config file if not specified
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = AppConfig.FindConfigFile();
            }
            
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            // Add JSON configuration if found
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
            }
            
            // Add environment variables
            configurationBuilder.AddEnvironmentVariables();
            
            // Add user secrets in development environment
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(userSecretsId))
                {
                    configurationBuilder.AddUserSecrets(userSecretsId);
                }
                else
                {
                    // Try to use the assembly's user secrets ID
                    configurationBuilder.AddUserSecrets<AppConfig>();
                }
            }
            
            return configurationBuilder.Build();
        }
        
        /// <summary>
        /// Creates a configuration with user secrets support for the given assembly type.
        /// </summary>
        /// <typeparam name="T">The type from the assembly that has the UserSecretsId attribute.</typeparam>
        /// <param name="environment">The current environment (development, production, etc.)</param>
        /// <param name="configPath">Optional path to the config file. If null, will search for config.json in standard locations.</param>
        /// <returns>A configured IConfiguration instance.</returns>
        public static IConfiguration BuildConfiguration<T>(
            string environment = "Development",
            string? configPath = null)
        where T : class
        {
            // Find the config file if not specified
            if (string.IsNullOrEmpty(configPath))
            {
                configPath = AppConfig.FindConfigFile();
            }
            
            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory());

            // Add JSON configuration if found
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                configurationBuilder.AddJsonFile(configPath, optional: false, reloadOnChange: true);
            }
            
            // Add environment variables
            configurationBuilder.AddEnvironmentVariables();
            
            // Add user secrets in development environment
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                configurationBuilder.AddUserSecrets<T>();
            }
            
            return configurationBuilder.Build();
        }
    }
}
