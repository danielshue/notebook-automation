# Getting Started Tutorial

A hands-on tutorial to get you up and running with Notebook Automation in 30 minutes.

## Prerequisites

- Notebook Automation CLI installed
- OpenAI API key (or other AI service)
- Sample content files (PDF or video)
- Text editor
- Basic command-line knowledge

## Part 1: Setup and Configuration (10 minutes)

### Step 1: Verify Installation

```bash
# Check that CLI is installed
na --version

# Should show version number like: 0.1.0-beta.8
```

**Troubleshooting:**
- If command not found, see [Installation Guide](../getting-started/installation.md)
- On Windows, you may need `.\na.exe` instead of `na`

### Step 2: Create Configuration File

Create a basic configuration file:

```bash
# Create config.json in current directory
cat > config.json << EOF
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key-here",
    "Model": "gpt-3.5-turbo"
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "./vault"
  }
}
EOF
```

**Windows PowerShell:**
```powershell
@"
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key-here",
    "Model": "gpt-3.5-turbo"
  },
  "Paths": {
    "NotebookVaultFullpathRoot": "./vault"
  }
}
"@ | Out-File -Encoding UTF8 config.json
```

### Step 3: Add Your API Key

1. Get your OpenAI API key from https://platform.openai.com/api-keys
2. Replace `your-api-key-here` in config.json with your actual key
3. Save the file

**Security Note:** Never commit config.json with API keys to version control!

```bash
# Add to .gitignore
echo "config.json" >> .gitignore
```

### Step 4: Create Vault Directory

```bash
# Create directory for processed notes
mkdir -p vault

# Verify it exists
ls -la vault
```

### Step 5: Test Configuration

```bash
# View configuration (API key will be masked)
na config view

# Should show your settings with ApiKey: "***masked***"
```

## Part 2: Process Your First File (10 minutes)

### Step 1: Prepare Sample Content

For this tutorial, you'll need a sample PDF or video. Use one of these options:

**Option A: Use Your Own File**
- Place a PDF or video file in a known location
- Note the full path to the file

**Option B: Download Sample File**
```bash
# Download a sample PDF (public domain book)
curl -o sample.pdf "https://www.gutenberg.org/files/1342/1342-pdf.pdf"
```

### Step 2: Process a PDF (Simple)

```bash
# Process the PDF without AI summary (fast)
na pdf-notes -p "sample.pdf" --no-summary

# Check the output
ls -la sample-pdf.md
```

**What Happened:**
- CLI read the PDF file
- Extracted text content
- Created markdown file with same name + `-pdf.md` suffix
- Added frontmatter with metadata

### Step 3: View the Output

```bash
# View the generated markdown
cat sample-pdf.md

# Or open in your text editor
# Windows: notepad sample-pdf.md
# Mac: open -a TextEdit sample-pdf.md
# Linux: nano sample-pdf.md
```

**What to Look For:**
```markdown
---
title: "Document Title"
source: "sample.pdf"
type: "pdf-note"
processed_date: "2025-01-18"
---

# Document Title

## Content

[Extracted text from PDF]

## Metadata

- **Pages**: X
- **Processed**: 2025-01-18
```

### Step 4: Process with AI Summary

Now let's add an AI-generated summary:

```bash
# Process with AI (requires API key)
na pdf-notes -p "sample.pdf" --force --verbose

# --force overwrites the existing file
# --verbose shows detailed progress
```

**Watch the Output:**
```
Processing: sample.pdf
Extracting text...
Generating AI summary...
Writing output...
Success!
```

### Step 5: Compare the Results

```bash
# View the file again
cat sample-pdf.md
```

**New Sections Added:**
```markdown
## Summary

[AI-generated summary of the content]

## Key Points

- [Important concept 1]
- [Important concept 2]
- [Important concept 3]
```

## Part 3: Batch Processing (10 minutes)

### Step 1: Create Test Directory

```bash
# Create directory with multiple files
mkdir -p test-batch

# Copy or move several PDFs there
cp file1.pdf test-batch/
cp file2.pdf test-batch/
cp file3.pdf test-batch/
```

### Step 2: Process All Files

```bash
# Process entire directory
na pdf-notes -p "test-batch" --verbose

# Watch as each file is processed
```

**Output Shows:**
```
Processing: test-batch/file1.pdf
Success!
Processing: test-batch/file2.pdf
Success!
Processing: test-batch/file3.pdf
Success!

Batch Summary:
Total: 3
Successful: 3
Failed: 0
```

### Step 3: Preview Before Processing

```bash
# Use dry-run to see what will be processed
na pdf-notes -p "test-batch" --dry-run

# Shows which files will be processed without actually doing it
```

### Step 4: Handle Errors

```bash
# If some files fail, retry just those
na pdf-notes -p "test-batch" --retry-failed --verbose
```

## Part 4: Working with Videos (Optional)

### Step 1: Process a Video

```bash
# Process video file (requires AI service)
na video-notes -p "lecture.mp4" --verbose
```

### Step 2: Review Video Output

```bash
# Open generated file
cat lecture-video.md
```

**Video-Specific Content:**
```markdown
## Summary

[AI-generated summary]

## Transcript

[00:00:15] Opening remarks...
[00:02:30] Main topic introduction...
[00:05:45] Key concept explanation...

## Key Topics

- Topic 1 (00:02:30)
- Topic 2 (00:05:45)
```

## Part 5: Organizing Your Notes

### Step 1: Create Vault Structure

```bash
# Create organized directory structure
mkdir -p vault/courses/course-101/{lectures,readings,assignments}

# Verify structure
tree vault/
# or
find vault -type d
```

### Step 2: Process into Vault

```bash
# Process lecture videos into vault
na video-notes -p "lectures/*.mp4" --overwrite-output-dir "vault/courses/course-101/lectures"

# Process reading PDFs
na pdf-notes -p "readings/*.pdf" --overwrite-output-dir "vault/courses/course-101/readings"
```

### Step 3: Generate Index

```bash
# Create index files for navigation
na vault generate-index "vault/courses/course-101" --recursive
```

### Step 4: View in Obsidian (Optional)

If you have Obsidian installed:

1. Open Obsidian
2. Open vault → Choose `vault` folder
3. Browse the generated notes
4. Use links and graph view

## Part 6: Advanced Features

### Add Tags

```bash
# Add hierarchical tags based on folder structure
na tag add-nested "vault/courses/course-101"

# Check the tags in your files
grep -A 5 "tags:" vault/courses/course-101/lectures/*
```

### Customize Processing

Edit config.json to customize behavior:

```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1500,
    "Temperature": 0.5
  },
  "Processing": {
    "GenerateSummaries": true,
    "ExtractMetadata": true,
    "ChunkSize": 4000
  }
}
```

### Extract Images from PDFs

```bash
# Extract images while processing
na pdf-notes -p "document.pdf" --extract-images

# Images saved to document-images/ folder
ls -la document-images/
```

## Common Workflows

### Daily Processing Workflow

```bash
# 1. Process new content
na pdf-notes -p "new-content" --verbose

# 2. Update tags
na tag add-nested "vault"

# 3. Generate indexes
na vault generate-index "vault" --recursive

# 4. Review in Obsidian
```

### Weekly Batch Processing

```bash
#!/bin/bash
# weekly-process.sh

echo "Weekly content processing"

# Process lectures
na video-notes -p "week-lectures" --verbose

# Process readings
na pdf-notes -p "week-readings" --extract-images --verbose

# Organize
na tag add-nested "vault"
na vault generate-index "vault" --recursive

echo "Done!"
```

## Troubleshooting

### Issue: Command not found

```bash
# Use full path
./na --version

# Or add to PATH (Linux/Mac)
export PATH=$PATH:$(pwd)
```

### Issue: API key not working

```bash
# Verify key in config
na config view

# Test with small file
na pdf-notes -p "small-test.pdf" --debug
```

### Issue: No output generated

```bash
# Use force flag to overwrite
na pdf-notes -p "file.pdf" --force

# Check verbose output
na pdf-notes -p "file.pdf" --verbose
```

### Issue: Processing timeout

```bash
# Increase timeout
na video-notes -p "long-video.mp4" --timeout 60

# Or disable AI for faster processing
na video-notes -p "long-video.mp4" --no-summary
```

## Next Steps

**Now that you're comfortable with basics:**

1. **Explore User Guides:**
   - [Batch Operations](../user-guide/batch-operations.md)
   - [Tag Management](../user-guide/tag-management.md)
   - [Academic Workflows](../user-guide/academic-workflows.md)

2. **Try Advanced Features:**
   - [OneDrive Integration](../user-guide/vault-synchronization.md)
   - [CI/CD Integration](../user-guide/ci-cd-integration.md)
   - [Performance Tuning](../user-guide/performance-tuning.md)

3. **Read More Tutorials:**
   - [Batch Processing Tutorial](batch-processing.md)
   - [Academic Notes Tutorial](academic-notes.md)
   - [Custom Configuration Tutorial](custom-configuration.md)

## Quick Reference

**Most Common Commands:**
```bash
# Process single PDF
na pdf-notes -p "file.pdf"

# Process single video
na video-notes -p "video.mp4"

# Process directory
na pdf-notes -p "directory" --verbose

# With AI summary
na pdf-notes -p "file.pdf" --verbose

# Without AI (faster)
na pdf-notes -p "file.pdf" --no-summary

# Extract images
na pdf-notes -p "file.pdf" --extract-images

# Overwrite existing
na pdf-notes -p "file.pdf" --force

# Preview (dry run)
na pdf-notes -p "directory" --dry-run
```

## Congratulations!

You've completed the getting started tutorial and learned:

- ✅ How to configure Notebook Automation
- ✅ How to process individual files
- ✅ How to batch process directories
- ✅ How to organize notes in a vault
- ✅ How to use basic and advanced features

**Ready to process your real content!**
