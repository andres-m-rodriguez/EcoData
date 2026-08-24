using EcoData.Organization.Contracts.Dtos;

namespace EcoData.Organization.DataAccess.Interfaces;

public interface IDataSourceRepository
{
    Task<DataSourceDtoForCreated?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<DataSourceDtoForList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataSourceDtoForList>> GetListAsync(CancellationToken cancellationToken = default);
    Task<DataSourceDtoForCreated> CreateAsync(DataSourceDtoForCreate dto, CancellationToken cancellationToken = default);
}
