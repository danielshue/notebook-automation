# Obsidian Plugin BRAT Setup Guide

## Overview

This guide explains how to set up the Notebook Automation Obsidian plugin for beta testing with BRAT(https://tfthacker.com/BRAT) (Beta Reviewer's Auto-update Tool). 

## How BRAT Works

BRAT examines the repository's GitHub releases to install and update plugins. It:
1. Fetches the list of available releases from GitHub
2. Selects the appropriate release (latest by semantic version)
3. Downloads the `manifest.json`, `main.js`, and `styles.css` directly from the release assets
4. Installs the plugin files into the user's Obsidian vault

## Current Repository Structure

```
notebook-automation/
├── src/obsidian-plugin/
│   ├── manifest.json (contains version info)
│   ├── main.ts (source code)
│   ├── styles.css (styling)
│   ├── package.json (npm package info)
│   └── dist/ (build output)
└── .github/workflows/
    └── ci-cross-platform.yml (builds and releases)
```

## Version Management Strategy

### 1. Development vs Release Versions

**For Beta Testing:**
- Use pre-release versions like `0.1.0-beta.1`, `0.1.0-beta.2`, etc.
- Mark GitHub releases as "pre-release"
- Don't commit version changes to the default branch yet

**For Production:**
- Use stable versions like `0.1.0`, `0.2.0`, etc.
- Create standard GitHub releases
- Commit version changes to the default branch

### 2. Version Synchronization

The version number must be consistent across:
- `src/obsidian-plugin/manifest.json`
- `src/obsidian-plugin/package.json`
- Git tags (e.g., `v0.1.0-beta.1`)
- GitHub releases (e.g., `0.1.0-beta.1`)

## Step-by-Step Setup Process

### Step 1: Prepare Beta Version

1. **Update package.json version:**
   ```json
   {
     "version": "0.1.0-beta.1"
   }
   ```

2. **Run version bump script:**
   ```bash
   cd src/obsidian-plugin
   npm version 0.1.0-beta.1 --no-git-tag-version
   npm run version
   ```

3. **Verify manifest.json is updated:**
   ```json
   {
     "version": "0.1.0-beta.1"
   }
   ```

### Step 2: Build and Test Locally

```bash
cd src/obsidian-plugin
npm run build
```

**Note**: The build process will automatically:
- Compile TypeScript and bundle with esbuild
- Copy plugin files to the root `dist` directory
- Preserve any existing executables from CI builds
- Verify all required files are present

The final `dist` directory will contain:
- `main.js` (compiled plugin code)
- `manifest.json` (plugin metadata)
- `styles.css` (plugin styling)
- `default-config.json` (default configuration)
- Cross-platform executables (if available from CI)

### Step 3: Create Git Tag and Release

1. **Create and push tag:**
   ```bash
   git add .
   git commit -m "feat: prepare v0.1.0-beta.1 for BRAT testing"
   git tag v0.1.0-beta.1
   git push origin v0.1.0-beta.1
   ```

2. **CI automatically creates release** (configured in `.github/workflows/ci-cross-platform.yml`)

3. **Manually mark release as pre-release** in GitHub UI

### Step 4: Manual Release Creation (Alternative)

If you prefer manual control:

```bash
# Create release with pre-release flag
gh release create v0.1.0-beta.1 \
  --title "v0.1.0-beta.1 - Beta Release" \
  --notes "Beta release for BRAT testing" \
  --prerelease \
  src/obsidian-plugin/dist/main.js \
  src/obsidian-plugin/manifest.json \
  src/obsidian-plugin/styles.css
```

## BRAT Installation Instructions

### For Beta Testers:

1. **Install BRAT plugin** in Obsidian
2. **Add beta plugin** using repository URL:
   ```
   https://github.com/danielshue/notebook-automation
   ```
3. **BRAT will automatically:**
   - Find the latest pre-release
   - Download and install the plugin files
   - Set up auto-updates for new pre-releases

### For Frozen Version Testing:

To test a specific version, use the tag in BRAT:
```
danielshue/notebook-automation@v0.1.0-beta.1
```

## Important Considerations

### Semantic Versioning and Obsidian

Obsidian has limited semver support. If users install a pre-release like `1.0.1-beta.1`, Obsidian will NOT automatically upgrade to `1.0.1` when released. Users need to:

- Use BRAT to upgrade to the final release, OR
- Wait for version `1.0.2` or higher for Obsidian's auto-update to work

### Version Progression Example

| Version      | Type        | Obsidian Auto-Update | BRAT Required |
| ------------ | ----------- | -------------------- | ------------- |
| 0.1.0        | Stable      | ✅                    | ❌             |
| 0.1.1-beta.1 | Pre-release | ❌                    | ✅             |
| 0.1.1-beta.2 | Pre-release | ❌                    | ✅             |
| 0.1.1        | Stable      | ❌ (blocked by beta)  | ✅             |
| 0.1.2        | Stable      | ✅                    | ❌             |

## Current CI Workflow

The existing CI workflow (`ci-cross-platform.yml`) already:
- ✅ Builds the plugin automatically
- ✅ Creates plugin directory with all required files
- ✅ Includes cross-platform executables
- ✅ Creates GitHub releases on tag push
- ✅ Uploads plugin as ready-to-install package

## Build Process Improvements

The plugin build process has been enhanced with:
- **Dedicated build script** (`build-plugin.mjs`) for better maintainability
- **Automatic executable preservation** from CI builds
- **Comprehensive validation** of all required files
- **Clear build output** showing what's included

You can test the complete workflow with:
```bash
# Test the complete BRAT workflow
.\scripts\test-brat-workflow.ps1
```

This script will:
- Build the plugin locally
- Verify all BRAT-required files are present
- Validate the manifest.json structure
- Check for executables
- Simulate BRAT installation process
- Verify version consistency

## Next Steps

1. **Fix version mismatch** - Update manifest.json to match release version
2. **Test BRAT installation** - Verify plugin installs correctly
3. **Document for users** - Create installation guide for beta testers
4. **Establish release process** - Define beta → stable promotion workflow

## Troubleshooting

### Common Issues:

1. **Version mismatch between manifest and release**
   - Solution: Always use `npm run version` to sync versions

2. **BRAT can't find plugin**
   - Check that release contains `manifest.json`, `main.js`, and `styles.css`
   - Verify GitHub release is public and accessible

3. **Plugin doesn't load in Obsidian**
   - Check console for JavaScript errors
   - Verify `main.js` was built correctly
   - Check `manifest.json` format

### Debug Commands:

```bash
# Check current versions
cat src/obsidian-plugin/manifest.json | grep version
cat src/obsidian-plugin/package.json | grep version
git tag --list

# Test build
cd src/obsidian-plugin
npm run build
ls -la dist/

# Verify release contents
gh release view v0.1.0-beta.1
```

## Resources

- [BRAT Plugin Documentation](https://github.com/TfTHacker/obsidian42-brat)
- [Obsidian Plugin Development](https://docs.obsidian.md/Plugins/Getting+started/Build+a+plugin)
- [Semantic Versioning](https://semver.org/)
