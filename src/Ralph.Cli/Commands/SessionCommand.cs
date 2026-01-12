using System.CommandLine;
using Ralph.Core.Services;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class SessionCommand
{
    public static Command Create()
    {
        var command = new Command("session", "Manage Claude Code CLI sessions");

        var statusCommand = new Command("status", "Show current session info");
        statusCommand.SetHandler(async () => await ShowStatusAsync());

        var resetCommand = new Command("reset", "Reset the current session");
        resetCommand.SetHandler(async () => await ResetAsync());

        var historyCommand = new Command("history", "Show session history");
        historyCommand.SetHandler(async () => await ShowHistoryAsync());

        command.AddCommand(statusCommand);
        command.AddCommand(resetCommand);
        command.AddCommand(historyCommand);

        return command;
    }

    private static async Task ShowStatusAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);
        var sessionManager = new SessionManager(stateStore);

        await sessionManager.InitializeAsync();
        var session = await sessionManager.GetCurrentSessionAsync();

        if (session != null)
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Session ID", session.SessionId ?? "[dim]None[/]");
            table.AddRow("Created At", session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Last Used", session.LastUsed.ToString("yyyy-MM-dd HH:mm:ss"));
            table.AddRow("Loop Count", session.LoopCount.ToString());
            table.AddRow("Is Valid", session.IsValid() ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("Is Expired", session.IsExpired ? "[red]Yes[/]" : "[green]No[/]");

            var age = DateTime.UtcNow - session.CreatedAt;
            table.AddRow("Age", $"{age.Hours}h {age.Minutes}m");

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No active session.[/]");
        }
    }

    private static async Task ResetAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);
        var sessionManager = new SessionManager(stateStore);

        await sessionManager.InitializeAsync();
        await sessionManager.ResetSessionAsync("Manual reset via CLI");

        AnsiConsole.MarkupLine("[green]Session reset successfully.[/]");
    }

    private static async Task ShowHistoryAsync()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var stateStore = new StateStore(currentDir);
        var sessionManager = new SessionManager(stateStore);

        await sessionManager.InitializeAsync();
        var history = await sessionManager.GetSessionHistoryAsync();

        if (history.Count > 0)
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("Timestamp");
            table.AddColumn("Event");
            table.AddColumn("Session ID");
            table.AddColumn("Reason");

            foreach (var entry in history.TakeLast(20))
            {
                table.AddRow(
                    entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.Event,
                    entry.SessionId ?? "[dim]-[/]",
                    entry.Reason ?? "[dim]-[/]"
                );
            }

            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No session history found.[/]");
        }
    }
}
