# =========================================================================
# Database Connection Forwarder for Dialogporten (PowerShell Version)
#
# Sets up secure SSH tunnels to Azure database resources using a jumper VM.
# Supports PostgreSQL and Redis connections across environments.
# =========================================================================

using namespace System.Management.Automation.Host

# =========================================================================
# Constants
# =========================================================================
$script:PRODUCT_TAG = "Dialogporten"
$script:DEFAULT_POSTGRES_PORT = 5432
$script:DEFAULT_REDIS_PORT = 6379
$script:VALID_ENVIRONMENTS = @("test", "yt01", "staging", "prod")
$script:VALID_DB_TYPES = @("postgres", "redis")
$script:SUBSCRIPTION_PREFIX = "Dialogporten"
$script:JIT_DURATION = "PT1H"  # 1 hour duration for JIT access

# Colors for console output
$script:Colors = @{
    Blue = [ConsoleColor]::Blue
    Green = [ConsoleColor]::Green
    Yellow = [ConsoleColor]::Yellow
    Red = [ConsoleColor]::Red
    Cyan = [ConsoleColor]::Cyan
    White = [ConsoleColor]::White
}

# =========================================================================
# Utility Functions
# =========================================================================

function Write-ColorMessage {
    param(
        [string]$Message,
        [ConsoleColor]$Color,
        [string]$Prefix = "",
        [switch]$NoNewline
    )
    
    $originalColor = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $Color
    if ($NoNewline) {
        Write-Host -NoNewline "$Prefix$Message"
    } else {
        Write-Host "$Prefix$Message"
    }
    $host.UI.RawUI.ForegroundColor = $originalColor
}

function Write-Info {
    param([string]$Message)
    Write-ColorMessage -Prefix "ℹ " -Message $Message -Color $Colors.Blue
}

function Write-Success {
    param([string]$Message)
    Write-ColorMessage -Prefix "✓ " -Message $Message -Color $Colors.Green
}

function Write-Warning {
    param([string]$Message)
    Write-ColorMessage -Prefix "⚠ " -Message $Message -Color $Colors.Yellow
}

function Write-Error {
    param([string]$Message)
    Write-ColorMessage -Prefix "✖ " -Message $Message -Color $Colors.Red
}

function Write-Title {
    param([string]$Title)
    Write-Host ""
    Write-ColorMessage -Message $Title -Color $Colors.Cyan
}

function Write-Box {
    param(
        [string]$Title,
        [string]$Content
    )
    
    $width = 70
    $padding = 2
    
    # Top border
    Write-Host ("╭" + "─" * $width)
    
    # Title
    Write-Host "│$(" " * $padding)$Title$(" " * ($width - $Title.Length - $padding))│"
    
    # Empty line
    Write-Host "│$(" " * $width)│"
    
    # Content
    foreach ($line in $Content -split "`n") {
        if ([string]::IsNullOrEmpty($line)) {
            Write-Host "│$(" " * $width)│"
        } else {
            Write-Host "│$(" " * $padding)$line$(" " * ($width - $line.Length - $padding))│"
        }
    }
    
    # Bottom border
    Write-Host ("╰" + "─" * $width)
}

function Show-Selection {
    param(
        [string]$Prompt,
        [array]$Options
    )
    
    Write-Host ""
    for ($i = 0; $i -lt $Options.Count; $i++) {
        Write-Host "$($i + 1)) $($Options[$i])"
    }
    
    while ($true) {
        $selection = Read-Host -Prompt $Prompt
        if ($selection -match '^\d+$' -and [int]$selection -ge 1 -and [int]$selection -le $Options.Count) {
            return $Options[[int]$selection - 1]
        }
        Write-Warning "Invalid selection. Please try again."
    }
}

# =========================================================================
# Validation Functions
# =========================================================================

function Test-Environment {
    param([string]$Environment)
    
    if ($Environment -in $VALID_ENVIRONMENTS) {
        return $true
    }
    
    Write-Error "Invalid environment: $Environment"
    Write-Info "Valid environments: $($VALID_ENVIRONMENTS -join ', ')"
    return $false
}

function Test-DbType {
    param([string]$DbType)
    
    if ($DbType -in $VALID_DB_TYPES) {
        return $true
    }
    
    Write-Error "Invalid database type: $DbType"
    Write-Info "Valid database types: $($VALID_DB_TYPES -join ', ')"
    return $false
}

function Test-Port {
    param([string]$Port)
    
    if ($Port -match '^\d+$' -and [int]$Port -ge 1 -and [int]$Port -le 65535) {
        return $true
    }
    
    Write-Error "Port must be a number between 1 and 65535"
    return $false
}

# =========================================================================
# Azure Functions
# =========================================================================

function Test-Dependencies {
    try {
        $null = Get-Command az -ErrorAction Stop
        Write-Success "Azure CLI is installed"
        return $true
    }
    catch {
        Write-Error "Azure CLI is not installed. Please visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        return $false
    }
}

function Get-SubscriptionName {
    param([string]$Environment)
    
    switch ($Environment) {
        { $_ -in @("test", "yt01") } { return "$SUBSCRIPTION_PREFIX-Test" }
        "staging" { return "$SUBSCRIPTION_PREFIX-Staging" }
        "prod" { return "$SUBSCRIPTION_PREFIX-Prod" }
        default { return "" }
    }
}

function Get-SubscriptionId {
    param([string]$Environment)
    
    $subscriptionName = Get-SubscriptionName -Environment $Environment
    if ([string]::IsNullOrEmpty($subscriptionName)) {
        Write-Error "Invalid environment: $Environment"
        return $null
    }
    
    $subId = (az account show --subscription $subscriptionName --query id -o tsv 2>$null)
    if ([string]::IsNullOrEmpty($subId)) {
        Write-Error "Could not find subscription '$subscriptionName'. Please ensure you are logged in to the correct Azure account."
        return $null
    }
    
    return $subId
}

function Get-ResourceGroup {
    param([string]$Environment)
    return "dp-be-$Environment-rg"
}

function Get-JumperVmName {
    param([string]$Environment)
    return "dp-be-$Environment-ssh-jumper"
}

function Set-JitAccess {
    param(
        [string]$Environment,
        [string]$SubscriptionId
    )
    
    Write-Info "Configuring JIT access..."
    
    # Get public IP
    Write-Info "Detecting your public IP address..."
    try {
        $myIp = (Invoke-RestMethod -Uri "https://ipinfo.io/json").ip
        $myIp2 = (Invoke-RestMethod -Uri "https://api.ipify.org")
        
        if ($myIp -ne $myIp2) {
            Write-Error "Inconsistent IP addresses detected:"
            Write-Error "ipinfo.io: $myIp"
            Write-Error "ipify.org: $myIp2"
            return $false
        }
        
        Write-Success "Public IP detected: $myIp"
    }
    catch {
        Write-Error "Failed to detect public IP address"
        return $false
    }
    
    # Get VM details
    Write-Info "Fetching VM details..."
    $resourceGroup = Get-ResourceGroup -Environment $Environment
    $vmName = Get-JumperVmName -Environment $Environment
    
    $vmId = (az vm show --resource-group $resourceGroup --name $vmName --query "id" -o tsv)
    if ([string]::IsNullOrEmpty($vmId)) {
        Write-Error "Failed to get VM ID for $vmName in resource group $resourceGroup"
        return $false
    }
    Write-Success "Found VM with ID: $vmId"
    
    $location = (az vm show --resource-group $resourceGroup --name $vmName --query "location" -o tsv)
    if ([string]::IsNullOrEmpty($location)) {
        Write-Error "Failed to get location for VM $vmName"
        return $false
    }
    Write-Success "VM is located in: $location"
    
    # Construct JIT request
    $endpoint = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$resourceGroup/providers/Microsoft.Security/locations/$location/jitNetworkAccessPolicies/${vmName}/initiate?api-version=2020-01-01"
    
    $body = @{
        virtualMachines = @(
            @{
                id = $vmId
                ports = @(
                    @{
                        number = 22
                        duration = $JIT_DURATION
                        allowedSourceAddressPrefix = "$myIp/32"
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 10
    
    Write-Info "Requesting JIT access..."
    Write-Info "Using endpoint: $endpoint"
    Write-Host ""
    
    try {
        $null = az rest --method post --uri $endpoint --headers "Content-Type=application/json" --body $body
        Write-Success "JIT access configured successfully (valid for 1 hour)"
        return $true
    }
    catch {
        Write-Error "Failed to configure JIT access. Error: $_"
        Write-Info "Please ensure you have the necessary permissions and that JIT access is enabled for this VM"
        return $false
    }
}

# =========================================================================
# Database Functions
# =========================================================================

function Get-PostgresInfo {
    param(
        [string]$Environment,
        [string]$SubscriptionId
    )
    
    Write-Info "Fetching PostgreSQL server information..."
    
    $name = (az postgres flexible-server list --subscription $SubscriptionId --query "[?tags.Environment=='$Environment' && tags.Product=='$PRODUCT_TAG'] | [0].name" -o tsv)
    if ([string]::IsNullOrEmpty($name)) {
        Write-Error "Postgres server not found"
        return $null
    }
    
    $hostname = "$name.postgres.database.azure.com"
    $port = $DEFAULT_POSTGRES_PORT
    
    $username = (az postgres flexible-server show --resource-group (Get-ResourceGroup -Environment $Environment) --name $name --query "administratorLogin" -o tsv)
    
    return @{
        name = $name
        hostname = $hostname
        port = $port
        connection_string = "postgresql://${username}:<retrieve-password-from-keyvault>@localhost:$port/dialogporten"
    }
}

function Get-RedisInfo {
    param(
        [string]$Environment,
        [string]$SubscriptionId
    )
    
    Write-Info "Fetching Redis server information..."
    
    $name = (az redis list --subscription $SubscriptionId --query "[?tags.Environment=='$Environment' && tags.Product=='$PRODUCT_TAG'] | [0].name" -o tsv)
    if ([string]::IsNullOrEmpty($name)) {
        Write-Error "Redis server not found"
        return $null
    }
    
    $hostname = "$name.redis.cache.windows.net"
    $port = $DEFAULT_REDIS_PORT
    
    return @{
        name = $name
        hostname = $hostname
        port = $port
        connection_string = "redis://:<retrieve-password-from-keyvault>@${hostname}:$port"
    }
}

function Start-SshTunnel {
    param(
        [string]$Environment,
        [string]$Hostname,
        [int]$RemotePort,
        [int]$LocalPort
    )
    
    Write-Info "Starting SSH tunnel..."
    Write-Info "Connecting to ${Hostname}:${RemotePort} via local port ${LocalPort}"
    
    az ssh vm -g (Get-ResourceGroup -Environment $Environment) -n (Get-JumperVmName -Environment $Environment) -- -L "${LocalPort}:${Hostname}:${RemotePort}"
}

# =========================================================================
# Easter Eggs
# =========================================================================

function Show-CoffeeBreak {
    Write-Host @"
   ( (
    ) )
  ........
  |      |]
  \      /
   `----'

Time for a coffee break! ☕
Remember: Database connections are like good coffee - they should be secure and well-filtered.
"@
    Start-Sleep -Seconds 2
}

# =========================================================================
# Main Function
# =========================================================================

function Start-DatabaseForwarder {
    param(
        [string]$Environment,
        [string]$DbType,
        [string]$LocalPort
    )
    
    # Handle Ctrl+C gracefully
    $null = [Console]::TreatControlCAsInput = $true
    
    Write-Title "Database Connection Forwarder"
    
    if (-not (Test-Dependencies)) {
        exit 1
    }
    
    # If environment is not provided, prompt for it
    if ([string]::IsNullOrEmpty($Environment)) {
        Write-Info "Please select target environment:"
        $Environment = Show-Selection -Prompt "Environment (1-$($VALID_ENVIRONMENTS.Count)): " -Options $VALID_ENVIRONMENTS
    }
    if (-not (Test-Environment -Environment $Environment)) {
        exit 1
    }
    
    # If db_type is not provided, prompt for it
    if ([string]::IsNullOrEmpty($DbType)) {
        Write-Info "Please select database type:"
        $DbType = Show-Selection -Prompt "Database (1-$($VALID_DB_TYPES.Count)): " -Options $VALID_DB_TYPES
    }
    if (-not (Test-DbType -DbType $DbType)) {
        exit 1
    }
    
    # If local_port is not provided, prompt for it
    if ([string]::IsNullOrEmpty($LocalPort)) {
        $defaultPort = if ($DbType -eq "postgres") { $DEFAULT_POSTGRES_PORT } else { $DEFAULT_REDIS_PORT }
        do {
            Write-Info "Select the local port to bind on localhost (127.0.0.1)"
            $LocalPort = Read-Host -Prompt "Port to bind on localhost (default: $defaultPort)"
            if ([string]::IsNullOrEmpty($LocalPort)) {
                $LocalPort = $defaultPort
            }
        } while (-not (Test-Port -Port $LocalPort))
    }
    elseif (-not (Test-Port -Port $LocalPort)) {
        exit 1
    }
    
    # Print confirmation
    Write-Box -Title "Configuration" -Content @"
Environment: $Environment
Database:    $DbType
Local Port:  $LocalPort
"@
    
    $confirm = Read-Host -Prompt "Proceed? (y/N)"
    if ($confirm -notmatch '^[Yy]$') {
        Write-Warning "Operation cancelled by user"
        exit 0
    }
    
    Write-Info "Setting up connection for $Environment environment"
    
    $subscriptionId = Get-SubscriptionId -Environment $Environment
    if ([string]::IsNullOrEmpty($subscriptionId)) {
        exit 1
    }
    
    $null = az account set --subscription $subscriptionId 2>$null
    Write-Success "Azure subscription set"
    
    # Get database information based on database type
    $resourceInfo = if ($DbType -eq "postgres") {
        Get-PostgresInfo -Environment $Environment -SubscriptionId $subscriptionId
    }
    else {
        Get-RedisInfo -Environment $Environment -SubscriptionId $subscriptionId
    }
    
    if ($null -eq $resourceInfo) {
        exit 1
    }
    
    # Print connection details
    Write-Box -Title "$($DbType.ToUpper()) Connection Info" -Content @"
Server:     $($resourceInfo.hostname)
Local Port: $LocalPort
Remote Port: $($resourceInfo.port)

Connection String:
$($resourceInfo.connection_string)
"@
    
    # Configure JIT access
    if (-not (Set-JitAccess -Environment $Environment -SubscriptionId $subscriptionId)) {
        exit 1
    }
    
    # Set up the SSH tunnel
    Start-SshTunnel -Environment $Environment -Hostname $resourceInfo.hostname -RemotePort $resourceInfo.port -LocalPort $LocalPort
}

# =========================================================================
# Script Entry Point
# =========================================================================

# Parse command line arguments
param(
    [Parameter(Position = 0)]
    [string]$Environment,
    
    [Parameter(Position = 1)]
    [string]$DbType,
    
    [Parameter(Position = 2)]
    [string]$LocalPort,
    
    [switch]$Help,
    
    [switch]$Coffee
)

if ($Help) {
    Get-Help $MyInvocation.MyCommand.Path
    exit 0
}

if ($Coffee) {
    Show-CoffeeBreak
    exit 0
}

Start-DatabaseForwarder -Environment $Environment -DbType $DbType -LocalPort $LocalPort 