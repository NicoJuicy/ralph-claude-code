namespace Ralph.Core.Models;

/// <summary>
/// Tracks exit signals across multiple loops
/// </summary>
public class ExitSignalInfo
{
    public List<int> TestOnlyLoops { get; set; } = new();
    public List<int> DoneSignals { get; set; } = new();
    public List<int> CompletionIndicators { get; set; } = new();

    // Thresholds
    public int MaxConsecutiveTestLoops { get; set; } = 3;
    public int MaxConsecutiveDoneSignals { get; set; } = 2;
    public int TestPercentageThreshold { get; set; } = 30;

    /// <summary>
    /// Check if we should exit based on accumulated signals
    /// </summary>
    public bool ShouldExit(int currentLoop)
    {
        // Check consecutive test-only loops
        var recentTestLoops = TestOnlyLoops
            .Where(l => l >= currentLoop - MaxConsecutiveTestLoops)
            .Count();

        if (recentTestLoops >= MaxConsecutiveTestLoops)
            return true;

        // Check consecutive done signals
        var recentDoneSignals = DoneSignals
            .Where(l => l >= currentLoop - MaxConsecutiveDoneSignals)
            .Count();

        if (recentDoneSignals >= MaxConsecutiveDoneSignals)
            return true;

        // Check test percentage in recent loops (last 10)
        var recentLoops = currentLoop >= 10 ? 10 : currentLoop;
        var recentTestCount = TestOnlyLoops
            .Where(l => l >= currentLoop - recentLoops)
            .Count();

        var testPercentage = (recentTestCount * 100) / recentLoops;
        if (testPercentage > TestPercentageThreshold)
            return true;

        return false;
    }
}
