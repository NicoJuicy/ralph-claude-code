using Ralph.Core.Interfaces;
using Ralph.Core.Models;

namespace Ralph.Core.Services;

/// <summary>
/// Circuit breaker implementation to prevent runaway loops
/// </summary>
public class CircuitBreaker : ICircuitBreaker
{
    private readonly IStateStore _stateStore;
    private const string StateFile = ".circuit_breaker_state";
    private const string HistoryFile = ".circuit_breaker_history";

    private CircuitBreakerInfo _state = new();

    public CircuitBreaker(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Load existing state or create new
        var loadedState = await _stateStore.LoadAsync<CircuitBreakerInfo>(StateFile, cancellationToken);

        if (loadedState != null)
        {
            _state = loadedState;
        }
        else
        {
            _state = new CircuitBreakerInfo
            {
                State = CircuitBreakerState.Closed,
                LastStateChange = DateTime.UtcNow
            };
            await SaveStateAsync(cancellationToken);
        }
    }

    public Task<CircuitBreakerInfo> GetStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_state);
    }

    public Task<bool> CanExecuteAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_state.State != CircuitBreakerState.Open);
    }

    public async Task RecordLoopResultAsync(int filesModified, string? error, CancellationToken cancellationToken = default)
    {
        var previousState = _state.State;

        // Check for no progress
        if (filesModified == 0 && _state.LastFilesModified == 0)
        {
            _state.NoProgressCount++;
        }
        else
        {
            _state.NoProgressCount = 0;
        }

        // Check for same error pattern
        if (!string.IsNullOrEmpty(error))
        {
            if (error == _state.LastError)
            {
                _state.SameErrorCount++;
            }
            else
            {
                _state.SameErrorCount = 1;
                _state.LastError = error;
            }
        }
        else
        {
            _state.SameErrorCount = 0;
            _state.LastError = null;
        }

        _state.LastFilesModified = filesModified;

        // State transition logic
        switch (_state.State)
        {
            case CircuitBreakerState.Closed:
                if (_state.NoProgressCount >= _state.NoProgressThreshold)
                {
                    await TransitionToAsync(CircuitBreakerState.HalfOpen,
                        "No progress detected for multiple loops", cancellationToken);
                }
                else if (_state.SameErrorCount >= _state.SameErrorThreshold)
                {
                    await TransitionToAsync(CircuitBreakerState.Open,
                        "Same error repeated multiple times", cancellationToken);
                }
                break;

            case CircuitBreakerState.HalfOpen:
                if (filesModified > 0)
                {
                    // Progress detected - recover
                    await TransitionToAsync(CircuitBreakerState.Closed,
                        "Progress detected, recovering", cancellationToken);
                }
                else if (_state.NoProgressCount >= _state.NoProgressThreshold + 1)
                {
                    // Still no progress - open circuit
                    await TransitionToAsync(CircuitBreakerState.Open,
                        "Continued stagnation detected", cancellationToken);
                }
                break;

            case CircuitBreakerState.Open:
                // Manual reset required
                break;
        }

        await SaveStateAsync(cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await TransitionToAsync(CircuitBreakerState.Closed, "Manual reset", cancellationToken);
        _state.NoProgressCount = 0;
        _state.SameErrorCount = 0;
        _state.LastError = null;
        await SaveStateAsync(cancellationToken);
    }

    public async Task<string> GetStatusStringAsync(CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken);
        return state.State switch
        {
            CircuitBreakerState.Closed => "CLOSED (normal operation)",
            CircuitBreakerState.HalfOpen => $"HALF_OPEN (monitoring, {state.NoProgressCount} loops no progress)",
            CircuitBreakerState.Open => $"OPEN (halted: {state.StateChangeReason})",
            _ => "UNKNOWN"
        };
    }

    private async Task TransitionToAsync(CircuitBreakerState newState, string reason, CancellationToken cancellationToken)
    {
        var oldState = _state.State;
        _state.State = newState;
        _state.StateChangeReason = reason;
        _state.LastStateChange = DateTime.UtcNow;

        // Log transition to history
        var historyEntry = new
        {
            Timestamp = DateTime.UtcNow,
            FromState = oldState.ToString(),
            ToState = newState.ToString(),
            Reason = reason
        };

        await _stateStore.AppendLogAsync(HistoryFile,
            System.Text.Json.JsonSerializer.Serialize(historyEntry), cancellationToken);
    }

    private async Task SaveStateAsync(CancellationToken cancellationToken)
    {
        await _stateStore.SaveAsync(StateFile, _state, cancellationToken);
    }
}
