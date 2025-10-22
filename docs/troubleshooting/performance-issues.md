# Performance Issues

Diagnose and resolve performance-related problems with Notebook Automation.

## Identifying Performance Problems

### Symptoms of Performance Issues

**Slow Processing:**
- Files taking much longer than expected
- Batch operations timing out
- System becoming unresponsive

**Resource Constraints:**
- High CPU usage (>90% sustained)
- High memory usage (>80% of available RAM)
- Disk I/O bottlenecks
- Network saturation

**Application Behavior:**
- Frequent timeouts
- Inconsistent processing speeds
- Application crashes or hangs

## Diagnostic Steps

### Step 1: Establish Baseline

**Measure Normal Performance:**
```bash
# Process a small test file and note the time
time na pdf-notes -p "test-file.pdf" --verbose

# Typical baselines:
# - Small PDF (5-10 pages): 30-60 seconds
# - Medium PDF (50-100 pages): 2-5 minutes
# - Short video (15-30 min): 3-8 minutes
# - Long video (60+ min): 10-20 minutes
```

### Step 2: Monitor Resources

**Windows:**
```powershell
# Open Task Manager (Ctrl+Shift+Esc)
# Monitor Performance tab during processing
# Note CPU, Memory, Disk, Network usage
```

**Linux:**
```bash
# Install htop if not available
sudo apt-get install htop

# Monitor during processing
htop

# Or use top
top -p $(pgrep -f "na ")
```

**macOS:**
```bash
# Use Activity Monitor
# Or command line
top -pid $(pgrep -f "na")
```

### Step 3: Enable Verbose Logging

```bash
# Run with verbose and debug flags
na pdf-notes -p "file.pdf" --verbose --debug

# Check where time is being spent:
# - File reading
# - AI processing
# - Output generation
# - Network calls
```

### Step 4: Isolate the Bottleneck

**Test Each Component:**

**1. File Processing (no AI):**
```bash
# Disable AI to test raw processing speed
time na pdf-notes -p "file.pdf" --no-summary

# If still slow, issue is in file processing
# If fast, issue is AI service
```

**2. AI Service:**
```bash
# Test AI service separately
# Check AI provider status pages
# - OpenAI: https://status.openai.com
# - Azure: https://status.azure.com
```

**3. Network:**
```bash
# Test network latency
ping api.openai.com

# Test download speed
curl -o /dev/null https://api.openai.com/v1/models
```

## Common Performance Problems

### Problem 1: Slow AI API Responses

**Symptoms:**
- Processing stalls during "Generating summary" step
- Long delays between "Processing" messages
- Inconsistent processing times

**Diagnosis:**
```bash
# Use debug mode to see API call times
na video-notes -p "video.mp4" --debug | grep "API"

# Check API response times in logs
tail -f logs/notebook-automation.log | grep "duration"
```

**Solutions:**

**1. Switch to Faster Model:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "Model": "gpt-3.5-turbo"  // Faster than gpt-4
  }
}
```

**2. Reduce Response Length:**
```json
{
  "AIService": {
    "MaxTokens": 500  // Reduce from default 1000
  }
}
```

**3. Optimize Prompts:**
```json
{
  "AIService": {
    "Temperature": 0.3  // Lower = faster, more deterministic
  }
}
```

**4. Process Without AI:**
```bash
# First pass: extract content
na pdf-notes -p "documents" --no-summary --verbose

# Second pass: add AI to important files only
na pdf-notes -p "important-doc.pdf" --force
```

### Problem 2: High Memory Usage

**Symptoms:**
- System running out of RAM
- Application crashes with out-of-memory errors
- Slow system performance during processing

**Diagnosis:**
```bash
# Monitor memory usage
# Windows: Task Manager > Performance > Memory
# Linux: htop or free -m
# macOS: Activity Monitor

# Check memory during processing
while true; do
  ps aux | grep na | awk '{print $6}'  # RSS memory in KB
  sleep 5
done
```

**Solutions:**

**1. Process Smaller Batches:**
```bash
# Instead of:
na pdf-notes -p "all-1000-files" --verbose

# Do:
na pdf-notes -p "batch-01" --verbose
na pdf-notes -p "batch-02" --verbose
# etc.
```

**2. Disable AI Summaries:**
```bash
# Significantly reduces memory usage
na video-notes -p "videos" --no-summary
```

**3. Close Other Applications:**
```bash
# Free up RAM before processing
# Close browsers, IDEs, and other memory-intensive apps
```

**4. Increase System RAM:**
- Consider upgrading system memory
- Minimum recommended: 8GB
- Recommended for large batches: 16GB+

**5. Use Smaller Chunk Sizes:**
```json
{
  "Processing": {
    "ChunkSize": 2000  // Reduce from default 4000
  }
}
```

### Problem 3: High CPU Usage

**Symptoms:**
- CPU at 100% for extended periods
- System becomes sluggish
- Fan noise increases
- Thermal throttling

**Diagnosis:**
```bash
# Check CPU usage
# Windows: Task Manager > Performance > CPU
# Linux: htop
# macOS: Activity Monitor

# Identify what's consuming CPU
top -o cpu
```

**Solutions:**

**1. Don't Run Multiple Operations:**
```bash
# ❌ Don't do this:
na video-notes -p "videos" &
na pdf-notes -p "pdfs" &

# ✅ Do this:
na video-notes -p "videos"
na pdf-notes -p "pdfs"
```

**2. Process During Off-Peak Hours:**
```bash
# Schedule processing for overnight
# or other low-usage times
```

**3. Lower Process Priority:**
```bash
# Linux/Mac
nice -n 10 na video-notes -p "video.mp4"

# Windows PowerShell
Start-Process -FilePath "na.exe" -ArgumentList "video-notes","-p","video.mp4" -Priority "BelowNormal"
```

**4. Ensure Adequate Cooling:**
- Clean dust from computer fans
- Ensure proper ventilation
- Consider external cooling solutions

### Problem 4: Disk I/O Bottleneck

**Symptoms:**
- Disk usage at 100%
- Slow file reading/writing
- Processing speed varies with disk activity

**Diagnosis:**
```bash
# Windows: Task Manager > Performance > Disk
# Linux: iotop (install if needed: sudo apt-get install iotop)
sudo iotop

# macOS: Activity Monitor > Disk
```

**Solutions:**

**1. Use SSD Instead of HDD:**
- SSDs are 10-100x faster for random I/O
- Significant improvement for file processing

**2. Keep Source and Output on Same Drive:**
```bash
# Avoid cross-drive operations when possible
na pdf-notes -p "D:\input" --overwrite-output-dir "D:\output"
```

**3. Ensure Sufficient Free Space:**
```bash
# Keep at least 20% free on drive
# Check free space:
df -h  # Linux/Mac
wmic logicaldisk get size,freespace,caption  # Windows
```

**4. Defragment HDD (Windows):**
```powershell
# For HDD only (not SSD)
Optimize-Volume -DriveLetter C -Defrag
```

**5. Close Disk-Intensive Applications:**
- Stop file sync services (OneDrive, Dropbox) temporarily
- Close backup software
- Pause antivirus scans

### Problem 5: Network Latency

**Symptoms:**
- Delays during AI processing
- OneDrive sync is slow
- Timeout errors

**Diagnosis:**
```bash
# Test latency to AI service
ping api.openai.com

# Test bandwidth
curl -o /dev/null -w "%{speed_download}" https://api.openai.com/v1/models

# Check for packet loss
ping -c 100 api.openai.com | grep loss
```

**Solutions:**

**1. Use Wired Connection:**
```bash
# Ethernet is more stable than Wi-Fi
# Especially important for large file uploads
```

**2. Process During Off-Peak Network Hours:**
```bash
# Avoid peak usage times
# Late night/early morning typically faster
```

**3. Choose Nearby API Region:**
```json
{
  "AIService": {
    "Endpoint": "https://api.openai.com",  // US endpoint
    // or
    "Endpoint": "https://api.openai.eu"    // EU endpoint (if available)
  }
}
```

**4. Disable Share Link Creation:**
```bash
# Reduces OneDrive API calls
na video-notes -p "video.mp4" --no-share-links
```

### Problem 6: Timeout Errors

**Symptoms:**
- "Operation timed out" errors
- Processing stops prematurely
- Inconsistent failures

**Diagnosis:**
```bash
# Check timeout configuration
na config view | grep Timeout

# Test with longer timeout
na video-notes -p "video.mp4" --timeout 60 --verbose
```

**Solutions:**

**1. Increase Timeout Values:**
```bash
# Command line
na video-notes -p "long-video.mp4" --timeout 120

# Or in configuration
{
  "Processing": {
    "VideoProcessingTimeoutMinutes": 60,
    "PdfProcessingTimeoutMinutes": 30
  }
}
```

**2. Process Large Files Individually:**
```bash
# Instead of batch processing
na video-notes -p "very-long-lecture.mp4" --timeout 180
```

**3. Check Network Stability:**
```bash
# Ensure stable connection for API calls
# Disable VPN if causing issues
# Check firewall settings
```

### Problem 7: Inconsistent Performance

**Symptoms:**
- Same files process at different speeds
- Performance degrades over time
- Random slowdowns

**Possible Causes:**
- System resources fluctuating
- Background processes
- API rate limiting
- Thermal throttling

**Solutions:**

**1. Monitor Background Processes:**
```bash
# Windows
tasklist /v

# Linux/Mac
ps aux | sort -rn -k 3,3 | head -n 10  # Top 10 CPU consumers
```

**2. Disable Background Services:**
```bash
# Temporarily stop:
# - Antivirus real-time scanning
# - File sync services
# - Scheduled backups
# - System updates
```

**3. Check for API Rate Limiting:**
```bash
# Review API usage on provider dashboard
# Spread processing over time if hitting limits
```

**4. Clear Caches and Temp Files:**
```bash
# Windows
cleanmgr

# Linux
sudo apt-get clean
rm -rf /tmp/*

# macOS
sudo periodic daily weekly monthly
```

## Performance Optimization Workflow

### For Large Batches (100+ files)

**Step 1: Quick Pass (No AI)**
```bash
# Extract all content quickly
na pdf-notes -p "all-files" --no-summary --verbose
```

**Step 2: Identify Important Files**
```bash
# Review content and identify which files need AI summaries
```

**Step 3: Selective AI Enhancement**
```bash
# Add AI to important files only
na pdf-notes -p "important-file-1.pdf" --force
na pdf-notes -p "important-file-2.pdf" --force
```

**Result:** 10x faster overall with AI where it matters

### For Time-Constrained Processing

**Priority-Based Approach:**
```bash
# 1. High priority with AI
na video-notes -p "critical-lectures" --verbose

# 2. Medium priority without AI
na pdf-notes -p "supplementary-reading" --no-summary

# 3. Low priority - schedule for later
# (process overnight or on weekend)
```

### For Resource-Constrained Systems

**Conservative Settings:**
```json
{
  "Processing": {
    "ChunkSize": 2000,
    "GenerateSummaries": false,
    "VideoProcessingTimeoutMinutes": 90
  },
  "AIService": {
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 500
  }
}
```

**Process in Very Small Batches:**
```bash
# 5-10 files at a time
for i in {1..10}; do
  na pdf-notes -p "batch-$i" --verbose
  sleep 30  # Brief pause between batches
done
```

## Monitoring and Profiling

### Performance Metrics to Track

**1. Processing Time:**
```bash
# Track time per file
echo "File,Start,End,Duration" > processing-log.csv
for file in input/*.pdf; do
  START=$(date +%s)
  na pdf-notes -p "$file"
  END=$(date +%s)
  DUR=$((END - START))
  echo "$file,$(date -d @$START),$(date -d @$END),$DUR" >> processing-log.csv
done
```

**2. Resource Usage:**
```bash
# Log resource usage during processing
while true; do
  echo "$(date),$(ps aux | grep na | awk '{print $3,$4}')" >> resources.log
  sleep 5
done &

# Then process files
na pdf-notes -p "documents" --verbose

# Stop monitoring
kill %1
```

**3. API Response Times:**
```bash
# Extract API timings from debug logs
na video-notes -p "video.mp4" --debug 2>&1 | \
  grep "API call" | \
  awk '{print $NF}' > api-times.txt
```

### Performance Benchmarking

**Create Benchmark Script:**
```bash
#!/bin/bash
# benchmark.sh

echo "Notebook Automation Performance Benchmark"
echo "========================================="

echo "System Info:"
uname -a
echo "CPU cores: $(nproc)"
echo "Memory: $(free -h | grep Mem | awk '{print $2}')"
echo ""

echo "Test 1: Small PDF (no AI)"
time na pdf-notes -p "test-small.pdf" --no-summary

echo "Test 2: Small PDF (with AI)"
time na pdf-notes -p "test-small.pdf" --force

echo "Test 3: Large PDF (no AI)"
time na pdf-notes -p "test-large.pdf" --no-summary

echo "Test 4: Video (no AI)"
time na video-notes -p "test-video.mp4" --no-summary

echo "Benchmark complete"
```

## Advanced Troubleshooting

### Using Debug Logs

**Enable Maximum Logging:**
```json
{
  "Logging": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/debug-.log"
        }
      }
    ]
  }
}
```

**Analyze Logs:**
```bash
# Find slowest operations
grep "duration" logs/debug*.log | \
  awk '{print $NF}' | \
  sort -rn | \
  head -n 10

# Find errors
grep ERROR logs/*.log

# Find warnings
grep WARN logs/*.log
```

### Profiling

**Profile CPU Usage:**
```bash
# Linux - use perf
perf record -g ./na pdf-notes -p "file.pdf"
perf report

# Or simpler profiling
time -v ./na pdf-notes -p "file.pdf"
```

**Profile Memory:**
```bash
# Linux - use valgrind
valgrind --tool=massif ./na pdf-notes -p "file.pdf"

# Or monitor with top
top -p $(pgrep na) -d 1
```

## When to Seek Further Help

**Performance issues persisting after trying these solutions:**

1. File a detailed issue on GitHub
2. Include:
   - System specs
   - Configuration (sanitized)
   - Performance measurements
   - Steps taken
   - Debug logs

**Performance Baseline Issues:**

If even small files are slow (>5 minutes for small PDF), may indicate:
- System resource constraints
- Network issues
- AI service problems
- Configuration errors

**Related Documentation:**
- [Performance Tuning](../user-guide/performance-tuning.md)
- [Common Issues](common-issues.md)
- [Configuration Problems](configuration-problems.md)
