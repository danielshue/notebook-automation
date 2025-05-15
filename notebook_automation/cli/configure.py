
#!/usr/bin/env python3
"""
Configuration Management CLI for Digital Note Management System

This script provides a robust, interactive command-line interface for managing the configuration
settings of the Digital Note Management system. It supports creating, viewing, updating, and
validating configuration parameters, and is designed for both direct CLI use and as a standalone
executable (EXE) for cross-platform deployment.

Features:
---------
- Centralized configuration file discovery and loading (auto-detects or prompts for config.json)
- Creation of default configuration with sensible, project-specific defaults
- Human-readable, colorized display of all configuration settings
- Update of individual configuration keys (paths, API credentials, etc.)
- Error handling with clear user feedback and contextual error messages
- Supports passing --config/-c to specify a config file path, or uses auto-discovery
- Designed for maintainability, extensibility, and integration with CI/CD workflows

Usage Examples:
---------------
    # Create a default configuration file (if not present)
    $ vault-configure create

    # Show current configuration in a readable format
    $ vault-configure show

    # Update a specific configuration value
    $ vault-configure update resources_root "/path/to/resources"

    # Use a custom config file location
    $ vault-configure --config "/custom/path/config.json" show

    # Run as a standalone EXE (Windows)
    > configure.exe show

Arguments:
----------
    create           Create a new configuration file with default settings
    show             Display the current configuration settings
    update           Update a specific configuration setting (key value)
    -c, --config     Path to config.json (optional; will prompt if not found)

Project Integration:
--------------------
- This CLI is the canonical tool for managing config.json for all notebook automation scripts.
- Used by all major EXE/CLI tools for consistent configuration handling.
- Included in CI/CD workflows for automated builds and artifact generation.

Author: Dan Shue
License: MIT
"""


import argparse
import sys
from notebook_automation.tools.utils import config as config_utils

# Error categories for standardized error classification across the system
class ErrorCategories:
    NETWORK = "network_error"
    AUTHENTICATION = "authentication_error"
    PERMISSION = "permission_error"
    RATE_LIMIT = "rate_limit_error"
    SERVER = "server_error"
    TIMEOUT = "timeout_error"
    VALIDATION = "validation_error"
    FILE_ERROR = "file_error"
    UNKNOWN = "unknown_error"


# Default configuration structure (for creation only)
DEFAULT_CONFIG = {
    "paths": {
        "resources_root": "C:\\Users\\username\\OneDrive\\Education\\MBA-Resources",
        "notebook_vault_root": "D:\\Vault\\01_Projects\\MBA",
        "metadata_file": "D:\\repos\\mba-notebook-automation\\metadata.yaml",
        "obsidian_vault_root": "C:\\Users\\username\\MBA\\",
        "onedrive_resources_basepath": "/Education/MBA-Resources",
        "logging_dir": "D:\\repos\\mba-notebook-automation\\logs"
    },
    "microsoft_graph": {
        "client_id": "489ad055-e4b0-4898-af27-53506ce83db7",
        "api_endpoint": "https://graph.microsoft.com/v1.0",
        "authority": "https://login.microsoftonline.com/common",
        "scopes": [
            "Files.ReadWrite.All",
            "Sites.ReadWrite.All",
            "offline_access",
            "User.Read"
        ]
    },
    "video_extensions": [".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v"]
}


def create_default_config(config_path: str = None) -> bool:
    """Create a default configuration file if it does not exist.

    Args:
        config_path (str, optional): Path to the config file. If None, uses auto-discovery.

    Returns:
        bool: True if the config file was created, False if it already exists or on error.

    Raises:
        OSError: If the file or directory cannot be created (printed, not propagated).

    Example:
        >>> create_default_config()
        True
    """
    import os, json
    if not config_path:
        config_path = config_utils.find_config_path()
    if not os.path.exists(config_path):
        try:
            os.makedirs(os.path.dirname(config_path), exist_ok=True)
            with open(config_path, 'w') as f:
                json.dump(DEFAULT_CONFIG, f, indent=2)
            print(f"Created default configuration file at {config_path}")
            print("Please edit this file with your actual paths before running other scripts.")
            return True
        except Exception as e:
            print(f"Error creating config file: {e}")
            print(f"  Details: {str(e)}")
            return False
    print(f"Configuration file already exists at {config_path}")
    return False


def load_config(config_path: str = None):
    """Load configuration from a file, using centralized logic.

    Args:
        config_path (str, optional): Path to the config file. If None, uses auto-discovery.

    Returns:
        dict: Loaded configuration dictionary. Returns DEFAULT_CONFIG on error.

    Example:
        >>> config = load_config()
        >>> config['paths']['notebook_vault_root']
        'D:/Vault/01_Projects/MBA'
    """
    try:
        return config_utils.load_config_data(config_path)
    except Exception as e:
        print(f"Error loading config: {e}")
        return DEFAULT_CONFIG


def save_config(config, config_path: str = None):
    """Save the configuration dictionary to a file.

    Args:
        config (dict): The configuration data to save.
        config_path (str, optional): Path to the config file. If None, uses auto-discovery.

    Returns:
        bool: True if the config was saved successfully, False otherwise.

    Raises:
        OSError: If the file cannot be written (printed, not propagated).

    Example:
        >>> save_config(config)
        True
    """
    import os, json
    if not config_path:
        config_path = config_utils.find_config_path()
    try:
        with open(config_path, 'w') as f:
            json.dump(config, f, indent=2)
        print(f"Configuration saved to {config_path}")
        return True
    except Exception as e:
        print(f"Error saving config file: {e}")
        return False


def update_path(config, path_key, new_value, config_path: str = None):
    """Update a specific path key in the configuration and save the result.

    Args:
        config (dict): The configuration dictionary to update.
        path_key (str): The key in the 'paths' section to update.
        new_value (str): The new value for the path key.
        config_path (str, optional): Path to the config file. If None, uses auto-discovery.

    Returns:
        bool: True if the update and save succeeded, False otherwise.

    Example:
        >>> update_path(config, 'notebook_vault_root', '/new/path')
        True
    """
    if path_key not in config["paths"]:
        print(f"Unknown path key: {path_key}")
        print(f"Available path keys: {', '.join(config['paths'].keys())}")
        return False
    config["paths"][path_key] = new_value
    return save_config(config, config_path)

def print_config(config):
    """Print the configuration in a human-readable, colorized format.

    Args:
        config (dict): The configuration dictionary to display.

    Returns:
        None

    Example:
        >>> print_config(config)
    """
    HEADER = '\033[95m'
    OKBLUE = '\033[94m'
    OKCYAN = '\033[96m'
    OKGREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'
    GREY = '\033[90m'
    BG_GREY = '\033[100m'
    BG_BLUE = '\033[44m'
    BG_CYAN = '\033[46m'

    print(f"\n{BG_BLUE}{BOLD}{HEADER}   Digital Note Management Configuration   {ENDC}\n")
    print(f"{OKBLUE}{BOLD}== Paths =={ENDC}")
    for key, value in config["paths"].items():
        if key == "resources_root":
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")
            print(f"    {GREY}↳ Top-level folder in cloud storage for your resources.{ENDC}")
        elif key == "notebook_vault_root":
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")
            print(f"    {GREY}↳ Root folder of your knowledge vault for notes.{ENDC}")
        elif key == "metadata_file":
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")
            print(f"    {GREY}↳ YAML file tracking content metadata.{ENDC}")
        elif key == "logging_dir":
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")
            print(f"    {GREY}↳ Directory where log files are stored for all CLI tools.{ENDC}")
        else:
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")

    print(f"\n{OKBLUE}{BOLD}== OneDrive =={ENDC}")
    if 'onedrive_resources_basepath' in config['paths']:
        print(f"  {OKCYAN}{BOLD}onedrive_resources_basepath {ENDC}: {OKGREEN}{config['paths']['onedrive_resources_basepath']}{ENDC}")
        print(f"    {GREY}↳ Path within OneDrive where resources are located (relative to your OneDrive root).{ENDC}")

    print(f"\n{OKBLUE}{BOLD}== Microsoft Graph API =={ENDC}")
    mg = config.get('microsoft_graph', {})
    print(f"  {OKCYAN}{BOLD}client_id      {ENDC}: {OKGREEN}{mg.get('client_id', '[not set]')}{ENDC}")
    print(f"    {GREY}↳ Application (client) ID for Microsoft Graph API access.{ENDC}")
    print(f"  {OKCYAN}{BOLD}api_endpoint   {ENDC}: {OKGREEN}{mg.get('api_endpoint', '[not set]')}{ENDC}")
    print(f"    {GREY}↳ Microsoft Graph API endpoint URL.{ENDC}")
    print(f"  {OKCYAN}{BOLD}authority      {ENDC}: {OKGREEN}{mg.get('authority', '[not set]')}{ENDC}")
    print(f"    {GREY}↳ Authentication authority for Microsoft login.{ENDC}")
    print(f"  {OKCYAN}{BOLD}scopes         {ENDC}: {OKGREEN}{', '.join(mg.get('scopes', []))}{ENDC}")
    print(f"    {GREY}↳ Permission scopes required for operation.{ENDC}")

    print(f"\n{OKBLUE}{BOLD}== Media Extensions =={ENDC}")
    print(f"  {OKCYAN}{BOLD}Extensions     {ENDC}: {OKGREEN}{', '.join(config['video_extensions'])}{ENDC}")
    print(f"    {GREY}↳ Recognized video file extensions for content processing.{ENDC}")

    print(f"\n{GREY}Tip: Use '{BOLD}vault-configure update <key> <value>{ENDC}{GREY}' to change a setting.{ENDC}\n")

def main():
    parser = argparse.ArgumentParser(
        description="""
Digital Note Management Configuration CLI

This tool manages configuration settings for a digital notebook and knowledge management system. It supports creating, viewing, and updating settings for paths, integrations, and content processing with a focus on clarity and ease of use.

Key Features:
  - Manage resource and workspace paths
  - Store API credentials and integration settings
  - Configure supported file types and extensions
  - Human-readable, colorized output""",
        epilog="""
For more information, see the documentation or use the 'show' command to view current settings.
        """
    )

    subparsers = parser.add_subparsers(dest="command", help="Command to execute (see below)")

    create_parser = subparsers.add_parser(
        "create",
        help="Create a new configuration file with default settings",
        description="""
Create a new configuration file with sensible defaults.

This command generates a new configuration file at the standard location. If a configuration file already exists, it will not be overwritten. Edit the generated file to customize paths, API credentials, or file type settings for your environment.

Example:
  vault-configure create
        """
    )

    show_parser = subparsers.add_parser(
        "show",
        help="Display the current configuration settings",
        description="""
Show the current configuration in a readable, colorized format.

Displays all current settings, including resource paths, API configuration, and file type associations.

Example:
  vault-configure show
        """
    )

    update_parser = subparsers.add_parser(
        "update",
        help="Update a specific configuration setting",
        description="""
Update an individual configuration value.

Specify the key to update and the new value. This preserves all other settings. Common updates include changing resource paths, updating API credentials, or modifying file type associations.

Examples:
  vault-configure update resources_root "/path/to/resources"
  vault-configure update client_id "your-client-id"
        """
    )
    update_parser.add_argument(
        "key",
        help="Configuration key to update (e.g., resources_root, notebook_vault_root, metadata_file, client_id, etc.)"
    )
    update_parser.add_argument(
        "value",
        help="New value for the specified configuration key"
    )

    args = parser.parse_args()

    config_path = None
    if args.command == "create":
        config_path = config_utils.find_config_path()
        if not create_default_config(config_path):
            print(f"Config file already exists at {config_path}")
            print("Use 'show' to view it or 'update' to modify values.")
    elif args.command == "show":
        config = load_config()
        print_config(config)
    elif args.command == "update":
        config = load_config()
        if update_path(config, args.key, args.value):
            print(f"Updated {args.key} to {args.value}")
            print_config(config)
    else:
        config = load_config()
        print_config(config)

if __name__ == "__main__":
    main()
    # This script is designed to be run as a standalone executable or as a CLI tool.
    # It can be integrated into CI/CD workflows or used directly by users for configuration management.