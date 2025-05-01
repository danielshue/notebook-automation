"""
YAML Metadata Helper Module for Structured Metadata Management

This module provides comprehensive utilities for handling YAML metadata throughout 
the Notebook Generator system. It ensures consistent formatting, template handling,
and variable substitution for all metadata operations in the system.

Key Features:
------------
1. Template Variable Substitution
   - Dynamic placeholder replacement in template strings
   - Support for nested metadata structures
   - Special handling for YAML frontmatter

2. Specialized YAML Formatting
   - Consistent formatting rules for various data types
   - Intelligent quoting based on content type (dates, numbers, strings)
   - Proper list handling for tags and collections
   - Handling of special characters and multi-line values

3. Metadata Transformation
   - Converting between YAML dictionaries and properly formatted strings
   - Maintaining readability in generated YAML
   - Ensuring compatibility with Obsidian frontmatter requirements

Integration Points:
-----------------
- Used by the PDF and notes modules for frontmatter generation
- Supports the AI summarization pipeline with template processing
- Ensures consistent metadata formatting across the entire system
- Provides utilities for both simple variable substitution and complex template processing
"""

import os
import re
import yaml
from pathlib import Path
from datetime import datetime

from ..utils.config import VAULT_LOCAL_ROOT
from ..utils.config import logger

def replace_template_variables(template, meta_info):
    """
    Replace template variables in a string with values from a metadata dictionary.
    
    This function performs simple placeholder substitution for template strings
    using a consistent double-curly brace format ({{variable}}). It handles 
    various data types by converting them to strings before substitution.
    
    The function is used throughout the Notebook Generator system to prepare
    template-based content such as note content, AI prompts, and documentation.
    
    Args:
        template (str): The template string containing placeholders in the format {{key}}
        meta_info (dict): Dictionary of variable names and values to insert into the template
                         Keys in this dictionary correspond to placeholder names
    
    Returns:
        str: The template string with all placeholders replaced by their corresponding values
    
    Example:
        >>> template = "Title: {{title}}, Date: {{date}}"
        >>> meta_info = {"title": "My Document", "date": "2025-04-28"}
        >>> replace_template_variables(template, meta_info)
        "Title: My Document, Date: 2025-04-28"
    
    Note:
        Placeholders without corresponding keys in meta_info will remain unchanged.
        All values are converted to strings regardless of their original type.
    """
    result = template
    for key, value in meta_info.items():
        placeholder = "{{" + key + "}}"
        result = result.replace(placeholder, str(value))
    return result

def replace_template_with_yaml_frontmatter(template, metadata):
    """
    Replace YAML frontmatter placeholder in a template with formatted YAML content.
    
    This specialized function handles the specific case of replacing a YAML frontmatter
    placeholder ({{yaml-frontmatter}}) with properly formatted YAML derived from
    a metadata dictionary. It's particularly useful for generating Markdown notes
    with YAML frontmatter sections that adhere to Obsidian's formatting expectations.
    
    The function follows a two-step approach:
    1. First attempts to replace a dedicated yaml-frontmatter placeholder
    2. Falls back to standard variable replacement if the dedicated placeholder is not found
    
    This function is used primarily in note generation workflows where metadata needs
    to be inserted as formatted YAML frontmatter.
    
    Args:
        template (str): The template string containing either the special {{yaml-frontmatter}}
                       placeholder or standard variable placeholders in the format {{key}}
        metadata (dict): Dictionary containing the metadata to be formatted as YAML and
                        inserted into the template
        
    Returns:
        str: The template string with either the yaml-frontmatter placeholder replaced by
             properly formatted YAML, or with standard variable placeholders replaced
    
    Example:
        >>> template = "---\n{{yaml-frontmatter}}\n---\n\n# {{title}}"
        >>> metadata = {"title": "My Document", "date": "2025-04-28", "tags": ["note", "example"]}
        >>> result = replace_template_with_yaml_frontmatter(template, metadata)
        >>> print(result)
        ---
        title: "My Document"
        date: 2025-04-28
        tags:
          - "note"
          - "example"
        ---
        
        # My Document
    """

    # Convert the metadata dictionary to a properly formatted YAML string
    # using the specialized yaml_to_string function to ensure consistent formatting
    yaml_as_string = yaml_to_string(metadata)

    # Validate the generated YAML by parsing it back to ensure it's well-formed
    # This validation step helps catch any formatting issues early
    try:
        yaml_dict = yaml.safe_load(yaml_as_string)
    except yaml.YAMLError as e:
        # If validation fails, we should log this but continue with the original string
        # This ensures the process doesn't fail completely due to YAML formatting issues
        import logging
        logging.getLogger(__name__).warning(f"YAML validation failed: {e}")

    # Define the special placeholder for YAML frontmatter
    frontMatterText = "{{yaml-frontmatter}}"

    # Two-path strategy:
    # 1. If the special frontmatter placeholder exists, replace it with the formatted YAML
    if frontMatterText in template:
        # Direct replacement of the frontmatter placeholder
        result = template.replace(frontMatterText, yaml_as_string)
    else:
        # 2. If no special placeholder exists, fall back to standard variable replacement
        # This provides flexibility in how templates are structured
        result = replace_template_variables(template, metadata)
        
    return result

def yaml_to_string(yaml_dict):
    """
    Convert YAML dictionary to a properly formatted YAML string with consistent formatting rules.
    
    This specialized function serves as the cornerstone of YAML formatting throughout the
    Notebook Generator system. Rather than using standard YAML dumping functions, it
    implements custom formatting logic specifically designed for educational content
    metadata with particular attention to Obsidian frontmatter compatibility.
    
    Formatting Rules:
    ----------------
    1. String values with spaces are properly quoted (especially file paths)
       - This ensures paths with spaces are correctly interpreted by markdown parsers
    
    2. Dates in YYYY-MM-DD format are NOT quoted
       - Preserves date semantics for sorting and filtering in Obsidian
       - Enables proper date handling in DataView and other plugins
    
    3. Pure numbers (with or without decimals) are NOT quoted
       - Maintains numeric semantics for calculations and filtering
    
    4. File sizes with units (e.g., "0.39 MB") ARE quoted
       - Prevents misinterpretation as calculated values
       - Preserves the exact formatting of size information
    
    5. Tags are formatted as a proper YAML list with each value quoted
       - Ensures compatibility with Obsidian's tag system
       - Maintains proper hierarchical tag structures
    
    6. None values are converted to empty strings
       - Prevents "null" literals in the output
       - Provides cleaner frontmatter display in Obsidian
    
    7. No values are split across multiple lines
       - Ensures file paths and long strings stay intact
       - Improves readability in Obsidian's metadata display
    
    Args:
        yaml_dict (dict): Dictionary of metadata to convert to formatted YAML string
        
    Returns:
        str: Properly formatted YAML string with consistent quoting and formatting,
             ready to be used as Obsidian frontmatter or in other markdown contexts
             
    Example:
        >>> metadata = {
        ...     "title": "Course Notes",
        ...     "date": "2025-04-28",
        ...     "size": "1.5 MB",
        ...     "rating": 4.5,
        ...     "tags": ["notes", "course", "MBA"]
        ... }
        >>> yaml_str = yaml_to_string(metadata)
        >>> print(yaml_str)
        title: "Course Notes"
        date: 2025-04-28
        size: "1.5 MB"
        rating: 4.5
        tags:
          - "notes"
          - "course"
          - "MBA"
    """
    # Create a new dictionary for the formatted output
    # We process each value individually to apply our custom formatting rules
    formatted_dict = {}
    
    # Define regex patterns for special value types
    date_pattern = r"^\d{4}-\d{2}-\d{2}$"  # ISO date format (YYYY-MM-DD)
    number_pattern = r"^-?\d+(\.\d+)?( MB)?$"  # Numbers with optional MB suffix
    
    # Process each key-value pair with custom formatting logic
    for key, value in yaml_dict.items():
        # RULE 1: Special handling for tags list
        # Tags are preserved as a list to enable proper YAML list formatting later
        if key == "tags" and isinstance(value, list):
            formatted_dict[key] = value
            continue
        
        # RULE 2: Handle None values
        # Convert None to empty string to avoid "null" in YAML output
        if value is None:
            formatted_dict[key] = ""
            continue
            
        # RULE 3: Type conversion
        # Ensure all non-list values are strings for consistent handling
        value_str = str(value) if not isinstance(value, str) else value
        
        # RULE 4: Apply specialized formatting rules based on content patterns
        if re.match(date_pattern, value_str):
            # RULE 4.1: Dates remain unquoted to preserve date semantics
            # This ensures proper handling by Obsidian date-related plugins
            formatted_dict[key] = value_str
        elif re.match(r"^-?\d+(\.\d+)?$", value_str):
            # RULE 4.2: Pure numbers (integers and decimals) remain unquoted
            # This maintains numeric semantics for calculations in Obsidian
            formatted_dict[key] = value_str
        else:
            # RULE 4.3: All other values (strings, paths, etc.)
            # These will be properly quoted in the final output
            formatted_dict[key] = value_str
    
    # PHASE 2: Manual YAML string construction for complete formatting control
    # We bypass standard YAML libraries to get exact control over quotation and formatting
    yaml_lines = []
    
    # Process each formatted key-value pair with custom output formatting
    for key, value in formatted_dict.items():
        # CASE 1: Tag lists require special multi-line formatting
        if key == "tags" and isinstance(value, list):
            if value:
                # Create YAML list with each item on its own line and properly quoted
                yaml_lines.append(f"{key}:")
                for tag in value:
                    # Each tag is quoted to handle special characters and spaces
                    yaml_lines.append(f'  - "{tag}"')
            else:
                # Empty tag list is still output as a key with no values
                yaml_lines.append(f"{key}:")
        
        # CASE 2: String values have specialized quoting rules
        elif isinstance(value, str):
            if re.match(date_pattern, value):
                # CASE 2.1: ISO dates remain unquoted for date semantics
                yaml_lines.append(f"{key}: {value}")
            elif re.match(r"^-?\d+(\.\d+)?$", value):
                # CASE 2.2: Pure numbers remain unquoted for numeric semantics
                yaml_lines.append(f"{key}: {value}")
            else:
                # CASE 2.3: All other strings are double-quoted
                # Double quotes (not single) handle escaping better in Obsidian
                yaml_lines.append(f'{key}: "{value}"')
        
        # CASE 3: Fallback for any other types that weren't converted to strings
        else:
            # This should rarely occur due to the preprocessing above
            yaml_lines.append(f"{key}: {value}")
    
    # Join all lines with newlines for the final YAML string
    # No trailing newline is added to avoid extra blank lines in frontmatter
    return "\n".join(yaml_lines)

def build_yaml_frontmatter(friendly_filename, file_path, sharing_link=None, metadata=None, template=None, template_type=None):
    """
    Build YAML frontmatter for a note using the template-type reference template.
    
    This function constructs a comprehensive YAML frontmatter dictionary for reference notes
    based off the template-type by combining data from multiple sources:
    
    1. Base template (provided or fallback)
    2. File metadata (size, creation date)
    3. Course/program hierarchical context
    4. OneDrive linking information
    
    The resulting frontmatter follows the consistent structure expected by the Obsidian vault
    and maintains compatibility with the broader notebook organization system.
    
    Args:
        friendly_filename (str): Name of the file without extension, used as the note title
        file_path (str or Path): Path to the file in the filesystem
        sharing_link (str, optional): OneDrive sharing link for external access
        metadata (dict, optional): Additional metadata like program/course hierarchical info
                                  Can include: program, course, class, module
        template (dict, optional): Base template dictionary to start with
        
    Returns:
        dict: Complete frontmatter dictionary with all required fields populated
              including template-type, title, file paths, size info, and metadata
    """
    title = Path(friendly_filename)
    
      # Handle the template
    # Start with a copy of the template (or empty dict if none)
    yaml_dict = template.copy() if template else {}
    
    # Set required fields (overwrite if they already exist)
    # If template_type is provided, use it; otherwise, try to infer from template or default to "pdf-reference"
    if template_type:
        yaml_dict["template-type"] = template_type
    elif template and template.get("template-type"):
        yaml_dict["template-type"] = template.get("template-type")
    else:
        # Default as a fallback only if nothing else is provided
        yaml_dict["template-type"] = "pdf-reference"
    
    yaml_dict["auto-generated-state"] = "writable"
    yaml_dict["title"] = title
    yaml_dict["date-created"] = datetime.now().strftime("%Y-%m-%d")
        
    # Set specific fields
    try:
        rel_path = file_path.relative_to(VAULT_LOCAL_ROOT) #RESOURCES_ROOT
        yaml_dict["vault-path"] = str(rel_path).replace("\\", "/")
    except (ValueError, AttributeError):
        yaml_dict["vault-path"] = str(file_path).replace("\\", "/")
    
    
    yaml_dict["onedrive-path"] = file_path
            
    # Set sharing link if provided
    if sharing_link:
        yaml_dict["onedrive-sharing-link"] = sharing_link
        
    # Set PDF specific metadata
    if template_type == "pdf-reference":
        try:
            yaml_dict["pdf-size"] = f"{round(file_path.stat().st_size / (1024 * 1024), 2)} MB"
            yaml_dict["pdf-uploaded"] = datetime.fromtimestamp(file_path.stat().st_ctime).strftime("%Y-%m-%d")
        except:
            yaml_dict["pdf-size"] = "Unknown"
            yaml_dict["pdf-uploaded"] = "Unknown"
    elif template_type == "video-reference":
        try:
            yaml_dict["video-size"] = f"{round(file_path.stat().st_size / (1024 * 1024), 2)} MB"
            yaml_dict["video-uploaded"] = datetime.fromtimestamp(file_path.stat().st_ctime).strftime("%Y-%m-%d")
        except:
            yaml_dict["video-size"] = "Unknown"
            yaml_dict["video-uploaded"] = "Unknown"
            
    
    
    
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
