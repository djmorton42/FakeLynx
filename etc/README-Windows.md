# FakeLynx - Windows Deployment

FakeLynx is software that simulates a LapTiming device which can connect to FinishLynx. It implements the Lynx Laptime Protocol (https://finishlynx.com/wp-content/uploads/2012/08/Lynx_LapTime_Protocol.pdf) and uses the 'Internal Sync' mechanism, which assumes that network latency will be minimal, therefore enabling FinishLynx to calculate lap times based on the delta between the timestamps received in the lap time packets. It enables you to simulate races of 1-10 racers.

## Simulation Variability

The application is built to include randomness so that each race is not the same. When specifying exact lap times in the configuration file, those times will be used. However, if average times are specified, the application will introduce randomness where sometimes racers will have very good or very bad laps. Each simulated race should end with different results.

When using average lap times, some extra factors are introduced on the first lap. For races with a whole nubmer of laps (4, 5, 9, etc.), a multiple of 1.25x will be applied to the average (in addition to random variability) to account for racers accelerating off the line, making the first lap take longer. For races that include a half lap (4.5, 13.5, etc.) a multiple of 0.75x will be applied to the average for the first lap, to account for both the racers accelerating off the line and the fact that the first lap is only half the distance.

## Configuration

The application uses a YAML file to set its configuration properties and the race configuration it will simulate. Several sample race configurations are included in the `race-configs` directory.

## Requirements

- Windows 10 or later
- FinishLynx timing software running and listening on localhost:2002
- No additional software installation required (self-contained executable)

## Quick Start

1. **Start FinishLynx** on your Windows computer
2. **Configure a FinishLynx LapTime Device of type "Lynx" using "Network (listen)" mode on port 2002, using 'Internal' sync mode.
3. **Double-click** `run-race.bat` to start the default race (5 racers, 4.5 laps)

The application will display information on the race configuration:

```
[INFO] Race Configuration:
  • Config file: race.yml
  • Application: FakeLynx.exe

[INFO] Starting FakeLynx...

=== Lap Time Synthesizer ===

Race Configuration:
  • Distance: 4.5 laps
  • Number of racers: 5
  • Target host: localhost
  • Target port: 2002
  • Dual transponder: Enabled
  • Transponder delay: 50ms

Created race with 5 racers

Please ensure FinishLynx is running and listening on the configured host and port.
Press any key to attempt connection...
```

When ready, press any key to attempt to connect to FinishLynx. You will be informed if the connection is successful or not. The application will work both in connected mode (with a connection to FinishLynx) and offline mode, without a FinishLynx connection. In offline mode, you can see the information that would be sent to FinishLynx and when, but no data will be transmitted. 

```
Failed to connect to timing device: Connection refused
Continuing without TCP connection...

Running in offline mode (no TCP connection). Press any key to start the race...
```

Once the connection has been determined, press any other key to start the race, when at the appropriate times, you'll see messages like this:

```
[+7.7s] Lane 1 - Half-lap: 7.6716s (Total: 7.6716s)
  -> Packet: S,08:50:43.468,1
  -> (Not sent - no TCP connection)
  -> Packet: S,08:50:43.518,1
  -> (Not sent - no TCP connection)
[+9.9s] Lane 3 - Half-lap: 11.5272s (Total: 11.5272s)
  -> Packet: S,08:50:45.631,3
  -> (Not sent - no TCP connection)
  -> Packet: S,08:50:45.681,3
  -> (Not sent - no TCP connection)
[+10.0s] Lane 2 - Half-lap: 8.4883s (Total: 8.4883s)
  -> Packet: S,08:50:45.785,2
  -> (Not sent - no TCP connection)
  -> Packet: S,08:50:45.835,2
  -> (Not sent - no TCP connection)
[+10.6s] Lane 4 - Half-lap: 11.4489s (Total: 11.4489s)
  -> Packet: S,08:50:46.345,4
  -> (Not sent - no TCP connection)
  -> Packet: S,08:50:46.395,4
  -> (Not sent - no TCP connection)
```

Note: If using online mode, you'll want to start the FinishLynx race very close to when you start the FakeLynx race (quickly tab between them, or use a starter device to trigger FinishLynx while manually pressing a key to start FakeLynx).

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

## Configuration Details

Edit any of the configuration files in the `race-configs/` directory to customize your race, or create your own configuration file:

```yaml
race:
  laps: 4.5  # Can be any whole or half number of laps (4, 4.5, 9, 13.5, etc.)
  tcp:
    host: localhost # The host FinishLynx is listening on. Localhost if it's running on the same computer, or an ip address otherwise.
    port: 2002 # The port FinishLynx is listening on. 2002 is the Lynx protocol default.
  dual_transponder:
    enabled: true  # Record two passings for each lap crossing (one for each ankle)
    delay_milliseconds: 50.0  # Delay between first and second transponder packets

racers:
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

## Protocol

The application sends FinishLynx protocol packets:
- **S Packets**: `S,HH:MM:SS.mmm,LANE` for each lap crossing
- **Internal Sync**: No Z packets needed
- **Real-time**: Packets sent immediately when laps occur

## Output

- **Console**: Real-time lap times and packet data
- **Files**: Race results saved to `output/` folder
- **FinishLynx**: Receives timing packets via TCP

