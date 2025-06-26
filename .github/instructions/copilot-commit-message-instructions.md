---
applyTo: "**"
---

# GitHub Copilot Commit Message Instructions

## Commit Message Structure

- Format: `<type>(<scope>): <subject>`
- Example: `feat(auth): add OAuth2 authentication`

## Types with Emojis

- **feat**: ✨ New feature
- **fix**: 🐛 Bug fix
- **docs**: 📚 Documentation changes
- **style**: 💄 Formatting, missing semi-colons, etc; no code change
- **refactor**: ♻️ Code change that neither fixes a bug nor adds a feature
- **perf**: ⚡ Code change that improves performance
- **test**: ✅ Adding or modifying tests
- **chore**: 🔧 Changes to build process or auxiliary tools

## Emoji Guidelines

**ALWAYS include appropriate emojis in commit messages.**

### Type Emojis (Required)

- ✨ `:sparkles:` - feat: New features
- 🐛 `:bug:` - fix: Bug fixes
- 📚 `:books:` - docs: Documentation
- 💄 `:lipstick:` - style: Code styling
- ♻️ `:recycle:` - refactor: Code refactoring
- ⚡ `:zap:` - perf: Performance improvements
- ✅ `:white_check_mark:` - test: Tests
- 🔧 `:wrench:` - chore: Maintenance

### Additional Context Emojis (Optional)

- 🚀 `:rocket:` - Deploy/release related
- 🔥 `:fire:` - Remove code or files
- 📝 `:memo:` - Add/update documentation
- 🎨 `:art:` - Improve structure/format
- 🚧 `:construction:` - Work in progress
- 💥 `:boom:` - Breaking changes
- 🔒 `:lock:` - Security improvements
- 🌐 `:globe_with_meridians:` - Internationalization

## Commit Message Format

**Required Format**: `<emoji> <type>(<scope>): <subject>`
**Example**: `✨ feat(auth): add OAuth2 authentication`

## Guidelines

- Use imperative, present tense: "add" not "added" or "adds"
- Don't capitalize first letter
- No period at the end
- Limit the first line to 72 characters
- Reference issues in the body of the commit
- **ALWAYS start with the appropriate emoji for the commit type**

## Body Format

- Separate subject from body with a blank line
- Use the body to explain what and why vs. how
- Include task/issue references at the bottom

## Example Complete Commit Message

```
✨ feat(auth): add OAuth2 authentication

Implement OAuth2 authentication to support multiple providers.
This change adds support for Google, GitHub, and Microsoft authentication.

- Add OAuth2 provider configuration
- Implement token validation middleware
- Add user profile retrieval from providers
- Update authentication routes

Closes #123
Refs #124
```

## Additional Examples

```
🐛 fix(pdf): resolve memory leak in PDF processing
📚 docs(api): update authentication endpoints documentation
♻️ refactor(tags): simplify tag hierarchy generation logic
✅ test(vault): add integration tests for vault operations
🔧 chore(deps): update dependencies to latest versions
```
