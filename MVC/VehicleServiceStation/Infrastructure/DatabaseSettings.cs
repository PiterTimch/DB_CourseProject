using Npgsql;

namespace VehicleServiceStation.Infrastructure;

public sealed class DatabaseSettings
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Database { get; init; } = string.Empty;
    public string MaintenanceDatabase { get; init; } = string.Empty;
    public string AdminUser { get; init; } = string.Empty;
    public string AdminPassword { get; init; } = string.Empty;
    public string AppUser { get; init; } = string.Empty;
    public string AppPassword { get; init; } = string.Empty;
    public string InitialAdminLogin { get; init; } = string.Empty;
    public string InitialAdminPassword { get; init; } = string.Empty;
    public string DemoManagerPassword { get; init; } = string.Empty;
    public string DemoMechanicPassword { get; init; } = string.Empty;
    public string DemoDirectorPassword { get; init; } = string.Empty;

    public static DatabaseSettings Load()
    {
        return new DatabaseSettings
        {
            Host = Required("PGHOST"),
            Port = int.TryParse(Required("PGPORT"), out var port) && port is > 0 and < 65536
                ? port
                : throw new InvalidOperationException("PGPORT must be a valid TCP port."),
            Database = Required("PGDATABASE"),
            MaintenanceDatabase = Environment.GetEnvironmentVariable("PGMAINTENANCE_DB") ?? string.Empty,
            AdminUser = Environment.GetEnvironmentVariable("PG_ADMIN_USER") ?? string.Empty,
            AdminPassword = Environment.GetEnvironmentVariable("PG_ADMIN_PASSWORD") ?? string.Empty,
            AppUser = Required("APP_DB_USER"),
            AppPassword = Required("APP_DB_PASSWORD"),
            InitialAdminLogin = Environment.GetEnvironmentVariable("INITIAL_ADMIN_LOGIN") ?? string.Empty,
            InitialAdminPassword = Environment.GetEnvironmentVariable("INITIAL_ADMIN_PASSWORD") ?? string.Empty,
            DemoManagerPassword = Environment.GetEnvironmentVariable("DEMO_MANAGER_PASSWORD") ?? string.Empty,
            DemoMechanicPassword = Environment.GetEnvironmentVariable("DEMO_MECHANIC_PASSWORD") ?? string.Empty,
            DemoDirectorPassword = Environment.GetEnvironmentVariable("DEMO_DIRECTOR_PASSWORD") ?? string.Empty
        };
    }

    public string BuildAdminConnectionString(string database)
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = database,
            Username = AdminUser,
            Password = AdminPassword,
            Timeout = 5,
            CommandTimeout = 15,
            ApplicationName = "VehicleServiceStation Bootstrap"
        }.ConnectionString;
    }

    public string BuildApplicationConnectionString()
    {
        return new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = AppUser,
            Password = AppPassword,
            Timeout = 5,
            CommandTimeout = 15,
            ApplicationName = "VehicleServiceStation"
        }.ConnectionString;
    }

    private static string Required(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Set {key} in the environment or local .env file.")
            : value;
    }
}
