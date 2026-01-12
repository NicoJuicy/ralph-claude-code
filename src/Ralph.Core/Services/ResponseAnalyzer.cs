using Ralph.Core.Interfaces;
using Ralph.Core.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ralph.Core.Services;

/// <summary>
/// Analyzes responses from Claude Code CLI
/// </summary>
public partial class ResponseAnalyzer : IResponseAnalyzer
{
    // Regex patterns for status block extraction
    [GeneratedRegex(@"---RALPH_STATUS---\s*(.*?)\s*---END_RALPH_STATUS---", RegexOptions.Singleline)]
    private static partial Regex StatusBlockRegex();

    [GeneratedRegex(@"^(.*?):\s*(.*)$", RegexOptions.Multiline)]
    private static partial Regex KeyValueRegex();

    // Test command patterns
    private static readonly string[] TestPatterns = new[]
    {
        "npm test", "bats", "pytest", "jest", "cargo test", "dotnet test",
        "mvn test", "gradle test", "./test", "run tests", "running tests"
    };

    // Completion signal keywords
    private static readonly string[] CompletionKeywords = new[]
    {
        "done", "complete", "finished", "all tasks complete",
        "project complete", "ready for review", "implementation complete"
    };

    // Error patterns (two-stage filtering)
    // Stage 1: Filter out JSON field names
    [GeneratedRegex(@"""[^""]*error[^""]*"":", RegexOptions.IgnoreCase)]
    private static partial Regex JsonFieldErrorRegex();

    // Stage 2: Detect actual errors
    [GeneratedRegex(@"(^Error:|^ERROR:|^error:|\]: error|Link: error|Error occurred|failed with error|[Ee]xception|Fatal|FATAL)", RegexOptions.Multiline)]
    private static partial Regex ActualErrorRegex();

    public Task<ClaudeResponse?> ParseResponseAsync(string output, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult<ClaudeResponse?>(null);

        try
        {
            // Try to detect JSON format
            var trimmed = output.Trim();
            if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
            {
                var response = JsonSerializer.Deserialize<ClaudeResponse>(trimmed, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Task.FromResult(response);
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, fall through to text parsing
        }

        // Text-based parsing - extract status block
        return Task.FromResult<ClaudeResponse?>(null);
    }

    public Task<RalphStatus?> ExtractStatusAsync(string output, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult<RalphStatus?>(null);

        // Try to extract status block
        var match = StatusBlockRegex().Match(output);
        if (!match.Success)
            return Task.FromResult<RalphStatus?>(null);

        var statusBlock = match.Groups[1].Value;
        var status = new RalphStatus();

        // Parse key-value pairs
        foreach (Match kvMatch in KeyValueRegex().Matches(statusBlock))
        {
            var key = kvMatch.Groups[1].Value.Trim();
            var value = kvMatch.Groups[2].Value.Trim();

            switch (key.ToUpperInvariant())
            {
                case "STATUS":
                    status.Status = value;
                    break;
                case "TASKS_COMPLETED_THIS_LOOP":
                    if (int.TryParse(value, out var tasks))
                        status.TasksCompletedThisLoop = tasks;
                    break;
                case "FILES_MODIFIED":
                    if (int.TryParse(value, out var files))
                        status.FilesModified = files;
                    break;
                case "TESTS_STATUS":
                    status.TestsStatus = value;
                    break;
                case "WORK_TYPE":
                    status.WorkType = value;
                    break;
                case "EXIT_SIGNAL":
                    status.ExitSignal = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    break;
                case "RECOMMENDATION":
                    status.Recommendation = value;
                    break;
            }
        }

        return Task.FromResult<RalphStatus?>(status);
    }

    public Task<bool> DetectTestOnlyLoopAsync(string output, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult(false);

        var lowerOutput = output.ToLowerInvariant();

        // Check if output contains test patterns
        var hasTestPattern = TestPatterns.Any(pattern => lowerOutput.Contains(pattern));

        // Also check if there's minimal other content (heuristic)
        if (hasTestPattern)
        {
            // Count non-test related lines (simple heuristic)
            var lines = output.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Count();

            var testLines = output.Split('\n')
                .Count(l => TestPatterns.Any(p => l.Contains(p, StringComparison.OrdinalIgnoreCase)));

            // If > 50% of lines are test-related, it's a test-only loop
            return Task.FromResult(lines > 0 && (testLines * 100 / lines) > 50);
        }

        return Task.FromResult(false);
    }

    public Task<bool> DetectStuckLoopAsync(string currentError, List<string> recentErrors, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentError))
            return Task.FromResult(false);

        if (recentErrors.Count < 3)
            return Task.FromResult(false);

        // Extract error lines from current error
        var currentErrorLines = currentError.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (currentErrorLines.Count == 0)
            return Task.FromResult(false);

        // Check if ALL error lines appear in ALL recent errors
        var allLinesInAllErrors = currentErrorLines.All(errorLine =>
            recentErrors.All(recentError =>
                recentError.Contains(errorLine, StringComparison.Ordinal)));

        return Task.FromResult(allLinesInAllErrors);
    }

    public Task<bool> DetectCompletionSignalsAsync(string output, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult(false);

        var lowerOutput = output.ToLowerInvariant();

        // Check for completion keywords
        return Task.FromResult(CompletionKeywords.Any(keyword =>
            lowerOutput.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<List<string>> ExtractErrorsAsync(string output, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(output))
            return Task.FromResult(errors);

        // Stage 1: Filter out JSON field names
        var lines = output.Split('\n');
        var filteredLines = lines.Where(line => !JsonFieldErrorRegex().IsMatch(line));

        // Stage 2: Detect actual errors
        foreach (var line in filteredLines)
        {
            if (ActualErrorRegex().IsMatch(line))
            {
                errors.Add(line.Trim());
            }
        }

        return Task.FromResult(errors);
    }

    public Task<int> CalculateExitConfidenceAsync(RalphStatus status, ExitSignalInfo exitSignals, CancellationToken cancellationToken = default)
    {
        var confidence = 0;

        // Exit signal explicitly set
        if (status.ExitSignal)
            confidence += 30;

        // Status is COMPLETE
        if (status.Status == RalphStatus.StatusValues.Complete)
            confidence += 25;

        // Tests are passing
        if (status.TestsStatus == RalphStatus.TestStatusValues.Passing)
            confidence += 20;

        // Multiple consecutive done signals
        if (exitSignals.DoneSignals.Count >= exitSignals.MaxConsecutiveDoneSignals)
            confidence += 15;

        // Multiple test-only loops
        if (exitSignals.TestOnlyLoops.Count >= exitSignals.MaxConsecutiveTestLoops)
            confidence += 10;

        return Task.FromResult(confidence);
    }
}
