<#
.SYNOPSIS
    .NET build and publish operations module.

.DESCRIPTION
    This module provides functions for building, restoring, cleaning, and publishing
    .NET projects and solutions with consistent error handling and retry logic.

.NOTES
    Module: Build.DotNetBuild
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Restores NuGet packages for a .NET solution or project.

.PARAMETER Path
    Path to the solution (.sln) or project (.csproj) file.

.PARAMETER ThrowOnFailure
    If true, throws an exception when restore fails.

.EXAMPLE
    Invoke-DotNetRestore -Path "MyApp.sln" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if restore succeeded, $false otherwise.
#>
function Invoke-DotNetRestore {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $Path)) {
        $message = "Solution or project file not found: $Path"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    dotnet restore $Path
    
    if ($LASTEXITCODE -ne 0) {
        $message = "dotnet restore failed with exit code $LASTEXITCODE"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    return $true
}

<#
.SYNOPSIS
    Cleans build outputs for a .NET solution or project.

.PARAMETER Path
    Path to the solution (.sln) or project (.csproj) file.

.PARAMETER Configuration
    Build configuration (Debug/Release). Default is "Release".

.PARAMETER ThrowOnFailure
    If true, throws an exception when clean fails.

.EXAMPLE
    Invoke-DotNetClean -Path "MyApp.sln" -Configuration "Release"

.OUTPUTS
    Boolean - $true if clean succeeded, $false otherwise.
#>
function Invoke-DotNetClean {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [ValidateSet("Debug", "Release")]
        [string]$Configuration = "Release",
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $Path)) {
        $message = "Solution or project file not found: $Path"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    dotnet clean $Path --configuration $Configuration
    
    if ($LASTEXITCODE -ne 0) {
        $message = "dotnet clean failed with exit code $LASTEXITCODE"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    return $true
}

<#
.SYNOPSIS
    Builds a .NET solution or project.

.PARAMETER Path
    Path to the solution (.sln) or project (.csproj) file.

.PARAMETER Configuration
    Build configuration (Debug/Release). Default is "Release".

.PARAMETER NoRestore
    Skip automatic restore before building.

.PARAMETER Verbosity
    Build verbosity level (quiet, minimal, normal, detailed, diagnostic).

.PARAMETER ThrowOnFailure
    If true, throws an exception when build fails.

.EXAMPLE
    Invoke-DotNetBuild -Path "MyApp.sln" -Configuration "Release" -ThrowOnFailure

.OUTPUTS
    Boolean - $true if build succeeded, $false otherwise.
#>
function Invoke-DotNetBuild {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [ValidateSet("Debug", "Release")]
        [string]$Configuration = "Release",
        
        [switch]$NoRestore,
        
        [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
        [string]$Verbosity = "",
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $Path)) {
        $message = "Solution or project file not found: $Path"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $buildArgs = @($Path, "--configuration", $Configuration)
    
    if ($NoRestore) {
        $buildArgs += "--no-restore"
    }
    
    if ($Verbosity) {
        $buildArgs += @("--verbosity", $Verbosity)
    }
    
    & dotnet build @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        $message = "dotnet build failed with exit code $LASTEXITCODE"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    return $true
}

<#
.SYNOPSIS
    Publishes a .NET project with retry logic for resilience.

.PARAMETER ProjectPath
    Path to the project (.csproj) file.

.PARAMETER RuntimeId
    Runtime identifier (e.g., "win-x64", "linux-x64", "osx-arm64").

.PARAMETER Configuration
    Build configuration (Debug/Release). Default is "Release".

.PARAMETER OutputDir
    Output directory for published files.

.PARAMETER PackageVersion
    Package version for the build.

.PARAMETER FileVersion
    File version for the build.

.PARAMETER AssemblyVersion
    Assembly version for the build.

.PARAMETER PublishSingleFile
    Publish as a single file executable.

.PARAMETER SelfContained
    Create a self-contained deployment.

.PARAMETER MaxRetries
    Maximum number of retry attempts. Default is 3.

.PARAMETER ThrowOnFailure
    If true, throws an exception when all retries fail.

.EXAMPLE
    Invoke-DotNetPublishWithRetry -ProjectPath "App.csproj" -RuntimeId "win-x64" -OutputDir "./dist" -PackageVersion "1.0.0"

.OUTPUTS
    Boolean - $true if publish succeeded, $false otherwise.
#>
function Invoke-DotNetPublishWithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath,
        
        [Parameter(Mandatory = $true)]
        [string]$RuntimeId,
        
        [ValidateSet("Debug", "Release")]
        [string]$Configuration = "Release",
        
        [Parameter(Mandatory = $true)]
        [string]$OutputDir,
        
        [string]$PackageVersion = "",
        [string]$FileVersion = "",
        [string]$AssemblyVersion = "",
        
        [switch]$PublishSingleFile,
        [switch]$SelfContained,
        
        [int]$MaxRetries = 3,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $ProjectPath)) {
        $message = "Project file not found: $ProjectPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $attempt = 1
    while ($attempt -le $MaxRetries) {
        try {
            # Ensure fresh output directory for each attempt
            if (Test-Path $OutputDir) {
                Remove-Item -Recurse -Force $OutputDir -ErrorAction SilentlyContinue
            }
            New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
            
            # Build publish arguments
            $publishArgs = @(
                $ProjectPath,
                "-c", $Configuration,
                "-r", $RuntimeId,
                "--output", $OutputDir
            )
            
            if ($PublishSingleFile) {
                $publishArgs += "/p:PublishSingleFile=true"
            }
            
            if ($SelfContained) {
                $publishArgs += "/p:SelfContained=true"
            }
            
            if ($PackageVersion) {
                $publishArgs += "/p:Version=$PackageVersion"
            }
            
            if ($FileVersion) {
                $publishArgs += "/p:FileVersion=$FileVersion"
            }
            
            if ($AssemblyVersion) {
                $publishArgs += "/p:AssemblyVersion=$AssemblyVersion"
            }
            
            & dotnet publish @publishArgs
            
            if ($LASTEXITCODE -eq 0) {
                return $true
            }
            
            throw "dotnet publish returned exit code $LASTEXITCODE"
        }
        catch {
            $message = $_.Exception.Message
            
            if ($attempt -ge $MaxRetries) {
                $errorMsg = "Publish failed for $RuntimeId after $MaxRetries attempts: $message"
                if ($ThrowOnFailure) {
                    throw $errorMsg
                }
                Write-Host "✗ $errorMsg" -ForegroundColor Red
                return $false
            }
            
            Write-Host "⚠️  Publish failed for $RuntimeId (attempt $attempt/$MaxRetries): $message" -ForegroundColor Yellow
            Write-Host "   Applying targeted clean/restore and retrying..." -ForegroundColor Yellow
            
            # Targeted clean on the project
            try {
                Invoke-DotNetClean -Path $ProjectPath -Configuration $Configuration | Out-Null
            }
            catch { }
            
            # Remove bin/obj directories
            $projDir = Split-Path $ProjectPath -Parent
            foreach ($d in @('bin', 'obj')) {
                $p = Join-Path $projDir $d
                if (Test-Path $p) {
                    Remove-Item -Recurse -Force $p -ErrorAction SilentlyContinue
                }
            }
            
            # Restore
            try {
                Invoke-DotNetRestore -Path $ProjectPath | Out-Null
            }
            catch { }
            
            # Small backoff before retry
            $delay = [Math]::Min(3 + (($attempt - 1) * 2), 10)
            Start-Sleep -Seconds $delay
            $attempt++
        }
    }
    
    return $false
}

<#
.SYNOPSIS
    Formats .NET code using dotnet format.

.PARAMETER Path
    Path to the solution (.sln) or project (.csproj) file.

.PARAMETER VerifyOnly
    Only verify formatting without making changes.

.PARAMETER Verbosity
    Format verbosity level (quiet, minimal, normal, detailed, diagnostic).

.PARAMETER ThrowOnFailure
    If true, throws an exception when format fails.

.EXAMPLE
    Invoke-DotNetFormat -Path "MyApp.sln" -VerifyOnly

.OUTPUTS
    Boolean - $true if format succeeded or no changes needed, $false otherwise.
#>
function Invoke-DotNetFormat {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        
        [switch]$VerifyOnly,
        
        [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
        [string]$Verbosity = "",
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $Path)) {
        $message = "Solution or project file not found: $Path"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    $formatArgs = @($Path)
    
    if ($VerifyOnly) {
        $formatArgs += "--verify-no-changes"
    }
    
    if ($Verbosity) {
        $formatArgs += @("--verbosity", $Verbosity)
    }
    
    & dotnet format @formatArgs
    
    if ($LASTEXITCODE -ne 0) {
        $message = "dotnet format failed with exit code $LASTEXITCODE"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    return $true
}

# Export all public functions
Export-ModuleMember -Function @(
    'Invoke-DotNetRestore',
    'Invoke-DotNetClean',
    'Invoke-DotNetBuild',
    'Invoke-DotNetPublishWithRetry',
    'Invoke-DotNetFormat'
)
