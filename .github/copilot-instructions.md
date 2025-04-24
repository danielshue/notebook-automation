Notebook Generator - A unified tool for managing MBA course notes

This script provides comprehensive automation for organizing course materials in an Obsidian vault.
It combines multiple workflows into a single unified tool to streamline course note management.

Key features:
----------------
1. File Conversion
   - Converts HTML files to Markdown with proper formatting
   - Converts transcript TXT files to Markdown
   - Cleans up filenames by removing numbering prefixes and improving readability
   - Adds YAML frontmatter with template type and auto-generated state properties

2. Index Generation
   - Creates a hierarchical structure of index files for easy navigation
   - Supports a 6-level hierarchy: Main → Program → Course → Class → Module → Lesson
   - Generates Obsidian-compatible wiki-links between index levels
   - Creates back-navigation links for seamless browsing
   - Respects "readonly" marked files to prevent overwriting customized content

3. Content Organization
   - Automatically categorizes content into readings, videos, transcripts, etc.
   - Adds appropriate icons for different content types
   - Implements a tagging system for enhanced content discovery
   - Supports structural, cognitive, and workflow tags

Directory Structure:
-------------------
- Root (main-index)
  - Program Folders (program-index)
    - Course Folders (course-index)
      - Case Study Folders (case-studies-index)
      - Class Folders (class-index)
        - Module Folders (module-index)
          - Live Session Folder (live-session-index)
          - Lesson Folders (lesson-index)
            - Content Files (readings, videos, transcripts, etc.)

pip install pyyaml html2text

Usage:
-------
  python mba_notebook_tool.py --convert --source <path>  # Convert files
  python mba_notebook_tool.py --generate-index --source <path>  # Generate indexes
  python mba_notebook_tool.py --all --source <path>  # Do both operations

PDF Note Generator for Course Materials with OneDrive Shared Links

This script scans PDFs from OneDrive MBA-Resources folder, creates shareable links in OneDrive,
and generates corresponding reference notes (markdown file) in the Obsidian vault. The script 
maintains the same folder structure from OneDrive in your Obsidian Vault.

Features:
- Authenticates with Microsoft Graph API for secure access to OneDrive
- Creates shareable links for PDFs stored in OneDrive
- Generates markdown notes with both local file:// links and shareable OneDrive links
- Extracts text from PDFs and generates AI-powered summaries with OpenAI
- Automatically generates relevant tags using content analysis via OpenAI
- Infers course and program information from file paths
- Maintains consistent folder structure between OneDrive and Obsidian vault
- Robust error handling with categorization and retry mechanism
- Integration with Microsoft Graph API using Azure best practices
- Preserves user modifications to notes (respects auto-generated-state flag)
- Secure token cache management with encryption

Generated Markdown Structure:
The script generates comprehensive markdown notes with the following structure:
1. YAML Frontmatter - Rich metadata including:
   - auto-generated-state: Tracks if note can be auto-updated (default: writable)
   - template-type: Type of template used (pdf-reference)
   - title: PDF title derived from filename
   - pdf-path: Relative path to the PDF in OneDrive
   - onedrive-pdf-path: Direct file:// URL to open PDF locally
   - onedrive-sharing-link: Shareable OneDrive link (when available)
   - date-created: Creation timestamp
   - tags: Auto-generated tags using OpenAI analysis
   - program/course: Inferred from file path structure
   - pdf-uploaded: PDF creation date
   - pdf-size: File size in MB
   - status: Reading status tracking (unread/in-progress/complete)
   - completion-date: When reading was finished (user filled)
   - review-date: When content was reviewed (user filled)
   - comprehension: Self-assessed understanding level (user filled)

2. Content Sections - AI-generated content including:
   - PDF Reference with links to local and OneDrive shared versions
   - Topics Covered: Key topics in bullet point format
   - Key Concepts Explained: Detailed explanations of main concepts
   - Important Takeaways: Practical applications and insights
   - Summary: Concise overview of PDF content
   - Notable Quotes/Insights: Important quotes from the document
   - Questions: Reflection prompts to connect content to broader learning
   - Notes: Section for user's personal notes

Usage:
    wsl python3 generate_pdf_notes_updated.py                       # Process all PDFs in OneDrive
    wsl python3 generate_pdf_notes_updated.py -f "path/to/file.pdf" # Process a single PDF file (relative to OneDrive)
    wsl python3 generate_pdf_notes_updated.py --folder "folder"     # Process PDFs in a specific OneDrive subfolder
    wsl python3 generate_pdf_notes_updated.py --dry-run             # Test without making changes
    wsl python3 generate_pdf_notes_updated.py --no-share-links      # Skip OneDrive shared links (faster)
    wsl python3 generate_pdf_notes_updated.py --debug               # Enable debug logging
    wsl python3 generate_pdf_notes_updated.py --retry-failed        # Only retry previously failed files
    wsl python3 generate_pdf_notes_updated.py --force               # Force overwrite of existing notes
    wsl python3 generate_pdf_notes_updated.py --timeout 15          # Set custom API request timeout (seconds)

Environment Variables:
    OPENAI_API_KEY: API key for OpenAI (required for AI summary and tag generation)

Requirements:
    - requests: For HTTP communication with Microsoft Graph API
    - msal: Microsoft Authentication Library for secure Azure AD authentication
    - pdfplumber: For extracting text from PDFs
    - openai: For AI-powered summary and tag generation
    - pyyaml: For parsing YAML templates and frontmatter
    - python-dotenv: For loading environment variables
    - cryptography: For secure token cache encryption
    - urllib3: For retry strategies and connection pooling