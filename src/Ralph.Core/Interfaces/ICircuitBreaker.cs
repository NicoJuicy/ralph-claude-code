using Ralph.Core.Models;

namespace Ralph.Core.Interfaces;

/// <summary>
/// Circuit breaker service to prevent runaway loops
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Initialize the circuit breaker
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current circuit breaker state
    /// </summary>
    Task<CircuitBreakerInfo> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if loop execution is allowed
    /// </summary>
    Task<bool> CanExecuteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Record the result of a loop execution
    /// </summary>
    Task RecordLoopResultAsync(int filesModified, string? error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset the circuit breaker to closed state
    /// </summary>
    Task ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get circuit breaker status as a string
    /// </summary>
    Task<string> GetStatusStringAsync(CancellationToken cancellationToken = default);
}
