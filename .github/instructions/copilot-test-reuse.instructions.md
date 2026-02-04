---
description: Guidelines for reusing test fixtures and utilities in the Notebook Automation project.
applyTo: "**/*.cs"
---

# GitHub Copilot Test Reuse Instructions

## Test Code Reuse Strategy

### 1. Reuse Test Fixtures and Utilities

- First check for existing test fixtures in the `tests/fixtures/` directory (repository root level)
- Leverage existing mock objects, test data generators, and assertion helpers within test classes
- Extend existing test base classes rather than creating new ones
- Note: Test fixtures are stored at repository root (`tests/fixtures/`), not within the test project

#### C# Example

```csharp
[TestClass]
public class MyServiceTests
{
    private readonly Mock<IDependency> _mockDependency;
    private readonly MyService _sut;

    public MyServiceTests()
    {
        // Arrange - setup mocks
        _mockDependency = new Mock<IDependency>();
        _sut = new MyService(_mockDependency.Object);
    }

    [TestMethod]
    public void ProcessData_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        var testData = "test input";
        _mockDependency.Setup(x => x.Validate(testData)).Returns(true);
        
        // Act
        var result = _sut.ProcessData(testData);
        
        // Assert
        Assert.IsTrue(result);
        _mockDependency.Verify(x => x.Validate(testData), Times.Once);
    }
}
```

### 2. Test Structure and Organization

- Follow the existing test organization pattern in the codebase:
  - `src/c-sharp/NotebookAutomation.Tests/Core/` - Core library tests
  - `src/c-sharp/NotebookAutomation.Tests/Cli/` - CLI-specific tests
- Place new tests in the same hierarchical structure that mirrors the main code
- Use one test file per class/service being tested
- Test fixtures and sample data files are stored in `tests/fixtures/` at repository root

### 3. Test Helper Discovery Process

When implementing tests:

1. Analyze existing test classes for helper methods and setup patterns
2. Look for test initialization patterns in constructors or `[TestInitialize]` methods
3. Check for specialized assertion methods or verification utilities in existing tests
4. Identify mock object setup patterns and request simulations
5. Review `tests/fixtures/` for reusable test data files

### 4. Mock Object Reuse

- Use Moq framework for creating mock objects (already referenced in test project)
- Follow existing patterns for mock setup and verification
- Create reusable mock setup methods for complex dependencies
- Share mock configurations across related test classes when appropriate

### 5. Test Utility Creation Criteria

Only create new test utilities when:

- No suitable test helpers exist in the test suite
- Existing helpers would require excessive modification
- The test scenario is significantly different from existing ones
- New utility will be reusable across multiple test modules

### 6. Test Data Management

- Reuse test data files from `tests/fixtures/` directory
- Create new fixture files in `tests/fixtures/` for complex test scenarios
- Share test data across related test cases
- Use meaningful names for fixture files that indicate their purpose

### 7. Mock and Stub Reuse

- Use existing mock objects and service stubs from test setup
- Maintain consistent mock response patterns across tests
- Extend existing mocks with additional behaviors as needed
- Document mock behavior for complex scenarios

### 8. Integration Test Reuse

- Reuse test client configuration and setup from existing integration tests
- Leverage existing test configurations and environment setup
- Use established patterns for service testing, file operations testing, etc.
- Share common test infrastructure across integration test classes

### 9. Test Documentation Standards

When creating or extending tests:

- Add XML documentation comments explaining the test purpose
- Document which test utilities or fixtures were reused
- Explain any customizations to existing test patterns
- Note any patterns being established for future test reuse
- Use descriptive test method names following the pattern: `MethodName_Scenario_ExpectedBehavior`
