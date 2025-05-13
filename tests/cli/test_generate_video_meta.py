"""
Unit tests for generate_video_meta.py CLI.
"""
import sys
import types
import pytest
from pathlib import Path
from unittest.mock import patch, MagicMock

# Patch sys.argv for CLI entry
@patch("notebook_automation.cli.generate_video_meta.create_share_link")
@patch("notebook_automation.cli.generate_video_meta.create_or_update_markdown_note_for_video")
@patch("notebook_automation.cli.generate_video_meta.find_files_by_extension")
@patch("notebook_automation.cli.generate_video_meta.process_transcript")
@patch("notebook_automation.cli.generate_video_meta.generate_summary_with_openai")
@patch("notebook_automation.cli.generate_video_meta.extract_metadata_from_path")
@patch("notebook_automation.cli.generate_video_meta.setup_logging")
def test_cli_main_success(
    mock_setup_logging,
    mock_extract_metadata,
    mock_generate_summary,
    mock_process_transcript,
    mock_find_files,
    mock_create_note,
    mock_create_share_link,
    monkeypatch
):
    # Arrange
    from notebook_automation.cli import generate_video_meta
    test_video = Path("/MBA-Resources/Value Chain Management/Marketing Management/test.mp4")
    mock_find_files.return_value = [test_video]
    mock_extract_metadata.return_value = {"title": "Test Video"}
    mock_process_transcript.return_value = "Transcript text"
    mock_generate_summary.return_value = "Summary text"
    mock_create_share_link.return_value = "https://share.link/test"
    mock_create_note.return_value = None
    mock_logger = MagicMock()
    mock_setup_logging.return_value = (mock_logger, mock_logger)

    # Patch sys.argv
    monkeypatch.setattr(sys, "argv", [
        "generate_video_meta.py",
        "--folder", "Marketing Management",
        "--resources-root", "/MBA-Resources/Value Chain Management"
    ])

    # Act
    generate_video_meta.main()

    # Assert
    mock_find_files.assert_called()
    mock_create_share_link.assert_called_with(test_video, timeout=15)
    mock_create_note.assert_called()
    mock_logger.info.assert_any_call("Note created/updated for test.mp4")

@patch("notebook_automation.cli.generate_video_meta.create_share_link")
@patch("notebook_automation.cli.generate_video_meta.create_or_update_markdown_note_for_video")
@patch("notebook_automation.cli.generate_video_meta.find_files_by_extension")
def test_cli_handles_no_videos(
    mock_find_files,
    mock_create_note,
    mock_create_share_link,
    monkeypatch
):
    from notebook_automation.cli import generate_video_meta
    mock_find_files.return_value = []
    monkeypatch.setattr(sys, "argv", [
        "generate_video_meta.py",
        "--folder", "Marketing Management",
        "--resources-root", "/MBA-Resources/Value Chain Management"
    ])
    # Should not raise
    generate_video_meta.main()
    mock_create_note.assert_not_called()
    mock_create_share_link.assert_not_called()

@patch("notebook_automation.cli.generate_video_meta.create_share_link", side_effect=Exception("Share error"))
@patch("notebook_automation.cli.generate_video_meta.create_or_update_markdown_note_for_video")
@patch("notebook_automation.cli.generate_video_meta.find_files_by_extension")
def test_cli_share_link_error(
    mock_find_files,
    mock_create_note,
    mock_create_share_link,
    monkeypatch
):
    from notebook_automation.cli import generate_video_meta
    test_video = Path("/MBA-Resources/Value Chain Management/Marketing Management/test.mp4")
    mock_find_files.return_value = [test_video]
    monkeypatch.setattr(sys, "argv", [
        "generate_video_meta.py",
        "--folder", "Marketing Management",
        "--resources-root", "/MBA-Resources/Value Chain Management"
    ])
    # Should not raise
    generate_video_meta.main()
    mock_create_note.assert_called()
