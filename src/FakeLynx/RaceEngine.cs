using System.Threading;

namespace FakeLynx;

/// <summary>
/// Core race engine that manages race logic, timing, and variability
/// </summary>
public class RaceEngine
{
    private readonly Random _random;
    private readonly double _variabilityPercentage;
    private readonly int _baseSeed;
    private int _lapCounter = 0;
    
    public RaceEngine(int? seed = null, double variabilityPercentage = 0.12)
    {
        _baseSeed = seed ?? Environment.TickCount;
        _random = new Random(_baseSeed);
        _variabilityPercentage = variabilityPercentage; // 12% default variability (increased from 8%)
    }
    
    /// <summary>
    /// Creates a race from configuration
    /// </summary>
    public Race CreateRace(RaceConfiguration config)
    {
        var race = new Race(config.Race.Laps);
        
        foreach (var skaterConfig in config.Skaters)
        {
            var skater = new Skater(skaterConfig.Lane, skaterConfig.AverageSplitTime, skaterConfig.Times);
            race.Skaters.Add(skater);
        }
        
        return race;
    }
    
    /// <summary>
    /// Calculates the next lap time for a skater with realistic variability
    /// </summary>
    public double CalculateLapTime(Skater skater, int lapNumber, bool isHalfLap = false)
    {
        // If skater has explicit times, use them directly
        if (skater.UsesExplicitTimes)
        {
            return GetExplicitLapTime(skater, lapNumber, isHalfLap);
        }
        
        // Otherwise, use the existing average time logic with variability
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
        
        // Create a more complex seed using multiple entropy sources
        var complexSeed = GenerateComplexSeed(skater, lapNumber);
        var skaterRandom = new Random(complexSeed);
        
        // Realistic variability: -1 second (best case) to +3 seconds (worst case)
        var timeVariation = GetRealisticTimeVariation(skaterRandom, baseTime);
        var variedTime = baseTime + timeVariation;
        
        // Add performance trends with realistic variation
        var performanceTrend = GetRealisticPerformanceTrend(skater.Lane, skaterRandom);
        variedTime *= performanceTrend;
        
        // Occasional extreme laps with realistic constraints
        if (skaterRandom.NextDouble() < 0.05) // 5% chance of extreme lap
        {
            var extremeVariation = GetExtremeLapVariation(skaterRandom, baseTime);
            variedTime = baseTime + extremeVariation;
        }
        
        // Add millisecond-level precision
        var millisecondPrecision = GetMillisecondPrecision(skaterRandom);
        variedTime += millisecondPrecision;
        
        return Math.Max(variedTime, 1.0); // Ensure minimum 1 second
    }
    
    /// <summary>
    /// Gets the explicit lap time for a skater, handling half laps appropriately
    /// </summary>
    private double GetExplicitLapTime(Skater skater, int lapNumber, bool isHalfLap)
    {
        if (skater.ExplicitTimes == null || skater.ExplicitTimes.Count == 0)
        {
            throw new InvalidOperationException($"Skater in lane {skater.Lane} has no explicit times configured");
        }
        
        // For half lap races, the first lap (lap 1) is a half lap
        if (isHalfLap && lapNumber == 1)
        {
            // Use the first explicit time for the half lap
            return skater.ExplicitTimes[0];
        }
        
        // For regular laps, use the appropriate index
        // If it's a half lap race, skip the first time (used for half lap) and use subsequent times
        var timeIndex = isHalfLap ? lapNumber : lapNumber - 1;
        
        // Ensure we don't go beyond the available times
        if (timeIndex >= skater.ExplicitTimes.Count)
        {
            // If we run out of explicit times, fall back to the last available time
            return skater.ExplicitTimes[^1];
        }
        
        return skater.ExplicitTimes[timeIndex];
    }
    
    /// <summary>
    /// Generates a complex seed using multiple entropy sources
    /// </summary>
    private int GenerateComplexSeed(Skater skater, int lapNumber)
    {
        // Use multiple entropy sources for better randomness
        var timeComponent = (int)(DateTime.Now.Ticks & 0x7FFFFFFF);
        var laneComponent = skater.Lane * 7919; // Prime number for better distribution
        var lapComponent = lapNumber * 65537; // Another prime
        var baseComponent = _baseSeed;
        var counterComponent = Interlocked.Increment(ref _lapCounter) * 9973; // Another prime
        
        return timeComponent ^ laneComponent ^ lapComponent ^ baseComponent ^ counterComponent;
    }
    
    /// <summary>
    /// Gets realistic time variation: -1 second (best) to +3 seconds (worst)
    /// </summary>
    private double GetRealisticTimeVariation(Random random, double baseTime)
    {
        // Use a weighted distribution that favors slower times (more realistic)
        var randomValue = random.NextDouble();
        
        if (randomValue < 0.1) // 10% chance of being faster (up to 1 second better)
        {
            return -random.NextDouble() * 1.0; // 0 to -1 second
        }
        else if (randomValue < 0.3) // 20% chance of being slightly faster (0 to 0.5 seconds better)
        {
            return -random.NextDouble() * 0.5; // 0 to -0.5 seconds
        }
        else if (randomValue < 0.6) // 30% chance of being close to average (within 0.5 seconds)
        {
            return (random.NextDouble() - 0.5) * 1.0; // -0.5 to +0.5 seconds
        }
        else if (randomValue < 0.85) // 25% chance of being slower (0.5 to 2 seconds slower)
        {
            return 0.5 + random.NextDouble() * 1.5; // 0.5 to 2.0 seconds
        }
        else // 15% chance of being much slower (2 to 3 seconds slower)
        {
            return 2.0 + random.NextDouble() * 1.0; // 2.0 to 3.0 seconds
        }
    }
    
    /// <summary>
    /// Gets realistic performance trend with small variation
    /// </summary>
    private double GetRealisticPerformanceTrend(int lane, Random random)
    {
        // Small performance variation: 98% to 102% of average
        var baseTrend = 0.98 + (random.NextDouble() * 0.04);
        
        // Add tiny lane-specific bias
        var laneBias = (lane % 2 == 0 ? 1 : -1) * 0.005; // ±0.5% bias
        return baseTrend + laneBias;
    }
    
    /// <summary>
    /// Gets extreme lap variation with realistic constraints
    /// </summary>
    private double GetExtremeLapVariation(Random random, double baseTime)
    {
        var extremeType = random.NextDouble();
        
        if (extremeType < 0.3) // 30% chance of great lap (up to 1.5 seconds better)
        {
            return -random.NextDouble() * 1.5; // 0 to -1.5 seconds
        }
        else if (extremeType < 0.6) // 30% chance of bad lap (1.5 to 4 seconds slower)
        {
            return 1.5 + random.NextDouble() * 2.5; // 1.5 to 4.0 seconds
        }
        else // 40% chance of very extreme lap (4 to 6 seconds slower)
        {
            return 4.0 + random.NextDouble() * 2.0; // 4.0 to 6.0 seconds
        }
    }
    
    /// <summary>
    /// Adds millisecond-level precision to lap times
    /// </summary>
    private double GetMillisecondPrecision(Random random)
    {
        // Add random milliseconds (0-999ms) for realistic precision
        var milliseconds = random.Next(0, 1000);
        return milliseconds / 1000.0; // Convert to seconds
    }
    
    /// <summary>
    /// Determines if a skater has finished the race
    /// </summary>
    public bool IsSkaterFinished(Skater skater, Race race)
    {
        if (race.HasHalfLap)
        {
            // For half lap races, finish after completing the half lap (4.5 laps = 5 events)
            return skater.CurrentLap > race.Laps + 0.5;
        }
        else
        {
            return skater.CurrentLap > race.Laps;
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
