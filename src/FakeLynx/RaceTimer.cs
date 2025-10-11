namespace FakeLynx;

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
        
        foreach (var racer in race.Racers)
        {
            ScheduleRacerLaps(racer, race);
        }
    }
    
    /// <summary>
    /// Schedules all lap events for a racer
    /// </summary>
    private void ScheduleRacerLaps(Racer racer, Race race)
    {
        var currentTime = DateTime.Now;
        var totalLaps = race.HasHalfLap ? (int)(race.Laps + 0.5) : (int)race.Laps;
        
        for (int i = 0; i < totalLaps; i++)
        {
            var lapNumber = i + 1; // Start from 1, not 0
            var isHalfLap = race.HasHalfLap && i == 0;
            
            // Calculate cumulative time for this lap
            var cumulativeTime = CalculateCumulativeTime(racer, i, race);
            var scheduledTime = currentTime.AddSeconds(cumulativeTime);
            
            var timerEvent = new TimerEvent
            {
                Racer = racer,
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
    private double CalculateCumulativeTime(Racer racer, int lapIndex, Race race)
    {
        double cumulativeTime = 0;
        
        for (int i = 0; i <= lapIndex; i++)
        {
            var isHalfLap = race.HasHalfLap && i == 0;
            var lapTime = _raceEngine.CalculateLapTime(racer, i + 1, isHalfLap);
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
        
        var racer = timerEvent.Racer;
        var lapTime = new LapTime(
            racer.Lane,
            timerEvent.LapNumber,
            _raceEngine.CalculateLapTime(racer, timerEvent.LapNumber, timerEvent.IsHalfLap),
            currentTime,
            timerEvent.IsHalfLap
        );
        
        racer.LapTimes.Add(lapTime);
        racer.CurrentLap = timerEvent.LapNumber + 1;
        
        timerEvent.Processed = true;
        
        // Raise event
        LapCompleted?.Invoke(this, new LapCompletedEventArgs
        {
            LapTime = lapTime,
            Racer = racer
        });
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
    public Racer Racer { get; set; } = null!;
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
    public Racer Racer { get; set; } = null!;
}
