# C# to TypeScript Conversion - Notebook Automation Services

This document describes the TypeScript implementation of the core Notebook Automation services, converted from the original C# codebase.

## Overview

The TypeScript services provide the same functionality as the C# CLI application but are designed to run natively within the Obsidian plugin environment. This conversion was necessary because the Obsidian Community plugin process doesn't allow downloading executables.

## Architecture

### Directory Structure

```
src/obsidian-plugin/
├── services/               # Service implementations
│   ├── AISummarizer.ts    # AI summarization with OpenAI
│   ├── TagService.ts      # Tag management operations
│   ├── VaultService.ts    # Vault browsing and search
│   ├── PdfService.ts      # PDF text extraction
│   ├── VideoService.ts    # Video transcript processing
│   ├── MarkdownService.ts # Markdown generation with frontmatter
│   └── index.ts           # Service exports
├── models/                 # TypeScript interfaces and types
│   └── index.ts           # Model definitions
├── utils/                  # Utility classes
│   └── TextChunking.ts    # Text chunking for AI processing
└── __tests__/              # Unit tests
    ├── ai-summarizer.test.ts
    └── text-chunking.test.ts
```

## Core Services

### 1. AISummarizer

**Purpose**: AI-powered text summarization using direct OpenAI API calls.

**Key Changes from C#**:
- Replaced Microsoft Semantic Kernel with direct OpenAI API calls using `fetch`
- Maintained the same chunking logic (8000 char chunks with 500 char overlap)
- Implemented retry logic with exponential backoff
- Uses `gpt-4o-mini` model by default

**Features**:
- Automatic chunking for large texts (>8000 characters)
- Sequential chunk processing with rate limiting
- Variable substitution in prompts
- Configurable timeout and retry behavior

**Usage**:
```typescript
import { AISummarizer } from './services';

const summarizer = new AISummarizer(apiKey);
const summary = await summarizer.summarizeWithVariables(
  longText,
  { course: 'MBA Strategy', type: 'video_transcript' }
);
```

### 2. TagService

**Purpose**: Tag management operations for markdown files in the Obsidian vault.

**Integration**: Uses Obsidian's `app.metadataCache` and `app.fileManager` APIs for tag operations.

**Implemented Methods**:
- `getTags(filePath)` - Get all tags from a file
- `addTag(filePath, tag)` - Add a tag to a file
- `removeTag(filePath, tag)` - Remove a tag from a file
- `updateFrontmatter(path, key, value)` - Update YAML frontmatter

**TODO (Future Implementation)**:
- Nested tag addition based on frontmatter fields
- Tag consolidation
- Tag restructuring according to hierarchy
- YAML frontmatter diagnosis

**Usage**:
```typescript
import { TagService } from './services';

const tagService = new TagService(app);
const tags = await tagService.getTags('path/to/file.md');
await tagService.addTag('path/to/file.md', 'mba/strategy');
```

### 3. VaultService

**Purpose**: Vault browsing and search operations.

**Integration**: Uses Obsidian's `app.vault` API for file system operations.

**Features**:
- Browse vault folders and list files
- Search vault for text content
- Return structured results with match information

**Usage**:
```typescript
import { VaultService } from './services';

const vaultService = new VaultService(app);
const items = await vaultService.browseVault('Notes/MBA');
const results = await vaultService.searchVault('strategy framework');
```

### 4. PdfService

**Purpose**: PDF text extraction and conversion to markdown notes.

**Status**: Interface and structure implemented. Requires `pdf-parse` or `pdfjs-dist` package for full functionality.

**Planned Features**:
- Extract text from PDF files
- Generate markdown notes with frontmatter
- Optional AI summarization of PDF content
- Batch processing of PDF directories

**Dependencies Required**:
- `pdf-parse` (Node.js) or `pdfjs-dist` (cross-platform)

### 5. VideoService

**Purpose**: Video transcript processing and note generation.

**Status**: Interface and structure implemented. Full implementation pending.

**Planned Features**:
- Extract metadata from video files
- Process video transcripts (.vtt, .srt, .txt formats)
- Generate markdown notes with AI summaries
- Consolidate transcripts from multiple videos

### 6. MarkdownService

**Purpose**: Markdown file generation with YAML frontmatter.

**Integration**: Uses Obsidian's `app.vault` API for file creation and modification.

**Features**:
- Generate markdown with YAML frontmatter
- Create files with automatic folder creation
- Parse existing frontmatter from markdown
- Support for arrays and nested objects in YAML

**Usage**:
```typescript
import { MarkdownService } from './services';

const mdService = new MarkdownService(app);
const file = await mdService.createMarkdownFile(
  'Notes/summary.md',
  '# Summary\n\nContent here...',
  {
    title: 'MBA Strategy Summary',
    tags: ['mba', 'strategy'],
    date: '2024-02-04'
  }
);
```

## Utilities

### TextChunkingService

**Purpose**: Split large texts into overlapping chunks for AI processing.

**Features**:
- Character-based chunking with configurable size and overlap
- Token count estimation (4:1 character-to-token ratio)
- Validation of chunk parameters

**Usage**:
```typescript
import { TextChunkingService } from './utils/TextChunking';

const chunker = new TextChunkingService();
const chunks = chunker.splitTextIntoChunks(largeText, 8000, 500);
const tokenCount = chunker.estimateTokenCount(text);
```

## Models and Interfaces

All data models and interfaces are defined in `models/index.ts`:

- `TagOperationResult` - Results from tag operations
- `YamlDiagnosisResult` - YAML frontmatter diagnosis results
- `PdfOperationResult` - PDF processing results
- `VideoOperationResult` - Video processing results
- `VideoConsolidationResult` - Video transcript consolidation results
- `VaultItem` - Vault file/folder representation
- `SearchResult` - Search result with matches

## Dependencies

### Added to package.json

```json
{
  "dependencies": {
    "openai": "^4.77.3",
    "pdf-parse": "^1.1.1"
  }
}
```

### Obsidian API Usage

The services leverage Obsidian's built-in APIs:
- `app.vault` - File system operations
- `app.metadataCache` - Frontmatter and tag access
- `app.fileManager` - File modification operations

## Testing

### Test Coverage

All core functionality is tested with Jest:

- **TextChunkingService**: 10 tests covering chunking logic and token estimation
- **AISummarizer**: 8 tests covering summarization, chunking, retries, and error handling

### Running Tests

```bash
cd src/obsidian-plugin
npm test
```

### Test Files

- `__tests__/text-chunking.test.ts` - TextChunkingService tests
- `__tests__/ai-summarizer.test.ts` - AISummarizer tests with mocked OpenAI API

## Migration from C#

### Key Differences

1. **AI Framework**: Replaced Microsoft Semantic Kernel with direct OpenAI API calls
2. **File System**: Uses Obsidian's vault API instead of System.IO
3. **Async/Await**: TypeScript `Promise` instead of C# `Task`
4. **Dependency Injection**: Constructor-based DI pattern maintained
5. **Error Handling**: Try-catch with console logging instead of ILogger

### Semantic Kernel Replacement

The C# implementation used Microsoft Semantic Kernel for AI operations. The TypeScript implementation uses direct fetch calls to OpenAI's API:

**Before (C#)**:
```csharp
var result = await semanticKernel.InvokeAsync(function, arguments);
```

**After (TypeScript)**:
```typescript
const response = await fetch('https://api.openai.com/v1/chat/completions', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${apiKey}`,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    model: 'gpt-4o-mini',
    messages: [
      { role: 'system', content: systemPrompt },
      { role: 'user', content: userContent }
    ]
  })
});
```

### Maintained Features

- Text chunking with 8000 character chunks and 500 character overlap
- Retry logic with exponential backoff
- Sequential chunk processing with rate limiting
- Variable substitution in prompts
- Comprehensive error handling

## Future Enhancements

### High Priority

1. Complete PDF text extraction implementation
2. Complete video transcript processing implementation
3. Implement remaining TagService operations (consolidate, restructure, etc.)

### Medium Priority

4. Add OneDrive integration (if needed)
5. Add batch processing capabilities
6. Implement telemetry/analytics

### Low Priority

7. Add CLI command parsing equivalent (if needed in plugin context)
8. GitHub Copilot chat integration

## Configuration

Services can be configured through the Obsidian plugin settings. The configuration format is compatible with the C# YAML-based `AppConfig`:

```typescript
interface NotebookAutomationSettings {
  openaiApiKey: string;
  vaultPath: string;
  defaultOutputPath: string;
  // ... other settings
}
```

## Performance Considerations

1. **Rate Limiting**: Chunked processing includes configurable rate limiting to avoid API throttling
2. **Parallel Processing**: Currently sequential for OpenAI API calls (can be made parallel in future)
3. **Caching**: Consider implementing response caching for frequently summarized content
4. **Token Usage**: Monitor token consumption to control costs

## Error Handling

All services implement comprehensive error handling:
- Input validation
- API error handling with retries
- Graceful degradation
- Detailed error messages in console

## Contributing

When adding new services:
1. Create interface in `services/[ServiceName].ts`
2. Implement using Obsidian APIs where appropriate
3. Add corresponding models to `models/index.ts`
4. Create unit tests in `__tests__/`
5. Export from `services/index.ts`
6. Update this README

## References

- [Original C# Implementation](../c-sharp/NotebookAutomation.Core/)
- [OpenAI API Documentation](https://platform.openai.com/docs/api-reference)
- [Obsidian API](https://github.com/obsidianmd/obsidian-api)
