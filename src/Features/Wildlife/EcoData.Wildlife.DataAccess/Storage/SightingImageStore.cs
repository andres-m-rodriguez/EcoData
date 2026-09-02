using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EcoData.Wildlife.DataAccess.Interfaces;

namespace EcoData.Wildlife.DataAccess.Storage;

public sealed class SightingImageStore(BlobContainerClient container) : ISightingImageStore
{
    public async Task UploadAsync(
        string blobName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        await container
            .GetBlobClient(blobName)
            .UploadAsync(
                content,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                },
                cancellationToken
            );
    }

    public async Task<Stream?> OpenReadAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var download = await container
                .GetBlobClient(blobName)
                .DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (RequestFailedException e) when (e.Status == 404)
        {
            return null;
        }
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default) =>
        container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
}
