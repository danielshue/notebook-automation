---
description: Guidelines for reusing test fixtures and utilities in the Notebook Automation project.
applyTo: "**"
---

# GitHub Copilot Test Reuse Instructions

## Test Code Reuse Strategy

### 1. Reuse Test Fixtures and Utilities

- First check for existing test fixtures in `tests/fixtures` or similar directories
- Leverage existing mock objects, test data generators, and assertion helpers
- Extend existing test base classes rather than creating new ones

#### C# Example

```csharp
[TestClass]
public class MyTestClass : TestBaseFixture
{
    [TestMethod]
    public void Test_Using_Existing_Fixture()
    {
        // Arrange
        var user = UserFixture.Create();
        // Act
        var result = SomeService.DoSomething(user);
        // Assert
        Assert.IsTrue(result);
    }
}
```

### 2. Test Structure and Organization

- Follow the existing test organization pattern in the codebase
- Place new tests in the same hierarchical structure that mirrors the main code
- Reuse test file organization patterns (one test file per module, etc.)

### 3. Test Helper Discovery Process

When implementing tests:

1. Analyze existing test modules for helper functions and fixtures
2. Look for test setup/teardown patterns already established
3. Check for specialized assertion methods or verification utilities
4. Identify mock object factories and request simulations

### 4. Test Fixture Extension Guidelines

- Extend existing fixtures with additional parameters rather than creating new ones if possible
- Use fixture composition to combine multiple fixtures
- Create fixture factories that build upon base fixtures

### 5. Test Utility Creation Criteria

Only create new test utilities when:

- No suitable test helpers exist in the test suite
- Existing helpers would require excessive modification
- The test scenario is significantly different from existing ones
- New utility will be reusable across multiple test modules

### 6. Test Data Management

- Reuse test data creation factories
- Extend existing test datasets rather than creating new ones
- Share test data across related test cases

### 7. Mock and Stub Reuse

- Use existing mock objects and service stubs
- Maintain a library of common mock responses
- Extend existing mocks with additional behaviors as needed

### 8. Integration Test Reuse

- Reuse test client configuration and setup
- Leverage existing authenticated sessions and test environments
- Use established patterns for API testing, database testing, etc.

### 9. Test Documentation Standards

When creating or extending tests:

- Document which test utilities were reused
- Explain any customizations to existing test fixtures
- Note any patterns being established for future test reuse
