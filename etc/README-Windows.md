# FakeLynx - Windows Deployment

This package contains a standalone executable for simulating lap tracking data and sending it to FinishLynx timing software.

## Files Included

- `FakeLynx.exe` - Main application executable
- `race-configs/` - Directory containing all available race configurations
  - `5-racers-4.5-laps-average-times.yml` - Default race configuration (5 racers, 4.5 laps, average times)
  - `5-racers-4.5-laps-specified-times.yml` - 5 racers, 4.5 laps, exact times
  - `5-racers-9-laps-average-times.yml` - 5 racers, 9 laps, average times
  - `5-racers-9-laps-specified-times.yml` - 5 racers, 9 laps, exact times
- `run-race.bat` - Windows batch file to run the application with default config
- `README-Windows.md` - This file

## Requirements

- Windows 10 or later
- FinishLynx timing software running and listening on localhost:2002
- No additional software installation required (self-contained executable)

## Quick Start

1. **Start FinishLynx** on your Windows computer
2. **Configure a FinishLynx LapTime Device of type "Lynx" using "Network (listen)" mode on port 2002
3. **Double-click** `run-race.bat` to start the default race (5 racers, 4.5 laps)
4. **Follow the prompts**:
   - Review race configuration
   - Ensure FinishLynx is ready, then press any key
   - Wait for connection confirmation
   - Press any key to start the race
   - Watch the simulation run in real-time
   - Review final results
   - Press any key to disconnect and exit

## Manual Usage

### Basic Command (Default Race)
```cmd
FakeLynx.exe race-configs\5-racers-4.5-laps-average-times.yml
```

### Using Other Race Configurations
```cmd
FakeLynx.exe race-configs\5-racers-9-laps-average-times.yml
FakeLynx.exe race-configs\5-racers-4.5-laps-specified-times.yml
FakeLynx.exe race-configs\5-racers-9-laps-specified-times.yml
```

### Custom Configuration
```cmd
FakeLynx.exe my-race-config.yml
```

## Configuration

Edit any of the configuration files in the `race-configs/` directory to customize your race, or create your own configuration file:

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

