---
applyTo: "**"
---

## Code Reuse Priority Hierarchy

### 1. Reuse Existing Packages First

- Always search for and utilize existing packages in the `tools` folder before creating new code
- Consider importing and extending existing utilities rather than duplicating functionality
- Example approach: `from tools.existing_module import ExistingClass`

### 2. Package Discovery Process

When implementing a solution:

1. First analyze the `tools` directory structure to identify relevant packages
2. Check for functionality that matches or can be adapted to current requirements
3. Consider composition of existing utilities to meet new requirements
4. Only proceed to creating new packages when existing ones cannot fulfill requirements

### 3. Package Extension Guidelines

- Do not modify existing package interfaces
- When additional functionality is needed:
  - Create subclasses that extend existing classes
  - Use composition to combine existing utilities
  - Create wrapper functions that utilize existing packages

### 4. New Package Creation Criteria

Only create new packages when:

- No suitable existing package exists in the `tools` folder
- Extending existing packages would violate their single responsibility
- The functionality is significantly different from anything available
- Attempting to reuse would create excessive complexity or coupling

### 5. Documentation Requirements for Reuse

When reusing packages:

- Document which packages were reused and why
- Note any limitations of the reused packages
- Explain integration points between existing and new code

### 6. New Package Structure

When a new package must be created:

- Place it in the appropriate subdirectory of `tools`
- Follow existing naming conventions
- Create proper interfaces for future reuse
- Document thoroughly to enable future discoverability