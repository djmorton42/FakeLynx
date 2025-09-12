using LapTimeSynth.Configuration;
using LapTimeSynth.Core;
using LapTimeSynth.Models;
using LapTimeSynth.Protocol;
using System.Net.Sockets;

namespace LapTimeSynth;

class Program
{
    private static Race? _currentRace;
    private static RaceTimer? _raceTimer;
    private static TcpClient? _tcpClient;
    private static PacketSerializer? _packetSerializer;
    private static List<string> _raceResults = new();

    private static async Task RunRace()
    {
        // Start the race
        Console.WriteLine("=== LAP TIMES ===");
        Console.WriteLine();

        _raceTimer!.StartRace(_currentRace!);
        
        // For Internal Sync mode, no Z packet is needed
        Console.WriteLine("Race started - using Internal Sync mode");
        Console.WriteLine();

        // Main race loop
        var raceStartTime = DateTime.Now;

        while (true)
        {
            var currentTime = DateTime.Now;
            
            // Process timer events
            _raceTimer.ProcessEvents(currentTime);

            // Check if race is finished - wait for all events to be processed
            if (IsRaceFinished())
            {
                // Process any remaining events to ensure all laps are completed
                _raceTimer.ProcessAllRemainingEvents();
                break;
            }

            // Check for early termination
            if (Console.KeyAvailable)
            {
                Console.ReadKey(true);
                Console.WriteLine("\nRace stopped by user.");
                break;
            }

            await Task.Delay(50); // Small delay to prevent excessive CPU usage
        }

        // Finish race
        _currentRace!.EndTime = DateTime.Now;
        _currentRace.IsFinished = true;

        Console.WriteLine();
        Console.WriteLine("=== RACE FINISHED ===");
        Console.WriteLine($"Total race time: {(_currentRace.EndTime!.Value - _currentRace.StartTime).TotalSeconds:F1} seconds");
        Console.WriteLine();

        // Display final results
        DisplayFinalResults();
    }

    private static void ResetApplicationState()
    {
        // Clear race results
        _raceResults.Clear();
        
        // Reset race timer if it exists
        if (_raceTimer != null)
        {
            // The race timer should be reset when we create a new race
            // but we can add any additional cleanup here if needed
        }
        
        // Note: We keep the TCP connection and packet serializer as they can be reused
        // The race engine will be reused to create a new race
    }

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Lap Time Synthesizer ===");
            Console.WriteLine();

            // Load configuration
            var configPath = args.Length > 0 ? args[0] : "config/sample-race.yml";
            var configLoader = new ConfigurationLoader();
            var config = configLoader.LoadFromFile(configPath);

            // Display race information
            Console.WriteLine($"Race Configuration:");
            Console.WriteLine($"  • Distance: {config.Race.Laps} laps");
            Console.WriteLine($"  • Number of racers: {config.Skaters.Count}");
            Console.WriteLine($"  • Target host: {config.Race.Tcp.Host}");
            Console.WriteLine($"  • Target port: {config.Race.Tcp.Port}");
            Console.WriteLine();

            // Initialize components
            var raceEngine = new RaceEngine();
            _raceTimer = new RaceTimer(raceEngine);
            _packetSerializer = new PacketSerializer();

            // Create race
            _currentRace = raceEngine.CreateRace(config);
            Console.WriteLine($"Created race with {_currentRace.Skaters.Count} skaters");
            Console.WriteLine();

            // First pause - ensure FinishLynx is running
            Console.WriteLine("Please ensure FinishLynx is running and listening on the configured host and port.");
            Console.WriteLine("Press any key to attempt connection...");
            Console.ReadKey(true);
            Console.WriteLine();

            // Connect to timing device
            await ConnectToTimingDevice(config.Race.Tcp.Host, config.Race.Tcp.Port);

            // Set up event handlers
            _raceTimer.LapCompleted += OnLapCompleted;

            // Second pause - ready to start race
            Console.WriteLine();
            Console.WriteLine("Connection established. Press any key to start the race...");
            Console.ReadKey(true);
            Console.WriteLine();

            // Run the first race
            await RunRace();

            // Save results
            await SaveRaceResults();

            Console.WriteLine();
            Console.WriteLine("Race results saved to output/race-results.txt");
            Console.WriteLine();

            // Main application loop - allow multiple races
            while (true)
            {
                Console.WriteLine("What would you like to do?");
                Console.WriteLine("1. Run another race (keep connection)");
                Console.WriteLine("2. Disconnect and exit");
                Console.Write("Enter your choice (1 or 2): ");
                
                var choice = Console.ReadLine();
                Console.WriteLine();

                if (choice == "1")
                {
                    // Reset application state for new race
                    ResetApplicationState();
                    
                    // Create new race with same configuration
                    _currentRace = raceEngine.CreateRace(config);
                    Console.WriteLine($"Created new race with {_currentRace.Skaters.Count} skaters");
                    Console.WriteLine();
                    
                    // Start the new race
                    Console.WriteLine("Press any key to start the new race...");
                    Console.ReadKey(true);
                    Console.WriteLine();
                    
                    // Run the race (extract race logic into a method)
                    await RunRace();
                }
                else if (choice == "2")
                {
                    // Disconnect and exit
                    if (_tcpClient?.Connected == true)
                    {
                        _tcpClient.Close();
                        Console.WriteLine("Disconnected from FinishLynx.");
                    }
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid choice. Please enter 1 or 2.");
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            _tcpClient?.Close();
        }
    }

    private static async Task ConnectToTimingDevice(string host, int port)
    {
        try
        {
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port);
            Console.WriteLine($"Connected to timing device at {host}:{port}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect to timing device: {ex.Message}");
            Console.WriteLine("Continuing without TCP connection...");
            _tcpClient = null;
        }
    }

    private static void OnLapCompleted(object? sender, LapCompletedEventArgs e)
    {
        var lapTime = e.LapTime;
        var skater = e.Skater;

        // Calculate elapsed time from race start
        var elapsedTime = lapTime.Timestamp - _currentRace!.StartTime;
        var elapsedSeconds = elapsedTime.TotalSeconds;
        
        // Calculate total elapsed time for this skater
        var totalElapsedTime = skater.LapTimes.Sum(l => l.TimeInSeconds);
        
        // Log lap time with both split time and total elapsed time
        var lapType = lapTime.IsHalfLap ? "Half-lap" : $"Lap {lapTime.LapNumber}";
        var logMessage = $"[+{elapsedSeconds:F1}s] Lane {skater.Lane} - {lapType}: {lapTime.TimeInSeconds:F3}s (Total: {totalElapsedTime:F3}s)";
        
        // Write the lap time (this will appear above the progress line)
        Console.WriteLine(logMessage);
        _raceResults.Add(logMessage);

        // Create and display packet (whether or not TCP is connected)
        if (_packetSerializer != null)
        {
            try
            {
                var packet = _packetSerializer.CreateSplitTimePacket(lapTime, useSyncOk: true);
                var packetData = _packetSerializer.SerializePacket(packet);
                var packetString = System.Text.Encoding.UTF8.GetString(packetData).Trim();
                
                // Always show the packet content
                Console.WriteLine($"  -> Packet: {packetString}");
                
                // Send packet to timing device if connected
                if (_tcpClient?.Connected == true)
                {
                    var stream = _tcpClient.GetStream();
                    stream.Write(packetData, 0, packetData.Length);
                    Console.WriteLine($"  -> Sent to FinishLynx");
                }
                else
                {
                    Console.WriteLine($"  -> (Not sent - no TCP connection)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> Packet error: {ex.Message}");
            }
        }

        // Check if skater finished
        if (skater.IsFinished)
        {
            var finishElapsed = skater.FinishTime!.Value - _currentRace!.StartTime;
            var finishMessage = $"[+{finishElapsed.TotalSeconds:F1}s] Lane {skater.Lane} - 🏁 FINISHED!";
            Console.WriteLine($"*** {finishMessage} ***");
            _raceResults.Add(finishMessage);
        }
    }

    private static string GetRaceProgress()
    {
        if (_currentRace == null) return "Unknown";

        var finishedSkaters = _currentRace.Skaters.Count(s => s.IsFinished);
        var totalSkaters = _currentRace.Skaters.Count;
        
        return $"{finishedSkaters}/{totalSkaters} finished";
    }

    private static bool IsRaceFinished()
    {
        if (_currentRace == null) return false;
        
        // Check if all skaters are finished AND all scheduled events have been processed
        var allSkatersFinished = _currentRace.Skaters.All(s => s.IsFinished);
        var remainingEvents = _raceTimer?.GetRemainingEvents() ?? new List<TimerEvent>();
        
        return allSkatersFinished && remainingEvents.Count == 0;
    }

    private static void DisplayFinalResults()
    {
        if (_currentRace == null) return;

        Console.WriteLine("=== FINAL RACE RESULTS ===");
        Console.WriteLine();

        var finishedSkaters = _currentRace.Skaters
            .Where(s => s.IsFinished)
            .OrderBy(s => s.FinishTime)
            .ToList();

        Console.WriteLine("Final Standings:");
        for (int i = 0; i < finishedSkaters.Count; i++)
        {
            var skater = finishedSkaters[i];
            var position = i + 1;
            var totalTime = skater.FinishTime.HasValue ? (skater.FinishTime.Value - _currentRace.StartTime).TotalSeconds : 0;
            
            Console.WriteLine($"  {position}. Lane {skater.Lane} - {totalTime:F3}s");
        }

        Console.WriteLine();
        Console.WriteLine($"Total race time: {(_currentRace.EndTime!.Value - _currentRace.StartTime).TotalSeconds:F1} seconds");
        Console.WriteLine($"Number of skaters: {_currentRace.Skaters.Count}");
        Console.WriteLine($"Race distance: {_currentRace.Laps} laps");
    }

    private static async Task SaveRaceResults()
    {
        if (_currentRace == null) return;

        var results = new List<string>
        {
            "=== RACE RESULTS ===",
            $"Race Date: {_currentRace.StartTime:yyyy-MM-dd HH:mm:ss}",
            $"Total Laps: {_currentRace.Laps}",
            $"Number of Skaters: {_currentRace.Skaters.Count}",
            $"Race Duration: {(_currentRace.EndTime - _currentRace.StartTime)?.TotalSeconds:F1} seconds",
            "",
            "=== LAP TIMES ===",
            ""
        };

        results.AddRange(_raceResults);

        results.Add("");
        results.Add("=== FINAL STANDINGS ===");
        
        var finishedSkaters = _currentRace.Skaters
            .Where(s => s.IsFinished)
            .OrderBy(s => s.FinishTime)
            .ToList();

        for (int i = 0; i < finishedSkaters.Count; i++)
        {
            var skater = finishedSkaters[i];
            var position = i + 1;
            var totalTime = skater.FinishTime.HasValue ? (skater.FinishTime.Value - _currentRace.StartTime).TotalSeconds : 0;
            
            results.Add($"{position}. Lane {skater.Lane} - {totalTime:F3}s");
        }

        var outputDir = "output";
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var fileName = $"race-results-{_currentRace.StartTime:yyyyMMdd-HHmmss}.txt";
        var filePath = Path.Combine(outputDir, fileName);
        
        await File.WriteAllLinesAsync(filePath, results);
    }
}