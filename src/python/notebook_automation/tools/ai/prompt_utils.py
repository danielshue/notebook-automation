#!/usr/bin/env python3
"""
AI Prompt Templates module for content summarization.

This module provides utilities for loading, managing, and formatting AI prompt templates
used in summarizing educational content (PDFs, videos, transcripts) for the
Notebook Generator system. It handles:

1. Loading prompt templates from files with fallback defaults
2. Formatting templates with metadata for chunk-based and final summarization
3. Managing template variables for different content types
4. Supporting context-aware AI prompt generation for OpenAI API integration
5. Handling both video and PDF content summarization workflows
"""

import re
import os
import logging
from pathlib import Path
from ..utils.config import ensure_logger_configured
from ..metadata.yaml_metadata_helper import yaml_to_string

# Initialize module logger
logger = ensure_logger_configured(__name__)

# Paths for loading external prompt templates
# The system uses markdown files in the shared repository root Prompts directory 
# to allow customization of the prompts without changing the code.
# These constants define the paths to those files.
# Determine the repository root path to locate the shared Prompts directory
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '../../../../..'))
PROMPT_DIR = os.path.join(REPO_ROOT, 'Prompts')
CHUNK_PROMPT_PATH = os.path.join(PROMPT_DIR, 'chunk_summary_prompt.md')  # For individual chunk processing
FINAL_PROMPT_PATH = os.path.join(PROMPT_DIR, 'final_summary_prompt.md')  # For the final combined summary
VIDEO_FINAL_PROMPT_PATH = os.path.join(PROMPT_DIR, 'final_summary_prompt_video.md')  # For video summarization

# System prompt provides concise instructions about the AI's role and task
# This is typically used as the "system message" in ChatGPT/OpenAI API calls
# It establishes the AI's expertise domain and general guidance for all responses
DEFAULT_SYSTEM_PROMPT = """You are an expert MBA educational content summarizer. Your task is to create clear, structured summaries with the following:

1. A concise overview of the key points and main arguments
2. Important business concepts and frameworks introduced
3. Practical implications and applications
4. Key takeaways for business strategy and management

Always use proper markdown formatting with headers and bullet points. Structure your summary with clear sections to enhance readability."""

def _load_prompt(path, default):
    """
    Load a prompt template from a file with fallback to a default value.
    
    This internal helper function attempts to read a prompt template from the given path
    and returns the default value if the file cannot be read for any reason.
    
    Args:
        path (str): Path to the prompt template file
        default (str): Default template to use if file can't be read
        
    Returns:
        str: Content of the prompt template file or the default value
    """
    try:
        with open(path, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception:
        return default


# Default chunk prompt - Used for processing individual chunks of content
# This prompt instructs the AI to analyze a specific chunk of content and extract key information
# Format variables:
# - {chunk_context}: Additional context about the chunk (e.g., "This is part X of Y. ")
# - {chunk_context_lower}: Lowercase version of chunk context for natural language flow
# - {file_name}: Name of the file being processed
# - {course_name}: Name of the course the content belongs to
# - {chunk}: The actual content chunk to be analyzed
DEFAULT_CHUNK_PROMPT = (
    "You are an educational content summarizer for MBA course materials.\n"
    "{chunk_context}Analyze this file chunk and extract the key information.\n\n"
    "For each chunk, identify:\n"
    "1. Main topics discussed\n"
    "2. Key concepts explained\n"
    "3. Important takeaways or insights\n"
    "4. Notable quotes\n"
    "5. Questions that might arise from this content\n\n"
    "Format your response as simple bullet points under each category.\n"
    "Keep your response concise but informative.\n\n"
    "This is {chunk_context_lower} from a video titled '{file_name}' from the course '{course_name}':\n\n{chunk}"
)

# Default final summary prompt - Used for creating a comprehensive summary from chunk summaries
# This prompt defines the exact structure of the final markdown summary document
# Format variables:
# - {{yaml-frontmatter}}: YAML metadata to be included in the summary
# - {file_name}: Name of the file being processed
# - {course_name}: Name of the course the content belongs to
# - {combined_summary}: Combined summary text from all processed chunks
DEFAULT_FINAL_PROMPT = (
    "You are an educational content summarizer for MBA course materials.\n"
    "Create a comprehensive final summary based on the provided chunk summaries.\n\n"
    "Structure your response in markdown format with these exact sections:\n\n"
    "# 🎓 Educational Video Summary (AI Generated)\n\n"
    "## 🧩 Topics Covered\n"
    "- List 3-5 main topics covered in the video\n"
    "- Be specific and use bullet points\n\n"
    "## 📝 Key Concepts Explained\n"
    "- Explain the key concepts in 3-5 paragraphs\n"
    "- Focus on the most important ideas\n\n"
    "## ⭐ Important Takeaways\n"
    "- List 3-5 important takeaways as bullet points\n"
    "- Focus on practical applications and insights\n\n"
    "## 🧠 Summary\n"
    "- Write a concise 1-paragraph summary of the overall video content\n\n"
    "## 💬 Notable Quotes / Insights\n"
    "- Include 1-2 significant quotes or key insights from the video\n"
    "- Format as proper markdown blockquotes using '>' symbol\n\n"
    "## ❓ Questions\n"
    "- What did I learn from this video?\n"
    "- What's still unclear or needs further exploration?\n"
    "- How does this material relate to the broader course or MBA program?\n"
    "\n{{yaml-frontmatter}}\n\nThese are the chunk summaries from a video titled '{file_name}' from the course '{course_name}'. Create a comprehensive final summary following the format in your instructions:\n\n{combined_summary}"
)

# Load templates from files with fallback to default values
# These are the actual template strings used throughout the application
# If the files don't exist or can't be read, the default values defined above are used
CHUNK_PROMPT_TEMPLATE = _load_prompt(CHUNK_PROMPT_PATH, DEFAULT_CHUNK_PROMPT)
FINAL_PROMPT_TEMPLATE = _load_prompt(FINAL_PROMPT_PATH, DEFAULT_FINAL_PROMPT)

def load_prompt_template(prompt_name):
    """
    Load a prompt template from a markdown file in the prompts directory.
    
    This function locates and loads a prompt template from the project's prompts directory.
    It handles removing any frontmatter comments and provides proper error handling and logging.
    
    Args:
        prompt_name (str): Name of the prompt template file without extension
                           (e.g., "chunk_summary_prompt" for "chunk_summary_prompt.md")
        
    Returns:
        str: Content of the prompt template file or None if not found or error occurs
              The content will have any frontmatter comments removed
              
    Example:
        >>> template = load_prompt_template("final_summary_prompt")
        >>> if template:
        >>>     formatted_prompt = template.replace("{{variable}}", "value")
    """
    # Define the prompt directory path
    prompt_dir = Path(__file__).parent.parent.parent / "prompts"
    
    # Check if the file exists with .md extension
    prompt_file = prompt_dir / f"{prompt_name}.md"
    
    if not prompt_file.exists():
        logger.warning(f"Prompt template file not found: {prompt_file}")
        return None
    
    try:
        with open(prompt_file, "r", encoding="utf-8") as f:
            content = f.read()
            # Remove the frontmatter comment if present
            content = re.sub(r'<!--\s*filepath:.*?-->\s*', '', content, flags=re.DOTALL)
            logger.info(f"Loaded prompt template from {prompt_file}")
            return content
    except Exception as e:
        logger.error(f"Error loading prompt template: {e}")
        return None

def format_final_user_prompt_for_pdf(metadata):
    """
    Format the final user prompt for PDF summarization with provided metadata.
    
    This function takes the metadata dictionary for a PDF and formats the final
    prompt template by replacing template variables with actual values. It
    handles YAML frontmatter and OneDrive path information.
    
    Template Variables Replaced:
    - {{yaml-frontmatter}}: Full YAML frontmatter for the PDF
    - {{onedrive-path}}: Path to the PDF in OneDrive
    - {{onedrive-sharing-link}}: Shareable link for the PDF
    
    Args:
        metadata (dict): Dictionary containing PDF metadata including
                         'onedrive-path', 'onedrive-sharing-link', etc.
                         
    Returns:
        str: Formatted final prompt for PDF summarization
    """    # Load the template from file
    template = _load_prompt(FINAL_PROMPT_PATH, DEFAULT_FINAL_PROMPT)
    
    # If metadata is provided, format the template
    if metadata:
        yaml_frontmatter = yaml_to_string(metadata)
        template = template.replace("{{yaml-frontmatter}}", yaml_frontmatter)
        
        # Replace OneDrive paths and links if they exist
        onedrive_path = str(metadata.get('onedrive-path', ''))
        onedrive_sharing_link = str(metadata.get('onedrive-sharing-link', ''))
        template = template.replace("{{onedrive-path}}", onedrive_path)
        template = template.replace("{{onedrive-sharing-link}}", onedrive_sharing_link)   
    
    return template

def format_chuncked_user_prompt_for_pdf(metadata):
    """
    Format the chunk-based user prompt for PDF summarization with provided metadata.
    
    This function prepares the prompt used for summarizing individual chunks of PDF content.
    It replaces template variables with actual values from the metadata dictionary.
    
    Template Variables Replaced:
    - file_name: Name of the PDF file 
    - course_name: Name of the course the PDF belongs to
    - program_name: Name of the program
    
    Args:
        metadata (dict): Dictionary containing PDF metadata including
                         'file_name', 'course_name', and 'program_name'
                         
    Returns:
        str: Formatted chunk prompt for PDF summarization
    """    # Load the template from file
    template = _load_prompt(CHUNK_PROMPT_PATH, DEFAULT_CHUNK_PROMPT)
    
    # Replace the template variables that the C# code expects
    template = template.replace("{{onedrive-path}}", str(metadata.get('onedrive-path', 'Unknown File')))
    template = template.replace("{{course}}", str(metadata.get('course_name', 'MBA Course')))
    
    # The chunk will be replaced by the summarizer
    # In Python, we'll adapt to use content variable which is what C# expects
    
    return template