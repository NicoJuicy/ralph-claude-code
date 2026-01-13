using System.CommandLine;
using Ralph.Core.Services;
using Ralph.Core.Models;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show current loop status");

        command.SetHandler(async () =>
        {
            await ExecuteAsync();
        });

        return command;
    }

    public static async Task ExecuteAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);

        AnsiConsole.MarkupLine("[bold]Ralph Status[/]");
        AnsiConsole.WriteLine();

        // Load status
        var statusData = await stateStore.LoadAsync<StatusData>("status.json");

        if (statusData != null)
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Loop Number", statusData.LoopNumber.ToString());
            table.AddRow("Last Update", statusData.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Status", GetStatusMarkup(statusData.Status.Status));
            table.AddRow("Files Modified", statusData.Status.FilesModified.ToString());
            table.AddRow("Tasks Completed", statusData.Status.TasksCompletedThisLoop.ToString());
            table.AddRow("Tests", GetTestStatusMarkup(statusData.Status.TestsStatus));
            table.AddRow("Work Type", statusData.Status.WorkType);
            table.AddRow("Exit Signal", statusData.Status.ExitSignal ? "[green]Yes[/]" : "[dim]No[/]");

            if (!string.IsNullOrEmpty(statusData.Status.Recommendation))
            {
                table.AddRow("Recommendation", Markup.Escape(statusData.Status.Recommendation));
            }

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No status data found. Loop has not been run yet.[/]");
        }

        AnsiConsole.WriteLine();

        // Show circuit breaker status
        var circuitBreaker = new Core.Services.CircuitBreaker(stateStore);
        await circuitBreaker.InitializeAsync();
        var cbStatus = await circuitBreaker.GetStatusStringAsync();
        AnsiConsole.MarkupLine($"[bold]Circuit Breaker:[/] {cbStatus}");

        // Show rate limit status
        var rateLimiter = new RateLimiter(stateStore);
        await rateLimiter.InitializeAsync();
        var rateInfo = await rateLimiter.GetInfoAsync();
        AnsiConsole.MarkupLine($"[bold]API Calls:[/] {rateInfo.CallCount}/{rateInfo.MaxCallsPerHour}");

        if (rateInfo.TimeUntilReset.TotalMinutes > 0)
        {
            AnsiConsole.MarkupLine($"[bold]Reset In:[/] {rateInfo.GetCountdownString()}");
        }
    }

    private static string GetStatusMarkup(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "IN_PROGRESS" => "[yellow]IN_PROGRESS[/]",
            "COMPLETE" => "[green]COMPLETE[/]",
            "BLOCKED" => "[red]BLOCKED[/]",
            _ => status
        };
    }

    private static string GetTestStatusMarkup(string testStatus)
    {
        return testStatus.ToUpperInvariant() switch
        {
            "PASSING" => "[green]PASSING[/]",
            "FAILING" => "[red]FAILING[/]",
            "NOT_RUN" => "[dim]NOT_RUN[/]",
            _ => testStatus
        };
    }

    private class StatusData
    {
        public int LoopNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public RalphStatus Status { get; set; } = new();
    }
}
