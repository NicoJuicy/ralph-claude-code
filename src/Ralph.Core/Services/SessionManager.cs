using Ralph.Core.Interfaces;
using Ralph.Core.Models;
using System.Text.Json;

namespace Ralph.Core.Services;

/// <summary>
/// Manages Claude Code CLI session continuity
/// </summary>
public class SessionManager : ISessionManager
{
    private readonly IStateStore _stateStore;
    private const string SessionFile = ".claude_session_id";
    private const string RalphSessionFile = ".ralph_session";
    private const string HistoryFile = ".ralph_session_history";
    private const int MaxHistoryEntries = 50;

    private SessionInfo? _currentSession;

    public SessionManager(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _currentSession = await _stateStore.LoadAsync<SessionInfo>(SessionFile, cancellationToken);

        // Check if session is expired
        if (_currentSession != null && !_currentSession.IsValid())
        {
            await ResetSessionAsync("Session expired (24 hour limit)", cancellationToken);
        }
    }

    public Task<SessionInfo?> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_currentSession);
    }

    public async Task StoreSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        if (_currentSession == null)
        {
            _currentSession = new SessionInfo
            {
                SessionId = sessionId,
                CreatedAt = now,
                LastUsed = now,
                LoopCount = 1
            };
        }
        else
        {
            _currentSession.SessionId = sessionId;
            _currentSession.LastUsed = now;
            _currentSession.LoopCount++;
        }

        await _stateStore.SaveAsync(SessionFile, _currentSession, cancellationToken);
        await LogSessionTransitionAsync("session_stored", sessionId, cancellationToken);
    }

    public async Task<bool> ShouldResumeSessionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentSession == null)
            return false;

        if (string.IsNullOrEmpty(_currentSession.SessionId))
            return false;

        return _currentSession.IsValid();
    }

    public async Task ResetSessionAsync(string reason, CancellationToken cancellationToken = default)
    {
        var oldSessionId = _currentSession?.SessionId;

        _currentSession = null;
        await _stateStore.DeleteAsync(SessionFile, cancellationToken);
        await _stateStore.DeleteAsync(RalphSessionFile, cancellationToken);

        await LogSessionTransitionAsync("session_reset", reason, cancellationToken);
    }

    public async Task LogSessionTransitionAsync(string eventName, string? reason = null, CancellationToken cancellationToken = default)
    {
        var entry = new SessionHistoryEntry
        {
            Timestamp = DateTime.UtcNow,
            Event = eventName,
            SessionId = _currentSession?.SessionId,
            Reason = reason
        };

        var json = JsonSerializer.Serialize(entry);
        await _stateStore.AppendLogAsync(HistoryFile, json, cancellationToken);

        // Keep only last N entries
        await TrimHistoryAsync(cancellationToken);
    }

    public async Task<List<SessionHistoryEntry>> GetSessionHistoryAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var lines = await _stateStore.ReadLogAsync(HistoryFile, cancellationToken);

        var entries = new List<SessionHistoryEntry>();
        foreach (var line in lines.TakeLast(count))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<SessionHistoryEntry>(line);
                if (entry != null)
                    entries.Add(entry);
            }
            catch (JsonException)
            {
                // Skip invalid entries
            }
        }

        return entries;
    }

    private async Task TrimHistoryAsync(CancellationToken cancellationToken)
    {
        var history = await GetSessionHistoryAsync(MaxHistoryEntries, cancellationToken);

        if (history.Count > MaxHistoryEntries)
        {
            // Rewrite file with only last N entries
            await _stateStore.DeleteAsync(HistoryFile, cancellationToken);

            foreach (var entry in history.TakeLast(MaxHistoryEntries))
            {
                var json = JsonSerializer.Serialize(entry);
                await _stateStore.AppendLogAsync(HistoryFile, json, cancellationToken);
            }
        }
    }
}
