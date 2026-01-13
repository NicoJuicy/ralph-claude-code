using System.CommandLine;
using Ralph.Core.Services;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class CircuitBreakerCommand
{
    public static Command Create()
    {
        var command = new Command("circuit-breaker", "Manage circuit breaker state");

        var statusCommand = new Command("status", "Show circuit breaker status");
        statusCommand.SetHandler(async () => await ShowStatusAsync());

        var resetCommand = new Command("reset", "Reset circuit breaker to CLOSED state");
        resetCommand.SetHandler(async () => await ResetAsync());

        command.AddCommand(statusCommand);
        command.AddCommand(resetCommand);

        return command;
    }

    private static async Task ShowStatusAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);
        var circuitBreaker = new Core.Services.CircuitBreaker(stateStore);

        await circuitBreaker.InitializeAsync();
        var state = await circuitBreaker.GetStateAsync();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        var stateColor = state.State switch
        {
            Core.Models.CircuitBreakerState.Closed => "green",
            Core.Models.CircuitBreakerState.HalfOpen => "yellow",
            Core.Models.CircuitBreakerState.Open => "red",
            _ => "white"
        };

        table.AddRow("State", $"[{stateColor}]{state.State}[/]");
        table.AddRow("No Progress Count", $"{state.NoProgressCount}/{state.NoProgressThreshold}");
        table.AddRow("Same Error Count", $"{state.SameErrorCount}/{state.SameErrorThreshold}");
        table.AddRow("Last Files Modified", state.LastFilesModified.ToString());

        if (!string.IsNullOrEmpty(state.LastError))
        {
            table.AddRow("Last Error", Markup.Escape(state.LastError));
        }

        if (!string.IsNullOrEmpty(state.StateChangeReason))
        {
            table.AddRow("Reason", Markup.Escape(state.StateChangeReason));
        }

        table.AddRow("Last State Change", state.LastStateChange.ToString("yyyy-MM-dd HH:mm:ss"));

        AnsiConsole.Write(table);
    }

    private static async Task ResetAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);
        var circuitBreaker = new Core.Services.CircuitBreaker(stateStore);

        await circuitBreaker.InitializeAsync();
        await circuitBreaker.ResetAsync();

        // Also reset session
        var sessionManager = new SessionManager(stateStore);
        await sessionManager.InitializeAsync();
        await sessionManager.ResetSessionAsync("Circuit breaker manual reset");

        AnsiConsole.MarkupLine("[green]Circuit breaker reset to CLOSED state.[/]");
        AnsiConsole.MarkupLine("[green]Session reset.[/]");
    }
}
