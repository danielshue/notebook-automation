<#
.SYNOPSIS
    Checksum generation and validation functions.

.DESCRIPTION
    This module provides functions to generate and validate SHA256 checksums
    for cross-platform executables, ensuring file integrity.

.NOTES
    Module: Quality.Checksums
    Version: 1.0.0
#>

<#
.SYNOPSIS
    Generates or validates a checksums.json file for executables.

.DESCRIPTION
    Creates a checksums.json file with SHA256 hashes for all cross-platform executables.
    If the file already exists, validates that existing checksums match current files.

.PARAMETER DistPath
    Path to the directory containing executables.

.PARAMETER Version
    Version string to include in the checksums file.

.PARAMETER Algorithm
    Hash algorithm to use (default: SHA256).

.PARAMETER ExpectedExecutables
    Array of expected executable names.

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation or generation fails.

.EXAMPLE
    $checksumsPath = New-OrValidateChecksumsFile -DistPath "./dist" -Version "1.0.0"

.OUTPUTS
    String - Path to the checksums.json file, or $null on failure.
#>
function New-OrValidateChecksumsFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath,
        
        [Parameter(Mandatory = $true)]
        [string]$Version,
        
        [string]$Algorithm = "SHA256",
        
        [string[]]$ExpectedExecutables = @(
            'na-win-x64.exe', 'na-win-arm64.exe',
            'na-linux-x64', 'na-linux-arm64',
            'na-macos-x64', 'na-macos-arm64'
        ),
        
        [switch]$ThrowOnFailure
    )
    
    # Validate dist directory exists
    if (-not (Test-Path $DistPath)) {
        $message = "Dist directory not found: $DistPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    # Check for all expected executables
    $executables = Get-ChildItem -Path $DistPath -File | Where-Object { $_.Name -in $ExpectedExecutables }
    $missing = $ExpectedExecutables | Where-Object { $_ -notin $executables.Name }
    
    if ($missing) {
        $message = "Cannot create checksums.json - missing executables: $($missing -join ', ')"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $null
    }
    
    $checksumsPath = Join-Path $DistPath 'checksums.json'
    
    # Generate hash map
    $hashMap = @{}
    foreach ($exe in $executables) {
        $hash = (Get-FileHash -Algorithm $Algorithm -Path $exe.FullName).Hash.ToLowerInvariant()
        $hashMap[$exe.Name] = $hash
    }
    
    # If checksums file exists, validate it
    if (Test-Path $checksumsPath) {
        try {
            $existing = Get-Content $checksumsPath -Raw | ConvertFrom-Json
            $existingFiles = $existing.files | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
            
            # Validate presence of all expected files
            foreach ($name in $ExpectedExecutables) {
                if ($name -notin $existingFiles) {
                    throw "checksums.json missing entry for $name"
                }
            }
            
            # Validate hash equality
            foreach ($name in $ExpectedExecutables) {
                $currentHash = $hashMap[$name]
                $recorded = $existing.files.$name
                
                if ($currentHash -ne $recorded) {
                    throw "Checksum mismatch for $name (recorded=$recorded actual=$currentHash)"
                }
            }
            
            Write-Host "✅ Existing checksums.json verified" -ForegroundColor Green
            return $checksumsPath
        }
        catch {
            $message = "checksums.json validation failed: $($_.Exception.Message)"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
    else {
        # Create new checksums file
        try {
            $payload = [ordered]@{
                version      = $Version
                algorithm    = $Algorithm
                generatedUtc = (Get-Date).ToUniversalTime().ToString('o')
                files        = $hashMap
            }
            
            ($payload | ConvertTo-Json -Depth 5) | Set-Content -Path $checksumsPath -Encoding UTF8
            Write-Host "🧾 Generated checksums.json" -ForegroundColor Green
            return $checksumsPath
        }
        catch {
            $message = "Failed to create checksums.json: $($_.Exception.Message)"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
            return $null
        }
    }
}

<#
.SYNOPSIS
    Validates checksums for files against a checksums.json file.

.DESCRIPTION
    Compares actual file checksums against the checksums recorded in checksums.json.

.PARAMETER ChecksumsPath
    Path to the checksums.json file.

.PARAMETER DistPath
    Path to the directory containing the files to validate.

.PARAMETER ThrowOnFailure
    If true, throws an exception when validation fails.

.EXAMPLE
    $valid = Test-ChecksumsFile -ChecksumsPath "./dist/checksums.json" -DistPath "./dist"

.OUTPUTS
    Boolean - $true if all checksums are valid, $false otherwise.
#>
function Test-ChecksumsFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ChecksumsPath,
        
        [Parameter(Mandatory = $true)]
        [string]$DistPath,
        
        [switch]$ThrowOnFailure
    )
    
    if (-not (Test-Path $ChecksumsPath)) {
        $message = "Checksums file not found: $ChecksumsPath"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
    
    try {
        $checksums = Get-Content $ChecksumsPath -Raw | ConvertFrom-Json
        $algorithm = $checksums.algorithm
        $files = $checksums.files | Get-Member -MemberType NoteProperty | Select-Object -ExpandProperty Name
        
        $allValid = $true
        foreach ($fileName in $files) {
            $filePath = Join-Path $DistPath $fileName
            
            if (-not (Test-Path $filePath)) {
                Write-Host "✗ File not found: $fileName" -ForegroundColor Red
                $allValid = $false
                continue
            }
            
            $actualHash = (Get-FileHash -Algorithm $algorithm -Path $filePath).Hash.ToLowerInvariant()
            $expectedHash = $checksums.files.$fileName
            
            if ($actualHash -eq $expectedHash) {
                Write-Host "✅ $fileName - checksum valid" -ForegroundColor Green
            }
            else {
                Write-Host "❌ $fileName - checksum mismatch!" -ForegroundColor Red
                Write-Host "   Expected: $expectedHash" -ForegroundColor Gray
                Write-Host "   Actual:   $actualHash" -ForegroundColor Gray
                $allValid = $false
            }
        }
        
        if ($allValid) {
            Write-Host "✅ All checksums are valid" -ForegroundColor Green
        }
        else {
            $message = "Checksum validation failed for one or more files"
            if ($ThrowOnFailure) {
                throw $message
            }
            Write-Host "✗ $message" -ForegroundColor Red
        }
        
        return $allValid
    }
    catch {
        $message = "Failed to validate checksums: $($_.Exception.Message)"
        if ($ThrowOnFailure) {
            throw $message
        }
        Write-Host "✗ $message" -ForegroundColor Red
        return $false
    }
}

<#
.SYNOPSIS
    Generates SHA256 checksum for a single file.

.PARAMETER FilePath
    Path to the file.

.PARAMETER Algorithm
    Hash algorithm to use (default: SHA256).

.EXAMPLE
    $hash = Get-FileChecksum -FilePath "./na-win-x64.exe"

.OUTPUTS
    String - The checksum hash in lowercase hexadecimal format.
#>
function Get-FileChecksum {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        
        [string]$Algorithm = "SHA256"
    )
    
    if (-not (Test-Path $FilePath)) {
        throw "File not found: $FilePath"
    }
    
    $hash = (Get-FileHash -Algorithm $Algorithm -Path $FilePath).Hash.ToLowerInvariant()
    return $hash
}

# Export all public functions
Export-ModuleMember -Function @(
    'New-OrValidateChecksumsFile',
    'Test-ChecksumsFile',
    'Get-FileChecksum'
)
