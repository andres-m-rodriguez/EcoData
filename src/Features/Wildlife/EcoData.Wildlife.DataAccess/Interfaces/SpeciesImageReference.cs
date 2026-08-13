namespace EcoData.Wildlife.DataAccess.Interfaces;

/// <summary>
/// Where a species' profile image lives. <see cref="ISpeciesRepository"/> reads it
/// off the row, <see cref="ISpeciesImageStore"/> turns it into bytes. Data-access
/// detail rather than a wire contract, so it stays out of Contracts.
/// </summary>
public sealed record SpeciesImageReference(string BlobName, string ContentType);
