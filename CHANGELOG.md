# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- TBD

### Fixed

- TBD

### Changed

- TBD

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
