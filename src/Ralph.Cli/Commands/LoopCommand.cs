using System.CommandLine;
using Ralph.Core.Interfaces;
using Ralph.Core.Services;
using Ralph.Core.Models;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class LoopCommand
{
    public static Command Create()
    {
        var command = new Command("loop", "Run the autonomous development loop");

        var monitorOption = new Option<bool>("--monitor", "Run with integrated monitoring UI");
        var callsOption = new Option<int>("--calls", () => 100, "Max API calls per hour");
        var timeoutOption = new Option<int>("--timeout", () => 15, "Claude Code timeout in minutes");
        var promptOption = new Option<string?>("--prompt", "Custom prompt file");
        var noContinueOption = new Option<bool>("--no-continue", "Disable session continuity");
        var verboseOption = new Option<bool>("--verbose", "Verbose progress");
        var outputFormatOption = new Option<string>("--output-format", () => "json", "Output format (json or text)");
        var allowedToolsOption = new Option<string?>("--allowed-tools", "Allowed tools for Claude");

        command.AddOption(monitorOption);
        command.AddOption(callsOption);
        command.AddOption(timeoutOption);
        command.AddOption(promptOption);
        command.AddOption(noContinueOption);
        command.AddOption(verboseOption);
        command.AddOption(outputFormatOption);
        command.AddOption(allowedToolsOption);

        command.SetHandler(async (monitor, calls, timeout, prompt, noContinue, verbose, outputFormat, allowedTools) =>
        {
            await ExecuteAsync(new LoopOptions
            {
                Monitor = monitor,
                MaxCallsPerHour = calls,
                TimeoutMinutes = timeout,
                PromptFile = prompt,
                NoContinue = noContinue,
                Verbose = verbose,
                OutputFormat = outputFormat,
                AllowedTools = allowedTools
            });
        }, monitorOption, callsOption, timeoutOption, promptOption, noContinueOption,
           verboseOption, outputFormatOption, allowedToolsOption);

        return command;
    }

    public static async Task ExecuteAsync(LoopOptions options)
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Initialize services
        var stateStore = new StateStore(currentDir);
        var circuitBreaker = new CircuitBreaker(stateStore);
        var sessionManager = new SessionManager(stateStore);
        var rateLimiter = new RateLimiter(stateStore);
        var responseAnalyzer = new ResponseAnalyzer();
        var exitDetector = new ExitDetector(stateStore);

        await circuitBreaker.InitializeAsync();
        await sessionManager.InitializeAsync();
        await rateLimiter.InitializeAsync(options.MaxCallsPerHour);
        await exitDetector.InitializeAsync();

        // Determine prompt file
        var promptFile = options.PromptFile ?? Path.Combine(currentDir, "PROMPT.md");
        if (!File.Exists(promptFile))
        {
            AnsiConsole.MarkupLine("[red]Error: PROMPT.md not found in current directory.[/]");
            AnsiConsole.MarkupLine("[yellow]Run 'ralph setup' to create a new Ralph project.[/]");
            return;
        }

        var loopNumber = 0;
        var maxLoops = 100; // Safety limit

        AnsiConsole.MarkupLine("[green]Starting Ralph autonomous development loop...[/]");
        AnsiConsole.MarkupLine($"[dim]Prompt file: {promptFile}[/]");
        AnsiConsole.MarkupLine($"[dim]Rate limit: {options.MaxCallsPerHour} calls/hour[/]");
        AnsiConsole.MarkupLine($"[dim]Session continuity: {(options.NoContinue ? "disabled" : "enabled")}[/]");
        AnsiConsole.WriteLine();

        try
        {
            while (loopNumber < maxLoops)
            {
                loopNumber++;

                // Check circuit breaker
                if (!await circuitBreaker.CanExecuteAsync())
                {
                    var cbStatus = await circuitBreaker.GetStatusStringAsync();
                    AnsiConsole.MarkupLine($"[red]Circuit breaker {cbStatus}[/]");
                    AnsiConsole.MarkupLine("[yellow]Run 'ralph circuit-breaker reset' to continue.[/]");
                    break;
                }

                // Check rate limit
                if (!await rateLimiter.CanMakeCallAsync())
                {
                    var info = await rateLimiter.GetInfoAsync();
                    AnsiConsole.MarkupLine($"[yellow]Rate limit reached ({info.CallCount}/{info.MaxCallsPerHour})[/]");

                    await rateLimiter.WaitForResetAsync(message =>
                    {
                        AnsiConsole.MarkupLine($"[dim]{message}[/]");
                    });
                }

                // Display loop header
                AnsiConsole.MarkupLine($"[bold blue]═══ Loop {loopNumber} ═══[/]");

                // Read prompt
                var promptContent = await File.ReadAllTextAsync(promptFile);

                // Execute Claude Code CLI
                var output = await ExecuteClaudeAsync(promptContent, options, sessionManager);

                // Record API call
                await rateLimiter.RecordCallAsync();

                // Parse response
                var status = await responseAnalyzer.ExtractStatusAsync(output);

                if (status == null)
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: Could not parse Ralph status from output[/]");
                    status = new RalphStatus { FilesModified = 0 };
                }

                // Display status
                DisplayLoopStatus(loopNumber, status);

                // Detect test-only loop
                var isTestOnly = await responseAnalyzer.DetectTestOnlyLoopAsync(output);
                if (isTestOnly)
                {
                    await exitDetector.RecordTestOnlyLoopAsync(loopNumber);
                }

                // Detect completion signals
                var hasCompletionSignal = await responseAnalyzer.DetectCompletionSignalsAsync(output);
                if (hasCompletionSignal)
                {
                    await exitDetector.RecordCompletionIndicatorAsync(loopNumber);
                }

                // Extract errors
                var errors = await responseAnalyzer.ExtractErrorsAsync(output);
                var errorString = errors.Any() ? string.Join("\n", errors) : null;

                // Record loop result in circuit breaker
                await circuitBreaker.RecordLoopResultAsync(status.FilesModified, errorString);

                // Check if should exit
                var shouldExit = await exitDetector.ShouldExitAsync(loopNumber, status);

                if (shouldExit)
                {
                    var exitSignals = await exitDetector.GetExitSignalsAsync();
                    var confidence = await responseAnalyzer.CalculateExitConfidenceAsync(status, exitSignals);

                    AnsiConsole.MarkupLine($"[green]Exit conditions met (confidence: {confidence}%)[/]");
                    await sessionManager.ResetSessionAsync("Project completion");
                    break;
                }

                // Save status
                await SaveStatusAsync(stateStore, loopNumber, status);

                AnsiConsole.WriteLine();
            }

            if (loopNumber >= maxLoops)
            {
                AnsiConsole.MarkupLine("[yellow]Reached maximum loop count safety limit.[/]");
            }

            AnsiConsole.MarkupLine("[green]Ralph loop completed.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            await sessionManager.ResetSessionAsync("Error occurred");
            throw;
        }
    }

    private static async Task<string> ExecuteClaudeAsync(string promptContent, LoopOptions options, ISessionManager sessionManager)
    {
        var args = new List<string> { "code" };

        // Add prompt
        args.Add("-p");
        args.Add(promptContent);

        // Add output format
        if (!string.IsNullOrEmpty(options.OutputFormat))
        {
            args.Add("--output-format");
            args.Add(options.OutputFormat);
        }

        // Add allowed tools
        if (!string.IsNullOrEmpty(options.AllowedTools))
        {
            args.Add("--allowed-tools");
            args.Add(options.AllowedTools);
        }

        // Add session continuity
        if (!options.NoContinue)
        {
            var currentSession = await sessionManager.GetCurrentSessionAsync();
            if (currentSession != null && await sessionManager.ShouldResumeSessionAsync())
            {
                args.Add("--continue");
                // Note: Session ID would be passed via stdin/environment in actual implementation
            }
        }

        // Execute Claude Code CLI
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", args.Select(a => $"\"{a}\"")),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                errorBuilder.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait with timeout
        var timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException($"Claude Code execution exceeded timeout of {options.TimeoutMinutes} minutes");
        }

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        // Try to extract session ID from output
        var sessionId = ExtractSessionId(output);
        if (!string.IsNullOrEmpty(sessionId))
        {
            await sessionManager.StoreSessionAsync(sessionId);
        }

        return output;
    }

    private static string? ExtractSessionId(string output)
    {
        // Try to extract session ID from JSON response or output
        // This is a simplified implementation
        var match = System.Text.RegularExpressions.Regex.Match(output, @"""sessionId""\s*:\s*""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static void DisplayLoopStatus(int loopNumber, RalphStatus status)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        table.AddRow("Status", GetStatusMarkup(status.Status));
        table.AddRow("Files Modified", status.FilesModified.ToString());
        table.AddRow("Tasks Completed", status.TasksCompletedThisLoop.ToString());
        table.AddRow("Tests", GetTestStatusMarkup(status.TestsStatus));
        table.AddRow("Work Type", status.WorkType);
        table.AddRow("Exit Signal", status.ExitSignal ? "[green]true[/]" : "[dim]false[/]");

        if (!string.IsNullOrEmpty(status.Recommendation))
        {
            table.AddRow("Recommendation", status.Recommendation);
        }

        AnsiConsole.Write(table);
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

    private static async Task SaveStatusAsync(IStateStore stateStore, int loopNumber, RalphStatus status)
    {
        var statusData = new
        {
            LoopNumber = loopNumber,
            Timestamp = DateTime.UtcNow,
            Status = status
        };

        await stateStore.SaveAsync("status.json", statusData);
    }
}
