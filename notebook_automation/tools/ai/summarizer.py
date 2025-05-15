#!/usr/bin/env python3
"""
AI Module for Smart Content Summarization with OpenAI

This module provides sophisticated AI-powered summarization capabilities for the 
Notebook Generator system, handling both educational videos and PDFs through OpenAI's API.

Key Features:
------------
1. Intelligent Content Processing
   - Automatic chunking of large documents for effective processing
   - Overlapping chunks to maintain context continuity 
   - Special handling for first/last chunks to preserve document flow
   - Adaptive processing based on content length and complexity

2. Multi-stage Summarization Pipeline
   - Initial chunk-by-chunk analysis and summarization
   - Consolidated comprehensive summary generation
   - Context-aware processing using document metadata
   - Template-based prompt generation with variable substitution

3. PDF and Video Content Support
   - Specialized processing for different content types
   - Course-specific contextualization for educational materials
   - Metadata extraction and integration in summaries
   - Tag generation for improved content discovery

4. Production-ready Error Handling
   - Comprehensive logging throughout the summarization process
   - Graceful fallbacks when API keys are missing
   - Chunk processing retry mechanisms
   - Dry-run capability for testing without API calls

Integration Points:
-----------------
- Works with the prompt_utils module for template management
- Interfaces with metadata modules for YAML frontmatter handling
- Configurable through environment variables and config files
- Designed to be called from PDF and video processing pipelines
"""

import openai
from ..utils.config import OPENAI_API_KEY
from ..utils.config import logger
from ..metadata.yaml_metadata_helper import yaml_to_string
from ..ai.prompt_utils import CHUNK_PROMPT_TEMPLATE, FINAL_PROMPT_TEMPLATE
from ..ai.prompt_utils import format_final_user_prompt_for_pdf, format_chuncked_user_prompt_for_pdf  
from ..metadata.yaml_metadata_helper import replace_template_variables
from ..metadata.yaml_metadata_helper import replace_template_with_yaml_frontmatter

# Configure OpenAI API
if OPENAI_API_KEY:
    openai.api_key = OPENAI_API_KEY
else:
    logger.warning("OpenAI API key not found in environment variables. Summary and tag generation will be disabled.")

def _chunk_text(text, max_chunk_size=2000, overlap=500):
    """Split a long text into overlapping chunks for efficient API processing.
    
    This function divides large text documents into smaller, manageable chunks
    with configurable overlap between chunks to maintain context and coherence.
    The chunking algorithm ensures that no information is lost during the splitting
    process while optimizing for the token limits of the OpenAI API.
    
    Args:
        text (str): The text content to split into chunks
        max_chunk_size (int, optional): Maximum characters per chunk, designed 
            to work within API token limits. Defaults to 2000.
        overlap (int, optional): Number of characters to overlap between adjacent chunks
            to maintain context continuity. Defaults to 500.
    
    Returns:
        List[str]: List of text chunks ready for processing by summarization functions
        
    Example:
        >>> text = "This is a very long document that needs to be split into chunks..."
        >>> chunks = _chunk_text(text, max_chunk_size=1000, overlap=200)
        >>> print(f"Created {len(chunks)} chunks")
        Created 3 chunks
    """
    if len(text) <= max_chunk_size:
        #logger.debug(f"Text fits in one chunk: {len(text)} chars")
        return [text]
    chunks = []
    start = 0
    chunk_num = 1
    while start < len(text):
        end = min(start + max_chunk_size, len(text))
        chunk = text[start:end]
        #logger.debug(f"Chunk {chunk_num}: start={start}, end={end}, size={len(chunk)}")
        chunks.append(chunk)
        if end == len(text):
            break  # Reached the end
        # Calculate next start position
        next_start = end - overlap
        if next_start <= start:
            next_start = end  # Prevent infinite loop if overlap is too large
        start = next_start
        chunk_num += 1
    logger.debug(f"Split text into {len(chunks)} chunks (avg {len(text)/len(chunks):.0f} chars per chunk)")
    return chunks

def _summarize_chunk(chunk, system_prompt, chunked_system_prompt, user_prompt, metadata=None, is_first_chunk=False, is_final_chunk=False, chunk_num=1, total_chunks=1):
    """Summarize a single chunk of content using OpenAI API with context awareness.
    
    This function processes an individual text chunk through the OpenAI API
    with specialized contextual handling to ensure coherent summaries. It dynamically
    builds context information based on the chunk's position in the document
    (beginning, middle, or end) and injects this context into the prompts.
    
    Args:
        chunk (str): The content text chunk to summarize
        system_prompt (str): System prompt for OpenAI API that defines the AI's role
        chunked_system_prompt (str): Template for chunk-specific instructions with 
            variable placeholders
        user_prompt (str): User prompt template with variable placeholders 
            (including {chunk})
        metadata (dict, optional): Dictionary of metadata values to expand in prompt 
            templates. Typically includes document title, course info. Defaults to None.
        is_first_chunk (bool, optional): Flag indicating if this is the first chunk, 
            triggering special handling for introductory content. Defaults to False.
        is_final_chunk (bool, optional): Flag indicating if this is the final chunk, 
            triggering special handling for concluding content. Defaults to False.
        chunk_num (int, optional): Current chunk number for context and logging. 
            Defaults to 1.
        total_chunks (int, optional): Total number of chunks for context and progress 
            tracking. Defaults to 1.
    
    Returns:
        str: The AI-generated summary for this specific chunk, or an error message
            if the API call fails (allowing the overall process to continue)
            
    Raises:
        No exceptions are raised as they are caught and returned as error messages
        
    Example:
        >>> chunk_summary = _summarize_chunk(
        ...     chunk="This is the introduction to the topic...",
        ...     system_prompt="You are an educational content summarizer",
        ...     chunked_system_prompt="Summarize this {chunk_context}: {chunk}",
        ...     user_prompt="Create a summary of {file_name}",
        ...     metadata={'file_name': 'Chapter 1'},
        ...     is_first_chunk=True,
        ...     chunk_num=1,
        ...     total_chunks=3
        ... )
    """
    chunk_context = ""
    if total_chunks > 1:
        chunk_context = f"This is part {chunk_num} of {total_chunks} from the file. "
        if is_first_chunk:
            chunk_context += "This is the beginning of the file. "
        elif is_final_chunk:
            chunk_context += "This is the end of the file. "
        else:
            chunk_context += "This is a middle section of the file. "
    # Build variable dict for expansion
    variables = metadata.copy() if metadata else {}
    variables.update({
        'chunk': chunk,
        'chunk_context': chunk_context,
        'chunk_context_lower': chunk_context.lower(),
        'is_first_chunk': is_first_chunk,
        'is_final_chunk': is_final_chunk,
        'chunk_num': chunk_num,
        'total_chunks': total_chunks
    })
    try:
        ##formatted_user_prompt = user_prompt.format(**variables)
        formatted_user_prompt = replace_template_variables(chunked_system_prompt, variables)
    except Exception as e:
        logger.error(f"Error formatting user_prompt with metadata: {e}")
        formatted_user_prompt = user_prompt
    try:
        logger.debug(f"*** executing _summarize_chunk - PROMPTS *** \n")        
        logger.debug(f"system_prompt: {system_prompt}\n")
        logger.debug(f"user_prompt: {formatted_user_prompt}\n")
        
        response = openai.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": formatted_user_prompt}
            ],
            max_tokens=1000,
            temperature=0.3
        )
        chunk_summary = response.choices[0].message.content
        logger.debug(f"Generated chunk summary ({chunk_num}/{total_chunks}): {len(chunk_summary)} chars")
        return chunk_summary
    except Exception as e:
        logger.error(f"Error summarizing chunk {chunk_num}: {e}")
        return f"Error processing chunk {chunk_num}: {str(e)}"

def _chunked_summary_with_openai(text, system_prompt, chunked_system_prompt, user_prompt, metadata=None, max_chunk_chars=2000, overlap=500):
    """Orchestrates multi-stage chunked summarization workflow for large documents.
    
    This intermediate-level function manages the complete chunking and summarization
    workflow for large documents. It splits the text into chunks, processes each chunk
    individually with position awareness, and combines the results into a structured
    intermediate summary that preserves the document's logical flow.
    
    Args:
        text (str): The complete text content to summarize
        system_prompt (str): System prompt for OpenAI defining the AI's role
        chunked_system_prompt (str): Template for chunk-specific instructions
        user_prompt (str): User prompt template with variable placeholders
        metadata (dict, optional): Dictionary of metadata values for prompt templates.
            Typically includes document title and content info. Defaults to None.
        max_chunk_chars (int, optional): Maximum characters per chunk for API processing.
            Defaults to 2000.
        overlap (int, optional): Character overlap between chunks for context continuity.
            Defaults to 500.
    
    Returns:
        str: Combined intermediate summary with clearly marked chunk divisions,
            ready for final consolidation processing
            
    Example:
        >>> metadata = {'title': 'Financial Analysis', 'author': 'John Doe'}
        >>> combined = _chunked_summary_with_openai(
        ...     text=long_document_text,
        ...     system_prompt="You are a content summarizer.",
        ...     chunked_system_prompt="Summarize this part: {chunk}",
        ...     user_prompt="Summarize the document titled {title}",
        ...     metadata=metadata
        ... )
        
    Notes:
        Process Flow:
        1. Splits input text into overlapping chunks using _chunk_text()
        2. Processes each chunk with contextual awareness (first/last chunk handling)
        3. Combines individual summaries with clear chunk demarcation
        4. Returns the combined result for final consolidation
    """

    chunks = _chunk_text(text, max_chunk_chars, overlap)
    total_chunks = len(chunks)
    chunk_summaries = []
    for i, chunk in enumerate(chunks):
        is_first = (i == 0)
        is_last = (i == total_chunks - 1)
        chunk_summary = _summarize_chunk(
            chunk=chunk,
            system_prompt=system_prompt,
            chunked_system_prompt=chunked_system_prompt,
            user_prompt=user_prompt,
            metadata=metadata,
            is_first_chunk=is_first,
            is_final_chunk=is_last,
            chunk_num=i+1,
            total_chunks=total_chunks
        )
        chunk_summaries.append(chunk_summary)
    combined_summary = "\n\n".join([
        f"--- CHUNK {i+1}/{total_chunks} SUMMARY ---\n{summary}"
        for i, summary in enumerate(chunk_summaries)
    ])
    return combined_summary

def generate_summary_with_openai(text_to_summarize, system_prompt, chunked_system_prompt, user_prompt, metadata=None, max_chunk_chars=2000, overlap=500, dry_run=False):
    """Main entry point for intelligent content summarization with OpenAI API.
    
    This top-level function provides a unified interface for content summarization,
    automatically selecting the appropriate processing strategy based on content length
    and complexity. It handles both direct summarization for shorter content and
    multi-stage chunked processing for longer documents.
    
    Args:
        text_to_summarize (str): The complete text content to summarize
        system_prompt (str): System prompt for OpenAI defining the AI's role
        chunked_system_prompt (str): Template for chunk-specific instructions
        user_prompt (str): User prompt template with variable placeholders
        metadata (dict, optional): Dictionary of metadata values for templates.
            May include document title, course info, etc. Defaults to None.
        max_chunk_chars (int, optional): Maximum characters per chunk for API processing.
            Affects chunking strategy. Defaults to 2000.
        overlap (int, optional): Character overlap between chunks for context continuity.
            Affects chunking strategy. Defaults to 500.
        dry_run (bool, optional): If True, simulates processing without making API calls.
            Useful for testing and debugging. Defaults to False.
    
    Returns:
        str: AI-generated comprehensive summary with proper markdown structure,
            or None if processing cannot be completed due to validation failures,
            or a placeholder message if in dry_run mode
            
    Raises:
        None: All exceptions are caught and logged internally
    
    Example:
        >>> metadata = {
        ...     'file_name': 'Lecture 5 - Financial Analysis',
        ...     'course_name': 'Corporate Finance 101'
        ... }
        >>> system_prompt = "You are an educational content summarizer."
        >>> chunked_prompt = "Analyze this section: {chunk}"
        >>> user_prompt = "Create a summary for {file_name} from {course_name}"
        >>> summary = generate_summary_with_openai(
        ...     lecture_text,
        ...     system_prompt,
        ...     chunked_prompt,
        ...     user_prompt,
        ...     metadata=metadata
        ... )
            
    Notes:
        Processing Workflow:
        1. Initial validation (API key, content length)
        2. Processing strategy selection (direct vs. chunked)
        3. For long content: chunk processing followed by final consolidation
        4. For shorter content: direct single-pass processing
        5. Return formatted summary with proper markdown structure
        
        Requires properly configured OpenAI API key in environment variables
    """
    if not OPENAI_API_KEY:
        logger.warning("OpenAI API key not set. Cannot generate summary.")
        return None
    if not text_to_summarize or len(text_to_summarize) < 100:
        logger.warning("file text too short or empty. Cannot generate summary.")
        return None
    if dry_run:
        logger.info(f"[DRY RUN] Would call OpenAI with system_prompt: {system_prompt[:100]}... user_prompt: {user_prompt[:100]}... metadata: {metadata}")
        return "[DRY RUN] OpenAI summary would be generated here."
    if len(text_to_summarize) > max_chunk_chars * 1.5:
        
        # Use chunked summarization
        combined_summary = _chunked_summary_with_openai(
            text=text_to_summarize,
            system_prompt=system_prompt,
            chunked_system_prompt=chunked_system_prompt,
            user_prompt=user_prompt,
            metadata=metadata,
            max_chunk_chars=max_chunk_chars,
            overlap=overlap
        )
        
        logger.debug(f"generate_summary_with_openai metadata is \n\n:{metadata}")
                 
        # Optionally, you can pass a different prompt for the final summary
        if metadata is None:
            metadata = {}
        metadata = metadata.copy()
        metadata['combined_summary'] = combined_summary
          
        final_user_prompt = ""
              
        try:
            final_user_prompt = replace_template_variables(user_prompt, metadata)
            
        except Exception as e:
            logger.error(f"Error formatting final user_prompt with metadata: {e}")
            final_user_prompt = user_prompt
        
        logger.debug(f"*** executing generate_summary_with_openai - FINAL PROMPTS *** \n")        
        logger.debug(f"system_prompt: {system_prompt}\n")
        logger.debug(f"user_prompt: {final_user_prompt}\n")
        
        response = openai.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": final_user_prompt}
            ],
            max_tokens=2000,
            temperature=0.5
        )
        summary = response.choices[0].message.content
        logger.debug(f"Successfully generated consolidated summary from all chunks with OpenAI:\n {summary}")
        return summary
    else:
        # For shorter files, process directly        
        try:
            variables = metadata.copy() if metadata else {}
            
            variables['chunk'] = text_to_summarize
            user_prompt_filled = replace_template_variables(user_prompt, variables)
        except Exception as e:
            logger.error(f"Error formatting user_prompt with metadata: {e}")
            user_prompt_filled = user_prompt
            
        # Use format_final_user_prompt_for_pdf with only the metadata/variables
        final_system_prompt = format_final_user_prompt_for_pdf(variables)
            
        logger.debug(f"*** executing generate_summary_with_openai (shorter files) - FINAL PROMPTS *** \n")        
        logger.debug(f"system_prompt: {final_system_prompt}\n")
        logger.debug(f"user_prompt: {user_prompt_filled}\n")
    
        response = openai.chat.completions.create(
            model="gpt-4.1",
            messages=[
                {"role": "system", "content": final_system_prompt},
                {"role": "user", "content": user_prompt_filled}
            ],
            max_tokens=2000,
            temperature=0.5
        )
        summary = response.choices[0].message.content
        logger.debug(f"Successfully generated comprehensive summary with OpenAI:\n {summary}")
        return summary