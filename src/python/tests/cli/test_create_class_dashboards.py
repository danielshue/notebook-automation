# Test for create_class_dashboards.py
import os
import shutil
import tempfile
from pathlib import Path
from notebook_automation.cli.create_class_dashboards import main as dashboard_main

def test_create_class_dashboards(tmp_path):
    # Setup: create a fake vault with class folders
    vault = tmp_path / "vault"
    vault.mkdir()
    program = vault / "Program1"
    course = program / "CourseA"
    class_folder = course / "ClassX"
    class_folder.mkdir(parents=True)
    (class_folder / "class-index.md").write_text("# Class Index\n")
    # Patch sys.argv for CLI
    import sys
    sys_argv_orig = sys.argv
    sys.argv = ["create_class_dashboards.py", str(vault)]
    try:
        dashboard_main()
        dashboard_file = class_folder / "Class Dashboard.md"
        assert dashboard_file.exists()
        content = dashboard_file.read_text()
        assert "Program1" in content
        assert "CourseA" in content
        assert "ClassX" in content
    finally:
        sys.argv = sys_argv_orig

def run():
    import pytest
    pytest.main([__file__])

if __name__ == "__main__":
    run()
