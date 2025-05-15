"""
Shared utilities for CLI scripts.

This module provides common functionality used across all CLI tools including color codes
for terminal output, logging configuration, and other helper functions. It standardizes
the look and feel of CLI tools and reduces code duplication.

Usage:
    from notebook_automation.cli.utils import OKGREEN, WARNING, remove_timestamps_from_logger
    
    print(f"{OKGREEN}Success!{ENDC}")
    remove_timestamps_from_logger(logger)
"""

import logging
from typing import Any

# ANSI color codes for CLI output
HEADER = '\033[95m'
OKBLUE = '\033[94m'
OKCYAN = '\033[96m'
OKGREEN = '\033[92m'
WARNING = '\033[93m'
FAIL = '\033[91m'
ENDC = '\033[0m'
BOLD = '\033[1m'
GREY = '\033[90m'
BG_BLUE = '\033[44m'

def remove_timestamps_from_logger(logger: logging.Logger) -> None:
    """Remove timestamps from all handlers of a logger.
    
    Configures all handlers of the given logger to use a formatter without timestamps,
    which provides cleaner output for CLI applications where the timestamp is usually
    not needed by the user.
    
    Args:
        logger (logging.Logger): The logger instance to modify.
        
    Returns:
        None
        
    Example:
        >>> import logging
        >>> logger = logging.getLogger("cli_logger")
        >>> remove_timestamps_from_logger(logger)
    """
    no_timestamp_formatter = logging.Formatter('%(levelname)s - %(message)s')
    for handler in logger.handlers:
        # Handle RichHandler differently as it has its own formatting system
        if handler.__class__.__name__ == 'RichHandler':
            # For Rich handlers, just use the message portion
            handler.setFormatter(logging.Formatter('%(message)s'))
        else:
            handler.setFormatter(no_timestamp_formatter)
    
    # Don't propagate to the parent logger to prevent duplicate entries
    logger.propagate = False

# Add more shared CLI helpers as needed
