using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using OneOf;

namespace EcoData.Common.Problems;

/// <summary>
/// Extension members for reading problem payloads from an <see cref="HttpResponseMessage"/>.
/// </summary>
public static class ProblemDetailsHttpExtensions
{
    /// <summary>The RFC 9457 media type for problem responses.</summary>
    public const string ProblemMediaType = "application/problem+json";

    private const string ReflectionWarning =
        "Uses reflection-based JSON serialization. Pass a JsonTypeInfo<T> instead where trimming or AOT matters.";

    extension(HttpResponseMessage response)
    {
        /// <summary>
        /// Returns the problem payload of a failed response, or null for a success response.
        /// A failed response without a parseable problem body yields a minimal problem built
        /// from the status code.
        /// </summary>
        public async Task<EcoDataProblemDetails?> ReadProblemAsync(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(response);
            if (response.IsSuccessStatusCode)
                return null;

            // Media types are case-insensitive per RFC 9110.
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (string.Equals(mediaType, ProblemMediaType, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var problem = await response.Content
                        .ReadFromJsonAsync(EcoDataProblemJsonContext.Default.EcoDataProblemDetails, cancellationToken)
                        .ConfigureAwait(false);
                    if (problem is not null)
                        return problem.Status is null
                            ? problem with { Status = response.StatusCode }
                            : problem;
                }
                catch (JsonException)
                {
                    // Body claimed to be JSON but was not parseable; fall through to the status-code fallback.
                }
            }

            return new EcoDataProblemDetails
            {
                Status = response.StatusCode,
                Title = string.IsNullOrEmpty(response.ReasonPhrase)
                    ? $"The request failed with status code {(int)response.StatusCode}."
                    : response.ReasonPhrase,
            };
        }

        /// <summary>
        /// Reads the response as either the success payload or a problem. A failed response yields
        /// the <see cref="EcoDataProblemDetails"/> case; a success response whose body is missing,
        /// unparseable, or JSON null throws <see cref="JsonException"/>.
        /// </summary>
        public async Task<OneOf<T, EcoDataProblemDetails>> ReadOneOfAsync<T>(
            JsonTypeInfo<T> typeInfo,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(typeInfo);

            var problem = await response.ReadProblemAsync(cancellationToken).ConfigureAwait(false);
            if (problem is not null)
                return problem;

            var payload = await response.Content
                .ReadFromJsonAsync(typeInfo, cancellationToken)
                .ConfigureAwait(false);
            return payload is null
                ? throw new JsonException("The success response body deserialized to null.")
                : payload;
        }

        /// <summary>
        /// Reflection-based counterpart of the <see cref="JsonTypeInfo{T}"/> overload, for callers
        /// without a source-generated context.
        /// </summary>
        [RequiresUnreferencedCode(ReflectionWarning)]
        [RequiresDynamicCode(ReflectionWarning)]
        public async Task<OneOf<T, EcoDataProblemDetails>> ReadOneOfAsync<T>(CancellationToken cancellationToken = default)
        {
            var problem = await response.ReadProblemAsync(cancellationToken).ConfigureAwait(false);
            if (problem is not null)
                return problem;

            var payload = await response.Content
                .ReadFromJsonAsync<T>(cancellationToken)
                .ConfigureAwait(false);
            return payload is null
                ? throw new JsonException("The success response body deserialized to null.")
                : payload;
        }
    }
}
