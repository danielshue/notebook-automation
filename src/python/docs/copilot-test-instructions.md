# GitHub Copilot Test Generation Instructions

## Testing Philosophy for MBA Notebook Automation

- Write comprehensive test cases that validate both expected behavior and edge cases
- Prefer smaller, focused tests over large, complex tests
- Each test should have a clear purpose, testing one specific behavior
- Tests should be self-contained and not rely on external resources unless necessary
- Include proper setup and teardown to clean up after tests

## Test Organization

- Place tests in the `/tests` directory, following the existing structure:
  - General tests at the root level of `/tests`
  - Module-specific tests in subdirectories (e.g., `/tests/tags` for tag-related tests)
- Name test files with the prefix `test_` followed by the name of the module or functionality being tested
- Group related test cases in the same file

## Test Format and Style

- Use pytest as the testing framework
- Each test file should include a module-level docstring explaining its purpose
- Each test function should have a descriptive docstring explaining what it tests
- Follow the naming convention `test_<functionality>` for test functions
- Use descriptive variable names in tests to clarify their purpose

```python
def test_yaml_formatting_preserves_quotes():
    """Test that the yaml_to_string function preserves quotes when needed."""
    input_data = {"key": "value: with special characters"}
    result = yaml_to_string(input_data)
    assert "key: 'value: with special characters'" in result
```

## Test Content Requirements

- Include tests for both valid and invalid inputs
- Test boundary conditions and edge cases
- For complicated functions, test multiple input combinations
- Include performance tests for operations that might be resource-intensive
- Make assertions specific and clear about what is being verified

## Mocking and Test Data

- Use pytest fixtures for common test setup and resources
- Mock external dependencies (file systems, networks, APIs) when possible
- Use separate test data directories for test files
- For tests requiring sample files, create them programmatically in the test or use fixtures
- Avoid hard-coding paths; use Path objects and relative paths

```python
@pytest.fixture
def sample_markdown_file(tmp_path):
    """Create a sample markdown file with frontmatter for testing."""
    file_path = tmp_path / "sample.md"
    content = """---
title: Sample Document
tags: [mba/course/finance, mba/lecture/finance/1]
---

# Sample Content
This is test content.
"""
    file_path.write_text(content)
    return file_path
```

## Testing Obsidian and YAML Functionality

- Use `ruamel.yaml` for YAML tests to match the project's usage
- For Obsidian-related tests, focus on frontmatter parsing and modification
- Create realistic sample documents that mirror actual Obsidian notes
- Test tag manipulation functions thoroughly, as they are central to the project

## Testing Tag-Related Functionality

- Test the tag hierarchy parsing and generation
- Verify proper nesting of tags based on project conventions
- Test edge cases like malformed tags, empty tags, or conflicting hierarchies
- Ensure tag preservation when modifying document frontmatter

## Testing PDF and Video Processing

- Mock file operations when testing PDF processing
- Use small, sample PDFs for integration tests
- Test metadata extraction and preservation
- For video processing, mock API calls and use sample responses

## Error Handling Tests

- Test error scenarios explicitly
- Verify that appropriate exceptions are raised
- Test logging functionality where applicable
- Ensure proper cleanup happens even when errors occur

## OneDrive Integration Tests

- Mock OneDrive API calls in unit tests
- For integration tests requiring actual OneDrive access, use test credentials
- Test both successful operations and failure scenarios
- Mark tests requiring external resources with pytest markers:

```python
@pytest.mark.onedrive
def test_onedrive_file_access():
    """Test accessing files from OneDrive (requires authentication)."""
    # Test implementation here
```

## Continuous Integration Considerations

- Ensure tests can run in a CI environment
- Avoid tests that depend on specific local environment configurations
- Use environment variables or configuration files for necessary customization
- Add appropriate pytest markers to categorize tests

## Best Practices

- Keep tests simple and focused
- Use parameterized tests for testing multiple similar scenarios
- Write deterministic tests that produce consistent results
- Comment complex test logic to explain the testing strategy
- Aim for high test coverage of critical functionality
