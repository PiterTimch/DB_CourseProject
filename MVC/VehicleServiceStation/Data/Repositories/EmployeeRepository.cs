using Npgsql;
using NpgsqlTypes;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public sealed class EmployeeRepository(NpgsqlDataSource dataSource) : IEmployeeRepository
{
    public async Task<Employee?> FindByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT employee_id, first_name, last_name, role, login, password_hash FROM employee WHERE login = @login";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("login", login);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadEmployee(reader) : null;
    }

    public async Task<Employee?> GetAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT employee_id, first_name, last_name, role, login, password_hash FROM employee WHERE employee_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", employeeId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadEmployee(reader) : null;
    }

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT employee_id, first_name, last_name, role, login, password_hash FROM employee ORDER BY last_name, first_name";
        var employees = new List<Employee>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            employees.Add(ReadEmployee(reader));
        }

        return employees;
    }

    public async Task<int> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO employee (first_name, last_name, role, login, password_hash) VALUES (@first_name, @last_name, @role, @login, @password_hash) RETURNING employee_id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdateAsync(Employee employee, string? passwordHash, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE employee SET first_name = @first_name, last_name = @last_name, role = @role, login = @login, password_hash = COALESCE(@password_hash, password_hash) WHERE employee_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);
        command.Parameters.AddWithValue("password_hash", NpgsqlDbType.Varchar, (object?)passwordHash ?? DBNull.Value);
        command.Parameters.AddWithValue("id", employee.EmployeeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM employee WHERE employee_id = @id", connection);
        command.Parameters.AddWithValue("id", employeeId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Employee ReadEmployee(NpgsqlDataReader reader)
    {
        return new Employee
        {
            EmployeeId = reader.GetInt32(0),
            FirstName = reader.GetString(1),
            LastName = reader.GetString(2),
            Role = reader.GetString(3),
            Login = reader.GetString(4),
            PasswordHash = reader.GetString(5)
        };
    }

    private static void AddEmployeeParameters(NpgsqlCommand command, Employee employee)
    {
        command.Parameters.AddWithValue("first_name", employee.FirstName);
        command.Parameters.AddWithValue("last_name", employee.LastName);
        command.Parameters.AddWithValue("role", employee.Role);
        command.Parameters.AddWithValue("login", employee.Login);
        command.Parameters.AddWithValue("password_hash", employee.PasswordHash);
    }
}
