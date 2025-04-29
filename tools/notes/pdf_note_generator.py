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
from ..metadata.path_metadata import load_metadata_templates
from ..metadata.yaml_metadata_helper import yaml_to_string
from ..ai.summarizer import generate_summary_with_openai
from ..pdf.processor import extract_pdf_text
from ..ai.prompt_utils import CHUNK_PROMPT_TEMPLATE, FINAL_PROMPT_TEMPLATE, DEFAULT_SYSTEM_PROMPT

def _get_pdf_reference_template(templates):
    """
    Get the PDF reference template from templates dictionary or list.
    
    This function performs intelligent lookup of the PDF reference template from either:
    1. A dictionary keyed by template types
    2. A list of template dictionaries with template-type field
    3. Creates a fallback template if no matching template is found
    
    The function handles multiple data structures and performs case-insensitive matching
    to ensure robustness when dealing with different template collections. It also
    provides comprehensive logging to trace template selection decisions.
    
    Args:
        templates (dict or list): Dictionary of templates by type or list of template dictionaries.
                                 If dict, keys are expected to be template types.
                                 If list, each item should be a dict with 'template-type' field.
        
    Returns:
        dict: PDF reference template with required fields or a fallback template if none found.
              Always returns a valid template structure that can be used for note generation.
    """
    # Handle case when templates is a dictionary (keyed by template-type)
    if isinstance(templates, dict):
        # Direct lookup if it's a dictionary
        if "pdf-reference" in templates:
            logger.info(f"Found PDF reference template by key lookup")
            return templates["pdf-reference"]
        else:
            # Try case-insensitive lookup
            for key, template in templates.items():
                if isinstance(key, str) and key.lower() == "pdf-reference":
                    logger.info(f"Found PDF reference template with case-insensitive key: {key}")
                    return template
    
    # Handle case when templates is a list
    elif isinstance(templates, (list, tuple)):
        for template in templates:
            # Skip any non-dict elements
            if not isinstance(template, dict):
                continue            
            if template.get("template-type") == "pdf-reference":
                logger.info("Found PDF reference template in list")
                return template
    
    # If we get here, no template was found
    logger.warning("No PDF reference template found in templates")
    
    # Create a basic fallback template
    fallback_template = {
        "template-type": "pdf-reference",
        "auto-generated-state": "writable",
        "tags": []
    }
    
    return fallback_template

def _build_pdf_yaml_frontmatter(pdf_name, pdf_path, sharing_link=None, metadata=None, template=None):
    """
    Build YAML frontmatter for a PDF note using the pdf-reference template.
    
    This function constructs a comprehensive YAML frontmatter dictionary for PDF reference notes
    by combining data from multiple sources:
    
    1. Base template (provided or fallback)
    2. File metadata (size, creation date)
    3. Course/program hierarchical context
    4. OneDrive linking information
    
    The resulting frontmatter follows the consistent structure expected by the Obsidian vault
    and maintains compatibility with the broader notebook organization system.
    
    Args:
        pdf_name (str): Name of the PDF without extension, used as the note title
        pdf_path (str or Path): Path to the PDF file in the filesystem
        sharing_link (str, optional): OneDrive sharing link for external access
        metadata (dict, optional): Additional metadata like program/course hierarchical info
                                  Can include: program, course, class, module
        template (dict, optional): Base template dictionary to start with
        
    Returns:
        dict: Complete frontmatter dictionary with all required fields populated
              including template-type, title, file paths, size info, and metadata
    """
    pdf_path = Path(pdf_path)
      # Handle the template
    # Start with a copy of the template (or empty dict if none)
    yaml_dict = template.copy() if template else {}
    
    # Set required fields (overwrite if they already exist)
    yaml_dict["template-type"] = "pdf-reference"
    yaml_dict["auto-generated-state"] = "writable"
    yaml_dict["title"] = pdf_name
    yaml_dict["date-created"] = datetime.now().strftime("%Y-%m-%d")
        
    # Set PDF-specific fields
    try:
        rel_path = pdf_path.relative_to(VAULT_LOCAL_ROOT) #RESOURCES_ROOT
        yaml_dict["vault-path"] = str(rel_path).replace("\\", "/")
    except (ValueError, AttributeError):
        yaml_dict["vault-path"] = str(pdf_path).replace("\\", "/")
    
    
    yaml_dict["onedrive-path"] = pdf_path
            
    # Set sharing link if provided
    if sharing_link:
        yaml_dict["onedrive-sharing-link"] = sharing_link
        
    # Set file metadata
    try:
        yaml_dict["pdf-size"] = f"{round(pdf_path.stat().st_size / (1024 * 1024), 2)} MB"
        yaml_dict["pdf-uploaded"] = datetime.fromtimestamp(pdf_path.stat().st_ctime).strftime("%Y-%m-%d")
    except:
        yaml_dict["pdf-size"] = "Unknown"
        yaml_dict["pdf-uploaded"] = "Unknown"
    
    # Set course/program metadata if provided
    if metadata:
        for key in ["program", "course", "class", "module"]:
            if key in metadata and metadata[key]:
                yaml_dict[key] = metadata[key]
    
    # Set default values for other fields if not already set
    if not yaml_dict.get("status"):
        yaml_dict["status"] = "unread"
    #if not yaml_dict.get("tags"):
    
    yaml_dict["tags"] = []
    
    return yaml_dict

def create_or_update_markdown_note_for_pdf(pdf_path, vault_dir, pdf_stem, include_embed=True, embed_html=None, dry_run=False, sharing_link=None, course_program_metadata=None):
    """
    Creates or updates a markdown note for a PDF file in an Obsidian vault with 
    AI-generated summaries and comprehensive metadata.
    
    This function serves as the main entry point for the PDF note generation workflow
    and orchestrates the entire process from PDF text extraction to final note creation.
    It supports both creating new notes and updating existing ones, while respecting
    user customizations and preserving manual notes.
    
    Workflow Steps:
    1. Check if note already exists and respect 'readonly' flags in frontmatter
    2. Extract text content from the PDF using the PDF processor module
    3. Load and apply appropriate metadata templates for PDF reference notes
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
        pdf_path (str or Path): Path to the source PDF file in the local filesystem
        vault_dir (str or Path): Directory in the Obsidian vault where the note will be created
        pdf_stem (str): Base name of the PDF without extension (used for note title and filename)
        include_embed (bool, optional): Whether to include PDF embed HTML code. Defaults to True.
        embed_html (str, optional): Custom HTML to embed the PDF in the note. If None, default embedding is used. Defaults to None.
        dry_run (bool, optional): If True, performs all operations but doesn't write any files. Defaults to False.
        sharing_link (str, optional): OneDrive sharing link for the PDF to include in note. Defaults to None.
        course_program_metadata (dict, optional): Additional metadata about course/program context. 
                                                 May include: program, course, class, module, and force (bool) to force PDF reprocessing. 
                                                 Defaults to None.
        
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
        result = create_or_update_markdown_note_for_pdf(
            pdf_path="/path/to/document.pdf",
            vault_dir="/path/to/obsidian/vault/Course",
            pdf_stem="Document Title",
            sharing_link="https://1drv.ms/b/s!ABC123",
            course_program_metadata={
                "program": "MBA Program",
                "course": "Financial Analysis",
                "class": "Week 3 Materials"
            }
        )
        
        if result['success']:
            print(f"Note created at {result['note_path']}")
        else:
            print(f"Error: {result['error']}")
        ```
    """
    logger.info(f"Starting to create or update the markdown note for PDF: {pdf_path}")
    
    pdf_path = Path(pdf_path)
    vault_dir = Path(vault_dir)
    note_name = f"{pdf_stem}-Notes.md"
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
    
    # Extract PDF text first or use the cached version if available
    try:
        logger.info("Extracting PDF text or loading from cache...")
        # Get force flag from args if available
        force_extraction = course_program_metadata.get('force', False) if course_program_metadata else False
        logger.info(f"Calling extract_pdf_text with the force to reprocess PDF if the txt version exist set to {force_extraction}")
        pdf_text = extract_pdf_text(pdf_path, force=force_extraction)
    except Exception as e:
        logger.error(f"Error extracting text from PDF {pdf_path}: {e}")
        pdf_text = ""    # Load templates 
    logger.info("Loading the base metadata template for the PDF")
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
 
    template = _get_pdf_reference_template(templates)    
    logger.debug(f"Selected PDF template: {template}")
 
    # Build YAML frontmatter early for use in both summary and final content
    logger.debug("Augmenting the base pdf yaml frontmatter.")
    
    yaml_dict = _build_pdf_yaml_frontmatter(
        pdf_name=pdf_stem,
        pdf_path=pdf_path,
        sharing_link=sharing_link,
        metadata=course_program_metadata,
        template=template
    )
    
    logger.info(f"Sending Metadata to Prompts: \n{yaml_dict}")
    
    # set the metadata for the system prompt
    system_prompt = DEFAULT_SYSTEM_PROMPT
    
    # Chunked system prompt for processing individual chunks
    chunked_system_prompt = CHUNK_PROMPT_TEMPLATE
    
    # User prompt for final consolidation
    user_prompt = FINAL_PROMPT_TEMPLATE.replace("{{yaml-frontmatter}}", yaml_to_string(yaml_dict))
    
    # Format the user prompt with the PDF text and metadata    
    logger.debug("Loading the prompt templates with metadata already populated.")
    logger.debug(f"System prompt: {system_prompt[:100]}...")
    logger.debug(f"Chunk prompt template: {chunked_system_prompt[:100]}...")
    logger.debug(f"User prompt template: {user_prompt[:100]}...")    
    logger.debug("Calling into generate_summary_with_openai")
    
    markdown = generate_summary_with_openai(
        pdf_text,    
        system_prompt=system_prompt,
        chunked_system_prompt=chunked_system_prompt,
        user_prompt=user_prompt,
        metadata=yaml_dict,
        dry_run=dry_run
    )
    logger.info(f"Summary was returned from generate_summary_with_openai: {markdown[:100]} \n...")
    
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
    notes_header = "## 📝 Notes\n\nAdd your notes about the PDF here."
    
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

    # Return a result dictionary for workflow compatibility
    return {
        'note_path': str(note_path),
        'success': error is None,
        'error': error,
        'tags': yaml_dict.get('tags', []),
        'file': str(pdf_path)
    }
