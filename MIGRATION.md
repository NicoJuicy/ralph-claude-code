# Ralph Migration Guide: Bash to .NET

This document explains the migration strategy from the original bash-based Ralph to the new .NET CLI version, with **Windows as the primary platform**.

## 🎯 Migration Goals

1. **Windows-First**: Make Ralph natively accessible on Windows without WSL/bash
2. **Keep All Functionality**: Preserve 100% of existing features
3. **KISS Principle**: Simplify where possible, don't over-engineer
4. **Cross-Platform**: Maintain Linux/macOS compatibility
5. **Better Maintainability**: Type safety, testing, modern tooling

## ✅ Migration Status: COMPLETE

All core functionality has been successfully migrated to .NET 8.0 with C#.

## 📊 Architecture Comparison

### Original Bash Architecture

```
ralph-claude-code/
├── ralph_loop.sh           (1,328 lines)
├── ralph_monitor.sh        (125 lines)
├── ralph_import.sh         (625 lines)
├── setup.sh                (34 lines)
├── install.sh              (293 lines)
├── lib/
│   ├── circuit_breaker.sh  (328 lines)
│   ├── response_analyzer.sh (683 lines)
│   └── date_utils.sh       (53 lines)
└── tests/ (276 bats tests)
```

**Total**: ~3,500 lines of bash + 1,064 lines of lib code

### New .NET Architecture

```
ralph-claude-code/
├── Ralph.sln
├── src/
│   ├── Ralph.Core/         # Core library (cross-platform)
│   │   ├── Models/         # Data structures
│   │   ├── Services/       # Business logic
│   │   ├── Interfaces/     # Abstractions
│   │   └── Utils/          # Helpers
│   └── Ralph.Cli/          # CLI application
│       ├── Commands/       # Command implementations
│       ├── Program.cs      # Entry point
│       └── Templates/      # Project templates
└── tests/
    └── Ralph.Tests/        # xUnit tests
```

**Total**: ~3,000 lines of C# (cleaner, more maintainable)

## 🔄 Component Mapping

### 1. Core Loop (ralph_loop.sh → LoopCommand.cs)

**Bash**: 1,328 lines in `ralph_loop.sh`
**C#**: ~400 lines in `LoopCommand.cs`

| Feature | Bash | C# |
|---------|------|-----|
| API rate limiting | Custom file-based | `RateLimiter` service |
| Circuit breaker | `lib/circuit_breaker.sh` | `CircuitBreaker` service |
| Session management | Manual JSON files | `SessionManager` service |
| Response parsing | grep/sed/awk | `ResponseAnalyzer` service |
| Exit detection | Multiple functions | `ExitDetector` service |
| Status display | echo statements | Spectre.Console tables |

**Key Improvements**:
- Dependency injection for testability
- Async/await for better I/O handling
- Type-safe models prevent runtime errors
- Service-based architecture (SOLID principles)

### 2. Monitoring (ralph_monitor.sh → MonitorCommand.cs)

**Bash**: 125 lines + tmux dependency (Linux/macOS only)
**C#**: ~250 lines with Spectre.Console (all platforms)

| Feature | Bash | C# |
|---------|------|-----|
| Live updates | tmux split-panes | Spectre.Console.Live |
| Platform | Linux/macOS only | Windows/Linux/macOS |
| Refresh | Manual script loop | Async Task.Delay |
| UI Layout | ASCII text | Styled panels/tables |
| Colors | ANSI escape codes | Spectre.Console colors |

**Windows Compatibility**:
- ✅ No tmux dependency
- ✅ Native Windows Terminal support
- ✅ Colored output works everywhere
- ✅ Responsive layout with panels

### 3. Circuit Breaker (lib/circuit_breaker.sh → CircuitBreaker.cs)

**Bash**: 328 lines
**C#**: ~180 lines

| Concept | Bash | C# |
|---------|------|-----|
| State storage | JSON files + jq | JSON files + System.Text.Json |
| State machine | String comparisons | Enum with switch expressions |
| Thresholds | Global variables | Configuration properties |
| History tracking | Log file append | Structured logging |

**Type Safety Benefits**:
```csharp
// Compile-time checking prevents typos
circuitBreakerState.State = CircuitBreakerState.Open; // ✅
circuitBreakerState.State = "OPEN"; // ❌ Won't compile

// Bash equivalent:
STATE="OPEN"  # Could be "Open", "open", "OPNE", etc.
```

### 4. Session Management (response_analyzer.sh → SessionManager.cs)

**Bash**: Part of 683-line `response_analyzer.sh`
**C#**: Dedicated ~150-line `SessionManager.cs`

| Feature | Bash | C# |
|---------|------|-----|
| Session expiry | Epoch time comparison | DateTime operations |
| Session storage | JSON file manipulation | Structured models |
| History tracking | JSON array append | List<SessionHistoryEntry> |
| Cross-platform dates | Platform-specific commands | DateTime.UtcNow |

**Windows Path Handling**:
```csharp
// C# handles Windows/Linux paths automatically
var sessionFile = Path.Combine(baseDir, ".claude_session_id"); // ✅

# Bash needs manual handling
session_file="$BASE_DIR/.claude_session_id"  # Breaks on Windows
```

### 5. Response Analysis (response_analyzer.sh → ResponseAnalyzer.cs)

**Bash**: 683 lines with grep/sed/awk
**C#**: ~300 lines with regex and LINQ

| Task | Bash | C# |
|------|------|-----|
| JSON parsing | jq command | System.Text.Json |
| Status extraction | Regex + grep | Regex.Match + parsing |
| Error filtering | Two-stage grep pipeline | LINQ Where + Regex |
| Test-only detection | Pattern matching | String.Contains + LINQ |
| Completion signals | Keyword matching | String.Contains |

**Two-Stage Error Filtering Example**:

Bash:
```bash
# Stage 1: Filter JSON fields
filtered=$(echo "$output" | grep -v '"[^"]*error[^"]*":')

# Stage 2: Find actual errors
errors=$(echo "$filtered" | grep -E '(^Error:|^ERROR:|Exception)')
```

C#:
```csharp
// Stage 1: Filter JSON fields
var filtered = lines.Where(line => !JsonFieldErrorRegex().IsMatch(line));

// Stage 2: Find actual errors
var errors = filtered.Where(line => ActualErrorRegex().IsMatch(line));
```

### 6. Rate Limiting (Bash functions → RateLimiter.cs)

**Bash**: Mixed into `ralph_loop.sh`
**C#**: Dedicated ~150-line `RateLimiter.cs`

| Feature | Bash | C# |
|---------|------|-----|
| Call counting | File-based counter | Persistent model |
| Hour detection | date +%Y%m%d%H | DateTime.ToString("yyyyMMddHH") |
| Countdown | Loop with sleep | Task.Delay with callback |
| Reset logic | String comparison | DateTime arithmetic |

**Cross-Platform Date Handling**:

Bash (platform-specific):
```bash
# macOS
next_hour=$(date -v+1H "+%Y-%m-%d %H:00:00")

# Linux
next_hour=$(date -d '+1 hour' "+%Y-%m-%d %H:00:00")
```

C# (works everywhere):
```csharp
var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0)
    .AddHours(1);
```

### 7. Project Setup (setup.sh → SetupCommand.cs)

**Bash**: 34 lines
**C#**: ~350 lines (with embedded templates)

| Feature | Bash | C# |
|---------|------|-----|
| Directory creation | mkdir -p | Directory.CreateDirectory |
| Template copying | cp from ~/.ralph/ | Embedded resources / inline strings |
| Git initialization | git init && git add | System.Diagnostics.Process |
| Error handling | if [ $? -eq 0 ] | try/catch blocks |

**Embedded Templates**:
```csharp
// C# embeds templates directly in code (simpler deployment)
private static string GetPromptTemplate(string projectName)
{
    return $@"# {projectName} Development Instructions
    ...
    ";
}
```

### 8. PRD Import (ralph_import.sh → ImportCommand.cs)

**Bash**: 625 lines
**C#**: ~350 lines

| Feature | Bash | C# |
|---------|------|-----|
| File reading | cat $file | File.ReadAllTextAsync |
| Claude invocation | claude code ... | Process.Start with async |
| JSON parsing | jq | System.Text.Json |
| Fallback handling | if/else checks | try/catch with fallback |

## 🧪 Testing Strategy

### Bash Tests (bats)

```bash
# tests/unit/test_cli_parsing.bats
@test "parses --calls flag" {
  run parse_args --calls 50
  assert_equal "$MAX_CALLS" "50"
}
```

### C# Tests (xUnit)

```csharp
[Fact]
public async Task RecordLoopResultAsync_WithNoProgress_IncrementsCount()
{
    // Arrange
    await _sut.InitializeAsync();

    // Act
    await _sut.RecordLoopResultAsync(filesModified: 0, error: null);

    // Assert
    var state = await _sut.GetStateAsync();
    state.NoProgressCount.Should().Be(1);
}
```

**Benefits of xUnit**:
- ✅ Async/await support (native async testing)
- ✅ Mocking with NSubstitute (test isolation)
- ✅ FluentAssertions (readable assertions)
- ✅ Fast execution (parallel test running)
- ✅ IDE integration (Visual Studio, VS Code, Rider)

## 🔧 Technology Choices

### Why These Libraries?

| Library | Purpose | Alternative Considered | Decision |
|---------|---------|----------------------|----------|
| **System.CommandLine** | CLI parsing | Spectre.Console.Cli, CommandLineParser | Official Microsoft library, modern API |
| **Spectre.Console** | Rich UI | Colorful.Console, ConsoleTables | Best Windows support, active development |
| **System.Text.Json** | JSON handling | Newtonsoft.Json | Built-in, faster, better async |
| **xUnit** | Testing | NUnit, MSTest | Industry standard, best async support |
| **FluentAssertions** | Assertions | Should, Assert | Most readable, excellent error messages |
| **NSubstitute** | Mocking | Moq, FakeItEasy | Clean syntax, easier to use |

### Why NOT These Approaches?

❌ **Entity Framework**: Too heavy for simple file-based state
❌ **ASP.NET Core**: Ralph is a CLI tool, not a web service
❌ **Complex DI Container**: Simple `new` statements keep it KISS
❌ **MediatR**: Overhead not needed for this use case
❌ **Reactive Extensions**: Async/await is sufficient

## 🎨 Code Style Comparison

### Bash Style

```bash
detect_stuck_loop() {
    local current_error="$1"
    local -a recent_errors=("${@:2}")

    if [ ${#recent_errors[@]} -lt 3 ]; then
        echo "false"
        return
    fi

    # Check if all errors contain current error
    for error in "${recent_errors[@]}"; do
        if ! echo "$error" | grep -qF "$current_error"; then
            echo "false"
            return
        fi
    done

    echo "true"
}
```

### C# Style

```csharp
public async Task<bool> DetectStuckLoopAsync(
    string currentError,
    List<string> recentErrors,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(currentError) || recentErrors.Count < 3)
        return false;

    var currentErrorLines = currentError.Split('\n')
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

    if (currentErrorLines.Count == 0)
        return false;

    return currentErrorLines.All(errorLine =>
        recentErrors.All(recentError =>
            recentError.Contains(errorLine, StringComparison.Ordinal)));
}
```

**Improvements**:
- ✅ Type safety (string vs List<string>)
- ✅ Async-first (CancellationToken support)
- ✅ LINQ expressiveness (All, Where)
- ✅ Explicit string comparison (StringComparison.Ordinal)
- ✅ No shell injection risk

## 📈 Performance Comparison

| Operation | Bash | C# | Winner |
|-----------|------|-----|--------|
| JSON parsing | jq (external process) | System.Text.Json (in-process) | C# 🏆 |
| File I/O | cat/echo/grep | File.ReadAllTextAsync | C# 🏆 |
| String manipulation | sed/awk | LINQ + Regex | C# 🏆 |
| Process spawning | Subshells $() | Process.Start | Similar |
| Startup time | Instant | ~100ms (JIT) | Bash 🏆 |

**Overall**: C# is faster for long-running operations (loops), bash is faster for one-off scripts.

## 🐛 Common Migration Pitfalls

### 1. Path Separators

❌ **Wrong**:
```csharp
var path = baseDir + "/.claude_session_id"; // Breaks on Windows
```

✅ **Right**:
```csharp
var path = Path.Combine(baseDir, ".claude_session_id"); // Works everywhere
```

### 2. Line Endings

❌ **Wrong**:
```csharp
var lines = content.Split('\n'); // Misses \r\n on Windows
```

✅ **Right**:
```csharp
var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
// Or use Environment.NewLine
```

### 3. Process Execution

❌ **Wrong**:
```csharp
Process.Start("bash", "-c 'git init'"); // Breaks on Windows
```

✅ **Right**:
```csharp
Process.Start("git", "init"); // Works everywhere
```

### 4. Async Deadlocks

❌ **Wrong**:
```csharp
var result = DoAsyncWork().Result; // Can deadlock
```

✅ **Right**:
```csharp
var result = await DoAsyncWork(); // Proper async/await
```

## 📝 Migration Checklist

When migrating bash scripts to .NET:

- [ ] Identify external command dependencies (jq, grep, etc.)
- [ ] Map shell functions to C# classes/methods
- [ ] Replace file-based state with typed models
- [ ] Use Path.Combine for all path operations
- [ ] Handle line endings properly (Environment.NewLine)
- [ ] Add async/await for I/O operations
- [ ] Write unit tests for core logic
- [ ] Test on Windows, Linux, macOS
- [ ] Replace tmux with cross-platform UI (Spectre.Console)
- [ ] Document API with XML comments
- [ ] Add error handling with try/catch
- [ ] Use CancellationToken for long operations
- [ ] Validate user input early
- [ ] Log errors clearly with context

## 🎓 Lessons Learned

### What Worked Well

1. **Service-Based Architecture**: Clean separation made testing easy
2. **Spectre.Console**: Beautiful UI that works on Windows
3. **System.CommandLine**: Modern, intuitive CLI parsing
4. **Async/Await**: Natural for I/O-heavy operations
5. **Type Safety**: Caught errors at compile time
6. **FluentAssertions**: Made tests very readable

### Challenges Overcome

1. **Tmux Replacement**: Spectre.Console.Live solved this elegantly
2. **Cross-Platform Paths**: Path.Combine everywhere
3. **Process Execution**: Proper quoting and error handling
4. **State Persistence**: JSON with System.Text.Json worked well
5. **Testing File I/O**: Mock IStateStore for testability

### What We'd Do Differently

1. **Embedded Templates**: Could use embedded resources instead of inline strings
2. **Configuration**: Consider appsettings.json for defaults
3. **Logging**: Add Serilog for structured logging
4. **Dependency Injection**: Consider Microsoft.Extensions.DependencyInjection for larger codebases

## 🚀 Next Steps

The .NET migration is complete! Future enhancements could include:

- [ ] Package to NuGet.org for easier installation
- [ ] Add GitHub Actions workflow for .NET
- [ ] Create Windows installer (MSI/EXE)
- [ ] Add application telemetry (optional)
- [ ] Support more Claude CLI flags
- [ ] Web dashboard (optional ASP.NET Core)
- [ ] VS Code extension integration

## 🤝 Contributing to .NET Version

To contribute:

1. Install .NET 8.0 SDK
2. Clone repository
3. Open `Ralph.sln` in Visual Studio/VS Code/Rider
4. Make changes
5. Run tests: `dotnet test`
6. Submit PR

Follow existing code style:
- Use async/await for I/O
- Add XML documentation comments
- Write unit tests for new features
- Keep KISS principle in mind

---

**Migration Complete**: Ralph is now fully Windows-native while maintaining all original functionality! 🎉
