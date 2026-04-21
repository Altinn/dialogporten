param (
    [Parameter(Mandatory=$true)]
    [ValidateSet('localdev','test','yt01','staging','prod')]
    [string]$ApiEnvironment,

    [string]$ApiVersion = 'v1',

    [Parameter(Mandatory=$true)]
    [string]$TokenGeneratorUsername,

    [Parameter(Mandatory=$true)]
    [String]$TokenGeneratorPassword,

    [Parameter(Mandatory=$true)]
    [string]$FilePath,

    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$K6Args,

    [switch]$ForceDocker
)

# Check if k6 is available
$k6Available = $true
try {
    Get-Command k6 -ErrorAction Stop > $null
} catch {
    $k6Available = $false
}

# Check if docker is available
$dockerAvailable = $true
try {
    Get-Command docker -ErrorAction Stop > $null
} catch {
    $dockerAvailable = $false
}

# Decide execution strategy
if (-not $k6Available -and -not $dockerAvailable) {
    Write-Error "Error: Both k6 and docker are not available. Please install one of them before proceeding."
    exit 1
}

# Check for file existence
if (-not (Test-Path $FilePath)) {
    Write-Error "Error: File '$FilePath' does not exist."
    exit 1
}

# Generate tests
& "$PSScriptRoot\scripts\generate_all_tests.ps1" "$PSScriptRoot\tests\serviceowner\" > $null

# Handle environment settings
$insecureSkipTLS = $null
if ($ApiEnvironment -eq "localdev") {
    # Handle self-signed certs when using docker compose
    $env:K6_INSECURE_SKIP_TLS_VERIFY = "true"
    $insecureSkipTLS = "K6_INSECURE_SKIP_TLS_VERIFY=true"
}

$env:API_ENVIRONMENT = $ApiEnvironment
$env:API_VERSION = $ApiVersion
$env:TOKEN_GENERATOR_USERNAME = $TokenGeneratorUsername
$env:TOKEN_GENERATOR_PASSWORD = $TokenGeneratorPassword

# Run k6 or docker
if (($k6Available -and -not $ForceDocker) -or (-not $dockerAvailable)) {
    Write-Host "Using local k6 installation"
    k6 run @K6Args $FilePath
} else {
    Write-Host "Using dockerized k6"

    $FilePath = $FilePath.Replace('\', '/')

    $ExternalPath = $PSScriptRoot + "\"
    $InternalPath = "/k6-scripts/"

    $dockerArgs = @(
        "run", "--rm", "-i",
        "-v", "${ExternalPath}:${InternalPath}",
        "-e", "API_ENVIRONMENT=$ApiEnvironment",
        "-e", "API_VERSION=$ApiVersion",
        "-e", "TOKEN_GENERATOR_USERNAME=$TokenGeneratorUsername",
        "-e", "TOKEN_GENERATOR_PASSWORD=$TokenGeneratorPassword",
        "-e", "IS_DOCKER=true" # Required to in order to replace "localhost" with "host.docker.internal" when testing locally
    )

    if ($insecureSkipTLS) {
        $dockerArgs += "-e", $insecureSkipTLS
    }

    $dockerArgs += "grafana/k6", "run"
    $dockerArgs += $K6Args + $InternalPath + $FilePath

    #Write-Host docker $dockerArgs
    docker $dockerArgs
}
