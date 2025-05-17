# 2. copilot-code-generation.md

```markdown
# GitHub Copilot Code Generation Instructions

## Code Structure
- Create modular, loosely coupled components
- Favor composition over inheritance
- Use appropriate design patterns when applicable
- Follow the Dependency Inversion Principle - depend on abstractions, not implementations

## Error Handling
- Use explicit try/except blocks with specific exception types
- Include contextual information in error messages
- Propagate exceptions appropriately (don't hide errors)
- Log errors with appropriate severity levels

## Performance Guidelines
- Prefer readable code over premature optimization
- Document performance-critical sections
- Select appropriate data structures for operations
- Include time/space complexity notes for algorithms when relevant

## Reusability
- Parameterize functions instead of hardcoding values
- Create pure functions when possible (no side effects)
- Use dependency injection where appropriate
- Design intuitive interfaces that minimize required knowledge

## Code Style
- Follow PEP 8 style guidelines
- Group imports in standard order: standard library, third-party, local
- Use meaningful whitespace to improve readability
- Include inline comments for complex logic or non-obvious implementations
- Explain "why" rather than "what" in comments

## Security Best Practices
- Never hardcode sensitive information (credentials, API keys)
- Sanitize user inputs before processing
- Use safe APIs for risky operations (file handling, network calls)
- Document security assumptions and requirements
- Use principle of least privilege when accessing resources

## Common Patterns to Implement
- Configuration management from environment variables
- Resource cleanup with context managers
- Logging with appropriate levels
- Factory methods for complex object creation
- Separation of data access from business logic