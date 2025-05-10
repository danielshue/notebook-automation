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
from notebook_automation.tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.tools.onedrive.onedrive_share import create_sharing_link as create_share_link
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


def _parse_arguments():
    """Parse command-line arguments for video note generation."""
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
    
    Parses arguments, sets up logging, and coordinates the video metadata generation workflow.
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
            folder_path = resources_root / args.folder
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

    # Authenticate with Microsoft Graph API if needed
    graph_client = None
    if not args.dry_run and not args.no_share_links:
        try:
            logger.info("Authenticating with Microsoft Graph API...")
            graph_client = authenticate_graph_api(force_refresh=args.refresh_auth, timeout=args.timeout)
            logger.info("Microsoft Graph API authentication successful.")
        except Exception as auth_exc:
            logger.error(f"Failed to authenticate with Microsoft Graph API: {auth_exc}")
            logger.debug(traceback.format_exc())
            sys.exit(1)
    else:
        logger.info("Skipping Microsoft Graph API authentication (dry-run or no-share-links mode).")

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
                    transcript = process_transcript(video_path)
                    summary = generate_summary_with_openai(transcript) if transcript else None
                    logger.debug("Transcript and summary generated.")
                except Exception as ts_exc:
                    logger.warning(f"Transcript/summary generation failed: {ts_exc}")
                    logger.debug(traceback.format_exc())

            # Create OneDrive share link unless skipped
            share_link = None
            if not args.no_share_links and not args.dry_run:
                try:
                    share_link = create_share_link(graph_client, video_path, timeout=args.timeout)
                    logger.debug(f"Share link created: {share_link}")
                except Exception as link_exc:
                    logger.warning(f"Failed to create share link: {link_exc}")
                    logger.debug(traceback.format_exc())

            # Generate or update the Obsidian markdown note
            if not args.dry_run:
                try:
                    create_or_update_markdown_note_for_video(
                        video_path=video_path,
                        metadata=metadata,
                        share_link=share_link,
                        transcript=transcript,
                        summary=summary,
                        vault_root=VAULT_LOCAL_ROOT,
                        force=args.force
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