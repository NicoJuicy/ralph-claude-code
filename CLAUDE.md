# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

This is the Ralph for Claude Code repository - an autonomous AI development loop system that enables continuous development cycles with intelligent exit detection and rate limiting.

**Version**: v1.0.0 (.NET) | **Platform**: .NET 8.0 | **Tests**: 30+ xUnit tests passing | **Language**: C# 12

**IMPORTANT**: This repository has been **migrated from bash to .NET 8.0**. All bash scripts have been removed. The system is now a cross-platform C# application with Windows as the primary platform.

## Core Architecture (.NET)

The system consists of a .NET solution with three main projects:

### Solution Structure

```
Ralph.sln
├── src/Ralph.Core/         - Core library (business logic)
├── src/Ralph.Cli/          - CLI application (commands)
└── tests/Ralph.Tests/      - xUnit test suite
```

### Core Library Components (Ralph.Core)

**Models** (`src/Ralph.Core/Models/`):
1. **CircuitBreakerState.cs** - Circuit breaker state and metrics
   - Enum: `Closed`, `HalfOpen`, `Open`
   - `CircuitBreakerInfo` class with thresholds and counters

2. **RalphStatus.cs** - Status block from Claude Code responses
   - Properties: `Status`, `FilesModified`, `TasksCompletedThisLoop`, `TestsStatus`, `WorkType`, `ExitSignal`
   - Constants for valid values

3. **ClaudeResponse.cs** - Parsed Claude Code CLI responses
   - Supports flat JSON and Claude CLI format
   - `ClaudeMetadata` for nested metadata

4. **SessionInfo.cs** - Session continuity management
   - 24-hour expiration
   - `SessionHistoryEntry` for transition tracking

5. **RateLimitInfo.cs** - API rate limiting state
   - Hourly reset logic
   - Countdown calculations

6. **ExitSignalInfo.cs** - Exit detection tracking
   - Test-only loop tracking
   - Done signal accumulation
   - Threshold-based exit logic

**Services** (`src/Ralph.Core/Services/`):
1. **StateStore.cs** - File-based state persistence
   - JSON serialization with `System.Text.Json`
   - Async file I/O
   - Log management

2. **CircuitBreaker.cs** - Prevents runaway loops
   - Three-state machine implementation
   - Tracks: no-progress count, same-error count, file modifications
   - State transitions: `Closed` → `HalfOpen` → `Open` (or back to `Closed`)
   - Default thresholds: 3 no-progress, 5 same-error
   - Manual reset capability

3. **SessionManager.cs** - Claude Code CLI session continuity
   - Session lifecycle: create, store, resume, reset, expire
   - Automatic expiration after 24 hours
   - History tracking (last 50 transitions)
   - Auto-reset triggers: circuit breaker open, manual interrupt, project completion

4. **RateLimiter.cs** - API call management
   - Hourly call tracking with automatic reset
   - Configurable max calls per hour (default: 100)
   - Countdown display for rate limit resets
   - Persistent state across restarts

5. **ResponseAnalyzer.cs** - Analyzes Claude Code output
   - Parses JSON and text responses
   - Extracts `RalphStatus` blocks
   - Detects test-only loops
   - Detects stuck loops (multi-line error matching)
   - Two-stage error filtering (eliminates JSON field false positives)
   - Calculates exit confidence scores

6. **ExitDetector.cs** - Determines when to exit the loop
   - Multiple detection methods: test-only loops, done signals, completion indicators
   - Checks `@fix_plan.md` for completed tasks (regex: `^\s*-\s*\[(x|X)\]`)
   - Configurable thresholds
   - Accumulates signals across loops

**Interfaces** (`src/Ralph.Core/Interfaces/`):
- `ICircuitBreaker`, `ISessionManager`, `IRateLimiter`, `IResponseAnalyzer`, `IExitDetector`, `IStateStore`
- All services implement interfaces for testability

**Utilities** (`src/Ralph.Core/Utils/`):
- **DateUtils.cs** - Cross-platform date handling
  - ISO timestamps, epoch seconds, hour boundaries
  - Works identically on Windows/Linux/macOS

### CLI Application (Ralph.Cli)

**Commands** (`src/Ralph.Cli/Commands/`):

1. **LoopCommand.cs** - Main autonomous development loop
   - Replaces `ralph_loop.sh`
   - Orchestrates all services (circuit breaker, rate limiter, session manager, etc.)
   - Executes Claude Code CLI via `Process.Start`
   - Parses responses and updates state
   - Displays status with Spectre.Console tables
   - Supports all original flags: `--monitor`, `--calls`, `--timeout`, `--prompt`, `--no-continue`

2. **MonitorCommand.cs** - Live monitoring dashboard
   - Replaces `ralph_monitor.sh` and tmux
   - Uses `Spectre.Console.Live` for real-time updates
   - Windows-native (no tmux dependency)
   - Shows: loop status, circuit breaker, rate limits, recent activity
   - Auto-refreshes every 2 seconds

3. **SetupCommand.cs** - Project initialization
   - Replaces `setup.sh`
   - Creates project directory structure
   - Generates template files (embedded in code, not external files)
   - Initializes git repository
   - Templates: `PROMPT.md`, `@fix_plan.md`, `@AGENT.md`, `README.md`, `.gitignore`

4. **ImportCommand.cs** - PRD/specification import
   - Replaces `ralph_import.sh`
   - Converts PRD documents to Ralph format using Claude
   - Supports: `.md`, `.txt`, `.json`, `.docx`, `.pdf`
   - Creates complete project structure
   - JSON output format with fallback

5. **StatusCommand.cs** - Current status display
   - Shows loop status, circuit breaker, rate limits
   - Reads from `status.json` and state files

6. **CircuitBreakerCommand.cs** - Circuit breaker management
   - Subcommands: `status`, `reset`
   - Also resets session on circuit breaker reset

7. **SessionCommand.cs** - Session management
   - Subcommands: `status`, `reset`, `history`
   - Shows session info, age, validity

**Program.cs** - Entry point
- Configures `System.CommandLine` root command
- Adds all subcommands
- Default behavior: run loop command with flags

## Key Commands

### Installation
```bash
# Build the solution
dotnet build Ralph.sln

# Install as global tool (optional)
dotnet tool install --global --add-source src/Ralph.Cli/bin/Release Ralph.Cli

# Or run directly
dotnet run --project src/Ralph.Cli/Ralph.Cli.csproj -- --help
```

### Setting Up a New Project
```bash
# Create a new Ralph-managed project
ralph setup my-project-name
cd my-project-name
```

### Running the Ralph Loop
```bash
# Start with integrated monitoring (Spectre.Console, not tmux)
ralph --monitor

# Start without monitoring
ralph

# Or use explicit command
ralph loop --monitor --calls 50 --prompt my_custom_prompt.md

# Check current status
ralph status

# Circuit breaker management
ralph circuit-breaker reset
ralph circuit-breaker status

# Session management
ralph session reset
ralph session status
ralph session history
```

### Monitoring
```bash
# Live monitoring dashboard (Windows-compatible!)
ralph monitor

# No tmux needed - uses Spectre.Console.Live
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test class
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests"
```

## Ralph Loop Configuration

The loop is controlled by files and configuration:

### Control Files
- **PROMPT.md** - Main prompt file that drives each loop iteration
- **@fix_plan.md** - Prioritized task list that Ralph follows (markdown checkboxes)
- **@AGENT.md** - Build and run instructions maintained by Ralph
- **specs/** - Project specifications directory

### State Files (generated, in `.gitignore`)
- **status.json** - Real-time status tracking
- **.call_count** - API call counter (JSON)
- **.last_reset** - Last rate limit reset (JSON)
- **.circuit_breaker_state** - Circuit breaker state and metrics (JSON)
- **.circuit_breaker_history** - State transition log (JSON lines)
- **.claude_session_id** - Current session info (JSON)
- **.ralph_session** - Ralph session lifecycle (JSON)
- **.ralph_session_history** - Session transition history (JSON lines, last 50)
- **.exit_signals** - Exit signal accumulation (JSON)
- **logs/ralph.log** - Execution logs (text)

### Rate Limiting
- Default: 100 API calls per hour (configurable via `--calls` flag)
- Automatic hourly reset with countdown display
- Persistent state in `.call_count` and `.last_reset`
- Implemented in `RateLimiter.cs`

### Intelligent Exit Detection

The loop automatically exits when `ExitDetector` detects:
- **Explicit exit signal**: `ExitSignal: true` in status block
- **Multiple test-only loops**: 3+ consecutive loops with only test commands
- **Multiple done signals**: 2+ consecutive "done" signals from Claude
- **@fix_plan.md complete**: All checkboxes marked `[x]` and no `[ ]` remain
- **High exit confidence**: Accumulated signals reach threshold

Exit detection thresholds (configurable in `ExitSignalInfo`):
- `MaxConsecutiveTestLoops`: 3
- `MaxConsecutiveDoneSignals`: 2
- `TestPercentageThreshold`: 30%

Circuit breaker thresholds (configurable in `CircuitBreakerInfo`):
- `NoProgressThreshold`: 3 loops
- `SameErrorThreshold`: 5 loops
- `OutputDeclineThreshold`: 70%

## Technology Stack

**Runtime**:
- .NET 8.0 SDK (cross-platform: Windows/Linux/macOS)
- C# 12 with nullable reference types enabled

**Dependencies**:
- **System.CommandLine** (2.0.0-beta4) - CLI framework
- **Spectre.Console** (0.49.1) - Rich console UI (replaces tmux)
- **Spectre.Console.Cli** (0.49.1) - CLI command structure
- **System.Text.Json** (8.0.5) - JSON serialization

**Testing**:
- **xUnit** (2.9.2) - Test framework
- **FluentAssertions** (6.12.2) - Expressive assertions
- **NSubstitute** (5.3.0) - Mocking library

## CI/CD Pipeline

Currently uses npm scripts in `package.json` (transitioned from bash/bats):

```bash
# Run tests
npm test                # Runs: dotnet test Ralph.sln

# Build
npm run build           # Runs: dotnet build Ralph.sln
npm run build:release   # Runs: dotnet build Ralph.sln -c Release

# Package
npm run pack            # Runs: dotnet pack src/Ralph.Cli/Ralph.Cli.csproj -c Release
```

Future: GitHub Actions workflow for .NET (`.github/workflows/dotnet.yml`)

## Project Structure for Ralph-Managed Projects

Each project created with `ralph setup` follows this structure:
```
project-name/
├── PROMPT.md          # Main development instructions
├── @fix_plan.md       # Prioritized TODO list (checkboxes)
├── @AGENT.md          # Build/run instructions
├── specs/             # Project specifications
├── src/               # Source code
├── examples/          # Usage examples
├── logs/              # Loop execution logs (auto-created)
└── docs/generated/    # Auto-generated documentation (auto-created)
```

## Cross-Platform Considerations

Ralph is designed to work identically on Windows, Linux, and macOS:

**Path Handling**:
- Always use `Path.Combine()` for file paths
- Never hardcode `/` or `\`
- Example: `Path.Combine(baseDir, ".claude_session_id")`

**Process Execution**:
- Use `Process.Start` with proper quoting
- Command: `"claude"` (assumes in PATH)
- Arguments: Built as string array, then joined with proper escaping

**Date/Time**:
- Always use `DateTime.UtcNow` for consistency
- Cross-platform formatting in `DateUtils.cs`
- No platform-specific date commands

**Line Endings**:
- Use `Environment.NewLine` for new lines
- Handle both `\n` and `\r\n` when reading files
- Split on `new[] { "\r\n", "\n" }` for cross-platform compatibility

**Console UI**:
- Spectre.Console works on Windows Terminal, PowerShell, CMD, bash, zsh
- No tmux dependency (Windows-compatible)
- Colored output supported everywhere

## Integration Points

Ralph integrates with:
- **Claude Code CLI**: Executes via `claude code` command (assumes in PATH)
- **Git**: Project initialization uses `git init`, `git add`, `git commit`
- **File System**: JSON-based state management (no database)
- **Process**: Spawns Claude Code CLI via `System.Diagnostics.Process`

## Exit Conditions and Thresholds

### Exit Detection

Implemented in `ExitDetector.cs`:

```csharp
public async Task<bool> ShouldExitAsync(int currentLoop, RalphStatus status)
{
    // Check explicit exit signal
    if (status.ExitSignal) return true;

    // Check accumulated signals
    if (_exitSignals.ShouldExit(currentLoop)) return true;

    // Check status is COMPLETE
    if (status.Status == RalphStatus.StatusValues.Complete) return true;

    return false;
}
```

### Circuit Breaker

Implemented in `CircuitBreaker.cs`:

**State Machine**:
```
CLOSED ──────────────→ HALF_OPEN ──────────→ OPEN
   ↑                       │                    │
   │                       ↓                    │
   └───── Progress ────────┘                    │
   ←──────────────────── Manual Reset ──────────┘
```

**Transitions**:
- `CLOSED` → `HALF_OPEN`: No progress for 3+ loops
- `HALF_OPEN` → `CLOSED`: Progress detected (files modified > 0)
- `HALF_OPEN` → `OPEN`: Continued stagnation (3+ additional loops)
- `CLOSED` → `OPEN`: Same error repeated 5+ times
- `OPEN` → `CLOSED`: Manual reset only

### Error Detection

Implemented in `ResponseAnalyzer.ExtractErrorsAsync()`:

**Two-Stage Filtering**:
1. **Stage 1**: Filter out JSON field names containing "error"
   - Regex: `"[^"]*error[^"]*":`
   - Eliminates false positives like `"is_error": false`

2. **Stage 2**: Detect actual error messages
   - Patterns: `^Error:`, `^ERROR:`, `]: error`, `Exception`, `Fatal`
   - Only counts real error messages

**Stuck Loop Detection**:
- Multi-line error matching
- Verifies ALL error lines appear in ALL recent history files
- Uses literal string matching (`Contains`) to avoid regex edge cases

## Test Suite

### Test Structure

```
tests/Ralph.Tests/
├── CircuitBreakerTests.cs      - Circuit breaker state machine tests
├── SessionManagerTests.cs      - Session lifecycle tests
├── ResponseAnalyzerTests.cs    - Response parsing and analysis tests
└── Ralph.Tests.csproj          - Test project configuration
```

### Running Tests

```bash
# All tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests.ResetAsync_WhenOpen_TransitionsToClosedAndResetsCounters"

# With detailed output
dotnet test -v detailed
```

### Test Coverage

Current coverage (30+ tests):
- **CircuitBreakerTests**: State transitions, thresholds, reset logic
- **SessionManagerTests**: Session creation, expiration, reset, history
- **ResponseAnalyzerTests**: JSON parsing, status extraction, error detection, stuck loop detection

### Writing Tests

Follow existing patterns:
```csharp
[Fact]
public async Task MethodName_WhenCondition_ExpectedBehavior()
{
    // Arrange
    var sut = new ServiceUnderTest(mockDependencies);

    // Act
    var result = await sut.MethodAsync(parameters);

    // Assert
    result.Should().Be(expectedValue);
    await mockDependency.Received().ExpectedCall();
}
```

Use FluentAssertions for readable assertions:
- `.Should().BeTrue()`
- `.Should().Be(expectedValue)`
- `.Should().HaveCount(3)`
- `.Should().Contain(x => x.Property == value)`

## Recent Improvements

### v1.0.0 - Complete .NET Migration
- Migrated entire codebase from bash to C# / .NET 8.0
- Windows-first approach with full cross-platform support
- Replaced tmux with Spectre.Console for Windows compatibility
- Maintained 100% feature parity with bash version
- All bash scripts removed (replaced with equivalent C# implementations)
- Comprehensive xUnit test suite
- KISS architecture with clean separation of concerns

## Feature Development Quality Standards

**CRITICAL**: All new features MUST meet the following mandatory requirements.

### Testing Requirements
- **Test Pass Rate**: 100% - all tests must pass
- **Test Coverage**: Write tests for new services/commands
- **Test Quality**: Use FluentAssertions for readable assertions
- **Test Isolation**: Mock dependencies with NSubstitute

### Git Workflow Requirements
1. **Commit with Clear Messages**: Use conventional commit format
   - `feat(module): description`
   - `fix(module): description`
   - `test(module): description`
2. **Push Regularly**: Don't leave work uncommitted
3. **Branch Hygiene**: Use feature branches, not `main` directly

### Documentation Requirements
1. **XML Comments**: Add XML documentation to public APIs
   ```csharp
   /// <summary>
   /// Resets the circuit breaker to closed state
   /// </summary>
   public async Task ResetAsync() { }
   ```
2. **Update README-DOTNET.md**: When adding commands or features
3. **Update CLAUDE.md**: When architecture changes
4. **Keep MIGRATION.md Current**: Document significant changes

### Code Style Requirements
1. **Async/Await**: Use for all I/O operations
2. **Nullable Reference Types**: Enable and annotate properly
3. **File-Scoped Namespaces**: Use `namespace Ralph.Core.Services;`
4. **Primary Constructors**: Use for simple dependency injection
5. **Pattern Matching**: Use modern C# patterns where appropriate
6. **KISS Principle**: Keep it simple - avoid over-engineering

### Feature Completion Checklist

Before marking ANY feature as complete:
- [ ] All tests pass
- [ ] Functionality manually tested on Windows
- [ ] Changes committed with conventional commit message
- [ ] Changes pushed to remote
- [ ] XML comments added to public APIs
- [ ] README-DOTNET.md updated (if needed)
- [ ] CLAUDE.md updated (if architecture changed)
- [ ] Manual testing performed

## Troubleshooting

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Test Failures
```bash
# Run tests with detailed output
dotnet test -v detailed

# Run specific failing test
dotnet test --filter "FullyQualifiedName~TestName"
```

### State File Corruption
```bash
# Remove all state files
rm .call_count .circuit_breaker_state .claude_session_id .exit_signals status.json

# Or just the problematic one
rm .circuit_breaker_state
```

### Circuit Breaker Stuck
```bash
ralph circuit-breaker reset
```

### Session Issues
```bash
ralph session reset
```

---

**This repository is now .NET-native with Windows as the primary platform. All bash scripts have been removed and replaced with equivalent C# implementations maintaining 100% feature parity.**
