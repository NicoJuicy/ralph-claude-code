# AGENT.md — ASP.NET MVC Agent Project

## Description
ASP.NET MVC web application with optional Swagger and Hangfire support. Full MVC stack suitable for agent web orchestration, controller-triggered actions, and scheduled background jobs.

# Agent Build Instructions

## Project Setup
```bash
# Create project
dotnet new mvc -n MyAgentWeb
cd MyAgentWeb

# Install dependencies
dotnet add package MediatR
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.Extensions.DependencyInjection

# Optional: Swagger for API
dotnet add package Swashbuckle.AspNetCore

# Optional: Hangfire for background jobs
dotnet add package Hangfire.AspNetCore

# Initialize git
git init
git branch -M main
git add .
git commit -m "chore(init): create MVC project"
```

## Running Tests
```bash
# Setup test project
dotnet new xunit -n MyAgentWeb.Tests
cd MyAgentWeb.Tests
dotnet add reference ../MyAgentWeb/MyAgentWeb.csproj
dotnet test
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
- Full MVC stack suitable for agent web orchestration.
- Agent can trigger controller actions or background jobs.
- Hangfire allows scheduled agent workflows.
- Swagger enables API testing for agent endpoints.

## Feature Development Quality Standards
**(Same as .NET CLI)**

### Testing Requirements
- Minimum coverage: 85%
- Unit + integration tests
- Coverage validation:
```bash
dotnet test /p:CollectCoverage=true
```

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
