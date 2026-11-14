using Microsoft.AspNetCore.Mvc.Rendering;
using VehicleServiceStation.Data.Repositories;

namespace VehicleServiceStation.Data;

public sealed class FormOptionsService(
    ICustomerRepository customers,
    ICarRepository cars,
    IEmployeeRepository employees,
    ICatalogRepository catalog)
{
    public async Task<IReadOnlyList<SelectListItem>> CustomersAsync(CancellationToken cancellationToken = default)
    {
        return (await customers.GetAllAsync(cancellationToken))
            .Select(item => new SelectListItem($"{item.LastName} {item.FirstName} · {item.Phone}", item.CustomerId.ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyList<SelectListItem>> CarsAsync(CancellationToken cancellationToken = default)
    {
        return (await cars.GetAllAsync(cancellationToken))
            .Select(item => new SelectListItem($"{item.Model} · {item.LicensePlate} · {item.CustomerName}", item.CarId.ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyList<SelectListItem>> EmployeesAsync(CancellationToken cancellationToken = default)
    {
        return (await employees.GetAllAsync(cancellationToken))
            .Where(item => item.Role is "manager" or "admin")
            .Select(item => new SelectListItem($"{item.LastName} {item.FirstName}", item.EmployeeId.ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyList<SelectListItem>> ServicesAsync(CancellationToken cancellationToken = default)
    {
        return (await catalog.GetServicesAsync(cancellationToken))
            .Select(item => new SelectListItem($"{item.Name} · {item.Price:N2} ₴", item.Id.ToString()))
            .ToArray();
    }

    public async Task<IReadOnlyList<SelectListItem>> PartsAsync(CancellationToken cancellationToken = default)
    {
        return (await catalog.GetPartsAsync(cancellationToken))
            .Select(item => new SelectListItem($"{item.Name} · {item.Article} · {item.Price:N2} ₴", item.Id.ToString()))
            .ToArray();
    }
}
