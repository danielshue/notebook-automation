# GitHub Copilot Commit Message Instructions

## Commit Message Structure

### Format
```
<type>(<scope>): <short summary>

<body>

<footer>
```

### Type
Must be one of the following:

- **feat**: A new feature
- **fix**: A bug fix
- **docs**: Documentation changes only
- **style**: Changes that don't affect code functionality (formatting, etc.)
- **refactor**: Code changes that neither fix bugs nor add features
- **perf**: Performance improvements
- **test**: Adding or modifying tests
- **chore**: Maintenance tasks, dependency updates, etc.
- **build**: Changes to build system or external dependencies
- **ci**: Changes to CI configuration or scripts

### Scope (Optional)
The scope provides additional contextual information about the area of the change:

- **tags**: Changes to tag management scripts or functionality
- **obsidian**: Obsidian-specific functionality
- **pdf**: PDF processing related changes
- **video**: Video processing related changes
- **auth**: Authentication related code
- **utils**: Utility scripts or functions
- **config**: Configuration related changes
- **docs**: Documentation updates
- **onedrive**: OneDrive integration related changes
- **tests**: Test-related changes

### Short Summary
- Written in imperative, present tense: "change" not "changed" nor "changes"
- First letter not capitalized
- No period at the end
- Maximum 72 characters
- Clearly describe what the commit does, not what was done

### Body (Optional)
- Use to explain the motivation for the change
- Include relevant background information
- Wrap at 72 characters
- Use multiple paragraphs if needed
- Use imperative, present tense

### Footer (Optional)
- Reference GitHub issues: "Fixes #123", "Closes #456"
- Note breaking changes: "BREAKING CHANGE: <description>"

## MBA Project-Specific Guidelines

### Tag Related Changes
When committing changes to tag-related scripts:
- Mention specific tag structures affected
- Note any backward compatibility considerations

### Obsidian Template Changes
For changes to Obsidian templates:
- Reference the template type being modified
- Note any changes to required metadata

### PDF/Video Processing
For commits affecting content processing:
- Note any performance impacts
- Mention file formats affected

## Examples

### Feature Addition
```
feat(tags): implement hierarchical tag generation

Add support for generating nested tag structures from existing tags
in Obsidian vault notes. This improves organization of MBA course
materials by automatically creating parent-child relationships.

Closes #42
```

### Bug Fix
```
fix(pdf): resolve encoding issues in PDF text extraction

Fix character encoding problems when extracting text from PDF files
containing special characters. Previously some non-ASCII characters
were being corrupted during extraction.

Fixes #78
```

### Documentation
```
docs: update onboarding guide with new tag structure

Expand the onboarding documentation to reflect the updated tag
hierarchy for MBA courses and provide examples of proper tag usage.
```

### Refactor
```
refactor(utils): consolidate duplicate file processing functions

Combine redundant file handling functions into a single utility
module to reduce code duplication and improve maintainability.
```

## Best Practices

1. **Be Specific**: Make commit messages detailed enough to understand the change without looking at the code
2. **Atomic Commits**: Each commit should address a single concern
3. **Consistency**: Follow the format consistently across the project
4. **Reference Issues**: Always reference related issues when applicable
5. **Context**: Provide context for why the change was made, not just what was changed
6. **No Jargon**: Avoid project-specific acronyms or jargon unless it's widely understood
7. **No Redundant Information**: Don't repeat what can be seen from the diff (like "updated file X")

## Anti-Patterns to Avoid

- Vague messages like "fix bug" or "update code"
- Overly technical implementation details
- Non-imperative voice (e.g. "fixed" instead of "fix")
- Messages that require understanding the code to know what changed
- Mixing multiple unrelated changes in a single commit
