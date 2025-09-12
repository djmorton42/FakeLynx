using LapTimeSynth.Models;
using System.Threading;

namespace LapTimeSynth.Core;

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
        
        // Create a more complex seed using multiple entropy sources
        var complexSeed = GenerateComplexSeed(skater, lapNumber);
        var skaterRandom = new Random(complexSeed);
        
        // Multiple layers of randomness for better distribution
        var primaryVariability = GetPrimaryVariability(skaterRandom);
        var secondaryVariability = GetSecondaryVariability(skaterRandom);
        var microVariability = GetMicroVariability(skaterRandom);
        
        // Combine all variability sources
        var totalVariability = primaryVariability + secondaryVariability + microVariability;
        var variedTime = baseTime * (1 + totalVariability);
        
        // Add performance trends with more variation
        var performanceTrend = GetPerformanceTrend(skater.Lane, skaterRandom);
        variedTime *= performanceTrend;
        
        // Occasional extreme laps with more realistic distribution
        if (skaterRandom.NextDouble() < 0.08) // 8% chance (increased from 5%)
        {
            var extremeMultiplier = GetExtremeLapMultiplier(skaterRandom);
            variedTime *= extremeMultiplier;
        }
        
        // Add final micro-adjustment for more realistic precision
        var finalAdjustment = (skaterRandom.NextDouble() - 0.5) * 0.01; // ±0.5% final adjustment
        variedTime *= (1 + finalAdjustment);
        
        return Math.Max(variedTime, 1.0); // Ensure minimum 1 second
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
    /// Primary variability - main random component
    /// </summary>
    private double GetPrimaryVariability(Random random)
    {
        // Use normal distribution approximation for more realistic variability
        var u1 = random.NextDouble();
        var u2 = random.NextDouble();
        var normal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        
        // Scale to our variability percentage
        return normal * (_variabilityPercentage / 3.0); // Divide by 3 to account for multiple layers
    }
    
    /// <summary>
    /// Secondary variability - additional random component
    /// </summary>
    private double GetSecondaryVariability(Random random)
    {
        // Additional random component with different distribution
        var value = random.NextDouble() * (_variabilityPercentage / 2.0) - (_variabilityPercentage / 4.0);
        return value;
    }
    
    /// <summary>
    /// Micro variability - small random adjustments
    /// </summary>
    private double GetMicroVariability(Random random)
    {
        // Very small random adjustments for more realistic precision
        return (random.NextDouble() - 0.5) * (_variabilityPercentage / 4.0);
    }
    
    /// <summary>
    /// Gets a performance trend for a skater with more variation
    /// </summary>
    private double GetPerformanceTrend(int lane, Random random)
    {
        // More varied performance trends
        var baseTrend = 0.92 + (random.NextDouble() * 0.16); // 92% to 108% of average
        
        // Add some lane-specific bias but with randomness
        var laneBias = (lane % 3 - 1) * 0.02; // Slight bias based on lane position
        return baseTrend + laneBias;
    }
    
    /// <summary>
    /// Gets extreme lap multiplier with more realistic distribution
    /// </summary>
    private double GetExtremeLapMultiplier(Random random)
    {
        // More varied extreme lap distribution
        var extremeType = random.NextDouble();
        if (extremeType < 0.3) // 30% chance of great lap
        {
            return 0.80 + (random.NextDouble() * 0.15); // 80% to 95% (great lap)
        }
        else if (extremeType < 0.6) // 30% chance of bad lap
        {
            return 1.10 + (random.NextDouble() * 0.15); // 110% to 125% (bad lap)
        }
        else // 40% chance of very extreme lap
        {
            return random.NextDouble() < 0.5 ? 
                0.70 + (random.NextDouble() * 0.10) : // 70% to 80% (exceptional lap)
                1.20 + (random.NextDouble() * 0.20);  // 120% to 140% (terrible lap)
        }
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
