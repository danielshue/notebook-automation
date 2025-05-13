#!/usr/bin/env python3
"""
List Transcripts with Verified Video Files

This script scans the vault for Markdown files ending with 'Transcript.md' 
and verifies if the corresponding MP4 video files exist in the OneDrive folder.
It can also find MP4 video files that don't have corresponding transcripts.

Additionally, it looks for text transcripts (.txt files) that share the same 
base name as video files (.mp4) but with a different extension.

Usage:
    python list_transcripts_with_videos.py [--no-auth] [--find-missing-transcripts]
"""

import os
import sys
import json
import argparse
import logging
from pathlib import Path
from datetime import datetime

# Import from tools package
from notebook_automation.tools.utils.config import setup_logging, ONEDRIVE_LOCAL_RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT
from notebook_automation.tools.utils.paths import normalize_path
from notebook_automation.tools.transcript.processor import find_transcript_file
from notebook_automation.tools.utils.file_operations import find_files_by_extension

# Configure logging with the system's standardized logger
logger, failed_logger = setup_logging()

# We can remove the load_config function since we're importing
# the configuration from tools.utils.config (RESOURCES_ROOT, NOTEBOOK_VAULT_ROOT)

def find_transcript_files(vault_path, onedrive_path=None):
    """Find all transcript files
    
    This includes:
    - Markdown files ending with 'Transcript.md' in the vault
    - Text files (.txt) in OneDrive that have the same base name as a video file
    
    Args:
        vault_path (Path): Path to the vault
        onedrive_path (Path, optional): Path to the OneDrive folder for .txt matching
        
    Returns:
        list: List of transcript file paths
    """
    logger.info(f"Searching for transcript files in vault: {vault_path}")
    transcript_files = []
    md_count = 0
    
    # Find all Markdown files ending with 'Transcript.md' in the vault
    for root, _, files in os.walk(vault_path):
        for file in files:
            if file.endswith("Transcript.md"):
                transcript_files.append(Path(os.path.join(root, file)))
                md_count += 1
                logger.debug(f"Found transcript MD: {os.path.join(root, file)}")
    
    logger.info(f"Found {md_count} Markdown transcript files in vault")
    
    # If onedrive_path is provided, look for .txt files that match video file names in OneDrive
    txt_count = 0
    if onedrive_path:
        logger.info(f"Searching for .txt transcript files in OneDrive: {onedrive_path}")
        
        # Find all video files first
        video_files = find_video_files(onedrive_path)
        logger.info(f"Checking {len(video_files)} videos for matching .txt transcripts")
        
        # For each video file, try to find a matching .txt transcript with the same name
        for video_path in video_files:
            txt_path = video_path.with_suffix('.txt')
            if txt_path.exists():
                transcript_files.append(txt_path)
                txt_count += 1
                logger.debug(f"Found transcript TXT: {txt_path}")
        
        logger.info(f"Found {txt_count} text transcript files in OneDrive")
    
    logger.info(f"Total transcript files found: {len(transcript_files)}")
    return transcript_files

def find_video_files(onedrive_path):
    """Find all MP4 video files in the OneDrive folder"""
    logger.info(f"Searching for video files in OneDrive: {onedrive_path}")
    # Use the generic find_files_by_extension function from tools.utils.file_operations
    video_files = find_files_by_extension(onedrive_path, extension=".mp4")
    return video_files

def get_transcript_base_name(transcript_path):
    """Extract the base name from a transcript file path (without -Transcript or _Transcript suffix)"""
    if isinstance(transcript_path, str):
        transcript_path = Path(transcript_path)
    filename = transcript_path.stem
    return filename.removesuffix("-Transcript").removesuffix("_Transcript")

def get_video_base_name(video_path):
    """Extract the base name from a video file path (without .mp4 extension)"""
    if isinstance(video_path, str):
        video_path = Path(video_path)
    return video_path.stem

def get_video_path(transcript_path, vault_path, onedrive_path):
    """Determine the expected video file path based on the transcript filename
    
    This function is the inverse of find_transcript_file from the transcript.processor module.
    We can implement a simplified version based on our needs.
    
    Args:
        transcript_path (Path): Path to the transcript file
        vault_path (Path): Path to the vault
        onedrive_path (Path): Path to the OneDrive folder
        
    Returns:
        Path or None: Path to the matching video file, or None if not found
    """
    logger.debug(f"Looking for video match for transcript: {transcript_path}")
    transcript_ext = transcript_path.suffix.lower()
    
    # For .txt files in OneDrive, check for matching .mp4 with same name
    if transcript_ext == ".txt" and str(transcript_path).startswith(str(onedrive_path)):
        video_path = transcript_path.with_suffix('.mp4')
        if video_path.exists():
            logger.debug(f"Found direct match: {video_path}")
            return video_path
    
    # For transcript files in the vault, use the filename pattern to find matching videos
    filename = transcript_path.stem
    
    # For Transcript.md files, remove 'Transcript' suffix
    video_name = filename
    if "transcript" in filename.lower():
        video_name = filename.removesuffix("-Transcript").removesuffix("_Transcript") \
                            .removesuffix("-transcript").removesuffix("_transcript")
        logger.debug(f"Transcript is .md, extracted video name: {video_name}")
    
    # Check if the expected video exists directly in OneDrive folder
    direct_match = onedrive_path / f"{video_name}.mp4"
    if direct_match.exists():
        logger.debug(f"Found direct match: {direct_match}")
        return direct_match
    
    # Recursively search for the video file
    logger.debug(f"Direct match not found, searching recursively for: {video_name}.mp4")
    for root, _, files in os.walk(onedrive_path):
        for file in files:
            if file.lower() == f"{video_name.lower()}.mp4":
                match_path = Path(os.path.join(root, file))
                logger.debug(f"Found match by case-insensitive name: {match_path}")
                return match_path
    
    # If not found, try with underscores instead of hyphens or vice versa
    if "-" in video_name:
        video_name_alt = video_name.replace("-", "_")
        logger.debug(f"Trying alternative name with underscores: {video_name_alt}")
    else:
        video_name_alt = video_name.replace("_", "-")
        logger.debug(f"Trying alternative name with hyphens: {video_name_alt}")
        
    for root, _, files in os.walk(onedrive_path):
        for file in files:
            if file.lower() == f"{video_name_alt.lower()}.mp4":
                match_path = Path(os.path.join(root, file))
                logger.debug(f"Found match by alternative name: {match_path}")
                return match_path
                
    # Return None if no matching video file is found
    logger.debug(f"No matching video found for transcript: {transcript_path}")
    return None

def get_transcript_path(video_path, vault_path, onedrive_path):
    """Determine the expected transcript file path based on the video filename
    
    Looks for:
    - *Transcript.md files in the vault
    - .txt files in OneDrive with the same basename as the video
    
    Args:
        video_path (Path): Path to the video file
        vault_path (Path): Path to the vault
        onedrive_path (Path): Path to the OneDrive folder
        
    Returns:
        Path or None: Path to the matching transcript file, or None if not found
    """
    logger.debug(f"Looking for transcript match for video: {video_path}")
    
    # Use the existing transcript finder from the tools package
    try:
        transcript_path = find_transcript_file(video_path, vault_path)
        
        # The find_transcript_file function already handles all the complex matching logic
        # and searches both in the vault and OneDrive
        if transcript_path:
            logger.debug(f"Found transcript using transcript processor: {transcript_path}")
            return transcript_path
    except Exception as e:
        logger.warning(f"Error using find_transcript_file: {e}")
        # Continue with our own implementation as fallback
        
    # Additionally check for a simple .txt file with the same name in OneDrive
    # as this specific case might not be covered by find_transcript_file
    txt_path = video_path.with_suffix('.txt')
    if txt_path.exists():
        logger.debug(f"Found TXT transcript with exact match: {txt_path}")
        return txt_path
        
    # Return None if no matching transcript file is found
    logger.debug(f"No matching transcript found for video: {video_path}")
    return None

def main():
    parser = argparse.ArgumentParser(description="List transcripts with verified video files.")
    parser.add_argument("--no-auth", action="store_true", help="Skip authentication checks")
    parser.add_argument("--find-missing-transcripts", action="store_true", 
                      help="Find video files without corresponding transcripts")
    parser.add_argument("--verbose", "-v", action="store_true", help="Enable verbose logging")
    parser.add_argument("--quiet", "-q", action="store_true", help="Suppress all but error messages")
    parser.add_argument("--run-stage2", action="store_true", 
                      help="Run Stage 2 processing after finding transcripts with videos")
    parser.add_argument("--dry-run", "-d", action="store_true", 
                      help="With --run-stage2, don't make changes, just log what would happen")
    args = parser.parse_args()
    
    # Configure logging level based on args
    if args.verbose:
        logger.setLevel(logging.DEBUG)
        logger.debug("Verbose logging enabled")
    elif args.quiet:
        logger.setLevel(logging.ERROR)
    
    logger.info("Script started: list_transcripts_with_videos.py")
    
    # Use the constants imported from tools.utils.config
    # Make sure we have Path objects
    try:
        vault_path = Path(NOTEBOOK_VAULT_ROOT)
        onedrive_path = Path(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        
        logger.debug(f"Using vault path from config: {vault_path}")
        logger.debug(f"Using OneDrive path from config: {onedrive_path}")
    except Exception as e:
        logger.error(f"Error setting up paths: {e}")
        print(f"Error setting up paths: {e}")
        sys.exit(1)
    
    # Paths come normalized from the config module, but we'll validate they exist
    logger.info("Verifying directories exist")
    if not os.path.isdir(vault_path):
        error_msg = f"Error: Vault path '{vault_path}' is not a valid directory."
        logger.error(error_msg)
        print(error_msg)
        sys.exit(1)
    
    if not os.path.isdir(onedrive_path):
        error_msg = f"Error: OneDrive path '{onedrive_path}' is not a valid directory."
        logger.error(error_msg)
        print(error_msg)
        sys.exit(1)
        
    logger.info(f"Paths verified. Using vault: {vault_path}")
    logger.info(f"Using OneDrive: {onedrive_path}")

    if args.find_missing_transcripts:
        logger.info("Starting search for videos without transcripts")
        # Find missing transcripts for video files
        video_files = find_video_files(onedrive_path)
        
        # Handle relative paths with error handling
        video_files_rel = []
        for path in video_files:
            try:
                video_files_rel.append(str(path.relative_to(onedrive_path)))
            except ValueError:
                logger.warning(f"Could not determine relative path for video: {path}")
                video_files_rel.append(str(path))
        
        # Check if matching transcript files exist
        results = []
        logger.info(f"Checking {len(video_files)} videos for matching transcripts")
        
        for i, (video_file, rel_path) in enumerate(zip(video_files, video_files_rel), 1):
            if i % 50 == 0:  # Log progress every 50 files
                logger.info(f"Progress: Checked {i}/{len(video_files)} videos")
                
            transcript_path = get_transcript_path(video_file, vault_path, onedrive_path)
            
            # If transcript not found, add to results
            if not transcript_path:
                results.append((rel_path, "NOT FOUND", "❌ MISSING"))
                logger.info(f"Missing transcript for video: {rel_path}")
        
        # Print results
        logger.info(f"Generating report for {len(results)} missing transcripts")
        print("\n=== VIDEO FILES WITHOUT TRANSCRIPTS ===\n")
        print(f"| {'VIDEO PATH':<220} | {'TRANSCRIPT FILE':<80} | {'STATUS':<15} |")
        print(f"| {'-'*220} | {'-'*80} | {'-'*15} |")
        
        for video, transcript, status in results:
            print(f"| {video:<220} | {transcript:<80} | {status:<15} |")
        
        # Print summary
        total = len(video_files)
        missing = len(results)
        found = total - missing
        
        summary = f"\nSummary: {found} out of {total} videos have matching transcript files."
        missing_msg = f"Missing transcripts: {missing}"
        
        print(summary)
        print(missing_msg)
        logger.info(summary)
        logger.info(missing_msg)
        
        # Optionally write missing files to a json file
        if missing > 0:
            output_file = "videos_without_transcripts.json"
            missing_videos = [vd for vd, _, _ in results]
            with open(output_file, "w") as f:
                json.dump(missing_videos, f, indent=2)
            logger.info(f"Wrote list of {missing} videos without transcripts to {output_file}")
    else:
        logger.info("Starting search for transcripts and checking for matching videos")
        # Original flow: Find transcript files and check for videos
        transcript_files = find_transcript_files(vault_path, onedrive_path)
        # For each transcript file, determine the base path to use for relative path calculation
        transcript_files_rel = []
        for path in transcript_files:
            try:
                if str(path).startswith(str(vault_path)):
                    # If file is in vault, get path relative to vault_path
                    rel_path = str(path.relative_to(vault_path))
                    transcript_files_rel.append(rel_path)
                    logger.debug(f"Vault transcript: {rel_path}")
                else:
                    # If file is in OneDrive (like .txt files), get path relative to onedrive_path
                    rel_path = str(path.relative_to(onedrive_path))
                    transcript_files_rel.append(rel_path)
                    logger.debug(f"OneDrive transcript: {rel_path}")
            except ValueError:
                # If path is not relative to either base path, use the full path
                logger.warning(f"Could not determine relative path for: {path}")
                transcript_files_rel.append(str(path))
                logger.debug(f"Using absolute path: {path}")
        
        # Check if matching video files exist
        results = []
        logger.info(f"Checking {len(transcript_files)} transcripts for matching videos")
        
        for i, (transcript_file, rel_path) in enumerate(zip(transcript_files, transcript_files_rel), 1):
            if i % 50 == 0:  # Log progress every 50 files
                logger.info(f"Progress: Checked {i}/{len(transcript_files)} transcripts")
                
            video_path = get_video_path(transcript_file, vault_path, onedrive_path)
            
            # If found, get the path relative to onedrive_path
            if video_path:
                try:
                    video_path_rel = str(video_path.relative_to(onedrive_path))
                except ValueError:
                    # If path is not relative to onedrive_path, use the full path
                    video_path_rel = str(video_path)
                    logger.warning(f"Video path is not relative to OneDrive path: {video_path}")
                
                status = "✅ FOUND"
                logger.debug(f"Found video for transcript: {rel_path} -> {video_path_rel}")
            else:
                video_path_rel = "NOT FOUND"
                status = "❌ MISSING"
                logger.warning(f"Missing video for transcript: {rel_path}")
            
            results.append((rel_path, video_path_rel, status))
        
        # Print results
        logger.info("Generating report for transcript to video matching")
        print("\n=== TRANSCRIPTS WITH VERIFIED VIDEO FILES ===\n")
        print(f"| {'TRANSCRIPT FILE':<80} | {'LOCATION':<10} | {'VIDEO PATH':<210} | {'STATUS':<15} |")
        print(f"| {'-'*80} | {'-'*10} | {'-'*210} | {'-'*15} |")
        
        for transcript, video, status in results:
            # Determine transcript location (Vault or OneDrive)
            location = "OneDrive" if transcript.endswith(".txt") else "Vault"
            print(f"| {transcript:<80} | {location:<10} | {video:<210} | {status:<15} |")
        
        # Print summary
        total = len(results)
        found = sum(1 for _, _, status in results if status == "✅ FOUND")
        missing = total - found
        
        # Count by transcript type
        md_transcripts = sum(1 for tr, _, _ in results if tr.endswith(".md"))
        txt_transcripts = sum(1 for tr, _, _ in results if tr.endswith(".txt"))
        
        summary = f"\nSummary: {found} out of {total} transcripts have matching video files."
        type_summary1 = f"  - {md_transcripts} Markdown transcripts in vault"
        type_summary2 = f"  - {txt_transcripts} Text transcripts in OneDrive"
        
        print(summary)
        print(type_summary1)
        print(type_summary2)
        
        logger.info(summary)
        logger.info(type_summary1)
        logger.info(type_summary2)
        
        # Create the mapping data for Stage 2
        mapping_data = []
        for tr_path, video_path, status in results:
            if status == "✅ FOUND":
                # Only include pairs where the video was found
                transcript_full_path = str(vault_path / tr_path) if not tr_path.startswith('/') else tr_path
                video_full_path = str(onedrive_path / video_path) if not video_path.startswith('/') else video_path
                
                mapping_data.append({
                    "transcript_path": transcript_full_path,
                    "transcript_name": Path(tr_path).name,
                    "video_path": video_full_path,
                    "exists": True
                })
        
        # Write the mapping to a JSON file
        mapping_file = "transcript_video_mapping.json"
        with open(mapping_file, "w") as f:
            json.dump(mapping_data, f, indent=2)
        logger.info(f"Wrote transcript-video mapping to {mapping_file} ({len(mapping_data)} pairs)")
        print(f"\nWrote transcript-video mapping to {mapping_file} ({len(mapping_data)} pairs)")
        
        if missing > 0:
            missing_msg = f"Missing videos: {missing}"
            print(missing_msg)
            logger.warning(missing_msg)
            
            # Optionally write missing files to a json file
            output_file = "transcripts_without_videos.json"
            missing_files = [tr for tr, vd, st in results if st == "❌ MISSING"]
            with open(output_file, "w") as f:
                json.dump(missing_files, f, indent=2)
            logger.info(f"Wrote list of {missing} transcripts without videos to {output_file}")
    
    # Run Stage 2 if requested
    if args.run_stage2 and os.path.exists("transcript_video_mapping.json"):
        logger.info("Running Stage 2 processing")
        print("\nRunning Stage 2 processing...")
        
        # Import the stage2 script and run it
        try:
            # Set up command to run the stage2 script
            cmd = [sys.executable, "process_transcript_stage2.py"]
            
            # Add verbose flag if needed
            if args.verbose:
                cmd.append("--verbose")
                
            # Add dry-run flag if needed
            if args.dry_run:
                cmd.append("--dry-run")
            
            # Run the stage2 script
            import subprocess
            result = subprocess.run(cmd, check=True)
            
            if result.returncode == 0:
                logger.info("Stage 2 processing completed successfully")
                print("Stage 2 processing completed successfully")
            else:
                logger.error(f"Stage 2 processing failed with return code {result.returncode}")
                print(f"Stage 2 processing failed with return code {result.returncode}")
        
        except Exception as e:
            logger.error(f"Failed to run Stage 2 processing: {e}")
            print(f"Failed to run Stage 2 processing: {e}")
    
    logger.info("Script completed successfully")
    print("\nScript completed successfully!")
            
if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        logger.error(f"Uncaught exception: {e}", exc_info=True)
        print(f"Error: {e}")
        sys.exit(1)