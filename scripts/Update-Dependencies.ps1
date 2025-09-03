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
    
    # First, restore packages to ensure we have resolved versions
    Write-Host "📦 Restoring packages to get resolved versions..." -ForegroundColor Yellow
    try {
        dotnet restore --verbosity quiet
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Package restore failed, falling back to .csproj parsing"
            return Get-NuGetPackagesFromCsproj
        }
    }
    catch {
        Write-Warning "Package restore failed, falling back to .csproj parsing"
        return Get-NuGetPackagesFromCsproj
    }
    
    # Use dotnet list package to get resolved versions
    Write-Host "🔍 Getting resolved package versions..." -ForegroundColor Yellow
    try {
        $packageListOutput = dotnet list package --include-transitive --format json 2>$null
        if ($LASTEXITCODE -eq 0 -and $packageListOutput) {
            $packageData = $packageListOutput | ConvertFrom-Json
            
            foreach ($project in $packageData.projects) {
                if ($project.frameworks) {
                    foreach ($framework in $project.frameworks) {
                        if ($framework.topLevelPackages) {
                            foreach ($pkg in $framework.topLevelPackages) {
                                if ($pkg.id -and $pkg.resolvedVersion) {
                                    if ($packages.ContainsKey($pkg.id)) {
                                        # Package already exists - validate version consistency and add file
                                        if ($packages[$pkg.id].Version -ne $pkg.resolvedVersion) {
                                            Write-Warning "Version mismatch for $($pkg.id): $($packages[$pkg.id].Version) vs $($pkg.resolvedVersion)"
                                        }
                                        # Add file if not already present
                                        if ($packages[$pkg.id].Files -notcontains $project.path) {
                                            $packages[$pkg.id].Files += $project.path
                                        }
                                    }
                                    else {
                                        # First occurrence - initialize entry
                                        $packages[$pkg.id] = @{
                                            Version = $pkg.resolvedVersion
                                            Files = @($project.path)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        else {
            Write-Warning "Failed to get resolved versions, falling back to .csproj parsing"
            return Get-NuGetPackagesFromCsproj
        }
    }
    catch {
        Write-Warning "Failed to parse resolved versions, falling back to .csproj parsing"
        return Get-NuGetPackagesFromCsproj
    }
    
    return $packages
}

function Get-NuGetPackagesFromCsproj {
    Write-Host "📄 Falling back to .csproj parsing..." -ForegroundColor Yellow
    
    $packages = @{}
    $csprojFiles = Get-ChildItem -Path "." -Recurse -Name "*.csproj"
    
    foreach ($file in $csprojFiles) {
        Write-Verbose "Processing: $file"
        [xml]$proj = Get-Content $file
        
        $packageRefs = $proj.Project.ItemGroup.PackageReference
        if ($packageRefs) {
            foreach ($pkg in $packageRefs) {
                if ($pkg.Include -and $pkg.Version) {
                    if ($packages.ContainsKey($pkg.Include)) {
                        # Package already exists - validate version consistency and add file
                        if ($packages[$pkg.Include].Version -ne $pkg.Version) {
                            Write-Warning "Version mismatch for $($pkg.Include): $($packages[$pkg.Include].Version) vs $($pkg.Version)"
                        }
                        # Add file if not already present
                        if ($packages[$pkg.Include].Files -notcontains $file) {
                            $packages[$pkg.Include].Files += $file
                        }
                    }
                    else {
                        # First occurrence - initialize entry
                        $packages[$pkg.Include] = @{
                            Version = $pkg.Version
                            Files = @($file)
                        }
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
        
        # Match image: field specifically, handling quotes and registry prefixes
        $imageMatches = [regex]::Matches($content, '^\s*image:\s*[''"]?([^\s''"]+)[''"]?\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline)
        
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
        
        # Create temporary file for dotnet-outdated output
        $tempFile = [IO.Path]::GetTempFileName()
        
        try {
            # Run dotnet-outdated and output to temporary file
            $null = & dotnet-outdated "Digdir.Domain.Dialogporten.sln" --output $tempFile 2>$null
            
            # Check if temporary file was created and has content
            if (Test-Path $tempFile) {
                $jsonContent = Get-Content $tempFile -Raw -ErrorAction SilentlyContinue
                if ($jsonContent -and $jsonContent.Trim().StartsWith("{")) {
                    try {
                        $outdatedData = $jsonContent | ConvertFrom-Json
                        return $outdatedData
                    }
                    catch {
                        Write-Verbose "Could not parse JSON: $($_.Exception.Message)"
                    }
                }
            }
        }
        finally {
            # Always clean up the temporary file
            if (Test-Path $tempFile) {
                Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
            }
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
    if ($content -notmatch '\*Last updated: ') {
        Write-Warning "Could not update timestamp - pattern not found"
    }
    
    # Update .NET version
    $content = $content -replace '\| \.NET SDK \| [0-9\.]+ \|', "| .NET SDK | $DotNetVersion |"
    if ($content -notmatch '\| \.NET SDK \|') {
        Write-Warning "Could not update .NET SDK version - pattern not found"
    }
    
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
        
        # Validate documentation patterns
        Write-Host "`n🔍 Validating documentation patterns..." -ForegroundColor Yellow
        $docContent = Get-Content $OutputPath -Raw -ErrorAction SilentlyContinue
        
        if ($docContent) {
            $validationIssues = @()
            
            # Check for required patterns
            if ($docContent -notmatch '\*Last updated: ') {
                $validationIssues += "Missing timestamp pattern"
            }
            if ($docContent -notmatch '\| \.NET SDK \|') {
                $validationIssues += "Missing .NET SDK table"
            }
            if ($docContent -notmatch '## 🤖 Auto-generated Dependencies') {
                $validationIssues += "Missing auto-generated section"
            }
            
            if ($validationIssues.Count -eq 0) {
                Write-Host "✅ All documentation patterns found" -ForegroundColor Green
            }
            else {
                Write-Host "❌ Documentation validation issues:" -ForegroundColor Red
                foreach ($issue in $validationIssues) {
                    Write-Host "  • $issue" -ForegroundColor Red
                }
            }
        }
        else {
            Write-Host "❌ Could not read documentation file: $OutputPath" -ForegroundColor Red
        }
        
        # Validate file aggregation
        Write-Host "`n🔍 Validating file aggregation..." -ForegroundColor Yellow
        $multiFilePackages = $nugetPackages.GetEnumerator() | Where-Object { $_.Value.Files.Count -gt 1 }
        if ($multiFilePackages) {
            Write-Host "✅ Found $($multiFilePackages.Count) packages used in multiple projects:" -ForegroundColor Green
            foreach ($pkg in $multiFilePackages) {
                Write-Host "  • $($pkg.Key): $($pkg.Value.Files.Count) files" -ForegroundColor White
            }
        }
        else {
            Write-Host "ℹ️  No packages found in multiple projects" -ForegroundColor Yellow
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

