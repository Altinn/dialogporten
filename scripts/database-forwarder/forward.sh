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

validate_port() {
    local port=$1
    
    # Check if the port is a number
    if ! [[ "$port" =~ ^[0-9]+$ ]]; then
        log_error "Port must be a number"
        return 1
    fi
    
    # Check if the port is within valid range (1-65535)
    if [ "$port" -lt 1 ] || [ "$port" -gt 65535 ]; then
        log_error "Port must be between 1 and 65535"
        return 1
    fi
    
    return 0
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

print_help() {
    cat << EOF
Database Connection Forwarder
============================
A tool to set up secure SSH tunnels to Azure database resources.

Usage:
    $0 [OPTIONS]

Options:
    -e, --environment ENV  Environment to connect to (${VALID_ENVIRONMENTS[*]})
                          Each environment maps to a specific Azure subscription:
                          - test/yt01  -> ${SUBSCRIPTION_PREFIX}-Test
                          - staging    -> ${SUBSCRIPTION_PREFIX}-Staging
                          - prod      -> ${SUBSCRIPTION_PREFIX}-Prod

    -t, --type TYPE       Database type to connect to (${VALID_DB_TYPES[*]})
                          - postgres: PostgreSQL Flexible Server (default port: $DEFAULT_POSTGRES_PORT)
                          - redis:    Redis Cache (default port: $DEFAULT_REDIS_PORT)

    -p, --port PORT       Local port to bind on localhost (127.0.0.1)
                          If not specified, will use the default port for the selected database

    -h, --help           Show this help message

Examples:
    # Interactive mode (will prompt for all options)
    $0

    # Connect to PostgreSQL in test environment
    $0 -e test -t postgres
    $0 --environment test --type postgres

    # Connect to Redis in prod with custom local port
    $0 -e prod -t redis -p 6380
    $0 --environment prod --type redis --port 6380

EOF
}

# Main function
main() {
    local environment=$1
    local db_type=$2
    local local_port=$3
    
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
    
    # If local_port is not provided, prompt for it
    if [ -z "$local_port" ]; then
        default_port=$([[ "$db_type" == "postgres" ]] && echo "$DEFAULT_POSTGRES_PORT" || echo "$DEFAULT_REDIS_PORT")
        while true; do
            log_info "Select the local port to bind on localhost (127.0.0.1)"
            read -rp "Port to bind on localhost (default: $default_port): " local_port
            local_port=${local_port:-$default_port}
            
            if validate_port "$local_port"; then
                break
            fi
        done
    else
        validate_port "$local_port" || exit 1
    fi
    
    # Print confirmation
    print_box "Configuration" "\
Environment: ${BOLD}${CYAN}${environment}${NC}
Database:    ${BOLD}${YELLOW}${db_type}${NC}
Local Port:  ${BOLD}${local_port:-"<default>"}${NC}"
    
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

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -e|--environment)
            environment="$2"
            shift 2
            ;;
        -t|--type)
            db_type="$2"
            shift 2
            ;;
        -p|--port)
            local_port="$2"
            shift 2
            ;;
        -h|--help)
            print_help
            exit 0
            ;;
        *)
            log_error "Invalid option: $1"
            log_info "Use -h or --help for help"
            exit 1
            ;;
    esac
done

# Call main with all arguments
main "${environment:-}" "${db_type:-}" "${local_port:-}"

