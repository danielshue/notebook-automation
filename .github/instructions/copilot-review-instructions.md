---
applyTo: "**"
---

# GitHub Copilot Code Review Instructions

## Review Focus Areas

### Functionality

- Does the code correctly implement the requirements?
- Are there any logical errors or edge cases not handled?
- Is input validation comprehensive?
- Are error cases properly handled?

### Design and Architecture

- Does the code follow SOLID principles?
- Is there appropriate separation of concerns?
- Is the code appropriately modular and reusable?
- Are dependencies properly managed?

### Performance

- Are there any potential performance bottlenecks?
- Are appropriate data structures and algorithms used?
- Are resources properly managed (connections, file handles, etc.)?
- Is there potential for optimization without sacrificing readability?

### Security

- Are inputs properly validated and sanitized?
- Are authentication and authorization handled correctly?
- Are secrets and credentials properly protected?
- Is data properly encrypted in transit and at rest where needed?

### Readability and Maintainability

- Is the code well-documented with appropriate comments?
- Do variable and function names clearly indicate their purpose?
- Is the code formatted consistently?
- Is there unnecessary complexity that could be simplified?

### Testing

- Are tests thorough and cover edge cases?
- Is there appropriate mocking of external dependencies?
- Do tests verify both positive and negative scenarios?
- Is test coverage sufficient?

## Review Comment Format

**Issue**: Brief description of the issue or suggestion
**Impact**: Why this matters (e.g., performance, security, maintainability)
**Suggestion**: Specific recommendation for improvement, with code example if applicable
**Reference**: Link to relevant documentation or best practice (if applicable)