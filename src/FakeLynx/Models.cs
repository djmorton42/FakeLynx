namespace FakeLynx;

/// <summary>
/// Represents a single lap time for a skater
/// </summary>
public class LapTime
{
    public int SkaterLane { get; set; }
    public int LapNumber { get; set; }
    public double TimeInSeconds { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsHalfLap { get; set; } = false;
    
    public LapTime(int skaterLane, int lapNumber, double timeInSeconds, DateTime timestamp, bool isHalfLap = false)
    {
        SkaterLane = skaterLane;
        LapNumber = lapNumber;
        TimeInSeconds = timeInSeconds;
        Timestamp = timestamp;
        IsHalfLap = isHalfLap;
    }
}

/// <summary>
/// Represents a complete race
/// </summary>
public class Race
{
    public double Laps { get; set; } // Can be 4.5, 9, or 13.5
    public List<Skater> Skaters { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public bool IsFinished { get; set; } = false;
    public bool HasHalfLap => Laps % 1 != 0;
    public bool HasWholeLaps => Laps % 1 == 0; // Integer number of laps means whole laps
    
    public Race(double laps)
    {
        Laps = laps;
        StartTime = DateTime.Now;
    }
    
    public bool IsValidLapCount()
    {
        return Laps == 4.5 || Laps == 9 || Laps == 13.5;
    }
}

/// <summary>
/// Configuration for a race loaded from YAML
/// </summary>
public class RaceConfiguration
{
    public RaceSettings Race { get; set; } = new();
    public List<SkaterConfiguration> Skaters { get; set; } = new();
}

public class RaceSettings
{
    public double Laps { get; set; }
    public TcpSettings Tcp { get; set; } = new();
    public DualTransponderSettings DualTransponder { get; set; } = new();
}

public class TcpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2002;
}

public class SkaterConfiguration
{
    public int Lane { get; set; }
    public double AverageSplitTime { get; set; }
    public List<double>? Times { get; set; } // Explicit split times in seconds
}

public class DualTransponderSettings
{
    public bool Enabled { get; set; } = true;
    public double DelayMilliseconds { get; set; } = 50.0; // Default 50ms delay between transponders
}

/// <summary>
/// Represents a skater in the race
/// </summary>
public class Skater
{
    public int Lane { get; set; }
    public double AverageSplitTime { get; set; } // in seconds
    public List<double>? ExplicitTimes { get; set; } // Explicit split times in seconds
    public List<LapTime> LapTimes { get; set; } = new();
    public bool IsFinished { get; set; } = false;
    public DateTime? FinishTime { get; set; }
    public int CurrentLap { get; set; } = 0;
    
    public Skater(int lane, double averageSplitTime)
    {
        Lane = lane;
        AverageSplitTime = averageSplitTime;
    }
    
    public Skater(int lane, double averageSplitTime, List<double>? explicitTimes)
    {
        Lane = lane;
        AverageSplitTime = averageSplitTime;
        ExplicitTimes = explicitTimes;
    }
    
    /// <summary>
    /// Gets whether this skater uses explicit times instead of average times
    /// </summary>
    public bool UsesExplicitTimes => ExplicitTimes != null && ExplicitTimes.Count > 0;
}

/// <summary>
/// Represents a timing packet sent to the FinishLynx timing device
/// </summary>
public class TimingPacket
{
    public SyncStatus SyncStatus { get; set; }
    public OpCode OpCode { get; set; }
    public DateTime Time { get; set; }
    public int? Identifier { get; set; } // Lane number for split times
    public string? Event { get; set; }
    public string? Round { get; set; }
    public string? Heat { get; set; }
    
    public TimingPacket(SyncStatus syncStatus, OpCode opCode, DateTime time, int? identifier = null, 
                       string? eventName = null, string? round = null, string? heat = null)
    {
        SyncStatus = syncStatus;
        OpCode = opCode;
        Time = time;
        Identifier = identifier;
        Event = eventName;
        Round = round;
        Heat = heat;
    }
}

/// <summary>
/// Sync status for the packet
/// </summary>
public enum SyncStatus
{
    SyncOk = 0x01,    // 01 hex - packet suitable for Internal Sync
    NoSync = 0x02     // 02 hex - packet not suitable for Internal Sync
}

/// <summary>
/// OpCode for the packet
/// </summary>
public enum OpCode
{
    S,  // Split time
    Z,  // Zero/start time
    T   // Current time of day
}
