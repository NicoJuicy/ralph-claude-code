using FluentAssertions;
using NSubstitute;
using Ralph.Core.Interfaces;
using Ralph.Core.Models;
using Ralph.Core.Services;
using Xunit;

namespace Ralph.Tests;

public class CircuitBreakerTests
{
    private readonly IStateStore _stateStore;
    private readonly CircuitBreaker _sut;

    public CircuitBreakerTests()
    {
        _stateStore = Substitute.For<IStateStore>();
        _sut = new CircuitBreaker(_stateStore);
    }

    [Fact]
    public async Task InitializeAsync_WhenNoExistingState_CreatesClosedState()
    {
        // Arrange
        _stateStore.LoadAsync<CircuitBreakerInfo>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<CircuitBreakerInfo?>(null));

        // Act
        await _sut.InitializeAsync();
        var state = await _sut.GetStateAsync();

        // Assert
        state.State.Should().Be(CircuitBreakerState.Closed);
        state.NoProgressCount.Should().Be(0);
        state.SameErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task CanExecuteAsync_WhenClosed_ReturnsTrue()
    {
        // Arrange
        await _sut.InitializeAsync();

        // Act
        var result = await _sut.CanExecuteAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RecordLoopResultAsync_WithNoProgress_IncrementsNoProgressCount()
    {
        // Arrange
        await _sut.InitializeAsync();

        // Act
        await _sut.RecordLoopResultAsync(filesModified: 0, error: null);
        await _sut.RecordLoopResultAsync(filesModified: 0, error: null);
        var state = await _sut.GetStateAsync();

        // Assert
        state.NoProgressCount.Should().Be(2);
    }

    [Fact]
    public async Task RecordLoopResultAsync_WithProgress_ResetsNoProgressCount()
    {
        // Arrange
        await _sut.InitializeAsync();
        await _sut.RecordLoopResultAsync(filesModified: 0, error: null);
        await _sut.RecordLoopResultAsync(filesModified: 0, error: null);

        // Act
        await _sut.RecordLoopResultAsync(filesModified: 5, error: null);
        var state = await _sut.GetStateAsync();

        // Assert
        state.NoProgressCount.Should().Be(0);
    }

    [Fact]
    public async Task RecordLoopResultAsync_WithRepeatedNoProgress_TransitionsToHalfOpen()
    {
        // Arrange
        await _sut.InitializeAsync();

        // Act
        for (int i = 0; i < 3; i++)
        {
            await _sut.RecordLoopResultAsync(filesModified: 0, error: null);
        }

        var state = await _sut.GetStateAsync();

        // Assert
        state.State.Should().Be(CircuitBreakerState.HalfOpen);
        state.StateChangeReason.Should().Contain("No progress");
    }

    [Fact]
    public async Task RecordLoopResultAsync_WithSameError_IncreasesSameErrorCount()
    {
        // Arrange
        await _sut.InitializeAsync();
        const string error = "TypeError: undefined is not a function";

        // Act
        await _sut.RecordLoopResultAsync(filesModified: 1, error: error);
        await _sut.RecordLoopResultAsync(filesModified: 1, error: error);
        await _sut.RecordLoopResultAsync(filesModified: 1, error: error);
        var state = await _sut.GetStateAsync();

        // Assert
        state.SameErrorCount.Should().Be(3);
        state.LastError.Should().Be(error);
    }

    [Fact]
    public async Task RecordLoopResultAsync_WithRepeatedSameError_TransitionsToOpen()
    {
        // Arrange
        await _sut.InitializeAsync();
        const string error = "TypeError: undefined is not a function";

        // Act
        for (int i = 0; i < 5; i++)
        {
            await _sut.RecordLoopResultAsync(filesModified: 1, error: error);
        }

        var state = await _sut.GetStateAsync();

        // Assert
        state.State.Should().Be(CircuitBreakerState.Open);
        state.StateChangeReason.Should().Contain("Same error");
    }

    [Fact]
    public async Task ResetAsync_WhenOpen_TransitionsToClosedAndResetsCounters()
    {
        // Arrange
        await _sut.InitializeAsync();
        // Force transition to Open
        for (int i = 0; i < 5; i++)
        {
            await _sut.RecordLoopResultAsync(filesModified: 0, error: "error");
        }

        // Act
        await _sut.ResetAsync();
        var state = await _sut.GetStateAsync();

        // Assert
        state.State.Should().Be(CircuitBreakerState.Closed);
        state.NoProgressCount.Should().Be(0);
        state.SameErrorCount.Should().Be(0);
        state.LastError.Should().BeNull();
    }

    [Fact]
    public async Task GetStatusStringAsync_ReturnsFormattedStatus()
    {
        // Arrange
        await _sut.InitializeAsync();

        // Act
        var status = await _sut.GetStatusStringAsync();

        // Assert
        status.Should().Contain("CLOSED");
        status.Should().Contain("normal operation");
    }
}
