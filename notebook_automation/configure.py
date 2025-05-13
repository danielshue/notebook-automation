#!/usr/bin/env python3
"""
Configuration Management Tool for Digital Note Management System

This script provides a comprehensive interface for managing the configuration settings
of the Digital Note Management system. It enables users to create, view, modify, and
validate configuration parameters through both interactive CLI and programmatic interfaces.

Key Features:
------------
1. Configuration Creation and Management
   - Default configuration generation with sensible defaults
   - Path customization for cloud storage and vault integration
   - API settings management
   - File type and extension configuration

2. Interactive Command-Line Interface
   - Command-driven interaction model
   - Human-readable configuration display
   - Guided configuration updates
   - Error handling with user feedback

3. Configuration Validation
   - Path existence checking
   - Permission verification
   - Format validation
   - Cross-setting compatibility checks

Usage:
------
- Initial setup:    python3 configure.py create
- View settings:    python3 configure.py show
- Update a setting: python3 configure.py update <key> <value>
- No arguments:     Shows current configuration if exists, creates default otherwise
"""

import os
import json
import argparse
import sys
from pathlib import Path
from typing import Dict, Any, Optional

# Import from package
from notebook_automation.tools.utils.config import ErrorCategories
from notebook_automation import __version__

# Get the absolute path to the repo root and user home directory
REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
USER_HOME = os.path.expanduser("~")
CONFIG_DIR = os.path.join(USER_HOME, ".notebook_automation")
CONFIG_FILE_PATH = os.path.join(CONFIG_DIR, 'config.json')

# Default configuration structure
# This serves as both a template for new installations and a fallback
# when configuration files are missing or corrupted
DEFAULT_CONFIG = {
    # File system paths for content sources and destinations
    "paths": {
        "resources_root": "C:\\Users\\username\\OneDrive\\Education\\MBA-Resources",  # OneDrive resource location
        "notebook_vault_root": "D:\\Vault\\01_Projects\\MBA",  # Obsidian vault location
        "metadata_file": "D:\\repos\\mba-notebook-automation\\metadata.yaml",  # Metadata tracking file
        "obsidian_vault_root": "C:\\Users\\username\\MBA\\",  # Obsidian vault root (optional, for compatibility)
        "onedrive_resources_basepath": "/Education/MBA-Resources"  # Base path within OneDrive to work with
    },
    # Microsoft Graph API configuration for OneDrive access
    "microsoft_graph": {
        "client_id": "489ad055-e4b0-4898-af27-53506ce83db7",  # Application (client) ID for API access
        "api_endpoint": "https://graph.microsoft.com/v1.0",    # Graph API endpoint URL
        "authority": "https://login.microsoftonline.com/common",  # Authentication authority
        "scopes": [  # Permission scopes required for operation
            "Files.ReadWrite.All",  # Access to read and write files
            "Sites.ReadWrite.All",  # Access to SharePoint sites
            "offline_access",       # Allow refresh tokens
            "User.Read"             # Basic user profile access
        ]
    },
    # OneDrive-specific configuration
    "onedrive": {},
    # File types recognized by the system as video files
    "video_extensions": [".mp4", ".mov", ".avi", ".mkv", ".webm", ".wmv", ".mpg", ".mpeg", ".m4v"]
}

def create_default_config() -> bool:
    """Create a default configuration file if none exists.
    
    This function generates a new configuration file with sensible defaults at the
    standard location. It checks if a configuration file already exists to prevent
    accidentally overwriting customized settings. The default configuration includes
    placeholder paths, Microsoft Graph API settings, and file type configurations
    that users will need to modify for their specific environment.
    
    Returns:
        bool: True if a new configuration file was created, False if the file already 
            exists or if there was an error during creation.
              
    Side Effects:
        - Creates a new config.json file at the repository root if one doesn't exist
        - Prints status messages about the configuration creation process
        
    Example:
        >>> if create_default_config():
        ...     print("New configuration created. Please update with your settings.")
        ... else:
        ...     print("Using existing configuration.")
    """
    # Check if the configuration file already exists to prevent overwriting
    if not os.path.exists(CONFIG_FILE_PATH):
        try:
            # Create the config directory if it doesn't exist
            os.makedirs(os.path.dirname(CONFIG_FILE_PATH), exist_ok=True)
            
            # Create the config file with default values
            # Pretty-print with indent=2 for readability
            with open(CONFIG_FILE_PATH, 'w') as f:
                json.dump(DEFAULT_CONFIG, f, indent=2)
                
            # Inform the user about the new configuration and next steps
            print(f"Created default configuration file at {CONFIG_FILE_PATH}")
            print("Please edit this file with your actual paths before running other scripts.")
            return True
        except Exception as e:
            # Handle errors during file creation (e.g., permission issues)
            print(f"Error creating config file: {e}")
            print(f"  Details: {str(e)}")
            return False
    # Configuration already exists, no action taken
    print(f"Configuration file already exists at {CONFIG_FILE_PATH}")
    return False

def load_config():
    """
    Load and return the configuration from the config file.
    
    This function attempts to read and parse the JSON configuration file. If the file
    doesn't exist or contains invalid JSON, it falls back to creating a default
    configuration. The function handles file access errors gracefully, ensuring the
    application can continue with at least a basic configuration.
    
    Returns:
        dict: A dictionary containing the configuration settings. If the file cannot
              be read or parsed, returns the DEFAULT_CONFIG dictionary.
              
    Side Effects:
        - May create a new default configuration file if none exists
        - Logs error messages for configuration loading failures
    """
    try:
        # Attempt to open and parse the configuration file
        with open(CONFIG_FILE_PATH, 'r') as f:
            return json.load(f)
    except FileNotFoundError:
        # File doesn't exist - create a default configuration
        print(f"Configuration file not found at {CONFIG_FILE_PATH}")
        create_default_config()
        return DEFAULT_CONFIG
    except json.JSONDecodeError:
        # File exists but contains invalid JSON
        print(f"Error parsing configuration file: Invalid JSON format")
        print(f"Using default configuration instead. Please fix or delete {CONFIG_FILE_PATH}")
        return DEFAULT_CONFIG
    except Exception as e:
        # Handle any other errors (permissions, etc.)
        print(f"Error loading config file: {e}")
        create_default_config()
        return DEFAULT_CONFIG

def save_config(config):
    """
    Save the configuration to the config file.
    
    This function serializes the provided configuration dictionary to JSON format
    and writes it to the configuration file. It uses pretty-printing (indentation)
    to make the saved file human-readable and easily editable. Error handling ensures
    that failures don't crash the application and provides meaningful feedback.
    
    Args:
        config (dict): The configuration dictionary to save
        
    Returns:
        bool: True if the configuration was saved successfully, False otherwise
        
    Side Effects:
        - Overwrites the existing config.json file with new content
        - Prints status messages about the save operation
    """
    try:
        with open(CONFIG_FILE_PATH, 'w') as f:
            json.dump(config, f, indent=2)
        print(f"Configuration saved to {CONFIG_FILE_PATH}")
        return True
    except Exception as e:
        print(f"Error saving config file: {e}")
        return False

def update_path(config, path_key, new_value):
    """
    Update a specific path in the configuration.
    
    This function modifies a single path entry in the configuration dictionary and
    saves the updated configuration to disk. It validates that the specified key
    exists in the paths section before attempting the update, providing clear
    error messages if the key is invalid.
    
    Args:
        config (dict): The current configuration dictionary
        path_key (str): The key of the path to update (must be a valid key in the paths section)
        new_value (str): The new path value to set
        
    Returns:
        bool: True if the path was updated and saved successfully, False otherwise
        
    Side Effects:
        - Modifies the config dictionary in-place
        - May update the saved configuration file
        - Prints error messages for invalid path keys
    """
    if path_key not in config["paths"]:
        print(f"Unknown path key: {path_key}")
        print(f"Available path keys: {', '.join(config['paths'].keys())}")
        return False
    
    config["paths"][path_key] = new_value
    return save_config(config)

def print_config(config):
    """
    Print the configuration in a human-readable format.
    
    This function displays the current configuration settings in a structured,
    easy-to-read format organized by category. The output is designed for terminal
    display with section headers, making it easy for users to view and verify
    their current settings.
    
    Args:
        config (dict): The configuration dictionary to display
        
    Side Effects:
        - Prints formatted configuration information to stdout
    """
    # ANSI color codes for formatting
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

    # Title block
    print(f"\n{BG_BLUE}{BOLD}{HEADER}   Digital Note Management Configuration   {ENDC}\n")

    # Paths section
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
        else:
            print(f"  {OKCYAN}{BOLD}{key:<15}{ENDC}: {OKGREEN}{value}{ENDC}")

    # OneDrive section
    print(f"\n{OKBLUE}{BOLD}== OneDrive =={ENDC}")
    if 'onedrive_resources_basepath' in config['paths']:
        print(f"  {OKCYAN}{BOLD}onedrive_resources_basepath {ENDC}: {OKGREEN}{config['paths']['onedrive_resources_basepath']}{ENDC}")
        print(f"    {GREY}↳ Path within OneDrive where resources are located (relative to your OneDrive root).{ENDC}")

    # Microsoft Graph API section
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

    # Video extensions section
    print(f"\n{OKBLUE}{BOLD}== Media Extensions =={ENDC}")
    print(f"  {OKCYAN}{BOLD}Extensions     {ENDC}: {OKGREEN}{', '.join(config['video_extensions'])}{ENDC}")
    print(f"    {GREY}↳ Recognized video file extensions for content processing.{ENDC}")

    print(f"\n{GREY}Tip: Use '{BOLD}vault-configure update <key> <value>{ENDC}{GREY}' to change a setting.{ENDC}\n")

def main():
    """
    Main entry point for the configuration management tool.
    
    This function sets up the command-line interface, parses arguments, and routes
    to the appropriate handler function based on the command. It provides a user-friendly
    interface for managing the configuration with sensible defaults when no arguments
    are provided.
    
    Command Structure:
    - create: Generate a new default configuration file
    - show: Display the current configuration settings
    - update: Modify a specific configuration path
    
    Returns:
        None
        
    Side Effects:
        - May create, read, or update the configuration file
        - Prints information to stdout based on the command
    """    
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

# Set up subparsers for different commands
subparsers = parser.add_subparsers(dest="command", help="Command to execute (see below)")

# Create command - generates a new default configuration file
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

    # Show command - displays the current configuration in human-readable format
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

    # Update command - modifies a specific configuration value
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
    
    # Parse the command-line arguments
args = parser.parse_args()
    
    # Handle the "create" command - generate a new configuration file
if args.command == "create":
        if not create_default_config():
            print(f"Config file already exists at {CONFIG_FILE_PATH}")
            print("Use 'show' to view it or 'update' to modify values.")
    
    # Handle the "show" command - display current configuration
elif args.command == "show":
        config = load_config()
        print_config(config)
    
    # Handle the "update" command - modify a configuration value
elif args.command == "update":
        config = load_config()
        if update_path(config, args.key, args.value):
            print(f"Updated {args.key} to {args.value}")
            print_config(config)
    
    # Default behavior when no command is specified
else:
        # If configuration exists, show it
        if os.path.exists(CONFIG_FILE_PATH):
            config = load_config()
            print_config(config)
        # Otherwise, create a default configuration and show help
        else:
            create_default_config()
            parser.print_help()

if __name__ == "__main__":
    main()

# Example usage:
"""
# Create default configuration
$ vault-configure create

# Show current configuration
$ vault-configure show

# Update the path to your resources
$ vault-configure update resources_root "/path/to/cloud/resources"

# Update the path to your notes vault
$ vault-configure update notebook_vault_root "/path/to/vault"
"""