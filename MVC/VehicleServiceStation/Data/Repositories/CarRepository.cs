using Npgsql;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public sealed class CarRepository(NpgsqlDataSource dataSource) : ICarRepository
{
    public async Task<IReadOnlyList<Car>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT c.car_id, c.customer_id, c.vin, c.license_plate, c.model, concat_ws(' ', cu.last_name, cu.first_name) FROM car c JOIN customer cu ON cu.customer_id = c.customer_id ORDER BY c.model, c.license_plate";
        var cars = new List<Car>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            cars.Add(ReadCar(reader));
        }

        return cars;
    }

    public async Task<Car?> GetAsync(int carId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT c.car_id, c.customer_id, c.vin, c.license_plate, c.model, concat_ws(' ', cu.last_name, cu.first_name) FROM car c JOIN customer cu ON cu.customer_id = c.customer_id WHERE c.car_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", carId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCar(reader) : null;
    }

    public async Task<CarForm?> GetFormAsync(int carId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT car_id, customer_id, vin, license_plate, model FROM car WHERE car_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", carId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new CarForm
            {
                CarId = reader.GetInt32(0),
                CustomerId = reader.GetInt32(1),
                Vin = reader.GetString(2),
                LicensePlate = reader.GetString(3),
                Model = reader.GetString(4)
            }
            : null;
    }

    public async Task<int> CreateAsync(CarForm form, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO car (customer_id, vin, license_plate, model) VALUES (@customer, @vin, @plate, @model) RETURNING car_id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, form);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdateAsync(CarForm form, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE car SET customer_id = @customer, vin = @vin, license_plate = @plate, model = @model WHERE car_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, form);
        command.Parameters.AddWithValue("id", form.CarId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int carId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM car WHERE car_id = @id", connection);
        command.Parameters.AddWithValue("id", carId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Car ReadCar(NpgsqlDataReader reader)
    {
        return new Car
        {
            CarId = reader.GetInt32(0),
            CustomerId = reader.GetInt32(1),
            Vin = reader.GetString(2),
            LicensePlate = reader.GetString(3),
            Model = reader.GetString(4),
            CustomerName = reader.GetString(5)
        };
    }

    private static void AddParameters(NpgsqlCommand command, CarForm form)
    {
        command.Parameters.AddWithValue("customer", form.CustomerId);
        command.Parameters.AddWithValue("vin", form.Vin.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("plate", form.LicensePlate.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("model", form.Model.Trim());
    }
}
