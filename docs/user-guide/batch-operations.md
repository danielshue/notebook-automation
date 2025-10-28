---
title: Batch Operations
---

# Batch Operations

Learn how to efficiently process multiple files and directories with Notebook Automation's batch processing capabilities.

## Overview

Batch operations allow you to process entire directories of files in a single command, making it efficient to handle large collections of educational materials. The CLI automatically detects and processes all supported files within the specified directory.

## Batch Processing Basics

### Automatic Directory Detection

Both `video-notes` and `pdf-notes` commands automatically switch to batch mode when provided with a directory path:

```bash
# Process all PDFs in a directory
na pdf-notes -p "path/to/directory"

# Process all videos in a directory
na video-notes -p "path/to/directory"
```

The tool will:
1. Scan the directory for supported files
2. Process each file sequentially
3. Generate output for each file
4. Report progress and any errors
5. Provide summary statistics at completion

### Recursive Processing

By default, batch operations process files in the specified directory only. For recursive processing of subdirectories, use the appropriate command options or process each subdirectory separately.

## Common Batch Workflows

### Processing Course Materials

Process an entire course directory structure:

```bash
# Process all lecture videos
na video-notes -p "courses/MBA101/lectures" --verbose

# Process all reading materials
na pdf-notes -p "courses/MBA101/readings" --verbose

# Process supplementary materials
na pdf-notes -p "courses/MBA101/supplementary" --extract-images
```

### Selective Batch Processing

Use file system tools to create selective batches:

```powershell
# Windows PowerShell - Process only specific file patterns
Get-ChildItem -Path "documents" -Filter "*lecture*.mp4" | ForEach-Object {
    na video-notes -p $_.FullName
}
```

```bash
# Linux/Mac - Process files matching a pattern
find documents -name "*lecture*.mp4" -exec na video-notes -p {} \;
```

### Incremental Processing

Process only new or failed files:

```bash
# Retry only failed files from previous batch
na pdf-notes -p "documents" --retry-failed

# Use dry-run to preview what will be processed
na pdf-notes -p "documents" --dry-run
```

## Batch Operation Options

### Essential Options

**Verbose Output:**
```bash
na pdf-notes -p "directory" --verbose
```
Shows detailed progress for each file being processed.

**Dry Run:**
```bash
na video-notes -p "directory" --dry-run
```
Previews what files will be processed without actually processing them.

**Force Overwrite:**
```bash
na pdf-notes -p "directory" --force
```
Overwrites existing output files. Without this flag, existing files are skipped unless they lack AI content (intelligent skip logic).

**Retry Failed:**
```bash
na video-notes -p "directory" --retry-failed
```
Processes only files that failed in a previous batch operation.

### Configuration Options

**Custom Configuration:**
```bash
na pdf-notes -p "directory" --config "custom-config.json"
```
Uses a custom configuration file for the batch operation.

**Custom Output Directory:**
```bash
na video-notes -p "inputs" --overwrite-output-dir "outputs"
```
Specifies a custom output location for all processed files.

## Performance Optimization

### Best Practices for Large Batches

**1. Use Verbose Mode for Monitoring:**
```bash
na pdf-notes -p "large-directory" --verbose
```
Monitor progress to identify slow files or potential issues.

**2. Process in Smaller Chunks:**
```bash
# Instead of processing all 1000 files at once
na pdf-notes -p "all-files"

# Process in smaller batches
na pdf-notes -p "all-files/batch1"
na pdf-notes -p "all-files/batch2"
```

**3. Use Dry Run First:**
```bash
# Preview the operation
na video-notes -p "directory" --dry-run --verbose

# Then run the actual operation
na video-notes -p "directory" --verbose
```

**4. Leverage Retry Failed:**
```bash
# First pass - process everything
na pdf-notes -p "documents" --verbose

# Second pass - retry any failures
na pdf-notes -p "documents" --retry-failed
```

### Resource Management

**Monitor System Resources:**
- CPU usage during AI processing
- Memory consumption for large files
- Disk I/O for file operations
- Network bandwidth for OneDrive operations

**Adjust Batch Size:**
- Smaller batches: Better for limited resources
- Larger batches: More efficient overall
- Consider system capabilities when planning batch sizes

## Error Handling in Batch Operations

### Common Batch Errors

**File Access Errors:**
```
Error: Cannot access file "locked-document.pdf" - file is in use
```
**Solution:** Close the file in other applications or skip it for now.

**API Rate Limiting:**
```
Error: AI service rate limit exceeded
```
**Solution:** Use `--retry-failed` after waiting for the rate limit to reset.

**Insufficient Disk Space:**
```
Error: Not enough disk space for output
```
**Solution:** Free up disk space or change output directory.

### Error Recovery Strategies

**1. Log Review:**
After a batch operation, review logs for patterns:
```bash
# Check the log file for errors
# Logs are typically in the application directory
```

**2. Retry Failed Files:**
```bash
# Automatically retry failed files
na pdf-notes -p "directory" --retry-failed --verbose
```

**3. Process Problem Files Individually:**
```bash
# Identify problematic files and process them separately
na pdf-notes -p "specific-problem-file.pdf" --debug
```

## Progress Tracking

### Real-Time Progress

With `--verbose` flag, you'll see:
- Current file being processed
- Files completed vs. total files
- Estimated time remaining
- Success/failure status per file

### Batch Summary

At completion, batch operations provide:
- Total files processed
- Number of successes
- Number of failures
- Total processing time
- Average time per file

Example output:
```
Batch Processing Summary:
========================
Total Files: 150
Successful: 145
Failed: 5
Total Time: 2h 15m
Average per file: 54s
```

## Integration with Other Features

### Batch + OneDrive Sync

Combine batch processing with vault synchronization:

```bash
# 1. Sync vault structure
na vault vault-sync "vault"

# 2. Batch process new content
na pdf-notes -p "vault/new-content" --verbose

# 3. Generate indexes
na vault generate-index "vault" --recursive
```

### Batch + Tag Management

Process files in batches, then update tags:

```bash
# 1. Process all documents
na pdf-notes -p "documents" --verbose

# 2. Add nested tags to all processed files
na tag add-nested "vault/documents" --verbose

# 3. Consolidate duplicate tags
na tag consolidate "vault/documents"
```

## Advanced Batch Patterns

### Multi-Stage Processing Pipeline

Create processing pipelines for complex workflows:

```bash
#!/bin/bash
# Example batch processing pipeline

echo "Stage 1: Sync vault structure"
na vault vault-sync "vault/courses"

echo "Stage 2: Process videos"
na video-notes -p "vault/courses/videos" --verbose

echo "Stage 3: Process PDFs"
na pdf-notes -p "vault/courses/pdfs" --extract-images --verbose

echo "Stage 4: Generate markdown from HTML"
na generate-markdown -p "vault/courses/html" --verbose

echo "Stage 5: Update tags"
na tag add-nested "vault/courses" --verbose

echo "Stage 6: Generate indexes"
na vault generate-index "vault/courses" --recursive

echo "Pipeline complete!"
```

### Parallel Processing (Advanced)

For very large batches, consider processing multiple directories in parallel:

```bash
# Process multiple directories simultaneously (use with caution)
na video-notes -p "directory1" --verbose &
na pdf-notes -p "directory2" --verbose &
wait
```

**Note:** Parallel processing should be used carefully to avoid:
- API rate limiting
- Resource exhaustion
- Concurrent file access issues

## Troubleshooting Batch Operations

### Slow Processing

**Symptom:** Batch taking much longer than expected

**Possible Causes:**
- Large files in the batch
- AI processing overhead
- Network latency for OneDrive operations
- Insufficient system resources

**Solutions:**
- Split into smaller batches
- Process large files separately
- Check network connectivity
- Monitor system resource usage

### Inconsistent Results

**Symptom:** Some files process correctly, others fail

**Possible Causes:**
- File corruption
- Unsupported file formats
- Encoding issues
- Permissions problems

**Solutions:**
- Process failed files individually with `--debug`
- Verify file integrity
- Check file permissions
- Review error logs for patterns

### Memory Issues

**Symptom:** System running out of memory during batch

**Possible Causes:**
- Very large files
- Too many concurrent operations
- Memory leaks (rare)

**Solutions:**
- Process in smaller batches
- Increase system memory if possible
- Close other applications
- Process large files individually

## Best Practices Summary

1. **Always start with `--dry-run`** to preview batch operations
2. **Use `--verbose`** for monitoring long-running batches
3. **Process in reasonable chunks** (50-100 files) rather than thousands at once
4. **Leverage `--retry-failed`** for error recovery
5. **Monitor system resources** during large batch operations
6. **Keep logs** for troubleshooting and progress tracking
7. **Test with small batches** before processing large collections
8. **Use consistent naming** for easy batch identification

## Related Documentation

- [Basic Commands](../getting-started/basic-commands.md) - Command syntax and options
- [File Processing](file-processing.md) - Detailed file processing information
- [Performance Tuning](performance-tuning.md) - Optimize processing performance
- [Troubleshooting](../troubleshooting/common-issues.md) - Solve common problems

## Example Scripts

See the [tutorials section](../tutorials/batch-processing.md) for complete batch processing tutorials and example scripts.
