# CI/CD Integration

Integrate Notebook Automation into continuous integration and deployment workflows for automated content processing.

## Overview

CI/CD (Continuous Integration/Continuous Deployment) integration allows you to automate the processing of educational materials as part of your development workflows. This is particularly useful for teams managing large content collections or automated publishing workflows.

## Use Cases for CI/CD Integration

### Educational Content Publishing

**Scenario:** Automatically process and publish course materials when updated

**Workflow:**
1. Instructor commits new lecture video to Git repository
2. CI/CD pipeline processes video with Notebook Automation
3. Generated markdown notes are committed back to repository
4. Notes are deployed to learning management system
5. Students receive notification of new content

### Research Documentation

**Scenario:** Maintain synchronized research paper library

**Workflow:**
1. Researchers add papers to shared folder
2. CI/CD detects new papers
3. Papers are processed to extract metadata
4. Index is updated automatically
5. Team is notified of new additions

### Course Material Validation

**Scenario:** Validate and format course materials before publishing

**Workflow:**
1. Content creator uploads materials
2. CI/CD validates file formats
3. Materials are processed and formatted
4. Quality checks are performed
5. Materials are published if checks pass

## Platform-Specific Integration

### GitHub Actions

**Setup for GitHub Actions:**

**.github/workflows/process-content.yml:**
```yaml
name: Process Educational Content

on:
  push:
    paths:
      - 'content/**/*.mp4'
      - 'content/**/*.pdf'
      - 'content/**/*.html'
  workflow_dispatch:

jobs:
  process-content:
    runs-on: ubuntu-latest
    
    steps:
      - name: Checkout repository
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
          
      - name: Download Notebook Automation CLI
        run: |
          wget https://github.com/danielshue/notebook-automation/releases/latest/download/linux-x64.zip
          unzip linux-x64.zip -d na-cli
          chmod +x na-cli/na
          
      - name: Configure Notebook Automation
        run: |
          cat > config.json << EOF
          {
            "AIService": {
              "Provider": "OpenAI",
              "ApiKey": "${{ secrets.OPENAI_API_KEY }}"
            },
            "Paths": {
              "NotebookVaultFullpathRoot": "${{ github.workspace }}/vault"
            }
          }
          EOF
          
      - name: Process Videos
        run: |
          ./na-cli/na video-notes -p content/videos --config config.json --verbose
          
      - name: Process PDFs
        run: |
          ./na-cli/na pdf-notes -p content/pdfs --config config.json --verbose
          
      - name: Generate Indexes
        run: |
          ./na-cli/na vault generate-index vault --recursive
          
      - name: Commit processed content
        run: |
          git config --local user.email "action@github.com"
          git config --local user.name "GitHub Action"
          git add vault/
          git diff --quiet && git diff --staged --quiet || git commit -m "docs: Process new educational content"
          git push
```

**Key Features:**
- Triggered by new content in `content/` directory
- Downloads latest Notebook Automation CLI
- Processes videos and PDFs
- Commits results back to repository
- Can be manually triggered via workflow_dispatch

### GitLab CI/CD

**.gitlab-ci.yml:**
```yaml
stages:
  - setup
  - process
  - publish

variables:
  NA_VERSION: "latest"
  VAULT_PATH: "${CI_PROJECT_DIR}/vault"

setup-cli:
  stage: setup
  script:
    - wget https://github.com/danielshue/notebook-automation/releases/latest/download/linux-x64.zip
    - unzip linux-x64.zip -d na-cli
    - chmod +x na-cli/na
  artifacts:
    paths:
      - na-cli/
    expire_in: 1 hour

process-videos:
  stage: process
  dependencies:
    - setup-cli
  script:
    - ./na-cli/na video-notes -p content/videos --config config.json --verbose
  artifacts:
    paths:
      - vault/
    expire_in: 1 day
  only:
    changes:
      - content/videos/**/*

process-pdfs:
  stage: process
  dependencies:
    - setup-cli
  script:
    - ./na-cli/na pdf-notes -p content/pdfs --config config.json --verbose
  artifacts:
    paths:
      - vault/
    expire_in: 1 day
  only:
    changes:
      - content/pdfs/**/*

publish-content:
  stage: publish
  dependencies:
    - process-videos
    - process-pdfs
  script:
    - git add vault/
    - git commit -m "docs: Automated content processing" || echo "No changes"
    - git push origin main
  only:
    - main
```

### Azure DevOps

**azure-pipelines.yml:**
```yaml
trigger:
  paths:
    include:
      - content/**/*.mp4
      - content/**/*.pdf

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '9.0.x'

- script: |
    wget https://github.com/danielshue/notebook-automation/releases/latest/download/linux-x64.zip
    unzip linux-x64.zip -d $(Build.SourcesDirectory)/na-cli
    chmod +x $(Build.SourcesDirectory)/na-cli/na
  displayName: 'Download Notebook Automation CLI'

- script: |
    cat > config.json << EOF
    {
      "AIService": {
        "Provider": "OpenAI",
        "ApiKey": "$(OPENAI_API_KEY)"
      }
    }
    EOF
  displayName: 'Configure Notebook Automation'

- script: |
    $(Build.SourcesDirectory)/na-cli/na video-notes -p content/videos --config config.json --verbose
  displayName: 'Process Videos'

- script: |
    $(Build.SourcesDirectory)/na-cli/na pdf-notes -p content/pdfs --config config.json --verbose
  displayName: 'Process PDFs'

- script: |
    git config --global user.email "pipeline@azure.com"
    git config --global user.name "Azure Pipeline"
    git add vault/
    git commit -m "docs: Automated content processing" || echo "No changes"
    git push
  displayName: 'Commit and Push Changes'
```

## Configuration Management

### Secrets Management

**GitHub Actions:**
```yaml
- name: Configure with secrets
  env:
    OPENAI_API_KEY: ${{ secrets.OPENAI_API_KEY }}
    AZURE_AI_KEY: ${{ secrets.AZURE_AI_KEY }}
  run: |
    cat > config.json << EOF
    {
      "AIService": {
        "Provider": "OpenAI",
        "ApiKey": "${OPENAI_API_KEY}"
      }
    }
    EOF
```

**GitLab CI:**
```yaml
process-content:
  script:
    - echo '{"AIService":{"ApiKey":"'$OPENAI_API_KEY'"}}' > config.json
  variables:
    OPENAI_API_KEY: $OPENAI_API_KEY
```

**Best Practices:**
- Store API keys in CI/CD secret variables
- Never commit secrets to repository
- Use environment-specific configurations
- Rotate keys regularly

### Environment-Specific Configuration

**Development, Staging, Production:**

```yaml
# GitHub Actions example
- name: Set environment config
  run: |
    if [ "${{ github.ref }}" == "refs/heads/main" ]; then
      ENV="production"
    elif [ "${{ github.ref }}" == "refs/heads/develop" ]; then
      ENV="staging"
    else
      ENV="development"
    fi
    cp config.$ENV.json config.json
```

## Advanced Workflows

### Conditional Processing

**Process Only Changed Files:**

```yaml
- name: Get changed files
  id: changed-files
  uses: tj-actions/changed-files@v40
  with:
    files: |
      content/**/*.mp4
      content/**/*.pdf

- name: Process changed videos
  if: steps.changed-files.outputs.any_modified == 'true'
  run: |
    echo "${{ steps.changed-files.outputs.all_modified_files }}" | while read file; do
      if [[ $file == *.mp4 ]]; then
        ./na-cli/na video-notes -p "$file" --config config.json
      fi
    done
```

### Parallel Processing

**Process Multiple Content Types in Parallel:**

```yaml
jobs:
  process-videos:
    runs-on: ubuntu-latest
    steps:
      # ... setup steps ...
      - name: Process videos
        run: ./na-cli/na video-notes -p content/videos --config config.json
      
  process-pdfs:
    runs-on: ubuntu-latest
    steps:
      # ... setup steps ...
      - name: Process PDFs
        run: ./na-cli/na pdf-notes -p content/pdfs --config config.json
      
  combine-results:
    needs: [process-videos, process-pdfs]
    runs-on: ubuntu-latest
    steps:
      - name: Merge and commit
        run: |
          git add vault/
          git commit -m "docs: Combined processing results"
          git push
```

### Quality Checks

**Validate Processed Content:**

```yaml
- name: Validate processed content
  run: |
    # Check that markdown files were created
    if [ -z "$(find vault -name '*.md' -type f)" ]; then
      echo "Error: No markdown files generated"
      exit 1
    fi
    
    # Check for malformed frontmatter
    for file in vault/**/*.md; do
      if ! grep -q "^---" "$file"; then
        echo "Error: Missing frontmatter in $file"
        exit 1
      fi
    done
    
    echo "Content validation passed"
```

### Notification Integration

**Slack Notifications:**

```yaml
- name: Notify Slack
  if: always()
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    text: |
      Content processing ${{ job.status }}
      Processed files available in vault/
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

**Email Notifications:**

```yaml
- name: Send email notification
  if: failure()
  uses: dawidd6/action-send-mail@v3
  with:
    server_address: smtp.gmail.com
    server_port: 465
    username: ${{ secrets.MAIL_USERNAME }}
    password: ${{ secrets.MAIL_PASSWORD }}
    subject: Content Processing Failed
    body: Check the workflow logs for details
    to: team@example.com
```

## Docker Integration

### Dockerfile for Notebook Automation

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0

WORKDIR /app

# Install dependencies
RUN apt-get update && apt-get install -y \
    wget \
    unzip \
    && rm -rf /var/lib/apt/lists/*

# Download and install Notebook Automation
RUN wget https://github.com/danielshue/notebook-automation/releases/latest/download/linux-x64.zip \
    && unzip linux-x64.zip \
    && chmod +x na \
    && rm linux-x64.zip

# Set up configuration
COPY config.json /app/config.json

# Create volume for content
VOLUME ["/content", "/vault"]

ENTRYPOINT ["./na"]
```

**Usage:**
```bash
# Build image
docker build -t notebook-automation .

# Process content
docker run -v $(pwd)/content:/content \
           -v $(pwd)/vault:/vault \
           notebook-automation video-notes -p /content --config /app/config.json
```

### Docker Compose

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  process-content:
    build: .
    volumes:
      - ./content:/content
      - ./vault:/vault
    environment:
      - OPENAI_API_KEY=${OPENAI_API_KEY}
    command: pdf-notes -p /content --config /app/config.json --verbose
```

## Scheduled Processing

### Cron-Based Processing (Linux/Mac)

**crontab entry:**
```bash
# Process new content daily at 2 AM
0 2 * * * cd /path/to/project && ./scripts/process-daily.sh

# Process weekly on Sundays at midnight
0 0 * * 0 cd /path/to/project && ./scripts/process-weekly.sh
```

**process-daily.sh:**
```bash
#!/bin/bash
set -e

echo "Starting daily content processing: $(date)"

# Pull latest content
git pull

# Process new videos
./na video-notes -p content/daily --config config.json --verbose

# Process new PDFs
./na pdf-notes -p content/daily --config config.json --verbose

# Update indexes
./na vault generate-index vault --recursive

# Commit and push
git add vault/
git commit -m "docs: Daily content processing $(date +%Y-%m-%d)" || echo "No changes"
git push

echo "Daily processing complete: $(date)"
```

### Windows Task Scheduler

**PowerShell script (process-daily.ps1):**
```powershell
# Daily processing script for Windows
Write-Host "Starting daily content processing: $(Get-Date)"

# Pull latest
git pull

# Process content
.\na.exe video-notes -p content\daily --config config.json --verbose
.\na.exe pdf-notes -p content\daily --config config.json --verbose

# Update vault
.\na.exe vault generate-index vault --recursive

# Commit changes
git add vault\
git commit -m "docs: Daily content processing $(Get-Date -Format 'yyyy-MM-dd')"
git push

Write-Host "Daily processing complete: $(Get-Date)"
```

**Create scheduled task:**
```powershell
$action = New-ScheduledTaskAction -Execute 'PowerShell.exe' `
  -Argument '-File C:\path\to\process-daily.ps1'

$trigger = New-ScheduledTaskTrigger -Daily -At 2am

Register-ScheduledTask -Action $action -Trigger $trigger `
  -TaskName "NotebookAutomation-Daily" -Description "Daily content processing"
```

## Monitoring and Logging

### Structured Logging

**Enhanced logging configuration:**
```json
{
  "Logging": {
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/processing-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  }
}
```

### Metrics Collection

**Track processing metrics:**
```bash
#!/bin/bash
# metrics-collector.sh

START_TIME=$(date +%s)
FILE_COUNT=0
SUCCESS_COUNT=0
FAIL_COUNT=0

# Process with logging
./na video-notes -p content --verbose 2>&1 | while read line; do
  echo "$line"
  
  if [[ $line == *"Processing:"* ]]; then
    ((FILE_COUNT++))
  fi
  
  if [[ $line == *"Success"* ]]; then
    ((SUCCESS_COUNT++))
  fi
  
  if [[ $line == *"Failed"* ]]; then
    ((FAIL_COUNT++))
  fi
done

END_TIME=$(date +%s)
DURATION=$((END_TIME - START_TIME))

# Log metrics
echo "Processing Metrics:" | tee -a metrics.log
echo "Duration: $DURATION seconds" | tee -a metrics.log
echo "Files: $FILE_COUNT" | tee -a metrics.log
echo "Success: $SUCCESS_COUNT" | tee -a metrics.log
echo "Failed: $FAIL_COUNT" | tee -a metrics.log
```

## Error Handling

### Retry Logic

**Automatic retry on failure:**
```yaml
- name: Process content with retry
  uses: nick-invision/retry@v2
  with:
    timeout_minutes: 60
    max_attempts: 3
    command: ./na-cli/na pdf-notes -p content --config config.json --verbose
```

### Failure Notifications

**Alert on failures:**
```yaml
- name: Process content
  id: process
  run: ./na-cli/na video-notes -p content --config config.json --verbose
  continue-on-error: true

- name: Handle failures
  if: steps.process.outcome == 'failure'
  run: |
    echo "Processing failed, retrying failed files only"
    ./na-cli/na video-notes -p content --retry-failed --config config.json --verbose
```

## Best Practices

1. **Use Secrets Management:** Never commit API keys or credentials
2. **Implement Caching:** Cache CLI downloads to speed up builds
3. **Monitor Resource Usage:** Track memory and CPU usage
4. **Set Appropriate Timeouts:** Prevent stuck pipelines
5. **Use Conditional Triggers:** Only process when content changes
6. **Validate Output:** Check generated content quality
7. **Log Comprehensively:** Maintain detailed logs for debugging
8. **Test Locally First:** Validate workflows locally before committing
9. **Use Version Pinning:** Pin specific CLI versions for reproducibility
10. **Document Workflows:** Maintain clear documentation for team members

## Troubleshooting CI/CD Issues

### Pipeline Failures

**Symptom:** Pipeline fails during processing

**Common Causes:**
- API rate limiting
- Insufficient permissions
- Missing configuration
- Timeout errors

**Solutions:**
- Check API quotas and limits
- Verify CI/CD permissions
- Validate configuration files
- Increase timeout values

### Performance Issues

**Symptom:** Pipeline takes too long

**Solutions:**
- Cache CLI downloads
- Process in parallel when possible
- Use `--no-summary` for faster processing
- Optimize batch sizes

## Related Documentation

- [Performance Tuning](performance-tuning.md) - Optimize processing performance
- [Batch Operations](batch-operations.md) - Efficient bulk processing
- [Configuration](../configuration/ai-services.md) - Configuration options
- [Troubleshooting](../troubleshooting/common-issues.md) - Solve common problems

## Example Projects

See the [tutorials section](../tutorials/) for complete CI/CD integration examples and sample projects.
