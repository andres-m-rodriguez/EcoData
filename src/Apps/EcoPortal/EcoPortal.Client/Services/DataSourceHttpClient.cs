using System.Net.Http.Json;
using EcoData.Common.Problems.Contracts;
using EcoData.Organization.Contracts.Dtos;
using OneOf;

namespace EcoPortal.Client.Services;

public sealed class DataSourceHttpClient(HttpClient httpClient) : IDataSourceHttpClient
{
    public async Task<OneOf<IReadOnlyList<DataSourceDtoForList>, RequestFailed>> GetDataSourcesAsync(
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var response = await httpClient.GetAsync("organization/datasources", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var problem = await ProblemDetailsParser.ParseAsync(response, cancellationToken);
                return new RequestFailed((int)response.StatusCode, problem?.Detail ?? problem?.Title);
            }

            var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<DataSourceDtoForList>>(cancellationToken);
            if (result is null)
                return new RequestFailed(
                    (int)response.StatusCode,
                    "The server returned an empty response."
                );

            return OneOf<IReadOnlyList<DataSourceDtoForList>, RequestFailed>.FromT0(result);
        }
        catch (HttpRequestException e)
        {
            return new RequestFailed(0, e.Message);
        }
    }
}
