using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> FindByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<Employee?> GetAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, string? passwordHash, CancellationToken cancellationToken = default);
    Task DeleteAsync(int employeeId, CancellationToken cancellationToken = default);
}
