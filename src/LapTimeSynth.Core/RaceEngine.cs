using LapTimeSynth.Models;

namespace LapTimeSynth.Core;

/// <summary>
/// Core race engine that manages race logic, timing, and variability
/// </summary>
public class RaceEngine
{
    private readonly Random _random;
    private readonly double _variabilityPercentage;
    
    public RaceEngine(int? seed = null, double variabilityPercentage = 0.08)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _variabilityPercentage = variabilityPercentage; // 8% default variability
    }
    
    /// <summary>
    /// Creates a race from configuration
    /// </summary>
    public Race CreateRace(RaceConfiguration config)
    {
        var race = new Race(config.Race.Laps);
        
        foreach (var skaterConfig in config.Skaters)
        {
            var skater = new Skater(skaterConfig.Lane, skaterConfig.AverageSplitTime);
            race.Skaters.Add(skater);
        }
        
        return race;
    }
    
    /// <summary>
    /// Calculates the next lap time for a skater with variability
    /// </summary>
    public double CalculateLapTime(Skater skater, int lapNumber, bool isHalfLap = false)
    {
        var baseTime = skater.AverageSplitTime;
        
        // Apply lap-specific multipliers
        if (isHalfLap)
        {
            baseTime *= 0.75; // Half lap is 75% of average time
        }
        else if (lapNumber == 1 && !isHalfLap)
        {
            baseTime *= 1.25; // First lap is 125% of average time
        }
        
        // Use a consistent random seed for this skater and lap combination
        var skaterRandom = new Random(skater.Lane * 1000 + lapNumber);
        
        // Add random variability
        var variability = skaterRandom.NextDouble() * (_variabilityPercentage * 2) - _variabilityPercentage;
        var variedTime = baseTime * (1 + variability);
        
        // Add some performance trends (some skaters consistently faster/slower)
        var performanceTrend = GetPerformanceTrend(skater.Lane);
        variedTime *= performanceTrend;
        
        // Occasional "bad laps" or "great laps" for realism
        if (skaterRandom.NextDouble() < 0.05) // 5% chance
        {
            var extremeMultiplier = skaterRandom.NextDouble() < 0.5 ? 0.85 : 1.15; // 15% better or worse
            variedTime *= extremeMultiplier;
        }
        
        return Math.Max(variedTime, 1.0); // Ensure minimum 1 second
    }
    
    /// <summary>
    /// Gets a performance trend for a skater (some are consistently faster/slower)
    /// </summary>
    private double GetPerformanceTrend(int lane)
    {
        // Use lane number as seed for consistent performance trends
        var laneRandom = new Random(lane * 1000);
        return 0.95 + (laneRandom.NextDouble() * 0.1); // 95% to 105% of average
    }
    
    /// <summary>
    /// Determines if a skater has finished the race
    /// </summary>
    public bool IsSkaterFinished(Skater skater, Race race)
    {
        if (race.HasHalfLap)
        {
            return skater.CurrentLap >= race.Laps + 0.5; // Half lap counts as 0.5
        }
        else
        {
            return skater.CurrentLap >= race.Laps;
        }
    }
    
    /// <summary>
    /// Gets the next lap number for a skater
    /// </summary>
    public int GetNextLapNumber(Skater skater, Race race)
    {
        if (race.HasHalfLap && skater.CurrentLap == 0)
        {
            return 0; // Half lap is lap 0
        }
        
        return skater.CurrentLap + 1;
    }
    
    /// <summary>
    /// Determines if the next lap is a half lap
    /// </summary>
    public bool IsNextLapHalfLap(Skater skater, Race race)
    {
        return race.HasHalfLap && skater.CurrentLap == 0;
    }
}
