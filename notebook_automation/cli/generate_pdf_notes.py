"""
Notebook PDF Note Generator CLI.

This CLI scans PDFs from a resources folder, creates shareable links in OneDrive,
and generates corresponding reference notes (markdown file) in the Obsidian vault.
Generalized for any program or course structure.

Usage:
    python -m notebook_automation.cli.generate_pdf_notes [options]

Examples:
    # Process all PDFs in a folder
    python -m notebook_automation.cli.generate_pdf_notes --folder "Value Chain Management/Operations Management/"
    
    # Process a single file
    python -m notebook_automation.cli.generate_pdf_notes --single-file "Value Chain Management/Operations Management/lecture5.pdf"
    
    # Retry previously failed files
    python -m notebook_automation.cli.generate_pdf_notes --retry-failed
"""

# === IMPORTS ===
import os
import sys
import json
import logging
import argparse
import traceback
import subprocess
import statistics
from pathlib import Path
from datetime import datetime, timedelta
from typing import Dict, List, Tuple, Optional, Union, Any

# Import Rich components for better console rendering
from rich.console import Console
from rich.progress import Progress, TextColumn, BarColumn, TaskProgressColumn, TimeRemainingColumn
from rich.live import Live
from rich.logging import RichHandler
from rich.panel import Panel
from rich.table import Table
from rich.layout import Layout

# Suppress logger output from external libraries
logging.getLogger('pdfplumber').setLevel(logging.ERROR)
logging.getLogger('PyPDF2').setLevel(logging.ERROR)
logging.getLogger('openai').setLevel(logging.ERROR)

# Import shared CLI utilities
from notebook_automation.cli.utils import (
    HEADER, OKBLUE, OKCYAN, OKGREEN, WARNING, FAIL, ENDC, BOLD, GREY, BG_BLUE,
    remove_timestamps_from_logger
)

# Import from the tools package
from notebook_automation.tools.utils.config import setup_logging, VAULT_LOCAL_ROOT, ONEDRIVE_LOCAL_RESOURCES_ROOT
from notebook_automation.tools.utils.file_operations import get_vault_path_for_pdf, find_all_pdfs, get_scan_root
from notebook_automation.tools.auth.microsoft_auth import authenticate_graph_api
from notebook_automation.tools.pdf.processor import extract_pdf_text
from notebook_automation.tools.notes.note_markdown_generator import create_or_update_markdown_note_for_pdf
from notebook_automation.tools.utils.error_handling import (
    update_failed_files, update_results_file, categorize_error, ErrorCategories
)
from notebook_automation.tools.metadata.path_metadata import infer_course_and_program
from notebook_automation.tools.ai.prompt_utils import (
    format_final_user_prompt_for_pdf, format_chuncked_user_prompt_for_pdf
)

# Initialize loggers as global variables to be populated in main()
logger = None
failed_logger = None

# Initialize Rich console for enhanced terminal output
console = Console()

# Helper function for statistics collection and reporting
def create_statistics_panel(processed: int, total: int, processing_times: List[float], 
                          success_count: int, start_time_overall: datetime,
                          current_file: str = "") -> Panel:
    """Create a panel with statistics about the PDF processing.
    
    Args:
        processed (int): Number of processed PDFs.
        total (int): Total number of PDFs.
        processing_times (list): List of processing times for each PDF.
        success_count (int): Number of successfully processed PDFs.
        start_time_overall (datetime): Start time of the overall processing.
        current_file (str, optional): Name of the current file being processed. Defaults to "".
        
    Returns:
        Panel: A Rich panel containing the statistics.
    """
    table = Table(box=None)
    table.add_column("Statistic", style="cyan")
    table.add_column("Value", style="green")

    # Calculate basic statistics
    table.add_row("Total PDFs", f"{total}")
    table.add_row("Processed", f"{processed}/{total} ({round(processed/total*100 if total else 0, 1)}%)")
    table.add_row("Remaining", f"{total - processed}")
    table.add_row("Success Rate", f"{success_count}/{processed} ({round(success_count/processed*100 if processed else 0, 1)}%)")

    # Time statistics
    elapsed_time = datetime.now() - start_time_overall
    elapsed_str = str(timedelta(seconds=int(elapsed_time.total_seconds())))
    table.add_row("Elapsed Time", elapsed_str)

    # Process time statistics if we have data
    if processing_times:
        avg_time = statistics.mean(processing_times)
        table.add_row("Average Processing Time", f"{avg_time:.2f}s")
        
        try:
            median_time = statistics.median(processing_times)
            table.add_row("Median Processing Time", f"{median_time:.2f}s")
        except statistics.StatisticsError:
            # Handle case with too few data points
            pass
        
        # Calculate estimated time remaining
        if processed > 0 and total > processed:
            remaining_files = total - processed
            estimated_remaining_seconds = avg_time * remaining_files
            estimated_time = timedelta(seconds=int(estimated_remaining_seconds))
            table.add_row("Est. Time Remaining", str(estimated_time))
    
    # Show current file if provided
    if current_file:
        table.add_row("Current File", current_file)

    return Panel(table, title="PDF Processing Statistics", border_style="blue")


# Constants
PDF_EXTENSIONS = {'.pdf'}
RESULTS_FILE = 'pdf_notes_results.json'
FAILED_FILES_JSON = 'failed_PDF_files.json'


# === HELPER FUNCTIONS ===
def create_share_link(relative_path: str, headers: Dict[str, str], embed: bool = False) -> Union[Dict[str, str], str]:
    """Create a shareable OneDrive link by calling the vault-onedrive-share CLI tool.
    
    Uses the vault-onedrive-share command-line tool to generate shareable links for
    OneDrive files. The function passes authentication headers to the CLI tool and
    handles the output processing.
    
    Args:
        relative_path: Path relative to OneDrive root
        headers: Headers containing authorization token
        embed: Whether to generate embed HTML. Defaults to False.
        
    Returns:
        Sharing information with webUrl and optional embedHtml, or just URL string
        
    Raises:
        ValueError: If no authorization token is provided
        RuntimeError: If the CLI command fails or other errors occur
    """
    try:
        cmd = ["vault-onedrive-share", relative_path]
        if embed:
            cmd.append("--embed")
        
        # Extract the authorization token from headers
        auth_token = headers.get("Authorization", "").replace("Bearer ", "")
        if not auth_token:
            raise ValueError("No authorization token provided")
            
        # Pass the token via environment variables
        env = os.environ.copy()
        env["MS_AUTH_TOKEN"] = auth_token
        
        # Run the CLI command
        result = subprocess.run(
            cmd,
            env=env,
            capture_output=True,
            text=True,
            check=True
        )
        
        # Parse the output as JSON if possible
        try:
            return json.loads(result.stdout)
        except json.JSONDecodeError:
            # Fall back to plain text output
            return result.stdout.strip()
                
    except subprocess.CalledProcessError as e:
        raise RuntimeError(f"OneDrive share command failed: {e.stderr}")
    except Exception as e:
        raise RuntimeError(f"Failed to create share link: {str(e)}")


def _record_failed_file(failed_data: Dict[str, Any]) -> None:
    """Record a failed file to the failed_files.json and log.
    
    Records the data about failed file processing attempts to both the log
    and the failed_files.json tracking file for potential retry.
    
    Args:
        failed_data: Data about the failed file, including:
            - file: Path to the failed file
            - error: Error message
            - category: Error category (if available)
            - timestamp: When the error occurred
    """
    try:
        failed_logger.error(f"Failed: {failed_data['file']} - {failed_data['error']}")
        
        # Load existing failed files data or create new structure
        try:
            with open(FAILED_FILES_JSON, 'r') as f:
                failed_files = json.load(f)
        except (FileNotFoundError, json.JSONDecodeError):
            failed_files = {"failed_pdfs": []}
            
        # Ensure we have an error category
        if 'category' not in failed_data:
            failed_data['category'] = categorize_error(failed_data['error'])
            
        # Add to failed files list
        failed_files["failed_pdfs"].append(failed_data)
        
        # Write updated data
        with open(FAILED_FILES_JSON, 'w') as f:
            json.dump(failed_files, f, indent=2)
            
    except Exception as e:
        logger.error(f"Error recording failed file: {e}")


def _process_single_pdf(pdf_path: Path, vault_dir: Path, args: Any = None, 
                       access_token: Optional[str] = None, dry_run: bool = False) -> Dict[str, Any]:
    """Process a single PDF file to create a markdown note with OneDrive sharing link.
    
    Takes a PDF file, extracts relevant information, creates a shareable link (if not in dry run),
    and generates a markdown note with appropriate metadata in the Obsidian vault.
    
    Args:
        pdf_path: Path to the PDF file
        vault_dir: Directory in the vault to create the note in
        args: Command-line arguments
        access_token: Microsoft Graph API access token
        dry_run: If True, don't actually write the file
        
    Returns:
        Result of the processing containing:
            - file: Path to the processed file
            - note_path: Path to the created note
            - success: Whether processing was successful
            - and other metadata
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
            # Construct the output path from vault_dir and pdf_stem
            output_path = vault_dir / f"{pdf_stem}-Notes.md"
            
            # Don't actually create the file in dry run mode
            if dry_run:
                note_result = {
                    'note_path': str(output_path),
                    'success': True,
                    'error': None,
                    'tags': [],
                    'file': str(pdf_path)
                }
            else:
                # Call create_or_update_markdown_note_for_pdf with the correct parameters
                note_result = create_or_update_markdown_note_for_pdf(
                    pdf_path=str(pdf_path),
                    output_path=str(output_path),
                    pdf_link=sharing_link or "",
                    summary=None,  # Will be handled by the function or set to placeholder
                    metadata=course_program_metadata
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


def _parse_arguments() -> argparse.Namespace:
    """Parse command-line arguments for PDF note generation.
    
    Returns:
        Parsed command-line arguments
    """
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


def _process_single_file(file_path: str, args: Optional[argparse.Namespace] = None, 
                         access_token: Optional[str] = None) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """Process a single file specified by the --single-file argument.
    
    Args:
        file_path: Path to the file (relative to OneDrive base path)
        args: Command-line arguments
        access_token: Microsoft Graph API access token
        
    Returns:
        Lists of processed PDFs and errors
    """
    verbose = getattr(args, 'verbose', False)
    
    console.rule("[bold blue]🔍 SINGLE FILE PROCESSING")
    
    logger.info("\n" + "="*60)
    logger.info("🔍 SINGLE FILE PROCESSING")
    logger.info("="*60)
    
    file_path = Path(file_path)
    if not file_path.is_absolute():
        file_path = ONEDRIVE_LOCAL_RESOURCES_ROOT / file_path
    
    if not file_path.exists():
        error_msg = f"File not found: {file_path}"
        logger.error(error_msg)
        console.print(f"[bold red]❌ {error_msg}")
        return [], []
        
    if file_path.suffix.lower() not in PDF_EXTENSIONS:
        error_msg = f"File is not a PDF: {file_path}"
        logger.error(error_msg)
        console.print(f"[bold red]❌ {error_msg}")
        return [], []
        
    file_info = f"📄 Processing file: {file_path.name}"
    logger.info(file_info)
    console.print(f"[cyan]{file_info}")
    
    location_info = f"📂 Located at: {file_path}"
    logger.info(location_info)
    console.print(f"[blue]{location_info}")
    
    vault_dir = get_vault_path_for_pdf(file_path)
    save_info = f"📝 Will save note to: {vault_dir}"
    logger.info(save_info)
    console.print(f"[green]{save_info}")
    
    logger.info("\n" + "-"*50)
    console.rule()
    
    result = _process_single_pdf(file_path, vault_dir, args, access_token)
    
    if result.get('success', False):
        if result.get('skipped', False):
            console.print(f"[yellow]⏭️  Skipped (already exists): {file_path.name}")
        else:
            console.print(f"[green]✓ Processed: {file_path.name}")
        return [result], []
    else:
        console.print(f"[red]✗ Failed: {file_path.name} - {result.get('error', 'Unknown error')}")
        return [], [result]


def _retry_failed_files(args: Optional[argparse.Namespace] = None, 
                       access_token: Optional[str] = None) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """Retry processing files that previously failed.
    
    Args:
        args: Command-line arguments
        access_token: Microsoft Graph API access token
        
    Returns:
        Lists of processed PDFs and errors
    """
    verbose = getattr(args, 'verbose', False)
    
    # Console header with Rich formatting
    console.rule("[bold blue]🔄 RETRYING FAILED FILES")
    
    # Also log to file
    logger.info("\n" + "="*60)
    logger.info("🔄 RETRYING FAILED FILES")
    logger.info("="*60)
    
    try:
        with open(FAILED_FILES_JSON, 'r') as f:
            failed_files = json.load(f)
    except Exception as e:
        error_msg = f"Error loading failed files: {e}"
        logger.error(error_msg)
        logger.info(f"❌ {error_msg}")
        console.print(f"[bold red]❌ {error_msg}")
        return [], []
        
    failed_pdfs = failed_files.get("failed_pdfs", [])
    if not failed_pdfs:
        success_msg = "✅ No failed files to retry."
        logger.info(success_msg)
        console.print(f"[bold green]{success_msg}")
        return [], []
        
    found_msg = f"🔄 Found {len(failed_pdfs)} previously failed files to retry..."
    logger.info(found_msg)
    console.print(f"[cyan]{found_msg}")
    
    # Display error categories
    categories = {}
    for f in failed_pdfs:
        cat = f.get('category', 'Unknown')
        if cat not in categories:
            categories[cat] = 0
        categories[cat] += 1
    
    logger.info("Error categories to retry:")
    console.print("[yellow]Error categories to retry:")
    
    for cat, count in categories.items():
        logger.info(f"  - {cat}: {count} files")
        console.print(f"[yellow]  - {cat}: [bold]{count}[/bold] files")
    
    processed = []
    errors = []
    
    # Create a Rich progress display for retrying failed files
    with Progress(
        TextColumn("[bold blue]{task.description}"),
        BarColumn(),
        TaskProgressColumn(),
        TextColumn("•"),
        TimeRemainingColumn(),
        console=console,
        transient=False
    ) as progress:
        retry_task = progress.add_task(f"[cyan]Retrying failed files", total=len(failed_pdfs))
        
        for index, failed in enumerate(failed_pdfs):
            file_path = failed.get('file')
            if not file_path:
                progress.update(retry_task, advance=1)
                continue
                
            try:
                pdf_path = Path(file_path)
                if not pdf_path.exists():
                    msg = f"Failed file no longer exists: {file_path}"
                    logger.warning(msg)
                    if verbose:
                        console.print(f"[yellow]⚠️  {msg}")
                    progress.update(retry_task, advance=1)
                    continue
                    
                pdf_name = pdf_path.name
                progress.update(retry_task, description=f"[cyan]Retrying {index+1}/{len(failed_pdfs)}: {pdf_name}")
                logger.info(f"[{index+1}/{len(failed_pdfs)}] Retrying: {pdf_name}")
                
                vault_dir = get_vault_path_for_pdf(pdf_path)
                result = _process_single_pdf(pdf_path, vault_dir, args, access_token)
                
                if result.get('success', False):
                    if result.get('skipped', False):
                        status = "⏭️ SKIPPED"
                        if verbose:
                            console.print(f"[yellow]⏭️  Skipped (already exists): {pdf_name}")
                    else:
                        status = "✅ SUCCESS"
                        if verbose:
                            console.print(f"[green]✓ Retried: {pdf_name}")
                    processed.append(result)
                else:
                    status = "❌ FAILED"
                    if verbose:
                        console.print(f"[red]✗ Failed: {pdf_name} - {result.get('error', 'Unknown error')}")
                    errors.append(result)
                
                # Update progress
                progress.update(retry_task, advance=1)
                
            except Exception as e:
                logger.error(f"Error retrying failed file {file_path}: {e}")
                if verbose:
                    console.print(f"[red]✗ Error retrying failed file {file_path}: {e}")
                progress.update(retry_task, advance=1)
    
    # Update the failed files list
    if processed:
        try:
            still_failed = [f for f in failed_pdfs if f.get('file') not in [p.get('file') for p in processed]]
            failed_files["failed_pdfs"] = still_failed
            with open(FAILED_FILES_JSON, 'w') as f:
                json.dump(failed_files, f, indent=2)
        except Exception as e:
            logger.error(f"Error updating failed files list: {e}")
    
    # Display results
    console.print(f"\n[green]✅ Successfully processed {len(processed)} previously failed files")
    console.print(f"[red]❌ Still failed: {len(errors)} files")
    
    logger.info(f"\n✅ Successfully processed {len(processed)} previously failed files")
    logger.info(f"❌ Still failed: {len(errors)} files")
    
    return processed, errors


def _process_pdfs_for_notes(args: Optional[argparse.Namespace] = None, 
                           folder_path: Optional[str] = None,
                           access_token: Optional[str] = None, 
                           dry_run: bool = False) -> Tuple[List[Dict[str, Any]], List[Dict[str, Any]]]:
    """Process all PDFs found in the specified folder or OneDrive root.
    
    Args:
        args: Command-line arguments
        folder_path: Path to folder to process
        access_token: Microsoft Graph API access token
        dry_run: If True, don't actually write any files
        
    Returns:
        List of processed PDFs and list of errors
    """
    verbose = getattr(args, 'verbose', False)
    
    # Rich formatted header
    console.rule("[bold blue]🔍 PDF SCAN & PROCESSING")
    
    # File log header
    logger.info("\n" + "="*60)
    logger.info("🔍 PDF SCAN & PROCESSING")
    logger.info("="*60)
    
    scan_root = get_scan_root(folder_path)
    if scan_root is None:
        error_msg = "❌ Could not determine scan root directory"
        logger.info(error_msg)
        console.print(f"[bold red]{error_msg}")
        return [], []
    
    scan_info = f"📁 Scanning for PDFs under OneDrive at: {scan_root}"
    logger.info(f"📁 Scanning for PDFs under OneDrive at:")
    logger.info(f"  {scan_root}")
    console.print(f"[cyan]{scan_info}")
    
    with console.status("[bold cyan]Scanning for PDF files...") as status:
        start_time = datetime.now()
        pdf_paths = find_all_pdfs(scan_root)
        scan_duration = datetime.now() - start_time
    
    total_pdfs = len(pdf_paths)
    logger.info(f"Found {total_pdfs} PDF files in OneDrive at {scan_root}")
    
    if total_pdfs > 0:
        found_msg = f"✅ Found {total_pdfs} PDF files to process"
        time_msg = f"⏱️  Scan completed in {scan_duration.total_seconds():.1f} seconds"
        
        logger.info(f"\n{found_msg}")
        logger.info(time_msg)
        
        console.print(f"[green]{found_msg}")
        console.print(f"[blue]{time_msg}")
        
        # Preview files
        preview_count = min(5, total_pdfs)
        console.print("[cyan]📄 Files to process (sample):")
        logger.info("\n📄 Files to process (sample):")
        
        for i in range(preview_count):
            file_info = f"  {i+1}. {pdf_paths[i].name}"
            logger.info(file_info)
            console.print(f"[cyan]{file_info}")
            
        if total_pdfs > preview_count:
            remaining_msg = f"  ... and {total_pdfs - preview_count} more files"
            logger.info(f"{remaining_msg}\n")
            console.print(f"[cyan]{remaining_msg}")
    else:
        no_files_msg = "⚠️ No PDF files found in the specified location"
        logger.info(no_files_msg)
        console.print(f"[yellow]{no_files_msg}")
        
    if total_pdfs == 0:
        nothing_msg = "❗ No PDF files found. Nothing to process."
        logger.info(nothing_msg)
        console.print(f"[bold yellow]{nothing_msg}")
        return [], []
        
    processed_pdfs = []
    errors = []
    processing_times = []
    start_time_overall = datetime.now()
      # Function to create statistics panel
    def make_stats_panel(processed: int, total: int, times: List[float], current_file: str = "") -> Panel:
        # Calculate success count by counting successful results in processed_pdfs
        success_count = len([p for p in processed_pdfs if p.get('success', False)])
        
        avg_time = statistics.mean(times) if times else 0
        median_time = statistics.median(times) if len(times) >= 3 else avg_time
        remaining = total - processed
        est_time_left = timedelta(seconds=round(avg_time * remaining)) if avg_time > 0 else "Unknown"
        
        # Create table for statistics
        table = Table.grid(padding=(0, 1))
        table.add_column(style="cyan", justify="right")
        table.add_column(style="magenta")
        
        # Add statistics rows
        table.add_row("Total PDFs:", f"[bold]{total}")
        table.add_row("Processed:", f"[bold]{processed}")
        table.add_row("Remaining:", f"[bold]{remaining}")
        success_rate = round(100 * (success_count / max(1, processed)), 1) if processed > 0 else 0
        table.add_row("Success rate:", f"[bold green]{success_rate}%")
        
        if processing_times:
            table.add_row("Avg processing time:", f"[bold]{round(avg_time, 2)} sec")
            table.add_row("Median time:", f"[bold]{round(median_time, 2)} sec")
        
        table.add_row("Est. time remaining:", f"[bold]{est_time_left}")
        
        if current_file:
            table.add_row("Currently processing:", f"[bold blue]{current_file}")
            
        now = datetime.now()
        elapsed = now - start_time_overall
        finish_time = now + timedelta(seconds=round(avg_time * remaining)) if avg_time > 0 else "Unknown"
        
        if isinstance(finish_time, datetime):
            finish_str = finish_time.strftime("%H:%M:%S")
        else:
            finish_str = str(finish_time)
            
        # Add timing information
        table.add_row("Elapsed time:", f"[bold]{elapsed}")
        table.add_row("Est. finish time:", f"[bold]{finish_str}")
        
        # Create panel with table inside
        return Panel(
            table,
            title="[bold blue]PDF Processing Statistics",
            border_style="blue",
            padding=(1, 2),
        )
    
    # Create a Rich progress display with statistics panel
    layout = Layout()
    layout.split_column(
        Layout(name="stats"),
        Layout(name="progress")
    )
    
    with Live(layout, console=console, refresh_per_second=4) as live:
        with Progress(
            TextColumn("[bold blue]{task.description}"),
            BarColumn(),
            TaskProgressColumn(),
            TextColumn("•"),
            TimeRemainingColumn(),
            console=None,  # We'll use our own display
            transient=False,
        ) as progress:
            task_id = progress.add_task(f"[cyan]Processing PDF files", total=total_pdfs)
            
            # Initial statistics display
            layout["stats"].update(make_stats_panel(0, total_pdfs, []))
            layout["progress"].update(progress)
        
            for index, pdf_path in enumerate(pdf_paths):
                pdf_name = pdf_path.name
                file_start_time = datetime.now()
                
                # Update displays
                progress.update(task_id, description=f"[cyan]Processing {index+1}/{total_pdfs}: {pdf_name}")
                layout["stats"].update(make_stats_panel(index, total_pdfs, processing_times, pdf_name))
                layout["progress"].update(progress)
                
                logger.info(f"[{index+1}/{total_pdfs}] Processing: {pdf_name}")
                
                vault_dir = get_vault_path_for_pdf(pdf_path)
                result = _process_single_pdf(pdf_path, vault_dir, args, access_token, dry_run)
                
                # Calculate and record processing time
                processing_time = (datetime.now() - file_start_time).total_seconds()
                processing_times.append(processing_time)
                
                # Update progress with status indicators
                if result.get('success', False):
                    if result.get('skipped', False):
                        status = "⏭️ SKIPPED"
                        if verbose:
                            console.print(f"[yellow]⏭️  Skipped (already exists): {pdf_name}")
                    else:
                        status = "✅ SUCCESS"
                        if verbose:
                            console.print(f"[green]✓ Processed: {pdf_name} ({processing_time:.2f}s)")
                    processed_pdfs.append(result)
                else:
                    status = "❌ FAILED"
                    if verbose:
                        console.print(f"[red]✗ Failed: {pdf_name} - {result.get('error', 'Unknown error')}")
                    errors.append(result)
                
                # Update progress and statistics
                progress.update(task_id, advance=1)
                layout["stats"].update(make_stats_panel(index + 1, total_pdfs, processing_times))
                layout["progress"].update(progress)
                logger.info("")
                
            # Final statistics update
            layout["stats"].update(make_stats_panel(total_pdfs, total_pdfs, processing_times))
            layout["progress"].update(progress)
            
    try:
        with open(RESULTS_FILE, 'r') as f:
            all_results = json.load(f)
            all_pdfs_count = len(all_results)
    except Exception:
        all_pdfs_count = len(processed_pdfs)
        
    # Display a Rich formatted summary
    console.rule("[bold blue]📊 SUMMARY: PDF Note Generation")
    console.print(f"[green]✅ Processed in this run: {len(processed_pdfs)} PDFs")
    console.print(f"[red]❌ Errors in this run: {len(errors)}")
    console.print(f"[cyan]📝 Total PDFs with notes: {all_pdfs_count}")
    console.print(f"[blue]📄 Full results saved to: {RESULTS_FILE}")
    console.rule()
    
    # Also log to file
    logger.info("\n" + "="*60)
    logger.info(f"📊 SUMMARY: PDF Note Generation")
    logger.info("="*60)
    logger.info(f"✅ Processed in this run: {len(processed_pdfs)} PDFs")
    logger.info(f"❌ Errors in this run: {len(errors)}")
    logger.info(f"📝 Total PDFs with notes: {all_pdfs_count}")
    logger.info(f"📄 Full results saved to: {RESULTS_FILE}")
    logger.info("="*60 + "\n")
    
    return processed_pdfs, errors


# === CLI ENTRYPOINT ===
def main() -> int:
    """Main entry point for the PDF note generation CLI.
    
    Returns:
        Exit code (0 for success, non-zero for error)
    """
    global logger, failed_logger
    args = _parse_arguments()

    # Setup logging
    logger, failed_logger = setup_logging(
        debug=args.debug,
        log_file="generate_pdf_notes.log",
        failed_log_file="generate_pdf_notes_failed_files.log",
        console_output=True,
        use_rich=True
    )
    
    # Remove timestamps from logger for cleaner CLI output (regardless of debug mode)
    remove_timestamps_from_logger(logger)
    remove_timestamps_from_logger(failed_logger)

    # Display Rich header
    console.print(f"[bold blue]{HEADER}")
    console.print("[bold cyan]PDF Note Generation CLI started.")
    
    # Also log to file
    logger.info(HEADER)
    logger.info("PDF Note Generation CLI started.")

    # Authenticate if needed (unless --no-share-links is set)
    access_token = None
    if not getattr(args, 'no_share_links', False):
        auth_msg = "Authenticating with Microsoft Graph API..."
        logger.info(auth_msg)
        console.print(f"[cyan]{auth_msg}")
        
        access_token = authenticate_graph_api(force_refresh=args.refresh_auth)
        if not access_token:
            error_msg = "Failed to authenticate with Microsoft Graph API."
            logger.error(error_msg)
            console.print(f"[bold red]❌ {error_msg}")
            return 1

    processed = []
    errors = []

    # Dispatch based on arguments
    if getattr(args, 'retry_failed', False):
        mode_msg = "Retrying previously failed files..."
        logger.info(mode_msg)
        console.print(f"[cyan]{mode_msg}")
        processed, errors = _retry_failed_files(args=args, access_token=access_token)
    elif getattr(args, 'single_file', None):
        mode_msg = f"Processing single file: {args.single_file}"
        logger.info(mode_msg)
        console.print(f"[cyan]{mode_msg}")
        processed, errors = _process_single_file(args.single_file, args=args, access_token=access_token)
    else:
        folder = getattr(args, 'folder', None)
        mode_msg = f"Processing folder: {folder if folder else '[default OneDrive root]'}"
        logger.info(mode_msg)
        console.print(f"[cyan]{mode_msg}")
        processed, errors = _process_pdfs_for_notes(args=args, folder_path=folder, access_token=access_token, dry_run=args.dry_run)

    # Print Rich formatted summary to console
    console.rule("[bold cyan]PDF Note Generation Summary")
    console.print(f"[green]✓ Processed: {len(processed)} PDFs")
    console.print(f"[red]✗ Errors: {len(errors)}")
    
    if errors and args.verbose:
        console.print("\n[bold red]Failed files:")
        for err in errors:
            console.print(f"  [red]• {err.get('file')}: {err.get('error')}")
    
    console.print(f"\n[blue]📄 Results saved to: [bold]{RESULTS_FILE}")
    console.rule()
    return 0


# Allow running as a script directly
if __name__ == "__main__":
    sys.exit(main())
