# Batch Processing Tutorial

Learn how to efficiently process large collections of files with practical examples and best practices.

## Tutorial Overview

**What You'll Learn:**
- Setting up efficient batch processing workflows
- Processing different content types in batches
- Error handling and recovery
- Performance optimization for large batches
- Automation scripts

**Time Required:** 45 minutes

**Prerequisites:**
- Notebook Automation CLI installed
- Basic configuration complete
- Sample content files (PDFs and/or videos)

## Scenario: Processing an MBA Course

Let's process a complete MBA course with 50 PDFs and 30 lecture videos.

### Course Structure

```
MBA-Finance/
├── lectures/
│   ├── week-01/
│   │   ├── 01-introduction.mp4
│   │   ├── 02-fundamentals.mp4
│   │   └── 03-principles.mp4
│   └── week-02/
│       ├── 04-analysis.mp4
│       └── 05-applications.mp4
├── readings/
│   ├── textbook-chapter-01.pdf
│   ├── textbook-chapter-02.pdf
│   ├── case-study-01.pdf
│   └── article-01.pdf
└── supplementary/
    ├── slides-01.pdf
    └── handout-01.pdf
```

## Part 1: Preparation (10 minutes)

### Step 1: Organize Source Files

```bash
# Create organized structure
mkdir -p content/lectures/{week-01,week-02,week-03,week-04}
mkdir -p content/readings/{textbooks,cases,articles}
mkdir -p content/supplementary

# Move files into appropriate folders
# (Use your file manager or command line)
```

### Step 2: Create Vault Structure

```bash
# Create matching vault structure
mkdir -p vault/MBA-Finance/{lectures,readings,supplementary}

# Verify structure
tree vault/MBA-Finance/
```

### Step 3: Set Up Configuration

**Create batch-config.json:**
```json
{
  "AIService": {
    "Provider": "OpenAI",
    "ApiKey": "your-api-key",
    "Model": "gpt-3.5-turbo",
    "MaxTokens": 1000
  },
  "Processing": {
    "VideoProcessingTimeoutMinutes": 30,
    "PdfProcessingTimeoutMinutes": 10,
    "ChunkSize": 4000
  },
  "Logging": {
    "MinimumLevel": "Information"
  }
}
```

## Part 2: Strategy Planning (5 minutes)

### Analyze Your Content

```bash
# Count files to process
echo "Lecture videos: $(find content/lectures -name '*.mp4' | wc -l)"
echo "Reading PDFs: $(find content/readings -name '*.pdf' | wc -l)"
echo "Supplementary: $(find content/supplementary -name '*.pdf' | wc -l)"
```

**Example Output:**
```
Lecture videos: 30
Reading PDFs: 40
Supplementary: 10
Total: 80 files
```

### Estimate Processing Time

**Calculations:**
```
Videos (30 files × 10 min average) = 300 minutes = 5 hours
PDFs (50 files × 2 min average) = 100 minutes = 1.7 hours
Total estimated time: 6-7 hours
```

### Choose Strategy

**Option A: Single Large Batch** (Not Recommended)
```bash
# Process everything at once
na pdf-notes -p "content" --recursive --verbose
# Risk: Long runtime, difficult error recovery
```

**Option B: Batched by Content Type** (Recommended)
```bash
# Process by type for better control
na video-notes -p "content/lectures" --verbose
na pdf-notes -p "content/readings" --verbose
na pdf-notes -p "content/supplementary" --verbose
```

**Option C: Batched by Week** (Most Controlled)
```bash
# Process week by week
for week in week-{01..04}; do
  na video-notes -p "content/lectures/$week" --verbose
done
```

## Part 3: Two-Pass Processing (15 minutes)

### Pass 1: Fast Content Extraction (No AI)

This extracts all content quickly without AI summaries.

**Process All PDFs:**
```bash
#!/bin/bash
# pass1-pdfs.sh

echo "=== Pass 1: Extracting PDF Content (No AI) ==="

# Textbooks
echo "Processing textbooks..."
na pdf-notes -p "content/readings/textbooks" \
  --no-summary \
  --overwrite-output-dir "vault/MBA-Finance/readings" \
  --verbose

# Cases  
echo "Processing case studies..."
na pdf-notes -p "content/readings/cases" \
  --no-summary \
  --overwrite-output-dir "vault/MBA-Finance/readings" \
  --verbose

# Articles
echo "Processing articles..."
na pdf-notes -p "content/readings/articles" \
  --no-summary \
  --overwrite-output-dir "vault/MBA-Finance/readings" \
  --verbose

echo "Pass 1 complete!"
```

**Process All Videos:**
```bash
#!/bin/bash
# pass1-videos.sh

echo "=== Pass 1: Extracting Video Transcripts (No AI) ==="

for week in week-{01..04}; do
  echo "Processing $week..."
  na video-notes -p "content/lectures/$week" \
    --no-summary \
    --overwrite-output-dir "vault/MBA-Finance/lectures" \
    --verbose
done

echo "Pass 1 complete!"
```

**Run Pass 1:**
```bash
# Make scripts executable
chmod +x pass1-pdfs.sh pass1-videos.sh

# Run
./pass1-pdfs.sh  # Completes in 30-60 minutes
./pass1-videos.sh  # Completes in 2-3 hours
```

### Pass 2: Selective AI Enhancement

Now add AI summaries to important files only.

**Identify Important Files:**
```bash
# Create list of important files
cat > important-files.txt << EOF
vault/MBA-Finance/lectures/01-introduction-video.md
vault/MBA-Finance/lectures/04-analysis-video.md
vault/MBA-Finance/readings/textbook-chapter-01-pdf.md
vault/MBA-Finance/readings/case-study-01-pdf.md
EOF
```

**Process with AI:**
```bash
#!/bin/bash
# pass2-ai.sh

echo "=== Pass 2: Adding AI Summaries to Key Files ==="

while read file; do
  # Convert .md back to source file path
  source_file=$(echo "$file" | sed 's/-video\.md/.mp4/' | sed 's/-pdf\.md/.pdf/' | sed 's|vault/MBA-Finance|content|')
  
  echo "Processing: $source_file"
  na video-notes -p "$source_file" --force --verbose 2>/dev/null || \
  na pdf-notes -p "$source_file" --force --verbose
  
done < important-files.txt

echo "Pass 2 complete!"
```

## Part 4: Advanced Batch Techniques (10 minutes)

### Parallel Processing

**Process multiple content types simultaneously:**

```bash
#!/bin/bash
# parallel-process.sh

echo "Starting parallel processing..."

# Start each in background
na video-notes -p "content/lectures/week-01" --verbose &
PID1=$!

na pdf-notes -p "content/readings/textbooks" --verbose &
PID2=$!

na pdf-notes -p "content/supplementary" --verbose &
PID3=$!

# Wait for all to complete
echo "Waiting for processes to complete..."
wait $PID1
echo "Week 1 lectures complete"
wait $PID2
echo "Textbooks complete"
wait $PID3
echo "Supplementary complete"

echo "All parallel processes complete!"
```

**Use with Caution:**
- Only on powerful systems (16GB+ RAM, 8+ CPU cores)
- Monitor resource usage
- Risk of API rate limiting

### Progress Tracking

**Create progress tracking script:**

```bash
#!/bin/bash
# track-progress.sh

LOG_FILE="batch-progress.log"

echo "Batch Processing Progress" > $LOG_FILE
echo "=========================" >> $LOG_FILE
echo "Started: $(date)" >> $LOG_FILE
echo "" >> $LOG_FILE

# Count total files
TOTAL_FILES=$(find content -name '*.mp4' -o -name '*.pdf' | wc -l)
echo "Total files: $TOTAL_FILES" >> $LOG_FILE

# Process with progress
PROCESSED=0
for file in content/**/*.{mp4,pdf}; do
  if [ -f "$file" ]; then
    echo "Processing ($PROCESSED/$TOTAL_FILES): $file" | tee -a $LOG_FILE
    
    # Process based on file type
    if [[ $file == *.mp4 ]]; then
      na video-notes -p "$file" --verbose >> $LOG_FILE 2>&1
    else
      na pdf-notes -p "$file" --verbose >> $LOG_FILE 2>&1
    fi
    
    ((PROCESSED++))
    
    # Progress percentage
    PERCENT=$((PROCESSED * 100 / TOTAL_FILES))
    echo "Progress: $PERCENT% complete" | tee -a $LOG_FILE
  fi
done

echo "" >> $LOG_FILE
echo "Completed: $(date)" >> $LOG_FILE
```

## Part 5: Error Handling (5 minutes)

### Handling Failed Files

**Create error recovery script:**

```bash
#!/bin/bash
# recover-failures.sh

echo "=== Recovering Failed Files ==="

# First attempt: Retry failed files
echo "Attempting retry of failed files..."
na pdf-notes -p "content" --retry-failed --verbose

# Check for remaining failures
echo "" 
echo "Checking for files without output..."

# Find source files without corresponding output
for pdf in content/**/*.pdf; do
  output=$(echo "$pdf" | sed 's|content|vault/MBA-Finance|' | sed 's/\.pdf/-pdf.md/')
  if [ ! -f "$output" ]; then
    echo "Missing output for: $pdf"
    echo "$pdf" >> remaining-failures.txt
  fi
done

if [ -f remaining-failures.txt ]; then
  echo "Remaining failures logged to: remaining-failures.txt"
  echo "Process these individually with --debug flag"
else
  echo "All files processed successfully!"
fi
```

### Individual Debugging

**Process problematic files with debug mode:**

```bash
# Process with full debugging
na pdf-notes -p "problematic-file.pdf" --debug --verbose

# Check logs
tail -f logs/notebook-automation.log
```

## Part 6: Post-Processing (10 minutes)

### Organize and Index

**Add tags and generate indexes:**

```bash
#!/bin/bash
# organize-vault.sh

echo "=== Organizing Vault ==="

# Add hierarchical tags
echo "Adding tags..."
na tag add-nested "vault/MBA-Finance" --verbose

# Generate indexes
echo "Generating indexes..."
na vault generate-index "vault/MBA-Finance" --recursive

# Check metadata consistency
echo "Checking metadata..."
na tag metadata-check "vault/MBA-Finance" --verbose

echo "Organization complete!"
```

### Quality Control

**Verify all files processed:**

```bash
#!/bin/bash
# verify-completeness.sh

echo "=== Verifying Batch Completeness ==="

# Count source files
SOURCE_PDFS=$(find content -name '*.pdf' | wc -l)
SOURCE_VIDEOS=$(find content -name '*.mp4' | wc -l)

# Count output files
OUTPUT_PDFS=$(find vault -name '*-pdf.md' | wc -l)
OUTPUT_VIDEOS=$(find vault -name '*-video.md' | wc -l)

echo "PDFs: $OUTPUT_PDFS/$SOURCE_PDFS processed"
echo "Videos: $OUTPUT_VIDEOS/$SOURCE_VIDEOS processed"

if [ $OUTPUT_PDFS -eq $SOURCE_PDFS ] && [ $OUTPUT_VIDEOS -eq $SOURCE_VIDEOS ]; then
  echo "✅ All files processed successfully!"
else
  echo "⚠️ Some files may be missing"
fi
```

## Complete Automation Script

**Full end-to-end batch processing:**

```bash
#!/bin/bash
# auto-batch-process.sh

set -e  # Exit on any error

echo "==================================="
echo "Automated Batch Processing"
echo "==================================="
echo ""

# Configuration
CONFIG_FILE="batch-config.json"
LOG_DIR="logs"
mkdir -p "$LOG_DIR"

LOGFILE="$LOG_DIR/batch-$(date +%Y%m%d-%H%M%S).log"
exec > >(tee -a "$LOGFILE")
exec 2>&1

echo "Log file: $LOGFILE"
echo ""

# Step 1: Count files
echo "Step 1: Analyzing content..."
TOTAL=$(find content -name '*.mp4' -o -name '*.pdf' | wc -l)
echo "Total files to process: $TOTAL"
echo ""

# Step 2: Process PDFs (no AI)
echo "Step 2: Processing PDFs (fast pass)..."
na pdf-notes -p "content/readings" \
  --no-summary \
  --config "$CONFIG_FILE" \
  --verbose

# Step 3: Process Videos (no AI)
echo "Step 3: Processing Videos (fast pass)..."
na video-notes -p "content/lectures" \
  --no-summary \
  --config "$CONFIG_FILE" \
  --verbose

# Step 4: Add AI to important files
echo "Step 4: Adding AI summaries to key files..."
if [ -f "important-files.txt" ]; then
  while read source_file; do
    echo "Processing with AI: $source_file"
    na pdf-notes -p "$source_file" --force --verbose 2>/dev/null || \
    na video-notes -p "$source_file" --force --verbose
  done < important-files.txt
fi

# Step 5: Organize
echo "Step 5: Organizing vault..."
na tag add-nested "vault" --verbose
na vault generate-index "vault" --recursive

# Step 6: Verify
echo "Step 6: Verifying completeness..."
PROCESSED=$(find vault -name '*.md' | wc -l)
echo "Processed files: $PROCESSED"

# Summary
echo ""
echo "==================================="
echo "Batch Processing Complete!"
echo "==================================="
echo "Total source files: $TOTAL"
echo "Generated notes: $PROCESSED"
echo "Log file: $LOGFILE"
echo ""
```

## Best Practices Summary

**1. Always Use Dry Run First:**
```bash
na pdf-notes -p "directory" --dry-run
```

**2. Process in Manageable Batches:**
- 25-50 files per batch recommended
- Group by content type or time period

**3. Two-Pass Approach:**
- Pass 1: Extract content (no AI) - fast
- Pass 2: Add AI to important files - selective

**4. Monitor Progress:**
- Use `--verbose` flag
- Log output to files
- Track completion percentage

**5. Handle Errors Gracefully:**
- Use `--retry-failed`
- Debug problematic files individually
- Keep logs for troubleshooting

**6. Optimize for Cost:**
- Use `--no-summary` for bulk extraction
- Add AI summaries selectively
- Monitor API usage

## Troubleshooting

**Batch Stops Midway:**
```bash
# Resume from failures
na pdf-notes -p "content" --retry-failed --verbose
```

**Out of Memory:**
```bash
# Process smaller batches
# Close other applications
# Increase swap space (Linux)
```

**API Rate Limiting:**
```bash
# Add delays between files
# Use --retry-failed after cooldown
# Reduce batch size
```

## Next Steps

- [Academic Workflows](../user-guide/academic-workflows.md)
- [Performance Tuning](../user-guide/performance-tuning.md)
- [CI/CD Integration](../user-guide/ci-cd-integration.md)

## Summary

You've learned:
- ✅ Planning batch processing workflows
- ✅ Two-pass processing for efficiency
- ✅ Error handling and recovery
- ✅ Automation scripts
- ✅ Quality control and verification
- ✅ Performance optimization techniques

**You're ready to process large content collections efficiently!**
