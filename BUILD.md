# Building and Distribution

## Quick Build

To build the application and create a distributable package, simply run:

```bash
./build-dist.sh
```

This script will:
1. Clean any previous builds
2. Build the entire solution in Release mode
3. Create a self-contained Windows executable
4. Package everything into a `dist/deployment/` folder ready for distribution

## What Gets Created

The build script creates a complete deployment package in `dist/deployment/`:

- **FakeLynx.exe** - Standalone Windows executable (68MB)
- **race-config.yml** - Sample race configuration
- **run-race.bat** - Windows batch file for easy execution
- **README-Windows.md** - Complete Windows deployment instructions

## Manual Build Commands

If you prefer to run the build commands manually:

```bash
# Clean and build solution
dotnet clean
dotnet build --configuration Release

# Create Windows executable
dotnet publish src/FakeLynx/FakeLynx.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -o dist/win-x64-no-trim
```

## Requirements

- .NET 9.0 SDK
- macOS, Linux, or Windows (cross-platform build)
- No additional dependencies required

## Deployment

1. Copy the entire `dist/deployment/` folder to your Windows computer
2. Ensure FinishLynx is running and listening on the configured port
3. Double-click `run-race.bat` to start the application
