#!/usr/bin/env python3
"""
WSL-Windows Path Testing Script

This script tests different approaches for dealing with cross-filesystem permissions
issues when writing files from WSL to Windows paths. It's designed to help diagnose
and fix permission issues encountered in the MBA notebook automation project.
"""

import os
import sys
import logging
import tempfile
import subprocess
import shutil
from pathlib import Path

# Configure logging
logging.basicConfig(
    level=logging.DEBUG,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

# Add the project root to the Python path to import our modules
project_root = str(Path(__file__).parent.parent.absolute())
if project_root not in sys.path:
    sys.path.append(project_root)


def is_wsl():
    """Check if running in Windows Subsystem for Linux."""
    try:
        with open('/proc/version', 'r') as f:
            return 'microsoft' in f.read().lower() or 'wsl' in f.read().lower()
    except:
        return False


def windows_path_to_wsl(win_path):
    """Convert Windows path to WSL path.
    
    Args:
        win_path (str): Windows path like C:\\Users\\...
    
    Returns:
        str: WSL path like /mnt/c/Users/...
    """
    if not win_path or len(win_path) < 3:
        return win_path
        
    # Handle paths with drive letters (C:\path\to\file)
    if win_path[1] == ':':
        drive = win_path[0].lower()
        path = win_path[2:].replace('\\', '/')
        return f"/mnt/{drive}{path}"
    return win_path


def wsl_path_to_windows(wsl_path):
    """Convert WSL path to Windows path.
    
    Args:
        wsl_path (str): WSL path like /mnt/c/Users/...
    
    Returns:
        str: Windows path like C:\\Users\\...
    """
    if not wsl_path.startswith('/mnt/'):
        return wsl_path
        
    drive = wsl_path[5].upper()
    path = wsl_path[6:].replace('/', '\\')
    return f"{drive}:{path}"


def test_write_with_powershell(file_path, content):
    """Test writing to a file using PowerShell."""
    logger.info(f"Testing writing to {file_path} with PowerShell...")
    
    # Create a temporary file with the content
    with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', delete=False, suffix='.md') as temp:
        temp.write(content)
        temp_path = temp.name
    
    # Convert path if needed
    if file_path.startswith('/mnt/'):
        windows_path = wsl_path_to_windows(file_path)
    else:
        windows_path = file_path
        
    # Ensure directory exists
    dir_path = os.path.dirname(windows_path)
    
    # Create directory with PowerShell
    ps_mkdir_cmd = f'powershell.exe -Command "if (!(Test-Path -Path \\\"{dir_path}\\\")) {{ New-Item -ItemType Directory -Force -Path \\\"{dir_path}\\\" }}"'
    logger.debug(f"PowerShell mkdir command: {ps_mkdir_cmd}")
    subprocess.run(ps_mkdir_cmd, shell=True, check=False)
    
    # Write file with PowerShell
    ps_cmd = f'powershell.exe -Command "$content = Get-Content -Raw \\\"{temp_path}\\\"; Set-Content -Path \\\"{windows_path}\\\" -Value $content -Encoding UTF8 -Force"'
    logger.debug(f"PowerShell write command: {ps_cmd}")
    result = subprocess.run(ps_cmd, shell=True, check=False)
    
    # Clean up temp file
    os.unlink(temp_path)
    
    if result.returncode == 0:
        logger.info("✓ PowerShell write successful")
        return True
    else:
        logger.error(f"✗ PowerShell write failed with code {result.returncode}")
        return False


def test_write_with_cmdexe(file_path, content):
    """Test writing to a file using CMD.EXE copy command."""
    logger.info(f"Testing writing to {file_path} with CMD.EXE...")
    
    # Create a temporary file with the content
    with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', delete=False) as temp:
        temp.write(content)
        temp_path = temp.name
    
    # Convert path if needed
    if file_path.startswith('/mnt/'):
        windows_path = wsl_path_to_windows(file_path)
    else:
        windows_path = file_path
    
    # Ensure directory exists
    dir_path = os.path.dirname(windows_path)
    cmd_mkdir = f'cmd.exe /c mkdir "{dir_path}" 2> nul'
    logger.debug(f"CMD mkdir command: {cmd_mkdir}")
    subprocess.run(cmd_mkdir, shell=True, check=False)
    
    # Use the Windows copy command
    cmd_copy = f'cmd.exe /c copy /Y "{temp_path}" "{windows_path}"'
    logger.debug(f"CMD copy command: {cmd_copy}")
    result = subprocess.run(cmd_copy, shell=True, check=False)
    
    # Clean up the temporary file
    os.unlink(temp_path)
    
    if result.returncode == 0:
        logger.info("✓ CMD.EXE copy successful")
        return True
    else:
        logger.error(f"✗ CMD.EXE copy failed with code {result.returncode}")
        return False


def test_write_with_direct_python(file_path, content):
    """Test writing to a file directly with Python."""
    logger.info(f"Testing writing to {file_path} with Python...")
    
    try:
        # Ensure directory exists
        dir_path = os.path.dirname(file_path)
        os.makedirs(dir_path, exist_ok=True)
        
        # Write the file
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
            
        logger.info("✓ Direct Python write successful")
        return True
    except Exception as e:
        logger.error(f"✗ Direct Python write failed: {e}")
        return False


def test_write_with_shutil_copy(file_path, content):
    """Test writing using Python's shutil.copy."""
    logger.info(f"Testing writing to {file_path} with shutil.copy...")
    
    try:
        # Create a temporary file with the content
        with tempfile.NamedTemporaryFile(mode='w', encoding='utf-8', delete=False) as temp:
            temp.write(content)
            temp_path = temp.name
        
        # Ensure directory exists
        dir_path = os.path.dirname(file_path)
        os.makedirs(dir_path, exist_ok=True)
        
        # Copy the file
        shutil.copy2(temp_path, file_path)
        
        # Clean up
        os.unlink(temp_path)
        
        logger.info("✓ shutil.copy write successful")
        return True
    except Exception as e:
        logger.error(f"✗ shutil.copy write failed: {e}")
        return False


def main():
    """Run tests for all the different file writing approaches."""
    if not is_wsl():
        logger.warning("This script is designed to test WSL-to-Windows path issues. You're not running in WSL.")
    
    # Test content to write
    content = """# Test File
    
This is a test file generated by the WSL-Windows Path Testing Script.
Testing WSL to Windows path permissions.
"""
    
    # Create test directories relative to your HOME directory in Windows
    test_paths = [
        "/mnt/c/temp/mba-test/test1.md",                            # Simple path
        "/mnt/c/Users/Public/Documents/mba-test/test2.md",          # Public folder
        "/mnt/c/Users/Public/mba-test/path/with/multiple/levels/test3.md",  # Deep path
    ]
    
    # Try to determine the user's Windows home directory
    try:
        windows_username = os.environ.get('USER') or os.environ.get('USERNAME')
        if windows_username:
            # Add a path in the user's home directory
            test_paths.append(f"/mnt/c/Users/{windows_username}/Documents/mba-test/test4.md")
    except Exception:
        pass
    
    # Track which methods work for which paths
    results = {}
    
    # Test each path with each method
    for path in test_paths:
        logger.info(f"\n=== Testing path: {path} ===")
        path_results = {
            "PowerShell": test_write_with_powershell(path, content),
            "CMD.EXE": test_write_with_cmdexe(path, content),
            "Direct Python": test_write_with_direct_python(path, content),
            "shutil.copy": test_write_with_shutil_copy(path, content)
        }
        results[path] = path_results
        
        # Clean up if any method succeeded
        if any(path_results.values()):
            try:
                logger.info(f"Cleaning up test file: {path}")
                os.unlink(path)
            except:
                pass
    
    # Print summary
    logger.info("\n=== RESULTS SUMMARY ===")
    for path, methods in results.items():
        logger.info(f"Path: {path}")
        for method, success in methods.items():
            status = "✓ Success" if success else "✗ Failed"
            logger.info(f"  {method}: {status}")


if __name__ == "__main__":
    main()
