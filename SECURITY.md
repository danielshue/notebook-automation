# Security Policy

## Supported Versions

We actively support the following versions of Notebook Automation with security updates:

| Version | Supported          | Notes                    |
| ------- | ------------------ | ------------------------ |
| 1.0.x   | :white_check_mark: | Current stable release   |
| < 1.0   | :x:                | Pre-release versions     |

## Security Assumptions

- All user inputs are sanitized before processing through the AI services and file system operations
- Sensitive data (API keys, credentials) is stored securely using .NET configuration system with user secrets
- File operations are restricted to user-specified directories with appropriate validation
- OneDrive integration uses OAuth 2.0 with Microsoft Graph API following Microsoft's security guidelines

## Security Requirements

### For Users

- Use HTTPS for all network communications with AI providers and Microsoft Graph
- Store API keys and credentials using .NET User Secrets or secure environment variables
- Regularly rotate API keys and access tokens
- Review file permissions when processing sensitive documents
- Use trusted Obsidian vaults and verify plugin sources

### For Developers

- Follow secure coding practices outlined in the contributing guide
- Implement input validation for all user-provided data
- Use parameterized queries and avoid dynamic code execution
- Conduct security reviews for changes affecting authentication or file handling
- Keep dependencies updated and monitor for security advisories

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please follow these steps:

### How to Report

1. **DO NOT** create a public GitHub issue for security vulnerabilities
2. Email security details to Dan Shue
3. Include the following information:

   - Detailed description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact assessment
   - Suggested remediation if you have one
   - Your contact information for follow-up

### What to Expect

- **Acknowledgment**: We will acknowledge receipt of your report within 48 hours
- **Initial Assessment**: We will provide an initial assessment within 5 business days
- **Regular Updates**: We will keep you informed of our progress weekly
- **Resolution Timeline**: We aim to resolve critical vulnerabilities within 30 days
- **Disclosure**: We follow responsible disclosure practices

### Vulnerability Response Process

1. **Triage**: We assess the severity and impact of the reported vulnerability
2. **Investigation**: Our team investigates and reproduces the issue
3. **Development**: We develop and test a fix
4. **Testing**: The fix undergoes security testing and code review
5. **Release**: We release the fix in a security update
6. **Disclosure**: We publicly disclose the vulnerability after users have had time to update

### Severity Classification

- **Critical**: Remote code execution, authentication bypass, data exfiltration
- **High**: Privilege escalation, significant data exposure
- **Medium**: Information disclosure, denial of service
- **Low**: Minor information leaks, configuration issues

## Security Best Practices for Users

### Configuration Security

- Store sensitive configuration in user secrets: `dotnet user-secrets set "ApiKey" "your-key"`
- Use environment variables for production deployments
- Regularly audit and rotate API keys
- Review permissions granted to OneDrive integration

### File Processing Security

- Process files from trusted sources only
- Review generated content before sharing
- Be cautious with executable files in plugin deployments
- Regularly update the application and dependencies

### Obsidian Plugin Security

- Download the plugin only from official sources
- Verify plugin file integrity before installation
- Review plugin permissions and network access
- Keep Obsidian and plugins updated

## Secure Development Guidelines

### Input Validation

```csharp
// Example: Always validate file paths
public void ProcessFile(string filePath)
{
    ArgumentException.ThrowIfNullOrEmpty(filePath);
    
    // Validate path is within allowed directories
    var fullPath = Path.GetFullPath(filePath);
    if (!IsPathAllowed(fullPath))
    {
        throw new SecurityException("File path not allowed");
    }
}
```

### API Key Management

```csharp
// Use configuration system, never hardcode
var apiKey = _configuration["OpenAI:ApiKey"];
if (string.IsNullOrEmpty(apiKey))
{
    throw new InvalidOperationException("API key not configured");
}
```

### File Operations

```csharp
// Use safe file operations with proper disposal
using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
using var reader = new StreamReader(fileStream);
// Process file safely
```

## Dependencies and Third-Party Security

We regularly monitor our dependencies for security vulnerabilities using:

- GitHub Dependabot alerts
- .NET security advisories
- NuGet package vulnerability scanning

### Key Dependencies Security Notes

- **Microsoft.Graph**: Official Microsoft library with enterprise security standards
- **OpenAI API clients**: Use official or well-maintained community libraries
- **File processing libraries**: Regularly updated with security patches

## Incident Response

In case of a security incident:

1. **Immediate Response**: Disable affected features if necessary
2. **Assessment**: Evaluate scope and impact
3. **Communication**: Notify affected users through GitHub and documentation
4. **Resolution**: Deploy fixes and security updates
5. **Post-Incident**: Conduct review and improve processes

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl/)
- [.NET Security Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/security/)
- [GitHub Security Lab](https://securitylab.github.com/)

## Contact

For security-related questions or concerns:

- Security Email: **Dan Shue**
- General Issues: [GitHub Issues](https://github.com/danielshue/notebook-automation/issues)
- Discussions: [GitHub Discussions](https://github.com/danielshue/notebook-automation/discussions)

---

**Last updated:** July 2025
