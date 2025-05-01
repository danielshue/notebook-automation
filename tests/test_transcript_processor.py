#!/usr/bin/env python3
"""
Tests for the transcript processor module functionality.

This module contains tests for the transcript finding functionality in the
tools.transcript.processor module, particularly the find_transcript_file function.
It creates various mock directory structures with video and transcript files to test
different search strategies and fallback mechanisms.
"""

import os
import shutil
import tempfile
import unittest
from pathlib import Path
import logging

# Configure logging to avoid interference with test output
logging.basicConfig(level=logging.WARNING)

# Import the module to test
from tools.transcript.processor import find_transcript_file, get_transcript_content
from tools.utils.config import ONEDRIVE_LOCAL_RESOURCES_ROOT, VAULT_LOCAL_ROOT


class TestTranscriptProcessor(unittest.TestCase):
    """Test suite for the transcript processor module."""

    def setUp(self):
        """Set up temporary test directories and files before each test."""
        # Create temporary directories to simulate OneDrive and Vault structures
        self.temp_dir = tempfile.mkdtemp()
        
        # Create mock OneDrive structure
        self.mock_onedrive_root = Path(self.temp_dir) / "onedrive"
        self.mock_onedrive_root.mkdir(exist_ok=True)
        
        # Create mock Vault structure
        self.mock_vault_root = Path(self.temp_dir) / "vault"
        self.mock_vault_root.mkdir(exist_ok=True)
        
        # Create a test education path that mimics the MBA-Resources structure
        self.edu_path = self.mock_onedrive_root / "Education" / "MBA-Resources" / "Managerial Economics" / "Course1"
        self.edu_path.mkdir(parents=True, exist_ok=True)
        
        # Create a simple course directory structure with a video
        self.course_path = self.mock_onedrive_root / "Courses" / "Finance101" / "Module1"
        self.course_path.mkdir(parents=True, exist_ok=True)
        
        # Transcript directory within course
        self.transcript_dir = self.course_path / "Transcripts"
        self.transcript_dir.mkdir(parents=True, exist_ok=True)

        # Save original values to restore them later
        self.original_onedrive_root = ONEDRIVE_LOCAL_RESOURCES_ROOT
        self.original_vault_root = VAULT_LOCAL_ROOT

    def tearDown(self):
        """Clean up temporary files and directories after each test."""
        # Remove the temporary directory and all its contents
        shutil.rmtree(self.temp_dir)

    def create_test_video_file(self, directory, filename="test_video.mp4"):
        """Create a mock video file in the specified directory."""
        video_path = directory / filename
        with open(video_path, 'w') as f:
            f.write("This is a mock video file content")
        return video_path
    
    def create_test_transcript_file(self, directory, filename, content="Test transcript content"):
        """Create a mock transcript file in the specified directory."""
        transcript_path = directory / filename
        with open(transcript_path, 'w') as f:
            f.write(content)
        return transcript_path

    def test_direct_match(self):
        """Test finding a transcript file with the exact same name as the video but with .txt extension."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create a transcript file with the same name but .txt extension
        transcript_path = self.create_test_transcript_file(self.course_path, "test_video.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find direct matching transcript")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_pattern_match(self):
        """Test finding a transcript file using various naming patterns."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create a transcript file with a pattern
        transcript_path = self.create_test_transcript_file(self.course_path, "test_video-Transcript.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find pattern matching transcript")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_transcript_subdirectory(self):
        """Test finding a transcript file in a Transcripts subdirectory."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create a transcript file in the Transcripts subdirectory
        transcript_path = self.create_test_transcript_file(self.transcript_dir, "test_video.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find transcript in subdirectory")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_markdown_transcript(self):
        """Test finding a markdown transcript file."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create a markdown transcript file
        transcript_path = self.create_test_transcript_file(self.course_path, "test_video.md")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find markdown transcript")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_spaces_in_filename(self):
        """Test finding a transcript for a video with spaces in its filename."""
        # Create a test video with spaces in name
        video_path = self.create_test_video_file(self.course_path, "test video with spaces.mp4")
        
        # Create a transcript file with the same spaces
        transcript_path = self.create_test_transcript_file(self.course_path, "test video with spaces.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find transcript with spaces in name")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_different_parent_dir(self):
        """Test finding a transcript in a parent directory."""
        # Create a subdirectory for the video
        video_subdir = self.course_path / "Videos"
        video_subdir.mkdir(exist_ok=True)
        
        # Create a test video in the subdirectory
        video_path = self.create_test_video_file(video_subdir)
        
        # Create a transcript file in the parent directory
        transcript_path = self.create_test_transcript_file(self.course_path, "test_video.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find transcript in parent directory")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_last_resort_only_txt(self):
        """Test finding the only txt file in the directory as a last resort."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path, "different_name.mp4")
        
        # Create a transcript file with an unrelated name (only txt in directory)
        transcript_path = self.create_test_transcript_file(self.course_path, "unrelated_transcript.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find only txt file as last resort")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_no_transcript_found(self):
        """Test case where no transcript can be found."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create an unrelated txt file in another directory
        other_dir = self.mock_onedrive_root / "OtherDir"
        other_dir.mkdir(exist_ok=True)
        self.create_test_transcript_file(other_dir, "unrelated.txt")
        
        # Test finding the transcript (should fail)
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNone(found_path, "Should not find a transcript that doesn't exist")

    def test_mba_resources_path(self):
        """Test finding a transcript in the MBA-Resources directory structure."""
        # Create a test video in the educational path
        module_path = self.edu_path / "01_module-intro" / "01_lesson-intro"
        module_path.mkdir(parents=True, exist_ok=True)
        video_path = self.create_test_video_file(module_path, "learn-on-your-terms.mp4")
        
        # Create a transcript file in the same directory
        transcript_path = self.create_test_transcript_file(module_path, "learn-on-your-terms.txt")
        
        # Test finding the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find transcript in MBA-Resources structure")
        self.assertEqual(str(found_path), str(transcript_path), 
                         f"Expected {transcript_path}, got {found_path}")

    def test_integration_get_transcript_content(self):
        """Test integration between find_transcript_file and get_transcript_content."""
        # Create a test video
        video_path = self.create_test_video_file(self.course_path)
        
        # Create a transcript file with sample content
        sample_content = """
        [00:01:15] This is a sample transcript.
        [00:02:30] It has timestamps that should be removed.
        Speaker 1: This is spoken text by speaker 1.
        Speaker 2: This is a response by speaker 2.
        """
        transcript_path = self.create_test_transcript_file(self.course_path, "test_video.txt", sample_content)
        
        # Find the transcript
        found_path = find_transcript_file(video_path, self.mock_onedrive_root)
        self.assertIsNotNone(found_path, "Failed to find transcript")
        
        # Get and verify the content
        content = get_transcript_content(found_path)
        self.assertIsNotNone(content, "Failed to get transcript content")
        
        # Verify timestamps and speaker labels are removed
        self.assertNotIn("[00:01:15]", content, "Timestamps should be removed")
        self.assertNotIn("Speaker 1:", content, "Speaker labels should be removed")
        
        # Verify content is preserved
        self.assertIn("This is a sample transcript", content, "Content should be preserved")
        self.assertIn("This is spoken text", content, "Content should be preserved")


if __name__ == '__main__':
    unittest.main()
