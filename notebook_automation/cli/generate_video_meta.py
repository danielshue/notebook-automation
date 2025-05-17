"""
Video Metadata Generator CLI

This CLI scans video files from a resources folder, creates shareable links in OneDrive,
and generates corresponding reference notes (markdown file) in the Obsidian vault.
Generalized for any program or course structure.
"""

import os
import sys
import json
import argparse
from pathlib import Path
from datetime import datetime
import traceback
from tqdm import tqdm  # For compatibility with existing tqdm usage
# Import Rich components for better console rendering
from rich.console import Console
from rich.progress import Progress, TextColumn, BarColumn, TaskProgressColumn, TimeRemainingColumn 
from rich.live import Live
from rich.logging import RichHandler
import logging

# Suppress logger output from external libraries
logging.getLogger('openai').setLevel(logging.ERROR)

# Import shared CLI utilities
from notebook_automation.cli.utils import HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, GREY, BG_BLUE, remove_timestamps_from_logger

# Import from the tools package
from notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT, ONEDRIVE_LOCAL_RESOURCES_ROOT, ensure_logger_configured
from notebook_automation.tools.utils.file_operations import find_files_by_extension
from notebook_automation.cli.onedrive_share import create_sharing_link
from notebook_automation.cli.onedrive_share_helper import create_share_link_once

from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_video
from notebook_automation.tools.ai.summarizer import generate_summary_with_openai
from notebook_automation.tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from notebook_automation.tools.metadata.path_metadata import extract_metadata_from_path
from notebook_automation.tools.transcript.processor import process_transcript
from typing import Dict, List, Tuple, Optional, Union, Any

from notebook_automation.tools.utils import config as config_utils

# Initialize loggers - module-level logger is initialized for immediate use,
# but will be replaced with a more comprehensive setup in main()
logger = ensure_logger_configured(__name__)
failed_logger = None

# Constants
VIDEO_EXTENSIONS = ['.mp4', '.mov', '.avi', '.mkv', '.webm']
RESULTS_FILE = 'video_links_results.json'
FAILED_FILES_JSON = 'failed_video_files.json'


# OpenAI API integration configuration
OPENAI_API_KEY: Optional[str] = os.getenv("OPENAI_API_KEY")

def create_share_link(video_path: Path, timeout: int = 15) -> str | None:
    """
    Get a shareable link for the given file using the onedrive_share_helper module.
    
    This function is a backward-compatible wrapper for create_share_link_once.
    
    Args:
        video_path: Path to the video file
        timeout: Request timeout in seconds
        
    Returns:
        Share link URL or None if creation failed
    """
    # Use the shared implementation from onedrive_share_helper
    result = create_share_link_once(video_path, access_token=None, timeout=timeout)
    
    # Handle the result based on its type (could be dict or string)
    if isinstance(result, dict):
        return result.get('webUrl')
    return result

def check_openai_requirements(args):
    """Check if OpenAI API key is available when needed."""
    if not OPENAI_API_KEY and not getattr(args, 'no_summary', False):
        console = Console(stderr=True)
        console.print("[red]Error:[/red] OpenAI API key not found and --no-summary flag not used.")
        console.print("[yellow]Either:[/yellow]")
        console.print("  1. Set the OPENAI_API_KEY environment variable")
        console.print("  2. Use the --no-summary flag to skip summary generation")
        sys.exit(1)


def _parse_arguments() -> argparse.Namespace:
    """Parse command-line arguments for video note generation.
    
    Sets up the argument parser with all supported options for the video metadata
    generator tool, including file selection, resource paths, authentication options,
    and processing controls.
    
    Returns:
        argparse.Namespace: Object containing all parsed command line arguments
        
    Example:
        >>> args = _parse_arguments()
        >>> print(args.verbose)
        True
    """
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for videos and create reference notes in Obsidian vault."
    )
    file_group = parser.add_mutually_exclusive_group()
    file_group.add_argument(
        "-f", "--single-file",
        help="Process only a single file (path relative to resources root)"
    )
    file_group.add_argument(
        "--folder",
        help="Process all video files in a directory (path relative to resources root)"
    )
    parser.add_argument(
        "--resources-root",
        type=str,
        default=None,
        help="Override the default resources root directory (for testing)"
    )
    parser.add_argument(
        "--no-summary",
        action="store_true",
        help="Disable OpenAI summary generation for transcripts"
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Show detailed per-file processing output"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Perform a dry run without creating actual links or notes"
    )
    parser.add_argument(
        "--debug",
        action="store_true",
        help="Enable debug logging"
    )
    parser.add_argument(
        "--retry-failed",
        action="store_true",
        help="Retry only previously failed files from failed_files.json"
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Force processing of videos that already have notes (overwrite existing)"
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=15,
        help="Set API request timeout in seconds (default: 15)"
    )
    parser.add_argument(
        "--refresh-auth",
        action="store_true",
        help="Force refresh the Microsoft Graph API authentication cache (ignore cached tokens)"
    )
    parser.add_argument(
        "--no-share-links",
        action="store_true",
        help="Skip creating OneDrive shared links (faster for testing)"
    )
    parser.add_argument(
        '-c', '--config', type=str, default=None, help='Path to config.json')
    return parser.parse_args()


def main() -> None:
    """Main entry point for the Video Metadata Generator CLI.
    
    Parses command line arguments, sets up logging, authenticates with OneDrive if needed,
    and processes video files to generate markdown notes with metadata, transcripts, and
    shareable links. Handles failures gracefully and maintains a record of processed files.
    
    Args:
        None
        
    Returns:
        None: This function doesn't return a value but creates files in the filesystem
        
    Example:
        When called from the command line:
        $ vault-generate-video-meta --folder "MBA/Finance" --verbose
    """
    global logger, failed_logger
    args = _parse_arguments()

    # Check OpenAI requirements before proceeding
    check_openai_requirements(args)    
    config = config_utils.load_config_data(args.config)
    
    # Create Rich console for all output
    console = Console()
    
    # Set up enhanced logging with our custom configuration
    # The module logger is already initialized, but we need the full setup for consistent formatting
    logger, failed_logger = setup_logging(debug=args.debug, use_rich=True)
    
    # Remove timestamps from ALL loggers for cleaner console output
    from notebook_automation.cli.utils import remove_timestamps_from_logger
    
    # Remove timestamps from root logger first to affect all output
    root_logger = logging.getLogger()
    remove_timestamps_from_logger(root_logger)
    
    # Also remove from our specific loggers to ensure consistency
    remove_timestamps_from_logger(logger)
    remove_timestamps_from_logger(failed_logger)
    
    if args.debug:
        logger.debug("Debug logging enabled.")
    if args.verbose:
        logger.info("Verbose output enabled.")
    if args.dry_run:
        logger.warning("Dry run mode: No files or links will be created.")

    logger.info("Starting Video Metadata Generator CLI...")
    logger.debug(f"Parsed arguments: {args}")

    # Log all relevant CLI options for transparency
    logger.debug(f"Options: dry_run={args.dry_run}, verbose={args.verbose}, "
                 f"retry_failed={args.retry_failed}, force={args.force}, "
                 f"timeout={args.timeout}, refresh_auth={args.refresh_auth}, "
                 f"no_share_links={args.no_share_links}")

    # Find video files to process (single file, folder, or retry failed)
    video_files = []
    # Determine resources root (for testability)
    resources_root = Path(args.resources_root) if args.resources_root else Path(config['paths']['resources_root'])
    try:
        if args.retry_failed:
            logger.info(f"{OKCYAN}Retrying failed files from previous run...{ENDC}")
            failed_path = Path(FAILED_FILES_JSON)
            if failed_path.exists():
                with failed_path.open("r", encoding="utf-8") as f:
                    failed_data = json.load(f)
                    video_files = [Path(p) for p in failed_data.get("failed_files", [])]
                logger.info(f"{OKGREEN}Loaded {len(video_files)} failed files from {FAILED_FILES_JSON}.{ENDC}")
            else:
                logger.warning(f"{WARNING}No failed files found at {FAILED_FILES_JSON}.{ENDC}")
        elif args.single_file:
            video_files = [resources_root / args.single_file]
            logger.info(f"{OKGREEN}Processing single file: {video_files[0]}{ENDC}")
        elif args.folder:
            folder_arg = Path(args.folder)
            if folder_arg.is_absolute():
                folder_path = folder_arg
            else:
                if folder_arg.parts and folder_arg.parts[0] == resources_root.name:
                    folder_path = resources_root
                    if len(folder_arg.parts) > 1:
                        folder_path = resources_root.joinpath(*folder_arg.parts[1:])
                else:
                    folder_path = resources_root / folder_arg
            video_files = []
            ext_counts = {}
            for ext in VIDEO_EXTENSIONS:
                found = find_files_by_extension(folder_path, ext)
                ext_counts[ext] = len(found)
                video_files.extend(found)
            # Colorful, grouped summary for all extensions
            found_lines = []
            for ext, count in ext_counts.items():
                color = OKGREEN if count > 0 else GREY
                found_lines.append(f"{color}{count} {ext}{ENDC}")
            logger.info(f"{OKCYAN}Video file summary in {folder_path}:{ENDC} " + ", ".join(found_lines))
            logger.info(f"{OKGREEN}Found {len(video_files)} video files in folder: {folder_path}{ENDC}")
        else:
            logger.error(f"{FAIL}No input specified. Use --single-file, --folder, or --retry-failed.{ENDC}")
            sys.exit(1)
    except Exception as file_exc:
        logger.error(f"{FAIL}Error finding video files: {file_exc}{ENDC}")
        logger.debug(traceback.format_exc())
        sys.exit(1)

    # No direct authentication here; handled by CLI if needed
    if not args.dry_run and not args.no_share_links:
        logger.info("Share links will be created using the onedrive_share.py CLI.")
    elif args.no_share_links:
        logger.info("Skipping share link creation (--no-share-links set).")
    else:
        logger.info("Skipping share link creation (dry-run mode).")

    # --- Enhanced summary and color output ---

    total_videos = len(video_files)
    transcript_files_found = 0
    transcript_files_processed = 0
    videos_success = 0
    videos_failed = 0
    results = []
    failed_files = []

    # Import has_corresponding_mp4 for transcript language handling
    try:
        from notebook_automation.cli.convert_markdown import has_corresponding_mp4
    except ImportError:
        has_corresponding_mp4 = None

    import re
    def find_transcript(video_path: Path) -> Path | None:
        """
        Find a transcript file for a video, handling language codes (e.g., foo.txt, foo.en.txt, foo.zh-cn.txt).
        Matches files with the same base name as the video, optionally with a language code, and ending in .txt.
        Language code: 2-5 lowercase letters, optionally hyphenated (e.g., en, zh-cn, pt-br).
        """
        parent = video_path.parent
        base = video_path.stem
        # Regex: foo.txt, foo.en.txt, foo.zh-cn.txt, etc.
        pattern = re.compile(rf'^{re.escape(base)}(?:\.[a-z]{{2,5}}(?:-[a-z]{{2,5}})?)?\.txt$', re.IGNORECASE)
        for cand in parent.iterdir():
            if cand.is_file() and pattern.match(cand.name):
                return cand
        return None
    
    logger.info(f"{OKCYAN}Total videos to process: {total_videos}{ENDC}")
    import time
    start_time = time.time()
    times = []
    
    # Create a console for Rich output
    console = Console()
      # Create a custom progress bar with specific configuration to stay fixed at the top
    from rich.panel import Panel
    
    # Create a progress bar with enhanced visibility for top placement
    progress = Progress(
        TextColumn("[bold blue on white]{task.description}"), # More visible header for top placement
        BarColumn(bar_width=None),
        "[progress.percentage]{task.percentage:>3.0f}%",
        "•",
        TaskProgressColumn(),
        "•",
        TimeRemainingColumn(),
        console=console,
        # Critical settings to ensure progress bar stability:
        transient=False,    # Ensure the progress bar stays visible
        expand=True,        # Use full terminal width
        auto_refresh=False, # Only refresh when explicitly requested
    )
      # Add our task with a highly visible format for top placement
    task = progress.add_task(f"Processing Videos: 0/{total_videos}", total=total_videos)    # Use Live display with specific parameters to keep progress bar visible
    with Live(
        progress, 
        console=console,
        refresh_per_second=0.5,    # Lower refresh rate to reduce flicker
        vertical_overflow="visible", # Prevent content scrolling
        auto_refresh=False,        # We'll control refreshes manually
        redirect_stdout=True,      # Capture all output to maintain progress stability
        transient=False            # Keep display visible at all times
        # Note: 'top' parameter removed as it's not supported in this Rich version
    ):
        # Now process each video file
        for idx, video_path in enumerate(video_files):
            file_start = time.time()
            current_file = f"Current: {video_path.name}"
              # Update the progress description with current file and explicitly refresh
            # Use bold blue on white for better visibility at the top of the screen
            progress.update(task, description=f"Processing Video {idx+1}/{total_videos}: {current_file}", refresh=True)
            
            try:
                # Extract metadata from path
                metadata = extract_metadata_from_path(video_path)
                logger.debug(f"Extracted metadata: {metadata}")

                # Optionally process transcript and summary
                transcript = None
                summary = None
                transcript_file = find_transcript(video_path)
                transcript_found = transcript_file is not None and transcript_file.exists()
                if not args.no_summary:
                    try:
                        if transcript_found:
                            transcript_files_found += 1
                            with open(transcript_file, 'r', encoding='utf-8') as tf:
                                transcript = tf.read()
                            # Load prompts from the prompts directory
                            chunk_prompt_path = Path(__file__).parent.parent.parent / 'prompts' / 'chunk_summary_prompt.md'
                            final_prompt_path = Path(__file__).parent.parent.parent / 'prompts' / 'final_summary_prompt_video.md'
                            fallback_prompt_path = Path(__file__).parent.parent.parent / 'prompts' / 'final_summary_prompt.md'
                            
                            # Load chunk prompt
                            if chunk_prompt_path.exists():
                                with open(chunk_prompt_path, 'r', encoding='utf-8') as pf:
                                    chunked_system_prompt = pf.read()
                            else:
                                chunked_system_prompt = "Summarize the following transcript chunk."
                                
                            # Load final prompt - try video-specific first, then generic, then default
                            if final_prompt_path.exists():
                                with open(final_prompt_path, 'r', encoding='utf-8') as pf:
                                    system_prompt = pf.read()
                                logger.debug(f"Loaded video summary prompt from {final_prompt_path}")
                            elif fallback_prompt_path.exists():
                                with open(fallback_prompt_path, 'r', encoding='utf-8') as pf:
                                    system_prompt = pf.read()
                                logger.debug(f"Loaded generic summary prompt from {fallback_prompt_path}")
                            else:
                                system_prompt = "You are an MBA course summarizer."
                                logger.warning(f"No summary prompt files found, using default system prompt")

                            prompt_metadata = metadata.copy() if metadata else {}
                            # Use share_link only if it has already been created, otherwise use video_path
                            prompt_metadata['onedrive_path'] = str(video_path)
                            prompt_metadata['course'] = metadata.get('course', '') if metadata else ''

                            onedrive_path = prompt_metadata['onedrive_path']
                            course = prompt_metadata['course']

                            # Use a template with placeholders for course and onedrive path
                            user_prompt_template = (
                                'You are an educational content summarizer for MBA course materials. '
                                f'Generate a clear and insightful summary of the following chunk from the video "{onedrive_path}", part of the course "{course}".'
                            )

                            # Prepare metadata for prompt formatting
                            user_prompt = user_prompt_template.format(**prompt_metadata)
                            summary = generate_summary_with_openai(
                                transcript,
                                system_prompt,
                                chunked_system_prompt,
                                user_prompt,
                                metadata=metadata
                            ) if transcript else None
                            transcript_files_processed += 1
                            if summary:
                                logger.debug(f"Summary returned from OpenAI (first 300 chars): {summary[:300]}")
                                # Check if summary has proper structure with headers
                                has_headers = any(line.startswith('#') for line in summary.split('\n') if line.strip())
                                logger.debug(f"Summary has markdown headers: {has_headers}")
                                logger.debug(f"Summary length: {len(summary)} chars, lines: {len(summary.split('\n'))}")
                            else:
                                logger.debug("No summary returned from OpenAI.")
                            logger.debug("Transcript and summary generated.")
                        else:
                            logger.info(f"Transcript file not found for {video_path}")
                    except Exception as ts_exc:
                        logger.warning(f"Transcript/summary generation failed: {ts_exc}")
                        logger.debug(traceback.format_exc())

                # Create OneDrive share link using imported function unless skipped
                share_link = None
                if not args.no_share_links and not args.dry_run:
                    share_link = create_share_link(video_path, timeout=args.timeout)
                    if share_link:
                        logger.debug(f"Share link created: {share_link}")
                    else:
                        logger.warning(f"Failed to create share link for {video_path}")

                # Generate or update the Obsidian markdown note
                note_success = True
                if not args.dry_run:
                    try:
                        # Compute output_path for the note in the vault
                        # Place note in the same relative structure under VAULT_LOCAL_ROOT, with .md extension
                        rel_path = video_path.relative_to(resources_root)
                        note_name = video_path.stem + "-video.md"
                        output_path = VAULT_LOCAL_ROOT / rel_path.parent / note_name
                        
                        create_or_update_markdown_note_for_video(
                            str(video_path),
                            str(output_path),
                            share_link,
                            transcript,
                            summary,
                            metadata
                        )
                        logger.info(f"Note created/updated for {video_path.name}")
                    except Exception as note_exc:
                        logger.error(f"Failed to create/update note: {note_exc}")
                        logger.debug(traceback.format_exc())
                        failed_files.append(str(video_path))
                        note_success = False

                # Log results
                results.append({
                    "file": str(video_path),
                    "metadata": metadata,
                    "share_link": share_link,
                    "summary": summary
                })
                
                if note_success:
                    videos_success += 1
                else:
                    videos_failed += 1
                    
            except Exception as exc:
                logger.error(f"Error processing {video_path}: {exc}")
                logger.debug(traceback.format_exc())
                failed_files.append(str(video_path))
                videos_failed += 1
                  # Advance progress bar and explicitly refresh it
            progress.advance(task)
            
            # Update progress description to show the next file that will be processed
            next_idx = idx + 1
            if next_idx < len(video_files):
                next_file = f"Next: {video_files[next_idx].name}"
                progress.update(task, description=f"Processing Video {next_idx+1}/{total_videos}: {next_file}", refresh=True)
            
            # Force refresh to ensure the progress bar updates correctly and stays at the top
            progress.refresh()
            file_end = time.time()
            times.append(file_end - file_start)# Write results and failed files to JSON
    if not args.dry_run:
        try:
            # Update results file with a dictionary containing the results list
            update_results_file(RESULTS_FILE, {'processed_videos': results, 'timestamp': datetime.now().isoformat()})
            logger.info(f"{OKGREEN}Results written to {RESULTS_FILE}{ENDC}")
        except Exception as res_exc:
            logger.error(f"{FAIL}Failed to write results: {res_exc}{ENDC}")
            logger.debug(traceback.format_exc())
            
        try:
            if failed_files:
                update_failed_files(FAILED_FILES_JSON, failed_files, "Failed to process video")
                logger.info(f"{OKGREEN}Failed files written to {FAILED_FILES_JSON}{ENDC}")
            else:
                logger.info(f"{OKGREEN}No failed files to record{ENDC}")
        except Exception as fail_exc:
            logger.error(f"{FAIL}Failed to write failed files: {fail_exc}{ENDC}")
            logger.debug(traceback.format_exc())

    # --- Colorized summary output ---
    SEP = '\n' + '-' * 48
    logger.info(f"{SEP}\n{BOLD}{OKCYAN}Summary of findings:{ENDC}")
    logger.info(f"  {OKGREEN}Total video files found:         {total_videos}{ENDC}")
    logger.info(f"  {OKGREEN}Transcript files found:          {transcript_files_found}{ENDC}")
    logger.info(f"  {OKGREEN}Transcript files processed:      {transcript_files_processed}{ENDC}")
    logger.info(f"  {OKGREEN}Videos successfully processed:   {videos_success}{ENDC}")
    logger.info(f"  {FAIL}Videos failed:                  {videos_failed}{ENDC}")
    logger.info(SEP)
    if args.dry_run:
        logger.info(f"{WARNING}This was a dry run. No files or notes were created.{ENDC}")
    logger.info(f"{OKCYAN}Video metadata generation process completed.{ENDC}")


if __name__ == "__main__":
    main()