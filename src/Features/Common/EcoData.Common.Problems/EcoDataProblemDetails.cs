using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoData.Common.Problems;

/// <summary>
/// A problem details payload as defined by RFC 9457.
/// </summary>
public sealed record EcoDataProblemDetails
{
    /// <summary>URI reference identifying the problem type. Defaults to "about:blank" per RFC 9457.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "about:blank";

    /// <summary>Short, human-readable summary of the problem type.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>HTTP status code for this occurrence of the problem.</summary>
    [JsonPropertyName("status")]
    public int? Status { get; init; }

    /// <summary>Human-readable explanation specific to this occurrence.</summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }

    /// <summary>URI reference identifying this specific occurrence.</summary>
    [JsonPropertyName("instance")]
    public string? Instance { get; init; }

    /// <summary>Correlation identifier for this occurrence.</summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>Per-field validation errors, keyed by field name.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>
    /// Additional members not covered by the fields above, preserved across serialization round trips.
    /// Settable rather than init-only because System.Text.Json cannot bind extension data through
    /// constructor-style init deserialization.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; set; }
}
