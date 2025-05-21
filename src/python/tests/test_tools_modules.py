#!/usr/bin/env python3
"""
Test script for all modules in the tools package.
This script verifies that all modules can be imported correctly.
"""

import os
import sys
import time
from pathlib import Path

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Make sure we can import from the tools package
sys.path.insert(0, os.path.abspath('.'))

def try_import(module_path, name=None):
    display_name = name or module_path
    start_time = time.time()
    try:
        module = __import__(module_path, fromlist=['*'])
        end_time = time.time()
        elapsed = end_time - start_time
        print(f"✅ Successfully imported {display_name} (took {elapsed:.4f} seconds)")
        return module
    except Exception as e:
        end_time = time.time()
        elapsed = end_time - start_time
        print(f"❌ Error importing {display_name} (took {elapsed:.4f} seconds): {e}")
        return None

def test_all_modules():
    errors = 0
    total_modules = 0
    total_time = 0
    results = []
    
    def import_module(module_path, name=None):
        nonlocal errors, total_modules, total_time
        total_modules += 1
        start_time = time.time()
        result = try_import(module_path, name)
        end_time = time.time()
        elapsed = end_time - start_time
        total_time += elapsed
        results.append({
            'module': name or module_path,
            'success': result is not None,
            'time': elapsed
        })
        if not result:
            errors += 1
        return result
    
    # --- Root Package ---
    print("\n=== Root Package ===")
    import_module('tools', 'tools package')

    # --- Utils Modules ---
    print("\n=== Utils Modules ===")
    import_module('tools.utils', 'utils package')
    import_module('tools.utils.config', 'config')
    import_module('tools.utils.paths', 'paths')
    import_module('tools.utils.error_handling', 'error_handling')
    import_module('tools.utils.file_operations', 'file_operations')
    import_module('tools.utils.http_utils', 'http_utils')

    # --- Auth Modules ---
    print("\n=== Auth Modules ===")
    import_module('tools.auth', 'auth package')
    import_module('tools.auth.microsoft_auth', 'microsoft_auth')

    # --- OneDrive Modules ---
    print("\n=== OneDrive Modules ===")
    import_module('tools.onedrive', 'onedrive package')
    import_module('tools.onedrive.file_operations', 'onedrive file_operations')

    # --- PDF Processing ---
    print("\n=== PDF Modules ===")
    import_module('tools.pdf', 'pdf package')
    import_module('tools.pdf.processor', 'pdf processor')
    import_module('tools.pdf.note_generator', 'pdf note_generator')

    # --- Transcript Processing ---
    print("\n=== Transcript Modules ===")
    import_module('tools.transcript', 'transcript package')
    import_module('tools.transcript.processor', 'transcript processor')

    # --- AI & Summarization ---
    print("\n=== AI Modules ===")
    import_module('tools.ai', 'ai package')
    import_module('tools.ai.summarizer', 'ai summarizer')

    # --- Metadata Processing ---
    print("\n=== Metadata Modules ===")
    import_module('tools.metadata', 'metadata package')
    import_module('tools.metadata.path_metadata', 'path_metadata')

    # --- Notes Modules ---
    print("\n=== Notes Modules ===")
    import_module('tools.notes', 'notes package')
    import_module('tools.notes.markdown_generator', 'markdown_generator')
    import_module('tools.notes.markdown_generator_fixed', 'markdown_generator_fixed')

    # --- Summary of results ---
    print("\n=== Import Summary ===")
    print(f"Total modules tested: {total_modules}")
    print(f"Total import time: {total_time:.4f} seconds")
    print(f"Average import time: {total_time / total_modules:.4f} seconds")
    
    # Sort modules by import time for performance analysis
    sorted_results = sorted(results, key=lambda x: x['time'], reverse=True)
    
    print("\n=== Top 5 Slowest Modules ===")
    for i, result in enumerate(sorted_results[:5]):
        status = "✅" if result['success'] else "❌"
        print(f"{i+1}. {status} {result['module']} - {result['time']:.4f} seconds")
    
    if errors == 0:
        print("\n🎉 All tools modules imported successfully!")
    else:
        print(f"\n❌ {errors} module(s) failed to import. Please check the error messages above.")
        sys.exit(1)

if __name__ == "__main__":
    test_all_modules()
