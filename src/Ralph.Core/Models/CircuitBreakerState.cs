namespace Ralph.Core.Models;

/// <summary>
/// Represents the state of the circuit breaker
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>
    /// Normal operation - loop can execute
    /// </summary>
    Closed,

    /// <summary>
    /// Monitoring mode - watching for improvement
    /// </summary>
    HalfOpen,

    /// <summary>
    /// Halted - loop execution stopped due to stagnation
    /// </summary>
    Open
}

/// <summary>
/// Circuit breaker metrics and state
/// </summary>
public class CircuitBreakerInfo
{
    public CircuitBreakerState State { get; set; } = CircuitBreakerState.Closed;
    public int NoProgressCount { get; set; }
    public int SameErrorCount { get; set; }
    public int LastFilesModified { get; set; }
    public string? LastError { get; set; }
    public DateTime LastStateChange { get; set; } = DateTime.UtcNow;
    public string? StateChangeReason { get; set; }

    // Thresholds (configurable)
    public int NoProgressThreshold { get; set; } = 3;
    public int SameErrorThreshold { get; set; } = 5;
    public int OutputDeclineThreshold { get; set; } = 70;
}
