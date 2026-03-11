using System.Net;
using System.Net.Http.Json;
using EcoData.AquaTrack.Contracts.Errors;
using EcoData.Common.Http.Helpers;
using EcoData.Identity.Contracts.Parameters;
using EcoData.Identity.Contracts.Requests;
using EcoData.Identity.Contracts.Responses;
using OneOf;

namespace EcoData.AquaTrack.Application.Client;

public sealed class AccessRequestHttpClient(HttpClient httpClient) : IAccessRequestHttpClient
{
    public IAsyncEnumerable<AccessRequestResponse> GetAllAsync(
        Guid organizationId,
        AccessRequestParameters parameters,
        CancellationToken cancellationToken = default
    )
    {
        var queryString = new QueryStringBuilder()
            .Add("pageSize", parameters.PageSize != 20 ? parameters.PageSize : null)
            .Add("cursor", parameters.Cursor)
            .Add("search", parameters.Search)
            .Add("status", parameters.Status)
            .Build();

        return httpClient.GetFromJsonAsAsyncEnumerable<AccessRequestResponse>(
            $"api/organizations/{organizationId}/members/access-requests{queryString}",
            cancellationToken
        )!;
    }

    public async Task<
        OneOf<AccessRequestResponse, NotFoundError, ConflictError, ApiError>
    > UpdateStatusAsync(
        Guid organizationId,
        Guid accessRequestId,
        bool approved,
        string? reviewNotes = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = new UpdateAccessRequestStatusRequest(approved, reviewNotes);
        var response = await httpClient.PutAsJsonAsync(
            $"api/organizations/{organizationId}/members/access-requests/{accessRequestId}/status",
            request,
            cancellationToken
        );

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundError();
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ConflictError(message);
        }

        if (!response.IsSuccessStatusCode)
        {
            return new ApiError(
                (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(cancellationToken)
            );
        }

        var result = await response.Content.ReadFromJsonAsync<AccessRequestResponse>(
            cancellationToken
        );
        return result!;
    }
}
