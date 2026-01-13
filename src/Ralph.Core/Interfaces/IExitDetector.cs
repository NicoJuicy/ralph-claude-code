using Ralph.Core.Models;

namespace Ralph.Core.Interfaces;

/// <summary>
/// Detects when the development loop should exit
/// </summary>
public interface IExitDetector
{
    /// <summary>
    /// Initialize exit detector
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if we should exit based on current loop state
    /// </summary>
    Task<bool> ShouldExitAsync(int currentLoop, RalphStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a test-only loop
    /// </summary>
    Task RecordTestOnlyLoopAsync(int loopNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a done signal
    /// </summary>
    Task RecordDoneSignalAsync(int loopNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a completion indicator
    /// </summary>
    Task RecordCompletionIndicatorAsync(int loopNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current exit signal information
    /// </summary>
    Task<ExitSignalInfo> GetExitSignalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if @fix_plan.md is complete
    /// </summary>
    Task<bool> IsFixPlanCompleteAsync(string fixPlanPath, CancellationToken cancellationToken = default);
}
