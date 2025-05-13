"""
Unit tests for onedrive_share.py OneDrive sharing logic.
"""
import pytest
from unittest.mock import patch, MagicMock

# Test authenticate_interactive returns a token
@patch("notebook_automation.cli.onedrive_share.msal.PublicClientApplication")
@patch("notebook_automation.cli.onedrive_share.os.path.exists", return_value=False)
def test_authenticate_interactive_success(mock_exists, mock_msal):
    from notebook_automation.cli.onedrive_share import authenticate_interactive
    mock_app = MagicMock()
    mock_msal.return_value = mock_app
    mock_app.get_accounts.return_value = []
    mock_app.acquire_token_interactive.return_value = {"access_token": "abc123"}
    token = authenticate_interactive()
    assert token == "abc123"

# Test create_sharing_link with a valid file path
@patch("notebook_automation.cli.onedrive_share.check_if_file_exists")
@patch("notebook_automation.cli.onedrive_share.requests.post")
def test_create_sharing_link_success(mock_post, mock_check):
    from notebook_automation.cli.onedrive_share import create_sharing_link
    mock_check.return_value = {"id": "fileid", "name": "file.mp4"}
    mock_response = MagicMock()
    mock_response.json.return_value = {"link": {"webUrl": "https://share.link/file.mp4"}}
    mock_response.raise_for_status.return_value = None
    mock_post.return_value = mock_response
    link = create_sharing_link("token", "MBA-Resources/Value Chain Management/Marketing Management/test.mp4")
    assert link == "https://share.link/file.mp4"

# Test create_sharing_link with a missing file
@patch("notebook_automation.cli.onedrive_share.check_if_file_exists", return_value=None)
def test_create_sharing_link_file_not_found(mock_check):
    from notebook_automation.cli.onedrive_share import create_sharing_link
    link = create_sharing_link("token", "MBA-Resources/Value Chain Management/Marketing Management/missing.mp4")
    assert link is None

# Test check_if_file_exists returns file metadata
@patch("notebook_automation.cli.onedrive_share.requests.get")
def test_check_if_file_exists_success(mock_get):
    from notebook_automation.cli.onedrive_share import check_if_file_exists
    mock_response = MagicMock()
    mock_response.json.return_value = {"id": "fileid", "name": "file.mp4"}
    mock_response.raise_for_status.return_value = None
    mock_get.return_value = mock_response
    meta = check_if_file_exists("token", "MBA-Resources/Value Chain Management/Marketing Management/test.mp4")
    assert meta["id"] == "fileid"
    assert meta["name"] == "file.mp4"

# Test check_if_file_exists returns None on error
@patch("notebook_automation.cli.onedrive_share.requests.get", side_effect=Exception("fail"))
def test_check_if_file_exists_error(mock_get):
    from notebook_automation.cli.onedrive_share import check_if_file_exists
    meta = check_if_file_exists("token", "MBA-Resources/Value Chain Management/Marketing Management/test.mp4")
    assert meta is None
