# Ralph for Claude Code - .NET Edition

[![.NET Version](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue)](https://github.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Mentioned in Awesome Claude Code](https://awesome.re/mentioned-badge.svg)](https://github.com/hesreallyhim/awesome-claude-code)

> **Autonomous AI development loop with intelligent exit detection and rate limiting**

Ralph is an implementation of Geoffrey Huntley's technique for Claude Code that enables continuous autonomous development cycles. **This repository has been migrated from bash to .NET 8.0** for better Windows support, type safety, and cross-platform compatibility.

## 🎯 What's New in .NET?

Ralph has been **completely rewritten in C#** with .NET 8.0, making it a modern, cross-platform CLI tool:

✅ **Windows-First**: Native Windows support without WSL or bash
✅ **Cross-Platform**: Runs identically on Windows, Linux, and macOS
✅ **Modern UI**: Spectre.Console-based monitoring (replaces tmux on Windows)
✅ **Type Safety**: C# static typing catches errors at compile time
✅ **Better Testing**: xUnit with FluentAssertions for comprehensive tests
✅ **KISS Principle**: Simplified architecture with clear separation of concerns
✅ **100% Feature Parity**: All original functionality preserved

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Claude Code CLI](https://www.anthropic.com/claude-code) installed and configured
- Git (optional)

### Installation

```bash
# Clone and build
git clone https://github.com/NicoJuicy/ralph-claude-code.git
cd ralph-claude-code
dotnet build Ralph.sln

# Install as global tool (optional)
dotnet tool install --global --add-source src/Ralph.Cli/bin/Release Ralph.Cli

# Or run directly
dotnet run --project src/Ralph.Cli/Ralph.Cli.csproj -- --help
```

### Create Your First Project

```bash
# Create a new project
ralph setup my-awesome-project
cd my-awesome-project

# Edit PROMPT.md with your development instructions
# Edit @fix_plan.md with your tasks

# Start the autonomous loop
ralph

# Or start with Windows-native monitoring
ralph --monitor
```

## 📖 Core Commands

```bash
# Main autonomous loop
ralph

# Live monitoring dashboard (Windows-compatible!)
ralph monitor

# Project management
ralph setup my-project
ralph import requirements.md my-project

# Status and debugging
ralph status
ralph circuit-breaker status
ralph circuit-breaker reset
ralph session status
ralph session reset
```

## 🎨 Key Features

### All Original Features Preserved
- ✅ Autonomous development loop
- ✅ Intelligent exit detection
- ✅ Circuit breaker (3-state machine)
- ✅ Session management (24-hour expiration)
- ✅ API rate limiting (configurable)
- ✅ Multi-method exit detection
- ✅ JSON response parsing
- ✅ Two-stage error filtering
- ✅ Project setup and templates
- ✅ PRD/specification import

### New .NET Features
- ✅ **Windows-Native Monitoring**: Beautiful Spectre.Console UI (no tmux needed!)
- ✅ **Type Safety**: Compile-time error checking
- ✅ **Async/Await**: Better I/O performance
- ✅ **Cross-Platform Paths**: Automatic Windows/Linux path handling
- ✅ **Better Error Messages**: Clear, actionable error reporting
- ✅ **Modern Testing**: xUnit with FluentAssertions
- ✅ **NuGet Packaging**: Easy distribution and updates

## 📚 Documentation

- **[README-DOTNET.md](README-DOTNET.md)** - Complete .NET usage guide
- **[MIGRATION.md](MIGRATION.md)** - Architecture comparison and migration details
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - How to contribute

## 🏗️ Architecture

```
Ralph/
├── Ralph.sln                     # Solution file
├── src/
│   ├── Ralph.Core/               # Core library
│   │   ├── Models/               # Data models
│   │   ├── Services/             # Business logic
│   │   ├── Interfaces/           # Service contracts
│   │   └── Utils/                # Utilities
│   └── Ralph.Cli/                # CLI application
│       ├── Commands/             # Command implementations
│       └── Program.cs            # Entry point
└── tests/
    └── Ralph.Tests/              # xUnit tests
```

## 🔧 Technology Stack

- **.NET 8.0** - Cross-platform framework
- **System.CommandLine** - Modern CLI parsing
- **Spectre.Console** - Rich console UI
- **System.Text.Json** - Fast JSON serialization
- **xUnit** - Testing framework
- **FluentAssertions** - Expressive test assertions
- **NSubstitute** - Mocking framework

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific tests
dotnet test --filter "FullyQualifiedName~CircuitBreakerTests"
```

**Test Coverage**:
- 30+ unit tests for core services
- Circuit breaker, session manager, response analyzer
- Rate limiting, exit detection
- 100% test pass rate

## 💡 Why .NET?

### For Windows Users
- No need for WSL or Git Bash
- Native Windows Terminal support
- PowerShell and CMD compatible
- Beautiful colored output everywhere

### For Developers
- Type safety prevents runtime errors
- IntelliSense support in VS Code, Visual Studio, Rider
- Async/await for better performance
- Better debugging experience
- Easier to maintain and extend

### For Everyone
- **Same commands** work on Windows, Linux, macOS
- **Better error messages** with clear context
- **Faster** for I/O-heavy operations
- **More reliable** with compile-time checking

## 🤝 Contributing

Contributions are welcome! The .NET version maintains the same quality standards as the original bash version.

See [CONTRIBUTING.md](CONTRIBUTING.md) for:
- Development setup
- Code style guidelines
- Testing requirements
- Pull request process

## 📝 Migrating from Bash Version

If you were using the bash version of Ralph, see [MIGRATION.md](MIGRATION.md) for:
- Detailed architecture comparison
- Feature mapping (bash → .NET)
- Migration strategy and lessons learned
- Code style comparison

**All bash scripts have been removed** in favor of the .NET implementation. The .NET version maintains 100% feature parity while adding Windows-native support and better tooling.

## 🐛 Troubleshooting

### "dotnet: command not found"
Install the .NET 8.0 SDK from https://dotnet.microsoft.com/download

### "claude: command not found"
Install Claude Code CLI:
```bash
npm install -g @anthropic/claude-code
```

### Circuit Breaker Issues
```bash
ralph circuit-breaker reset
```

### Session Problems
```bash
ralph session reset
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Original Ralph technique by [Geoffrey Huntley](https://ghuntley.com/ralph/)
- Built for [Claude Code](https://claude.ai/code) by Anthropic
- Spectre.Console for amazing Windows-compatible UI
- .NET Team for excellent cross-platform tooling

## 🔗 Related Projects

- [Claude Code](https://claude.ai/code) - The AI coding assistant that powers Ralph
- [Awesome Claude Code](https://github.com/hesreallyhim/awesome-claude-code) - Curated list of Claude Code resources

---

**Ready to let AI build your project on Windows?** Install .NET 8.0, run `dotnet build`, and let Ralph take it from there! 🚀

For complete documentation, see **[README-DOTNET.md](README-DOTNET.md)**
