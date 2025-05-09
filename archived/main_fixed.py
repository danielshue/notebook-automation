def main():
    """Main entry point for the script."""
    # Parse command line arguments
    parser = argparse.ArgumentParser(
        description="Process markdown files to add nested tags based on frontmatter fields."
    )
    parser.add_argument(
        "directory", 
        nargs="?",
        help="Directory to recursively scan for markdown files (defaults to Obsidian vault)"
    )
    parser.add_argument(
        "--dry-run", 
        action="store_true", 
        help="Don't write changes to files, just simulate"
    )
    parser.add_argument(
        "--verbose", "-v", 
        action="store_true", 
        help="Print detailed information about changes"
    )
    
    args = parser.parse_args()
    
    # Use specified directory or default to VAULT_LOCAL_ROOT
    if args.directory:
        directory = Path(args.directory)
    else:
        directory = Path(normalize_wsl_path(VAULT_LOCAL_ROOT))
        logger.info(f"No directory specified, using Obsidian vault: {directory}")
    
    # Validate directory
    if not directory.exists() or not directory.is_dir():
        logger.error(f"Directory not found or is not a directory: {directory}")
        sys.exit(1)
    
    # Process the directory
    processor = MarkdownFrontmatterProcessor(
        dry_run=args.dry_run, 
        verbose=args.verbose
    )
    
    print(f"{'[DRY RUN] ' if args.dry_run else ''}Processing markdown files in: {directory}")
    stats = processor.process_directory(directory)
    
    # Print summary
    print("\nSummary:")
    print(f"Files processed: {stats['files_processed']}")
    print(f"Files modified: {stats['files_modified']}")
    print(f"Tags added: {stats['tags_added']}")
    print(f"Index files cleared: {stats['index_files_cleared']}")
    print(f"Files with errors: {stats['files_with_errors']}")
    
    if args.dry_run:
        print("\nThis was a dry run. No files were modified.")
