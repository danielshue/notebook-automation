# CLI Troubleshooting Guide

Common errors and solutions when using the Notebook Automation CLI.

## Exit Codes

| Code | Meaning              | Description                                                     |
| ---- | -------------------- | --------------------------------------------------------------- |
| 0    | Success              | The command completed successfully.                             |
| 1    | General Error        | an unexpected error occurred. Check logs for details.           |
| 2    | Invalid Arguments    | The command arguments were invalid. Check usage with `--help`.  |
| 3    | File Not Found       | The specified input file could not be found.                    |
| 4    | Directory Not Found  | The specified directory could not be found.                     |
| 5    | Configuration Error  | The configuration file is invalid or missing required settings. |
| 6    | API Error            | External API call (OpenAI, Azure, Graph) failed.                |
| 7    | Authentication Error | Microsoft Graph authentication failed or token expired.         |

## Common Error Messages

### "Command not found: na" or "The term 'na' is not recognized"
**Cause**: The CLI tool is not in your system PATH.
**Solution**:
1. Ensure you have installed the tool correctly.
2. Add the installation directory to your PATH environment variable.
3. Or use the full path to the executable: `.\na.exe` (Windows) or `./na` (Linux/Mac).

### "API key is missing or invalid"
**Cause**: OpenAI or Azure OpenAI API key is not configured.
**Solution**:
1. Check your `config.json` for `AIService` settings.
2. Verify you have set the API key in User Secrets or Environment Variables.
```powershell
na config update "AIService.ApiKey" "sk-..."
```

### "Access token has expired"
**Cause**: The OneDrive authentication token is no longer valid.
**Solution**:
Run the refresh token command to re-authenticate:
```powershell
na refresh-token
```

### "File is locked by another process"
**Cause**: You are trying to process a file that is open in another application (e.g., Word, PDF Viewer).
**Solution**:
Close the application using the file and try again.

### "Rate limit exceeded"
**Cause**: You have exceeded the API rate limits for your AI provider.
**Solution**:
1. Wait a few minutes and try again.
2. Reduce the `MaxConcurrentFiles` setting in your configuration.
3. Check your quota with your AI provider.

## Debugging

If you are unable to resolve an issue, you can enable debug logging to get more detailed information.

**Enable Debug Output:**
```powershell
na video-notes -p "video.mp4" --debug
```

**Check Log Files:**
Logs are stored in the directory specified in your configuration (default: `logs/`).
Open the latest log file to see detailed error traces.
