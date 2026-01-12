using Ralph.Core.Models;

namespace Ralph.Core.Interfaces;

/// <summary>
/// Analyzes responses from Claude Code CLI
/// </summary>
public interface IResponseAnalyzer
{
    /// <summary>
    /// Parse a response from Claude Code CLI
    /// </summary>
    Task<ClaudeResponse?> ParseResponseAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract Ralph status block from response
    /// </summary>
    Task<RalphStatus?> ExtractStatusAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect if this is a test-only loop
    /// </summary>
    Task<bool> DetectTestOnlyLoopAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect if the loop is stuck on the same error
    /// </summary>
    Task<bool> DetectStuckLoopAsync(string currentError, List<string> recentErrors, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect completion signals in the response
    /// </summary>
    Task<bool> DetectCompletionSignalsAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract errors from the response (with two-stage filtering)
    /// </summary>
    Task<List<string>> ExtractErrorsAsync(string output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate confidence score for exit decision
    /// </summary>
    Task<int> CalculateExitConfidenceAsync(RalphStatus status, ExitSignalInfo exitSignals, CancellationToken cancellationToken = default);
}
