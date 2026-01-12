using Ralph.Core.Interfaces;
using Ralph.Core.Models;
using System.Text.RegularExpressions;

namespace Ralph.Core.Services;

/// <summary>
/// Detects when the development loop should exit
/// </summary>
public partial class ExitDetector : IExitDetector
{
    private readonly IStateStore _stateStore;
    private const string ExitSignalsFile = ".exit_signals";

    private ExitSignalInfo _exitSignals = new();

    [GeneratedRegex(@"^\s*-\s*\[(x|X)\]", RegexOptions.Multiline)]
    private static partial Regex CompletedTaskRegex();

    [GeneratedRegex(@"^\s*-\s*\[ \]", RegexOptions.Multiline)]
    private static partial Regex PendingTaskRegex();

    public ExitDetector(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var loaded = await _stateStore.LoadAsync<ExitSignalInfo>(ExitSignalsFile, cancellationToken);
        if (loaded != null)
        {
            _exitSignals = loaded;
        }
    }

    public async Task<bool> ShouldExitAsync(int currentLoop, RalphStatus status, CancellationToken cancellationToken = default)
    {
        // Check explicit exit signal
        if (status.ExitSignal)
            return true;

        // Check if accumulated signals suggest exit
        if (_exitSignals.ShouldExit(currentLoop))
            return true;

        // Check if status is COMPLETE
        if (status.Status == RalphStatus.StatusValues.Complete)
            return true;

        return false;
    }

    public async Task RecordTestOnlyLoopAsync(int loopNumber, CancellationToken cancellationToken = default)
    {
        if (!_exitSignals.TestOnlyLoops.Contains(loopNumber))
        {
            _exitSignals.TestOnlyLoops.Add(loopNumber);
            await SaveExitSignalsAsync(cancellationToken);
        }
    }

    public async Task RecordDoneSignalAsync(int loopNumber, CancellationToken cancellationToken = default)
    {
        if (!_exitSignals.DoneSignals.Contains(loopNumber))
        {
            _exitSignals.DoneSignals.Add(loopNumber);
            await SaveExitSignalsAsync(cancellationToken);
        }
    }

    public async Task RecordCompletionIndicatorAsync(int loopNumber, CancellationToken cancellationToken = default)
    {
        if (!_exitSignals.CompletionIndicators.Contains(loopNumber))
        {
            _exitSignals.CompletionIndicators.Add(loopNumber);
            await SaveExitSignalsAsync(cancellationToken);
        }
    }

    public Task<ExitSignalInfo> GetExitSignalsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_exitSignals);
    }

    public async Task<bool> IsFixPlanCompleteAsync(string fixPlanPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(fixPlanPath))
            return false;

        try
        {
            var content = await File.ReadAllTextAsync(fixPlanPath, cancellationToken);

            // Count completed tasks [x]
            var completedCount = CompletedTaskRegex().Matches(content).Count;

            // Count pending tasks [ ]
            var pendingCount = PendingTaskRegex().Matches(content).Count;

            // If there are tasks and all are completed
            if (completedCount > 0 && pendingCount == 0)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveExitSignalsAsync(CancellationToken cancellationToken)
    {
        await _stateStore.SaveAsync(ExitSignalsFile, _exitSignals, cancellationToken);
    }
}
