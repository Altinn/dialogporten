#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Oppdaterer avhengighetsoversikten for Dialogporten

.DESCRIPTION
    Dette scriptet scanner kodebasen og oppdaterer Dependencies.md med aktuelle versjoner
    av alle avhengigheter, inkludert NuGet-pakker, Docker images og Azure-ressurser.

.PARAMETER OutputPath
    Sti til Dependencies.md filen som skal oppdateres. Standard: docs/Dependencies.md

.PARAMETER Validate
    Kun valider eksisterende avhengigheter uten å oppdatere dokumentasjon

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

# Funksjoner for å hente avhengighetsinformasjon
function Get-NuGetPackages {
    Write-Host "🔍 Skanner NuGet-pakker..." -ForegroundColor Green
    
    $packages = @{}
    $csprojFiles = Get-ChildItem -Path "." -Recurse -Name "*.csproj"
    
    foreach ($file in $csprojFiles) {
        Write-Verbose "Prosesserer: $file"
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
    Write-Host "🐳 Skanner Docker images..." -ForegroundColor Green
    
    $images = @{}
    $composeFiles = Get-ChildItem -Path "." -Name "docker-compose*.yml"
    
    foreach ($file in $composeFiles) {
        Write-Verbose "Prosesserer: $file"
        $content = Get-Content $file -Raw
        
        # Enkel regex for å finne image-linjer
        $imageMatches = [regex]::Matches($content, 'image:\s*([^\s\r\n]+)')
        
        foreach ($match in $imageMatches) {
            $imageName = $match.Groups[1].Value
            if ($imageName -notlike "*dialogporten*") {  # Skip egne images
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
    Write-Host "⚙️ Sjekker .NET versjon..." -ForegroundColor Green
    
    $globalJson = "global.json"
    if (Test-Path $globalJson) {
        $config = Get-Content $globalJson | ConvertFrom-Json
        return $config.sdk.version
    }
    
    return $null
}

function Test-PackageUpdates {
    Write-Host "🔄 Sjekker for oppdateringer..." -ForegroundColor Green
    
    try {
        # Sjekk om dotnet outdated er installert
        $outdatedTool = dotnet tool list -g | Select-String "dotnet-outdated-tool"
        if (-not $outdatedTool) {
            Write-Warning "dotnet-outdated-tool er ikke installert. Installer med: dotnet tool install -g dotnet-outdated-tool"
            return @{}
        }
        
        # Kjør dotnet outdated på solution-nivå
        Write-Verbose "Sjekker oppdateringer for hele solution..."
        
        # Kjør dotnet-outdated og la den lage "json" fil
        $null = & dotnet-outdated "Digdir.Domain.Dialogporten.sln" --output json 2>$null
        
        # Sjekk om "json" filen ble opprettet
        if (Test-Path "json") {
            $jsonContent = Get-Content "json" -Raw -ErrorAction SilentlyContinue
            if ($jsonContent -and $jsonContent.Trim().StartsWith("{")) {
                try {
                    $outdatedData = $jsonContent | ConvertFrom-Json
                    Remove-Item "json" -Force -ErrorAction SilentlyContinue
                    return $outdatedData
                }
                catch {
                    Write-Verbose "Kunne ikke parse JSON: $($_.Exception.Message)"
                }
            }
            Remove-Item "json" -Force -ErrorAction SilentlyContinue
        }
        
        return @{}
    }
    catch {
        Write-Warning "Kunne ikke sjekke for oppdateringer: $($_.Exception.Message)"
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
    
    Write-Host "📝 Oppdaterer Dependencies.md..." -ForegroundColor Green
    
    if (-not (Test-Path $FilePath)) {
        Write-Error "Dependencies.md finnes ikke på: $FilePath"
        return
    }
    
    $content = Get-Content $FilePath -Raw
    $currentDate = Get-Date -Format "yyyy-MM-dd HH:mm"
    
    # Oppdater timestamp
    $content = $content -replace '\*Sist oppdatert: .*\*', "*Sist oppdatert: $currentDate*"
    
    # Oppdater .NET versjon
    $content = $content -replace '\| \.NET SDK \| [0-9\.]+ \|', "| .NET SDK | $DotNetVersion |"
    
    # Generer oppdatert NuGet-tabell
    $nugetTableHeader = @"
### Automatisk oppdatert NuGet-oversikt

*Generert automatisk fra alle .csproj filer*

| Pakke | Versjon | Antall filer |
|-------|---------|--------------|
"@
    
    $nugetRows = @()
    foreach ($pkg in ($NuGetPackages.GetEnumerator() | Sort-Object Key)) {
        $fileCount = if ($pkg.Value.Files) { $pkg.Value.Files.Count } else { 1 }
        $nugetRows += "| **$($pkg.Key)** | $($pkg.Value.Version) | $fileCount |"
    }
    
    $nugetTable = $nugetTableHeader + "`n" + ($nugetRows -join "`n")
    
    # Generer Docker images tabell
    $dockerTableHeader = @"

### Automatisk oppdatert Docker-oversikt

*Generert fra docker-compose filer*

| Image | Versjon | Fil |
|-------|---------|-----|
"@
    
    $dockerRows = @()
    foreach ($img in ($DockerImages.GetEnumerator() | Sort-Object Key)) {
        $dockerRows += "| **$($img.Key)** | $($img.Value.Version) | $($img.Value.File) |"
    }
    
    $dockerTable = $dockerTableHeader + "`n" + ($dockerRows -join "`n")
    
    # Finn eller lag seksjonen for automatisk genererte tabeller
    $autoSectionPattern = '## 🤖 Automatisk genererte avhengigheter[\s\S]*?(?=##|$)'
    $newAutoSection = @"
## 🤖 Automatisk genererte avhengigheter

*Denne seksjonen oppdateres automatisk av Update-Dependencies.ps1*

$nugetTable

$dockerTable

"@
    
    if ($content -match $autoSectionPattern) {
        $content = $content -replace $autoSectionPattern, $newAutoSection
    } else {
        # Legg til seksjonen før den siste linjen
        $content = $content -replace '(\*Dette dokumentet er automatisk generert.*\*)', "$newAutoSection`n`$1"
    }
    
    Set-Content -Path $FilePath -Value $content -Encoding UTF8
    Write-Host "✅ Dependencies.md oppdatert med $(($NuGetPackages.Count)) NuGet-pakker og $(($DockerImages.Count)) Docker images!" -ForegroundColor Green
}

function Show-DependencySummary {
    param(
        [hashtable]$NuGetPackages,
        [hashtable]$DockerImages,
        [string]$DotNetVersion
    )
    
    Write-Host "`n📊 AVHENGIGHETSSAMMENDRAG" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host ".NET SDK Versjon: $DotNetVersion" -ForegroundColor White
    Write-Host "NuGet-pakker: $($NuGetPackages.Count)" -ForegroundColor White
    Write-Host "Docker images: $($DockerImages.Count)" -ForegroundColor White
    
    Write-Host "`n🔝 MEST BRUKTE NUGET-PAKKER:" -ForegroundColor Yellow
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

# Hovedlogikk
try {
    Write-Host "🚀 Starter avhengighetsanalyse for Dialogporten..." -ForegroundColor Magenta
    Write-Host "=================================================" -ForegroundColor Magenta
    
    # Sjekk at vi er i riktig katalog
    if (-not (Test-Path "Digdir.Domain.Dialogporten.sln")) {
        Write-Error "Dette scriptet må kjøres fra rot-katalogen til Dialogporten-repositoryet"
        exit 1
    }
    
    # Samle inn avhengighetsinformasjon
    $nugetPackages = Get-NuGetPackages
    $dockerImages = Get-DockerImages
    $dotnetVersion = Get-DotNetVersion
    
    # Vis sammendrag
    Show-DependencySummary -NuGetPackages $nugetPackages -DockerImages $dockerImages -DotNetVersion $dotnetVersion
    
    if ($Validate) {
        Write-Host "`n✅ Validering fullført!" -ForegroundColor Green
        
        # Sjekk for potensielle oppdateringer
        Write-Host "`n🔍 Sjekker for oppdateringer..." -ForegroundColor Yellow
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
            
            Write-Host "`n📊 TILGJENGELIGE OPPDATERINGER:" -ForegroundColor Yellow
            Write-Host "Total: $totalUpdates pakker" -ForegroundColor White
            Write-Host "  🔴 Major: $majorUpdates (mulige breaking changes)" -ForegroundColor Red
            Write-Host "  🟡 Minor: $minorUpdates (nye features)" -ForegroundColor Yellow  
            Write-Host "  🟢 Patch: $patchUpdates (bugfixes)" -ForegroundColor Green
            
            if ($majorUpdates -gt 0) {
                Write-Host "`n⚠️  VIKTIG: $majorUpdates major oppdateringer krever ekstra testing!" -ForegroundColor Red
            }
        }
        else {
            Write-Host "✅ Alle pakker er oppdaterte!" -ForegroundColor Green
        }
    }
    else {
        # Oppdater dokumentasjon
        Update-DependenciesDoc -NuGetPackages $nugetPackages -DockerImages $dockerImages -DotNetVersion $dotnetVersion -FilePath $OutputPath
    }
    
    Write-Host "`n🎉 Ferdig!" -ForegroundColor Green
}
catch {
    Write-Error "Feil under kjøring: $($_.Exception.Message)"
    Write-Error $_.ScriptStackTrace
    exit 1
}

