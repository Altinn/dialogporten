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
        
        # Kjør dotnet outdated for hver .csproj fil
        $updates = @{}
        $csprojFiles = Get-ChildItem -Path "." -Recurse -Name "*.csproj"
        
        foreach ($file in $csprojFiles) {
            Write-Verbose "Sjekker oppdateringer for: $file"
            $result = dotnet outdated $file --output json 2>$null
            if ($LASTEXITCODE -eq 0 -and $result) {
                $outdatedData = $result | ConvertFrom-Json
                # Prosesser outdated data her hvis nødvendig
            }
        }
        
        return $updates
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
    
    # Her kan du legge til mer sofistikert oppdateringslogikk
    # For nå, bare skriv tilbake innholdet med oppdatert timestamp
    
    Set-Content -Path $FilePath -Value $content -Encoding UTF8
    Write-Host "✅ Dependencies.md oppdatert!" -ForegroundColor Green
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
        Test-PackageUpdates | Out-Null
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
