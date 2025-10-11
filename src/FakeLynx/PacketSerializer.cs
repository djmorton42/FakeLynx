namespace FakeLynx;

/// <summary>
/// Serializes timing packets for transmission to the FinishLynx timing device
/// Implements the exact protocol format: <sot><opcode>,<time>[,<identifier>[,<event>,<round>,<heat>]]<eot>
/// </summary>
public class PacketSerializer
{
    /// <summary>
    /// Serializes a timing packet to bytes for TCP transmission
    /// Format: <sot><opcode>,<time>[,<identifier>[,<event>,<round>,<heat>]]<eot>
    /// </summary>
    public byte[] SerializePacket(TimingPacket packet)
    {
        var packetString = BuildPacketString(packet);
        return System.Text.Encoding.UTF8.GetBytes(packetString);
    }
    
    /// <summary>
    /// Builds the packet string according to the FinishLynx protocol
    /// </summary>
    private string BuildPacketString(TimingPacket packet)
    {
        var parts = new List<string>();
        
        // Add opcode
        parts.Add(packet.OpCode.ToString());
        
        // Add time in HH:MM:SS.mmm format
        parts.Add(packet.Time.ToString("HH:mm:ss.fff"));
        
        // Add identifier if present (for S opcode)
        if (packet.Identifier.HasValue)
        {
            parts.Add(packet.Identifier.Value.ToString());
        }
        
        // Add optional event, round, heat if present
        if (!string.IsNullOrEmpty(packet.Event))
        {
            parts.Add(packet.Event);
            if (!string.IsNullOrEmpty(packet.Round))
            {
                parts.Add(packet.Round);
                if (!string.IsNullOrEmpty(packet.Heat))
                {
                    parts.Add(packet.Heat);
                }
            }
        }
        
        // Join with commas
        var dataString = string.Join(",", parts);
        
        // Add SOT (Start of Transmission) and EOT (End of Transmission)
        var syncByte = (char)packet.SyncStatus;
        var eot = "\r\n"; // CRLF as specified
        
        return $"{syncByte}{dataString}{eot}";
    }
    
    /// <summary>
    /// Creates a split time packet from lap time data
    /// </summary>
    public TimingPacket CreateSplitTimePacket(LapTime lapTime, bool useSyncOk = true)
    {
        return new TimingPacket(
            useSyncOk ? SyncStatus.SyncOk : SyncStatus.NoSync,
            OpCode.S,
            lapTime.Timestamp,
            lapTime.RacerLane
        );
    }
    
    /// <summary>
    /// Creates a zero/start time packet
    /// </summary>
    public TimingPacket CreateZeroTimePacket(DateTime startTime)
    {
        return new TimingPacket(
            SyncStatus.SyncOk,
            OpCode.Z,
            startTime
        );
    }
    
    /// <summary>
    /// Creates a current time packet for sync
    /// </summary>
    public TimingPacket CreateTimeSyncPacket(DateTime currentTime)
    {
        return new TimingPacket(
            SyncStatus.SyncOk,
            OpCode.T,
            currentTime
        );
    }
}
