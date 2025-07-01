#!/bin/bash

# Shell script to build and deploy the Obsidian plugin to the test vault
# Works on macOS and Linux

set -e

PLUGIN_NAME="notebook-automation"
VAULT_PLUGINS_PATH="../../tests/obsidian-vault/Obsidian Vault Test/.obsidian/plugins"

echo "Building the plugin..."

# Install dependencies and build
npm install
npm run build

# Define paths
SOURCE_PATH="$(pwd)"
DEST_PATH="$SOURCE_PATH/$VAULT_PLUGINS_PATH/$PLUGIN_NAME"

echo "Source path: $SOURCE_PATH"
echo "Destination path: $DEST_PATH"

# Ensure destination directory exists
if [ ! -d "$DEST_PATH" ]; then
    echo "Creating plugin directory at $DEST_PATH"
    mkdir -p "$DEST_PATH"
else
    echo "Plugin directory already exists at $DEST_PATH"
fi

# Determine platform and set na executable source
na_source=""
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS
    na_source="../../dist/all-platform-executables-1.0.1-19/published-executables-macos-latest-x64-1.0.1-19/na"
else
    # Linux - try x64 first, then arm64
    na_source="../../dist/all-platform-executables-1.0.1-19/published-executables-ubuntu-latest-x64-1.0.1-19/na"
    if [ ! -f "$na_source" ]; then
        na_source="../../dist/all-platform-executables-1.0.1-19/published-executables-ubuntu-latest-arm64-1.0.1-19/na"
    fi
fi

files_to_copy=("dist/main.js" "manifest.json" "default-config.json")

# Copy na executable if it exists
if [ -f "$na_source" ]; then
    na_executable_name=$(basename "$na_source")
    vault_na_path="$DEST_PATH/$na_executable_name"
    cp "$na_source" "$vault_na_path"
    echo "Copied platform-specific na executable ($na_executable_name) to plugin vault directory: $vault_na_path"
    
    # Also copy to local plugin source directory for dev/test parity
    local_na_path="$SOURCE_PATH/$na_executable_name"
    cp "$na_source" "$local_na_path"
    echo "Copied platform-specific na executable ($na_executable_name) to plugin source directory: $local_na_path"
    
    files_to_copy+=("$na_executable_name")
    
    # Set executable permissions
    chmod +x "$vault_na_path"
    chmod +x "$local_na_path"
    echo "Set executable permissions for na executables."
else
    echo "Warning: na executable not found at $na_source. Plugin may not function properly without the CLI executable."
fi

# Copy all required files
for file in "${files_to_copy[@]}"; do
    src="$SOURCE_PATH/$file"
    dst="$DEST_PATH/$(basename "$file")"
    if [ -f "$src" ]; then
        cp "$src" "$dst"
        echo "Copied $file to $DEST_PATH"
    else
        echo "Warning: $file not found in $SOURCE_PATH"
    fi
done

echo "Plugin deployed to $DEST_PATH. Reload plugins in Obsidian to see changes."
