namespace Ralph.Core.Utils;

/// <summary>
/// Cross-platform date utilities
/// </summary>
public static class DateUtils
{
    /// <summary>
    /// Get ISO 8601 timestamp with timezone (YYYY-MM-DDTHH:MM:SS+HH:MM)
    /// </summary>
    public static string GetIsoTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    /// <summary>
    /// Get basic timestamp (YYYY-MM-DD HH:MM:SS)
    /// </summary>
    public static string GetBasicTimestamp()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Get Unix epoch seconds
    /// </summary>
    public static long GetEpochSeconds()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Get the next hour boundary from a given time
    /// </summary>
    public static DateTime GetNextHourTime(DateTime from)
    {
        var next = new DateTime(from.Year, from.Month, from.Day, from.Hour, 0, 0, from.Kind);
        return next.AddHours(1);
    }

    /// <summary>
    /// Get current hour key in YYYYMMDDHH format (for rate limiting)
    /// </summary>
    public static string GetCurrentHourKey()
    {
        return DateTime.UtcNow.ToString("yyyyMMddHH");
    }

    /// <summary>
    /// Parse hour key back to DateTime
    /// </summary>
    public static DateTime ParseHourKey(string hourKey)
    {
        if (hourKey.Length != 10)
            throw new ArgumentException("Invalid hour key format. Expected YYYYMMDDHH.", nameof(hourKey));

        var year = int.Parse(hourKey.Substring(0, 4));
        var month = int.Parse(hourKey.Substring(4, 2));
        var day = int.Parse(hourKey.Substring(6, 2));
        var hour = int.Parse(hourKey.Substring(8, 2));

        return new DateTime(year, month, day, hour, 0, 0, DateTimeKind.Utc);
    }
}
