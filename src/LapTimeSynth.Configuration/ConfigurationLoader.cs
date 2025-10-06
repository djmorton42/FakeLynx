using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using LapTimeSynth.Models;

namespace LapTimeSynth.Configuration;

/// <summary>
/// Loads race configuration from YAML files
/// </summary>
public class ConfigurationLoader
{
    private readonly IDeserializer _deserializer;
    
    public ConfigurationLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
    }
    
    public RaceConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }
        
        var yamlContent = File.ReadAllText(filePath);
        var config = _deserializer.Deserialize<RaceConfiguration>(yamlContent);
        
        ValidateConfiguration(config);
        return config;
    }
    
    private void ValidateConfiguration(RaceConfiguration config)
    {
        if (config.Race == null)
        {
            throw new InvalidOperationException("Race configuration is missing");
        }
        
        if (!IsValidLapCount(config.Race.Laps))
        {
            throw new InvalidOperationException($"Invalid lap count: {config.Race.Laps}. Must either be a whole number or end with .5 (4, 4.5, 9, 13.5, etc.)");
        }
        
        if (config.Skaters == null || config.Skaters.Count == 0)
        {
            throw new InvalidOperationException("No skaters configured");
        }
        
        if (config.Skaters.Count > 10)
        {
            throw new InvalidOperationException($"Too many skaters: {config.Skaters.Count}. Maximum is 10");
        }
        
        foreach (var skater in config.Skaters)
        {
            if (skater.Lane < 1 || skater.Lane > 10)
            {
                throw new InvalidOperationException($"Invalid lane number: {skater.Lane}. Must be between 1 and 10");
            }
            
            // Validate that either average split time or explicit times are provided
            bool hasAverageTime = skater.AverageSplitTime > 0;
            bool hasExplicitTimes = skater.Times != null && skater.Times.Count > 0;
            
            if (!hasAverageTime && !hasExplicitTimes)
            {
                throw new InvalidOperationException($"Skater in lane {skater.Lane} must have either average_split_time or times configured");
            }
            
            if (hasAverageTime && hasExplicitTimes)
            {
                throw new InvalidOperationException($"Skater in lane {skater.Lane} cannot have both average_split_time and times configured. Choose one.");
            }
            
            if (hasAverageTime && skater.AverageSplitTime <= 0)
            {
                throw new InvalidOperationException($"Invalid average split time for lane {skater.Lane}: {skater.AverageSplitTime}");
            }
            
            if (hasExplicitTimes)
            {
                foreach (var time in skater.Times!)
                {
                    if (time <= 0)
                    {
                        throw new InvalidOperationException($"Invalid explicit time for lane {skater.Lane}: {time}. All times must be positive.");
                    }
                }
            }
        }
    }
    
    private bool IsValidLapCount(double laps)
    {
        return laps % 1 == 0 || laps % 1 == 0.5;
    }
}
