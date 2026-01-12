using System.Text.Json.Serialization;

namespace Ralph.Core.Models;

/// <summary>
/// Represents a response from Claude Code CLI (JSON format)
/// </summary>
public class ClaudeResponse
{
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("metadata")]
    public ClaudeMetadata? Metadata { get; set; }

    // Flat format support
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("exit_signal")]
    public bool? ExitSignal { get; set; }

    [JsonPropertyName("work_type")]
    public string? WorkType { get; set; }

    [JsonPropertyName("files_modified")]
    public int? FilesModified { get; set; }

    [JsonPropertyName("error_count")]
    public int? ErrorCount { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

/// <summary>
/// Metadata from Claude CLI format
/// </summary>
public class ClaudeMetadata
{
    [JsonPropertyName("files_changed")]
    public int? FilesChanged { get; set; }

    [JsonPropertyName("has_errors")]
    public bool? HasErrors { get; set; }

    [JsonPropertyName("completion_status")]
    public string? CompletionStatus { get; set; }

    [JsonPropertyName("progress_indicators")]
    public List<string>? ProgressIndicators { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }
}
