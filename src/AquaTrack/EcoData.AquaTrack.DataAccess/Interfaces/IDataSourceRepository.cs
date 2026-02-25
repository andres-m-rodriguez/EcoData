using EcoData.AquaTrack.Contracts.Dtos;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IDataSourceRepository
{
    Task<DataSourceDtoForCreated?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DataSourceDtoForList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSourceDtoForList>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DataSourceDtoForCreated> CreateAsync(DataSourceDtoForCreate dto, CancellationToken cancellationToken = default);
}
