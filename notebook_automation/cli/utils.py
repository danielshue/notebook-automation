"""
Shared utilities for CLI scripts: color codes, logging setup, and helpers.
"""

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

import logging

def remove_timestamps_from_logger(logger):
    """Remove timestamps from all handlers of a logger."""
    no_timestamp_formatter = logging.Formatter('%(levelname)s - %(message)s')
    for handler in logger.handlers:
        handler.setFormatter(no_timestamp_formatter)
    logger.propagate = False

# Add more shared CLI helpers as needed
