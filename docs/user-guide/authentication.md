# Authentication Guide

Learn how to authenticate with OneDrive to enable cloud integration features in Notebook Automation.

## Overview

Notebook Automation uses Microsoft Graph API to integrate with OneDrive, enabling features like:

- **Vault Synchronization**: Sync folder structures between local vault and OneDrive
- **Share Link Generation**: Create shareable links for documents
- **Document Placeholders**: Reference OneDrive content in markdown files
- **Content Extraction**: Pull HTML and other content from OneDrive

Authentication is required for any features that access OneDrive content or APIs.

---

## Why Authentication Matters

OneDrive integration enables:
- ✅ Seamless cloud storage access
- ✅ Collaborative workflows with shared links
- ✅ Location-agnostic vault organization
- ✅ Secure access to your content
- ✅ Cross-device synchronization support

---

## Core Concepts

### Microsoft Graph API

Notebook Automation uses Microsoft Graph API to access OneDrive:

- **Secure Authentication**: OAuth 2.0 protocol
- **Scoped Permissions**: Limited to necessary OneDrive operations
- **Refresh Tokens**: Long-lived access without repeated logins
- **User Consent**: You control what the application can access

### Token Management

**Access Token:**
- Short-lived (typically 1 hour)
- Used for API requests
- Automatically refreshed

**Refresh Token:**
- Long-lived (can last months)
- Used to obtain new access tokens
- Stored securely in user secrets

---

## Initial Setup

### Step 1: Configure Client ID

Ensure your configuration file has the OneDrive client ID:

**Location:** `config/config.json`

```json
{
  "OneDrive": {
    "ClientId": "489ad055-e4b0-4898-af27-53506ce83db7"
  }
}
```

**Note:** The default client ID is pre-configured for Notebook Automation. You typically don't need to change this unless using a custom Azure AD application.

### Step 2: Configure OneDrive Paths

Set your OneDrive root paths in the configuration:

```json
{
  "Paths": {
    "OneDriveFullpathRoot": "C:\\Users\\YourName\\OneDrive\\",
    "OneDriveResourcesBasePath": "Education"
  }
}
```

**Adjust paths for your system:**
- Windows: `C:\\Users\\YourName\\OneDrive\\`
- macOS: `/Users/YourName/OneDrive/`
- Linux: `/home/yourname/OneDrive/`

---

## Authentication Process

### First-Time Authentication

Run the `refresh-token` command to authenticate:

```bash
na refresh-token
```

**What happens:**

1. **Browser Opens**: A browser window opens to Microsoft login page
2. **Sign In**: Enter your Microsoft account credentials
3. **Grant Permissions**: Review and accept requested permissions
4. **Redirect**: Browser shows success message
5. **Token Stored**: Refresh token saved securely in user secrets

**Expected Output:**
```
Opening browser for authentication...
Waiting for authentication to complete...
✓ Authentication successful
✓ Refresh token stored securely
```

### What Permissions Are Requested?

The application requests minimal necessary permissions:

- **Files.Read**: Read files in your OneDrive
- **Files.ReadWrite**: Create and modify files (for sync)
- **Sites.Read.All**: Access OneDrive site information
- **offline_access**: Maintain access with refresh token

### Security Considerations

**Token Storage:**
- Tokens stored in .NET User Secrets (not in config files)
- Encrypted at the operating system level
- Not committed to source control
- Separate from configuration files

**Token Access:**
- Only accessible by your user account
- Protected by OS-level security
- Isolated from other applications

---

## Using Authentication

### Commands That Require Authentication

**Vault Synchronization:**
```bash
na vault vault-sync "path/to/vault"
```

**Generate Markdown with Share Links:**
```bash
na generate-markdown -p "documents/" 
# (Creates OneDrive share links by default)
```

**Video/PDF Processing with OneDrive:**
```bash
na video-notes -p "path/to/video.md" --refresh-auth
na pdf-notes -p "path/to/document.md" --refresh-auth
```

### Commands That Don't Require Authentication

These commands work without OneDrive authentication:

- `na config view`
- `na config update`
- `na tag add-nested`
- `na vault generate-index` (local operations)
- `na video-notes -p <local-file>` (without OneDrive features)
- `na pdf-notes -p <local-file>` (without OneDrive features)

---

## Token Refresh

### When to Refresh

Refresh your authentication token when:

**You see authentication errors:**
```
Error: Authentication token expired
Error: Unable to access OneDrive
Error: 401 Unauthorized
```

**After extended periods:**
- Not used the tool in months
- Refresh token may have expired
- OneDrive integration features fail

**After password changes:**
- Changed Microsoft account password
- Updated security settings
- Modified account permissions

### How to Refresh

Simply run the refresh-token command again:

```bash
na refresh-token
```

**Process:**
1. Opens browser for re-authentication
2. Sign in with Microsoft account
3. Stores new refresh token
4. Ready to use OneDrive features

### Automatic Refresh

Many commands support automatic token refresh:

```bash
# Video processing with automatic token refresh
na video-notes -p "video.mp4" --refresh-auth

# PDF processing with automatic token refresh
na pdf-notes -p "document.pdf" --refresh-auth
```

**How it works:**
- Checks token validity before processing
- Automatically refreshes if expired
- Continues with operation
- No manual intervention needed

---

## Troubleshooting

### Common Issues

#### Problem: Browser doesn't open

**Symptoms:**
```
Opening browser for authentication...
Error: Unable to open browser
```

**Solutions:**
1. **Manually open the URL**
   - Copy the authentication URL from console
   - Open in your default browser
   - Complete authentication

2. **Check browser availability**
   - Ensure a web browser is installed
   - Set default browser in OS settings

3. **Use different terminal**
   - Try from different command prompt
   - Run with elevated permissions if needed

#### Problem: Authentication fails in browser

**Symptoms:**
- Browser shows error after sign-in
- "Unable to complete authentication" message
- Redirect fails

**Solutions:**
1. **Check account status**
   - Verify Microsoft account is active
   - Ensure two-factor authentication is configured if required

2. **Clear browser cache**
   - Clear cookies and cache
   - Try incognito/private window

3. **Check network**
   - Verify internet connection
   - Check firewall settings
   - Disable VPN temporarily if issues persist

#### Problem: Token not saved

**Symptoms:**
```
✓ Authentication successful
✗ Error saving refresh token
```

**Solutions:**
1. **Check user secrets location**
   ```bash
   # View secrets status
   na config secrets
   ```

2. **Verify permissions**
   - Ensure write permissions to user profile
   - Check disk space available

3. **Re-run command**
   ```bash
   na refresh-token
   ```

#### Problem: Repeated authentication requests

**Symptoms:**
- Asked to authenticate frequently
- Token doesn't persist between sessions

**Solutions:**
1. **Check token storage**
   ```bash
   na config secrets
   ```
   Should show refresh token is set

2. **Verify configuration**
   - Check client ID in config.json
   - Ensure paths are correct

3. **Re-authenticate**
   ```bash
   na refresh-token --verbose
   ```

### Authentication Errors During Commands

#### Error: "Token expired"

**Solution:**
```bash
# Refresh token first
na refresh-token

# Then retry your command
na vault vault-sync "vault/"
```

**Or use automatic refresh:**
```bash
na vault vault-sync "vault/" --refresh-auth
```

#### Error: "Invalid client ID"

**Solution:**
Verify client ID in configuration:

```bash
na config view
```

Check that OneDrive.ClientId matches the expected value.

#### Error: "Permission denied"

**Solution:**
1. Re-authenticate to grant permissions
2. Review requested permissions carefully
3. Ensure account has necessary access

---

## Advanced Configuration

### Custom Azure AD Application

If you need to use your own Azure AD application:

**Step 1: Register Application**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to Azure Active Directory → App registrations
3. Create new registration
4. Note the Application (client) ID

**Step 2: Configure Permissions**
Add Microsoft Graph API permissions:
- Files.Read
- Files.ReadWrite
- Sites.Read.All
- offline_access

**Step 3: Update Configuration**
```json
{
  "OneDrive": {
    "ClientId": "your-client-id-here"
  }
}
```

**Step 4: Authenticate**
```bash
na refresh-token
```

### Multi-Account Support

To work with multiple OneDrive accounts:

**Option 1: Multiple Configuration Files**
```bash
# Personal account
na vault vault-sync "vault/" --config "config-personal.json"

# Work account
na vault vault-sync "vault-work/" --config "config-work.json"
```

**Option 2: Re-authenticate as Needed**
```bash
# Switch to different account
na refresh-token
# Complete authentication with different account

# Use OneDrive features
na vault vault-sync "vault/"
```

---

## Security Best Practices

### Protecting Your Tokens

**Do:**
- ✅ Keep refresh tokens secure in user secrets
- ✅ Use strong passwords for Microsoft account
- ✅ Enable two-factor authentication on your account
- ✅ Regularly review authorized applications

**Don't:**
- ❌ Share refresh tokens with others
- ❌ Store tokens in config files
- ❌ Commit tokens to source control
- ❌ Use tokens on untrusted systems

### Account Security

**Recommendations:**
1. **Enable 2FA**: Add extra security to Microsoft account
2. **Strong Password**: Use unique, complex password
3. **Review Permissions**: Periodically check authorized apps
4. **Monitor Activity**: Review account activity logs

### Token Lifecycle

**Best practices:**
- Refresh tokens when you see authentication errors
- Re-authenticate after long periods of inactivity
- Revoke tokens if device is compromised
- Use `--refresh-auth` flag for critical operations

---

## Integration with OneDrive Features

### Vault Synchronization

After authentication, sync vault with OneDrive:

```bash
# Authenticate first
na refresh-token

# Then sync
na vault vault-sync "vault/" --verbose
```

**What gets synced:**
- Folder structure (not file contents)
- Document placeholder creation
- Metadata alignment

### Share Link Generation

Generate share links for OneDrive files:

```bash
# Authenticate
na refresh-token

# Generate markdown with share links
na generate-markdown -p "documents/"
```

**Result:**
- OneDrive share links added to frontmatter
- Links are shareable with others
- Maintain access control through OneDrive

### Document Placeholder Processing

Process placeholders that reference OneDrive:

```bash
# Authenticate
na refresh-token

# Process video from OneDrive path
na video-notes -p "placeholder-video.md" --verbose

# Process PDF from OneDrive path
na pdf-notes -p "placeholder-pdf.md" --verbose
```

---

## Workflow Examples

### Workflow 1: Initial Setup

**Goal:** Set up OneDrive authentication for first time.

**Steps:**

1. **Configure paths:**
   ```bash
   na config update "Paths.OneDriveFullpathRoot" "C:\Users\Me\OneDrive"
   na config update "Paths.OneDriveResourcesBasePath" "Documents"
   ```

2. **Authenticate:**
   ```bash
   na refresh-token
   ```

3. **Verify:**
   ```bash
   na config secrets
   ```
   Should show refresh token is set

4. **Test:**
   ```bash
   na vault vault-sync "vault/" --dry-run
   ```

### Workflow 2: Token Refresh Maintenance

**Goal:** Refresh authentication after extended period.

**Steps:**

1. **Check for issues:**
   ```bash
   na vault vault-sync "vault/" --dry-run
   ```

2. **If authentication errors:**
   ```bash
   na refresh-token
   ```

3. **Retry operation:**
   ```bash
   na vault vault-sync "vault/" --verbose
   ```

### Workflow 3: Multi-Device Setup

**Goal:** Use same vault on multiple computers.

**Steps on each device:**

1. **Install application**

2. **Copy configuration:**
   - Use same `config.json` on each device
   - Adjust paths for local file system

3. **Authenticate separately:**
   ```bash
   na refresh-token
   ```
   Each device needs its own authentication

4. **Sync vault:**
   ```bash
   na vault vault-sync "vault/"
   ```

---

## Related Commands

- **[vault vault-sync](vault-synchronization.md)** - Synchronize with OneDrive
- **[generate-markdown](markdown-generation.md)** - Generate markdown with share links
- **[config commands](../getting-started/basic-commands.md#configuration-management)** - Manage configuration

---

## Additional Resources

- **[CLI Reference](../cli-reference.md#refresh-token-command)** - Complete refresh-token documentation
- **[Vault Synchronization Guide](vault-synchronization.md)** - Using OneDrive sync features
- **[Troubleshooting](../troubleshooting/index.md)** - Common authentication issues
- **[Microsoft Graph API Docs](https://docs.microsoft.com/en-us/graph/)** - Official API documentation

---

*Last updated: 2025-10-28*
