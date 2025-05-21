#!/usr/bin/env python3
"""
Test script for all modules in the tools package.
"""

import os
import sys
from pathlib import Path

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Make sure we can import from the tools package
sys.path.insert(0, os.path.abspath('.'))

def try_import(module_path, name=None):
    try:
        module = __import__(module_path, fromlist=['*'])
        print(f"✅ Successfully imported {name or module_path}")
        return module
    except Exception as e:
        print(f"❌ Error importing {name or module_path}: {e}")
        sys.exit(1)

# --- Root Package ---
try_import('tools', 'tools package')

# --- Utils Modules ---
print("\n--- Utils Modules ---")
try_import('tools.utils', 'utils package')
try_import('tools.utils.config', 'config')
try_import('tools.utils.paths', 'paths')
try_import('tools.utils.error_handling', 'error_handling')
try_import('tools.utils.file_operations', 'file_operations')
try_import('tools.utils.http_utils', 'http_utils')

# --- Auth Modules ---
print("\n--- Auth Modules ---")
try_import('tools.auth', 'auth package')
try_import('tools.auth.microsoft_auth', 'microsoft_auth')

# --- OneDrive Modules ---
print("\n--- OneDrive Modules ---")
try_import('tools.onedrive', 'onedrive package')
try_import('tools.onedrive.file_operations', 'onedrive file_operations')

# --- PDF Processing ---
print("\n--- PDF Modules ---")
try_import('tools.pdf', 'pdf package')
try_import('tools.pdf.processor', 'pdf processor')
try_import('tools.pdf.note_generator', 'pdf note_generator')

# --- Transcript Processing ---
print("\n--- Transcript Modules ---")
try_import('tools.transcript', 'transcript package')
try_import('tools.transcript.processor', 'transcript processor')

# --- AI & Summarization ---
print("\n--- AI Modules ---")
try_import('tools.ai', 'ai package')
try_import('tools.ai.summarizer', 'ai summarizer')

# --- Metadata Processing ---
print("\n--- Metadata Modules ---")
try_import('tools.metadata', 'metadata package')
try_import('tools.metadata.path_metadata', 'path_metadata')

# --- Notes Modules ---
print("\n--- Notes Modules ---")
try_import('tools.notes', 'notes package')
try_import('tools.notes.markdown_generator', 'markdown_generator')
try_import('tools.notes.markdown_generator_fixed', 'markdown_generator_fixed')

print("\n🎉 All tools modules imported successfully! The refactoring appears to be working.")
