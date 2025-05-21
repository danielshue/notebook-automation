#!/usr/bin/env python3
"""
Process Transcripts Stage 2

This script performs the second stage of transcript processing:
1. Reads transcript-video mappings from JSON file
2. Generates OneDrive share links for the videos
3. Updates the YAML metadata in the transcript files
4. Creates clean .txt versions of the transcripts without metadata
5. Moves the .txt files to the appropriate OneDrive location

Usage:
    python process_transcript_stage2.py [--verbose] [--dry-run]
"""

import os
import sys
import json
import shutil
import argparse
import logging
import re
import yaml
from pathlib import Path
from datetime import datetime

# Import from tools package
from notebook_automation.tools.utils.config import setup_logging, ONEDRIVE_LOCAL_RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT
from notebook_automation.tools.utils.paths import normalize_path
from notebook_automation.tools.onedrive.sharing import create_sharing_link

# Configure logging
logger, failed_logger = setup_logging()

# Log the import paths
logger.debug(f"NOTEBOOK_VAULT_ROOT: {NOTEBOOK_VAULT_ROOT}")
logger.debug(f"ONEDRIVE_LOCAL_RESOURCES_ROOT: {ONEDRIVE_LOCAL_RESOURCES_ROOT}")

def load_transcript_video_mapping(mapping_file="transcript_video_mapping.json"):
    """
    Load the transcript-to-video mapping from a JSON file.
    
    Args:
        mapping_file (str): Path to the mapping JSON file
        
    Returns:
        dict: Dictionary containing mapping information or None if file not found
    """
    try:
        with open(mapping_file, 'r') as f:
            return json.load(f)
    except FileNotFoundError:
        logger.error(f"Mapping file {mapping_file} not found. Run list_transcripts_with_videos.py first.")
        return None
    except json.JSONDecodeError:
        logger.error(f"Invalid JSON format in {mapping_file}.")
        return None

def parse_yaml_frontmatter(content):
    """
    Extract YAML frontmatter from markdown content.
    
    Args:
        content (str): Markdown content with potential YAML frontmatter
        
    Returns:
        tuple: (dict with YAML data, str with content without frontmatter)
    """
    yaml_pattern = re.compile(r'^---\s*\n(.*?)\n---\s*\n', re.DOTALL)
    match = yaml_pattern.match(content)
    
    if match:
        try:
            yaml_content = match.group(1)
            yaml_data = yaml.safe_load(yaml_content)
            remaining_content = content[match.end():]
            return yaml_data, remaining_content
        except yaml.YAMLError as e:
            logger.error(f"Error parsing YAML frontmatter: {e}")
            return {}, content
    else:
        # No YAML frontmatter found
        return {}, content

def create_yaml_frontmatter(metadata_dict):
    """
    Create YAML frontmatter string from dictionary.
    
    Args:
        metadata_dict (dict): Dictionary with metadata
        
    Returns:
        str: YAML frontmatter as string
    """
    try:
        yaml_content = yaml.dump(metadata_dict, default_flow_style=False, sort_keys=False)
        return f"---\n{yaml_content}---\n"
    except yaml.YAMLError as e:
        logger.error(f"Error creating YAML frontmatter: {e}")
        return ""

def generate_onedrive_link(video_path):
    """
    Generate a OneDrive sharing link for the video file.
    
    Args:
        video_path (str or Path): Path to the video file - can be local path or cloud path
        
    Returns:
        str: OneDrive sharing link or None if generation failed
    """
    try:
        # Convert to string if it's a Path object
        video_path_str = str(video_path)
        
        # Log the path details to help with debugging
        logger.debug(f"Path type: {type(video_path)}")
        logger.debug(f"Original path: {video_path_str}")
        
        # Try to normalize the path if it contains MBA-Resources but has duplicate paths
        if "MBA-Resources/MBA-Resources" in video_path_str:
            parts = video_path_str.split("MBA-Resources/MBA-Resources")
            if len(parts) > 1:
                video_path_str = os.path.join(parts[0], "MBA-Resources", parts[1])
                logger.debug(f"Normalized duplicated MBA-Resources path to: {video_path_str}")
                video_path = video_path_str
        
        # First check if the file exists locally when it's a local path
        # (but don't fail if not - it might be a cloud path)
        if os.path.isabs(video_path_str) and not os.path.isfile(video_path_str):
            logger.warning(f"Video file does not exist locally: {video_path}")
            logger.debug(f"Will attempt to generate sharing link using cloud path conversion")
        
        # Print the type and path to help with debugging
        logger.debug(f"Generating OneDrive sharing link for: {video_path}")
        
        # Use the improved OneDrive sharing module
        # This module will handle the conversion from local to cloud path
        link = create_sharing_link(video_path)
        
        if link:
            logger.info(f"Successfully generated OneDrive sharing link")
            logger.info(f"Path: {video_path}")
            logger.info(f"Link: {link}")
            return link
        else:
            logger.error(f"Failed to generate OneDrive sharing link for {video_path}")
            logger.error("Possible reasons: invalid path format, file not found in OneDrive, or authentication issue")
            return None
    except Exception as e:
        logger.error(f"Failed to generate OneDrive sharing link for {video_path}: {e}")
        logger.error(f"Exception type: {type(e).__name__}")
        return None

def update_transcript_metadata(transcript_path, video_path, txt_path=None, sharing_link=None):
    """
    Update the YAML metadata in a transcript file.
    
    Args:
        transcript_path (Path): Path to the transcript markdown file
        video_path (Path): Path to the corresponding video file
        txt_path (Path, optional): Path to the txt version of the transcript
        sharing_link (str, optional): OneDrive sharing link for the video
        
    Returns:
        bool: True if update was successful, False otherwise
    """
    try:
        # Read the transcript file
        with open(transcript_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Parse YAML frontmatter
        metadata, remaining_content = parse_yaml_frontmatter(content)
        
        # If no YAML frontmatter exists, create an empty one
        if not metadata:
            metadata = {}
            logger.info(f"No existing YAML frontmatter found in {transcript_path}. Creating new metadata.")
        
        # Update metadata with new values
        metadata['vault-path'] = str(transcript_path)
        metadata['onedrive-path'] = str(video_path)
        
        if txt_path:
            metadata['transcript'] = str(txt_path)
        
        if sharing_link:
            metadata['onedrive-sharing-link'] = sharing_link
        
        # Create updated content with new frontmatter
        updated_content = create_yaml_frontmatter(metadata) + remaining_content
        
        # Write back to file
        with open(transcript_path, 'w', encoding='utf-8') as f:
            f.write(updated_content)
        
        logger.info(f"Updated metadata in {transcript_path}")
        return True
    
    except Exception as e:
        logger.error(f"Failed to update metadata in {transcript_path}: {e}")
        return False

def create_txt_transcript(transcript_path, output_dir=None):
    """
    Create a clean .txt version of a transcript file without YAML metadata.
    
    Args:
        transcript_path (Path): Path to the transcript markdown file
        output_dir (Path, optional): Directory to save the .txt file
        
    Returns:
        Path: Path to the created .txt file or None if creation failed
    """
    try:
        # Read the transcript file
        with open(transcript_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Extract content without YAML frontmatter
        _, clean_content = parse_yaml_frontmatter(content)
        
        # Determine output path
        if output_dir:
            txt_path = output_dir / f"{transcript_path.stem}.txt"
        else:
            txt_path = transcript_path.with_suffix('.txt')
        
        # Create parent directory if it doesn't exist
        txt_path.parent.mkdir(parents=True, exist_ok=True)
        
        # Write clean content to .txt file
        with open(txt_path, 'w', encoding='utf-8') as f:
            f.write(clean_content)
        
        logger.info(f"Created clean .txt version at {txt_path}")
        return txt_path
    
    except Exception as e:
        logger.error(f"Failed to create .txt version of {transcript_path}: {e}")
        return None

def move_txt_to_onedrive(txt_path, video_path):
    """
    Move the .txt transcript file to the same location as the video in OneDrive.
    
    Args:
        txt_path (Path): Path to the .txt transcript file
        video_path (Path): Path to the corresponding video file
        
    Returns:
        Path: Path to the moved file or None if move failed
    """
    try:
        # First ensure video_path is a valid local path
        video_path_str = str(video_path)
        logger.debug(f"Moving txt file to video path parent: {video_path.parent}")
        
        # Check if the parent directory exists
        if not os.path.exists(video_path.parent):
            logger.debug(f"Video path parent directory doesn't exist locally: {video_path.parent}")
            
            # Try to get the OneDrive root directory from the config
            try:
                from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT
                onedrive_path = Path(ONEDRIVE_LOCAL_RESOURCES_ROOT)
                
                # Find key path components to reconstruct the path
                path_components = ["MBA-Resources", "Value Chain Management", "Marketing Management"]
                constructed_path = None
                
                for component in path_components:
                    if component in video_path_str:
                        idx = video_path_str.find(component)
                        relative_path = video_path_str[idx:].lstrip('/')
                        
                        # Try different base paths
                        base_paths = [
                            onedrive_path,
                            onedrive_path.parent,
                            onedrive_path.parent.parent / "Education"
                        ]
                        
                        for base_path in base_paths:
                            test_path = base_path / relative_path
                            test_parent = test_path.parent
                            
                            logger.debug(f"Testing potential path: {test_parent}")
                            if os.path.exists(test_parent):
                                constructed_path = test_path
                                logger.debug(f"Found valid parent path: {test_parent}")
                                break
                        
                        if constructed_path:
                            break
                
                if constructed_path:
                    video_path = constructed_path
                    logger.debug(f"Using corrected video path: {video_path}")
            except Exception as e:
                logger.warning(f"Failed to correct video path: {e}")
        
        # Determine destination path (same directory as video)
        dest_path = video_path.parent / f"{video_path.stem}.txt"
        
        logger.debug(f"Destination path for .txt file: {dest_path}")
        
        # Create parent directory tree if it doesn't exist
        try:
            os.makedirs(dest_path.parent, exist_ok=True)
            logger.debug(f"Created directory tree: {dest_path.parent}")
        except Exception as e:
            logger.warning(f"Could not create directory tree {dest_path.parent}: {e}")
            # If we can't create the directory, it might be a permissions issue
            # Try to find an existing directory we can write to
            
            # Try checking if we can at least reach OneDrive
            from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT
            fallback_dir = Path(ONEDRIVE_LOCAL_RESOURCES_ROOT)
            
            if os.path.exists(fallback_dir) and os.access(fallback_dir, os.W_OK):
                # We can write to the OneDrive root, so use that as fallback
                dest_path = fallback_dir / f"{video_path.stem}.txt"
                logger.warning(f"Using fallback path: {dest_path}")
            else:
                logger.error(f"Cannot access OneDrive directory for writing: {fallback_dir}")
                return None
        
        if not os.path.exists(dest_path.parent):
            logger.error(f"Destination directory does not exist and could not be created: {dest_path.parent}")
            return None
        
        # Copy the file
        logger.debug(f"Copying {txt_path} to {dest_path}")
        shutil.copy2(txt_path, dest_path)
        
        # Remove the original after copying
        logger.debug(f"Removing original file: {txt_path}")
        os.remove(txt_path)
        
        logger.info(f"Moved .txt transcript to {dest_path}")
        return dest_path
    
    except Exception as e:
        logger.error(f"Failed to move .txt transcript to OneDrive: {e}")
        return None

def process_transcript(transcript_path, video_path, dry_run=False):
    """
    Process a single transcript file.
    
    Args:
        transcript_path (Path): Path to the transcript markdown file
        video_path (Path): Path to the corresponding video file
        dry_run (bool): If True, don't make any changes, just log what would happen
        
    Returns:
        bool: True if processing was successful, False otherwise
    """
    logger.info(f"Processing transcript: {transcript_path}")
    
    try:
        # Normalize the video path to handle potential path issues
        video_path_str = str(video_path)
        logger.debug(f"Original video path: {video_path_str}")
        
        # Fix common path problems like duplicated MBA-Resources
        if "MBA-Resources/MBA-Resources" in video_path_str:
            parts = video_path_str.split("MBA-Resources/MBA-Resources")
            if len(parts) > 1:
                video_path_str = os.path.join(parts[0], "MBA-Resources", parts[1])
                logger.debug(f"Normalized duplicated MBA-Resources path to: {video_path_str}")
                video_path = Path(video_path_str)
        
        # First, verify transcript exists
        transcript_exists = os.path.isfile(transcript_path)
        if not transcript_exists:
            logger.error(f"Transcript file not found: {transcript_path}")
            return False
        
        # We'll store all potential paths to check for the video
        video_paths_to_check = []
        
        # Add the original path first
        video_paths_to_check.append((str(video_path), "Original path"))
        
        # Add various alternatives based on the path structure
        try:
            from notebook_automation.tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT
            
            # Alternative 1: Try paths with different OneDrive root structures
            if "OneDrive" in video_path_str:
                # Extract path after "OneDrive"
                parts = video_path_str.split("OneDrive")
                if len(parts) > 1:
                    after_onedrive = parts[1].lstrip('/\\')
                    
                    # Try variations of OneDrive local path
                    onedrive_variations = []
                    
                    # Try exact ONEDRIVE_LOCAL_RESOURCES_ROOT path
                    onedrive_variations.append(str(ONEDRIVE_LOCAL_RESOURCES_ROOT.parent))
                    
                    # Try parent directory as a fallback (in case ONEDRIVE_LOCAL_RESOURCES_ROOT has MBA-Resources in it)
                    onedrive_variations.append(str(ONEDRIVE_LOCAL_RESOURCES_ROOT.parent.parent))
                    
                    # Try Windows-style paths on WSL
                    if "/mnt/" in str(ONEDRIVE_LOCAL_RESOURCES_ROOT):
                        win_drive = str(ONEDRIVE_LOCAL_RESOURCES_ROOT).split('/')[2]  # Extract drive letter
                        onedrive_variations.append(f"/mnt/{win_drive}/Users")
                        
                    for onedrive_root in onedrive_variations:
                        alt_path = os.path.join(onedrive_root, after_onedrive)
                        video_paths_to_check.append((alt_path, f"OneDrive root variation: {onedrive_root}"))
            
            # Alternative 2: Try with various MBA-Resources paths
            if "MBA-Resources" in video_path_str:
                # Extract path after MBA-Resources
                parts = video_path_str.split("MBA-Resources")
                if len(parts) > 1:
                    after_mba = parts[1].lstrip('/\\')
                    
                    # Try different MBA-Resources paths
                    alt_path1 = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), after_mba)
                    video_paths_to_check.append((alt_path1, "Using MBA-Resources direct from config"))
                    
                    # Try one level up
                    alt_path2 = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT.parent), "MBA-Resources", after_mba)
                    video_paths_to_check.append((alt_path2, "Using parent/MBA-Resources"))
                    
                    # Try direct path after Value Chain Management if present
                    if "Value Chain Management" in after_mba:
                        vcm_parts = after_mba.split("Value Chain Management")
                        if len(vcm_parts) > 1:
                            after_vcm = vcm_parts[1].lstrip('/\\')
                            alt_path3 = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), "Value Chain Management", after_vcm)
                            video_paths_to_check.append((alt_path3, "Using Value Chain Management path"))
            
            # Alternative 3: Try with just the filename in various locations
            filename = os.path.basename(video_path_str)
            parent_dir = os.path.basename(os.path.dirname(video_path_str))
            
            # Try in MBA-Resources with parent dir
            alt_filename_path1 = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), parent_dir, filename)
            video_paths_to_check.append((alt_filename_path1, "Filename with parent in MBA-Resources"))
            
            # If path has a very specific structure like module names
            path_parts = str(video_path).split('/')
            course_indicators = ["module", "lesson"]
            
            # If we detect a course-like structure, try alternative paths
            if any(indicator in part.lower() for part in path_parts for indicator in course_indicators):
                # Extract key directories - this is specialized for the MBA directory structure
                course_dirs = []
                for i, part in enumerate(path_parts):
                    if any(indicator in part.lower() for indicator in course_indicators):
                        if i >= 2:  # Make sure we have enough previous directories
                            course_name = path_parts[i-2]
                            course_dirs.append(course_name)
                
                if course_dirs:
                    for course in course_dirs:
                        # Try to locate all files with this name in MBA-Resources
                        alt_course_path = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), course, "**", filename)
                        video_paths_to_check.append((alt_course_path, f"Course-based path under {course}"))
                        
                        # Also try with Value Chain Management if that's part of your structure
                        if "Value Chain Management" in video_path_str:
                            alt_vcm_path = os.path.join(str(ONEDRIVE_LOCAL_RESOURCES_ROOT), 
                                                     "Value Chain Management", course, "**", filename)
                            video_paths_to_check.append((alt_vcm_path, f"VCM/{course} path"))
            
        except Exception as e:
            logger.warning(f"Error generating alternative video paths: {e}")
        
        # Now check all potential paths
        video_found = False
        video_found_path = None
        
        for potential_path, description in video_paths_to_check:
            logger.debug(f"Checking {description}: {potential_path}")
            
            # Handle glob patterns
            if "**" in potential_path:
                import glob
                matching_files = glob.glob(potential_path, recursive=True)
                if matching_files:
                    logger.info(f"Found video using glob pattern: {matching_files[0]}")
                    video_path = Path(matching_files[0])
                    video_found = True
                    video_found_path = str(video_path)
                    break
            # Regular path check
            elif os.path.isfile(potential_path):
                logger.info(f"Found video at {description}: {potential_path}")
                video_path = Path(potential_path)
                video_found = True
                video_found_path = potential_path
                break
        
        # If we still didn't find the video
        if not video_found:
            logger.warning(f"Video file not found locally: {video_path}")
            # Log all paths we checked
            for i, (path, desc) in enumerate(video_paths_to_check):
                logger.debug(f"  Path {i+1} ({desc}): {path}")
            # Continue anyway as it might exist in OneDrive
        else:
            logger.info(f"Found video at: {video_found_path}")
            # Update video_path to the found path
            video_path = Path(video_found_path)
        
        if dry_run:
            logger.info(f"DRY RUN: Would generate OneDrive link for {video_path}")
            logger.info(f"DRY RUN: Would update metadata in {transcript_path}")
            logger.info(f"DRY RUN: Would create .txt version of {transcript_path}")
            logger.info(f"DRY RUN: Would move .txt to {video_path.parent}")
            return True
        
        # Step 1: Generate OneDrive sharing link
        # The sharing link must be created using the cloud path format, not the local path
        # Even if the file exists locally, we need to use a path that OneDrive API understands
        
        cloud_path = None
        from notebook_automation.tools.onedrive.sharing import convert_local_path_to_cloud_path
        
        try:
            # First, try to get the cloud path using our conversion function
            logger.debug(f"Converting local path to cloud path: {video_path}")
            cloud_path = convert_local_path_to_cloud_path(str(video_path))
            
            if cloud_path:
                logger.debug(f"Successfully converted to cloud path: {video_path} -> {cloud_path}")
            else:
                # If the conversion function fails, try some manual extraction
                raw_path_str = str(video_path)
                logger.debug(f"Conversion failed, trying manual extraction from: {raw_path_str}")
                
                # First, fix any duplicate MBA-Resources paths
                if "MBA-Resources/MBA-Resources" in raw_path_str:
                    parts = raw_path_str.split("MBA-Resources/MBA-Resources")
                    if len(parts) > 1:
                        raw_path_str = os.path.join(parts[0], "MBA-Resources", parts[1])
                        logger.debug(f"Normalized duplicated MBA-Resources path to: {raw_path_str}")
                
                # If this path contains "OneDrive", extract everything after it
                if "OneDrive" in raw_path_str:
                    onedrive_index = raw_path_str.find("OneDrive")
                    if onedrive_index >= 0:
                        # Extract the part after "OneDrive/"
                        cloud_path = raw_path_str[onedrive_index + len("OneDrive") + 1:]
                        logger.debug(f"Manually extracted OneDrive cloud path: {cloud_path}")
                # If it contains "MBA-Resources" but not "OneDrive", it might be a relative path
                elif "MBA-Resources" in raw_path_str:
                    mba_resources_index = raw_path_str.find("MBA-Resources")
                    if mba_resources_index >= 0:
                        cloud_path = raw_path_str[mba_resources_index:]
                        logger.debug(f"Using MBA-Resources relative path: {cloud_path}")
                else:
                    # Just use the raw path as a last resort
                    cloud_path = raw_path_str
                    logger.debug(f"Using original path for sharing: {cloud_path}")
                    
        except Exception as e:
            logger.error(f"Error converting path for sharing: {e}")
            cloud_path = str(video_path)  # Fallback to original path
            
        # Generate the sharing link using the cloud path
        logger.debug(f"Generating sharing link using path: {cloud_path}")
        sharing_link = generate_onedrive_link(cloud_path)
            
        if not sharing_link:
            logger.warning(f"Could not generate OneDrive sharing link for {video_path}")
            # Continue anyway as we can still process the transcript
        
        # Step 2: Create .txt version of transcript
        txt_path = create_txt_transcript(transcript_path)
        if not txt_path:
            raise Exception(f"Failed to create .txt version of {transcript_path}")
        
        # Step 3: Move .txt file to OneDrive
        onedrive_txt_path = move_txt_to_onedrive(txt_path, video_path)
        if not onedrive_txt_path:
            raise Exception(f"Failed to move .txt file to OneDrive")
        
        # Step 4: Update metadata in the original transcript file
        success = update_transcript_metadata(
            transcript_path, 
            video_path, 
            onedrive_txt_path,
            sharing_link
        )
        if not success:
            raise Exception(f"Failed to update metadata in {transcript_path}")
        
        logger.info(f"Successfully processed {transcript_path}")
        return True
    
    except Exception as e:
        logger.error(f"Failed to process {transcript_path}: {e}")
        return False

def main():
    """Main function to run the script."""
    parser = argparse.ArgumentParser(description="Process transcripts (Stage 2).")
    parser.add_argument("--verbose", "-v", action="store_true", help="Enable verbose logging")
    parser.add_argument("--dry-run", "-d", action="store_true", 
                      help="Don't make any changes, just log what would happen")
    parser.add_argument("--mapping-file", "-m", default="transcript_video_mapping.json",
                      help="Path to the transcript-video mapping JSON file")
    args = parser.parse_args()
    
    # Configure logging level based on args
    if args.verbose:
        logger.setLevel(logging.DEBUG)
        logger.debug("Verbose logging enabled")
    
    logger.info("Starting Stage 2 transcript processing")
    
    if args.dry_run:
        logger.info("DRY RUN mode enabled - no files will be modified")
    
    # Step 1: Load transcript-video mapping
    mapping_data = load_transcript_video_mapping(args.mapping_file)
    if not mapping_data:
        logger.error("No mapping data found. Exiting.")
        return
    
    # Get the vault and OneDrive paths from config
    try:
        vault_path = Path(NOTEBOOK_VAULT_ROOT)
        onedrive_path = Path(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        
        logger.info(f"Using vault path from config: {vault_path}")
        logger.info(f"Using OneDrive path from config: {onedrive_path}")
        
        # Validate paths
        if not vault_path.exists():
            logger.warning(f"Vault path doesn't exist: {vault_path}")
            # Try to find an alternative path
            alt_path = Path('/mnt/d/Vault')
            if alt_path.exists():
                logger.info(f"Using alternative vault path: {alt_path}")
                vault_path = alt_path
            else:
                logger.error("Could not find a valid vault path")
                print(f"Error: Vault path not found - {vault_path}")
                return
                
        if not onedrive_path.exists():
            logger.warning(f"OneDrive path doesn't exist: {onedrive_path}")
            # Try common OneDrive paths on WSL
            alt_paths = [
                Path('/mnt/c/Users/danielshue/OneDrive'),
                Path('/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources'),
                Path('/mnt/d/OneDrive'),
                Path('/mnt/d/OneDrive/Education/MBA-Resources')
            ]
            
            for alt_path in alt_paths:
                if alt_path.exists():
                    logger.info(f"Using alternative OneDrive path: {alt_path}")
                    onedrive_path = alt_path
                    break
            else:
                logger.error("Could not find a valid OneDrive path")
                print(f"Error: OneDrive path not found - {onedrive_path}")
                return
    except Exception as e:
        logger.error(f"Error setting up paths: {e}")
        print(f"Error setting up paths: {e}")
        return
    
    # Process each transcript-video pair
    success_count = 0
    failure_count = 0
    
    # The mapping format depends on your transcript_video_mapping.json structure
    # Check what fields we have in the mapping data
    if len(mapping_data) > 0:
        sample_item = mapping_data[0]
        logger.debug(f"Sample mapping item structure: {list(sample_item.keys())}")
        
    # Process each transcript-video pair in the mapping
    for item in mapping_data:
        # Extract paths based on available fields
        transcript_path = Path(item.get("transcript", item.get("transcript_path", "")))
        raw_video_path = item.get("video_path", "")
        
        if not transcript_path or str(transcript_path).strip() == "":
            logger.warning("Missing transcript path in mapping data")
            failure_count += 1
            continue
        
        if not raw_video_path or str(raw_video_path).strip() == "":
            logger.warning(f"Missing video path for transcript: {transcript_path}")
            failure_count += 1
            continue
            
        # Normalize path to handle potential issues with duplicate MBA-Resources paths
        if "MBA-Resources/MBA-Resources" in raw_video_path:
            logger.debug(f"Found duplicate 'MBA-Resources' in path: {raw_video_path}")
            parts = raw_video_path.split("MBA-Resources/MBA-Resources")
            if len(parts) > 1:
                raw_video_path = os.path.join(parts[0], "MBA-Resources", parts[1])
                logger.debug(f"Normalized path: {raw_video_path}")
        
        # Fix video path if it's relative or missing the OneDrive root
        video_path_str = str(raw_video_path)
        
        # Generate multiple potential paths to try
        candidate_paths = []
        
        # First add the original path
        candidate_paths.append((Path(video_path_str), "Original path"))
        
        # Add paths derived from different path structures
        
        # Case 1: Path is already a full local path
        if os.path.isabs(video_path_str) and '/OneDrive/' in video_path_str:
            candidate_paths.append((Path(video_path_str), "Absolute local OneDrive path"))
            
            # Try alternate drive letters on WSL
            if video_path_str.startswith('/mnt/'):
                for drive in 'cde':
                    if not video_path_str.startswith(f'/mnt/{drive}'):
                        alt_path = video_path_str.replace('/mnt/', f'/mnt/{drive}/')
                        candidate_paths.append((Path(alt_path), f"Drive letter variation: {drive}"))
        
        # Case 2: Path starts with MBA-Resources
        if 'MBA-Resources' in video_path_str:
            idx = video_path_str.find('MBA-Resources')
            after_mbares = video_path_str[idx + len('MBA-Resources'):].lstrip('/')
            
            # Try different base paths for MBA-Resources
            base_paths = [
                onedrive_path,
                onedrive_path / 'MBA-Resources',
                onedrive_path.parent / 'MBA-Resources',
                Path('/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources'),
                Path('/mnt/d/OneDrive/Education/MBA-Resources')
            ]
            
            for i, base_path in enumerate(base_paths):
                candidate_paths.append((base_path / after_mbares, f"MBA-Resources base {i}: {base_path}"))
        
        # Case 3: Path contains "Value Chain Management" or other specific directories
        for special_dir in ['Value Chain Management', 'Marketing Management']:
            if special_dir in video_path_str:
                idx = video_path_str.find(special_dir)
                after_special = video_path_str[idx:].lstrip('/')
                
                # Try with different base paths
                base_paths = [
                    onedrive_path,
                    onedrive_path.parent,
                    Path('/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources'),
                    Path('/mnt/d/OneDrive/Education/MBA-Resources')
                ]
                
                for i, base_path in enumerate(base_paths):
                    candidate_paths.append((base_path / after_special, f"{special_dir} base {i}: {base_path}"))
        
        # Case 4: Just try filename with various paths
        filename = os.path.basename(video_path_str)
        course_parts = video_path_str.split('/')
        
        # Try to identify important directory components from the path
        for i, part in enumerate(course_parts):
            if part in ['marketing-management', 'marketing-management-two', 'Value Chain Management']:
                if i + 1 < len(course_parts) and i + 2 < len(course_parts):
                    # Construct path with important components
                    important_path = '/'.join(course_parts[i:i+3]) + '/' + filename
                    
                    base_paths = [
                        onedrive_path,
                        onedrive_path.parent,
                        Path('/mnt/c/Users/danielshue/OneDrive/Education/MBA-Resources')
                    ]
                    
                    for j, base_path in enumerate(base_paths):
                        candidate_paths.append((base_path / important_path, f"Important path {j}: {base_path}/{important_path}"))
        
        # Try all candidate paths and use the first one that exists
        video_path = None
        for candidate_path, description in candidate_paths:
            if os.path.isfile(candidate_path):
                video_path = candidate_path
                logger.info(f"Using video path: {description} - {video_path}")
                break
        
        # If no candidate path exists, use the raw path
        if video_path is None:
            video_path = Path(raw_video_path)
            logger.info(f"No valid local video path found, using raw path: {video_path}")
        
        # Log the paths we're using
        logger.info(f"Processing transcript: {transcript_path}")
        logger.info(f"Video path: {video_path}")
        
        # Verify transcript exists
        transcript_exists = os.path.isfile(transcript_path)
        if not transcript_exists:
            logger.warning(f"Transcript file not found: {transcript_path}")
            failure_count += 1
            continue
        
        # Process the transcript
        if process_transcript(transcript_path, video_path, args.dry_run):
            success_count += 1
        else:
            failure_count += 1
    
    # Print summary
    logger.info("Stage 2 processing complete")
    logger.info(f"Successfully processed: {success_count}")
    logger.info(f"Failed to process: {failure_count}")
    
    # Print a nice message to the console
    print("\n==== Stage 2 Transcript Processing Complete ====")
    print(f"Successfully processed: {success_count}")
    print(f"Failed to process: {failure_count}")
    print("================================================\n")

if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        logger.error(f"Uncaught exception: {e}", exc_info=True)
        print(f"Error: {e}")
        sys.exit(1)