using System.CommandLine;
using Ralph.Cli.Commands;
using Ralph.Core.Interfaces;
using Ralph.Core.Services;

namespace Ralph.Cli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Create root command
        var rootCommand = new RootCommand("yolo - Autonomous AI development loop system");

        // Add commands
        rootCommand.AddCommand(LoopCommand.Create());
        rootCommand.AddCommand(MonitorCommand.Create());
        rootCommand.AddCommand(SetupCommand.Create());
        rootCommand.AddCommand(ImportCommand.Create());
        rootCommand.AddCommand(StatusCommand.Create());
        rootCommand.AddCommand(CircuitBreakerCommand.Create());
        rootCommand.AddCommand(SessionCommand.Create());

        // Add global options that apply to the loop command when run directly
        var monitorOption = new Option<bool>(
            aliases: new[] { "--monitor", "-m" },
            description: "Run with integrated monitoring UI");

        var callsOption = new Option<int>(
            aliases: new[] { "--calls" },
            getDefaultValue: () => 100,
            description: "Max API calls per hour");

        var timeoutOption = new Option<int>(
            aliases: new[] { "--timeout" },
            getDefaultValue: () => 15,
            description: "Claude Code execution timeout in minutes");

        var promptOption = new Option<string?>(
            aliases: new[] { "--prompt", "-p" },
            description: "Custom prompt file path");

        var noContinueOption = new Option<bool>(
            aliases: new[] { "--no-continue" },
            description: "Disable session continuity");

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Verbose progress updates");

        // Add options to root command (for backward compatibility)
        rootCommand.AddOption(monitorOption);
        rootCommand.AddOption(callsOption);
        rootCommand.AddOption(timeoutOption);
        rootCommand.AddOption(promptOption);
        rootCommand.AddOption(noContinueOption);
        rootCommand.AddOption(verboseOption);

        // Set handler for root command (run loop by default)
        rootCommand.SetHandler(async (monitor, calls, timeout, prompt, noContinue, verbose) =>
        {
            await LoopCommand.ExecuteAsync(new LoopOptions
            {
                Monitor = monitor,
                MaxCallsPerHour = calls,
                TimeoutMinutes = timeout,
                PromptFile = prompt,
                NoContinue = noContinue,
                Verbose = verbose
            });
        }, monitorOption, callsOption, timeoutOption, promptOption, noContinueOption, verboseOption);

        return await rootCommand.InvokeAsync(args);
    }
}

/// <summary>
/// Options for the loop command
/// </summary>
public class LoopOptions
{
    public bool Monitor { get; set; }
    public int MaxCallsPerHour { get; set; } = 100;
    public int TimeoutMinutes { get; set; } = 15;
    public string? PromptFile { get; set; }
    public bool NoContinue { get; set; }
    public bool Verbose { get; set; }
    public string OutputFormat { get; set; } = "json";
    public string? AllowedTools { get; set; }
}
