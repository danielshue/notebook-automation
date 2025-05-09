    def process_directory(self, directory: Path) -> Dict[str, int]:
        """
        Recursively process all markdown files in a directory.
        
        Args:
            directory (Path): Directory to process
            
        Returns:
            dict: Statistics about the processing
        """
        # Reset statistics
        self.stats = {
            'files_processed': 0,
            'files_modified': 0,
            'files_with_errors': 0,
            'tags_added': 0,
            'index_files_cleared': 0
        }
        
        # Walk through the directory recursively
        for root, _, files in os.walk(directory):
            for file in files:
                if file.lower().endswith('.md'):
                    filepath = Path(root) / file
                    
                    file_stats = self.process_file(filepath)
                    
                    # Update overall statistics
                    self.stats['files_processed'] += 1
                    if file_stats['modified']:
                        self.stats['files_modified'] += 1
                    if file_stats['error']:
                        self.stats['files_with_errors'] += 1
                        print(f"Error processing file {filepath}: {file_stats['error']}")
                    # Track index files that had their tags cleared
                    if file_stats.get('index_cleared', False):
                        self.stats['index_files_cleared'] += 1
                    else:
                        self.stats['tags_added'] += file_stats['tags_added']
        
        return self.stats
