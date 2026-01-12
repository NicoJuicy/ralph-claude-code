namespace Ralph.Core.Models;

/// <summary>
/// Represents rate limiting state
/// </summary>
public class RateLimitInfo
{
    public int CallCount { get; set; }
    public int MaxCallsPerHour { get; set; } = 100;
    public DateTime LastReset { get; set; }
    public DateTime NextReset { get; set; }

    /// <summary>
    /// Check if rate limit has been reached
    /// </summary>
    public bool IsLimitReached => CallCount >= MaxCallsPerHour;

    /// <summary>
    /// Get time remaining until reset
    /// </summary>
    public TimeSpan TimeUntilReset => NextReset - DateTime.UtcNow;

    /// <summary>
    /// Get formatted countdown string
    /// </summary>
    public string GetCountdownString()
    {
        var remaining = TimeUntilReset;
        if (remaining.TotalMinutes < 1)
            return "less than 1 minute";
        if (remaining.TotalHours < 1)
            return $"{(int)remaining.TotalMinutes} minutes";
        return $"{(int)remaining.TotalHours} hours {remaining.Minutes} minutes";
    }
}
