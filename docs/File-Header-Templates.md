# Standard Header Templates for Notebook Automation Project

This document provides standardized header templates for different types of files in the Notebook Automation project.

## C# File Header Template

### Standard Template

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: {PURPOSE_DESCRIPTION}
// Created: {CREATION_DATE}
// </summary>
```

### Template Variables

- `{FILENAME}`: The name of the file (e.g., `ConfigValidation.cs`)
- `{YEAR}`: Current year (e.g., `2025`)
- `{AUTHOR_NAME}`: Author's full name (from git config or manual entry)
- `{AUTHOR_EMAIL}`: Author's email (from git config or manual entry)
- `{RELATIVE_PATH}`: Path relative to project root (e.g., `src/c-sharp/Project/File.cs`)
- `{PURPOSE_DESCRIPTION}`: Brief description of the file's purpose and functionality
- `{CREATION_DATE}`: Date file was created (YYYY-MM-DD format)

## File Type Specific Templates

### 1. Service Classes

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Service class for {SERVICE_DESCRIPTION}. Provides {MAIN_FUNCTIONALITY}.
// Dependencies: {KEY_DEPENDENCIES}
// Created: {CREATION_DATE}
// </summary>
```

### 2. Test Classes

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Unit tests for {CLASS_UNDER_TEST}. Tests {TEST_SCENARIOS}.
// Test Framework: MSTest
// Created: {CREATION_DATE}
// </summary>
```

### 3. Model/Data Classes

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Data model representing {ENTITY_DESCRIPTION}. Used for {PRIMARY_USE_CASE}.
// Properties: {KEY_PROPERTIES}
// Created: {CREATION_DATE}
// </summary>
```

### 4. Interface Definitions

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Interface contract for {INTERFACE_DESCRIPTION}. Defines {CONTRACT_DETAILS}.
// Implementations: {KNOWN_IMPLEMENTATIONS}
// Created: {CREATION_DATE}
// </summary>
```

### 5. Utility Classes

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Utility class providing {UTILITY_FUNCTIONS}. Contains static helper methods for {DOMAIN}.
// Usage: {COMMON_USAGE_PATTERN}
// Created: {CREATION_DATE}
// </summary>
```

### 6. Configuration Classes

```csharp
// <copyright file="{FILENAME}" company="Notebook Automation Project">
// Copyright (c) {YEAR} Notebook Automation Project. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for license information.
// </copyright>
// <author>{AUTHOR_NAME} <{AUTHOR_EMAIL}></author>
// <summary>
// File: {RELATIVE_PATH}
// Purpose: Configuration class for {CONFIG_DOMAIN}. Manages settings for {FEATURE_AREA}.
// Configuration Source: {CONFIG_SOURCE} (appsettings.json, user secrets, etc.)
// Created: {CREATION_DATE}
// </summary>
```

## Usage Guidelines

### 1. Purpose Description Guidelines

- **Be Specific**: Describe what the class/file actually does, not just what it is
- **Include Context**: Mention the domain or feature area it belongs to
- **Mention Key Responsibilities**: List 2-3 main responsibilities

### 2. Good Purpose Examples

- ✅ "Handles PDF document processing and metadata extraction for course materials"
- ✅ "Service for managing OneDrive file synchronization and mapping local vault paths"
- ✅ "Configuration validation and setup for AI service integration with Azure OpenAI"

### 3. Poor Purpose Examples

- ❌ "Configuration class" (too generic)
- ❌ "Handles files" (too vague)
- ❌ "Main service" (not descriptive)

## VS Code Snippet Integration

Add this to your VS Code user snippets for C# (`csharp.json`):

```json
{
    "File Header": {
        "prefix": "fileheader",
        "body": [
            "// <copyright file=\"${TM_FILENAME}\" company=\"Notebook Automation Project\">",
            "// Copyright (c) ${CURRENT_YEAR} Notebook Automation Project. All rights reserved.",
            "// Licensed under the MIT License. See LICENSE file in the project root for license information.",
            "// </copyright>",
            "// <author>${1:Author Name} <${2:email@domain.com}></author>",
            "// <summary>",
            "// File: ${RELATIVE_FILEPATH}",
            "// Purpose: ${3:Describe the purpose of this file}",
            "// Created: ${CURRENT_YEAR}-${CURRENT_MONTH}-${CURRENT_DATE}",
            "// </summary>",
            "",
            "$0"
        ],
        "description": "Insert standard file header for C# files"
    }
}
```

## Automated Template Application

Use the existing script with custom templates:

```powershell
# Apply standard headers
pwsh scripts/add-file-headers.ps1 -Path "src/c-sharp"

# Apply with custom company/author
pwsh scripts/add-file-headers.ps1 -Path "src/c-sharp" -Author "Your Name" -Company "Your Company"
```

## Template Customization

To customize the default template, modify the `Get-FileHeader` function in `scripts/add-file-headers.ps1`:

1. Update company name
2. Change license information
3. Modify purpose placeholder text
4. Add additional metadata fields
5. Customize date formats

## Integration with Development Workflow

1. **New Files**: Use VS Code snippet (`fileheader`) when creating new files
2. **Bulk Updates**: Use PowerShell script for multiple files
3. **CI/CD**: Consider adding header validation to build pipeline
4. **Code Reviews**: Ensure purpose descriptions are meaningful and accurate

## Maintenance

- **Regular Reviews**: Periodically review purpose descriptions for accuracy
- **Template Updates**: Update templates when project structure or standards change
- **Documentation**: Keep this template guide updated with new patterns and examples
