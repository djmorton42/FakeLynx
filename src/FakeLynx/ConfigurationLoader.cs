using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FakeLynx;

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
        
        if (config.Racers == null || config.Racers.Count == 0)
        {
            throw new InvalidOperationException("No racers configured");
        }
        
        if (config.Racers.Count > 10)
        {
            throw new InvalidOperationException($"Too many racers: {config.Racers.Count}. Maximum is 10");
        }
        
        foreach (var racer in config.Racers)
        {
            if (racer.Lane < 1 || racer.Lane > 10)
            {
                throw new InvalidOperationException($"Invalid lane number: {racer.Lane}. Must be between 1 and 10");
            }
            
            // Validate that either average split time or explicit times are provided
            bool hasAverageTime = racer.AverageSplitTime > 0;
            bool hasExplicitTimes = racer.Times != null && racer.Times.Count > 0;
            
            if (!hasAverageTime && !hasExplicitTimes)
            {
                throw new InvalidOperationException($"Racer in lane {racer.Lane} must have either average_split_time or times configured");
            }
            
            if (hasAverageTime && hasExplicitTimes)
            {
                throw new InvalidOperationException($"Racer in lane {racer.Lane} cannot have both average_split_time and times configured. Choose one.");
            }
            
            if (hasAverageTime && racer.AverageSplitTime <= 0)
            {
                throw new InvalidOperationException($"Invalid average split time for lane {racer.Lane}: {racer.AverageSplitTime}");
            }
            
            if (hasExplicitTimes)
            {
                foreach (var time in racer.Times!)
                {
                    if (time <= 0)
                    {
                        throw new InvalidOperationException($"Invalid explicit time for lane {racer.Lane}: {time}. All times must be positive.");
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
