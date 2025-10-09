# Release Notes Generation Prompt

Generate professional release notes from these git commits.

## REQUIREMENTS

- Use emojis for visual appeal (✨ 🐛 🔧 ⚠️ 📦 🎨 ⚡ 🔒 etc.)
- Organize into clear markdown sections with ### headers
- Use bullet points with - prefix
- Be concise and user-friendly
- Group related changes together
- Exclude version bump commits (like 'prepare v0.1.0-beta.X' or 'bump version')
- Exclude trivial commits (like 'update changelog', 'merge branch')
- Focus on user-facing changes and improvements
- Combine similar changes into single, clear bullet points

## SECTIONS

Use only the sections that have relevant changes:

### ✨ New Features

For new functionality, features, enhancements, and additions

### 🐛 Bug Fixes

For bug fixes, corrections, and resolved issues

### 🔧 Improvements

For refactoring, performance improvements, code quality, and chores

### ⚠️ Breaking Changes

For breaking changes or backwards-incompatible updates

### 📦 Dependencies

For dependency updates or changes

### 🔒 Security

For security fixes or improvements

## OUTPUT FORMAT

Output ONLY the categorized sections in markdown format.

Do NOT include:

- A release title or version header
- Preamble or introduction text
- Explanations or meta-commentary
- Empty sections

Start directly with the first ### section header.

## COMMITS

{{COMMITS}}
