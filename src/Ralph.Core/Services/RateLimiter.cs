using Ralph.Core.Interfaces;
using Ralph.Core.Models;

namespace Ralph.Core.Services;

/// <summary>
/// API rate limiting implementation
/// </summary>
public class RateLimiter : IRateLimiter
{
    private readonly IStateStore _stateStore;
    private const string CallCountFile = ".call_count";
    private const string LastResetFile = ".last_reset";

    private RateLimitInfo _info = new();

    public RateLimiter(IStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public async Task InitializeAsync(int maxCallsPerHour = 100, CancellationToken cancellationToken = default)
    {
        _info.MaxCallsPerHour = maxCallsPerHour;

        // Load call count
        var countData = await _stateStore.LoadAsync<CallCountData>(CallCountFile, cancellationToken);
        if (countData != null)
        {
            _info.CallCount = countData.Count;
        }

        // Load last reset time
        var resetData = await _stateStore.LoadAsync<ResetData>(LastResetFile, cancellationToken);
        if (resetData != null)
        {
            _info.LastReset = resetData.LastReset;
        }
        else
        {
            _info.LastReset = DateTime.UtcNow;
        }

        // Calculate next reset (next hour boundary)
        _info.NextReset = GetNextHourBoundary(_info.LastReset);

        // Check if we need to reset based on current time
        var currentHour = GetCurrentHourKey();
        var lastResetHour = GetHourKey(_info.LastReset);

        if (currentHour != lastResetHour)
        {
            await ResetAsync(cancellationToken);
        }
    }

    public Task<bool> CanMakeCallAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!_info.IsLimitReached);
    }

    public async Task RecordCallAsync(CancellationToken cancellationToken = default)
    {
        _info.CallCount++;

        var countData = new CallCountData { Count = _info.CallCount };
        await _stateStore.SaveAsync(CallCountFile, countData, cancellationToken);
    }

    public Task<RateLimitInfo> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_info);
    }

    public async Task WaitForResetAsync(Action<string>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        while (_info.IsLimitReached && DateTime.UtcNow < _info.NextReset)
        {
            var remaining = _info.TimeUntilReset;

            if (progressCallback != null)
            {
                var message = $"API rate limit reached ({_info.CallCount}/{_info.MaxCallsPerHour}). " +
                             $"Reset in {_info.GetCountdownString()}";
                progressCallback(message);
            }

            // Check every minute
            var delay = remaining.TotalSeconds > 60 ? TimeSpan.FromMinutes(1) : remaining;
            await Task.Delay(delay, cancellationToken);

            // Check if it's time to reset
            var currentHour = GetCurrentHourKey();
            var lastResetHour = GetHourKey(_info.LastReset);

            if (currentHour != lastResetHour)
            {
                await ResetAsync(cancellationToken);
                break;
            }
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _info.CallCount = 0;
        _info.LastReset = DateTime.UtcNow;
        _info.NextReset = GetNextHourBoundary(_info.LastReset);

        var countData = new CallCountData { Count = 0 };
        await _stateStore.SaveAsync(CallCountFile, countData, cancellationToken);

        var resetData = new ResetData { LastReset = _info.LastReset };
        await _stateStore.SaveAsync(LastResetFile, resetData, cancellationToken);
    }

    private static DateTime GetNextHourBoundary(DateTime from)
    {
        var next = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, DateTimeKind.Utc);
        return next.AddHours(1);
    }

    private static string GetCurrentHourKey()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHH");
    }

    private static string GetHourKey(DateTime dateTime)
    {
        return dateTime.ToString("yyyyMMddHH");
    }

    private class CallCountData
    {
        public int Count { get; set; }
    }

    private class ResetData
    {
        public DateTime LastReset { get; set; }
    }
}
