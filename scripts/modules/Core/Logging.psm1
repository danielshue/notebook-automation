<#
.SYNOPSIS
    Unified logging and output functions for PowerShell scripts.

.DESCRIPTION
    This module provides consistent, colored console output functions used across
    all Notebook Automation PowerShell scripts. It standardizes logging patterns
    and provides conditional output based on verbosity settings.

.NOTES
    Module: Core.Logging
    Version: 1.0.0
#>

# Export module members at the end of this file

<#
.SYNOPSIS
    Writes a success message to the console with green formatting.

.PARAMETER Message
    The success message to display.

.EXAMPLE
    Write-Success "Build completed successfully"
#>
function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

<#
.SYNOPSIS
    Writes an error message to the console with red formatting.

.PARAMETER Message
    The error message to display.

.EXAMPLE
    Write-Error "Build failed"
#>
function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

<#
.SYNOPSIS
    Writes a warning message to the console with yellow formatting.

.PARAMETER Message
    The warning message to display.

.EXAMPLE
    Write-Warning "Configuration file not found, using defaults"
#>
function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

<#
.SYNOPSIS
    Writes a step/section header to the console with cyan formatting.

.PARAMETER Message
    The step message to display.

.EXAMPLE
    Write-Step "Building solution"
#>
function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

<#
.SYNOPSIS
    Writes colored output to the console.

.PARAMETER Message
    The message to display.

.PARAMETER Color
    The console color to use (default: White).

.EXAMPLE
    Write-ColoredOutput "Processing files..." "Yellow"
#>
function Write-ColoredOutput {
    param(
        [string]$Message,
        [System.ConsoleColor]$Color = [System.ConsoleColor]::White
    )
    
    if ($Color) {
        Write-Host $Message -ForegroundColor $Color
    }
    else {
        Write-Host $Message
    }
}

<#
.SYNOPSIS
    Writes output conditionally based on the -Quiet flag.

.PARAMETER Message
    The message to display.

.PARAMETER ForegroundColor
    The console color to use (default: White).

.EXAMPLE
    Write-ConditionalHost "Starting process..." -ForegroundColor Cyan
#>
function Write-ConditionalHost {
    param(
        [string]$Message,
        [string]$ForegroundColor = "White",
        [string]$BackgroundColor = ""
    )
    
    # Check if script-level $Quiet variable is set
    if (-not $script:Quiet -and -not $global:Quiet) {
        $params = @{
            Object = $Message
        }
        
        if ($ForegroundColor) {
            $params.ForegroundColor = $ForegroundColor
        }
        
        if ($BackgroundColor) {
            $params.BackgroundColor = $BackgroundColor
        }
        
        Write-Host @params
    }
}

<#
.SYNOPSIS
    Writes verbose/diagnostic output conditionally.

.PARAMETER Message
    The verbose message to display.

.PARAMETER ForegroundColor
    The console color to use (default: DarkGray).

.EXAMPLE
    Write-VerboseHost "Checking file: $filePath"
#>
function Write-VerboseHost {
    param(
        [string]$Message,
        [string]$ForegroundColor = "DarkGray"
    )
    
    # Check if script-level $Diagnostic variable is set
    if ($script:Diagnostic -or $global:Diagnostic) {
        Write-Host $Message -ForegroundColor $ForegroundColor
    }
}

# Export all public functions
Export-ModuleMember -Function @(
    'Write-Success',
    'Write-Error',
    'Write-Warning',
    'Write-Step',
    'Write-ColoredOutput',
    'Write-ConditionalHost',
    'Write-VerboseHost'
)
