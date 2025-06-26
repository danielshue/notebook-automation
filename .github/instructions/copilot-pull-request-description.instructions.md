---
description: Guidelines for creating consistent and informative pull request descriptions.
applyTo: "**"
---

# GitHub Copilot Pull Request Description Instructions

## Pull Request Title Format

- Format: `<type>: <short summary>`
- Example: `Feature: Add user authentication system`
- Keep titles concise and descriptive (max 72 characters)

## Types

- **Feature**: New functionality
- **Fix**: Bug resolution
- **Refactor**: Code improvements without changing functionality
- **Performance**: Performance improvements
- **Security**: Security-related changes
- **Docs**: Documentation updates
- **Test**: Test additions or modifications
- **Infrastructure**: Build process, CI/CD, tooling changes

## PR Description Structure

### Summary

- Brief explanation of what the PR accomplishes (1-3 sentences)
- Explain the "why" not just the "what"

### Changes

- Bullet-point list of major changes
- Note any significant architectural decisions
- Highlight any breaking changes

### Related Issues

- Link to related tickets, stories, or issues
- Use GitHub keywords to link issues: `Fixes #123`, `Resolves #456`, `Related to #789`

### Testing Instructions

- Step-by-step guide for how to test the changes
- Include any specific test scenarios that should be verified
- Note any environment setup requirements

### Screenshots/Videos (when applicable)

- Include visual evidence of UI changes
- Before/after comparisons if helpful

### Deployment Notes

- Any special considerations for deployment
- Required environment variables
- Database migrations or schema changes
- New dependencies

### Checklist (customize as needed)

- [ ] Code follows project style guidelines
- [ ] Documentation has been updated
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Security implications have been considered
- [ ] Performance impact has been evaluated
- [ ] Breaking changes are clearly documented

## Example Pull Request Description

