# Notebook Automation CLI + GitHub Copilot SDK Integration

**Status:** Draft  
**Created:** 2026-01-24  
**Branch:** GHCP-SDK

---

## Branding & Welcome Experience

When chat mode starts, users are greeted with branded ASCII art, copyright notice, and product tagline:

```
$ na

  _   _       _       _                 _    
 | \ | | ___ | |_ ___| |__   ___   ___ | | __
 |  \| |/ _ \| __/ _ \ '_ \ / _ \ / _ \| |/ /
 | |\  | (_) | ||  __/ |_) | (_) | (_) |   < 
 |_| \_|\___/ \__\___|_.__/ \___/ \___/|_|\_\
     _         _                        _   _             
    / \  _   _| |_ ___  _ __ ___   __ _| |_(_) ___  _ __  
   / _ \| | | | __/ _ \| '_ ` _ \ / _` | __| |/ _ \| '_ \ 
  / ___ \ |_| | || (_) | | | | | | (_| | |_| | (_) | | | |
 /_/   \_\__,_|\__\___/|_| |_| |_|\__,_|\__|_|\___/|_| |_|

 © 2026 Daniel Shue | Your intelligent vault assistant
 
╭──────────────────────────────────────────────────────────────────╮
│  🤖 Powered by GitHub Copilot                                    │
│                                                                  │
│  I can help you manage your notes vault, convert documents,   │
│  organize notes, and answer questions about your content.        │
│                                                                  │
│  Commands: 'help' | 'exit' | 'clear' | 'history'                 │
╰──────────────────────────────────────────────────────────────────╯

You ❯ 
```

---

## Table of Contents

- [Branding & Welcome Experience](#branding--welcome-experience)
- [Overview](#overview)
- [Goals](#goals)
- [Current CLI Commands (Unchanged)](#current-cli-commands-unchanged)
- [New Behavior: No Arguments = Chat Mode](#new-behavior-no-arguments--chat-mode)
- [New CLI Commands](#new-cli-commands)
  - [na chat — Explicit Chat Mode Entry](#na-chat--explicit-chat-mode-entry)
  - [na ask — One-Shot Question Mode](#na-ask--one-shot-question-mode)
- [Chat Mode Capabilities](#chat-mode-capabilities)
  - [1. Question & Answer Over Vault Content](#1-question--answer-over-vault-content)
  - [2. Execute Existing Commands via Natural Language](#2-execute-existing-commands-via-natural-language)
  - [3. Multi-Step Workflow Automation](#3-multi-step-workflow-automation)
  - [4. Vault Search & Discovery](#4-vault-search--discovery)
  - [5. Content Generation & Enhancement](#5-content-generation--enhancement)
  - [6. Configuration Management via Chat](#6-configuration-management-via-chat)
- [Complete CLI Command Reference via Chat](#complete-cli-command-reference-via-chat)
  - [Vault Commands](#vault-commands-na-vault)
  - [Tag Commands](#tag-commands-na-tag)
  - [PDF Commands](#pdf-commands-na-pdf-notes)
  - [Video Commands](#video-commands-na-video-notes-na-video-transcripts)
  - [Markdown Generation Commands](#markdown-generation-commands-na-generate-markdown)
  - [Configuration Commands](#configuration-commands-na-config)
  - [OneDrive Commands](#onedrive-commands-na-refresh-token)
  - [Direct Command Execution](#direct-command-execution)
- [Chat Mode Built-in Commands](#chat-mode-built-in-commands)
- [Interactive Help & Guidance](#interactive-help--guidance)
- [Streaming Response Display](#streaming-response-display)
- [Model Selection](#model-selection)
- [Session Persistence](#session-persistence)
  - [First-Run Setup](#first-run-setup)
  - [Resuming Sessions](#resuming-sessions)
  - [Purging Sessions](#purging-sessions)
- [Error Handling & Graceful Degradation](#error-handling--graceful-degradation)
- [Configuration](#configuration)
- [Chat Input & Interaction](#chat-input--interaction)
  - [Keyboard Shortcuts](#keyboard-shortcuts)
  - [Multi-Line Input](#multi-line-input)
  - [File Attachments in Chat](#file-attachments-in-chat)
- [Offline & Network Handling](#offline--network-handling)
- [Logging & Telemetry](#logging--telemetry)
- [Accessibility](#accessibility)
- [Internationalization](#internationalization)
- [Chat Command Help](#chat-command-help)
- [Security Considerations](#security-considerations)
- [Example User Sessions](#example-user-sessions)
- [Technical Exploration: Copilot SDK as Processing Backend](#technical-exploration-copilot-sdk-as-processing-backend)
- [Future Enhancements (Out of Scope for v1)](#future-enhancements-out-of-scope-for-v1)
  - [UI Extensions (VS Code & Obsidian)](#ui-extensions-visual-studio-code--obsidian)
  - [MCP Server Integration](#mcp-server-integration)
  - [Custom Agents](#custom-agents)
- [Next Steps](#next-steps)

---

## Overview

This document outlines the proposed integration of the GitHub Copilot SDK into the Notebook Automation CLI (`na.exe` / `na`). The integration will provide an intelligent, conversational interface that allows users to interact with their notes vault using natural language while preserving all existing CLI commands.

---

## Goals

1. **Natural Language Interface** — Enable users to manage their vault through conversation
2. **Intelligent Reasoning** — Allow Copilot to search, read, and reason over vault content
3. **Backward Compatibility** — All existing CLI commands continue to work unchanged
4. **Progressive Enhancement** — Users can choose between traditional commands or chat mode

---

## Current CLI Commands (Unchanged)

All existing commands remain fully functional:

| Command                | Subcommands                                                                                                                       |
| ---------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| `na config`            | `view`, `update`, `validate`, `list-keys`, `secrets`, `display-secrets`                                                           |
| `na tag`               | `add-nested`, `clean-index`, `consolidate`, `restructure`, `add-example`, `metadata-check`, `update-frontmatter`, `diagnose-yaml` |
| `na vault`             | `generate-index`, `ensure-metadata`, `clean-index`, `vault-sync`                                                                  |
| `na video-notes`       | _(video processing options)_                                                                                                      |
| `na video-transcripts` | `consolidate`                                                                                                                     |
| `na pdf-notes`         | _(PDF extraction options)_                                                                                                        |
| `na generate-markdown` | _(HTML/TXT/EPUB conversion options)_                                                                                              |
| `na refresh-token`     | _(OneDrive authentication)_                                                                                                       |

---

## New Behavior: No Arguments = Chat Mode

### Current Behavior

```bash
na                    # Shows help/usage information
```

### New Behavior

```bash
na                    # Enters interactive Copilot chat mode
na --help             # Shows help/usage information (preserved)
```

### Chat Mode Entry Screen

The full welcome experience is displayed when entering chat mode (see [Branding & Welcome Experience](#branding--welcome-experience) for the complete ASCII art banner). This provides users with context about capabilities and available commands.

> **Note:** Users who want the traditional help output can use `na --help`.

---

## New CLI Commands

### `na chat` — Explicit Chat Mode Entry

```bash
na chat                      # Enter interactive chat (same as `na` with no args)
na chat --resume             # Resume previous conversation session
na chat --model <model>      # Start chat with specific model (e.g., gpt-5, claude-sonnet-4.5)
na chat --session <id>       # Resume a specific named session
```

### `na ask` — One-Shot Question Mode

For quick questions without entering interactive mode:

```bash
na ask "What notes do I have about machine learning?"
na ask "Summarize my finance course notes"
na ask "Which notes were modified this week?"
na ask "Find all notes tagged with #important"
```

**With Options:**

```bash
na ask "Summarize this note" --file "Notes/Finance/Chapter1.md"
na ask "What's in this folder?" --path "Courses/Data Science"
na ask --model gpt-5 "Explain the concept of compound interest from my notes"
```

---

## Chat Mode Capabilities

### 1. Question & Answer Over Vault Content

Copilot can search, read, and synthesize information from your notes:

```
You ❯ What topics are covered in my Finance course notes?

🤖 Based on your Finance course notes, I found coverage of these topics:

   📁 Finance/
   ├── Budgeting & Cash Flow (3 notes)
   ├── Investment Strategies (5 notes)
   ├── Tax Planning (2 notes)
   └── Retirement Planning (4 notes)

   Key concepts include: emergency funds, compound interest,
   asset allocation, and tax-advantaged accounts.

You ❯ Tell me more about what I learned about emergency funds

🤖 From "Chapter 3 - Emergency Funds.md":

   Your notes cover the 3-6 month expense rule, recommended
   account types (high-yield savings), and when to use vs.
   preserve emergency funds...
```

### 2. Execute Existing Commands via Natural Language

Users can describe what they want, and Copilot will execute the appropriate CLI commands:

```
You ❯ Convert all the HTML files in my "Data Science" course folder to markdown

🤖 I'll convert those files for you.

   [Executing: generate-markdown --path "Data Science/Course Content"]

   ✓ Processed: 15 files
   ✓ Failed: 0

   All HTML files have been converted to markdown in your vault.

You ❯ Now tag them all with #data-science and #course-notes

🤖 [Executing: tag update-frontmatter --path "Data Science" --key "tags" --value "#data-science, #course-notes"]

   ✓ Tagged 15 notes with #data-science, #course-notes
```

### 3. Multi-Step Workflow Automation

Copilot can plan and execute complex, multi-step operations:

```
You ❯ Organize my course notes by topic and create an index file

🤖 I'll help organize your notes. Here's my plan:

   1. Scan all course folders to understand content
   2. Identify common topics/themes
   3. Suggest a folder structure
   4. Move files (with your approval)
   5. Generate index files

   Should I proceed with step 1?

You ❯ Yes, go ahead

🤖 [Scanning folders...]

   I found 47 notes across 5 courses. Suggested organization:

   📁 Courses/
   ├── 📁 Programming/
   │   ├── Python (12 notes)
   │   └── JavaScript (8 notes)
   ├── 📁 Data/
   │   ├── Statistics (7 notes)
   │   └── Machine Learning (9 notes)
   └── 📁 Finance/
       └── Personal Finance (11 notes)

   Would you like me to reorganize the files this way?
```

### 4. Vault Search & Discovery

```
You ❯ Find all notes that mention both "Python" and "data analysis"

🤖 Found 7 notes mentioning both terms:

   1. Programming/Python/Data-Analysis-Basics.md
   2. Programming/Python/Pandas-Tutorial.md
   3. Data/Statistics/Python-Stats-Examples.md
   ...

   Would you like me to summarize any of these?

You ❯ What did I learn last week?

🤖 Based on notes modified between Jan 17-24:

   📝 New notes created: 4
   ✏️ Notes updated: 12

   Topics you worked on:
   • Machine Learning fundamentals (3 notes)
   • Python decorators (1 note)
   • Budget review Q1 (1 note)
```

### 5. Content Generation & Enhancement

```
You ❯ Create a summary note for my Machine Learning course

🤖 I'll create a summary by reading your ML course notes...

   [Reading 9 notes in "Courses/Machine Learning/"]

   Created: "Courses/Machine Learning/Course-Summary.md"

   The summary includes:
   • Key concepts covered
   • Important formulas
   • Links to individual topic notes
   • Study questions

You ❯ Add flashcard-style questions to my Python notes

🤖 I'll add review questions to each note...

   [Processing 12 Python notes]

   ✓ Added Q&A sections to 12 notes

   Each note now has 3-5 review questions based on the content.
```

### 6. Configuration Management via Chat

Users can view and update CLI configuration through natural language:

```
You ❯ Show me my current configuration

🤖 Here's your current Notebook Automation configuration:

   📁 Paths:
   • Vault Root: C:\Users\You\Documents\Notes\MyVault
   • OneDrive Root: C:\Users\You\OneDrive\Courses
   • Resources Base: Resources/

   🔧 Settings:
   • HTML Extensions: .html, .htm, .xhtml
   • Default Model: gpt-5
   • Streaming: enabled

   🔑 Secrets Status:
   • OpenAI API Key: ✓ configured
   • OneDrive Token: ✓ configured

   Run 'na config view' for full details, or ask me to change something.

You ❯ Change my vault root to D:\Notes\Vault

🤖 I'll update your vault root path.

   [Executing: config update paths.notebookVaultFullpathRoot "D:\Notes\Vault"]
   
   ✓ Updated vault root to: D:\Notes\Vault
   
   Note: Make sure this directory exists and contains your vault.

You ❯ What configuration keys are available?

🤖 Here are the available configuration keys:

   📁 Paths:
   • paths.notebookVaultFullpathRoot - Root path of your notes vault
   • paths.notebookVaultResourcesBasepath - Subdirectory for resources
   • paths.onedriveFullpathRoot - Root path of OneDrive sync folder
   • paths.onedriveResourcesBasepath - OneDrive resources subdirectory

   🤖 Copilot:
   • copilot.enabled - Enable/disable chat mode
   • copilot.defaultModel - Default AI model
   • copilot.streaming - Enable streaming responses

   🔧 Processing:
   • htmlExtensions - File extensions to treat as HTML

   Would you like me to update any of these?

You ❯ Validate my configuration

🤖 Running configuration validation...

   [Executing: config validate]

   ✓ Configuration file: valid JSON
   ✓ Vault root: exists and accessible
   ✓ OneDrive root: exists and accessible
   ⚠️ Warning: Resources directory doesn't exist (will be created on first use)
   ✓ Secrets: all required keys configured

   Overall: Configuration is valid with 1 warning.

You ❯ I moved my OneDrive folder, help me update the path

🤖 I can help you update your OneDrive path. Where is your OneDrive folder now?

You ❯ It's now at E:\Cloud\OneDrive

🤖 I'll update the OneDrive root path for you.

   [Executing: config update paths.onedriveFullpathRoot "E:\Cloud\OneDrive"]

   ✓ Updated OneDrive root to: E:\Cloud\OneDrive

   Let me verify the path exists...

   ✓ Path verified: E:\Cloud\OneDrive exists
   ✓ Found expected subdirectories: Courses/, Resources/

   Your OneDrive configuration is now updated and validated.
```

---

## Complete CLI Command Reference via Chat

All existing CLI commands are accessible through chat mode. Users can invoke them using natural language or the `!` prefix for direct execution.

### Vault Commands (`na vault`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na vault generate-index --path <path>` | "Generate index files for my vault"<br>"Create an index for the Finance folder" |
| `na vault ensure-metadata --path <path>` | "Ensure metadata consistency in my notes"<br>"Fix metadata in my course notes" |
| `na vault clean-index --path <path>` | "Remove all index files from my vault"<br>"Clean up index files in Data Science folder" |
| `na vault vault-sync` | "Sync my vault with OneDrive"<br>"Synchronize OneDrive to vault only" |

**Example interaction:**
```
You ❯ Generate index files for my entire vault and ensure metadata is consistent

🤖 I'll run both operations for you.

   [Executing: vault generate-index --path "/"]
   ✓ Generated 47 index files
   
   [Executing: vault ensure-metadata --path "/"]
   ✓ Updated metadata in 156 files
   ✓ 12 files were already consistent

You ❯ Sync my vault with OneDrive, but only pull new files from OneDrive

🤖 [Executing: vault vault-sync --direction onedrive-to-vault]
   
   ✓ Found 8 new files in OneDrive
   ✓ Synced to vault: 8 files
   ✓ No conflicts detected
```

### Tag Commands (`na tag`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na tag add-nested --path <path>` | "Add nested tags based on folder structure"<br>"Create hierarchical tags for my courses" |
| `na tag clean-index --path <path>` | "Remove tags from index files"<br>"Clean tags in index files" |
| `na tag consolidate --path <path>` | "Consolidate duplicate tags"<br>"Merge similar tags in my vault" |
| `na tag restructure --path <path>` | "Restructure my tags for consistency"<br>"Reorganize tags in Finance folder" |
| `na tag update-frontmatter --path <path> --key <key> --value <value>` | "Add status: draft to all new notes"<br>"Set author to 'Daniel' in my course notes" |
| `na tag diagnose-yaml --path <path>` | "Check for YAML issues in my notes"<br>"Diagnose frontmatter problems" |
| `na tag metadata-check --path <path>` | "Check metadata consistency"<br>"Verify frontmatter in my vault" |

**Example interaction:**
```
You ❯ Add the tag #course/finance to all notes in my Finance folder

🤖 [Executing: tag update-frontmatter --path "Finance" --key "tags" --value "#course/finance" --append]
   
   ✓ Updated 14 notes with tag #course/finance

You ❯ Now consolidate any duplicate tags in that folder

🤖 [Executing: tag consolidate --path "Finance"]
   
   Found duplicates:
   • #finance and #Finance → merged to #finance
   • #budget and #budgeting → merged to #budgeting
   
   ✓ Consolidated tags in 6 files

You ❯ Check if there are any YAML issues in my vault

🤖 [Executing: tag diagnose-yaml --path "/"]
   
   ⚠️ Found 3 files with YAML issues:
   
   1. Notes/Old/draft.md - Missing closing frontmatter delimiter
   2. Projects/idea.md - Invalid date format in 'created' field
   3. Archive/note.md - Duplicate 'tags' key
   
   Would you like me to fix these issues?
```

### PDF Commands (`na pdf-notes`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na pdf-notes --path <path>` | "Convert my PDF lectures to notes"<br>"Extract text from this PDF" |
| `na pdf-notes --path <path> --extract-images` | "Convert PDF with images"<br>"Extract PDF including diagrams" |
| `na pdf-notes --path <path> --output <output>` | "Convert PDFs and save to my Courses folder"<br>"Process PDFs into the Lectures directory" |

**Example interaction:**
```
You ❯ I have some PDF lecture slides in my Downloads folder. Convert them to notes with images.

🤖 Where would you like me to save the converted notes?

You ❯ Put them in Courses/Computer Science/Lectures

🤖 [Executing: pdf-notes --path "Downloads/*.pdf" --output "Courses/Computer Science/Lectures" --extract-images]
   
   Processing 12 PDF files...
   
   ✓ Lecture01.pdf → Lecture01.md (3 images extracted)
   ✓ Lecture02.pdf → Lecture02.md (5 images extracted)
   ✓ Lecture03.pdf → Lecture03.md (2 images extracted)
   ... (9 more)
   
   ✓ Converted 12 PDFs with 47 total images extracted
```

### Video Commands (`na video-notes`, `na video-transcripts`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na video-notes --url <url>` | "Create notes from this YouTube video"<br>"Process this video URL" |
| `na video-notes --path <path>` | "Generate notes from videos in this folder"<br>"Process local video files" |
| `na video-transcripts consolidate --path <path>` | "Consolidate video transcripts"<br>"Merge transcript files into one note" |

**Example interaction:**
```
You ❯ Create notes from this YouTube video: https://youtube.com/watch?v=abc123

🤖 [Executing: video-notes --url "https://youtube.com/watch?v=abc123"]
   
   📺 Fetching video: "Introduction to Machine Learning"
   📝 Extracting transcript...
   🤖 Generating summary and key points...
   
   ✓ Created: "Videos/Introduction to Machine Learning.md"
   
   Note includes:
   • Video metadata (title, channel, duration)
   • Full transcript with timestamps
   • AI-generated summary
   • Key takeaways

You ❯ Consolidate all the video transcripts in my ML course folder

🤖 [Executing: video-transcripts consolidate --path "Courses/Machine Learning/Videos"]
   
   Found 8 transcript files
   
   ✓ Created: "Courses/Machine Learning/Videos/Consolidated-Transcripts.md"
   
   The consolidated file includes all 8 videos organized by topic.
```

### Markdown Generation Commands (`na generate-markdown`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na generate-markdown --path <path>` | "Convert HTML files to markdown"<br>"Generate markdown from my course content" |
| `na generate-markdown --path <path> --extract-from-markdown` | "Extract HTML content from frontmatter path"<br>"Process markdown with OneDrive source" |
| `na generate-markdown --path <path> --no-share-links` | "Convert without OneDrive links"<br>"Generate markdown without share links" |

**Example interaction:**
```
You ❯ Convert all the HTML files in my "Data Science/Course Content" folder to markdown

🤖 [Executing: generate-markdown --path "Data Science/Course Content"]
   
   Found 23 HTML files
   
   Processing...
   ✓ Module1-Introduction.html → Module1-Introduction.md
   ✓ Module2-DataCleaning.html → Module2-DataCleaning.md
   ... (21 more)
   
   ✓ Converted 23 files to markdown
   ✓ All files saved to vault with proper frontmatter
```

### Configuration Commands (`na config`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na config view` | "Show my configuration"<br>"What are my current settings?" |
| `na config validate` | "Validate my configuration"<br>"Check if my config is correct" |
| `na config update <key> <value>` | "Change my vault path to..."<br>"Update the default model to gpt-5" |
| `na config list-keys` | "What settings can I change?"<br>"Show available configuration options" |
| `na config secrets` | "Show my secrets status"<br>"Are my API keys configured?" |

**Example interaction:**
```
You ❯ What configuration options are available?

🤖 [Executing: config list-keys]
   
   Available configuration keys:
   
   📁 Paths:
   • paths.notebookVaultFullpathRoot
   • paths.notebookVaultResourcesBasepath
   • paths.onedriveFullpathRoot
   • paths.onedriveResourcesBasepath
   
   🤖 Copilot:
   • copilot.enabled
   • copilot.defaultModel
   • copilot.streaming
   
   🔧 Processing:
   • htmlExtensions
   
   Which setting would you like to update?

You ❯ Change copilot.defaultModel to claude-sonnet-4.5

🤖 [Executing: config update copilot.defaultModel "claude-sonnet-4.5"]
   
   ✓ Updated copilot.defaultModel to: claude-sonnet-4.5
   
   This will take effect in your next chat session.
```

### OneDrive Commands (`na refresh-token`)

| CLI Command | Natural Language Examples |
|-------------|---------------------------|
| `na refresh-token` | "Refresh my OneDrive token"<br>"Reauthenticate with OneDrive" |

**Example interaction:**
```
You ❯ My OneDrive sync isn't working. Can you help?

🤖 Let me check your OneDrive configuration...

   ✓ OneDrive path configured: C:\Users\You\OneDrive
   ⚠️ Authentication token may be expired
   
   Would you like me to refresh your OneDrive token?

You ❯ Yes

🤖 [Executing: refresh-token]
   
   🔐 Opening browser for OneDrive authentication...
   
   ✓ Token refreshed successfully
   ✓ OneDrive connection verified
   
   Your OneDrive sync should now work correctly.
```

### Direct Command Execution

For power users who prefer exact CLI syntax, use the `!` prefix:

```
You ❯ !vault generate-index --path "Courses" --dry-run

🤖 [Executing: vault generate-index --path "Courses" --dry-run]
   
   Dry run - no changes will be made
   
   Would generate:
   • Courses/index.md
   • Courses/Finance/index.md
   • Courses/Data Science/index.md
   • Courses/Programming/index.md
   
   4 index files would be created

You ❯ !config view --debug

🤖 [Executing: config view --debug]
   
   [Debug output with full configuration details...]
```

---

## Chat Mode Built-in Commands

Available within chat mode:

| Command                               | Description                                                       |
| ------------------------------------- | ----------------------------------------------------------------- |
| `help`                                | Interactive help — ask about features, get tips, see examples     |
| `help <topic>`                        | Get help on a specific topic (e.g., `help tags`, `help sessions`) |
| `exit` or `quit`                      | Exit chat mode                                                    |
| `clear`                               | Clear conversation history (start fresh)                          |
| `history`                             | Show conversation history                                         |
| `model`                               | Show or change the current model                                  |
| `session save <name>`                 | Save current session with a name                                  |
| `session list`                        | List saved sessions                                               |
| `session load <name>`                 | Load a saved session                                              |
| `session purge --older-than <period>` | Purge old sessions (1d, 7d, 30d, 90d)                             |
| `!<command>`                          | Execute a CLI command directly (e.g., `!config view`)             |

---

## Interactive Help & Guidance

The `help` command provides intelligent, context-aware guidance about Notebook Automation:

### Basic Help

```
You ❯ help

🤖 Here's what I can help you with:

   📚 **Managing Your Vault**
   • Search and find notes
   • Organize folders and structure
   • Generate index files
   • Ensure metadata consistency

   📄 **Converting Content**
   • HTML → Markdown
   • PDF → Notes with extracted text/images
   • Video transcripts → Organized notes
   • EPUB → Markdown

   🏷️ **Tagging & Metadata**
   • Add/remove tags
   • Update frontmatter
   • Consolidate and restructure tags

   ⚙️ **Configuration**
   • View and update settings
   • Validate configuration
   • Manage paths and secrets

   💡 Type 'help <topic>' for details, or just ask me anything!

   Examples:
   • "help tags" — Learn about tagging features
   • "help convert" — Learn about file conversion
   • "What can you do with PDFs?"
```

### Topic-Specific Help

```
You ❯ help tags

🤖 **Tagging & Metadata Features**

   I can help you manage tags in your notes vault:

   🏷️ **Adding Tags**
   • "Add #finance tag to all notes in my Budget folder"
   • "Tag this note with #important #review"

   🔄 **Organizing Tags**
   • "Consolidate duplicate tags"
   • "Restructure my tags for consistency"
   • "Show me all tags in my vault"

   📝 **Frontmatter**
   • "Update the author field in my course notes"
   • "Add a 'status: draft' to new notes"
   • "Check metadata consistency"

   🔧 **CLI Commands Available**
   • na tag add-nested
   • na tag consolidate
   • na tag restructure
   • na tag update-frontmatter
   • na tag diagnose-yaml

   💡 **Tips:**
   • Use nested tags like #course/finance for organization
   • Run 'na tag diagnose-yaml' to find YAML issues
   • I can batch-update tags across multiple files

   Want me to help with any of these?
```

### Conversational Help

```
You ❯ How do I convert my lecture PDFs to notes?

🤖 Great question! Here's how to convert PDFs to notes:

   **Option 1: Ask me directly**
   Just tell me where your PDFs are:
   • "Convert the PDFs in my Downloads folder to notes"
   • "Process Lecture01.pdf and add it to my CS course"

   **Option 2: Use the CLI command**
```

na pdf-notes --path "path/to/pdfs" --output "Vault/Courses"

```

**What happens during conversion:**
1. Text is extracted from each PDF
2. Images are optionally extracted
3. Content is converted to clean markdown
4. Frontmatter is added (title, source, date)
5. Files are saved to your vault

**Tips:**
• Use `--extract-images` to include diagrams
• PDFs with selectable text work best
• Scanned PDFs may need OCR (limited support)

Would you like me to convert some PDFs now?

You ❯ What tips do you have for organizing a large vault?

🤖 Here are my top tips for organizing a large vault:

**📁 Folder Structure**
• Use a consistent hierarchy (e.g., Areas/Projects/Resources)
• Keep folder depth to 3-4 levels max
• Create an index.md in each folder

**🏷️ Tagging Strategy**
• Use nested tags: #project/active, #project/archived
• Limit to 3-5 tags per note
• Run `na tag consolidate` to clean up duplicates

**📝 Metadata Consistency**
• Use `na vault ensure-metadata` to standardize frontmatter
• Include: title, created, tags, status
• Use templates for new notes

**🔍 Findability**
• Use descriptive file names
• Add aliases in frontmatter for alternative names
• Regularly review and archive old notes

**🛠️ Maintenance**
• Run `na vault generate-index` monthly
• Use `na tag diagnose-yaml` to catch issues
• Back up before major reorganizations

Want me to help analyze your current vault structure?
```

### Quick Tips on Startup (Optional)

After the welcome banner, occasionally show a random tip:

```
╭──────────────────────────────────────────────────────────────────╮
│  🤖 Notebook Automation Assistant                                │
│  Powered by GitHub Copilot                                       │
╰──────────────────────────────────────────────────────────────────╯

💡 Tip: You can convert files by just describing what you want, like
   "Convert all HTMLs in my Data Science folder to markdown"

You ❯
```

---

## Streaming Response Display

Responses stream in real-time for a responsive experience:

```
You ❯ Summarize my notes on investment strategies

🤖 Based on your investment notes, here are the key strategies...
   |
   [cursor blinks as response streams in]
```

---

## Model Selection

The default model is determined by the Copilot CLI — users don't need to configure anything to get started. However, users can override the model if desired:

```bash
# Via command line
na chat --model gpt-5
na chat --model claude-sonnet-4.5
na ask --model gpt-5 "Summarize my notes"

# Within chat mode
You ❯ model gpt-5
🤖 Switched to model: gpt-5

You ❯ model
🤖 Current model: gpt-5
   Available models: gpt-5, gpt-4, claude-sonnet-4.5, ...
```

---

## Session Persistence

Conversations can be saved and resumed. Sessions are retained indefinitely by default — users choose their retention policy on first run.

### First-Run Setup

On first launch, users are prompted for initial configuration:

**Git Repository Detection:**

If the current working directory is not part of a Git repository, the user is prompted:

```
$ na

╭──────────────────────────────────────────────────────────────────╮
│  🤖 Notebook Automation Assistant - First Time Setup              │
╰──────────────────────────────────────────────────────────────────╯

📁 Current directory: C:\Users\You\Documents\Notes

⚠️  This directory is not a Git repository.
    
    Initializing a Git repo enables:
    • Better context awareness for Copilot
    • Version history for your notes
    • Change tracking and rollback capabilities

Would you like to initialize a Git repository? [Y/n]: 
```

> **Note:** A Git repository is not required for Notebook Automation or Copilot SDK to function. This is an optional enhancement that improves Copilot's context awareness and provides version control benefits.

**Session Retention:**

Users are then prompted to choose their session retention preference:

```
How long would you like to keep chat sessions?

  1. Forever (manual cleanup only) [default]
  2. 90 days
  3. 30 days
  4. 7 days

Select [1-4] or press Enter for default: 
```

### Resuming Sessions

```bash
# Resume last session
na chat --resume

# Resume specific session
na chat --session "finance-review"
```

**Within chat mode:**

```
You ❯ session save finance-review
🤖 Session saved as "finance-review"

You ❯ session list
🤖 Saved sessions:
   • finance-review (Jan 24, 2026 - 15 messages)
   • ml-course-work (Jan 22, 2026 - 8 messages)

You ❯ session load ml-course-work
🤖 Loaded session "ml-course-work" (8 messages)
   Last topic: Machine learning model evaluation
```

### Purging Sessions

Manual cleanup is available via CLI or within chat mode:

```bash
na chat purge --older-than 1d      # Purge sessions older than 1 day
na chat purge --older-than 7d      # Purge sessions older than 7 days
na chat purge --older-than 30d     # Purge sessions older than 30 days
na chat purge --older-than 90d     # Purge sessions older than 90 days
na chat purge --all                # Purge all sessions
```

**Within chat mode:**

```
You ❯ session purge --older-than 30d
🤖 Purged 12 sessions older than 30 days.
   Remaining sessions: 3
```

---

## Error Handling & Graceful Degradation

### When Copilot CLI is Not Available

```
$ na

⚠️  GitHub Copilot CLI not found in PATH.

    To use chat mode, install Copilot CLI:
    https://docs.github.com/copilot/how-tos/set-up/install-copilot-cli

    Traditional commands are still available:
    na --help

$ na generate-markdown --path "Course Content"
[Works normally - existing commands unaffected]
```

### When Not Authenticated

```
$ na

⚠️  GitHub Copilot authentication required.

    Run: copilot auth login

    Or use traditional commands: na --help
```

---

## Configuration

New configuration options in `config.json`:

```json
{
  "copilot": {
    "enabled": true,
    "defaultModel": null,
    "sessionDirectory": "~/.notebookautomation/sessions",
    "sessionRetention": "forever",
    "streaming": true,
    "language": "en",
    "accessibility": {
      "highContrast": false,
      "reducedMotion": false,
      "announceProgress": true
    },
    "logging": {
      "level": "info",
      "sessionLogging": false
    },
    "telemetry": {
      "enabled": false
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `enabled` | `true` | Enable/disable chat mode |
| `defaultModel` | `null` | Default AI model (null = let Copilot decide) |
| `sessionDirectory` | `~/.notebookautomation/sessions` | Where sessions are stored |
| `sessionRetention` | `"forever"` | Auto-delete sessions: `"forever"`, `"90d"`, `"30d"`, `"7d"` |
| `streaming` | `true` | Stream responses in real-time |
| `language` | `"en"` | Response language |

**Via CLI:**

```bash
na config update copilot.enabled true
na config update copilot.sessionRetention "30d"
na config update copilot.language "ja"
```

---

## Chat Input & Interaction

### Keyboard Shortcuts

The following keyboard shortcuts are available in chat mode:

| Shortcut | Action |
|----------|--------|
| `Ctrl+C` | Cancel current operation / Exit chat mode |
| `Ctrl+D` | Exit chat mode (end of input) |
| `↑` / `↓` | Navigate through input history |
| `Ctrl+L` | Clear screen (keep session) |
| `Ctrl+U` | Clear current input line |
| `Tab` | Auto-complete commands and file paths |
| `Esc` | Cancel current input |

### Multi-Line Input

For complex prompts that span multiple lines, users can:

**Option 1: Use triple backticks**
```
You ❯ ```
      Here is my multi-line prompt.
      It can contain code examples:
      
      def hello():
          print("world")
      
      Please explain this code.
      ```
```

**Option 2: Use backslash for line continuation**
```
You ❯ Convert all PDFs in my Downloads folder \
      and organize them by topic \
      into my Courses directory
```

**Option 3: Paste directly**
Multi-line content pasted from clipboard is automatically handled.

### File Attachments in Chat

Users can reference or attach files during chat:

**Reference by path:**
```
You ❯ Summarize the file at Notes/Finance/Budget.md

🤖 [Reading: Notes/Finance/Budget.md]
   
   This note covers your Q1 2026 budget with...
```

**Attach with @ syntax:**
```
You ❯ @Notes/Finance/Budget.md What are the key takeaways?

🤖 Based on the attached file, the key takeaways are...
```

**Multiple files:**
```
You ❯ Compare @file1.md and @file2.md

🤖 Comparing the two files...
```

**Drag and drop (when supported by terminal):**
```
You ❯ [Dropped: Lecture05.pdf]
      Convert this to notes

🤖 [Processing: Lecture05.pdf]
   ✓ Created: Lecture05.md
```

---

## Offline & Network Handling

### Offline Mode Behavior

When network connectivity is unavailable:

```
You ❯ Summarize my finance notes

🤖 ⚠️ Network connection unavailable.
   
   The following features require connectivity:
   • AI-powered chat and responses
   • Content summarization
   • Smart organization suggestions
   
   Available offline:
   • Direct CLI commands (!command syntax)
   • Session history viewing
   • Local file operations
   
   Retry when connected, or use: !vault generate-index --path "/"
```

### Connection Recovery

If connection is lost mid-session:

```
You ❯ Create a summary of my ML course

🤖 Creating summary...

   ⚠️ Connection lost. Attempting to reconnect...
   ✓ Reconnected.
   
   Continuing where we left off...
   
   Created: "Courses/ML/Course-Summary.md"
```

### Rate Limiting Handling

When Copilot quota limits are reached:

```
You ❯ Process all 500 PDFs in my archive

🤖 ⚠️ Rate limit approaching.
   
   Processed: 47 of 500 files
   Remaining quota: Low
   
   Options:
   1. Continue slowly (with delays between requests)
   2. Pause and resume later
   3. Use direct CLI commands for remaining files
   
   Select [1-3]: 
```

---

## Logging & Telemetry

### Debug Logging

Enable detailed logging for troubleshooting:

```bash
# Via command line
na chat --debug

# Via environment variable
set NOTEBOOK_AUTOMATION_LOG_LEVEL=debug
na
```

**Log output:**
```
[DEBUG] 2026-01-24 10:15:32 - Copilot SDK initialized
[DEBUG] 2026-01-24 10:15:33 - Session created: abc123
[DEBUG] 2026-01-24 10:15:34 - Sending message: "What notes..."
[DEBUG] 2026-01-24 10:15:35 - Tool invoked: search_vault
[DEBUG] 2026-01-24 10:15:36 - Response received (245 tokens)
```

### Session Logging

Chat interactions can be logged for review:

```
You ❯ session log enable

🤖 Session logging enabled.
   Log file: ~/.notebookautomation/logs/session-2026-01-24.log
   
   Note: Logs contain your prompts and responses.
   Disable with: session log disable
```

### Telemetry (Optional)

Anonymous usage telemetry can be enabled to help improve the product:

```json
{
  "copilot": {
    "telemetry": {
      "enabled": false,
      "anonymousUsageStats": true
    }
  }
}
```

> **Privacy:** No personal data, file contents, or prompts are ever collected. Only aggregate usage patterns (command frequency, error rates) when explicitly enabled.

---

## Accessibility

### Screen Reader Support

Chat mode is designed to work with screen readers:

- All visual elements have text alternatives
- Progress indicators include spoken descriptions
- Tool execution status is announced
- Emoji are accompanied by descriptive text

**Example with descriptions:**
```
You ❯ Convert my PDFs

🤖 [Status: Processing] Converting 5 PDF files...
   [Progress: 20%] File 1 of 5 complete
   [Progress: 40%] File 2 of 5 complete
   [Complete] All 5 files converted successfully
```

### High Contrast Mode

For users with visual impairments:

```bash
na chat --high-contrast
```

Or configure in settings:
```json
{
  "copilot": {
    "accessibility": {
      "highContrast": true,
      "reducedMotion": true,
      "announceProgress": true
    }
  }
}
```

### Color-Blind Considerations

- Status indicators use shapes in addition to colors
- ✓ Success (green + checkmark)
- ⚠️ Warning (yellow + triangle)
- ✗ Error (red + X mark)
- Progress bars include percentage text

---

## Internationalization

### Language Support

Copilot responds in the user's preferred language:

```bash
# Set via environment
set LANG=ja_JP
na

# Or via config
na config update copilot.language "ja"
```

**Example (Japanese):**
```
You ❯ ノートを要約して

🤖 ファイナンスフォルダのノートを要約します...
   
   主なトピック：
   • 予算管理（3ノート）
   • 投資戦略（5ノート）
```

### Supported Languages

- English (default)
- Spanish (es)
- French (fr)
- German (de)
- Japanese (ja)
- Chinese Simplified (zh-CN)
- Chinese Traditional (zh-TW)
- Korean (ko)
- Portuguese (pt-BR)
- And others supported by Copilot

> **Note:** CLI command syntax remains in English. Natural language prompts and responses are localized.

---

## Chat Command Help

### `na chat --help`

```
$ na chat --help

USAGE:
    na chat [OPTIONS]

OPTIONS:
    --resume              Resume the last conversation session
    --session <name>      Resume a specific named session
    --model <model>       Use a specific AI model (e.g., gpt-5, claude-sonnet-4.5)
    --debug               Enable debug logging
    --high-contrast       Enable high contrast mode for accessibility
    --no-banner           Skip the welcome banner
    
EXAMPLES:
    na chat                      Start a new chat session
    na chat --resume             Continue your last conversation
    na chat --model gpt-5        Start with a specific model
    na chat --session work       Resume the "work" session

BUILT-IN COMMANDS (within chat):
    help                  Show help and capabilities
    exit, quit            Exit chat mode
    clear                 Clear conversation history
    history               Show conversation history
    model [name]          Show or change the current model
    session save <name>   Save current session
    session list          List saved sessions
    session load <name>   Load a saved session
    session purge         Clean up old sessions
    !<command>            Execute CLI command directly

For more information: https://github.com/your-repo/notebook-automation
```

---

## Security Considerations

1. **File Access Scope** — Copilot can only access files within configured vault paths
2. **Confirmation for Destructive Actions** — Delete/move operations require explicit confirmation
3. **No Sensitive Data in Sessions** — Session files don't store API keys or secrets
4. **Audit Trail** — All file modifications are logged

---

## Example User Sessions

### Session 1: Quick Research

```
$ na

You ❯ What do my notes say about compound interest?

🤖 From your Finance notes, compound interest is covered in 3 places:

   1. "Chapter 5 - Time Value of Money.md" - Main explanation with formulas
   2. "Investment Strategies.md" - Applied to long-term investing
   3. "Retirement Planning.md" - 401k growth projections

   Key formula noted: A = P(1 + r/n)^(nt)

You ❯ exit
```

### Session 2: Batch Processing

```
$ na

You ❯ I just downloaded 20 PDF lecture slides. Convert them to notes and organize by topic.

🤖 I'll help with that. Where are the PDF files located?

You ❯ In my Downloads folder, they're named "Lecture01.pdf" through "Lecture20.pdf"

🤖 Found 20 PDF files. Here's my plan:

   1. Extract text and images from each PDF
   2. Convert to markdown notes
   3. Analyze content to determine topics
   4. Create appropriate folder structure
   5. Move notes to vault with proper metadata

   Proceed?

You ❯ Yes

🤖 [Processing 20 files...]

   ✓ Extracted and converted: 20 PDFs

   Detected topics:
   • Algorithms (Lectures 1-5)
   • Data Structures (Lectures 6-12)
   • System Design (Lectures 13-20)

   Created structure:
   📁 Computer Science/
   ├── 📁 Algorithms/ (5 notes)
   ├── 📁 Data Structures/ (7 notes)
   └── 📁 System Design/ (8 notes)

   ✓ Generated index files for each folder
   ✓ Added consistent metadata and tags

You ❯ Perfect, thanks!

🤖 You're welcome! Your notes are ready in the vault.
```

### Session 3: One-Shot Query

```
$ na ask "How many notes do I have about Python?"

🤖 You have 23 notes mentioning Python:
   • 12 in Programming/Python/
   • 6 in Data Science/
   • 5 scattered across other folders
```

---

## Technical Exploration: Copilot SDK as Processing Backend

**Status:** To be explored during implementation

Currently, file processing (PDF extraction, HTML-to-markdown, video transcripts) uses:

- **OpenAI API** (ChatGPT) — via Semantic Kernel
- **Azure OpenAI** — via Semantic Kernel
- Model configuration in `config.json`

### Exploration Goal

Investigate whether the **Copilot SDK can replace these backends** for content processing:

```
Current Flow:
┌─────────────┐     ┌──────────────────┐     ┌─────────────┐
│ Source File │ ──▶ │ Semantic Kernel  │ ──▶ │ Output File │
│ (PDF/HTML)  │     │ + OpenAI/Azure   │     │ (Markdown)  │
└─────────────┘     └──────────────────┘     └─────────────┘

Proposed Exploration:
┌─────────────┐     ┌──────────────────┐     ┌─────────────┐
│ Source File │ ──▶ │   Copilot SDK    │ ──▶ │ Output File │
│ (PDF/HTML)  │     │ (prompt + file)  │     │ (Markdown)  │
└─────────────┘     └──────────────────┘     └─────────────┘
```

### Questions to Answer

1. **Can Copilot SDK accept file attachments for processing?**
   - SDK supports `Attachments` in `MessageOptions`
   - Need to test with large files (PDFs, HTML)

2. **Can we use Copilot SDK for batch file processing?**
   - Process multiple files in a session
   - Maintain consistent output format

3. **Cost/billing implications?**
   - Copilot uses premium request quota
   - Compare with direct OpenAI/Azure API costs

4. **Model parity?**
   - Are the same models available (GPT-4, GPT-5)?
   - Quality comparison for summarization/extraction tasks

5. **Can we keep both backends?**
   - User choice: Copilot SDK vs OpenAI/Azure
   - Fallback if one is unavailable

### Potential Benefits

- **Single authentication** — Copilot subscription covers both chat and processing
- **Simplified architecture** — Remove Semantic Kernel dependency for AI calls
- **Consistent experience** — Same model/behavior in chat and batch processing

### Potential Concerns

- **Rate limits** — Premium request quotas may limit batch processing
- **File size limits** — SDK attachment handling for large files
- **Offline/enterprise scenarios** — Azure OpenAI may still be needed

### Implementation Note

This exploration should happen early in the technical implementation phase. The outcome will determine:

- Whether to add Copilot SDK as an additional processing backend
- Whether it can fully replace existing OpenAI/Azure integration
- Configuration options for users to choose their preferred backend

---

## Future Enhancements (Out of Scope for v1)

### UI Extensions (Visual Studio Code & Obsidian)

Beyond the CLI, future versions could provide graphical interfaces:

#### Visual Studio Code Extension

```
┌─────────────────────────────────────────────────────────────────┐
│ NOTEBOOK AUTOMATION                               [−][□][×]     │
├─────────────────────────────────────────────────────────────────┤
│ 🤖 Copilot Assistant                                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│ You: Convert my course PDFs to notes                            │
│                                                                 │
│ 🤖: I found 12 PDFs in your workspace. Converting...            │
│     ✓ Lecture01.pdf → Lecture01.md                              │
│     ✓ Lecture02.pdf → Lecture02.md                              │
│     [============================] 100%                         │
│                                                                 │
│ ┌─────────────────────────────────────────────────────────────┐ │
│ │ Type a message...                                       [↵] │ │
│ └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

**Potential features:**
- Side panel chat interface
- File tree integration (right-click → "Convert to notes")
- Inline note suggestions
- Live preview of generated markdown
- Integration with VS Code's Copilot Chat

#### Obsidian Plugin

```
┌─────────────────────────────────────────────────────────────────┐
│ 📔 My Vault                          Notebook Automation 🤖     │
├─────────────────────────────────────────────────────────────────┤
│ 📁 Courses                   │  Ask Notebook Automation...      │
│   📁 Finance                 │  ─────────────────────────────── │
│   📁 Programming             │  "Summarize my Finance notes"    │
│   📁 Data Science            │  "Find notes about Python"       │
│ 📁 Projects                  │  "Tag all notes in Projects"     │
│ 📁 Archive                   │                                  │
├──────────────────────────────┼──────────────────────────────────┤
│ # Budget 2026                │  🤖 Your Finance notes cover:    │
│                              │  • Budgeting (3 notes)           │
│ ## Overview                  │  • Investing (5 notes)           │
│ This quarter's budget...     │  • Tax Planning (2 notes)        │
└─────────────────────────────────────────────────────────────────┘
```

**Potential features:**
- Command palette integration
- Ribbon icon for quick access
- Right-click context menu on files/folders
- Floating chat window
- Note linking suggestions
- Auto-tagging recommendations

### MCP Server Integration

Connect to Model Context Protocol servers for extended capabilities:

```typescript
// Future: Connect to external MCP servers
const session = await client.createSession({
    mcpServers: {
        github: {
            type: "http",
            url: "https://api.githubcopilot.com/mcp/",
        },
        notion: {
            type: "http", 
            url: "https://notion-mcp.example.com/",
        }
    },
});
```

**Potential MCP integrations:**
- GitHub (issues, PRs, repositories)
- Notion (databases, pages)
- Google Drive (documents, sheets)
- Zotero (research papers, citations)
- Calendar services (meeting notes)

### Custom Agents

Define specialized AI personas for specific workflows:

```typescript
// Future: Custom agents
const session = await client.createSession({
    customAgents: [{
        name: "study-assistant",
        displayName: "Study Assistant",
        description: "Helps with learning and review",
        prompt: `You are a study assistant. Help users:
                 - Create flashcards from notes
                 - Generate practice questions
                 - Explain difficult concepts
                 - Track learning progress`,
    }],
});
```

### Additional Future Ideas

- **Voice input/output** — Speak commands and hear responses
- **Scheduled automation** — "Every Monday, summarize new notes"
- **External knowledge bases** — Connect to Wikipedia, documentation sites
- **Collaborative sessions** — Multiple users in shared sessions
- **Custom tool development** — Users create their own tools/skills
- **Mobile companion app** — Quick capture and query on the go
- **Web dashboard** — Browser-based vault management
- **Webhook integrations** — Trigger actions from external events

---

## Next Steps

1. [ ] Review and approve this feature specification
2. [ ] Create GitHub issue with implementation plan
3. [ ] Design technical architecture
4. [ ] Implement in phases:
   - Phase 1: Core SDK integration + chat mode
   - Phase 2: All CLI commands via chat
   - Phase 3: Session persistence + first-run experience
   - Phase 4: Advanced features (accessibility, i18n)
   - Phase 5: UI extensions (VS Code, Obsidian)

---

_Document maintained in: `docs/features/copilot-sdk-integration.md`_
