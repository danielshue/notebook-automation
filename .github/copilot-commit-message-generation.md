# copilot-commit-message-generation.md

```markdown
# GitHub Copilot Commit Message Instructions

## Commit Message Structure
- Format: `<type>(<scope>): <subject>`
- Example: `feat(auth): add OAuth2 authentication`

## Types
- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Formatting, missing semi-colons, etc; no code change
- **refactor**: Code change that neither fixes a bug nor adds a feature
- **perf**: Code change that improves performance
- **test**: Adding or modifying tests
- **chore**: Changes to build process or auxiliary tools

## Guidelines
- Use imperative, present tense: "add" not "added" or "adds"
- Don't capitalize first letter
- No period at the end
- Limit the first line to 72 characters
- Reference issues in the body of the commit

## Body Format
- Separate subject from body with a blank line
- Use the body to explain what and why vs. how
- Include task/issue references at the bottom

## Example Complete Commit Message
