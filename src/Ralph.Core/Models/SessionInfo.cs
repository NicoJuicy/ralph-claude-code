namespace Ralph.Core.Models;

/// <summary>
/// Represents session state for Claude Code CLI
/// </summary>
public class SessionInfo
{
    public string? SessionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }
    public int LoopCount { get; set; }
    public bool IsExpired { get; set; }

    /// <summary>
    /// Session expiration time (24 hours by default)
    /// </summary>
    public static readonly TimeSpan ExpirationPeriod = TimeSpan.FromHours(24);

    /// <summary>
    /// Check if the session is still valid
    /// </summary>
    public bool IsValid()
    {
        var age = DateTime.UtcNow - CreatedAt;
        return !IsExpired && age < ExpirationPeriod;
    }
}

/// <summary>
/// Session history entry for tracking transitions
/// </summary>
public class SessionHistoryEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = string.Empty;
    public string? SessionId { get; set; }
    public string? Reason { get; set; }
}
