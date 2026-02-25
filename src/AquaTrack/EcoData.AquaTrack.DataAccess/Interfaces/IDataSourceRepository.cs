using EcoData.AquaTrack.Contracts.Dtos;
using EcoData.AquaTrack.Database.Models;

namespace EcoData.AquaTrack.DataAccess.Interfaces;

public interface IDataSourceRepository
{
    Task<DataSource?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DataSourceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSourceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DataSource> CreateAsync(DataSource dataSource, CancellationToken cancellationToken = default);
}
