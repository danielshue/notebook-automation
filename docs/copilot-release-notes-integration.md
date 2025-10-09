# GitHub Copilot CLI Release Notes Integration

## Overview

The release notes system uses GitHub Copilot CLI to generate professional, AI-powered release notes with proper emoji formatting and categorization. **GitHub Copilot CLI is required** - the script will fail if it's not installed.

## Architecture

```mermaid
flowchart TD
    A[New-AIReleaseNotes] --> B{Copilot CLI Available?}
    B -->|No| C[ERROR: Copilot CLI Required]
    B -->|Yes| D[Load Prompt Template]
    D --> E{Template Exists?}
    E -->|No| F[ERROR: Template Required]
    E -->|Yes| G[Replace {{COMMITS}} Placeholder]
    G --> H[Save to Temp File]
    H --> I[Execute: copilot -p]
    I --> J{Timeout 60s}
    J -->|Timeout| K[ERROR: Timeout]
    J -->|Success| L[Parse & Clean Output]
    L --> M{Valid Output?}
    M -->|No| N[ERROR: Invalid Output]
    M -->|Yes| O[Return AI Notes]
```

## Prerequisites

### Required Software

1. **GitHub Copilot CLI**
   ```powershell
   npm install -g @github/copilot
   ```

2. **Node.js** v22+ (for Copilot CLI)
   - Download from https://nodejs.org/

3. **GitHub Copilot Subscription**
   - Individual, Business, or Enterprise plan
   - Authenticate with `gh auth login`

### Required Files

- `scripts/release-notes-prompt.md` - Prompt template with structured sections

- Prompt stored in `config/release-notes-prompt.md`
- Easy to customize without changing code
- Uses `{{COMMITS}}` placeholder for commit injection
## Key Features

### 1. Template-Driven Prompts

- Prompt stored in `scripts/release-notes-prompt.md`
- Easy to customize without changing code
- Uses `{{COMMITS}}` placeholder for commit injection
- Structured sections: Features, Fixes, Improvements, Breaking Changes, Dependencies, Security

### 2. GitHub Copilot CLI Integration

- **Command**: `copilot -p "prompt text"`
- **Programmatic Mode**: Non-interactive execution via `-p` flag
- **Timeout Protection**: 60-second limit prevents hanging
- **Output Filtering**: Removes usage stats, quotas, authentication messages
- **Error Handling**: Clear error messages if Copilot unavailable or fails

## Usage

### Basic Usage

```powershell
New-AIReleaseNotes -Version "0.1.0-beta.30" -Type "beta" -RepoRoot "D:\source\notebook-automation"
```

### Integrated in Release Process

The function is automatically called during release creation:

```powershell
.\scripts\manage-version.ps1 -Version "0.1.0-beta.30" -Type beta -CreateRelease -PreRelease
```

## Customizing the Prompt

Edit `scripts/release-notes-prompt.md` to:

- Change section names/emojis
- Add new categories
- Modify formatting rules
- Adjust commit filtering logic

Example customization:

```markdown
## SECTIONS

### 🎨 UI/UX Changes
For visual and user experience improvements

### ⚡ Performance  
For speed and optimization enhancements
```

## Output Examples

### Copilot CLI Generated

```markdown
### ✨ New Features

- AI-powered release notes generation using GitHub Copilot CLI integration
- Template-driven prompt engineering for consistent output

### 🐛 Bug Fixes

- Fixed timeout handling in Copilot job execution
- Corrected output filtering regex patterns

### 🔧 Improvements

- Enhanced error messages with specific fallback reasons
- Optimized commit log retrieval for large histories
```

### Output Example (from Copilot CLI)

```markdown
### ✨ New Features

- AI-powered release notes generation using GitHub Copilot CLI integration
- Template-driven prompt engineering for consistent output

### 🐛 Bug Fixes  

- Improved AI release notes generation quality

### 🔧 Improvements

- Enhanced error handling
- Optimized commit processing
```

## Technical Details

### Commit Range Detection

Compares against the last release of the **same type**:

- Beta releases compare to previous beta
- Stable releases compare to previous stable
- Limits to 50 commits max for performance

### Timeout Implementation

Uses PowerShell `Start-Job` with `Wait-Job -Timeout 60`:

```powershell
$copilotJob = Start-Job -ScriptBlock {
    param($PromptFile)
    $content = Get-Content $PromptFile -Raw
    & copilot -p $content 2>&1
} -ArgumentList $tempPrompt

$completed = Wait-Job $copilotJob -Timeout 60
```

### Output Cleaning

Removes Copilot CLI metadata:

```powershell
$outputText -replace "(?m)^.*?authenticat.*$", ""
$outputText -replace "(?m)^.*?premium request.*$", ""
$outputText -replace "(?m)^.*?quota.*$", ""
$outputText -replace "(?m)^.*?Total usage.*$", ""
$outputText -replace "(?m)^.*?Total duration.*$", ""
$outputText -replace "(?m)^.*?Usage by model.*$", ""
```

## Troubleshooting

### Copilot CLI Not Found

**Error Message:**
```text
ERROR: GitHub Copilot CLI is not installed. Install with: npm install -g @github/copilot
```

**Solution**: Install GitHub Copilot CLI
```powershell
npm install -g @github/copilot
```

### Prompt Template Missing

**Error Message:**
```text
ERROR: Prompt template not found at scripts/release-notes-prompt.md
```

**Solution**: Ensure the template file exists at `scripts/release-notes-prompt.md`

### Timeout Issues

**Error Message:**
```text
ERROR: Failed to generate release notes - Copilot CLI timeout
```

**Solutions**:
- Reduce commit count (already limited to 50)
- Check network connectivity
- Verify Copilot subscription is active

### Empty Output

**Error Message:**
```text
ERROR: Failed to generate release notes - output was too short or empty
```

**Solutions**:
- Verify prompt template exists and is valid
- Check Copilot subscription status
- Ensure commits contain meaningful messages
- Review Copilot CLI authentication with `gh auth status`

### Installation Verification

Check if everything is installed correctly:

```powershell
# Check Node.js
node --version  # Should be v22+

# Check npm
npm --version

# Check Copilot CLI
copilot --version

# Check GitHub CLI authentication
gh auth status
```

## Future Enhancements

- [ ] Configurable timeout duration
- [ ] Cache recent generations to reduce API calls
- [ ] Multi-language support for release notes
- [ ] Custom emoji mappings via config file
- [ ] Integration with GitHub Issues for context
- [ ] Support for multiple Copilot models
- [ ] Retry logic with exponential backoff
