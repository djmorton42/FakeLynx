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
cp config/sample-race.yml dist/deployment/
cp config/sample-race.yml dist/deployment/race-config.yml

# Copy documentation
cp README.md dist/deployment/

# Create Windows batch file
cat > dist/deployment/run-race.bat << 'EOF'
@echo off
FakeLynx.exe race-config.yml
EOF

# Create Windows README
cat > dist/deployment/README-Windows.md << 'EOF'
# FakeLynx - Windows Deployment

This package contains a standalone executable for simulating lap tracking data and sending it to FinishLynx timing software.

## Files Included

- `FakeLynx.exe` - Main application executable
- `race-config.yml` - Sample race configuration
- `run-race.bat` - Windows batch file to run the application
- `README-Windows.md` - This file

## Requirements

- Windows 10 or later
- FinishLynx timing software running and listening on localhost:2002
- No additional software installation required (self-contained executable)

## Quick Start

1. **Start FinishLynx** on your Windows computer
2. **Configure a FinishLynx LapTime Device of type "Lynx" using "Network (listen)" mode on port 2002
3. **Double-click** `run-race.bat` to start a sample race
4. **Follow the prompts**:
   - Review race configuration
   - Ensure FinishLynx is ready, then press any key
   - Wait for connection confirmation
   - Press any key to start the race
   - Watch the simulation run in real-time
   - Review final results
   - Press any key to disconnect and exit

## Manual Usage

### Basic Command
```cmd
FakeLynx.exe race-config.yml
```

### Custom Configuration
```cmd
FakeLynx.exe my-race-config.yml
```

## Configuration

Edit `race-config.yml` to customize your race:

```yaml
race:
  laps: 4.5  # Can be any whole or half number of laps (4, 4.5, 9, 13.5, etc.)
  tcp:
    host: localhost
    port: 2002
  dual_transponder:
    enabled: true  # Record two passings for each lap crossing (one for each ankle)
    delay_milliseconds: 50.0  # Delay between first and second transponder packets

skaters:
  - lane: 1
    average_split_time: 12.2  # seconds (use this for random lap times around the average)
  - lane: 2
    average_split_time: 11.8
  # ... up to 10 skaters

# Alternative: Specify exact lap times instead of averages
# skaters:
#   - lane: 1
#     times:
#       - 7.633
#       - 10.267
#       - 9.933
#       - 9.967
#       - 10.367
#   - lane: 2
#     times:
#       - 7.433
#       - 10.000
#       - 9.667
#       - 9.733
#       - 10.167
```

### Configuration Options

- **laps**: Number of laps (supports half laps like 4.5, 9, 13.5)
- **dual_transponder**: 
  - `enabled`: When true, sends two packets per lap crossing (simulating both ankle transponders)
  - `delay_milliseconds`: Time delay between the two transponder packets
- **skaters**: Define up to 10 skaters with either:
  - `average_split_time`: Random lap times generated around this average
  - `times`: Exact lap times array (uncomment and use instead of average_split_time)

## Protocol

The application sends FinishLynx protocol packets:
- **S Packets**: `S,HH:MM:SS.mmm,LANE` for each lap crossing
- **Internal Sync**: No Z packets needed
- **Real-time**: Packets sent immediately when laps occur

## Output

- **Console**: Real-time lap times and packet data
- **Files**: Race results saved to `output/` folder
- **FinishLynx**: Receives timing packets via TCP

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
echo "  • race-config.yml (race configuration)"
echo "  • run-race.bat (Windows batch file)"
echo "  • README-Windows.md (Windows instructions)"
echo
echo "Zip file created: FakeLynx-win-x64.zip ($ZIP_SIZE)"
echo
echo "To deploy:"
echo "  1. Copy the entire 'dist/deployment/' folder to your Windows computer, OR"
echo "  2. Use the zip file 'FakeLynx-win-x64.zip' and extract it on Windows"
echo "  3. Double-click 'run-race.bat' to run the application"
echo
print_success "Ready for deployment!"
