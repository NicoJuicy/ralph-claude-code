using FluentAssertions;
using NSubstitute;
using Ralph.Core.Interfaces;
using Ralph.Core.Models;
using Ralph.Core.Services;
using Xunit;

namespace Ralph.Tests;

public class SessionManagerTests
{
    private readonly IStateStore _stateStore;
    private readonly SessionManager _sut;

    public SessionManagerTests()
    {
        _stateStore = Substitute.For<IStateStore>();
        _sut = new SessionManager(_stateStore);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoExistingSession_CreatesNullSession()
    {
        // Arrange
        _stateStore.LoadAsync<SessionInfo>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionInfo?>(null));

        // Act
        await _sut.InitializeAsync();
        var session = await _sut.GetCurrentSessionAsync();

        // Assert
        session.Should().BeNull();
    }

    [Fact]
    public async Task InitializeAsync_WhenExpiredSession_ResetsSession()
    {
        // Arrange
        var expiredSession = new SessionInfo
        {
            SessionId = "old-session-id",
            CreatedAt = DateTime.UtcNow.AddDays(-2), // 2 days old
            LastUsed = DateTime.UtcNow.AddDays(-2),
            LoopCount = 5
        };

        _stateStore.LoadAsync<SessionInfo>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SessionInfo?>(expiredSession));

        // Act
        await _sut.InitializeAsync();
        var session = await _sut.GetCurrentSessionAsync();

        // Assert
        session.Should().BeNull();
        await _stateStore.Received().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreSessionAsync_WhenNoCurrentSession_CreatesNewSession()
    {
        // Arrange
        await _sut.InitializeAsync();
        const string sessionId = "test-session-123";

        // Act
        await _sut.StoreSessionAsync(sessionId);
        var session = await _sut.GetCurrentSessionAsync();

        // Assert
        session.Should().NotBeNull();
        session!.SessionId.Should().Be(sessionId);
        session.LoopCount.Should().Be(1);
        session.IsValid().Should().BeTrue();
    }

    [Fact]
    public async Task StoreSessionAsync_WhenExistingSession_UpdatesSessionAndIncrementsLoopCount()
    {
        // Arrange
        await _sut.InitializeAsync();
        await _sut.StoreSessionAsync("session-1");

        // Act
        await _sut.StoreSessionAsync("session-2");
        var session = await _sut.GetCurrentSessionAsync();

        // Assert
        session.Should().NotBeNull();
        session!.SessionId.Should().Be("session-2");
        session.LoopCount.Should().Be(2);
    }

    [Fact]
    public async Task ShouldResumeSessionAsync_WhenNoSession_ReturnsFalse()
    {
        // Arrange
        await _sut.InitializeAsync();

        // Act
        var result = await _sut.ShouldResumeSessionAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldResumeSessionAsync_WhenValidSession_ReturnsTrue()
    {
        // Arrange
        await _sut.InitializeAsync();
        await _sut.StoreSessionAsync("valid-session");

        // Act
        var result = await _sut.ShouldResumeSessionAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ResetSessionAsync_DeletesSessionAndLogsTransition()
    {
        // Arrange
        await _sut.InitializeAsync();
        await _sut.StoreSessionAsync("session-to-reset");

        // Act
        await _sut.ResetSessionAsync("Test reset");
        var session = await _sut.GetCurrentSessionAsync();

        // Assert
        session.Should().BeNull();
        await _stateStore.Received().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _stateStore.Received().AppendLogAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("session_reset")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogSessionTransitionAsync_AppendsTransitionToHistory()
    {
        // Arrange
        await _sut.InitializeAsync();
        await _sut.StoreSessionAsync("test-session");

        // Act
        await _sut.LogSessionTransitionAsync("test_event", "test reason");

        // Assert
        await _stateStore.Received().AppendLogAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("test_event") && s.Contains("test reason")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSessionHistoryAsync_ReturnsHistoryEntries()
    {
        // Arrange
        var historyLines = new List<string>
        {
            "{\"Timestamp\":\"2024-01-01T10:00:00Z\",\"Event\":\"session_stored\",\"SessionId\":\"session-1\",\"Reason\":null}",
            "{\"Timestamp\":\"2024-01-01T11:00:00Z\",\"Event\":\"session_reset\",\"SessionId\":null,\"Reason\":\"Manual reset\"}"
        };

        _stateStore.ReadLogAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(historyLines));

        await _sut.InitializeAsync();

        // Act
        var history = await _sut.GetSessionHistoryAsync();

        // Assert
        history.Should().HaveCount(2);
        history[0].Event.Should().Be("session_stored");
        history[1].Event.Should().Be("session_reset");
    }
}
