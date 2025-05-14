#!/bin/bash
# Script to build all PyInstaller spec files in the repository

echo "Starting to build all executables..."

# Find all .spec files and build them
for spec_file in *.spec; do
    echo "Building $spec_file..."
    pyinstaller "$spec_file"
    if [ $? -eq 0 ]; then
        echo "Successfully built $spec_file"
    else
        echo "Failed to build $spec_file"
    fi
done

echo "All builds completed. Executables can be found in the dist/ directory."
