# Test Suite for MBA Notebook Automation

This directory contains test scripts for verifying the functionality of the MBA Notebook Automation system. These tests help ensure that various components of the system are working correctly and help identify issues when making changes to the codebase.

## Key Test Categories

- **Module Import Tests**: Verify that all modules can be imported correctly
- **Functionality Tests**: Check specific functions and features
- **Integration Tests**: Test how components work together
- **Configuration Tests**: Validate configuration loading
- **Authentication Tests**: Test OneDrive and API authentication flows
- **YAML Processing Tests**: Check YAML formatting and handling

## Running Tests

Most tests can be run directly with Python:

```bash
python tests/test_tools_packages.py
python tests/test_transcript_processor.py
```

Some tests may require access to the Obsidian vault or OneDrive resources. Make sure your environment is properly set up before running these tests.

## Test Scripts Overview

- **test_tools_packages.py**: Tests importing of all modules in the tools package
- **test_tools_modules.py**: Tests importing specific modules
- **test_transcript_processor.py**: Tests the transcript finding and processing functionality
- **test_pdf_modules.py**: Tests the PDF processing modules
- **test_yaml_formatting.py**: Tests YAML formatting preservation
- **test_config.py**: Tests configuration loading
- **test_authenticate_troubleshoot.py**: Tests authentication flows

## Adding New Tests

When adding new functionality to the system, please create corresponding test scripts in this directory. Follow the existing naming convention of `test_<component>.py`.
