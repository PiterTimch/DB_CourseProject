using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public interface IOrderRepository
{
    Task<IReadOnlyList<OrderSummary>> GetAllAsync(string? query, string? status, CancellationToken cancellationToken = default);
    Task<OrderDetails?> GetDetailsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummary>> GetCarHistoryAsync(int carId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(OrderCreateForm form, int employeeId, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderEditForm form, CancellationToken cancellationToken = default);
    Task DeleteAsync(int orderId, CancellationToken cancellationToken = default);
    Task AddServiceAsync(int orderId, AddServiceForm form, CancellationToken cancellationToken = default);
    Task AddPartAsync(int orderId, AddPartForm form, CancellationToken cancellationToken = default);
    Task RemoveServiceAsync(int orderId, int serviceId, CancellationToken cancellationToken = default);
    Task RemovePartAsync(int orderId, int partId, CancellationToken cancellationToken = default);
    Task UpdateDiscountAsync(int orderId, decimal discount, CancellationToken cancellationToken = default);
    Task CloseAsync(int orderId, CancellationToken cancellationToken = default);
}
