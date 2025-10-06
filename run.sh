#!/bin/bash

# Lap Time Synthesizer - Run Script
# This script builds and runs the application with a race configuration file

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 [race-config-file]"
    echo
    echo "Arguments:"
    echo "  race-config-file    Path to race configuration YAML file (optional)"
    echo "                     If not provided, uses 'race.yml' from project root"
    echo
    echo "Examples:"
    echo "  $0                                    # Uses test-start-crossings.yml"
    echo "  $0 config/sample-race.yml            # Uses sample config"
    echo "  $0 my-custom-race.yml                # Uses custom config"
    echo
    echo "Available config files:"
    if [ -f "race.yml" ]; then
        echo "  • race.yml (default)"
    fi
    if [ -f "config/sample-race.yml" ]; then
        echo "  • config/sample-race.yml"
    fi
    echo
}

# Check if we're in the right directory
if [ ! -f "LapTimeSynth.sln" ]; then
    print_error "Please run this script from the project root directory (where LapTimeSynth.sln is located)"
    exit 1
fi

# Parse command line arguments
CONFIG_FILE=""
if [ $# -eq 0 ]; then
    # No arguments provided, use default
    CONFIG_FILE="race.yml"
elif [ $# -eq 1 ]; then
    # One argument provided, use it as config file
    CONFIG_FILE="$1"
else
    # Too many arguments
    print_error "Too many arguments provided"
    echo
    show_usage
    exit 1
fi

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    print_error "Configuration file not found: $CONFIG_FILE"
    echo
    show_usage
    exit 1
fi

print_status "Using configuration file: $CONFIG_FILE"

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_error ".NET is not installed or not in PATH"
    print_error "Please install .NET 6.0 or later from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if the application is already built
if [ ! -d "src/LapTimeSynth/bin/Release" ] || [ ! -f "src/LapTimeSynth/bin/Release/LapTimeSynth.dll" ]; then
    print_status "Application not built yet. Building..."

    # Build the solution
    dotnet build --configuration Release
    if [ $? -eq 0 ]; then
        print_success "Application built successfully"
    else
        print_error "Build failed"
        exit 1
    fi
else
    print_status "Application already built, skipping build step"
fi

# Display race configuration info
print_status "Race Configuration:"
echo "  • Config file: $CONFIG_FILE"
echo "  • Application: src/LapTimeSynth/bin/Release/LapTimeSynth.dll"
echo

# Run the application
print_status "Starting Lap Time Synthesizer..."
echo

dotnet run --project src/LapTimeSynth/LapTimeSynth.csproj --configuration Release -- "$CONFIG_FILE"

# Check exit status
if [ $? -eq 0 ]; then
    print_success "Application completed successfully"
else
    print_error "Application exited with an error"
    exit 1
fi
