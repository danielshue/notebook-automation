"""
Unit tests for ensure_metadata.py CLI script.

These tests verify the correct behavior of the MetadataUpdater class and CLI entrypoint.

Tested features:
- Extraction and updating of YAML frontmatter
- Directory structure-based metadata inference
- File and directory processing
- CLI dry-run and inspect modes

Uses pytest and temporary directories/files for isolation.
"""
import os
import shutil
import tempfile
from pathlib import Path
import pytest

from notebook_automation.cli import ensure_metadata

@pytest.fixture
def temp_vault(tmp_path):
    """Create a temporary Obsidian vault structure for testing."""
    # Structure: vault/Program/Course/Class/file.md
    vault = tmp_path / "vault"
    program_dir = vault / "MBA" / "Accounting"
    class_dir = program_dir / "Class01"
    class_dir.mkdir(parents=True)
    # Create index files with frontmatter
    (vault / "main-index.md").write_text(
        """---\ntitle: MBA Vault\nindex-type: main-index\n---\n"""
    )
    (vault / "MBA" / "program-index.md").write_text(
        """---\ntitle: MBA\nindex-type: program-index\n---\n"""
    )
    (program_dir / "course-index.md").write_text(
        """---\ntitle: Accounting\nindex-type: course-index\n---\n"""
    )
    (class_dir / "class-index.md").write_text(
        """---\ntitle: Class01\nindex-type: class-index\n---\n"""
    )
    # Create a markdown file with incomplete frontmatter
    test_md = class_dir / "test.md"
    test_md.write_text(
        """---\ntitle: Test File\n---\n\nContent here."""
    )
    return vault

def test_extract_frontmatter():
    updater = ensure_metadata.MetadataUpdater()
    content = """---\ntitle: Test\nprogram: MBA\n---\nBody"""
    fm, fm_text, rest = updater.extract_frontmatter(content)
    assert fm["title"] == "Test"
    assert fm["program"] == "MBA"
    assert rest.strip() == "Body"

def test_update_frontmatter_adds_fields():
    updater = ensure_metadata.MetadataUpdater()
    fm = {"title": "Test"}
    meta = {"program": "MBA", "course": "Accounting", "class": "Class01"}
    updated, stats = updater.update_frontmatter(fm, meta)
    assert updated["program"] == "MBA"
    assert updated["course"] == "Accounting"
    assert updated["class"] == "Class01"
    assert stats["program_updated"]
    assert stats["course_updated"]
    assert stats["class_updated"]

def test_find_parent_index_info(temp_vault):
    # Patch VAULT_LOCAL_ROOT to temp_vault for correct relative path logic
    import importlib
    import notebook_automation.tools.utils.config as config_mod
    config_mod.VAULT_LOCAL_ROOT = str(temp_vault)
    importlib.reload(ensure_metadata)
    updater = ensure_metadata.MetadataUpdater()
    file_path = temp_vault / "MBA" / "Accounting" / "Class01" / "test.md"
    info = updater.find_parent_index_info(file_path)
    assert info["program"] == "MBA"
    assert info["course"] == "Accounting"
    assert info["class"] == "Class01"

def test_process_file_updates_metadata(temp_vault):
    updater = ensure_metadata.MetadataUpdater(dry_run=True)
    file_path = temp_vault / "MBA" / "Accounting" / "Class01" / "test.md"
    stats = updater.process_file(file_path)
    assert stats["modified"]
    assert stats["program_updated"]
    assert stats["course_updated"]
    assert stats["class_updated"]

def test_process_directory_counts_files(temp_vault):
    updater = ensure_metadata.MetadataUpdater(dry_run=True)
    stats = updater.process_directory(temp_vault)
    assert stats["files_processed"] >= 1
    assert stats["files_modified"] >= 1
    assert stats["program_updated"] >= 1
    assert stats["course_updated"] >= 1
    assert stats["class_updated"] >= 1

def test_inspect_file(temp_vault):
    # Patch VAULT_LOCAL_ROOT to temp_vault for correct relative path logic
    import importlib
    import notebook_automation.tools.utils.config as config_mod
    config_mod.VAULT_LOCAL_ROOT = str(temp_vault)
    importlib.reload(ensure_metadata)
    updater = ensure_metadata.MetadataUpdater()
    file_path = temp_vault / "MBA" / "Accounting" / "Class01" / "test.md"
    result = updater.inspect_file(file_path)
    assert result["file"].endswith("test.md")
    assert result["dir_program"] == "MBA"
    assert result["dir_course"] == "Accounting"
    assert result["dir_class"] == "Class01"
    assert result["program"] is None  # Not set in file frontmatter

def test_cli_dry_run(tmp_path, monkeypatch):
    # Simulate CLI call: python ensure_metadata.py <vault> --dry-run
    vault = tmp_path / "vault"
    (vault / "MBA" / "Accounting" / "Class01").mkdir(parents=True)
    (vault / "MBA" / "program-index.md").write_text(
        """---\ntitle: MBA\nindex-type: program-index\n---\n"""
    )
    (vault / "MBA" / "Accounting" / "course-index.md").write_text(
        """---\ntitle: Accounting\nindex-type: course-index\n---\n"""
    )
    (vault / "MBA" / "Accounting" / "Class01" / "class-index.md").write_text(
        """---\ntitle: Class01\nindex-type: class-index\n---\n"""
    )
    test_md = vault / "MBA" / "Accounting" / "Class01" / "test.md"
    test_md.write_text("""---\ntitle: Test\n---\n\nContent""")
    monkeypatch.setattr(ensure_metadata, "VAULT_LOCAL_ROOT", str(vault))
    import sys
    sys_argv = sys.argv
    sys.argv = ["ensure_metadata.py", str(vault), "--dry-run"]
    try:
        ensure_metadata.main()
    finally:
        sys.argv = sys_argv
