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
    /// Absolute base for public image URLs — container included, trailing slash
    /// present — so a projection can concatenate a blob name onto it and hand the
    /// browser a URL it fetches from storage directly.
    ///
    /// <para>A plain string rather than a method so it can be captured as a local
    /// and translated into SQL concatenation inside an EF projection.</para>
    /// </summary>
    string PublicBaseUrl { get; }
}
