#!/usr/bin/env python3
"""
Test script to validate the logger configuration from config.py
"""
import sys
import os

# Add the project root directory to the Python path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from tools.utils.config import setup_logging

def main():
    # Set up logging with a custom log file
    logger, failed_logger = setup_logging(
        debug=True,
        log_file="test_logger.log",
        failed_log_file="test_logger_failed.log"
    )
    
    # Test the main logger
    logger.debug("This is a debug message")
    logger.info("This is an info message")
    logger.warning("This is a warning message")
    logger.error("This is an error message")
    
    # Test the failed logger
    failed_logger.info("This is a failed info entry")
    failed_logger.error("This is a failed error entry")
    
    print("Logger test complete. Check test_logger.log and test_logger_failed.log")

if __name__ == "__main__":
    main()
