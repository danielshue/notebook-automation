# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Document Placeholder System**: Enhanced placeholder creation with automatic content type suffixes
  - Video placeholders: `filename-video.md` (e.g., `03_01_defining-operations-management-video.md`)
  - PDF placeholders: `filename-pdf.md` (e.g., `case-study-analysis-pdf.md`)
  - HTML content placeholders: `filename-html.md` (e.g., `course-instructions-html.md`)
- **HTML Document Processing**: Added HTML support to DocumentNoteBatchProcessor with proper `-html.md` suffix handling
- **Comprehensive CLI Documentation**: Complete documentation overhaul with 71KB of new content
  - CLI Reference (19KB) - Complete command reference with examples
  - Tag Management Guide (13KB) - Comprehensive guide for all 8 tag commands
  - Vault Synchronization Guide (14KB) - OneDrive integration and document placeholders
  - Quick Start Guide (7KB) - 5-minute setup for new users
  - Command Cheat Sheet (8KB) - Quick reference for all commands
  - Deprecation Guide - Clear migration path for renamed commands
- **Intelligent Force Flag Handling**: Smart detection of AI content in existing markdown files
  - Automatically processes files without AI content without requiring `--force` flag
  - Multi-strategy AI content detection in existing files
  - Maintains backward compatibility with existing force flag behavior

### Fixed

- **Document Processing Pipeline**: Enhanced `DocumentNoteBatchProcessor.GenerateOutputPath` to handle video (`-video.md`), PDF (`-pdf.md`), and HTML (`-html.md`) file suffixes correctly
- **CLI Documentation Errors**: Fixed all incorrect command examples in getting-started and user-guide documentation
  - Replaced non-existent `process` command with `video-notes -p` and `pdf-notes -p`
  - Corrected `config init/show/set` to actual commands `config view/update`
  - Fixed all OneDrive command references to use correct syntax
- **Obsidian Plugin Error Handling**: Enhanced error messages for corrupted executables
  - Improved null exit code handling with clear diagnostic messages
  - Better checksum validation error messages with troubleshooting steps
  - Graceful handling of executable failures during version detection

### Changed

- **VaultFolderSyncProcessor**: Updated placeholder creation to automatically apply appropriate content type suffixes based on referenced file extension and template type
- **DocumentNoteBatchProcessor**: Added HTML document type support and enhanced processor type detection with intelligent skip logic
- **Test Suite**: Updated all related unit tests to reflect new naming convention expectations
- **Documentation Quality**: Improved documentation from 4/10 to 9/10 quality rating
  - Fixed all broken command references in getting-started guides
  - Updated all user guide examples with correct CLI syntax
  - Reorganized documentation structure for better discoverability
  - Added comprehensive troubleshooting and quick reference materials
- **Plugin Error Messages**: Enhanced error reporting for better debugging experience
  - Clear diagnostic information for process crashes
  - Detailed guidance for checksum mismatch issues
  - Type-safe error handling for null exit codes




## [0.1.0-beta.8] - 2025-08-22

## [0.1.0-beta.7] - 2025-08-21

## [0.1.0-beta.6] - 2025-07-16

## [0.1.0-beta.2] - 2025-07-06

### Added

- **Obsidian Plugin UI Enhancements**:
  - Advanced Configuration toggle to show/hide technical settings (timeout and other configuration sections)
  - Conditional visibility for Microsoft Graph Configuration based on OneDrive Shared Link toggle
  - Conditional visibility for Banners Configuration based on Banners Enabled toggle
  - Improved settings organization with cleaner interface for basic users
- **Configuration Management**:
  - Enhanced timeout configuration fields with proper validation
  - Added video and PDF extension configuration in Other Configuration section
  - Improved configuration loading from environment variables and default config files
- **Development Tools**:
  - Comprehensive guidelines for REST APIs, localization, and .NET MAUI patterns
  - Enhanced logging system with reduced verbosity and rolling log files
  - Improved build process with better file handling for BRAT compatibility

### Fixed

- Fixed PR comment permission errors in Windows CI build
- Corrected output file path in esbuild configuration
- Improved compilation error handling with missing using statements
- Enhanced logging clarity for unavailable AI services

### Changed

- **Logging Improvements**:
  - Converted verbose LogInformation to LogDebug for better CLI experience
  - Adjusted logging levels for CLI appropriateness
  - Implemented rolling log files for better log management
- **UI/UX Improvements**:
  - Enhanced OneDrive sync and index menu titles for clarity and consistency
  - Updated settings interface with better organization and descriptions
  - Improved folder handling and logging functionality
- **Documentation**:
  - Updated README with detailed Obsidian integration features
  - Added comprehensive developer guidelines and best practices
  - Enhanced PowerShell examples in documentation

## [0.1.0-beta.1] - 2025-07-03

### Added

- **Core Features**:
  - AI-powered content analysis and summarization
  - Obsidian integration with hierarchical course structures
  - OneDrive synchronization and shared link management
  - PDF annotation extraction and video transcript processing
  - Cross-platform support for Windows, Linux, and macOS
- **Obsidian Plugin**:
  - Feature toggles for AI Video Summary, AI PDF Summary, Index Creation, and Metadata Management
  - Command flags for verbose, debug, dry-run, and force modes
  - Configuration management for AI services (Azure, OpenAI, Foundry)
  - Microsoft Graph integration for OneDrive functionality
- **Development Infrastructure**:
  - BRAT (Beta Reviewer's Auto-update Tool) compatibility
  - Automated version management and release scripts
  - Local CI build script mirroring GitHub Actions pipeline
  - Comprehensive documentation including user guide, developer guide, and API reference

### Fixed

- Initial bug fixes and stability improvements

### Changed

- Established beta release process for community testing

[Unreleased]: https://github.com/danielshue/notebook-automation/compare/v0.1.0-beta.2...HEAD
[0.1.0-beta.2]: https://github.com/danielshue/notebook-automation/compare/v0.1.0-beta.1...v0.1.0-beta.2
[0.1.0-beta.1]: https://github.com/danielshue/notebook-automation/releases/tag/v0.1.0-beta.1
