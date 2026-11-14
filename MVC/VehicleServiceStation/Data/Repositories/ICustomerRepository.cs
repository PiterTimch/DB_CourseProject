using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CustomerForm?> GetFormAsync(int customerId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CustomerForm form, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomerForm form, CancellationToken cancellationToken = default);
    Task DeleteAsync(int customerId, CancellationToken cancellationToken = default);
}
