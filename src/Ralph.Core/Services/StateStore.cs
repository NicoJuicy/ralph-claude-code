using System.Text.Json;
using Ralph.Core.Interfaces;

namespace Ralph.Core.Services;

/// <summary>
/// File-based state storage implementation
/// </summary>
public class StateStore : IStateStore
{
    private readonly string _baseDirectory;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public StateStore(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
    }

    public async Task SaveAsync<T>(string filename, T data, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_baseDirectory, filename);
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public async Task<T?> LoadAsync<T>(string filename, CancellationToken cancellationToken = default) where T : class
    {
        var filePath = Path.Combine(_baseDirectory, filename);

        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            // Invalid JSON - return null
            return null;
        }
    }

    public Task<bool> ExistsAsync(string filename, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_baseDirectory, filename);
        return Task.FromResult(File.Exists(filePath));
    }

    public Task DeleteAsync(string filename, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_baseDirectory, filename);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    public async Task AppendLogAsync(string filename, string line, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_baseDirectory, filename);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        await File.AppendAllLinesAsync(filePath, new[] { line }, cancellationToken);
    }

    public async Task<List<string>> ReadLogAsync(string filename, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_baseDirectory, filename);

        if (!File.Exists(filePath))
            return new List<string>();

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        return lines.ToList();
    }
}
