namespace Ralph.Core.Interfaces;

/// <summary>
/// Stores and retrieves state from the file system
/// </summary>
public interface IStateStore
{
    /// <summary>
    /// Save state to a file
    /// </summary>
    Task SaveAsync<T>(string filename, T data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load state from a file
    /// </summary>
    Task<T?> LoadAsync<T>(string filename, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Check if a state file exists
    /// </summary>
    Task<bool> ExistsAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a state file
    /// </summary>
    Task DeleteAsync(string filename, CancellationToken cancellationToken = default);

    /// <summary>
    /// Append a line to a log file
    /// </summary>
    Task AppendLogAsync(string filename, string line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read all lines from a log file
    /// </summary>
    Task<List<string>> ReadLogAsync(string filename, CancellationToken cancellationToken = default);
}
