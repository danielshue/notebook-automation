"""
Integration test for the generate_video_meta CLI.

This test runs the CLI in dry-run mode on a test fixture directory and verifies that all videos are processed.
"""
import subprocess
from pathlib import Path
import sys

import pytest

CLI_PATH = Path("notebook_automation/cli/generate_video_meta.py")
FIXTURE_FOLDER = Path("course1/class1/module1")

@pytest.mark.integration
def test_generate_video_meta_cli_dry_run():
    """Test the generate_video_meta CLI in dry-run mode on a fixture folder."""
    # Build the command
    cmd = [
        sys.executable,
        str(CLI_PATH),
        "--folder", str(FIXTURE_FOLDER),
        "--resources-root", str(Path("tests/fixtures/onedrive_resources")),
        "--dry-run",
        "--verbose"
    ]
    # Run the CLI as a subprocess
    result = subprocess.run(cmd, capture_output=True, text=True)
    # Combine stdout and stderr for assertions (logs may go to either)
    output = result.stdout + result.stderr
    # Check exit code
    assert result.returncode == 0, f"CLI failed: {output}"
    # Check output for expected video files
    assert "Processing video" in output
    assert "Dry run mode" in output
    # Optionally, check that both video1.mp4 and video2.mp4 are mentioned
    assert "video1.mp4" in output
    assert "video2.mp4" in output
