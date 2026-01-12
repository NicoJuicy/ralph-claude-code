# Ralph for .NET - Windows-First Autonomous AI Development Loop

[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)](https://github.com)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

**Ralph** is an autonomous AI development loop system that enables continuous development cycles with intelligent exit detection and rate limiting. This is the **.NET CLI version**, designed with **Windows as the primary platform** while maintaining full cross-platform compatibility.

## 🎯 Why .NET?

The .NET version of Ralph brings several advantages over the bash-based version:

- ✅ **Windows-First**: Native Windows support without WSL or bash dependencies
- ✅ **Cross-Platform**: Runs on Windows, Linux, and macOS with identical functionality
- ✅ **Modern UI**: Spectre.Console-based live monitoring (replaces tmux on Windows)
- ✅ **Type Safety**: C# static typing catches errors at compile time
- ✅ **Better Tooling**: Visual Studio, VS Code, Rider support with IntelliSense
- ✅ **NuGet Ecosystem**: Easy dependency management
- ✅ **Better Testing**: xUnit, FluentAssertions, NSubstitute for comprehensive testing
- ✅ **KISS Principle**: Simplified architecture with clear separation of concerns

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Claude Code CLI](https://www.anthropic.com/claude-code) installed and configured
- Git (optional, for project initialization)

### Installation

#### Option 1: Install as Global .NET Tool (Recommended)

```bash
# Build the tool
dotnet pack src/Ralph.Cli/Ralph.Cli.csproj -c Release

# Install globally
dotnet tool install --global --add-source src/Ralph.Cli/bin/Release Ralph.Cli

# Verify installation
ralph --help
```

#### Option 2: Build and Run Locally

```bash
# Clone the repository
git clone https://github.com/yourusername/ralph-claude-code.git
cd ralph-claude-code

# Build the solution
dotnet build Ralph.sln

# Run directly
dotnet run --project src/Ralph.Cli/Ralph.Cli.csproj -- --help
```

### Create Your First Ralph Project

```bash
# Create a new project
ralph setup my-awesome-project
cd my-awesome-project

# Edit PROMPT.md with your development instructions
# Edit @fix_plan.md with your tasks

# Start the autonomous loop
ralph

# Or start with live monitoring
ralph --monitor
```

## 📖 Core Commands

### Main Loop

```bash
# Start autonomous development loop
ralph

# With monitoring dashboard (Windows-compatible!)
ralph --monitor

# With custom rate limiting
ralph --calls 50

# With custom prompt file
ralph --prompt my-custom-prompt.md

# Disable session continuity
ralph --no-continue
```

### Project Management

```bash
# Create new Ralph project
ralph setup my-project

# Import from PRD/specification
ralph import requirements.md my-project
```

### Monitoring & Status

```bash
# Check current status
ralph status

# Live monitoring dashboard (replaces tmux)
ralph monitor

# Circuit breaker status
ralph circuit-breaker status

# Circuit breaker reset
ralph circuit-breaker reset

# Session management
ralph session status
ralph session reset
ralph session history
```

## 🏗️ Architecture

The .NET version maintains all functionality from the bash version with improved structure:

```
Ralph/
├── src/
│   ├── Ralph.Core/              # Core library
│   │   ├── Models/              # Data models
│   │   │   ├── CircuitBreakerState.cs
│   │   │   ├── RalphStatus.cs
│   │   │   ├── ClaudeResponse.cs
│   │   │   ├── SessionInfo.cs
│   │   │   ├── RateLimitInfo.cs
│   │   │   └── ExitSignalInfo.cs
│   │   ├── Services/            # Core services
│   │   │   ├── StateStore.cs
│   │   │   ├── CircuitBreaker.cs
│   │   │   ├── SessionManager.cs
│   │   │   ├── RateLimiter.cs
│   │   │   ├── ResponseAnalyzer.cs
│   │   │   └── ExitDetector.cs
│   │   ├── Interfaces/          # Service contracts
│   │   └── Utils/               # Utilities
│   │       └── DateUtils.cs
│   └── Ralph.Cli/               # CLI application
│       ├── Commands/            # Command implementations
│       │   ├── LoopCommand.cs
│       │   ├── MonitorCommand.cs
│       │   ├── SetupCommand.cs
│       │   ├── ImportCommand.cs
│       │   ├── StatusCommand.cs
│       │   ├── CircuitBreakerCommand.cs
│       │   └── SessionCommand.cs
│       ├── Program.cs           # Entry point
│       └── Templates/           # Project templates
└── tests/
    └── Ralph.Tests/             # xUnit tests
        ├── CircuitBreakerTests.cs
        ├── SessionManagerTests.cs
        └── ResponseAnalyzerTests.cs
```

## 🎨 Windows-Native Monitoring

The .NET version replaces tmux with **Spectre.Console** for a beautiful, cross-platform live monitoring UI:

```bash
ralph monitor
```

Features:
- ✅ Real-time loop status updates
- ✅ Circuit breaker state visualization
- ✅ API rate limit tracking
- ✅ Recent activity log
- ✅ Color-coded status indicators
- ✅ Auto-refresh every 2 seconds
- ✅ **Works perfectly on Windows!**

## 🔧 Configuration

Ralph uses JSON files for state persistence (stored in project root):

| File | Purpose |
|------|---------|
| `.call_count` | API calls this hour |
| `.circuit_breaker_state` | Circuit breaker state |
| `.claude_session_id` | Current Claude session |
| `.exit_signals` | Exit detection tracking |
| `status.json` | Current loop status |
| `logs/ralph.log` | Execution logs |

## 🧪 Testing

The .NET version includes comprehensive unit tests using xUnit:

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test file
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests"
```

### Test Coverage

- ✅ **CircuitBreaker**: State transitions, thresholds, reset logic
- ✅ **SessionManager**: Session lifecycle, expiration, history
- ✅ **ResponseAnalyzer**: Status parsing, error detection, exit signals
- ✅ **RateLimiter**: Call counting, hourly reset, countdown
- ✅ **ExitDetector**: Multi-method exit detection, fix_plan parsing

## 📦 Dependencies

### Production

- **System.CommandLine** (2.0.0-beta4): Modern CLI framework
- **Spectre.Console** (0.49.1): Rich console UI
- **Spectre.Console.Cli** (0.49.1): CLI command framework
- **System.Text.Json** (8.0.5): JSON serialization

### Development

- **xUnit** (2.9.2): Test framework
- **FluentAssertions** (6.12.2): Expressive assertions
- **NSubstitute** (5.3.0): Mocking framework

## 🔄 Migration from Bash Version

All bash scripts have been migrated to C#:

| Bash Script | .NET Equivalent | Status |
|-------------|-----------------|--------|
| `ralph_loop.sh` | `LoopCommand.cs` | ✅ Complete |
| `ralph_monitor.sh` | `MonitorCommand.cs` | ✅ Complete (Spectre.Console) |
| `setup.sh` | `SetupCommand.cs` | ✅ Complete |
| `ralph_import.sh` | `ImportCommand.cs` | ✅ Complete |
| `lib/circuit_breaker.sh` | `CircuitBreaker.cs` | ✅ Complete |
| `lib/response_analyzer.sh` | `ResponseAnalyzer.cs` | ✅ Complete |
| `lib/date_utils.sh` | `DateUtils.cs` | ✅ Complete |

### Key Improvements

1. **Windows Compatibility**: No bash/tmux dependency
2. **Type Safety**: Compile-time error checking
3. **Async/Await**: Better async handling than bash
4. **Dependency Injection**: Testable, modular architecture
5. **JSON Everywhere**: Consistent state management
6. **Cross-Platform Paths**: `Path.Combine()` handles Windows/Linux paths

## 🎯 KISS Principle

The .NET version follows **Keep It Simple, Stupid** principles:

- ✅ Clear separation of concerns (Models, Services, Commands)
- ✅ Interfaces for testability, not over-abstraction
- ✅ JSON files for state (no database overhead)
- ✅ Direct CLI invocation (no complex middleware)
- ✅ Simple error handling with clear error messages
- ✅ Straightforward async/await patterns
- ✅ Minimal dependencies (only what's needed)

## 🐛 Troubleshooting

### "dotnet: command not found"

Install the .NET 8.0 SDK from https://dotnet.microsoft.com/download

### "claude: command not found"

Install Claude Code CLI:
```bash
npm install -g @anthropic/claude-code
```

### Circuit Breaker Stuck

```bash
ralph circuit-breaker reset
```

### Session Issues

```bash
ralph session reset
```

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet build
```

## 📝 Example Workflows

### Create a New Web API Project

```bash
# Create project
ralph setup my-web-api

cd my-web-api

# Edit PROMPT.md
cat > PROMPT.md << 'EOF'
# Web API Development

Build a RESTful API with .NET Core with the following features:
- User authentication (JWT)
- CRUD operations for resources
- Entity Framework Core with PostgreSQL
- Swagger documentation
- Unit tests with xUnit

Report status after each task completion.
EOF

# Edit @fix_plan.md
cat > @fix_plan.md << 'EOF'
## High Priority
- [ ] Set up .NET Core Web API project
- [ ] Add Entity Framework Core with PostgreSQL
- [ ] Implement user model and authentication
- [ ] Add JWT token generation
- [ ] Create resource endpoints (CRUD)

## Medium Priority
- [ ] Add Swagger documentation
- [ ] Write unit tests
- [ ] Add input validation
- [ ] Implement error handling middleware

## Low Priority
- [ ] Add logging
- [ ] Performance optimization
- [ ] Docker containerization
EOF

# Start Ralph
ralph --monitor
```

### Import from Existing PRD

```bash
# You have a PRD document
ralph import product-requirements.md my-product

cd my-product

# Review generated files
cat PROMPT.md
cat @fix_plan.md

# Start development
ralph
```

## 🤝 Contributing

Contributions are welcome! The .NET version maintains the same quality standards:

1. All tests must pass (`dotnet test`)
2. Follow existing code style
3. Update documentation
4. Add tests for new features

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Acknowledgments

- Original Ralph bash version
- Anthropic's Claude Code CLI
- Spectre.Console for amazing Windows-compatible UI
- .NET Team for excellent cross-platform tooling

---

**Ralph .NET** - Making autonomous AI development accessible on Windows! 🚀
