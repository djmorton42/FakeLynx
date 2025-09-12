using LapTimeSynth.Models;

namespace LapTimeSynth.Core;

/// <summary>
/// Manages race timing and lap events
/// </summary>
public class RaceTimer
{
    private readonly RaceEngine _raceEngine;
    private readonly List<TimerEvent> _scheduledEvents = new();
    private readonly object _lock = new object();
    private Race? _currentRace;
    
    public event EventHandler<LapCompletedEventArgs>? LapCompleted;
    
    public RaceTimer(RaceEngine raceEngine)
    {
        _raceEngine = raceEngine;
    }
    
    /// <summary>
    /// Starts the race and schedules all lap events
    /// </summary>
    public void StartRace(Race race)
    {
        _currentRace = race;
        race.StartTime = DateTime.Now;
        
        foreach (var skater in race.Skaters)
        {
            ScheduleSkaterLaps(skater, race);
        }
    }
    
    /// <summary>
    /// Schedules all lap events for a skater
    /// </summary>
    private void ScheduleSkaterLaps(Skater skater, Race race)
    {
        var currentTime = DateTime.Now;
        var totalLaps = race.HasHalfLap ? (int)(race.Laps + 0.5) : (int)race.Laps;
        
        for (int i = 0; i < totalLaps; i++)
        {
            var lapNumber = i + 1; // Start from 1, not 0
            var isHalfLap = race.HasHalfLap && i == 0;
            
            // Calculate cumulative time for this lap
            var cumulativeTime = CalculateCumulativeTime(skater, i, race);
            var scheduledTime = currentTime.AddSeconds(cumulativeTime);
            
            var timerEvent = new TimerEvent
            {
                Skater = skater,
                LapNumber = lapNumber,
                ScheduledTime = scheduledTime,
                IsHalfLap = isHalfLap
            };
            
            _scheduledEvents.Add(timerEvent);
        }
        
        // Sort events by scheduled time
        _scheduledEvents.Sort((a, b) => a.ScheduledTime.CompareTo(b.ScheduledTime));
    }
    
    /// <summary>
    /// Calculates cumulative time for a lap number
    /// </summary>
    private double CalculateCumulativeTime(Skater skater, int lapIndex, Race race)
    {
        double cumulativeTime = 0;
        
        for (int i = 0; i <= lapIndex; i++)
        {
            var isHalfLap = race.HasHalfLap && i == 0;
            var lapTime = _raceEngine.CalculateLapTime(skater, i + 1, isHalfLap);
            cumulativeTime += lapTime;
        }
        
        return cumulativeTime;
    }
    
    /// <summary>
    /// Processes all scheduled events up to the current time
    /// </summary>
    public void ProcessEvents(DateTime currentTime)
    {
        lock (_lock)
        {
            var eventsToProcess = _scheduledEvents
                .Where(e => e.ScheduledTime <= currentTime && !e.Processed)
                .OrderBy(e => e.ScheduledTime)
                .ToList();
            
            foreach (var timerEvent in eventsToProcess)
            {
                ProcessLapEvent(timerEvent, currentTime);
            }
        }
    }

    /// <summary>
    /// Processes all remaining events immediately (for race completion)
    /// </summary>
    public void ProcessAllRemainingEvents()
    {
        lock (_lock)
        {
            var remainingEvents = _scheduledEvents
                .Where(e => !e.Processed)
                .OrderBy(e => e.ScheduledTime)
                .ToList();
            
            foreach (var timerEvent in remainingEvents)
            {
                ProcessLapEvent(timerEvent, DateTime.Now);
            }
        }
    }
    
    /// <summary>
    /// Processes a single lap event
    /// </summary>
    private void ProcessLapEvent(TimerEvent timerEvent, DateTime currentTime)
    {
        if (timerEvent.Processed) return;
        
        var skater = timerEvent.Skater;
        var lapTime = new LapTime(
            skater.Lane,
            timerEvent.LapNumber,
            _raceEngine.CalculateLapTime(skater, timerEvent.LapNumber, timerEvent.IsHalfLap),
            currentTime,
            timerEvent.IsHalfLap
        );
        
        skater.LapTimes.Add(lapTime);
        skater.CurrentLap = timerEvent.LapNumber + 1;
        
        timerEvent.Processed = true;
        
        // Raise event
        LapCompleted?.Invoke(this, new LapCompletedEventArgs
        {
            LapTime = lapTime,
            Skater = skater
        });
    }
    
    private Race GetRaceFromSkater(Skater skater)
    {
        if (_currentRace == null)
        {
            throw new InvalidOperationException("No race is currently active");
        }
        return _currentRace;
    }
    
    /// <summary>
    /// Gets all remaining events
    /// </summary>
    public List<TimerEvent> GetRemainingEvents()
    {
        lock (_lock)
        {
            return _scheduledEvents.Where(e => !e.Processed).ToList();
        }
    }
}

/// <summary>
/// Represents a scheduled timer event
/// </summary>
public class TimerEvent
{
    public Skater Skater { get; set; } = null!;
    public int LapNumber { get; set; }
    public DateTime ScheduledTime { get; set; }
    public bool IsHalfLap { get; set; }
    public bool Processed { get; set; }
}

/// <summary>
/// Event arguments for lap completion
/// </summary>
public class LapCompletedEventArgs : EventArgs
{
    public LapTime LapTime { get; set; } = null!;
    public Skater Skater { get; set; } = null!;
}
