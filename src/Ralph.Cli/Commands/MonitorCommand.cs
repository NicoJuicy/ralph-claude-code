using System.CommandLine;
using Ralph.Core.Services;
using Ralph.Core.Models;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class MonitorCommand
{
    public static Command Create()
    {
        var command = new Command("monitor", "Run live monitoring dashboard (replaces tmux on Windows)");

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

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold green]Ralph Live Monitor[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to exit[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Live(CreateMonitorLayout(null, null, null, null))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                while (true)
                {
                    try
                    {
                        // Load current status
                        var status = await LoadStatusAsync(stateStore);

                        // Load circuit breaker state
                        var circuitBreaker = new Core.Services.CircuitBreaker(stateStore);
                        await circuitBreaker.InitializeAsync();
                        var cbState = await circuitBreaker.GetStateAsync();

                        // Load rate limit info
                        var rateLimiter = new RateLimiter(stateStore);
                        await rateLimiter.InitializeAsync();
                        var rateInfo = await rateLimiter.GetInfoAsync();

                        // Load recent logs
                        var recentLogs = await LoadRecentLogsAsync(stateStore);

                        // Update layout
                        ctx.UpdateTarget(CreateMonitorLayout(status, cbState, rateInfo, recentLogs));
                    }
                    catch (Exception ex)
                    {
                        // Display error but keep running
                        ctx.UpdateTarget(CreateErrorLayout(ex.Message));
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            });
    }

    private static Layout CreateMonitorLayout(
        StatusData? status,
        CircuitBreakerInfo? cbState,
        RateLimitInfo? rateInfo,
        List<string>? logs)
    {
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Main"),
                new Layout("Logs").Size(10)
            );

        // Header
        layout["Header"].Update(CreateHeaderPanel());

        // Main area - split into status and circuit breaker
        layout["Main"].SplitColumns(
            new Layout("Status"),
            new Layout("Metrics")
        );

        layout["Status"].Update(CreateStatusPanel(status));
        layout["Metrics"].Update(CreateMetricsPanel(cbState, rateInfo));

        // Logs
        layout["Logs"].Update(CreateLogsPanel(logs));

        return layout;
    }

    private static Panel CreateHeaderPanel()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddRow($"[bold green]Ralph Monitor[/] [dim]{DateTime.Now:yyyy-MM-dd HH:mm:ss}[/]");

        return new Panel(grid)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Green);
    }

    private static Panel CreateStatusPanel(StatusData? status)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        if (status != null)
        {
            grid.AddRow("[bold]Loop Number:[/]", status.LoopNumber.ToString());
            grid.AddRow("[bold]Status:[/]", GetStatusMarkup(status.Status.Status));
            grid.AddRow("[bold]Files Modified:[/]", status.Status.FilesModified.ToString());
            grid.AddRow("[bold]Tasks Completed:[/]", status.Status.TasksCompletedThisLoop.ToString());
            grid.AddRow("[bold]Tests:[/]", GetTestStatusMarkup(status.Status.TestsStatus));
            grid.AddRow("[bold]Work Type:[/]", status.Status.WorkType);
            grid.AddRow("[bold]Exit Signal:[/]", status.Status.ExitSignal ? "[green]Yes[/]" : "[dim]No[/]");

            if (!string.IsNullOrEmpty(status.Status.Recommendation))
            {
                grid.AddRow();
                grid.AddRow("[bold]Recommendation:[/]", Markup.Escape(status.Status.Recommendation));
            }
        }
        else
        {
            grid.AddRow("[dim]No status data available[/]", "");
            grid.AddRow("[dim]Waiting for loop to start...[/]", "");
        }

        return new Panel(grid)
            .Header("[bold]Current Loop Status[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue);
    }

    private static Panel CreateMetricsPanel(CircuitBreakerInfo? cbState, RateLimitInfo? rateInfo)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        // Circuit breaker
        grid.AddRow("[bold underline]Circuit Breaker[/]", "");

        if (cbState != null)
        {
            var stateColor = cbState.State switch
            {
                CircuitBreakerState.Closed => "green",
                CircuitBreakerState.HalfOpen => "yellow",
                CircuitBreakerState.Open => "red",
                _ => "white"
            };

            grid.AddRow("State:", $"[{stateColor}]{cbState.State}[/]");
            grid.AddRow("No Progress:", $"{cbState.NoProgressCount}/{cbState.NoProgressThreshold}");
            grid.AddRow("Same Error:", $"{cbState.SameErrorCount}/{cbState.SameErrorThreshold}");

            if (!string.IsNullOrEmpty(cbState.StateChangeReason))
            {
                grid.AddRow("Reason:", Markup.Escape(cbState.StateChangeReason));
            }
        }
        else
        {
            grid.AddRow("[dim]Not initialized[/]", "");
        }

        grid.AddRow();
        grid.AddRow("[bold underline]Rate Limiting[/]", "");

        if (rateInfo != null)
        {
            var limitColor = rateInfo.IsLimitReached ? "red" : "green";
            grid.AddRow("API Calls:", $"[{limitColor}]{rateInfo.CallCount}/{rateInfo.MaxCallsPerHour}[/]");

            if (rateInfo.TimeUntilReset.TotalMinutes > 0)
            {
                grid.AddRow("Reset In:", rateInfo.GetCountdownString());
            }
        }
        else
        {
            grid.AddRow("[dim]Not initialized[/]", "");
        }

        return new Panel(grid)
            .Header("[bold]Metrics & Limits[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Yellow);
    }

    private static Panel CreateLogsPanel(List<string>? logs)
    {
        var content = logs != null && logs.Count > 0
            ? string.Join("\n", logs.TakeLast(8).Select(Markup.Escape))
            : "No logs available";

        var markup = new Markup($"[grey]{content}[/]");

        return new Panel(markup)
            .Header("[bold]Recent Activity[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Grey);
    }


    private static Layout CreateErrorLayout(string error)
    {
        var layout = new Layout("Root");
        var panel = new Panel($"[red]Error:[/] {Markup.Escape(error)}")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red);

        layout.Update(panel);
        return layout;
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

    private static async Task<StatusData?> LoadStatusAsync(StateStore stateStore)
    {
        return await stateStore.LoadAsync<StatusData>("status.json");
    }

    private static async Task<List<string>> LoadRecentLogsAsync(StateStore stateStore)
    {
        try
        {
            var logs = await stateStore.ReadLogAsync("logs/ralph.log");
            return logs.TakeLast(20).ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private class StatusData
    {
        public int LoopNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public RalphStatus Status { get; set; } = new();
    }
}
