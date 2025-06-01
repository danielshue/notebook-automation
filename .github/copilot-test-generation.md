# GitHub Copilot Test Generation Instructions

## Test Structure

- Follow the Arrange-Act-Assert pattern
- Each test should verify a single behavior or edge case
- Test name should clearly describe what is being tested
- Include setup and teardown as needed
- Use MSFT Test and Moq for testing frameworks

## Test Coverage

- Test happy paths and edge cases
- Include input validation tests
- Test error conditions and exception handling
- Test boundary conditions

## Best Practices

- Make tests independent of each other
- Avoid test interdependencies
- Use appropriate mocks and test doubles
- Keep tests fast and deterministic
- Test external dependencies via interfaces

## Example Test Structure (pytest)

```python
def test_function_should_behavior_when_condition():
    # Arrange
    test_input = setup_input()
    expected_output = define_expected_result()
    
    # Act
    actual_result = function_under_test(test_input)
    
    # Assert
    assert actual_result == expected_output