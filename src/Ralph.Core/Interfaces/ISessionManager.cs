using Ralph.Core.Models;

namespace Ralph.Core.Interfaces;

/// <summary>
/// Manages Claude Code CLI session continuity
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Initialize session tracking
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current session information
    /// </summary>
    Task<SessionInfo?> GetCurrentSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Store a new session ID
    /// </summary>
    Task StoreSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if we should resume the current session
    /// </summary>
    Task<bool> ShouldResumeSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset the current session with a reason
    /// </summary>
    Task ResetSessionAsync(string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a session transition to history
    /// </summary>
    Task LogSessionTransitionAsync(string eventName, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get session history (last N entries)
    /// </summary>
    Task<List<SessionHistoryEntry>> GetSessionHistoryAsync(int count = 50, CancellationToken cancellationToken = default);
}
