#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Updates the dependency overview for Dialogporten

.DESCRIPTION
    This script scans the codebase and updates Dependencies.md with current versions
    of NuGet packages, Docker images, and .NET SDK versions.

.PARAMETER OutputPath
    Path to the Dependencies.md file to be updated. Default: docs/Dependencies.md

.PARAMETER Validate
    Only validate existing dependencies without updating documentation

.EXAMPLE
    .\scripts\Update-Dependencies.ps1
    
.EXAMPLE
    .\scripts\Update-Dependencies.ps1 -Validate
#>

param(
    [string]$OutputPath = "docs/Dependencies.md",
    [switch]$Validate
)

$ErrorActionPreference = "Stop"

# Functions for retrieving dependency information
function Get-NuGetPackages {
    Write-Host "🔍 Scanning NuGet packages..." -ForegroundColor Green
    
    $packages = @{}
    $csprojFiles = Get-ChildItem -Path "." -Recurse -Name "*.csproj"
    
    foreach ($file in $csprojFiles) {
        Write-Verbose "Processing: $file"
        [xml]$proj = Get-Content $file
        
        $packageRefs = $proj.Project.ItemGroup.PackageReference
        if ($packageRefs) {
            foreach ($pkg in $packageRefs) {
                if ($pkg.Include -and $pkg.Version) {
                    $packages[$pkg.Include] = @{
                        Version = $pkg.Version
                        Files = @($file)
                    }
                }
            }
        }
    }
    
    return $packages
}

function Get-DockerImages {
    Write-Host "🐳 Scanning Docker images..." -ForegroundColor Green
    
    $images = @{}
    $composeFiles = Get-ChildItem -Path "." -Name "docker-compose*.yml"
    
    foreach ($file in $composeFiles) {
        Write-Verbose "Processing: $file"
        $content = Get-Content $file -Raw
        
        # Simple regex to find image lines
        $imageMatches = [regex]::Matches($content, 'image:\s*([^\s\r\n]+)')
        
        foreach ($match in $imageMatches) {
            $imageName = $match.Groups[1].Value
            if ($imageName -notlike "*dialogporten*") {  # Skip our own images
                $parts = $imageName -split ":"
                $name = $parts[0]
                $version = if ($parts.Count -gt 1) { $parts[1] } else { "latest" }
                
                $images[$name] = @{
                    Version = $version
                    File = $file
                }
            }
        }
    }
    
    return $images
}

function Get-DotNetVersion {
    Write-Host "⚙️ Checking .NET version..." -ForegroundColor Green
    
    $globalJson = "global.json"
    if (Test-Path $globalJson) {
        $config = Get-Content $globalJson | ConvertFrom-Json
        return $config.sdk.version
    }
    
    return $null
}

function Test-PackageUpdates {
    Write-Host "🔄 Checking for updates..." -ForegroundColor Green
    
    try {
        # Check if dotnet outdated is installed
        $outdatedTool = dotnet tool list -g | Select-String "dotnet-outdated-tool"
        if (-not $outdatedTool) {
            Write-Warning "dotnet-outdated-tool is not installed. Install with: dotnet tool install -g dotnet-outdated-tool"
            return @{}
        }
        
        # Run dotnet outdated at solution level
        Write-Verbose "Checking updates for entire solution..."
        
        # Run dotnet-outdated and let it create "json" file
        $null = & dotnet-outdated "Digdir.Domain.Dialogporten.sln" --output json 2>$null
        
        # Check if "json" file was created
        if (Test-Path "json") {
            $jsonContent = Get-Content "json" -Raw -ErrorAction SilentlyContinue
            if ($jsonContent -and $jsonContent.Trim().StartsWith("{")) {
                try {
                    $outdatedData = $jsonContent | ConvertFrom-Json
                    Remove-Item "json" -Force -ErrorAction SilentlyContinue
                    return $outdatedData
                }
                catch {
                    Write-Verbose "Could not parse JSON: $($_.Exception.Message)"
                }
            }
            Remove-Item "json" -Force -ErrorAction SilentlyContinue
        }
        
        return @{}
    }
    catch {
        Write-Warning "Could not check for updates: $($_.Exception.Message)"
        return @{}
    }
}

function Update-DependenciesDoc {
    param(
        [hashtable]$NuGetPackages,
        [hashtable]$DockerImages,
        [string]$DotNetVersion,
        [string]$FilePath
    )
    
    Write-Host "📝 Updating Dependencies.md..." -ForegroundColor Green
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "Dependencies.md not found at: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $currentDate = Get-Date -Format "yyyy-MM-dd HH:mm"
    
    # Update timestamp
    $content = $content -replace '\*Last updated: .*\*', "*Last updated: $currentDate*"
    
    # Update .NET version
    $content = $content -replace '\| \.NET SDK \| [0-9\.]+ \|', "| .NET SDK | $DotNetVersion |"
    
    # Generate updated NuGet table
    $nugetTableHeader = @"
### Automatically Updated NuGet Overview

*Generated automatically from all .csproj files*

| Package | Version |
|---------|---------|
"@
    
    $nugetRows = @()
    foreach ($pkg in ($NuGetPackages.GetEnumerator() | Sort-Object Key)) {
        $nugetRows += "| **$($pkg.Key)** | $($pkg.Value.Version) |"
    }
    
    $nugetTable = $nugetTableHeader + "`n" + ($nugetRows -join "`n")
    
    # Generate Docker images table
    $dockerTableHeader = @"

### Automatically Updated Docker Overview

*Generated from docker-compose files*

| Image | Version | File |
|-------|---------|------|
"@
    
    $dockerRows = @()
    foreach ($img in ($DockerImages.GetEnumerator() | Sort-Object Key)) {
        $dockerRows += "| **$($img.Key)** | $($img.Value.Version) | $($img.Value.File) |"
    }
    
    $dockerTable = $dockerTableHeader + "`n" + ($dockerRows -join "`n")
    
    # Find or create section for automatically generated tables
    $autoSectionPattern = '## 🤖 [^#]*[\s\S]*?(?=---\s*\*This document is automatically generated|\z)'
    $newAutoSection = @"
## 🤖 Auto-generated Dependencies

*This section is automatically updated by Update-Dependencies.ps1*

$nugetTable

$dockerTable

"@
    
    if ($content -match $autoSectionPattern) {
        $content = $content -replace $autoSectionPattern, $newAutoSection
    } else {
        # Add section before the last line
        $content = $content -replace '(\*This document is automatically generated.*\*)', "$newAutoSection`n`$1"
    }
    
    Set-Content -Path $FilePath -Value $content -Encoding UTF8
    Write-Host "✅ Dependencies.md updated with $(($NuGetPackages.Count)) NuGet packages and $(($DockerImages.Count)) Docker images!" -ForegroundColor Green
}

function Show-DependencySummary {
    param(
        [hashtable]$NuGetPackages,
        [hashtable]$DockerImages,
        [string]$DotNetVersion
    )
    
    Write-Host "`n📊 DEPENDENCY SUMMARY" -ForegroundColor Cyan
    Write-Host "=====================" -ForegroundColor Cyan
    Write-Host ".NET SDK Version: $DotNetVersion" -ForegroundColor White
    Write-Host "NuGet packages: $($NuGetPackages.Count)" -ForegroundColor White
    Write-Host "Docker images: $($DockerImages.Count)" -ForegroundColor White
    
    Write-Host "`n🔝 MOST USED NUGET PACKAGES:" -ForegroundColor Yellow
    $topPackages = $NuGetPackages.GetEnumerator() | 
        Where-Object { $_.Key -like "Microsoft.*" -or $_.Key -like "System.*" } |
        Sort-Object Key |
        Select-Object -First 10
    
    foreach ($pkg in $topPackages) {
        Write-Host "  • $($pkg.Key): $($pkg.Value.Version)" -ForegroundColor Gray
    }
    
    Write-Host "`n🐳 DOCKER IMAGES:" -ForegroundColor Yellow
    foreach ($img in $DockerImages.GetEnumerator() | Sort-Object Key) {
        Write-Host "  • $($img.Key): $($img.Value.Version)" -ForegroundColor Gray
    }
}

# Main logic
try {
    Write-Host "🚀 Starting dependency analysis for Dialogporten..." -ForegroundColor Magenta
    Write-Host "=================================================" -ForegroundColor Magenta
    
    # Check that we are in the correct directory
    if (-not (Test-Path "Digdir.Domain.Dialogporten.sln")) {
        Write-Error "This script must be run from the root directory of the Dialogporten repository"
        exit 1
    }
    
    # Collect dependency information
    $nugetPackages = Get-NuGetPackages
    $dockerImages = Get-DockerImages
    $dotnetVersion = Get-DotNetVersion
    
    # Show summary
    Show-DependencySummary -NuGetPackages $nugetPackages -DockerImages $dockerImages -DotNetVersion $dotnetVersion
    
    if ($Validate) {
        Write-Host "`n✅ Validation completed!" -ForegroundColor Green
        
        # Check for potential updates
        Write-Host "`n🔍 Checking for updates..." -ForegroundColor Yellow
        $outdatedData = Test-PackageUpdates
        
        if ($outdatedData -and $outdatedData.Projects) {
            $totalUpdates = 0
            $majorUpdates = 0
            $minorUpdates = 0
            $patchUpdates = 0
            
            foreach ($project in $outdatedData.Projects) {
                foreach ($framework in $project.TargetFrameworks) {
                    foreach ($dep in $framework.Dependencies) {
                        $totalUpdates++
                        switch ($dep.UpgradeSeverity) {
                            "Major" { $majorUpdates++ }
                            "Minor" { $minorUpdates++ }
                            "Patch" { $patchUpdates++ }
                        }
                    }
                }
            }
            
            Write-Host "`n📊 AVAILABLE UPDATES:" -ForegroundColor Yellow
            Write-Host "Total: $totalUpdates packages" -ForegroundColor White
            Write-Host "  🔴 Major: $majorUpdates (possible breaking changes)" -ForegroundColor Red
            Write-Host "  🟡 Minor: $minorUpdates (new features)" -ForegroundColor Yellow  
            Write-Host "  🟢 Patch: $patchUpdates (bugfixes)" -ForegroundColor Green
            
            if ($majorUpdates -gt 0) {
                Write-Host "`n⚠️  IMPORTANT: $majorUpdates major updates require extra testing!" -ForegroundColor Red
            }
        }
        else {
            Write-Host "✅ All packages are up to date!" -ForegroundColor Green
        }
    }
    else {
        # Update documentation
        Update-DependenciesDoc -NuGetPackages $nugetPackages -DockerImages $dockerImages -DotNetVersion $dotnetVersion -FilePath $OutputPath
    }
    
    Write-Host "`n🎉 Done!" -ForegroundColor Green
}
catch {
    Write-Error "Error during execution: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    exit 1
}

