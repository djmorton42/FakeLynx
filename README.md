# Lap Time Synthesizer

A .NET console application that simulates a lap tracking device for speed skating races. It generates realistic lap times with variability and sends timing packets to a FinishLynx timing system.

## Features

- **Realistic Race Simulation**: Supports 4.5, 9, or 13.5 lap races with 1-10 skaters
- **FinishLynx Protocol**: Implements the exact FinishLynx LapTime Protocol for Lynx type devices
- **Variable Lap Times**: Includes realistic variability in lap times with performance trends
- **Half-Lap Support**: Properly handles half-lap races (0.75x multiplier for first half-lap)
- **First Lap Logic**: First lap takes 1.25x average time for integer lap races
- **Real-time Display**: Shows race progress and lap times in real-time
- **Results Export**: Saves detailed race results to files

## Configuration

The application uses YAML configuration files. See `config/sample-race.yml` for an example:

```yaml
race:
  laps: 9  # Can be 4.5, 9, or 13.5
  tcp:
    host: localhost
    port: 2002

skaters:
  - lane: 1
    average_split_time: 12.2  # seconds
  - lane: 2
    average_split_time: 11.8
  # ... up to 10 skaters
```

## Usage

### Basic Usage
```bash
dotnet run --project src/FakeLynx/FakeLynx.csproj
```

### With Custom Configuration
```bash
dotnet run --project src/FakeLynx/FakeLynx.csproj config/my-race.yml
```

### Build and Run
```bash
dotnet build
dotnet run --project src/FakeLynx/FakeLynx.csproj
```

## Protocol Implementation

The application implements the FinishLynx LapTime Protocol exactly as specified:

- **S Opcode**: Split time packets with lane number
- **Z Opcode**: Zero/start time packets
- **T Opcode**: Time sync packets
- **Sync Status**: Uses 0x01 (sync ok) for real-time packets
- **Format**: `<sot><opcode>,<time>[,<identifier>[,<event>,<round>,<heat>]]<eot>`

### Example Packets
```
01S,13:24:12.876,2\r\n
01Z,13:23:15.345\r\n
01T,13:24:00.000\r\n
```

## Race Logic

### Lap Time Calculation
- **Half-lap races**: First lap is 0.75x average time
- **Integer lap races**: First lap is 1.25x average time
- **Variability**: ±8% random variation plus performance trends
- **Realism**: Occasional "bad laps" or "great laps" for variety

### Performance Trends
Each skater has a consistent performance trend (95-105% of average) to create realistic race outcomes where different skaters can win on different runs.

## Output

### Console Output
- Real-time race progress
- Lap times as they occur
- TCP packet transmission logs
- Final standings

### File Output
Race results are saved to `output/race-results-YYYYMMDD-HHMMSS.txt` containing:
- Race metadata
- Complete lap time log
- Final standings with total times

## Development

### Project Structure
```
FakeLynx/
├── src/
│   └── FakeLynx/                  # Main console application (consolidated)
├── config/                        # Configuration files
├── output/                        # Race results output
└── tests/                         # Unit tests
```

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

## Requirements

- .NET 9.0 SDK
- Windows (production) or macOS/Linux (development)
- TCP connection to FinishLynx timing system (optional)

## License

This project is for testing and development purposes.
