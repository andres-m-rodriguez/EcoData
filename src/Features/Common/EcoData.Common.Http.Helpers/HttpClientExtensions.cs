using System.Net.Http.Json;
using EcoData.Common.Results;

namespace EcoData.Common.Http.Helpers;

public static class HttpClientExtensions
{
    public static async Task<Result<T>> TryGetFromJsonAsync<T>(
        this HttpClient httpClient,
        string? requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<T>(requestUri, cancellationToken);

            return response is not null
                ? response
                : CommonErrors.External("Http", "Received null response");
        }
        catch (HttpRequestException ex)
        {
            return CommonErrors.External("Http", ex.Message)
                .WithMetadata("StatusCode", ex.StatusCode?.ToString() ?? "Unknown");
        }
    }

    public static async Task<Result<T>> TryGetFromJsonAsync<T>(
        this HttpClient httpClient,
        Uri? requestUri,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<T>(requestUri, cancellationToken);

            return response is not null
                ? response
                : CommonErrors.External("Http", "Received null response");
        }
        catch (HttpRequestException ex)
        {
            return CommonErrors.External("Http", ex.Message)
                .WithMetadata("StatusCode", ex.StatusCode?.ToString() ?? "Unknown");
        }
    }
}
