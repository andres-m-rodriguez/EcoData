using System.Text.Json.Serialization;

namespace EcoData.Common.Problems;

/// <summary>
/// Source-generated serialization context for <see cref="EcoDataProblemDetails"/>.
/// </summary>
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(EcoDataProblemDetails))]
public sealed partial class EcoDataProblemJsonContext : JsonSerializerContext;
