#!/usr/bin/env python3
"""
Path Metadata Module

This module provides functions for extracting and managing metadata from file paths
in the MBA notebook system.
"""

import os
import re
from pathlib import Path
from typing import Dict, Optional, Tuple, Any

def infer_course_and_program(path: str) -> Dict[str, str]:
    """
    Infer course and program information from a file path.
    
    Args:
        path (str): The file path to analyze
        
    Returns:
        Dict[str, str]: Dictionary containing inferred course and program info
    """
    parts = Path(path).parts
    metadata = {
        'course': '',
        'program': '',
        'term': ''
    }
    
    # Look for program indicators
    program_indicators = {
        'mba': 'MBA',
        'emba': 'Executive MBA',
        'pmba': 'Professional MBA'
    }
    
    for part in parts:
        part_lower = part.lower()
        # Match program
        for indicator, program_name in program_indicators.items():
            if indicator in part_lower:
                metadata['program'] = program_name
                break
                
        # Look for course codes (e.g., MBA-123, EMBA-456)
        if re.match(r'^[A-Za-z]+-\d{3}', part):
            metadata['course'] = part
            
        # Look for term indicators
        term_patterns = [
            (r'(spring|summer|fall|winter)\s*\d{4}', lambda m: m.group(0)),
            (r'(q[1-4])\s*\d{4}', lambda m: m.group(0))
        ]
        
        for pattern, formatter in term_patterns:
            match = re.search(pattern, part_lower)
            if match:
                metadata['term'] = formatter(match)
                break
    
    return metadata

def extract_metadata_from_path(file_path: str) -> Dict[str, Any]:
    """
    Extract metadata information from a file path.
    
    Args:
        file_path (str): Path to the file
        
    Returns:
        Dict[str, Any]: Extracted metadata
    """
    path = Path(file_path)
    metadata = {
        'title': path.stem,
        'type': path.suffix.lstrip('.'),
        **infer_course_and_program(str(path))
    }
    
    # Extract additional metadata from path components
    parts = path.parts
    for i, part in enumerate(parts):
        # Look for lecture numbers
        lecture_match = re.search(r'lecture[_-]?(\d+)', part.lower())
        if lecture_match:
            metadata['lecture'] = int(lecture_match.group(1))
            
        # Look for week numbers
        week_match = re.search(r'week[_-]?(\d+)', part.lower())
        if week_match:
            metadata['week'] = int(week_match.group(1))
            
        # Look for topic indicators
        topic_indicators = ['topic', 'subject', 'theme']
        for indicator in topic_indicators:
            if indicator in part.lower():
                # Use the next path component as the topic if it exists
                if i + 1 < len(parts):
                    metadata['topic'] = parts[i + 1]
                break
    
    return metadata

def validate_metadata(metadata: Dict[str, Any]) -> Tuple[bool, Optional[str]]:
    """
    Validate metadata for completeness and correctness.
    
    Args:
        metadata (Dict[str, Any]): The metadata to validate
        
    Returns:
        Tuple[bool, Optional[str]]: (is_valid, error_message)
    """
    required_fields = ['title']
    
    # Check required fields
    for field in required_fields:
        if field not in metadata or not metadata[field]:
            return False, f"Missing required field: {field}"
            
    # Validate numeric fields
    numeric_fields = ['lecture', 'week']
    for field in numeric_fields:
        if field in metadata:
            try:
                int(metadata[field])
            except (ValueError, TypeError):
                return False, f"Invalid numeric value for {field}: {metadata[field]}"
                
    # Validate course code format if present
    if 'course' in metadata and metadata['course']:
        if not re.match(r'^[A-Za-z]+-\d{3}', metadata['course']):
            return False, f"Invalid course code format: {metadata['course']}"
            
    return True, None
