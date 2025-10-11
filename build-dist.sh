#!/bin/bash

# FakeLynx - Build and Distribution Script
# This script builds the application and creates a distributable package

set -e  # Exit on any error

echo "=== FakeLynx - Build & Distribution ==="
echo

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

# Check if we're in the right directory
if [ ! -f "FakeLynx.sln" ]; then
    print_error "Please run this script from the project root directory (where FakeLynx.sln is located)"
    exit 1
fi

# Clean previous builds
print_status "Cleaning previous builds..."
if [ -d "dist" ]; then
    rm -rf dist
fi
print_success "Previous builds cleaned"

# Build the solution
print_status "Building solution..."
dotnet build --configuration Release
if [ $? -eq 0 ]; then
    print_success "Solution built successfully"
else
    print_error "Build failed"
    exit 1
fi

# Create distribution directory
print_status "Creating distribution directory..."
mkdir -p dist/deployment
print_success "Distribution directory created"

# Build Windows executable (self-contained, no trimming for compatibility)
print_status "Building Windows executable (self-contained)..."
dotnet publish src/FakeLynx/FakeLynx.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o dist/win-x64-no-trim

if [ $? -eq 0 ]; then
    print_success "Windows executable built successfully"
else
    print_error "Windows executable build failed"
    exit 1
fi

# Copy files to deployment directory
print_status "Creating deployment package..."

# Copy executable
cp dist/win-x64-no-trim/FakeLynx.exe dist/deployment/

# Copy configuration files
mkdir -p dist/deployment/race-configs
cp config/*.yml dist/deployment/race-configs/

# Copy documentation
cp etc/README-Windows.md dist/deployment/README.md

# Copy VERSION.txt if it exists
if [ -f "VERSION.txt" ]; then
    cp VERSION.txt dist/deployment/
    print_success "VERSION.txt copied to deployment package"
else
    print_warning "VERSION.txt not found - skipping"
fi

# Create Windows batch file
cat > dist/deployment/run-race.bat << 'EOF'
@echo off
FakeLynx.exe race-configs\5-racers-4.5-laps-average-times.yml
EOF


# Create zip file in root directory
print_status "Creating zip file..."
cd dist/deployment
zip -r ../../FakeLynx-win-x64.zip .
cd ../..
print_success "Zip file created: FakeLynx-win-x64.zip"

# Get executable size
EXE_SIZE=$(ls -lh dist/deployment/FakeLynx.exe | awk '{print $5}')

# Get zip file size
ZIP_SIZE=$(ls -lh FakeLynx-win-x64.zip | awk '{print $5}')

# Display summary
echo
print_success "=== BUILD COMPLETE ==="
echo
echo "Distribution package created in: dist/deployment/"
echo "  • FakeLynx.exe ($EXE_SIZE)"
echo "  • race-configs/ (directory with all race configurations)"
echo "  • run-race.bat (Windows batch file)"
echo "  • README.md (Windows instructions)"
if [ -f "VERSION.txt" ]; then
    echo "  • VERSION.txt (version information)"
fi
echo
echo "Zip file created: FakeLynx-win-x64.zip ($ZIP_SIZE)"
echo
echo "To deploy:"
echo "  1. Copy the entire 'dist/deployment/' folder to your Windows computer, OR"
echo "  2. Use the zip file 'FakeLynx-win-x64.zip' and extract it on Windows"
echo "  3. Double-click 'run-race.bat' to run the application"
echo
print_success "Ready for deployment!"
