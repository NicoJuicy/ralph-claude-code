using System.CommandLine;
using Ralph.Cli.Services;
using Spectre.Console;

namespace Ralph.Cli.Commands;

public static class SetupCommand
{
    public static Command Create()
    {
        var command = new Command("setup", "Create a new Ralph-managed project");

        var nameArgument = new Argument<string?>(
            name: "project-name",
            description: "Name of the project to create",
            getDefaultValue: () => null);

        var agentOption = new Option<string?>(
            name: "--agent",
            description: "Agent template to use (e.g., 'dotnet-cli', 'dotnet-asp-mvc'). If not specified, prompts for selection.");
        agentOption.AddAlias("-a");

        command.AddArgument(nameArgument);
        command.AddOption(agentOption);

        command.SetHandler(async (name, agent) =>
        {
            await ExecuteAsync(name, agent);
        }, nameArgument, agentOption);

        return command;
    }

    public static async Task ExecuteAsync(string? projectName, string? agentTemplate = null)
    {
        // Determine project name
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = Path.GetFileName(Directory.GetCurrentDirectory());
        }

        // Determine target directory
        var targetDir = Directory.GetCurrentDirectory();
        var isNewDirectory = false;

        // If project name is different from current directory, create new directory
        if (!projectName.Equals(Path.GetFileName(Directory.GetCurrentDirectory()), StringComparison.OrdinalIgnoreCase))
        {
            targetDir = Path.Combine(Directory.GetCurrentDirectory(), projectName);
            isNewDirectory = true;
        }

        AnsiConsole.MarkupLine($"[bold]Setting up Ralph project: [green]{projectName}[/][/]");
        AnsiConsole.MarkupLine($"[dim]Target directory: {targetDir}[/]");
        AnsiConsole.WriteLine();

        // Select agent template
        var selectedAgent = await SelectAgentTemplateAsync(agentTemplate);

        // Create directory structure
        await CreateDirectoryStructureAsync(targetDir, isNewDirectory);

        // Create template files
        await CreateTemplateFilesAsync(targetDir, projectName, selectedAgent);

        // Initialize git repository
        await InitializeGitAsync(targetDir);

        AnsiConsole.MarkupLine("[green]✓[/] Ralph project setup complete!");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. cd {(isNewDirectory ? projectName : ".")}");
        AnsiConsole.MarkupLine("  2. Edit PROMPT.md with your project instructions");
        AnsiConsole.MarkupLine("  3. Edit @fix_plan.md with your tasks");
        AnsiConsole.MarkupLine("  4. Run: ralph");
    }

    private static async Task CreateDirectoryStructureAsync(string targetDir, bool createRoot)
    {
        AnsiConsole.Status()
            .Start("Creating directory structure...", ctx =>
            {
                if (createRoot && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }

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
                    var fullPath = Path.Combine(targetDir, dir);
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }
                }
            });

        AnsiConsole.MarkupLine("[green]✓[/] Directory structure created");
    }

    private static async Task<AgentTemplateInfo> SelectAgentTemplateAsync(string? agentTemplateName)
    {
        var templates = TemplateService.GetAgentTemplates();

        // If agent template specified via command line, find it
        if (!string.IsNullOrEmpty(agentTemplateName))
        {
            var matchingTemplate = templates.FirstOrDefault(t =>
                t.FileName.Equals(agentTemplateName + ".md", StringComparison.OrdinalIgnoreCase) ||
                t.FileName.Equals(agentTemplateName, StringComparison.OrdinalIgnoreCase) ||
                t.Name.Equals(agentTemplateName, StringComparison.OrdinalIgnoreCase));

            if (matchingTemplate != null)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Using agent template: [cyan]{matchingTemplate.Name}[/]");
                return matchingTemplate;
            }

            AnsiConsole.MarkupLine($"[yellow]![/] Agent template '{agentTemplateName}' not found. Available templates:");
            foreach (var t in templates)
            {
                AnsiConsole.MarkupLine($"    [dim]-[/] {t.FileName.Replace(".md", "")}");
            }
            AnsiConsole.WriteLine();
        }

        // Prompt user to select a template
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<AgentTemplateInfo>()
                .Title("[bold]Select an agent template:[/]")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to see more templates)[/]")
                .UseConverter(t => $"{t.Name} - [dim]{t.Description}[/]")
                .AddChoices(templates));

        AnsiConsole.MarkupLine($"[green]✓[/] Selected: [cyan]{selected.Name}[/]");
        return selected;
    }

    private static async Task CreateTemplateFilesAsync(string targetDir, string projectName, AgentTemplateInfo selectedAgent)
    {
        AnsiConsole.Status()
            .Start("Creating template files...", ctx =>
            {
                // Create PROMPT.md from embedded template
                var promptPath = Path.Combine(targetDir, "PROMPT.md");
                if (!File.Exists(promptPath))
                {
                    var promptContent = TemplateService.GetTemplateContent("PROMPT.md");
                    if (promptContent != null)
                    {
                        // Replace placeholder with project name
                        promptContent = promptContent.Replace("[YOUR PROJECT NAME]", projectName);
                        File.WriteAllText(promptPath, promptContent);
                    }
                    else
                    {
                        File.WriteAllText(promptPath, GetPromptTemplate(projectName));
                    }
                }

                // Create @fix_plan.md from embedded template
                var fixPlanPath = Path.Combine(targetDir, "@fix_plan.md");
                if (!File.Exists(fixPlanPath))
                {
                    var fixPlanContent = TemplateService.GetTemplateContent("fix_plan.md");
                    File.WriteAllText(fixPlanPath, fixPlanContent ?? GetFixPlanTemplate());
                }

                // Create @AGENT.md from selected agent template
                var agentPath = Path.Combine(targetDir, "@AGENT.md");
                if (!File.Exists(agentPath))
                {
                    var agentContent = TemplateService.GetAgentTemplateContent(selectedAgent.ResourceName);
                    if (agentContent != null)
                    {
                        // Replace project name placeholder if present
                        agentContent = agentContent.Replace("MyAgentProject", projectName);
                        agentContent = agentContent.Replace("MyAgentWeb", projectName);
                        agentContent = agentContent.Replace("MyAgentApi", projectName + ".Api");
                        agentContent = agentContent.Replace("MyAgentFrontend", projectName + ".Frontend");
                        File.WriteAllText(agentPath, agentContent);
                    }
                    else
                    {
                        File.WriteAllText(agentPath, GetAgentTemplate(projectName));
                    }
                }

                // Create README.md
                var readmePath = Path.Combine(targetDir, "README.md");
                if (!File.Exists(readmePath))
                {
                    File.WriteAllText(readmePath, GetReadmeTemplate(projectName));
                }

                // Create .gitignore
                var gitignorePath = Path.Combine(targetDir, ".gitignore");
                if (!File.Exists(gitignorePath))
                {
                    File.WriteAllText(gitignorePath, GetGitignoreTemplate());
                }
            });

        AnsiConsole.MarkupLine("[green]✓[/] Template files created");
    }

    private static async Task InitializeGitAsync(string targetDir)
    {
        try
        {
            // Check if git is installed
            var gitCheck = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = targetDir
                }
            };

            gitCheck.Start();
            await gitCheck.WaitForExitAsync();

            if (gitCheck.ExitCode == 0)
            {
                // Initialize git repo
                var gitInit = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "init",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = targetDir
                    }
                };

                gitInit.Start();
                await gitInit.WaitForExitAsync();

                // Initial commit
                var gitAdd = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "add .",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = targetDir
                    }
                };
                gitAdd.Start();
                await gitAdd.WaitForExitAsync();

                var gitCommit = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = "commit -m \"Initial Ralph project setup\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = targetDir
                    }
                };
                gitCommit.Start();
                await gitCommit.WaitForExitAsync();

                AnsiConsole.MarkupLine("[green]✓[/] Git repository initialized");
            }
        }
        catch
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Could not initialize git repository (git not found)");
        }
    }

    private static string GetPromptTemplate(string projectName)
    {
        return $@"# {projectName} Development Instructions

## Context & Objectives
- Brief description of the project
- Key goals for this development cycle
- Important constraints or requirements

## Key Principles
- **ONE task per loop** - Focus on a single, specific task from @fix_plan.md
- **Search before assuming** - Always check existing code before implementing
- **Test as you go** - Write tests for new functionality

## 🧪 Testing Guidelines
- Limit testing effort to ~20% of each loop
- Prioritize implementation over test perfection
- Run existing tests to ensure no regressions

## Execution Guidelines

### Before implementing:
1. Read @fix_plan.md and select ONE task
2. Search codebase for related functionality
3. Understand existing patterns and conventions

### After implementing:
1. Test your changes
2. Update @fix_plan.md (mark completed items with [x])
3. Report status using the block below

## 🎯 Status Reporting (CRITICAL)

At the end of EVERY loop, you MUST include this status block:

```
---RALPH_STATUS---
STATUS: IN_PROGRESS | COMPLETE | BLOCKED
TASKS_COMPLETED_THIS_LOOP: <number>
FILES_MODIFIED: <number>
TESTS_STATUS: PASSING | FAILING | NOT_RUN
WORK_TYPE: IMPLEMENTATION | TESTING | DOCUMENTATION | REFACTORING
EXIT_SIGNAL: false | true
RECOMMENDATION: <one line summary of next step>
---END_RALPH_STATUS---
```

### EXIT_SIGNAL Rules
Set EXIT_SIGNAL to `true` ONLY when ALL of these conditions are met:
1. All items in @fix_plan.md are marked [x]
2. All tests are passing
3. No errors or warnings remain
4. All requirements in specs/ are implemented
5. Nothing meaningful is left to implement

## Example Status Responses

### In Progress:
```
---RALPH_STATUS---
STATUS: IN_PROGRESS
TASKS_COMPLETED_THIS_LOOP: 1
FILES_MODIFIED: 3
TESTS_STATUS: PASSING
WORK_TYPE: IMPLEMENTATION
EXIT_SIGNAL: false
RECOMMENDATION: Continue with next task in @fix_plan.md
---END_RALPH_STATUS---
```

### Complete:
```
---RALPH_STATUS---
STATUS: COMPLETE
TASKS_COMPLETED_THIS_LOOP: 1
FILES_MODIFIED: 2
TESTS_STATUS: PASSING
WORK_TYPE: IMPLEMENTATION
EXIT_SIGNAL: true
RECOMMENDATION: Project complete, all tasks done
---END_RALPH_STATUS---
```
";
    }

    private static string GetFixPlanTemplate()
    {
        return @"# Development Plan

## High Priority
- [ ] Set up project structure
- [ ] Implement core functionality
- [ ] Add basic tests

## Medium Priority
- [ ] Add documentation
- [ ] Refactor code for clarity
- [ ] Add error handling

## Low Priority
- [ ] Optimize performance
- [ ] Add advanced features
- [ ] Polish UI/UX

## Completed
<!-- Completed tasks will be moved here automatically -->

## Notes
<!-- Add any important notes, blockers, or learnings here -->
";
    }

    private static string GetAgentTemplate(string projectName)
    {
        return $@"# {projectName} Build & Run Instructions

## Project Setup
```bash
# Add your setup commands here
# Examples:
# npm install
# dotnet restore
# pip install -r requirements.txt
```

## Running Tests
```bash
# Add your test commands here
# Examples:
# npm test
# dotnet test
# pytest
```

## Build Commands
```bash
# Add your build commands here
# Examples:
# npm run build
# dotnet build
# python setup.py build
```

## Development Server
```bash
# Add your dev server commands here
# Examples:
# npm run dev
# dotnet run
# python app.py
```

## Key Learnings
<!-- Document important discoveries, optimizations, and patterns -->

## Feature Development Quality Standards

### Testing Requirements
- Test pass rate: 100% (all tests must pass)
- Coverage: Aim for reasonable coverage of critical paths
- Test quality: Tests should validate behavior, not just achieve coverage

### Git Workflow
1. Commit with clear messages: `feat(module): description`
2. Push to remote repository regularly
3. Keep commits focused and atomic

### Documentation Requirements
- Keep README up to date
- Document breaking changes
- Update API documentation when interfaces change
- Maintain this @AGENT.md file with build/run instructions
";
    }

    private static string GetReadmeTemplate(string projectName)
    {
        return $@"# {projectName}

Project managed by Ralph - Autonomous AI development loop system.

## Getting Started

1. Review the development plan in `@fix_plan.md`
2. Check build instructions in `@AGENT.md`
3. Read the AI development instructions in `PROMPT.md`
4. Run Ralph: `ralph`

## Project Structure

- `src/` - Source code
- `specs/` - Project specifications
- `examples/` - Usage examples
- `logs/` - Ralph execution logs
- `docs/generated/` - Auto-generated documentation

## Development

This project uses Ralph for autonomous development:

```bash
# Start the development loop
ralph

# Monitor progress (live dashboard)
ralph monitor

# Check status
ralph status

# Reset circuit breaker if stuck
ralph circuit-breaker reset
```

## License

<!-- Add your license here -->
";
    }

    private static string GetGitignoreTemplate()
    {
        return @"# Ralph state files
.call_count
.last_reset
.exit_signals
.circuit_breaker_state
.circuit_breaker_history
.claude_session_id
.ralph_session
.ralph_session_history
status.json
progress.json

# Logs
logs/
*.log

# Generated docs
docs/generated/

# Build artifacts
node_modules/
target/
dist/
build/
bin/
obj/
__pycache__/
*.pyc
*.pyo

# IDE files
.vscode/
.idea/
*.swp
*.swo
*~
.DS_Store

# Environment
.env
.env.local
*.env
";
    }
}
