using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public interface ICatalogRepository
{
    Task<IReadOnlyList<CatalogItem>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<ServiceForm?> GetServiceFormAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateServiceAsync(ServiceForm form, CancellationToken cancellationToken = default);
    Task UpdateServiceAsync(ServiceForm form, CancellationToken cancellationToken = default);
    Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogItem>> GetPartsAsync(CancellationToken cancellationToken = default);
    Task<PartForm?> GetPartFormAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreatePartAsync(PartForm form, CancellationToken cancellationToken = default);
    Task UpdatePartAsync(PartForm form, CancellationToken cancellationToken = default);
    Task DeletePartAsync(int id, CancellationToken cancellationToken = default);
}
