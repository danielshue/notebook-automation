#!/bin/bash

# Shell script to build and deploy the Obsidian plugin to the test vault
# Works on macOS and Linux

set -e

PLUGIN_NAME="notebook-automation"
VAULT_PLUGINS_PATH="../../tests/obsidian-vault/.obsidian/plugins"

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

# Determine available executables and copy all of them
dist_path="../../dist"
executables_found=()

# Look for executables in dist folder (new flat structure)
if [ -d "$dist_path" ]; then
    available_executables=($(find "$dist_path" -maxdepth 1 -name "na-*" -type f))
    
    if [ ${#available_executables[@]} -gt 0 ]; then
        echo "Found executables in flat dist structure:"
        for exe in "${available_executables[@]}"; do
            echo "  - $(basename "$exe")"
            executables_found+=("$exe")
        done
    else
        echo "No executables found in flat dist structure, trying old structure..."
        
        # Fallback: Look for old structure with version directories
        for version_dir in "$dist_path"/*executables*; do
            if [ -d "$version_dir" ]; then
                while IFS= read -r -d '' exe; do
                    echo "  - $(basename "$exe") (from $(basename "$version_dir"))"
                    executables_found+=("$exe")
                done < <(find "$version_dir" -name "na*" -type f -print0)
            fi
        done
    fi
fi

files_to_copy=("dist/main.js" "manifest.json" "default-config.json")

# Copy all found executables
if [ ${#executables_found[@]} -gt 0 ]; then
    echo "Copying ${#executables_found[@]} executables to plugin directories..."
    
    for exe_path in "${executables_found[@]}"; do
        exe_name=$(basename "$exe_path")
        
        # Copy to vault plugin directory
        vault_exe_path="$DEST_PATH/$exe_name"
        cp "$exe_path" "$vault_exe_path"
        echo "Copied $exe_name to plugin vault directory: $vault_exe_path"
        
        # Copy to local plugin source directory for dev/test parity
        local_exe_path="$SOURCE_PATH/$exe_name"
        cp "$exe_path" "$local_exe_path"
        echo "Copied $exe_name to plugin source directory: $local_exe_path"
        
        # Add to files to copy list
        files_to_copy+=("$exe_name")
        
        # Set executable permissions for non-Windows executables
        if [[ "$exe_name" != *.exe ]]; then
            chmod +x "$vault_exe_path"
            chmod +x "$local_exe_path"
            echo "Set executable permissions for $exe_name"
        fi
    done
else
    echo "Warning: No na executables found in $dist_path. Plugin may not function properly without the CLI executables."
    echo "Expected structure: $dist_path/na-win-x64.exe, na-macos-arm64, etc."
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
