#!/bin/bash

# Usage help
display_usage() {
    echo "Usage: $0 [OPTIONS] FILEPATH [K6OPTIONS]"
    echo
    echo "Options:"
    echo "-e|--environment               Either 'localdev', 'test', 'yt01', 'staging' or 'prod' (required)"
    echo "-a|--api-version               Defaults to 'v1' if not supplied (optional)"
    echo "-u|--token-generator-username  Username to Altinn Token Generator (required)"
    echo "-p|--token-generator-password  Password to Altinn Token Generator (required)"
    echo "-f|--force-docker              Use Docker even if k6 is available"
    echo "-h|--help                      Displays this help"
    echo
    echo "Example:"
    echo "./run.sh --token-generator-username=supersecret --token-generator-password=supersecret -e localdev suites/all-single-pass.js --http-debug"
}

# Default value for API version
API_VERSION="v1"
FORCE_DOCKER=false

declare -a K6_ARGS

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        -f|--force-docker)
            FORCE_DOCKER=true
            shift
            ;;
        -e|--environment)
            API_ENVIRONMENT="$2"
            shift
            shift
            ;;
        -a|--api-version)
            API_VERSION="$2"
            shift
            shift
            ;;
        -u|--token-generator-username)
            TOKEN_GENERATOR_USERNAME="$2"
            shift
            shift
            ;;
        -p|--token-generator-password)
            TOKEN_GENERATOR_PASSWORD="$2"
            shift
            shift
            ;;
        -h|--help)
            display_usage
            exit 0
            ;;
        *)
             # Capture all remaining arguments into the K6_ARGS array
            K6_ARGS+=("$1")
            shift
            ;;
    esac
done

FILEPATH=${K6_ARGS[0]}
unset K6_ARGS[0] # Remove FILEPATH from the array

# Check required options
if [[ -z $API_ENVIRONMENT || -z $TOKEN_GENERATOR_USERNAME || -z $TOKEN_GENERATOR_PASSWORD ]]; then
    echo "Error: Some required options are missing."
    display_usage
    exit 1
fi

# Check if valid environment
if [[ "$API_ENVIRONMENT" != "localdev" && "$API_ENVIRONMENT" != "prod" && "$API_ENVIRONMENT" != "test" && "$API_ENVIRONMENT" != "yt01" && "$API_ENVIRONMENT" != "staging" ]]; then
    echo "Error: Invalid environment value. Must be 'localdev', 'prod', 'test', 'yt01' or 'staging'."
    exit 1
fi

# Check if the file path exists
if [[ ! -f "$FILEPATH" ]]; then
    echo "Error: File '$FILEPATH' does not exist."
    exit 1
fi

K6_AVAILABLE=true
if ! which k6 > /dev/null 2>&1; then
    K6_AVAILABLE=false
fi

DOCKER_AVAILABLE=true
if ! which docker > /dev/null 2>&1; then
    DOCKER_AVAILABLE=false
fi

if [ "$K6_AVAILABLE" = false ] && [ "$DOCKER_AVAILABLE" = false ]; then
    echo "Error: Both k6 and docker are not available. Please install one of them before proceeding."
    exit 1
fi

DIR="$(dirname "$0")"

"$DIR/scripts/generate_all_tests.sh" "$DIR/tests/serviceowner/" >/dev/null

if [[ "$API_ENVIRONMENT" == "localdev" ]]; then
    # Handle self-signed certs when using docker compose
    K6_INSECURE_SKIP_TLS_VERIFY=true
fi

if { [ "$K6_AVAILABLE" = true ] && [ "$FORCE_DOCKER" = false ] ; } || [ "$DOCKER_AVAILABLE" = false ]; then
    echo "Using local k6 installation"
    # Execute k6 with options as environment variables
    API_ENVIRONMENT=$API_ENVIRONMENT \
    API_VERSION=$API_VERSION \
    TOKEN_GENERATOR_USERNAME=$TOKEN_GENERATOR_USERNAME \
    TOKEN_GENERATOR_PASSWORD=$TOKEN_GENERATOR_PASSWORD \
    k6 run "${K6_ARGS[@]}" "$FILEPATH" 
else
    echo "Using dockerized k6"
    FILEPATH=$(echo "$FILEPATH" | sed 's:\\:/:g') # Replace \ with /
    EXTERNAL_PATH="$DIR/"
    INTERNAL_PATH="/k6-scripts/"
    
    DOCKER_ARGS=(
        "run" "--rm" "-i"
        "-v" "${EXTERNAL_PATH}:${INTERNAL_PATH}"
        "-e" "API_ENVIRONMENT=$API_ENVIRONMENT"
        "-e" "API_VERSION=$API_VERSION"
        "-e" "TOKEN_GENERATOR_USERNAME=$TOKEN_GENERATOR_USERNAME"
        "-e" "TOKEN_GENERATOR_PASSWORD=$TOKEN_GENERATOR_PASSWORD"
        "-e" "IS_DOCKER=true"
    )
    
    if [ "$API_ENVIRONMENT" == "localdev" ]; then
        DOCKER_ARGS+=("-e" "K6_INSECURE_SKIP_TLS_VERIFY=true")
    fi
    
    DOCKER_ARGS+=("grafana/k6" "run" "${K6_ARGS[@]}" "$INTERNAL_PATH$FILEPATH")
    
    docker "${DOCKER_ARGS[@]}"
fi
