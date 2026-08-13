using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EcoData.Wildlife.DataAccess.Interfaces;

namespace EcoData.Wildlife.DataAccess.Storage;

/// <summary>
/// Species profile images in the storage account's blob service: one blob per
/// species under the <c>species/</c> prefix, in a container this type owns. The
/// name is handed back to the caller to persist, so this stays the only place
/// that knows the layout.
/// </summary>
internal sealed class SpeciesImageStore(BlobServiceClient blobServiceClient) : ISpeciesImageStore
{
    private const string ContainerName = "species-images";

    public async Task<string> SaveAsync(
        Guid speciesId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var container = blobServiceClient.GetBlobContainerClient(ContainerName);

        // Azurite comes up with an empty account and a fresh storage account has
        // no containers either, so the write path creates it. Unconditional
        // rather than cached: the call is idempotent, and images are written by
        // the seeder and by uploads, never on a hot path.
        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        var blobName = BuildBlobName(speciesId, contentType);

        using var stream = new MemoryStream(content, writable: false);
        await container
            .GetBlobClient(blobName)
            .UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                },
                cancellationToken
            );

        return blobName;
    }

    public async Task<Stream?> OpenReadAsync(
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        var blob = blobServiceClient.GetBlobContainerClient(ContainerName).GetBlobClient(blobName);

        try
        {
            var download = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
            return download.Value.Content;
        }
        catch (RequestFailedException ex) when (ex.Status is 404)
        {
            // Covers both a missing blob and a missing container — a row whose
            // image never made it to storage reads as "no image".
            return null;
        }
    }

    private static string BuildBlobName(Guid speciesId, string contentType) =>
        $"species/{speciesId}{ExtensionFor(contentType)}";

    /// <summary>
    /// Cosmetic only — the content type travels on the blob's own headers. An
    /// unrecognised type just gets no suffix rather than a wrong one.
    /// </summary>
    private static string ExtensionFor(string contentType) =>
        contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => "",
        };
}
