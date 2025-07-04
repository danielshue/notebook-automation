# Notebook Automation Plugin - Beta Testing with BRAT

## For Beta Testers

### Installation via BRAT

1. **Install BRAT** (if not already installed):
   - Go to Obsidian Settings → Community Plugins
   - Search for "BRAT" and install it
   - Enable the BRAT plugin

2. **Add our plugin**:
   - Open BRAT settings
   - Click "Add Beta Plugin"
   - Enter: `danielshue/notebook-automation`
   - Click "Add Plugin"

3. **BRAT will automatically**:
   - Download the latest beta version
   - Install it in your plugins folder
   - Enable auto-updates for new beta releases

### What's Included

The plugin includes:
- **Main plugin files** (main.js, manifest.json, styles.css)
- **Cross-platform executables** for Windows, macOS, and Linux
- **Configuration files** for easy setup

### Beta Testing Focus Areas

Please test:
1. **Plugin installation** - Does it install correctly via BRAT?
2. **Basic functionality** - Can you access the plugin commands?
3. **Cross-platform compatibility** - Do the executables work on your system?
4. **Configuration** - Can you set up the plugin with your preferences?
5. **Error handling** - Any crashes or unexpected behavior?

### Reporting Issues

Please report issues on GitHub:
- **Repository**: https://github.com/danielshue/notebook-automation
- **Issues**: https://github.com/danielshue/notebook-automation/issues

Include:
- Your operating system
- Obsidian version
- Plugin version (should be 0.1.0-beta.1)
- Steps to reproduce the issue
- Any error messages

### Version Updates

BRAT will automatically notify you when new beta versions are available. You can:
- **Auto-update**: Let BRAT handle updates
- **Manual update**: Check for updates in BRAT settings

## For Developers

### Quick Beta Release

```bash
# Update to next beta version
cd src/obsidian-plugin
npm version 0.1.0-beta.2 --no-git-tag-version

# Sync manifest.json
npm run version

# Build and test
npm run build

# Commit and tag
git add .
git commit -m "feat: prepare v0.1.0-beta.2 for BRAT testing"
git tag v0.1.0-beta.2
git push origin v0.1.0-beta.2
```

The CI workflow will automatically create a GitHub release when you push a tag.

### Using the Management Script

For a more automated approach:

```bash
# Create beta release
.\scripts\manage-plugin-version.ps1 -Version "0.1.0-beta.2" -Type "beta" -CreateRelease -PreRelease

# Create stable release
.\scripts\manage-plugin-version.ps1 -Version "0.1.0" -Type "stable" -CreateRelease
```

## Current Status

- **Latest Beta**: v0.1.0-beta.1
- **Repository**: https://github.com/danielshue/notebook-automation
- **BRAT URL**: `danielshue/notebook-automation`

## Next Steps

1. **Test the beta version** with BRAT
2. **Gather feedback** from beta testers
3. **Fix any issues** found during testing
4. **Release stable version** when ready

## Resources

- [BRAT Plugin](https://github.com/TfTHacker/obsidian42-brat)
- [Obsidian Plugin Development](https://docs.obsidian.md/Plugins/Getting+started/Build+a+plugin)
- [Our Plugin Documentation](../docs/obsidian-plugin-brat-setup.md)
