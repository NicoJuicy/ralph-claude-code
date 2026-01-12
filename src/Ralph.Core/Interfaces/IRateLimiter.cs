using Ralph.Core.Models;

namespace Ralph.Core.Interfaces;

/// <summary>
/// Manages API rate limiting
/// </summary>
public interface IRateLimiter
{
    /// <summary>
    /// Initialize rate limiter
    /// </summary>
    Task InitializeAsync(int maxCallsPerHour = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a call can be made
    /// </summary>
    Task<bool> CanMakeCallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a call has been made
    /// </summary>
    Task RecordCallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current rate limit info
    /// </summary>
    Task<RateLimitInfo> GetInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait until rate limit resets (if needed)
    /// </summary>
    Task WaitForResetAsync(Action<string>? progressCallback = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset rate limiter manually
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);
}
