#!/usr/bin/env bash

set -euo pipefail

# Colors and formatting
BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m' # No Color

# Replace associative array with a function
get_subscription_name() {
    local env=$1
    case "$env" in
        "test") echo "Dialogporten-Test" ;;
        "yt01") echo "Dialogporten-Test" ;;
        "staging") echo "Dialogporten-Staging" ;;
        "prod") echo "Dialogporten-Prod" ;;
        *) echo "" ;;
    esac
}

# Logging functions
log_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

log_success() {
    echo -e "${GREEN}✓${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

log_error() {
    echo -e "${RED}✖${NC} $1" >&2
}

log_title() {
    echo -e "\n${BOLD}${CYAN}$1${NC}"
}

print_box() {
    local title="$1"
    local content="$2"
    local width=55
    
    # Top border
    printf "╭%${width}s╮\n" | tr ' ' '─'
    
    # Title line with proper padding
    printf "│ ${BOLD}%s${NC}%$((width - ${#title} - 1))s│\n" "$title"
    
    # Empty line
    printf "│%${width}s│\n" ""
    
    # Content (handle multiple lines)
    while IFS= read -r line; do
        # Remove escape sequences for length calculation
        clean_line=$(echo -e "$line" | sed 's/\x1b\[[0-9;]*m//g')
        # Calculate padding needed
        padding=$((width - ${#clean_line} - 2))
        # Print line with proper padding
        printf "│ %b%${padding}s│\n" "$line" " "
    done <<< "$content"
    
    # Bottom border
    printf "╰%${width}s╯\n" | tr ' ' '─'
}

# Check prerequisites
check_dependencies() {
    if ! command -v az >/dev/null 2>&1; then
        log_error "Azure CLI is not installed. Please visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    fi
    log_success "Azure CLI is installed"
}

get_subscription_id() {
    local env=$1
    local subscription_name
    subscription_name=$(get_subscription_name "$env")
    
    if [ -z "$subscription_name" ]; then
        log_error "Invalid environment: $env"
        exit 1
    fi
    
    local sub_id
    sub_id=$(az account show --subscription "$subscription_name" --query id -o tsv 2>/dev/null)
    
    if [ -z "$sub_id" ]; then
        log_error "Could not find subscription '$subscription_name'. Please ensure you are logged in to the correct Azure account."
        exit 1
    fi
    
    echo "$sub_id"
}

get_postgres_info() {
    local env=$1
    local subscription_id=$2
    
    local name
    name=$(az postgres flexible-server list --subscription "$subscription_id" \
        --query "[?tags.Environment=='$env' && tags.Product=='Dialogporten'] | [0].name" -o tsv)
    
    if [ -z "$name" ]; then
        log_error "Postgres server not found"
        exit 1
    fi
    
    local hostname="${name}.postgres.database.azure.com"
    local port=5432
    
    local username
    username=$(az postgres flexible-server show \
        --resource-group "dp-be-${env}-rg" \
        --name "$name" \
        --query "administratorLogin" -o tsv)
    
    echo "name=$name"
    echo "hostname=$hostname"
    echo "port=$port"
    echo "connection_string=postgresql://${username}:<retrieve-password-from-keyvault>@localhost:${port}/dialogporten"
}

get_redis_info() {
    local env=$1
    local subscription_id=$2
    
    local name
    name=$(az redis list --subscription "$subscription_id" \
        --query "[?tags.Environment=='$env' && tags.Product=='Dialogporten'] | [0].name" -o tsv)
    
    if [ -z "$name" ]; then
        log_error "Redis server not found"
        exit 1
    fi
    
    local hostname="${name}.redis.cache.windows.net"
    local port=6379

    echo "name=$name"
    echo "hostname=$hostname"
    echo "port=$port"
    echo "connection_string=redis://:<retrieve-password-from-keyvault>@${hostname}:${port}"
}

setup_ssh_tunnel() {
    local env=$1
    local hostname=$2
    local port=$3
    
    log_info "Starting SSH tunnel..."
    az ssh vm \
        -g "dp-be-${env}-rg" \
        -n "dp-be-${env}-ssh-jumper" \
        -- -L "${port}:${hostname}:${port}"
}

prompt_selection() {
    local prompt=$1
    shift
    local options=("$@")
    local selected
    
    trap 'echo -e "\nOperation cancelled by user"; exit 130' INT
    
    PS3="$prompt "
    select selected in "${options[@]}"; do
        if [ -n "$selected" ]; then
            echo "$selected"
            return
        fi
    done
}

# Add this function near the top with other utility functions
to_upper() {
    echo "$1" | tr '[:lower:]' '[:upper:]'
}

# Main function
main() {
    local environment=$1
    local db_type=$2
    
    log_title "Database Connection Forwarder"
    
    check_dependencies
    
    # If environment is not provided, prompt for it
    if [ -z "$environment" ]; then
        environment=$(prompt_selection "Select environment (1-4):" "test" "yt01" "staging" "prod")
    fi
    
    # If db_type is not provided, prompt for it
    if [ -z "$db_type" ]; then
        db_type=$(prompt_selection "Select database type (1-2):" "postgres" "redis")
    fi
    
    # Print confirmation
    print_box "Configuration" "\
Environment: ${BOLD}${CYAN}${environment}${NC}
Database:    ${BOLD}${YELLOW}${db_type}${NC}"
    
    read -rp "Proceed? (y/N) " confirm
    if [[ ! $confirm =~ ^[Yy]$ ]]; then
        log_warning "Operation cancelled by user"
        exit 0
    fi
    
    log_info "Setting up connection for ${BOLD}${environment}${NC} environment"
    
    local subscription_id
    subscription_id=$(get_subscription_id "$environment")
    az account set --subscription "$subscription_id" >/dev/null 2>&1
    log_success "Azure subscription set"
    
    local resource_info
    if [ "$db_type" = "postgres" ]; then
        resource_info=$(get_postgres_info "$environment" "$subscription_id")
    else
        resource_info=$(get_redis_info "$environment" "$subscription_id")
    fi
    
    local hostname="" port="" connection_string=""
    while IFS='=' read -r key value; do
        case "$key" in
            "hostname") hostname="$value" ;;
            "port") port="$value" ;;
            "connection_string") connection_string="$value" ;;
        esac
    done <<< "$resource_info"
    
    if [ -z "$hostname" ] || [ -z "$port" ] || [ -z "$connection_string" ]; then
        log_error "Failed to get resource information"
        exit 1
    fi
    
    # Print connection details
    print_box "$(to_upper "$db_type") Connection Info" "\
Server:    ${hostname}
Port:      ${port}

Connection String:
${BOLD}${connection_string}${NC}"
    
    setup_ssh_tunnel "$environment" "${hostname}" "${port}"
}

# Parse command line arguments
environment=""
db_type=""

while getopts "e:t:h" opt; do
    case $opt in
        e) environment="$OPTARG" ;;
        t) db_type="$OPTARG" ;;
        h)
            echo "Usage: $0 [-e environment] [-t database_type]"
            echo "  -e: Environment (test, yt01, staging, prod)"
            echo "  -t: Database type (postgres, redis)"
            exit 0
            ;;
        *)
            echo "Invalid option: -$OPTARG" >&2
            exit 1
            ;;
    esac
done

main "$environment" "$db_type" 