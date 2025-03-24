#!/usr/bin/env bash

set -euo pipefail

# Constants
readonly PRODUCT_TAG="Dialogporten"
readonly DEFAULT_POSTGRES_PORT=5432
readonly DEFAULT_REDIS_PORT=6379
readonly VALID_ENVIRONMENTS=("test" "yt01" "staging" "prod")
readonly VALID_DB_TYPES=("postgres" "redis")
readonly SUBSCRIPTION_PREFIX="Dialogporten"

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
        "test"|"yt01")  echo "${SUBSCRIPTION_PREFIX}-Test"     ;;
        "staging")      echo "${SUBSCRIPTION_PREFIX}-Staging"   ;;
        "prod")         echo "${SUBSCRIPTION_PREFIX}-Prod"      ;;
        *)              echo ""                                 ;;
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

# Resource naming helper functions
get_resource_group() {
    local env=$1
    echo "dp-be-${env}-rg"
}

get_jumper_vm_name() {
    local env=$1
    echo "dp-be-${env}-ssh-jumper"
}

validate_environment() {
    local env=$1
    for valid_env in "${VALID_ENVIRONMENTS[@]}"; do
        if [[ "$env" == "$valid_env" ]]; then
            return 0
        fi
    done
    log_error "Invalid environment: $env"
    log_info "Valid environments: ${VALID_ENVIRONMENTS[*]}"
    exit 1
}

validate_db_type() {
    local db_type=$1
    for valid_type in "${VALID_DB_TYPES[@]}"; do
        if [[ "$db_type" == "$valid_type" ]]; then
            return 0
        fi
    done
    log_error "Invalid database type: $db_type"
    log_info "Valid database types: ${VALID_DB_TYPES[*]}"
    exit 1
}

get_postgres_info() {
    local env=$1
    local subscription_id=$2
    
    log_info "Fetching PostgreSQL server information..."
    local name
    name=$(az postgres flexible-server list --subscription "$subscription_id" \
        --query "[?tags.Environment=='$env' && tags.Product=='$PRODUCT_TAG'] | [0].name" -o tsv)
    
    if [ -z "$name" ]; then
        log_error "Postgres server not found"
        exit 1
    fi
    
    local hostname="${name}.postgres.database.azure.com"
    local port=$DEFAULT_POSTGRES_PORT
    
    local username
    username=$(az postgres flexible-server show \
        --resource-group "$(get_resource_group "$env")" \
        --name "$name" \
        --query "administratorLogin" -o tsv)
    
    echo "name=$name"
    echo "hostname=$hostname"
    echo "port=$port"
    echo "connection_string=postgresql://${username}:<retrieve-password-from-keyvault>@localhost:${local_port:-$port}/dialogporten"
}

get_redis_info() {
    local env=$1
    local subscription_id=$2
    
    log_info "Fetching Redis server information..."
    local name
    name=$(az redis list --subscription "$subscription_id" \
        --query "[?tags.Environment=='$env' && tags.Product=='$PRODUCT_TAG'] | [0].name" -o tsv)
    
    if [ -z "$name" ]; then
        log_error "Redis server not found"
        exit 1
    fi
    
    local hostname="${name}.redis.cache.windows.net"
    local port=$DEFAULT_REDIS_PORT

    echo "name=$name"
    echo "hostname=$hostname"
    echo "port=$port"
    echo "connection_string=redis://:<retrieve-password-from-keyvault>@${hostname}:${local_port:-$port}"
}

setup_ssh_tunnel() {
    local env=$1
    local hostname=$2
    local remote_port=$3
    local local_port=${4:-$remote_port}
    
    log_info "Starting SSH tunnel..."
    log_info "Connecting to ${hostname}:${remote_port} via local port ${local_port}"
    
    az ssh vm \
        -g "$(get_resource_group "$env")" \
        -n "$(get_jumper_vm_name "$env")" \
        -- -L "${local_port}:${hostname}:${remote_port}"
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
        log_info "Please select target environment:"
        environment=$(prompt_selection "Environment (1-${#VALID_ENVIRONMENTS[@]}): " "${VALID_ENVIRONMENTS[@]}")
    fi
    validate_environment "$environment"
    
    # If db_type is not provided, prompt for it
    if [ -z "$db_type" ]; then
        log_info "Please select database type:"
        db_type=$(prompt_selection "Database (1-${#VALID_DB_TYPES[@]}): " "${VALID_DB_TYPES[@]}")
    fi
    validate_db_type "$db_type"
    
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
Local Port: ${local_port:-$port}
Remote Port: ${port}

Connection String:
${BOLD}${connection_string}${NC}"
    
    setup_ssh_tunnel "$environment" "${hostname}" "${port}" "${local_port:-$port}"
}

# Parse command line arguments
environment=""
db_type=""
local_port=""

while getopts "e:t:p:h" opt; do
    case $opt in
        e) environment="$OPTARG" ;;
        t) db_type="$OPTARG" ;;
        p) local_port="$OPTARG" ;;
        h)
            echo "Usage: $0 [-e environment] [-t database_type] [-p local_port]"
            echo "  -e: Environment (${VALID_ENVIRONMENTS[*]})"
            echo "  -t: Database type (${VALID_DB_TYPES[*]})"
            echo "  -p: Local port to forward to (defaults to remote port)"
            exit 0
            ;;
        *)
            echo "Invalid option: -$OPTARG" >&2
            exit 1
            ;;
    esac
done

main "$environment" "$db_type"

