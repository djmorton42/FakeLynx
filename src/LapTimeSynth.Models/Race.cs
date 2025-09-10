namespace LapTimeSynth.Models;

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
