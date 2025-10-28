# Performance Tuning

Optimize Notebook Automation for better performance, faster processing, and efficient resource usage.

## Overview

Performance tuning becomes important when processing large collections of files, working with resource-intensive AI operations, or managing tight processing windows. This guide covers strategies to optimize Notebook Automation's performance.

## Understanding Performance Factors

### Key Performance Areas

**1. File Processing Speed**
- File size and complexity
- Content type (video, PDF, HTML)
- Number of files in batch

**2. AI Service Performance**
- API response times
- Rate limiting
- Model selection
- Prompt complexity

**3. System Resources**
- CPU utilization
- Memory availability
- Disk I/O speed
- Network bandwidth

**4. Configuration**
- Batch sizes
- Timeout settings
- Concurrency limits
- Caching strategies

## Measuring Performance

### Baseline Metrics

**Establish Baselines:**
```bash
# Process a small batch with verbose output
na pdf-notes -p "test-batch" --verbose

# Note the processing time per file
# Average: 45 seconds per PDF
# Average: 120 seconds per video
```

**Track Key Metrics:**
- Files processed per hour
- Average processing time per file type
- Memory usage during processing
- CPU utilization
- API call latency

### Monitoring Tools

**Built-in Monitoring:**
```bash
# Use verbose mode for detailed progress
na video-notes -p "videos" --verbose

# Use debug mode for diagnostic information
na pdf-notes -p "pdfs" --debug
```

**System Monitoring:**
- Task Manager (Windows)
- Activity Monitor (macOS)
- htop/top (Linux)
- Network monitoring tools

## Optimization Strategies

### 1. Batch Size Optimization

**Small Batches (1-25 files):**
- Better for limited resources
- Easier error recovery
- More responsive feedback
- Lower memory footprint

```bash
# Process in small batches
na pdf-notes -p "batch-01" --verbose
na pdf-notes -p "batch-02" --verbose
```

**Medium Batches (25-100 files):**
- Good balance of efficiency and control
- Reasonable memory usage
- Manageable error handling

```bash
# Process moderate batches
na video-notes -p "week-01-lectures" --verbose
```

**Large Batches (100+ files):**
- Maximum throughput
- Higher resource requirements
- Longer recovery if errors occur

```bash
# Process large batches
na pdf-notes -p "entire-semester" --verbose
```

**Recommendation:** Start with batches of 50 files and adjust based on system performance.

### 2. AI Service Optimization

**Disable AI When Not Needed:**
```bash
# Skip AI summary generation for faster processing
na pdf-notes -p "documents" --no-summary
na video-notes -p "videos" --no-summary
```

**Benefits:**
- 50-70% faster processing
- Reduced API costs
- Lower rate limiting risk
- Useful for base content extraction

**Configure AI Model Selection:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",  // Faster, cheaper
    // vs
    "Model": "gpt-4"            // Slower, more accurate
  }
}
```

**Model Selection Guidelines:**
- Use faster models (gpt-3.5-turbo) for large batches
- Use advanced models (gpt-4) for important content
- Consider costs vs. quality trade-offs

### 3. Timeout Configuration

**Adjust Timeouts for Content Type:**
```json
{
  "Processing": {
    "VideoProcessingTimeoutMinutes": 30,    // Long videos need more time
    "PdfProcessingTimeoutMinutes": 10,      // PDFs typically faster
    "DefaultTimeoutMinutes": 15
  }
}
```

**Command-Line Override:**
```bash
# Increase timeout for long videos
na video-notes -p "long-lecture.mp4" --timeout 45
```

**Guidelines:**
- Short videos (< 30 min): 10-15 minute timeout
- Long videos (> 60 min): 30-45 minute timeout
- Small PDFs (< 50 pages): 5-10 minute timeout
- Large PDFs (> 200 pages): 15-20 minute timeout

### 4. Resource Management

**Memory Optimization:**

**Check Memory Usage:**
```bash
# Monitor memory during processing
# Windows: Task Manager > Performance tab
# macOS: Activity Monitor
# Linux: htop or free -m
```

**Reduce Memory Footprint:**
- Process fewer files concurrently
- Close other applications
- Increase system RAM if needed
- Use `--no-summary` for memory-intensive batches

**CPU Optimization:**

**Balance CPU Load:**
- Video transcription is CPU-intensive
- AI processing uses CPU for some operations
- Monitor CPU usage during processing

**Recommendations:**
- Don't run multiple batch operations simultaneously
- Close CPU-intensive applications during processing
- Consider processing during off-peak hours

**Disk I/O Optimization:**

**Improve Disk Performance:**
- Use SSD instead of HDD when possible
- Ensure sufficient free space (20%+ recommended)
- Defragment HDD (Windows)
- Keep source and output on same drive when possible

### 5. Network Optimization

**AI API Performance:**

**Reduce Network Latency:**
- Use wired connection instead of Wi-Fi
- Process during off-peak network hours
- Choose datacenter region close to your location

**Handle Rate Limiting:**
```bash
# If rate limited, retry failed files later
na pdf-notes -p "documents" --retry-failed
```

**OneDrive Sync Optimization:**
```bash
# Use dry-run first to estimate time
na vault vault-sync "vault" --dry-run

# Then run actual sync
na vault vault-sync "vault" --verbose
```

### 6. Configuration Optimization

**Optimal Configuration Example:**
```json
{
  "Processing": {
    "ChunkSize": 4000,                      // Balance between API calls and context
    "GenerateSummaries": true,              // Can be disabled for speed
    "ExtractMetadata": true,                // Minimal performance impact
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10
  },
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo",              // Faster than gpt-4
    "MaxTokens": 1000,                      // Limit response length
    "Temperature": 0.3                      // Lower = more consistent/faster
  },
  "Logging": {
    "MinimumLevel": "Information"           // Don't use Debug in production
  }
}
```

## Performance Patterns for Common Scenarios

### Scenario 1: Large Course Processing (500+ files)

**Challenge:** Process entire semester of materials efficiently

**Strategy:**
```bash
# 1. Organize into batches by week
for week in week-{01..12}; do
  # 2. Process videos without AI first (fast)
  na video-notes -p "courses/$week/videos" --no-summary --verbose
  
  # 3. Process PDFs without AI
  na pdf-notes -p "courses/$week/pdfs" --no-summary --verbose
done

# 4. Add AI summaries to important files only
na video-notes -p "courses/week-01/intro-lecture.mp4" --force --verbose
na pdf-notes -p "courses/week-01/syllabus.pdf" --force --verbose
```

**Result:** 10x faster initial processing, selective AI enhancement

### Scenario 2: Limited Time Window

**Challenge:** Process materials before deadline

**Strategy:**
```bash
# Priority processing pipeline

# 1. High priority - process immediately with AI
na video-notes -p "priority-lectures" --verbose

# 2. Medium priority - process without AI
na pdf-notes -p "readings" --no-summary --verbose

# 3. Low priority - queue for later
# (process after deadline)
```

### Scenario 3: Resource-Constrained Environment

**Challenge:** Limited RAM, slow CPU

**Strategy:**
```bash
# Process in very small batches
for file in videos/*.mp4; do
  na video-notes -p "$file" --verbose
  sleep 10  # Brief pause between files
done
```

**Configuration:**
```json
{
  "Processing": {
    "ChunkSize": 2000,              // Smaller chunks = less memory
    "GenerateSummaries": false,     // Disable AI to save resources
    "VideoProcessingTimeoutMinutes": 60  // More time for slow systems
  }
}
```

### Scenario 4: High-Volume Daily Processing

**Challenge:** Process new content daily

**Strategy:**
```bash
#!/bin/bash
# Daily processing script

# 1. Process new videos from today
na video-notes -p "new-content/$(date +%Y-%m-%d)/videos" --verbose

# 2. Process new PDFs
na pdf-notes -p "new-content/$(date +%Y-%m-%d)/pdfs" --verbose

# 3. Update indexes
na vault generate-index "vault" --recursive

# 4. Apply tags
na tag add-nested "vault/$(date +%Y-%m-%d)"
```

**Automation:** Schedule with cron (Linux/Mac) or Task Scheduler (Windows)

## Advanced Performance Techniques

### Parallel Processing (Use with Caution)

**Concept:** Process multiple batches simultaneously

**Risk Factors:**
- API rate limiting
- Resource exhaustion
- File access conflicts

**Safe Parallel Pattern:**
```bash
# Process different content types in parallel
na video-notes -p "videos" --verbose &
na pdf-notes -p "pdfs" --verbose &
na generate-markdown -p "html" --verbose &
wait  # Wait for all to complete
```

**Recommendations:**
- Only use on powerful systems (16GB+ RAM, 8+ CPU cores)
- Monitor resource usage carefully
- Don't exceed API rate limits
- Use for independent content sets only

### Caching Strategies

**File-Level Caching:**
- Intelligent skip logic automatically caches results
- Files with AI content are skipped by default
- Use `--force` only when reprocessing needed

**API Response Caching:**
- Some AI providers support response caching
- Configure if available in your AI service

### Incremental Processing

**Process Only New Content:**
```bash
# Get list of new files
new_files=$(find content -name "*.mp4" -mtime -1)

# Process only new files
for file in $new_files; do
  na video-notes -p "$file"
done
```

**Use Retry Failed:**
```bash
# First pass - process everything
na pdf-notes -p "documents" --verbose

# Second pass - retry only failures
na pdf-notes -p "documents" --retry-failed
```

## Performance Troubleshooting

### Slow Processing

**Symptom:** Processing takes much longer than expected

**Diagnostic Steps:**
1. Check system resources (CPU, memory, disk)
2. Review AI API latency
3. Check network connectivity
4. Review file sizes and complexity

**Solutions:**
- Reduce batch size
- Disable AI summaries temporarily
- Increase system resources
- Check for background processes
- Use `--no-summary` for base processing

### High Memory Usage

**Symptom:** System running out of memory

**Diagnostic Steps:**
1. Monitor memory during processing
2. Check file sizes being processed
3. Review batch sizes

**Solutions:**
- Process smaller batches
- Close other applications
- Increase system RAM
- Use `--no-summary` to reduce memory footprint

### API Rate Limiting

**Symptom:** Processing stops with rate limit errors

**Diagnostic Steps:**
1. Check API provider dashboard
2. Review rate limit policies
3. Check processing frequency

**Solutions:**
- Add delays between files
- Use `--retry-failed` after cooldown period
- Upgrade API tier if possible
- Use `--no-summary` for bulk processing

### Timeout Errors

**Symptom:** Files failing with timeout errors

**Diagnostic Steps:**
1. Check timeout configuration
2. Review file sizes/complexity
3. Test network connectivity

**Solutions:**
- Increase timeout values
- Process large files individually
- Check network stability
- Split large files if possible

## Performance Best Practices Summary

1. **Start Small:** Test with small batches before large operations
2. **Measure First:** Establish baseline performance metrics
3. **Optimize Incrementally:** Make one change at a time
4. **Monitor Resources:** Watch CPU, memory, and network usage
5. **Use Dry Run:** Preview operations with `--dry-run`
6. **Batch Appropriately:** Find optimal batch size for your system
7. **Disable AI When Possible:** Use `--no-summary` for bulk processing
8. **Schedule Off-Peak:** Process during off-peak hours
9. **Clean Up Regularly:** Remove failed outputs and logs
10. **Document Your Settings:** Keep track of what works best

## Performance Monitoring Script

**Example Performance Tracking:**
```bash
#!/bin/bash
# performance-monitor.sh

echo "Starting batch processing: $(date)"
START_TIME=$(date +%s)

# Run processing
na pdf-notes -p "documents" --verbose | tee process.log

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))
FILE_COUNT=$(grep -c "Processing:" process.log)
AVG_TIME=$((DURATION / FILE_COUNT))

echo "Performance Summary:"
echo "Total files: $FILE_COUNT"
echo "Total time: $DURATION seconds"
echo "Average per file: $AVG_TIME seconds"
```

## Related Documentation

- [Batch Operations](batch-operations.md) - Efficient bulk processing
- [Configuration](../configuration/ai-services.md) - AI service setup
- [Troubleshooting](../troubleshooting/performance-issues.md) - Solve performance problems
- [CLI Reference](../cli-reference.md) - Command options

## Getting Help

For performance-related questions:
- Check [Performance Issues](../troubleshooting/performance-issues.md)
- Review [Common Issues](../troubleshooting/common-issues.md)
- Ask in [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)
