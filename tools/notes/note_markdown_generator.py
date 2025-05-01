#!/usr/bin/env python3
"""
PDF note generator module for creating Markdown notes for PDF files.

A comprehensive tool for automatically generating structured Markdown notes 
for PDF documents and integrating them into an Obsidian vault while supporting
external file storage through OneDrive shared links.

Key Features:
- Creates richly formatted Markdown notes with consistent structure and metadata
- Extracts text content from PDFs for AI-powered summary and analysis generation
- Uses OpenAI API to generate intelligent summaries based on document content
- Applies standardized YAML frontmatter with proper metadata and tagging
- Supports both local file references and OneDrive shared links
- Preserves user notes during updates with smart section management
- Integrates with course/program hierarchical organization structure
- Respects "readonly" flags to prevent overwriting customized content
- Extensive logging and error handling for reliability

This module serves as a core component of the Notebook Generator suite,
handling PDF document integration and knowledge extraction to create a
comprehensive learning resource library.
"""

import os
import re
import yaml
from pathlib import Path
from datetime import datetime

from ..utils.config import VAULT_LOCAL_ROOT
from ..utils.config import logger
from ..metadata.path_metadata import load_metadata_templates, get_reference_template
from ..metadata.yaml_metadata_helper import yaml_to_string
from ..ai.summarizer import generate_summary_with_openai
from ..pdf.processor import extract_pdf_text
from ..ai.prompt_utils import CHUNK_PROMPT_TEMPLATE, FINAL_PROMPT_TEMPLATE, DEFAULT_SYSTEM_PROMPT
from  ..metadata.yaml_metadata_helper import build_yaml_frontmatter            

def create_or_update_markdown_note_for_pdf(pdf_path, vault_dir, friendly_filename, include_embed=True, embed_html=None, dry_run=False, sharing_link=None, course_program_metadata=None):
    """
    Creates or updates a markdown note for a PDF file in an Obsidian vault with 
    AI-generated summaries and comprehensive metadata.    
    
    Args:
        pdf_path (str or Path): Path to the source PDF file in the local filesystem
        vault_dir (str or Path): Directory in the Obsidian vault where the note will be created
        friendly_filename (str): Base name of the PDF without extension (used for note title and filename)
        include_embed (bool, optional): Whether to include PDF embed HTML code. Defaults to True.
        embed_html (str, optional): Custom HTML to embed the PDF in the note. If None, default embedding is used. Defaults to None.
        dry_run (bool, optional): If True, performs all operations but doesn't write any files. Defaults to False.
        sharing_link (str, optional): OneDrive sharing link for the PDF to include in note. Defaults to None.
        course_program_metadata (dict, optional): Additional metadata about course/program context. 
                                                 May include: program, course, class, module, and force (bool) to force PDF reprocessing. 
                                                 Defaults to None.
    
    """
    logger.info(f"Starting to create or update the markdown note for: {friendly_filename}")
    
    file_path = Path(pdf_path)
    text_to_summarize=""

    # Extract PDF text first or use the cached version if available
    try:
        logger.info("Extracting PDF text or loading PDF from cache...")
        # Get force flag from args if available
        force_extraction = course_program_metadata.get('force', False) if course_program_metadata else False
        logger.info(f"Calling extract_pdf_text with the force to reprocess PDF if the txt version exist set to {force_extraction}")
        text_to_summarize = extract_pdf_text(pdf_path, force=force_extraction)
    except Exception as e:
        logger.error(f"Error extracting text from PDF {pdf_path}: {e}")
        text_to_summarize = ""    # Load templates 
            
    return create_or_update_markdown_note(text_to_summarize, vault_dir, file_path, friendly_filename, include_embed, embed_html, dry_run, sharing_link, course_program_metadata, template_type="pdf-reference")
    
def create_or_update_markdown_note(text_to_summarize, vault_dir, file_path, friendly_filename, include_embed=True, embed_html=None, dry_run=False, sharing_link=None, course_program_metadata=None, template_type="video-reference"):
    """
    Creates or updates a markdown note for a file in an Obsidian vault with AI-generated summaries and comprehensive metadata.
    
    This function serves as the main entry point for the note generation workflow
    and orchestrates the entire process from summarizing to final note creation.
    It supports both creating new notes and updating existing ones, while respecting
    user customizations and preserving manual notes.
    
    Workflow Steps:
    1. Check if note already exists and respect 'readonly' flags in frontmatter
    3. Load and apply appropriate metadata templates for the notes based off the template-type
    4. Build comprehensive YAML frontmatter with metadata from multiple sources
    5. Generate AI-powered summaries and content analysis using OpenAI API
    6. Preserve any existing user notes when updating existing files
    7. Write the assembled markdown content to the Obsidian vault (unless in dry_run mode)
    
    Integration Features:
    - Connects with OneDrive sharing system for external file references
    - Compatible with the hierarchical course/program/class organization structure
    - Uses consistent naming and metadata conventions for system-wide compatibility
    - Supports the Obsidian-based note-taking and knowledge management workflow
    - Generates AI summaries using OpenAI's advanced language models
    
    File Structure Generated:
    - YAML frontmatter with metadata (template-type, title, creation date, etc.)
    - Markdown content with AI-generated summaries and analysis
    - Notes section at the end marked with "## 📝 Notes" heading
    
    Metadata Handling:
    - Automatically extracts and includes file metadata (size, upload date)
    - Incorporates course/program context when provided
    - Sets proper YAML formatting with special character handling
    - Generates appropriate tags based on content and context
    - Maintains consistency with the global metadata schema
    
    Notes Preservation:
    - Any existing content in the "## 📝 Notes" section is preserved during updates
    - Notes are always maintained at the end of the document
    - New notes section is created for new files with placeholder text
    
    Error Handling:
    - PDF extraction failures are logged but don't prevent note creation
    - File read/write errors are caught and returned in the result
    - No exceptions are thrown; all errors are handled internally
    - Comprehensive logging for troubleshooting and process monitoring
    
    Args:
        text_to_summarize (str): Text content to summarize for example, it could be extracted from the PDF or Video transcript.
        vault_dir (str or Path): Directory in the Obsidian vault where the note will be created
        file_path (str or Path): Path to the source file in the local filesystem
        friendly_filename (str): Base name of the file without extension (used for note title and filename)
        include_embed (bool, optional): Whether to include PDF embed HTML code. Defaults to True.
        embed_html (str, optional): Custom HTML to embed the PDF in the note. If None, default embedding is used. Defaults to None.
        dry_run (bool, optional): If True, performs all operations but doesn't write any files. Defaults to False.
        sharing_link (str, optional): OneDrive sharing link for the PDF to include in note. Defaults to None.
        course_program_metadata (dict, optional): Additional metadata about course/program context. 
                                                 May include: program, course, class, module, and force (bool) to force PDF reprocessing. 
                                                 Defaults to None.
        template_type (str, optional): Type of template to use for the note. Defaults to "video-reference".
    
    Returns:
        dict: Result dictionary containing:
            - note_path (str): Absolute path to the created/updated note file
            - success (bool): Whether the operation completed without errors
            - error (str or None): Error message if any occurred, None otherwise
            - tags (list): List of tags generated for the note
            - file (str): Source PDF file path as string
            
    Raises:
        No exceptions are raised externally; all errors are caught and returned in the result dictionary.
        
    Note:
        - If the note exists and has "auto-generated-state": "readonly" in its frontmatter,
          the function respects this and returns immediately without updating.
        - AI summary generation requires an OpenAI API key to be properly configured.
        - User notes are always preserved when updating existing files.
        - All operations are logged with detailed information for troubleshooting.
        - In dry_run mode, all processing occurs but no files are written.
    
    Example:
        ```python
        result = create_or_update_markdown_note(
            text_to_summarize="This is the text extracted from the PDF or Video Transcript",
            vault_dir="/path/to/obsidian/vault/Course",
            file_path="/path/to/document.pdf",
            friendly_filename="Document Title",
            sharing_link="https://1drv.ms/b/s!ABC123",
            course_program_metadata={
                "program": "MBA Program",
                "course": "Financial Analysis",
                "class": "Week 3 Materials"
            }
            template_type="video-reference"
        )
        
        if result['success']:
            print(f"Note created at {result['note_path']}")
        else:
            print(f"Error: {result['error']}")
        ```
    """    
    logger.info(f"Starting to create or update the markdown note for: {friendly_filename}")
    logger.info(f"Using template_type: {template_type}")
    
    vault_dir = Path(vault_dir)
    # Remove any file extension from friendly_filename to ensure clean naming
    friendly_filename_clean = Path(friendly_filename).stem
    markdown = ""
    
    note_title = friendly_filename_clean.replace("-", " ").replace("_", " ").title()
    
    note_name = f"{friendly_filename_clean}-notes.md"    
    onedrive_sharing_link = sharing_link
    
    if template_type == "video-reference":

        note_name = f"{friendly_filename_clean}-video.md"
        logger.info(f"Creating video note with name: {note_name}")
        
        transcript_link = course_program_metadata.get("transcript_link")
        markdown += f"# {note_title}\n"    
        markdown += f"## 🔗 Video Source\n"
        markdown += f"- 📺 Link: [Watch here]({onedrive_sharing_link})\n"
        if transcript_link:
            markdown += f"- 📜 Transcript: [Read]({transcript_link})\n"
        
        logger.info(f"Note Markdown: {markdown}\n")
        
    elif template_type == "pdf-reference":
        note_name = f"{friendly_filename_clean}-notes.md"
        logger.info(f"Creating PDF note with name: {note_name}")
    
    note_path = vault_dir / note_name
    
    # Create the vault directory if it doesn't exist
    if not dry_run:
        os.makedirs(vault_dir, exist_ok=True)
    
    # Check if the note already exists and respects auto-generated-state
    logger.info(f"Checking to see if the note already exist? {note_path}")

    if note_path.exists():
        try:
            logger.info(f"{note_path} already exists, checking for readonly flag")
            with open(note_path, 'r', encoding='utf-8') as f:
                existing_content = f.read()
            
            # Check if the note is marked as read-only by reading the YAML frontmatter
            yaml_match = re.match(r"---\n(.*?)\n---", existing_content, re.DOTALL)
            
            if yaml_match:
                yaml_text = yaml_match.group(1)
                yaml_data = yaml.safe_load(yaml_text)
                
                if yaml_data.get("auto-generated-state") == "readonly":
                    logger.info(f"Note {note_path} is marked as readonly, skipping update")
                    return note_path
        except Exception as e:
            logger.warning(f"Error checking existing note {note_path}: {e}")
    
    # file_path
    logger.info("Loading the base metadata template for the file.")
    try:
        templates = load_metadata_templates()
        if templates:
            logger.info(f"Loaded {len(templates)} metadata templates")
            
            # Log the template types for debugging
            template_types = [t.get("template-type") for t in templates if isinstance(t, dict)]
            logger.info(f"Available template types: {template_types}")
        else:
            logger.warning("No templates were loaded")
    except Exception as e:
        logger.error(f"Error loading metadata templates: {e}")
        templates = []
 
    template = get_reference_template(templates)    
    logger.debug(f"Selected reference template: {template}")    # Build YAML frontmatter early for use in both summary and final content
    logger.debug("Augmenting the base yaml frontmatter.")
    logger.info(f"Building YAML frontmatter with template_type: {template_type}")
    logger.info(f"Course metadata being passed: {course_program_metadata}")
    
    yaml_dict = build_yaml_frontmatter(
        friendly_filename=friendly_filename_clean,
        file_path=file_path,
        sharing_link=sharing_link,
        metadata=course_program_metadata,
        template=template,
        template_type=template_type
    )
    
    logger.info(f"Sending Metadata to Prompts: \n{yaml_dict}")
    
    # set the metadata for the system prompt
    system_prompt = DEFAULT_SYSTEM_PROMPT
    
    # Chunked system prompt for processing individual chunks
    chunked_system_prompt = CHUNK_PROMPT_TEMPLATE
    
    # User prompt for final consolidation
    user_prompt = FINAL_PROMPT_TEMPLATE.replace("{{yaml-frontmatter}}", yaml_to_string(yaml_dict))
    
    # Format the user prompt with the text and metadata    
    logger.debug("Loading the prompt templates with metadata already populated.")
    
    
    markdown_from_openai = ""
    if text_to_summarize:
        logger.debug(f"System prompt: {system_prompt[:100]}...")
        logger.debug(f"Chunk prompt template: {chunked_system_prompt[:100]}...")
        logger.debug(f"User prompt template: {user_prompt[:100]}...")    
        logger.info(f"Text to summarize: {text_to_summarize[:100]}...")        
        logger.debug("Calling into generate_summary_with_openai")    
        markdown_from_openai += generate_summary_with_openai(
            text_to_summarize,    
            system_prompt=system_prompt,
            chunked_system_prompt=chunked_system_prompt,
            user_prompt=user_prompt,
            metadata=yaml_dict,
            dry_run=dry_run
        )
        logger.info(f"Summary was returned from generate_summary_with_openai: {markdown[:100]} \n...")
    else:
        logger.warning("No text to summarize, skipping summary generation")
        markdown_from_openai = ""
    
    # Extract YAML frontmatter from the OpenAI generated content if present
    markdown_yaml = ""
    if markdown_from_openai.startswith("---"):
        yaml_match = re.match(r"---\n(.*?)\n---", markdown_from_openai, re.DOTALL)
        if yaml_match:
            yaml_text = yaml_match.group(0)
            markdown_yaml = yaml_text
            # Remove the YAML frontmatter from markdown_from_openai
            markdown_from_openai = markdown_from_openai[len(yaml_text):].lstrip()
            logger.info("Extracted YAML frontmatter from AI-generated content")
    
    # Compose the final markdown with proper ordering
    markdown = markdown_yaml + "\n" + markdown + markdown_from_openai
    
    # Initialize error variable
    error = None

    # Check if the note exists to preserve any existing notes
    existing_notes_content = None
    if note_path.exists():
        try:
            with open(note_path, 'r', encoding='utf-8') as f:
                existing_content = f.read()
                
            # Look for the notes section which is always at the end, starting with "## 📝 Notes"
            notes_match = re.search(r'(## 📝 Notes[\s\S]*?)$', existing_content)
            if notes_match:
                existing_notes_content = notes_match.group(1)
                logger.info("Found existing notes section, will preserve it")
                logger.info(f"Existing notes content: {existing_notes_content[:50]}...")
        except Exception as e:
            logger.error(f"Error reading existing note file: {e}")
            error = str(e)
    
    # Add notes section either from existing content or create new one
    notes_header = "## 📝 Notes\n\nAdd your notes here."
    
    # Ensure the markdown content doesn't end with blank lines to maintain proper spacing    
    markdown = markdown.rstrip()
    
    if existing_notes_content:
        logger.info("Appending existing notes section to the new content")
        markdown += f"\n\n{existing_notes_content}"
    else:
        logger.info("Adding new notes section to the content")
        markdown += f"\n\n{notes_header}"
    
    try:
        if not dry_run:
            with open(note_path, 'w', encoding='utf-8') as f:
                f.write(markdown)
            logger.info(f"Created or updated markdown note: {note_path}")
        else:
            logger.info(f"[DRY RUN] Would create note at: {note_path}")
    except Exception as e:
        logger.error(f"Error writing note file: {e}")
        error = str(e)
    
    if not text_to_summarize:
        return {
            'note_path': str(note_path),
            'success': False,
            'error': "No text to summarize",
            'tags': yaml_dict.get('tags', []),
            'file': str(file_path)
        }
    else:
        # Return a result dictionary for workflow compatibility
        return {
            'note_path': str(note_path),
            'success': error is None,
            'error': error,
            'tags': yaml_dict.get('tags', []),
            'file': str(file_path)
        }