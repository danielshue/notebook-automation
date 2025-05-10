def _parse_arguments():
    """Parse command-line arguments for PDF note generation."""
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for PDFs and create reference notes in Obsidian vault."
    )
    file_group = parser.add_mutually_exclusive_group()
    file_group.add_argument(
        "-f", "--single-file",
        help="Process only a single file (path relative to OneDrive base path)"
    )
    file_group.add_argument(
        "--folder",
        help="Process all PDF files in a directory (path relative to OneDrive base path)"
    )
    parser.add_argument(
        "--no-summary",
        action="store_true",
        help="Disable OpenAI summary generation"
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
        help="Force processing of PDFs that already have notes (overwrite existing)"
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


# === IMPORTS (move to top) ===
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
from notebook_automation.tools.utils.file_operations import get_vault_path_for_pdf, find_all_pdfs, get_scan_root
from notebook_automation.tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.tools.onedrive.onedrive_share import create_sharing_link as create_share_link
from notebook_automation.tools.pdf.processor import extract_pdf_text
from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_pdf
from notebook_automation.tools.ai.summarizer import generate_summary_with_openai
from notebook_automation.tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from notebook_automation.tools.metadata.path_metadata import infer_course_and_program
from notebook_automation.tools.ai.prompt_utils import format_final_user_prompt_for_pdf, format_chuncked_user_prompt_for_pdf

# Initialize loggers as global variables to be populated in main()
logger = None
failed_logger = None

# Constants
PDF_EXTENSIONS = {'.pdf'}
RESULTS_FILE = 'pdf_notes_results.json'
FAILED_FILES_JSON = 'failed_PDF_files.json'

# === CLI Entrypoint ===
def main():
    """Main entry point for the PDF note generation CLI."""
    global logger, failed_logger
    args = _parse_arguments()

    # Setup logging
    logger, failed_logger = setup_logging(
        debug=args.debug,
        log_file="generate_pdf_notes.log",
        failed_log_file="generate_pdf_notes_failed_files.log",
        console_output=True
    )

    # Remove timestamps from logger if not in debug mode for cleaner CLI output
    if not args.debug:
        remove_timestamps_from_logger(logger)
        remove_timestamps_from_logger(failed_logger)

    logger.info(HEADER)
    logger.info("PDF Note Generation CLI started.")

    # Authenticate if needed (unless --no-share-links is set)
    access_token = None
    if not getattr(args, 'no_share_links', False):
        logger.info("Authenticating with Microsoft Graph API...")
        access_token = authenticate_graph_api(force_refresh=args.refresh_auth)
        if not access_token:
            logger.error("Failed to authenticate with Microsoft Graph API.")
            print(f"{FAIL}❌ Failed to authenticate with Microsoft Graph API.{ENDC}")
            return 1

    processed = []
    errors = []

    # Dispatch based on arguments
    if getattr(args, 'retry_failed', False):
        logger.info("Retrying previously failed files...")
        processed, errors = _retry_failed_files(args=args, access_token=access_token)
    elif getattr(args, 'single_file', None):
        logger.info(f"Processing single file: {args.single_file}")
        processed, errors = _process_single_file(args.single_file, args=args, access_token=access_token)
    else:
        folder = getattr(args, 'folder', None)
        logger.info(f"Processing folder: {folder if folder else '[default OneDrive root]'}")
        processed, errors = _process_pdfs_for_notes(args=args, folder_path=folder, access_token=access_token, dry_run=args.dry_run)

    # Print summary to console
    print(f"\n{BOLD}PDF Note Generation Summary{ENDC}")
    print(f"{OKGREEN}✓ Processed: {len(processed)} PDFs{ENDC}")
    print(f"{FAIL}✗ Errors: {len(errors)}{ENDC}")
    if errors and args.verbose:
        print(f"\n{FAIL}Failed files:{ENDC}")
        for err in errors:
            print(f"  {err.get('file')}: {err.get('error')}")
    print(f"\n{OKBLUE}Results saved to: {RESULTS_FILE}{ENDC}")
    return 0

def _parse_arguments():
    """Parse command-line arguments for PDF note generation."""
    parser = argparse.ArgumentParser(
        description="Generate shareable OneDrive links for PDFs and create reference notes in Obsidian vault."
    )
    file_group = parser.add_mutually_exclusive_group()
    file_group.add_argument(
        "-f", "--single-file",
        help="Process only a single file (path relative to OneDrive base path)"
    )
    file_group.add_argument(
        "--folder",
        help="Process all PDF files in a directory (path relative to OneDrive base path)"
    )
    parser.add_argument(
        "--no-summary",
        action="store_true",
        help="Disable OpenAI summary generation"
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
        help="Force processing of PDFs that already have notes (overwrite existing)"
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

# Allow running as a script directly
if __name__ == "__main__":
    import sys
    sys.exit(main())
def _process_single_file(file_path, args=None, access_token=None):
    """
    Process a single file specified by the --single-file argument.
    Args:
        file_path (str): Path to the file (relative to OneDrive base path)
        args (Namespace): Command-line arguments
        access_token (str): Microsoft Graph API access token
        verbose (bool): Whether to print per-file colorized output
    Returns:
        tuple: Lists of processed PDFs and errors
    """
    verbose = getattr(args, 'verbose', False)
    logger.info("\n" + "="*60)
    logger.info("🔍 SINGLE FILE PROCESSING")
    logger.info("="*60)
    file_path = Path(file_path)
    if not file_path.is_absolute():
        file_path = ONEDRIVE_LOCAL_RESOURCES_ROOT / file_path
    if not file_path.exists():
        logger.error(f"File not found: {file_path}")
        logger.info(f"❌ File not found: {file_path}")
        if verbose:
            print(f"{FAIL}❌ File not found: {file_path}{ENDC}")
        return [], []
    if file_path.suffix.lower() not in PDF_EXTENSIONS:
        logger.error(f"File is not a PDF: {file_path}")
        logger.info(f"❌ File is not a PDF: {file_path}")
        if verbose:
            print(f"{FAIL}❌ File is not a PDF: {file_path}{ENDC}")
        return [], []
    logger.info(f"📄 Processing file: {file_path.name}")
    logger.info(f"📂 Located at: {file_path}")
    vault_dir = get_vault_path_for_pdf(file_path)
    logger.info(f"📝 Will save note to: {vault_dir}")
    logger.info("\n" + "-"*50)
    result = _process_single_pdf(file_path, vault_dir, args, access_token)
    if verbose:
        if result.get('success', False):
            if result.get('skipped', False):
                print(f"{WARNING}⏭️  Skipped (already exists): {file_path.name}{ENDC}")
            else:
                print(f"{OKGREEN}✓ Processed: {file_path.name}{ENDC}")
        else:
            print(f"{FAIL}✗ Failed: {file_path.name} - {result.get('error', 'Unknown error')}{ENDC}")
    if result.get('success', False):
        return [result], []
    else:
        return [], [result]
def _retry_failed_files(args=None, access_token=None):
    """
    Retry processing files that previously failed.
    Args:
        args (Namespace): Command-line arguments
        access_token (str): Microsoft Graph API access token
        verbose (bool): Whether to print per-file colorized output
    Returns:
        tuple: Lists of processed PDFs and errors
    """
    verbose = getattr(args, 'verbose', False)
    logger.info("\n" + "="*60)
    logger.info("🔄 RETRYING FAILED FILES")
    logger.info("="*60)
    try:
        with open(FAILED_FILES_JSON, 'r') as f:
            failed_files = json.load(f)
    except Exception as e:
        logger.error(f"Error loading failed files: {e}")
        logger.info(f"❌ Error loading failed files: {e}")
        if verbose:
            print(f"{FAIL}❌ Error loading failed files: {e}{ENDC}")
        return [], []
    failed_pdfs = failed_files.get("failed_pdfs", [])
    if not failed_pdfs:
        logger.info("✅ No failed files to retry.")
        if verbose:
            print(f"{OKGREEN}✅ No failed files to retry.{ENDC}")
        return [], []
    logger.info(f"🔄 Found {len(failed_pdfs)} previously failed files to retry...")
    categories = {}
    for f in failed_pdfs:
        cat = f.get('category', 'Unknown')
        if cat not in categories:
            categories[cat] = 0
        categories[cat] += 1
    logger.info("Error categories to retry:")
    for cat, count in categories.items():
        logger.info(f"  - {cat}: {count} files")
    processed = []
    errors = []
    for index, failed in enumerate(failed_pdfs):
        file_path = failed.get('file')
        if not file_path:
            continue
        try:
            pdf_path = Path(file_path)
            if not pdf_path.exists():
                logger.warning(f"Failed file no longer exists: {file_path}")
                if verbose:
                    print(f"{WARNING}⚠️  Failed file no longer exists: {file_path}{ENDC}")
                continue
            logger.info(f"[{index+1}/{len(failed_pdfs)}] Retrying: {pdf_path.name}")
            vault_dir = get_vault_path_for_pdf(pdf_path)
            result = _process_single_pdf(pdf_path, vault_dir, args, access_token)
            if verbose:
                if result.get('success', False):
                    if result.get('skipped', False):
                        print(f"{WARNING}⏭️  Skipped (already exists): {pdf_path.name}{ENDC}")
                    else:
                        print(f"{OKGREEN}✓ Retried: {pdf_path.name}{ENDC}")
                else:
                    print(f"{FAIL}✗ Failed: {pdf_path.name} - {result.get('error', 'Unknown error')}{ENDC}")
            if result.get('success', False):
                processed.append(result)
            else:
                errors.append(result)
        except Exception as e:
            logger.error(f"Error retrying failed file {file_path}: {e}")
            if verbose:
                print(f"{FAIL}✗ Error retrying failed file {file_path}: {e}{ENDC}")
    if processed:
        try:
            still_failed = [f for f in failed_pdfs if f.get('file') not in [p.get('file') for p in processed]]
            failed_files["failed_pdfs"] = still_failed
            with open(FAILED_FILES_JSON, 'w') as f:
                json.dump(failed_files, f, indent=2)
        except Exception as e:
            logger.error(f"Error updating failed files list: {e}")
    logger.info(f"\n✅ Successfully processed {len(processed)} previously failed files")
    logger.info(f"❌ Still failed: {len(errors)} files")
    return processed, errors
def _process_pdfs_for_notes(args=None, folder_path=None, access_token=None, dry_run=False):
    """
    Process all PDFs found in the specified folder or OneDrive root.
    Args:
        args (Namespace): Command-line arguments
        folder_path (str): Path to folder to process
        access_token (str): Microsoft Graph API access token
        dry_run (bool): If True, don't actually write any files
        verbose (bool): Whether to print per-file colorized output
    Returns:
        tuple: List of processed PDFs and list of errors
    """
    verbose = getattr(args, 'verbose', False)
    logger.info("\n" + "="*60)
    logger.info("🔍 PDF SCAN & PROCESSING")
    logger.info("="*60)
    scan_root = get_scan_root(folder_path)
    if scan_root is None:
        logger.info("❌ Could not determine scan root directory")
        if verbose:
            print(f"{FAIL}❌ Could not determine scan root directory{ENDC}")
        return [], []
    logger.info(f"📁 Scanning for PDFs under OneDrive at:")
    logger.info(f"  {scan_root}")
    start_time = datetime.now()
    pdf_paths = find_all_pdfs(scan_root)
    scan_duration = datetime.now() - start_time
    total_pdfs = len(pdf_paths)
    logger.info(f"Found {total_pdfs} PDF files in OneDrive at {scan_root}")
    if total_pdfs > 0:
        logger.info(f"\n✅ Found {total_pdfs} PDF files to process")
        logger.info(f"⏱️  Scan completed in {scan_duration.total_seconds():.1f} seconds")
        preview_count = min(5, total_pdfs)
        logger.info("\n📄 Files to process (sample):")
        for i in range(preview_count):
            logger.info(f"  {i+1}. {pdf_paths[i].name}")
        if total_pdfs > preview_count:
            logger.info(f"  ... and {total_pdfs - preview_count} more files\n")
    else:
        logger.info("⚠️ No PDF files found in the specified location")
    if total_pdfs == 0:
        logger.info("❗ No PDF files found. Nothing to process.")
        if verbose:
            print(f"{WARNING}⚠️  No PDF files found in the specified location{ENDC}")
        return [], []
    processed_pdfs = []
    errors = []
    for index, pdf_path in enumerate(pdf_paths):
        pdf_name = pdf_path.name
        logger.info(f"[{index+1}/{total_pdfs}] Processing: {pdf_name}")
        vault_dir = get_vault_path_for_pdf(pdf_path)
        result = _process_single_pdf(pdf_path, vault_dir, args, access_token, dry_run)
        if verbose:
            if result.get('success', False):
                if result.get('skipped', False):
                    print(f"{WARNING}⏭️  Skipped (already exists): {pdf_name}{ENDC}")
                else:
                    print(f"{OKGREEN}✓ Processed: {pdf_name}{ENDC}")
            else:
                print(f"{FAIL}✗ Failed: {pdf_name} - {result.get('error', 'Unknown error')}{ENDC}")
        if result.get('success', False):
            processed_pdfs.append(result)
        else:
            errors.append(result)
        logger.info("")
    try:
        with open(RESULTS_FILE, 'r') as f:
            final_results = json.load(f)
            all_pdfs_count = len(final_results.get('processed_pdfs', []))
    except Exception:
        all_pdfs_count = len(processed_pdfs)
    logger.info("\n" + "="*60)
    logger.info(f"📊 SUMMARY: PDF Note Generation")
    logger.info("="*60)
    logger.info(f"✅ Processed in this run: {len(processed_pdfs)} PDFs")
    logger.info(f"❌ Errors in this run: {len(errors)}")
    logger.info(f"📝 Total PDFs with notes: {all_pdfs_count}")
    logger.info(f"📄 Full results saved to: {RESULTS_FILE}")
    logger.info("="*60 + "\n")
    return processed_pdfs, errors
"""
Notebook PDF Note Generator CLI

This CLI scans PDFs from a resources folder, creates shareable links in OneDrive,
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
from notebook_automation.tools.utils.file_operations import get_vault_path_for_pdf, find_all_pdfs, get_scan_root
from notebook_automation.tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.tools.onedrive.onedrive_share import create_sharing_link as create_share_link
from notebook_automation.tools.pdf.processor import extract_pdf_text
from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_pdf
from notebook_automation.tools.ai.summarizer import generate_summary_with_openai
from notebook_automation.tools.utils.error_handling import update_failed_files, update_results_file, categorize_error, ErrorCategories
from notebook_automation.tools.metadata.path_metadata import infer_course_and_program
from notebook_automation.tools.ai.prompt_utils import format_final_user_prompt_for_pdf, format_chuncked_user_prompt_for_pdf

# Initialize loggers as global variables to be populated in main()
logger = None
failed_logger = None

# Constants
PDF_EXTENSIONS = {'.pdf'}
RESULTS_FILE = 'pdf_notes_results.json'
FAILED_FILES_JSON = 'failed_PDF_files.json'


def _record_failed_file(failed_data):
    """Record a failed file to the failed_files.json and log."""
    try:
        failed_logger.error(f"Failed: {failed_data['file']} - {failed_data['error']}")
        try:
            with open(FAILED_FILES_JSON, 'r') as f:
                failed_files = json.load(f)
        except Exception:
            failed_files = {"failed_pdfs": []}
        if 'category' not in failed_data:
            failed_data['category'] = categorize_error(failed_data['error'])
        failed_files["failed_pdfs"].append(failed_data)
        with open(FAILED_FILES_JSON, 'w') as f:
            json.dump(failed_files, f, indent=2)
    except Exception as e:
        logger.error(f"Error recording failed file: {e}")


def _process_single_pdf(pdf_path, vault_dir, args=None, access_token=None, dry_run=False):
    """
    Process a single PDF file to create a markdown note with OneDrive sharing link.
    Args:
        pdf_path (Path): Path to the PDF file
        vault_dir (Path): Directory in the vault to create the note in
        args (Namespace): Command-line arguments
        access_token (str): Microsoft Graph API access token
        dry_run (bool): If True, don't actually write the file
    Returns:
        dict: Result of the processing
    """
    try:
        pdf_stem = pdf_path.stem
        rel_path = pdf_path.relative_to(ONEDRIVE_LOCAL_RESOURCES_ROOT)
        note_name = f"{pdf_stem}-Notes.md"
        os.makedirs(vault_dir, exist_ok=True)
        note_path = vault_dir / note_name
        if note_path.exists() and not (args and getattr(args, 'force', False)):
            logger.info(f"Skipping {pdf_stem}: note already exists and --force not set.")
            logger.info(f"⏭️  Skipping: {pdf_stem}")
            logger.info(f"    Note already exists at: {note_path.name}")
            logger.info(f"    Use --force to overwrite existing notes")
            return {
                'file': str(rel_path),
                'note_path': str(note_path),
                'success': True,
                'skipped': True,
                'reason': 'already_exists',
                'modified_date': datetime.now().isoformat()
            }

        logger.info(f"  ├─ Processing: {pdf_stem}")

        # Step 1: Generate sharing link if requested and token available
        sharing_link = None
        embed_html = None
        if not getattr(args, 'no_share_links', False) and access_token:
            logger.info("  ├─ Generating OneDrive sharing and embed links...")
            onedrive_path = str(rel_path).replace('\\', '/')
            try:
                headers = {"Authorization": f"Bearer {access_token}"}
                try:
                    share_result = create_share_link(onedrive_path, headers, embed=True)
                except TypeError:
                    share_result = create_share_link(onedrive_path, headers)
                if isinstance(share_result, dict):
                    sharing_link = share_result.get('webUrl')
                    embed_html = share_result.get('embedHtml')
                else:
                    sharing_link = share_result
                if sharing_link:
                    logger.info(f"  │  └─ Shared link created ✓")
                else:
                    logger.info(f"  │  └─ Failed to create shared link ✗")
            except Exception as e:
                logger.warning(f"Failed to generate sharing link: {e}")
                logger.warning(f"  │  └─ Error generating sharing link: {str(e)}")
                sharing_link = None
                embed_html = None
        else:
            logger.info("  ├─ Skipping OneDrive sharing links (--no-share-links is set)")

        # Step 2: Extract PDF text
        logger.info(f"  ├─ Extracting PDF text...")
        pdf_text = extract_pdf_text(pdf_path)

        # Step 3: Infer course/program metadata
        logger.info(f"  ├─ Inferring course/program metadata...")
        course_program_metadata = infer_course_and_program(pdf_path)
        if course_program_metadata is None:
            course_program_metadata = {}
        if args and hasattr(args, 'force'):
            course_program_metadata['force'] = getattr(args, 'force', False)

        # Step 4: Generate summary with OpenAI (handled inside create_or_update_markdown_note_for_pdf)
        summary = None
        tags = []
        if not getattr(args, 'no_summary', False):
            logger.info(f"  ├─ Generating summary and tags with OpenAI (handled in note creation)...")
        else:
            logger.info("  ├─ Skipping summary generation (--no-summary is set)")

        logger.info("  ├─ Creating final markdown note...")
        include_embed = getattr(args, 'include_embed', True)
        embed_html_final = embed_html
        try:
            note_result = create_or_update_markdown_note_for_pdf(
                pdf_path=pdf_path,
                vault_dir=vault_dir,
                pdf_stem=pdf_stem,
                include_embed=include_embed,
                embed_html=embed_html_final,
                dry_run=dry_run,
                sharing_link=sharing_link,
                course_program_metadata=course_program_metadata
            )
            if not isinstance(note_result, dict):
                note_result = {
                    'note_path': str(note_result),
                    'success': True,
                    'error': None,
                    'tags': [],
                    'file': str(pdf_path)
                }
        except Exception as e:
            logger.error(f"Exception in create_or_update_markdown_note_for_pdf: {e}")
            error_message = traceback.format_exc()
            logger.error(error_message)
            note_result = {
                'note_path': None,
                'success': False,
                'error': str(e),
                'tags': [],
                'file': str(pdf_path)
            }
        if not isinstance(note_result, dict):
            note_result = {
                'note_path': str(note_result),
                'success': True,
                'error': None,
                'tags': [],
                'file': str(pdf_path)
            }
        logger.info(f"Created markdown note: {note_result.get('note_path')}")
        logger.info("  │  └─ Note created at: " + str(note_result.get('note_path', note_path)).replace(str(VAULT_LOCAL_ROOT), "").lstrip('/\\'))

        result = {
            'file': str(rel_path),
            'onedrive_path': str(pdf_path),
            'note_path': note_result.get('note_path'),
            'success': note_result.get('success', True),
            'error': note_result.get('error'),
            'tags': note_result.get('tags', []),
            'modified_date': datetime.now().isoformat()
        }
        if sharing_link:
            result['sharing_link'] = sharing_link
        update_results_file(RESULTS_FILE, result)
        logger.info("  └─ Results saved ✓")
        return result
    except Exception as e:
        error_msg = str(e)
        error_category = categorize_error(e)
        logger.error(f"Error processing {pdf_path.name}: {error_msg} (Category: {error_category})")
        logger.error(f"  └─ Error: {error_msg}")
        failed_data = {
            'file': str(pdf_path),
            'relative_path': str(rel_path) if 'rel_path' in locals() else None,
            'error': error_msg,
            'category': error_category,
            'timestamp': datetime.now().isoformat()
        }
        _record_failed_file(failed_data)
        return {
            'file': str(pdf_path),
            'success': False,
            'error': error_msg,
            'error_category': error_category,
            'timestamp': datetime.now().isoformat()
        }