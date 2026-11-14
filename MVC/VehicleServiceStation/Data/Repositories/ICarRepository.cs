using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public interface ICarRepository
{
    Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Car?> GetAsync(int carId, CancellationToken cancellationToken = default);
    Task<CarForm?> GetFormAsync(int carId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CarForm form, CancellationToken cancellationToken = default);
    Task UpdateAsync(CarForm form, CancellationToken cancellationToken = default);
    Task DeleteAsync(int carId, CancellationToken cancellationToken = default);
}
