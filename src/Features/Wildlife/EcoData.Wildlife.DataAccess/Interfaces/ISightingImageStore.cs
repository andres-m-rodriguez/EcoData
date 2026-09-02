namespace EcoData.Wildlife.DataAccess.Interfaces;

// The bytes behind sighting_images. Blob names are {sightingId}/{imageId}.{ext},
// minted by the endpoint; nothing here ever hands out a URL into the container.
public interface ISightingImageStore
{
    Task UploadAsync(
        string blobName,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default
    );

    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);

    Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
