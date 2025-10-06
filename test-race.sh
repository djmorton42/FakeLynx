#!/bin/bash

echo "Running a complete race simulation..."
echo "This will run for about 2-3 minutes to complete a 9-lap race."
echo ""

# Run the application and let it complete naturally
timeout 300 dotnet run --project src/FakeLynx/FakeLynx.csproj

echo ""
echo "Race completed! Check the output/ directory for results."
