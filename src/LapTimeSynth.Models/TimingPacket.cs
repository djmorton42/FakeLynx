namespace LapTimeSynth.Models;

/// <summary>
/// Represents a timing packet sent to the FinishLynx timing device
/// </summary>
public class TimingPacket
{
    public SyncStatus SyncStatus { get; set; }
    public OpCode OpCode { get; set; }
    public DateTime Time { get; set; }
    public int? Identifier { get; set; } // Lane number for split times
    public string? Event { get; set; }
    public string? Round { get; set; }
    public string? Heat { get; set; }
    
    public TimingPacket(SyncStatus syncStatus, OpCode opCode, DateTime time, int? identifier = null, 
                       string? eventName = null, string? round = null, string? heat = null)
    {
        SyncStatus = syncStatus;
        OpCode = opCode;
        Time = time;
        Identifier = identifier;
        Event = eventName;
        Round = round;
        Heat = heat;
    }
}

/// <summary>
/// Sync status for the packet
/// </summary>
public enum SyncStatus
{
    SyncOk = 0x01,    // 01 hex - packet suitable for Internal Sync
    NoSync = 0x02     // 02 hex - packet not suitable for Internal Sync
}

/// <summary>
/// OpCode for the packet
/// </summary>
public enum OpCode
{
    S,  // Split time
    Z,  // Zero/start time
    T   // Current time of day
}
