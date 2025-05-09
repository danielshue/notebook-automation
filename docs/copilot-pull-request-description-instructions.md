# GitHub Copilot Pull Request Description Instructions

## Overview
These instructions guide GitHub Copilot in generating consistent, informative pull request descriptions for the MBA Notebook Automation project.

## PR Description Structure

### Title Format
- Begin with a category tag: `[Feature]`, `[Fix]`, `[Refactor]`, `[Docs]`, `[Test]`, or `[Chore]`
- Follow with a concise description of the primary change
- Example: `[Feature] Add nested tag generation for MBA course materials`

### Description Sections

#### 📝 Summary
- Provide a brief overview of what the pull request accomplishes
- Focus on the "what" and "why" rather than the "how"
- Relate the change to the project goals (MBA notebook organization, automation, etc.)

#### 🔍 Changes
- List specific changes in bullet points
- Group related changes under subheadings if necessary
- For each significant change, explain:
  - What was changed
  - Why it was changed
  - Impact on the overall system

#### 🧪 Testing
- Describe how the changes were tested
- Include:
  - Test methods used (unit tests, integration tests, manual testing)
  - Test coverage for new functionality
  - Any edge cases considered and tested
- If applicable, provide sample input/output scenarios

#### 📚 Documentation
- Note any documentation updates made
- If documentation updates are needed but not included, mention this
- Include references to related documentation or examples

#### 🔄 Related Issues
- Link to any relevant GitHub issues using syntax: `Closes #123`, `Addresses #456`
- Explain how this PR relates to each linked issue
- If part of a larger feature, explain where this PR fits in the sequence

## Project-Specific Guidelines

### Tag System Changes
- For changes related to the tag system:
  - Note any impact on existing tag hierarchies
  - Explain how changes align with the documented tag structure
  - Mention any migration steps for existing tagged content

### Obsidian Integration
- For changes related to Obsidian integration:
  - Explain compatibility with current Obsidian version
  - Note any template or plugin dependencies
  - Describe any changes to expected vault structure

### Script Enhancements
- For new or modified automation scripts:
  - List any new dependencies introduced
  - Describe command-line interface changes
  - Provide example usage if applicable

### Breaking Changes
- Clearly highlight any breaking changes with a dedicated section
- Explain migration steps or workarounds for users
- Justify why the breaking change was necessary

## Example Pull Request Description

```markdown
# [Feature] Add Video Transcript Processing to MBA Note Generator

## 📝 Summary
This PR enhances the note generation process by adding support for video transcript processing. It allows the system to automatically extract key points from lecture videos and incorporate them into the generated MBA notes.

## 🔍 Changes
- Added new `process_transcript_stage2.py` script for advanced transcript processing
- Enhanced video metadata extraction to include transcript timestamps
- Updated configuration system to support transcript processing options
- Integrated transcript processing into the main note generation workflow
- Added caching for processed transcripts to improve performance

## 🧪 Testing
- Added unit tests for transcript processing functions
- Created sample video files and transcripts for testing
- Manually verified output for 3 different MBA course videos
- Edge case testing for videos without proper transcripts

## 📚 Documentation
- Updated README.md with new transcript processing features
- Added example commands to the documentation
- Created new section in MBA-Tag-System-Documentation.md for video-related tags

## 🔄 Related Issues
- Closes #42: Add support for video transcript processing
- Addresses #39: Improve note generation from multimedia sources

## Note
This PR implements the first phase of video processing improvements. Future PRs will add support for speaker identification and slide content extraction.
```

## Review Expectations
- Pull requests should have at least one approving review before merging
- Code should pass all automated tests
- Documentation should be updated to reflect changes
- Breaking changes should be clearly communicated
