using Npgsql;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public sealed class CustomerRepository(NpgsqlDataSource dataSource) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT cu.customer_id, cu.customer_type, cu.first_name, cu.last_name, cu.phone, COUNT(c.car_id)::int FROM customer cu LEFT JOIN car c ON c.customer_id = cu.customer_id GROUP BY cu.customer_id ORDER BY cu.last_name, cu.first_name";
        var customers = new List<Customer>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            customers.Add(new Customer
            {
                CustomerId = reader.GetInt32(0),
                CustomerType = reader.GetString(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                Phone = reader.GetString(4),
                CarCount = reader.GetInt32(5)
            });
        }

        return customers;
    }

    public async Task<CustomerForm?> GetFormAsync(int customerId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT customer_id, customer_type, first_name, last_name, phone FROM customer WHERE customer_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", customerId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new CustomerForm
            {
                CustomerId = reader.GetInt32(0),
                CustomerType = reader.GetString(1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                Phone = reader.GetString(4)
            }
            : null;
    }

    public async Task<int> CreateAsync(CustomerForm form, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO customer (customer_type, first_name, last_name, phone) VALUES (@type, @first_name, @last_name, @phone) RETURNING customer_id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, form);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdateAsync(CustomerForm form, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE customer SET customer_type = @type, first_name = @first_name, last_name = @last_name, phone = @phone WHERE customer_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, form);
        command.Parameters.AddWithValue("id", form.CustomerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int customerId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM customer WHERE customer_id = @id", connection);
        command.Parameters.AddWithValue("id", customerId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameters(NpgsqlCommand command, CustomerForm form)
    {
        command.Parameters.AddWithValue("type", form.CustomerType);
        command.Parameters.AddWithValue("first_name", form.FirstName.Trim());
        command.Parameters.AddWithValue("last_name", form.LastName.Trim());
        command.Parameters.AddWithValue("phone", form.Phone.Trim());
    }
}
