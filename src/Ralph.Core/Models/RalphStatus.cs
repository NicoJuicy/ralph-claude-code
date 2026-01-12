namespace Ralph.Core.Models;

/// <summary>
/// Represents the status block returned by Claude at the end of each loop
/// </summary>
public class RalphStatus
{
    public string Status { get; set; } = "IN_PROGRESS";
    public int TasksCompletedThisLoop { get; set; }
    public int FilesModified { get; set; }
    public string TestsStatus { get; set; } = "NOT_RUN";
    public string WorkType { get; set; } = "IMPLEMENTATION";
    public bool ExitSignal { get; set; }
    public string? Recommendation { get; set; }
    public int? ErrorCount { get; set; }
    public string? Summary { get; set; }

    /// <summary>
    /// Valid status values
    /// </summary>
    public static class StatusValues
    {
        public const string InProgress = "IN_PROGRESS";
        public const string Complete = "COMPLETE";
        public const string Blocked = "BLOCKED";
    }

    /// <summary>
    /// Valid test status values
    /// </summary>
    public static class TestStatusValues
    {
        public const string Passing = "PASSING";
        public const string Failing = "FAILING";
        public const string NotRun = "NOT_RUN";
    }

    /// <summary>
    /// Valid work type values
    /// </summary>
    public static class WorkTypeValues
    {
        public const string Implementation = "IMPLEMENTATION";
        public const string Testing = "TESTING";
        public const string Documentation = "DOCUMENTATION";
        public const string Refactoring = "REFACTORING";
    }
}
