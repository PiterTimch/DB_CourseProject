using Npgsql;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Data.Repositories;

public sealed class CatalogRepository(NpgsqlDataSource dataSource) : ICatalogRepository
{
    public Task<IReadOnlyList<CatalogItem>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        return GetItemsAsync("service", "service_id", false, cancellationToken);
    }

    public Task<IReadOnlyList<CatalogItem>> GetPartsAsync(CancellationToken cancellationToken = default)
    {
        return GetItemsAsync("part", "part_id", true, cancellationToken);
    }

    public async Task<ServiceForm?> GetServiceFormAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT service_id, name, price FROM service WHERE service_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new ServiceForm { Id = reader.GetInt32(0), Name = reader.GetString(1), Price = reader.GetDecimal(2) }
            : null;
    }

    public async Task<int> CreateServiceAsync(ServiceForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO service (name, price) VALUES (@name, @price) RETURNING service_id", connection);
        command.Parameters.AddWithValue("name", form.Name.Trim());
        command.Parameters.AddWithValue("price", form.Price);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdateServiceAsync(ServiceForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE service SET name = @name, price = @price WHERE service_id = @id", connection);
        command.Parameters.AddWithValue("name", form.Name.Trim());
        command.Parameters.AddWithValue("price", form.Price);
        command.Parameters.AddWithValue("id", form.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync("service", "service_id", id, cancellationToken);
    }

    public async Task<PartForm?> GetPartFormAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT part_id, name, article, price FROM part WHERE part_id = @id";
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new PartForm { Id = reader.GetInt32(0), Name = reader.GetString(1), Article = reader.GetString(2), Price = reader.GetDecimal(3) }
            : null;
    }

    public async Task<int> CreatePartAsync(PartForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("INSERT INTO part (name, article, price) VALUES (@name, @article, @price) RETURNING part_id", connection);
        AddPartParameters(command, form);
        return (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
    }

    public async Task UpdatePartAsync(PartForm form, CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE part SET name = @name, article = @article, price = @price WHERE part_id = @id", connection);
        AddPartParameters(command, form);
        command.Parameters.AddWithValue("id", form.Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public Task DeletePartAsync(int id, CancellationToken cancellationToken = default)
    {
        return DeleteAsync("part", "part_id", id, cancellationToken);
    }

    private async Task<IReadOnlyList<CatalogItem>> GetItemsAsync(string table, string idColumn, bool isPart, CancellationToken cancellationToken)
    {
        var article = isPart ? ", article" : string.Empty;
        var sql = $"SELECT {idColumn}, name{article}, price FROM {table} ORDER BY name";
        var items = new List<CatalogItem>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new CatalogItem
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Article = isPart ? reader.GetString(2) : string.Empty,
                Price = reader.GetDecimal(isPart ? 3 : 2),
                IsPart = isPart
            });
        }

        return items;
    }

    private async Task DeleteAsync(string table, string idColumn, int id, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"DELETE FROM {table} WHERE {idColumn} = @id", connection);
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddPartParameters(NpgsqlCommand command, PartForm form)
    {
        command.Parameters.AddWithValue("name", form.Name.Trim());
        command.Parameters.AddWithValue("article", form.Article.Trim().ToUpperInvariant());
        command.Parameters.AddWithValue("price", form.Price);
    }
}
