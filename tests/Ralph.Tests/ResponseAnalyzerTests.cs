using FluentAssertions;
using Ralph.Core.Models;
using Ralph.Core.Services;
using Xunit;

namespace Ralph.Tests;

public class ResponseAnalyzerTests
{
    private readonly ResponseAnalyzer _sut;

    public ResponseAnalyzerTests()
    {
        _sut = new ResponseAnalyzer();
    }

    [Fact]
    public async Task ParseResponseAsync_WithValidJson_ReturnsClaudeResponse()
    {
        // Arrange
        var jsonOutput = @"{
            ""result"": ""Task completed"",
            ""sessionId"": ""session-123"",
            ""metadata"": {
                ""files_changed"": 3,
                ""has_errors"": false,
                ""completion_status"": ""in_progress""
            }
        }";

        // Act
        var result = await _sut.ParseResponseAsync(jsonOutput);

        // Assert
        result.Should().NotBeNull();
        result!.Result.Should().Be("Task completed");
        result.SessionId.Should().Be("session-123");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.FilesChanged.Should().Be(3);
        result.Metadata.HasErrors.Should().BeFalse();
    }

    [Fact]
    public async Task ParseResponseAsync_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "This is not JSON";

        // Act
        var result = await _sut.ParseResponseAsync(invalidJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractStatusAsync_WithValidStatusBlock_ReturnsRalphStatus()
    {
        // Arrange
        var output = @"
Some output text...

---RALPH_STATUS---
STATUS: IN_PROGRESS
TASKS_COMPLETED_THIS_LOOP: 2
FILES_MODIFIED: 5
TESTS_STATUS: PASSING
WORK_TYPE: IMPLEMENTATION
EXIT_SIGNAL: false
RECOMMENDATION: Continue with next task
---END_RALPH_STATUS---

More output...
";

        // Act
        var result = await _sut.ExtractStatusAsync(output);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("IN_PROGRESS");
        result.TasksCompletedThisLoop.Should().Be(2);
        result.FilesModified.Should().Be(5);
        result.TestsStatus.Should().Be("PASSING");
        result.WorkType.Should().Be("IMPLEMENTATION");
        result.ExitSignal.Should().BeFalse();
        result.Recommendation.Should().Be("Continue with next task");
    }

    [Fact]
    public async Task ExtractStatusAsync_WithMissingStatusBlock_ReturnsNull()
    {
        // Arrange
        var output = "Some output without status block";

        // Act
        var result = await _sut.ExtractStatusAsync(output);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DetectTestOnlyLoopAsync_WithTestCommands_ReturnsTrue()
    {
        // Arrange
        var output = @"
Running tests...
npm test
All tests passed!
";

        // Act
        var result = await _sut.DetectTestOnlyLoopAsync(output);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectTestOnlyLoopAsync_WithMixedContent_ReturnsFalse()
    {
        // Arrange
        var output = @"
Implementing new feature...
Modified src/app.js
Modified src/utils.js
Added tests in test/app.test.js
npm test - all passing
Updated documentation
";

        // Act
        var result = await _sut.DetectTestOnlyLoopAsync(output);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DetectCompletionSignalsAsync_WithCompletionKeywords_ReturnsTrue()
    {
        // Arrange
        var output = "All tasks complete and ready for review.";

        // Act
        var result = await _sut.DetectCompletionSignalsAsync(output);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectCompletionSignalsAsync_WithoutCompletionKeywords_ReturnsFalse()
    {
        // Arrange
        var output = "Still working on implementation...";

        // Act
        var result = await _sut.DetectCompletionSignalsAsync(output);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractErrorsAsync_WithActualErrors_ReturnsErrorList()
    {
        // Arrange
        var output = @"
Error: Cannot find module './missing-file'
TypeError: undefined is not a function
Some normal output
Fatal: Process crashed
";

        // Act
        var result = await _sut.ExtractErrorsAsync(output);

        // Assert
        result.Should().HaveCountGreaterThan(0);
        result.Should().Contain(e => e.Contains("Error:"));
        result.Should().Contain(e => e.Contains("Fatal:"));
    }

    [Fact]
    public async Task ExtractErrorsAsync_WithJsonFieldErrors_FiltersThemOut()
    {
        // Arrange
        var output = @"
{
  ""is_error"": false,
  ""error_code"": null
}
Error: Actual error message
";

        // Act
        var result = await _sut.ExtractErrorsAsync(output);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Contain("Actual error message");
    }

    [Fact]
    public async Task DetectStuckLoopAsync_WithSameErrorInAllRecentErrors_ReturnsTrue()
    {
        // Arrange
        var currentError = "TypeError: Cannot read property 'foo' of undefined";
        var recentErrors = new List<string>
        {
            "TypeError: Cannot read property 'foo' of undefined",
            "TypeError: Cannot read property 'foo' of undefined",
            "TypeError: Cannot read property 'foo' of undefined"
        };

        // Act
        var result = await _sut.DetectStuckLoopAsync(currentError, recentErrors);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DetectStuckLoopAsync_WithDifferentErrors_ReturnsFalse()
    {
        // Arrange
        var currentError = "TypeError: Cannot read property 'foo' of undefined";
        var recentErrors = new List<string>
        {
            "ReferenceError: bar is not defined",
            "SyntaxError: Unexpected token",
            "TypeError: Cannot read property 'baz' of null"
        };

        // Act
        var result = await _sut.DetectStuckLoopAsync(currentError, recentErrors);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CalculateExitConfidenceAsync_WithAllExitConditionsMet_ReturnsHighConfidence()
    {
        // Arrange
        var status = new RalphStatus
        {
            ExitSignal = true,
            Status = RalphStatus.StatusValues.Complete,
            TestsStatus = RalphStatus.TestStatusValues.Passing
        };

        var exitSignals = new ExitSignalInfo
        {
            DoneSignals = new List<int> { 1, 2 },
            TestOnlyLoops = new List<int> { 3, 4, 5 }
        };

        // Act
        var confidence = await _sut.CalculateExitConfidenceAsync(status, exitSignals);

        // Assert
        confidence.Should().BeGreaterThan(80); // High confidence
    }

    [Fact]
    public async Task CalculateExitConfidenceAsync_WithNoExitConditions_ReturnsLowConfidence()
    {
        // Arrange
        var status = new RalphStatus
        {
            ExitSignal = false,
            Status = RalphStatus.StatusValues.InProgress,
            TestsStatus = RalphStatus.TestStatusValues.NotRun
        };

        var exitSignals = new ExitSignalInfo();

        // Act
        var confidence = await _sut.CalculateExitConfidenceAsync(status, exitSignals);

        // Assert
        confidence.Should().BeLessThan(30); // Low confidence
    }
}
