using EcoData.Wildlife.Contracts.Dtos;

namespace EcoData.Wildlife.DataAccess.Interfaces;

public interface ISpeciesDocumentRepository
{
    Task<IReadOnlyList<SpeciesDocumentDto>> GetBySpeciesAsync(Guid speciesId, CancellationToken cancellationToken = default);
}
