#!/usr/bin/env python3
"""
Helper script for fixing logging in generate_video_meta.py
"""

import sys
from pathlib import Path

# Get path to generate_video_meta.py
file_path = Path("d:/repos/mba-notebook-automation/notebook_automation/cli/generate_video_meta.py")

if not file_path.exists():
    print(f"Error: Could not find {file_path}")
    sys.exit(1)

# Read the file content
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the problematic section
old_section = """    config = config_utils.load_config_data(args.config)
    
    # Create Rich console for all output
    console = Console()
      # Configure Rich logging handler with version-compatible parameters
    rich_handler_kwargs = {
        "console": console
    }
    # Only add parameters supported by the installed rich version
    if hasattr(RichHandler, "__init__"):
        if "rich_tracebacks" in RichHandler.__init__.__code__.co_varnames:
            rich_handler_kwargs["rich_tracebacks"] = True
        if "markup" in RichHandler.__init__.__code__.co_varnames:
            rich_handler_kwargs["markup"] = True
            
    # Set up basic logging with compatible RichHandler
    logging.basicConfig(
        level=logging.DEBUG if args.debug else logging.INFO,
        format="%(message)s",
        handlers=[RichHandler(**rich_handler_kwargs)]
    )
      # Set up enhanced logging with our custom configuration
    logger, failed_logger = setup_logging(debug=args.debug, use_rich=True)"""

new_section = """    config = config_utils.load_config_data(args.config)
    
    # Create Rich console for all output
    console = Console()
    
    # Set up enhanced logging with our custom configuration
    # The module logger is already initialized, but we need the full setup for consistent formatting
    logger, failed_logger = setup_logging(debug=args.debug, use_rich=True)"""

# Replace the content
new_content = content.replace(old_section, new_section)

# Write the updated file
with open(file_path, 'w', encoding='utf-8') as f:
    f.write(new_content)

print(f"Successfully updated {file_path}")
