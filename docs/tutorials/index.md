# Tutorials

Step-by-step tutorials to help you get the most out of Notebook Automation.

## Tutorial 1: Processing Your First PDF

Learn how to process a PDF document and generate structured notes.

### Prerequisites

- Notebook Automation installed and configured
- A sample PDF file (e.g., course material, research paper)
- OpenAI API key configured

### Steps

1. **Prepare your PDF file**
   - Choose a PDF with good text content (not scanned images)
   - Place it in an accessible directory

2. **Run the PDF processor**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- process-pdf "path/to/your/document.pdf"
```

3. **Review the generated note**
   - Check the output directory for the generated Markdown file
   - Review the structure: frontmatter, summary, content sections

4. **Customize the output**
   - Modify processing options in your config file
   - Adjust AI summarization settings
   - Test with different chunk sizes

### Expected Results

You should see:
- A structured Markdown note with proper frontmatter
- An AI-generated summary section
- Organized content with clear sections
- Hierarchical tags based on document structure

## Tutorial 2: Setting Up OneDrive Integration

Configure OneDrive integration to process cloud-stored educational materials.

### Prerequisites

- Microsoft 365 or OneDrive account
- Azure app registration (for OAuth)
- Administrator access to configure app permissions

### Steps

1. **Create Azure App Registration**
   - Go to Azure Portal > App Registrations
   - Create new registration with redirect URI: `http://localhost:8080`
   - Note the Client ID and Tenant ID

2. **Configure API Permissions**
   - Add Microsoft Graph permissions:
     - `Files.Read.All`
     - `Sites.Read.All`
   - Grant admin consent

3. **Update Configuration**

```json
{
  "OneDrive": {
    "ClientId": "your-client-id",
    "TenantId": "your-tenant-id",
    "RedirectUri": "http://localhost:8080"
  }
}
```

4. **Authenticate**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- onedrive-auth
```

5. **Test Connection**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- onedrive-list
```

### Expected Results

- Successful authentication flow
- Ability to list OneDrive folders
- Access to download and process files

## Tutorial 3: Batch Processing Course Materials

Process an entire course worth of materials in one operation.

### Scenario

You have a directory structure like:

```
Course Materials/
├── Week 1/
│   ├── Lecture.pdf
│   ├── Reading.pdf
│   └── Assignment.pdf
├── Week 2/
│   ├── Lecture.pdf
│   └── Case Study.pdf
└── Resources/
    ├── Syllabus.pdf
    └── Reference Guide.pdf
```

### Steps

1. **Organize Source Materials**
   - Ensure consistent naming conventions
   - Verify all files are accessible
   - Remove any corrupted or problematic files

2. **Configure Batch Processing**

```json
{
  "Processing": {
    "BatchSize": 5,
    "MaxConcurrency": 2,
    "EnableVideoProcessing": true,
    "EnablePdfProcessing": true
  }
}
```

3. **Run Batch Processing**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- batch-process "Course Materials/" --recursive
```

4. **Monitor Progress**
   - Watch console output for processing status
   - Check log files for detailed information
   - Review any error messages

5. **Review Results**
   - Check output directory structure
   - Verify all files were processed
   - Review generated metadata and tags

### Expected Results

- Structured output matching source organization
- Consistent metadata across all notes
- Hierarchical tags reflecting course structure
- Cross-references between related materials

## Tutorial 4: Video Processing with Transcription

Process video lectures and generate transcript-based notes.

### Prerequisites

- FFmpeg installed for audio extraction
- Video files with clear audio
- AI service with transcription capabilities

### Steps

1. **Prepare Video Files**
   - Ensure good audio quality
   - Supported formats: MP4, AVI, MOV, WMV
   - Reasonable file sizes (under 2GB recommended)

2. **Configure Video Processing**

```json
{
  "VideoProcessing": {
    "AudioFormat": "wav",
    "TranscriptionService": "OpenAI",
    "EnableTimestamps": true,
    "ChunkDuration": 300
  }
}
```

3. **Process Single Video**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- process-video "lecture.mp4"
```

4. **Review Transcript Quality**
   - Check for accuracy in technical terms
   - Verify timestamp alignment
   - Note any unclear sections

5. **Batch Process Multiple Videos**

```bash
dotnet run --project src/c-sharp/NotebookAutomation.Console -- batch-process "Videos/" --filter "*.mp4"
```

### Expected Results

- Complete transcript with timestamps
- AI-generated summary of key topics
- Structured notes with topic markers
- Linked references to specific timepoints

## Tutorial 5: Customizing Note Templates

Create custom templates for specific types of content.

### Steps

1. **Understand Template Structure**
   - Review existing templates in `templates/`
   - Understand placeholder syntax
   - Learn template inheritance patterns

2. **Create Custom PDF Template**

Create `templates/custom-pdf-template.md`:

```markdown
---
title: "{{title}}"
course: "{{course}}"
week: "{{week}}"
type: "course-material"
processed: "{{processed_date}}"
tags: {{tags}}
---

# {{title}}

## Course Information
- **Course**: {{course}}
- **Week**: {{week}}
- **Topic**: {{topic}}

## Executive Summary
{{summary}}

## Key Learning Objectives
{{learning_objectives}}

## Content Analysis
{{content}}

## Discussion Questions
{{discussion_questions}}

## Additional Resources
{{additional_resources}}
```

3. **Configure Template Usage**

```json
{
  "Templates": {
    "PdfNoteTemplate": "templates/custom-pdf-template.md"
  }
}
```

4. **Test Template**
   - Process a sample PDF
   - Verify custom sections appear
   - Adjust template as needed

### Expected Results

- Notes following your custom structure
- Consistent formatting across documents
- Custom sections populated with relevant content

## Troubleshooting Common Issues

### Issue: API Rate Limits

**Problem**: Getting rate limit errors from OpenAI

**Solution**:
- Reduce batch size in configuration
- Add delays between API calls
- Upgrade to higher rate limit tier

### Issue: Poor OCR Quality

**Problem**: Scanned PDFs produce poor text extraction

**Solution**:
- Use higher quality source documents
- Consider pre-processing with OCR tools
- Manually review and correct critical content

### Issue: Memory Usage

**Problem**: Application consuming too much memory

**Solution**:
- Reduce chunk size for large documents
- Process files individually instead of batching
- Monitor system resources and adjust accordingly

## Next Steps

After completing these tutorials:

1. **Explore Advanced Features**
   - Custom metadata extraction
   - Advanced tagging strategies
   - Integration with other tools

2. **Optimize for Your Workflow**
   - Create custom templates
   - Develop processing scripts
   - Set up automated pipelines

3. **Contribute to the Project**
   - Report issues and suggest improvements
   - Share custom templates
   - Contribute code enhancements

For more help, see the [User Guide](../user-guide/index.md) and [Troubleshooting Guide](../troubleshooting/index.md).

- [Getting Started](../getting-started/) for basic usage
- [User Guide](../user-guide/) for detailed feature documentation
- [Configuration](../configuration/) for setup instructions
