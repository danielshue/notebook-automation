# Using TypeScript Services in the Obsidian Plugin

This guide shows how to integrate the newly converted TypeScript services into the Obsidian plugin.

## Setup

### 1. Install Dependencies

```bash
cd src/obsidian-plugin
npm install
```

### 2. Configure OpenAI API Key

The AISummarizer requires an OpenAI API key. This should be configured in the plugin settings:

```typescript
// In your plugin settings interface
interface NotebookAutomationSettings {
  openaiApiKey: string;
  // ... other settings
}
```

## Service Initialization

Services should be initialized in your plugin's `onload()` method:

```typescript
import { 
  AISummarizer, 
  TagService, 
  VaultService, 
  MarkdownService,
  PdfService,
  VideoService
} from './services';

export default class NotebookAutomationPlugin extends Plugin {
  settings: NotebookAutomationSettings;
  
  // Services
  private aiSummarizer: AISummarizer;
  private tagService: TagService;
  private vaultService: VaultService;
  private markdownService: MarkdownService;
  private pdfService: PdfService;
  private videoService: VideoService;

  async onload() {
    await this.loadSettings();
    
    // Initialize services
    this.aiSummarizer = new AISummarizer(this.settings.openaiApiKey);
    this.tagService = new TagService(this.app);
    this.vaultService = new VaultService(this.app);
    this.markdownService = new MarkdownService(this.app);
    this.pdfService = new PdfService(this.aiSummarizer);
    this.videoService = new VideoService(this.aiSummarizer);
    
    // Register commands, menus, etc.
    this.registerCommands();
  }
}
```

## Usage Examples

### AI Summarization

```typescript
// Summarize a document
async summarizeDocument(filePath: string) {
  const file = this.app.vault.getAbstractFileByPath(filePath);
  if (!(file instanceof TFile)) {
    return;
  }
  
  const content = await this.app.vault.cachedRead(file);
  const summary = await this.aiSummarizer.summarizeWithVariables(
    content,
    {
      course: 'MBA Strategy',
      type: 'lecture_notes'
    }
  );
  
  if (summary) {
    new Notice('Summary generated!');
    console.log(summary);
  }
}
```

### Tag Management

```typescript
// Add tags to a file
async addTagsToFile(filePath: string, tags: string[]) {
  for (const tag of tags) {
    await this.tagService.addTag(filePath, tag);
  }
  new Notice(`Added ${tags.length} tags to ${filePath}`);
}

// Get all tags from a file
async getFileTags(filePath: string) {
  const tags = await this.tagService.getTags(filePath);
  console.log('Tags:', tags);
  return tags;
}

// Update frontmatter
async updateMetadata(filePath: string, key: string, value: string) {
  const result = await this.tagService.updateFrontmatter(
    filePath,
    key,
    value,
    false // dryRun = false to actually update
  );
  
  if (result.success) {
    new Notice(`Updated ${key} in ${filePath}`);
  }
}
```

### Vault Operations

```typescript
// Browse a folder
async browseFolder(folderPath: string) {
  const items = await this.vaultService.browseVault(folderPath);
  
  console.log(`Found ${items.length} items:`);
  for (const item of items) {
    console.log(`  ${item.isFolder ? '[DIR]' : '[FILE]'} ${item.name}`);
  }
  
  return items;
}

// Search vault
async searchVault(query: string) {
  const results = await this.vaultService.searchVault(query);
  
  console.log(`Found ${results.length} matches for "${query}"`);
  for (const result of results) {
    console.log(`  ${result.path}: ${result.matches.length} matches`);
  }
  
  return results;
}
```

### Markdown Generation

```typescript
// Create a new markdown file with frontmatter
async createNote(path: string, title: string, content: string, tags: string[]) {
  const file = await this.markdownService.createMarkdownFile(
    path,
    content,
    {
      title,
      tags,
      created: new Date().toISOString(),
      modified: new Date().toISOString()
    },
    false // overwrite = false
  );
  
  if (file) {
    new Notice(`Created note: ${path}`);
    await this.app.workspace.getLeaf().openFile(file);
  }
}

// Generate markdown with frontmatter
generateMarkdownContent(title: string, content: string) {
  return this.markdownService.generateWithFrontmatter(
    content,
    {
      title,
      author: 'AI Assistant',
      generated: new Date().toISOString()
    }
  );
}
```

## Registering Commands

Add commands to the command palette to expose service functionality:

```typescript
registerCommands() {
  // Summarize current file
  this.addCommand({
    id: 'summarize-current-file',
    name: 'Summarize current file',
    editorCallback: async (editor, view) => {
      const file = view.file;
      if (!file) return;
      
      const content = editor.getValue();
      const summary = await this.aiSummarizer.summarizeWithVariables(content);
      
      if (summary) {
        // Insert summary at cursor
        editor.replaceSelection(`\n## AI Summary\n\n${summary}\n\n`);
      }
    }
  });
  
  // Add nested tags
  this.addCommand({
    id: 'add-nested-tags',
    name: 'Add nested tags to folder',
    callback: async () => {
      // Show folder picker modal
      const folderPath = await this.promptForFolder();
      if (!folderPath) return;
      
      const result = await this.tagService.addNestedTags(folderPath, false);
      new Notice(result.message);
    }
  });
  
  // Search vault
  this.addCommand({
    id: 'search-vault',
    name: 'Search vault',
    callback: async () => {
      const query = await this.promptForInput('Enter search query:');
      if (!query) return;
      
      const results = await this.vaultService.searchVault(query);
      // Display results in a modal or view
      this.displaySearchResults(results);
    }
  });
}
```

## Context Menu Integration

Add service operations to file/folder context menus:

```typescript
this.registerEvent(
  this.app.workspace.on('file-menu', (menu, file) => {
    // Add "Summarize" option for markdown files
    if (file instanceof TFile && file.extension === 'md') {
      menu.addItem((item) => {
        item
          .setTitle('Summarize with AI')
          .setIcon('sparkles')
          .onClick(async () => {
            const content = await this.app.vault.cachedRead(file);
            const summary = await this.aiSummarizer.summarizeWithVariables(content);
            
            if (summary) {
              // Create summary file
              const summaryPath = file.path.replace('.md', '-summary.md');
              await this.markdownService.createMarkdownFile(
                summaryPath,
                summary,
                {
                  title: `Summary of ${file.basename}`,
                  source: file.path,
                  generated: new Date().toISOString()
                }
              );
            }
          });
      });
    }
    
    // Add tag operations
    menu.addItem((item) => {
      item
        .setTitle('Add tag...')
        .setIcon('tag')
        .onClick(async () => {
          const tag = await this.promptForInput('Enter tag:');
          if (tag && file instanceof TFile) {
            await this.tagService.addTag(file.path, tag);
            new Notice(`Added tag: ${tag}`);
          }
        });
    });
  })
);
```

## Error Handling

Always wrap service calls in try-catch blocks:

```typescript
async summarizeWithErrorHandling(content: string) {
  try {
    const summary = await this.aiSummarizer.summarizeWithVariables(content);
    return summary;
  } catch (error) {
    console.error('Failed to generate summary:', error);
    new Notice('Error: Failed to generate summary. Check console for details.');
    return null;
  }
}
```

## Performance Tips

1. **Cache Results**: Cache AI summaries to avoid redundant API calls
2. **Rate Limiting**: The AISummarizer has built-in rate limiting for chunked processing
3. **Batch Operations**: Use batch operations for tag/frontmatter updates
4. **Lazy Loading**: Initialize services only when needed

```typescript
// Lazy loading example
private _aiSummarizer?: AISummarizer;

get aiSummarizer(): AISummarizer {
  if (!this._aiSummarizer) {
    this._aiSummarizer = new AISummarizer(this.settings.openaiApiKey);
  }
  return this._aiSummarizer;
}
```

## Testing

Services can be tested independently:

```typescript
// Example test
import { AISummarizer } from './services';

describe('AISummarizer Integration', () => {
  it('should summarize content', async () => {
    const summarizer = new AISummarizer(testApiKey);
    const summary = await summarizer.summarizeWithVariables('Test content');
    expect(summary).toBeTruthy();
  });
});
```

## Next Steps

1. Implement PDF text extraction in `PdfService`
2. Implement video transcript processing in `VideoService`
3. Complete advanced tag operations in `TagService`
4. Add prompt template loading system
5. Implement caching layer for AI responses
6. Add telemetry/analytics

## Troubleshooting

### OpenAI API Errors

- Verify API key is set correctly
- Check rate limits in OpenAI dashboard
- Ensure sufficient API credits

### File Access Errors

- Verify file paths are correct
- Check file permissions in vault
- Ensure files are markdown (.md extension)

### Build Errors

- Run `npm install` to ensure all dependencies are installed
- Run `npm run lint` to check for TypeScript errors
- Clear `node_modules` and reinstall if issues persist

## Additional Resources

- [OpenAI API Documentation](https://platform.openai.com/docs/api-reference)
- [Obsidian API](https://github.com/obsidianmd/obsidian-api)
- [CONVERSION-README.md](./CONVERSION-README.md) - Technical details of the C# to TypeScript conversion
