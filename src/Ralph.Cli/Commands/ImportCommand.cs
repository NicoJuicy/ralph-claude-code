using System.CommandLine;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class ImportCommand
{
    public static Command Create()
    {
        var command = new Command("import", "Import a PRD or specification document to Ralph format");

        var sourceArgument = new Argument<string>(
            name: "source-file",
            description: "Path to the PRD/specification file (.md, .txt, .json)");

        var projectNameArgument = new Argument<string?>(
            name: "project-name",
            description: "Name for the new project (optional)",
            getDefaultValue: () => null);

        command.AddArgument(sourceArgument);
        command.AddArgument(projectNameArgument);

        command.SetHandler(async (source, projectName) =>
        {
            await ExecuteAsync(source, projectName);
        }, sourceArgument, projectNameArgument);

        return command;
    }

    public static async Task ExecuteAsync(string sourceFile, string? projectName)
    {
        // Validate source file
        if (!File.Exists(sourceFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: Source file not found: {sourceFile}[/]");
            return;
        }

        // Determine project name
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = Path.GetFileNameWithoutExtension(sourceFile)
                .Replace("_", "-")
                .Replace(" ", "-")
                .ToLowerInvariant();
        }

        AnsiConsole.MarkupLine($"[bold]Importing PRD to Ralph project: [green]{projectName}[/][/]");
        AnsiConsole.MarkupLine($"[dim]Source file: {sourceFile}[/]");
        AnsiConsole.WriteLine();

        // Read source content
        var content = await File.ReadAllTextAsync(sourceFile);

        // Use Claude to convert PRD to Ralph format
        var conversionResult = await AnsiConsole.Status()
            .StartAsync("Converting PRD with Claude Code...", async ctx =>
            {
                return await ConvertPrdWithClaudeAsync(content, projectName);
            });

        if (conversionResult == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to convert PRD.[/]");
            return;
        }

        // Create project directory
        var targetDir = Path.Combine(Directory.GetCurrentDirectory(), projectName);

        if (Directory.Exists(targetDir))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Directory '{projectName}' already exists.[/]");
            if (!AnsiConsole.Confirm("Overwrite?", false))
            {
                AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
                return;
            }
        }

        Directory.CreateDirectory(targetDir);

        // Create directory structure
        await CreateDirectoryStructureAsync(targetDir);

        // Save converted files
        await SaveConvertedFilesAsync(targetDir, conversionResult);

        // Save original PRD in specs/
        var specsDir = Path.Combine(targetDir, "specs");
        Directory.CreateDirectory(specsDir);
        await File.WriteAllTextAsync(Path.Combine(specsDir, "requirements.md"), content);

        // Initialize git
        await InitializeGitAsync(targetDir);

        AnsiConsole.MarkupLine("[green]✓[/] PRD import complete!");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. cd {projectName}");
        AnsiConsole.MarkupLine("  2. Review PROMPT.md and @fix_plan.md");
        AnsiConsole.MarkupLine("  3. Run: ralph");
    }

    private static async Task<ConversionResult?> ConvertPrdWithClaudeAsync(string prdContent, string projectName)
    {
        try
        {
            // Create a prompt for Claude to convert the PRD
            var prompt = $@"Convert this PRD/specification document into Ralph format.

Create the following:

1. **PROMPT.md**: Development instructions for Claude Code. Include:
   - Context & objectives from the PRD
   - Key principles (ONE task per loop, search before assuming, test as you go)
   - Testing guidelines (~20% effort)
   - The status reporting block (CRITICAL)

2. **@fix_plan.md**: Break down the requirements into prioritized tasks:
   - High priority tasks (core functionality)
   - Medium priority tasks (nice-to-haves)
   - Low priority tasks (future enhancements)
   - Use markdown checkboxes: - [ ] Task description

3. **@AGENT.md**: Build and run instructions based on the tech stack mentioned in the PRD

Output your response as JSON with these fields:
- prompt_md: string (content for PROMPT.md)
- fix_plan_md: string (content for @fix_plan.md)
- agent_md: string (content for @AGENT.md)

Here's the PRD:

---
{prdContent}
---
";

            // Execute Claude Code CLI
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = $"code -p \"{prompt}\" --output-format json",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();

            if (!process.WaitForExit(60000)) // 1 minute timeout
            {
                process.Kill();
                return null;
            }

            var output = outputBuilder.ToString();

            // Try to parse JSON response
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(output);
                var root = json.RootElement;

                return new ConversionResult
                {
                    PromptMd = root.GetProperty("prompt_md").GetString() ?? "",
                    FixPlanMd = root.GetProperty("fix_plan_md").GetString() ?? "",
                    AgentMd = root.GetProperty("agent_md").GetString() ?? ""
                };
            }
            catch
            {
                // Fallback: use simple template-based conversion
                return CreateFallbackConversion(prdContent, projectName);
            }
        }
        catch
        {
            return CreateFallbackConversion(prdContent, projectName);
        }
    }

    private static ConversionResult CreateFallbackConversion(string prdContent, string projectName)
    {
        // Simple fallback conversion
        return new ConversionResult
        {
            PromptMd = $@"# {projectName} Development Instructions

## Context & Objectives
{ExtractFirstParagraph(prdContent)}

## Key Principles
- **ONE task per loop** - Focus on a single, specific task from @fix_plan.md
- **Search before assuming** - Always check existing code before implementing
- **Test as you go** - Write tests for new functionality

[Include full status reporting block as per template...]
",
            FixPlanMd = @"# Development Plan

## High Priority
- [ ] Implement core requirements from PRD
- [ ] Set up project structure

## Medium Priority
- [ ] Add tests
- [ ] Add documentation

## Low Priority
- [ ] Polish and optimize

## Notes
Review the original PRD in specs/requirements.md
",
            AgentMd = $@"# {projectName} Build & Run Instructions

## Project Setup
```bash
# Add setup commands based on your tech stack
```

## Running Tests
```bash
# Add test commands
```

[Include standard template content...]
"
        };
    }

    private static string ExtractFirstParagraph(string content)
    {
        var lines = content.Split('\n');
        var paragraph = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (paragraph.Length > 0)
                    break;
                continue;
            }

            paragraph.AppendLine(line);

            if (paragraph.Length > 300)
                break;
        }

        return paragraph.ToString();
    }

    private static async Task CreateDirectoryStructureAsync(string targetDir)
    {
        var directories = new[]
        {
            "src",
            "specs",
            "examples",
            "logs",
            Path.Combine("docs", "generated")
        };

        foreach (var dir in directories)
        {
            Directory.CreateDirectory(Path.Combine(targetDir, dir));
        }
    }

    private static async Task SaveConvertedFilesAsync(string targetDir, ConversionResult result)
    {
        await File.WriteAllTextAsync(Path.Combine(targetDir, "PROMPT.md"), result.PromptMd);
        await File.WriteAllTextAsync(Path.Combine(targetDir, "@fix_plan.md"), result.FixPlanMd);
        await File.WriteAllTextAsync(Path.Combine(targetDir, "@AGENT.md"), result.AgentMd);

        // Create README
        var readmePath = Path.Combine(targetDir, "README.md");
        await File.WriteAllTextAsync(readmePath, $@"# {Path.GetFileName(targetDir)}

Project created from PRD import.

## Getting Started

1. Review the development plan in `@fix_plan.md`
2. Check build instructions in `@AGENT.md`
3. Read the AI development instructions in `PROMPT.md`
4. Run Ralph: `ralph`

Original requirements are in `specs/requirements.md`.
");

        // Create .gitignore
        var gitignorePath = Path.Combine(targetDir, ".gitignore");
        await File.WriteAllTextAsync(gitignorePath, @"# Ralph state files
.call_count
.circuit_breaker_state
.claude_session_id
status.json
logs/
");
    }

    private static async Task InitializeGitAsync(string targetDir)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "init",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = targetDir
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }
        catch
        {
            // Silently fail if git is not available
        }
    }

    private class ConversionResult
    {
        public string PromptMd { get; set; } = "";
        public string FixPlanMd { get; set; } = "";
        public string AgentMd { get; set; } = "";
    }
}
