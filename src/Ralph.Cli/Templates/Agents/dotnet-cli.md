# AGENT.md — .NET CLI Agent Project

## Description
.NET Console CLI application using MediatR for command handling. Lightweight agent suitable for orchestrating scripts, scheduled tasks, and command-line tools.

# Agent Build Instructions

## Project Setup
```bash
# Create project
dotnet new console -n MyAgentProject
cd MyAgentProject

# Install dependencies
dotnet add package MediatR
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Newtonsoft.Json

# Initialize git
git init
git branch -M main
git add .
git commit -m "chore(init): create basic console project"
```

## Running Tests
```bash
# Setup tests
dotnet new xunit -n MyAgentProject.Tests
cd MyAgentProject.Tests
dotnet add reference ../MyAgentProject/MyAgentProject.csproj
dotnet test --logger "trx"
```

## Build Commands
```bash
dotnet build
dotnet run
```

## Development Server
```bash
dotnet watch run
```

## Key Learnings
- Lightweight, easy to scaffold with CLI.
- Agent can orchestrate scripts or scheduled tasks.
- `dotnet watch` enables fast iteration.
- Ensure MediatR handlers are correctly registered.

## Feature Development Quality Standards

**CRITICAL**: All new features MUST meet the following mandatory requirements before being considered complete.

### Testing Requirements
- **Minimum Coverage**: 85%
- **Test Pass Rate**: 100%
- **Test Types Required**:
  - Unit tests for all business logic
  - Integration tests for console command workflows
- **Coverage Validation**:
```bash
dotnet test /p:CollectCoverage=true
```
- **Test Quality**: Validate behavior, not only coverage
- **Test Documentation**: Comment complex test scenarios

### Git Workflow Requirements

Before moving to the next feature, ALL changes must be:

1. **Committed with Clear Messages**:
   ```bash
   git add .
   git commit -m "feat(module): descriptive message following conventional commits"
   ```
   - Use conventional commit format: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, etc.
   - Include scope when applicable: `feat(api):`, `fix(ui):`, `test(auth):`
   - Write descriptive messages that explain WHAT changed and WHY

2. **Pushed to Remote Repository**:
   ```bash
   git push origin <branch-name>
   ```
   - Never leave completed features uncommitted
   - Push regularly to maintain backup and enable collaboration
   - Ensure CI/CD pipelines pass before considering feature complete

3. **Branch Hygiene**:
   - Work on feature branches, never directly on `main`
   - Branch naming convention: `feature/<feature-name>`, `fix/<issue-name>`, `docs/<doc-update>`
   - Create pull requests for all significant changes

4. **Ralph Integration**:
   - Update @fix_plan.md with new tasks before starting work
   - Mark items complete in @fix_plan.md upon completion
   - Update PROMPT.md if development patterns change
   - Test features work within Ralph's autonomous loop

### Documentation Requirements

**ALL implementation documentation MUST remain synchronized with the codebase**:

1. **Code Documentation**:
   - Language-appropriate documentation (JSDoc, docstrings, etc.)
   - Update inline comments when implementation changes
   - Remove outdated comments immediately

2. **Implementation Documentation**:
   - Update relevant sections in this AGENT.md file
   - Keep build and test commands current
   - Update configuration examples when defaults change
   - Document breaking changes prominently

3. **README Updates**:
   - Keep feature lists current
   - Update setup instructions when dependencies change
   - Maintain accurate command examples
   - Update version compatibility information

4. **AGENT.md Maintenance**:
   - Add new build patterns to relevant sections
   - Update "Key Learnings" with new insights
   - Keep command examples accurate and tested
   - Document new testing patterns or quality gates

### Feature Completion Checklist

Before marking ANY feature as complete, verify:

- [ ] All tests pass with appropriate framework command
- [ ] Code coverage meets 85% minimum threshold
- [ ] Coverage report reviewed for meaningful test quality
- [ ] Code formatted according to project standards
- [ ] Type checking passes (if applicable)
- [ ] All changes committed with conventional commit messages
- [ ] All commits pushed to remote repository
- [ ] @fix_plan.md task marked as complete
- [ ] Implementation documentation updated
- [ ] Inline code comments updated or added
- [ ] AGENT.md updated (if new patterns introduced)
- [ ] Breaking changes documented
- [ ] Features tested within Ralph loop (if applicable)
- [ ] CI/CD pipeline passes

### Rationale

These standards ensure:
- **Quality**: High test coverage and pass rates prevent regressions
- **Traceability**: Git commits and @fix_plan.md provide clear history of changes
- **Maintainability**: Current documentation reduces onboarding time and prevents knowledge loss
- **Collaboration**: Pushed changes enable team visibility and code review
- **Reliability**: Consistent quality gates maintain production stability
- **Automation**: Ralph integration ensures continuous development practices

**Enforcement**: AI agents should automatically apply these standards to all feature development tasks without requiring explicit instruction for each task.