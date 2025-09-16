namespace LapTimeSynth.Models;

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
