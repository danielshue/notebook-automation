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

# Import shared CLI utilities
from notebook_automation.cli.utils import HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, GREY, BG_BLUE, remove_timestamps_from_logger

# Import from the tools package
from notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT, ONEDRIVE_LOCAL_RESOURCES_ROOT
from notebook_automation.tools.utils.file_operations import find_files_by_extension
from notebook_automation.cli.onedrive_share import create_sharing_link

def create_share_link(video_path: Path, timeout: int = 15) -> str | None:
    """Get a shareable link for the given file using the imported function from onedrive_share.py."""
    # The function signature in onedrive_share.py is: create_sharing_link(access_token: str, file_path: str) -> str | None
    # We need to authenticate and get an access token first.
    from notebook_automation.cli.onedrive_share import authenticate_interactive
    access_token = authenticate_interactive()
    if not access_token:
        return None
    # file_path should be relative to OneDrive root, e.g. /Education/MBA-Resources/...
    from notebook_automation.tools.utils.config import ONEDRIVE_BASE
    video_path_str = str(video_path).replace("\\", "/")
    marker = ONEDRIVE_BASE if ONEDRIVE_BASE.startswith("/") else "/" + ONEDRIVE_BASE
    idx = video_path_str.find(marker)
    if idx == -1:
        # Try without leading slash
        marker = ONEDRIVE_BASE.lstrip("/")
        idx = video_path_str.find(marker)
        if idx == -1:
            # Fallback: use the filename only (will likely fail, but avoids crash)
            rel_path = os.path.basename(video_path_str)
        else:
            rel_path = "/" + video_path_str[idx:]
    else:
        rel_path = video_path_str[idx:]
        if not rel_path.startswith("/"):
            rel_path = "/" + rel_path
    return create_sharing_link(access_token, rel_path)
from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_video
from notebook_automation.tools.ai.summarizer import generate_summary_with_openai
from notebook_automation.tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from notebook_automation.tools.metadata.path_metadata import extract_metadata_from_path
from notebook_automation.tools.transcript.processor import process_transcript

# Initialize loggers as global variables to be populated in main()
logger = None
failed_logger = None

# Constants
VIDEO_EXTENSIONS = ['.mp4', '.mov', '.avi', '.mkv', '.webm']
RESULTS_FILE = 'video_links_results.json'
FAILED_FILES_JSON = 'failed_video_files.json'


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

    # Set up logging based on CLI arguments
    logger, failed_logger = setup_logging(debug=args.debug)
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
    resources_root = Path(args.resources_root) if args.resources_root else ONEDRIVE_LOCAL_RESOURCES_ROOT
    try:
        if args.retry_failed:
            logger.info("Retrying failed files from previous run...")
            failed_path = Path(FAILED_FILES_JSON)
            if failed_path.exists():
                with failed_path.open("r", encoding="utf-8") as f:
                    failed_data = json.load(f)
                    video_files = [Path(p) for p in failed_data.get("failed_files", [])]
                logger.info(f"Loaded {len(video_files)} failed files from {FAILED_FILES_JSON}.")
            else:
                logger.warning(f"No failed files found at {FAILED_FILES_JSON}.")
        elif args.single_file:
            video_files = [resources_root / args.single_file]
            logger.info(f"Processing single file: {video_files[0]}")
        elif args.folder:
            # If args.folder is an absolute path, use it directly. If it's relative and starts with the name of the resources root, don't join twice.
            folder_arg = Path(args.folder)
            if folder_arg.is_absolute():
                folder_path = folder_arg
            else:
                # If the folder argument is the same as the resources root name, don't join twice
                if folder_arg.parts and folder_arg.parts[0] == resources_root.name:
                    folder_path = resources_root
                    if len(folder_arg.parts) > 1:
                        folder_path = resources_root.joinpath(*folder_arg.parts[1:])
                else:
                    folder_path = resources_root / folder_arg
            video_files = []
            for ext in VIDEO_EXTENSIONS:
                video_files.extend(find_files_by_extension(folder_path, ext))
            logger.info(f"Found {len(video_files)} video files in folder: {folder_path}")
        else:
            logger.error("No input specified. Use --single-file, --folder, or --retry-failed.")
            sys.exit(1)
    except Exception as file_exc:
        logger.error(f"Error finding video files: {file_exc}")
        logger.debug(traceback.format_exc())
        sys.exit(1)

    # No direct authentication here; handled by CLI if needed
    if not args.dry_run and not args.no_share_links:
        logger.info("Share links will be created using the onedrive_share.py CLI.")
    elif args.no_share_links:
        logger.info("Skipping share link creation (--no-share-links set).")
    else:
        logger.info("Skipping share link creation (dry-run mode).")

    # Process each video file
    results = []
    failed_files = []
    for video_path in video_files:
        try:
            logger.info(f"Processing video: {video_path}")
            # Extract metadata from path
            metadata = extract_metadata_from_path(video_path)
            logger.debug(f"Extracted metadata: {metadata}")


            # Optionally process transcript and summary
            transcript = None
            summary = None
            if not args.no_summary:
                try:
                    # Look for transcript file with same stem as video, .txt extension
                    transcript_path = video_path.with_suffix('.txt')
                    if transcript_path.exists():
                        with open(transcript_path, 'r', encoding='utf-8') as tf:
                            transcript = tf.read()
                        # Load prompts from the prompts directory
                        chunk_prompt_path = Path(__file__).parent.parent.parent / 'prompts' / 'chunk_summary_prompt.md'
                        if chunk_prompt_path.exists():
                            with open(chunk_prompt_path, 'r', encoding='utf-8') as pf:
                                chunked_system_prompt = pf.read()
                        else:
                            chunked_system_prompt = "Summarize the following transcript chunk."
                        # Use a simple system/user prompt for now
                        system_prompt = "You are an MBA course video summarizer."
                        user_prompt = "Summarize the following transcript for MBA students."
                        summary = generate_summary_with_openai(
                            transcript,
                            system_prompt,
                            chunked_system_prompt,
                            user_prompt
                        ) if transcript else None
                        logger.debug("Transcript and summary generated.")
                    else:
                        logger.info(f"Transcript file not found for {video_path}, expected at {transcript_path}")
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
                    continue

            # Log results
            results.append({
                "file": str(video_path),
                "metadata": metadata,
                "share_link": share_link,
                "summary": summary
            })
        except Exception as exc:
            logger.error(f"Error processing {video_path}: {exc}")
            logger.debug(traceback.format_exc())
            failed_files.append(str(video_path))

    # Write results and failed files to JSON
    if not args.dry_run:
        try:
            update_results_file(RESULTS_FILE, results)
            logger.info(f"Results written to {RESULTS_FILE}")
        except Exception as res_exc:
            logger.error(f"Failed to write results: {res_exc}")
            logger.debug(traceback.format_exc())
        try:
            update_failed_files(FAILED_FILES_JSON, failed_files)
            logger.info(f"Failed files written to {FAILED_FILES_JSON}")
        except Exception as fail_exc:
            logger.error(f"Failed to write failed files: {fail_exc}")
            logger.debug(traceback.format_exc())

    logger.info("Video metadata generation process completed.")


if __name__ == "__main__":
    main()