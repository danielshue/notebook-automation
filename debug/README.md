# Debug Tools for MBA Notebook Automation

This directory contains various debug and troubleshooting scripts for the MBA Notebook Automation system. These scripts are not required for regular operation but may help diagnose issues when they arise.

## Contents

- **debug_transcript_finder.py** - A standalone version of the transcript finder functionality for testing problems with transcript file discovery
- **debug_tag_doc.py** - Debug version of tag document generator
- **debug_script.py** - Simple wrapper to catch and print exceptions for any other script
- **debug_transcript.sh** - Shell script to run transcript debug operations

## Usage

These scripts should only be used for debugging purposes when issues arise with the main system. They are not required for normal operation and generally run simplified versions of the functionality in the main scripts to isolate issues.

### Examples

```
python debug/debug_transcript_finder.py "/path/to/video.mp4"
python debug/debug_tag_doc.py "/path/to/vault"
```

Note: When running these scripts, you may need to adjust import paths if running directly from the debug directory.
