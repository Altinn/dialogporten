#!/bin/bash

# Usage help
display_usage() {
    echo "Usage: $0 [OPTIONS] FILEPATH"
    echo
    echo "Options:"
    echo "-e|--environment               Either 'localdev', 'dev', or 'staging' (required)"
    echo "-a|--api-version               Defaults to 'v1' if not supplied (optional)"
    echo "-u|--token-generator-username  Username to Altinn Token Generator (required)"
    echo "-p|--token-generator-password  Password to Altinn Token Generator (required)"
    echo "-h|--help                      Displays this help"
    echo
    echo "Example:"
    echo "./run.sh --token-generator-username=supersecret --token-generator-password=supersecret -e localdev testsuite.js"
}

# Default value for API version
API_VERSION="v1"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
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
            # Assume the file path is the last argument
            FILEPATH="$1"
            shift
            ;;
    esac
done

# Check required options
if [[ -z $API_ENVIRONMENT || -z $TOKEN_GENERATOR_USERNAME || -z $TOKEN_GENERATOR_PASSWORD ]]; then
    echo "Error: Some required options are missing."
    display_usage
    exit 1
fi

# Check if valid environment
if [[ "$API_ENVIRONMENT" != "localdev" && "$API_ENVIRONMENT" != "dev" && "$API_ENVIRONMENT" != "staging" ]]; then
    echo "Error: Invalid environment value. Must be 'localdev', 'dev', or 'staging'."
    exit 1
fi

# Check if the file path exists
if [[ ! -f "$FILEPATH" ]]; then
    echo "Error: File '$FILEPATH' does not exist."
    exit 1
fi

# Execute k6 with options as environment variables
API_ENVIRONMENT=$API_ENVIRONMENT \
API_VERSION=$API_VERSION \
TOKEN_GENERATOR_USERNAME=$TOKEN_GENERATOR_USERNAME \
TOKEN_GENERATOR_PASSWORD=$TOKEN_GENERATOR_PASSWORD \
k6 run "$FILEPATH"

