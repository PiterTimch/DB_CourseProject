using Npgsql;
using NpgsqlTypes;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public sealed class OrderRepository(NpgsqlDataSource dataSource) : IOrderRepository
{
    private const string SummarySelect = "SELECT o.order_id, o.car_id, concat(c.model, ', ', c.license_plate), concat_ws(' ', cu.last_name, cu.first_name), concat_ws(' ', e.last_name, e.first_name), o.order_date, o.deadline, o.status, o.total_cost FROM orders o JOIN car c ON c.car_id = o.car_id JOIN customer cu ON cu.customer_id = c.customer_id JOIN employee e ON e.employee_id = o.employee_id";

    public Task<IReadOnlyList<OrderSummary>> GetAllAsync(string? query, string? status, CancellationToken cancellationToken = default)
    {
        const string sql = SummarySelect + " WHERE (@query = '' OR c.model ILIKE @pattern OR c.license_plate ILIKE @pattern OR cu.first_name ILIKE @pattern OR cu.last_name ILIKE @pattern) AND (@status = '' OR o.status = @status) ORDER BY o.order_date DESC, o.order_id DESC";
        return GetSummariesAsync(sql, command =>
        {
            var search = query?.Trim() ?? string.Empty;
            command.Parameters.AddWithValue("query", search);
            command.Parameters.AddWithValue("pattern", $"%{search}%");
            command.Parameters.AddWithValue("status", status?.Trim() ?? string.Empty);
        }, cancellationToken);
    }

    public Task<IReadOnlyList<OrderSummary>> GetCarHistoryAsync(int carId, CancellationToken cancellationToken = default)
    {
        return GetSummariesAsync(SummarySelect + " WHERE o.car_id = @car_id ORDER BY o.order_date DESC, o.order_id DESC", command => command.Parameters.AddWithValue("car_id", carId), cancellationToken);
    }

    public async Task<OrderDetails?> GetDetailsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        const string orderSql = "SELECT o.order_id, o.car_id, concat(c.model, ', ', c.license_plate), concat_ws(' ', cu.last_name, cu.first_name), cu.phone, concat_ws(' ', e.last_name, e.first_name), o.order_date, o.deadline, o.status, o.mileage, o.discount, o.total_cost FROM orders o JOIN car c ON c.car_id = o.car_id JOIN customer cu ON cu.customer_id = c.customer_id JOIN employee e ON e.employee_id = o.employee_id WHERE o.order_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        OrderDetails? details;
        await using (var command = new NpgsqlCommand(orderSql, connection))
        {
            command.Parameters.AddWithValue("id", orderId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            details = new OrderDetails
            {
                OrderId = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                Car = reader.GetString(2),
                Customer = reader.GetString(3),
                Phone = reader.GetString(4),
                Employee = reader.GetString(5),
                OrderDate = reader.GetFieldValue<DateOnly>(6),
                Deadline = reader.IsDBNull(7) ? null : reader.GetFieldValue<DateOnly>(7),
                Status = reader.GetString(8),
                Mileage = reader.GetInt32(9),
                Discount = reader.GetDecimal(10),
                TotalCost = reader.GetDecimal(11)
            };
        }

        details.Services.AddRange(await GetLinesAsync(connection, orderId, false, cancellationToken));
        details.Parts.AddRange(await GetLinesAsync(connection, orderId, true, cancellationToken));
        return details;
    }

    public async Task<int> CreateAsync(OrderCreateForm form, int employeeId, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO orders (car_id, employee_id, order_date, deadline, status, mileage) VALUES (@car_id, @employee_id, @order_date, @deadline, 'open', @mileage) RETURNING order_id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("car_id", form.CarId);
        command.Parameters.AddWithValue("employee_id", employeeId);
        command.Parameters.AddWithValue("order_date", form.OrderDate);
        command.Parameters.AddWithValue("deadline", NpgsqlDbType.Date, (object?)form.Deadline ?? DBNull.Value);
        command.Parameters.AddWithValue("mileage", form.Mileage);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdateAsync(OrderEditForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, form.OrderId, cancellationToken);
        const string sql = "UPDATE orders SET car_id = @car_id, order_date = @order_date, deadline = @deadline, mileage = @mileage WHERE order_id = @id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("car_id", form.CarId);
        command.Parameters.AddWithValue("order_date", form.OrderDate);
        command.Parameters.AddWithValue("deadline", NpgsqlDbType.Date, (object?)form.Deadline ?? DBNull.Value);
        command.Parameters.AddWithValue("mileage", form.Mileage);
        command.Parameters.AddWithValue("id", form.OrderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureDraftAsync(connection, transaction, orderId, cancellationToken);
        await using var command = new NpgsqlCommand("DELETE FROM orders WHERE order_id = @id", connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddServiceAsync(int orderId, AddServiceForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, orderId, cancellationToken);
        const string sql = "INSERT INTO order_service (order_id, service_id, employee_id, quantity, price) SELECT @order_id, s.service_id, @employee_id, @quantity, s.price FROM service s WHERE s.service_id = @service_id ON CONFLICT (order_id, service_id) DO UPDATE SET quantity = order_service.quantity + EXCLUDED.quantity, employee_id = EXCLUDED.employee_id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("order_id", orderId);
        command.Parameters.AddWithValue("service_id", form.ServiceId);
        command.Parameters.AddWithValue("employee_id", form.EmployeeId);
        command.Parameters.AddWithValue("quantity", form.Quantity);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new KeyNotFoundException("Послугу не знайдено.");
        }

        await MarkInProgressAsync(connection, transaction, orderId, cancellationToken);
        await RecalculateAsync(connection, transaction, orderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task AddPartAsync(int orderId, AddPartForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, orderId, cancellationToken);
        const string sql = "INSERT INTO order_part (order_id, part_id, quantity, price) SELECT @order_id, p.part_id, @quantity, p.price FROM part p WHERE p.part_id = @part_id ON CONFLICT (order_id, part_id) DO UPDATE SET quantity = order_part.quantity + EXCLUDED.quantity";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("order_id", orderId);
        command.Parameters.AddWithValue("part_id", form.PartId);
        command.Parameters.AddWithValue("quantity", form.Quantity);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new KeyNotFoundException("Запчастину не знайдено.");
        }

        await MarkInProgressAsync(connection, transaction, orderId, cancellationToken);
        await RecalculateAsync(connection, transaction, orderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task RemoveServiceAsync(int orderId, int serviceId, CancellationToken cancellationToken = default)
    {
        return RemoveLineAsync(orderId, serviceId, false, cancellationToken);
    }

    public Task RemovePartAsync(int orderId, int partId, CancellationToken cancellationToken = default)
    {
        return RemoveLineAsync(orderId, partId, true, cancellationToken);
    }

    public async Task UpdateDiscountAsync(int orderId, decimal discount, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, orderId, cancellationToken);
        await using (var command = new NpgsqlCommand("UPDATE orders SET discount = @discount WHERE order_id = @id", connection, transaction))
        {
            command.Parameters.AddWithValue("discount", discount);
            command.Parameters.AddWithValue("id", orderId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await RecalculateAsync(connection, transaction, orderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CloseAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, orderId, cancellationToken);
        await using (var check = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM order_service WHERE order_id = @id)", connection, transaction))
        {
            check.Parameters.AddWithValue("id", orderId);
            if ((bool)(await check.ExecuteScalarAsync(cancellationToken) ?? false) is false)
            {
                throw new InvalidOperationException("Додайте щонайменше одну послугу перед закриттям замовлення.");
            }
        }

        await RecalculateAsync(connection, transaction, orderId, cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE orders SET status = 'closed' WHERE order_id = @id", connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<OrderSummary>> GetSummariesAsync(string sql, Action<NpgsqlCommand> addParameters, CancellationToken cancellationToken)
    {
        var orders = new List<OrderSummary>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        addParameters(command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(new OrderSummary
            {
                OrderId = reader.GetInt32(0),
                CarId = reader.GetInt32(1),
                Car = reader.GetString(2),
                Customer = reader.GetString(3),
                Employee = reader.GetString(4),
                OrderDate = reader.GetFieldValue<DateOnly>(5),
                Deadline = reader.IsDBNull(6) ? null : reader.GetFieldValue<DateOnly>(6),
                Status = reader.GetString(7),
                TotalCost = reader.GetDecimal(8)
            });
        }

        return orders;
    }

    private static async Task<List<OrderLine>> GetLinesAsync(NpgsqlConnection connection, int orderId, bool parts, CancellationToken cancellationToken)
    {
        var sql = parts
            ? "SELECT p.part_id, p.name, p.article, op.quantity, op.price, '', true FROM order_part op JOIN part p ON p.part_id = op.part_id WHERE op.order_id = @id ORDER BY p.name"
            : "SELECT s.service_id, s.name, '', os.quantity, os.price, concat_ws(' ', e.last_name, e.first_name), false FROM order_service os JOIN service s ON s.service_id = os.service_id JOIN employee e ON e.employee_id = os.employee_id WHERE os.order_id = @id ORDER BY s.name";
        var lines = new List<OrderLine>();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", orderId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            lines.Add(new OrderLine
            {
                ItemId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Article = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                Price = reader.GetDecimal(4),
                Employee = reader.GetString(5),
                IsPart = reader.GetBoolean(6)
            });
        }

        return lines;
    }

    private async Task RemoveLineAsync(int orderId, int itemId, bool part, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await EnsureOpenAsync(connection, transaction, orderId, cancellationToken);
        var table = part ? "order_part" : "order_service";
        var key = part ? "part_id" : "service_id";
        await using var command = new NpgsqlCommand($"DELETE FROM {table} WHERE order_id = @order_id AND {key} = @item_id", connection, transaction);
        command.Parameters.AddWithValue("order_id", orderId);
        command.Parameters.AddWithValue("item_id", itemId);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await RecalculateAsync(connection, transaction, orderId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureOpenAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int orderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT status FROM orders WHERE order_id = @id FOR UPDATE", connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        var status = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (status is null)
        {
            throw new KeyNotFoundException("Замовлення не знайдено.");
        }

        if (status == "closed")
        {
            throw new InvalidOperationException("Закрите замовлення не можна змінити.");
        }
    }

    private static async Task EnsureDraftAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int orderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT status FROM orders WHERE order_id = @id FOR UPDATE", connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        var status = await command.ExecuteScalarAsync(cancellationToken) as string;
        if (status is null)
        {
            throw new KeyNotFoundException("Замовлення не знайдено.");
        }

        if (status != "open")
        {
            throw new InvalidOperationException("Видалити можна лише нове замовлення без виконаних робіт.");
        }
    }

    private static async Task RecalculateAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int orderId, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE orders o SET total_cost = ROUND(((SELECT COALESCE(SUM(quantity * price), 0) FROM order_service WHERE order_id = o.order_id) + (SELECT COALESCE(SUM(quantity * price), 0) FROM order_part WHERE order_id = o.order_id)) * (100 - o.discount) / 100, 2) WHERE o.order_id = @id";
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MarkInProgressAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, int orderId, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("UPDATE orders SET status = 'in_progress' WHERE order_id = @id AND status = 'open'", connection, transaction);
        command.Parameters.AddWithValue("id", orderId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
