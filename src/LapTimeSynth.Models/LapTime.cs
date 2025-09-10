namespace LapTimeSynth.Models;

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
