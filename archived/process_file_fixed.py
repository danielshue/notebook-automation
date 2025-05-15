    def process_file(self, filepath: Path) -> Dict[str, Any]:
        """
        Process a single markdown file.
        
        Args:
            filepath (Path): Path to the markdown file
            
        Returns:
            dict: Statistics about the processing of this file
        """
        file_stats = {
            'file': str(filepath),
            'tags_added': 0,
            'modified': False,
            'error': None
        }
        
        try:
            # Read the file content
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
                
            # Extract frontmatter
            frontmatter, frontmatter_text, remaining_content = self.extract_frontmatter(content)
            
            if frontmatter is None:
                file_stats['error'] = "No valid YAML frontmatter found"
                return file_stats
            
            # Special handling for index files - clear any tags
            if 'index-type' in frontmatter:
                # Initialize flag for tracking index file tag clearing
                file_stats['index_cleared'] = False
                
                # If it's an index file, check if it has tags that need to be cleared
                if 'tags' in frontmatter and frontmatter['tags']:
                    # Flag that we found and will remove tags
                    file_stats['modified'] = True
                    file_stats['index_cleared'] = True
                    
                    # Clear the tags
                    frontmatter['tags'] = []
                    
                    if not self.dry_run:
                        # Convert updated frontmatter back to YAML
                        if USE_RUAMEL:
                            from io import StringIO
                            string_stream = StringIO()
                            yaml_parser.dump(frontmatter, string_stream)
                            updated_yaml = string_stream.getvalue()
                        else:
                            updated_yaml = pyyaml.safe_dump(
                                frontmatter,
                                default_flow_style=False,
                                allow_unicode=True
                            )
                        
                        updated_content = f"---\n{updated_yaml}---\n{remaining_content}"
                        
                        # Write the updated content back to the file
                        with open(filepath, 'w', encoding='utf-8') as f:
                            f.write(updated_content)
                    
                    if self.verbose:
                        print(f"\nFile: {filepath}")
                        print(f"Cleared tags from index file")
                
                return file_stats  # Skip further processing for index files
                
            # Generate nested tags from frontmatter fields
            new_tags = self.generate_nested_tags(frontmatter)
            
            # Update frontmatter with new tags
            updated_frontmatter, tags_added = self.update_frontmatter(frontmatter, new_tags)
            file_stats['tags_added'] = tags_added
            
            # If tags were added, update the file
            if tags_added > 0:
                file_stats['modified'] = True
                
                if not self.dry_run:
                    # Convert updated frontmatter back to YAML
                    if USE_RUAMEL:
                        from io import StringIO
                        string_stream = StringIO()
                        yaml_parser.dump(updated_frontmatter, string_stream)
                        updated_yaml = string_stream.getvalue()
                    else:
                        updated_yaml = pyyaml.safe_dump(
                            updated_frontmatter, 
                            default_flow_style=False,
                            allow_unicode=True
                        )
                    
                    updated_content = f"---\n{updated_yaml}---\n{remaining_content}"
                    
                    # Write the updated content back to the file
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(updated_content)
                
                if self.verbose:
                    print(f"\nFile: {filepath}")
                    print(f"Added {tags_added} new tags:")
                    for tag in sorted(new_tags - set(updated_frontmatter.get('tags', []))):
                        print(f"  {tag}")
            
        except Exception as e:
            file_stats['error'] = str(e)
            logger.error(f"Error processing file {filepath}: {e}")
            
        return file_stats
