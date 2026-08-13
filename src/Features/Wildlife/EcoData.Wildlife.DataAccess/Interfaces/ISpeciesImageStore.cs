namespace EcoData.Wildlife.DataAccess.Interfaces;

/// <summary>
/// Species profile images, held in blob storage rather than in the wildlife
/// database. The row keeps the blob name this returns; everything else about the
/// storage layout stays behind the implementation.
/// </summary>
public interface ISpeciesImageStore
{
    /// <summary>
    /// Writes (or replaces) the profile image for a species.
    /// </summary>
    /// <returns>The blob name to persist on the species row.</returns>
    Task<string> SaveAsync(
        Guid speciesId,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Opens the stored image for reading, or returns <see langword="null"/> when
    /// the blob is gone — a row can outlive its image, and that is a 404, not a 500.
    /// </summary>
    Task<Stream?> OpenReadAsync(string blobName, CancellationToken cancellationToken = default);
}
