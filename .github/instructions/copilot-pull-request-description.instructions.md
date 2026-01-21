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

### Feature: Add video transcript consolidation

This PR implements recursive transcript consolidation for lesson folders, allowing users to combine individual video transcripts into comprehensive class-level notes.

### Changes

- Added `ConsolidateTranscriptsRecursively` method to `TranscriptConsolidator` service
- Implemented recursive folder scanning in `VaultService`
- Updated Obsidian plugin to support recursive consolidation toggle
- Added CLI option `--recursive` for transcript consolidation command
- Enhanced metadata extraction to include lesson hierarchy information

### Related Issues

Resolves #247
Related to #198

### Testing Instructions

1. Place test video transcripts in nested lesson folders within your vault
2. Run the consolidation command:
   ```bash
   notebook-automation consolidate-transcripts --path "./my-course" --recursive
   ```
3. Verify that:
   - A consolidated transcript is created at the class level
   - All nested lesson transcripts are included
   - Section headings are generated with friendly titles
   - Links to original transcripts are preserved

### Screenshots/Videos

![Consolidated Transcript Example](docs/images/consolidated-transcript.png)

### Deployment Notes

- No database migrations required
- New configuration option: `recursiveTranscriptConsolidation` (default: false)
- Backward compatible with existing non-recursive consolidation

### Checklist

- [x] Code follows project style guidelines
- [x] Documentation has been updated (README.md and plugin docs)
- [x] Unit tests added/updated (TranscriptConsolidatorTests)
- [x] Integration tests added/updated
- [x] Security implications have been considered
- [x] Performance impact has been evaluated (tested with 50+ transcripts)
- [ ] Breaking changes are clearly documented (N/A - backward compatible)