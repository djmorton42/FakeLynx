namespace LapTimeSynth.Models;

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
}
