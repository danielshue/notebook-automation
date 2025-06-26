---
description: Guidelines for generating tests with MSTest and Moq for the Notebook Automation project.
applyTo: "**"
---

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

## Example Test Structure (MSTest)

```csharp
[TestClass]
public class MyTestClass
{
    [TestMethod]
    public void Test_Method_Should_Behavior_When_Condition()
    {
        // Arrange
        var input = SetupInput();
        var expected = DefineExpectedResult();

        // Act
        var actual = MethodUnderTest(input);

        // Assert
        Assert.AreEqual(expected, actual);
    }
}
```