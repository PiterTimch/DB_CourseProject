using Npgsql;
using NpgsqlTypes;

namespace VehicleServiceStation.Infrastructure;

public sealed class DatabaseBootstrapper(
    DatabaseSettings settings,
    NpgsqlDataSource dataSource,
    CommandFileRunner commandRunner,
    ILogger<DatabaseBootstrapper> logger)
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await Gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureDatabaseExistsAsync(cancellationToken);
            await EnsureSchemaExistsAsync(cancellationToken);
            await EnsureApplicationPermissionsAsync(cancellationToken);
            await EnsureSeedDataAsync(cancellationToken);
            await EnsureInitialAdministratorAsync(cancellationToken);
            await EnsureDemoPasswordsAsync(cancellationToken);
            await VerifyApplicationConnectionAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogCritical(exception, "The application could not initialize or connect to its database.");
            throw new InvalidOperationException("Не вдалося підготувати базу даних. Перевірте доступність PostgreSQL, значення в .env та наявність psql у PATH.", exception);
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState is "3D000" or "28P01")
        {
            if (string.IsNullOrWhiteSpace(settings.AdminUser) || string.IsNullOrWhiteSpace(settings.AdminPassword) || string.IsNullOrWhiteSpace(settings.MaintenanceDatabase))
            {
                throw new InvalidOperationException("Set PG_ADMIN_USER, PG_ADMIN_PASSWORD and PGMAINTENANCE_DB in .env so the application can create the database or initialize its application account.", exception);
            }

            logger.LogInformation("Database or application account needs initialization. Running database creation command.");
            await commandRunner.RunAsync("create-database", cancellationToken);
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        }
    }

    private async Task EnsureSchemaExistsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT to_regclass('public.customer') IS NOT NULL AND to_regclass('public.employee') IS NOT NULL AND to_regclass('public.car') IS NOT NULL AND to_regclass('public.orders') IS NOT NULL AND to_regclass('public.service') IS NOT NULL AND to_regclass('public.part') IS NOT NULL AND to_regclass('public.order_service') IS NOT NULL AND to_regclass('public.order_part') IS NOT NULL", connection);
        var exists = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        if (!exists)
        {
            logger.LogInformation("Database schema is incomplete. Applying schema.sql.");
            await commandRunner.RunSqlFileAsync("schema.sql", settings, cancellationToken);
        }
    }

    private async Task EnsureSeedDataAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM customer UNION ALL SELECT 1 FROM employee UNION ALL SELECT 1 FROM car UNION ALL SELECT 1 FROM orders UNION ALL SELECT 1 FROM service UNION ALL SELECT 1 FROM part UNION ALL SELECT 1 FROM order_service UNION ALL SELECT 1 FROM order_part)", connection);
        var hasData = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        if (!hasData)
        {
            logger.LogInformation("Database has no records. Running database population command.");
            await commandRunner.RunAsync("populate-database", cancellationToken);
        }
    }

    private async Task EnsureApplicationPermissionsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT has_table_privilege(current_user, 'customer', 'SELECT')", connection);
        var hasPermission = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        if (hasPermission)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.AdminUser) || string.IsNullOrWhiteSpace(settings.AdminPassword) || string.IsNullOrWhiteSpace(settings.MaintenanceDatabase))
        {
            throw new InvalidOperationException("The application database user lacks table permissions. Set administrator credentials in .env to initialize them.");
        }

        logger.LogInformation("Application database permissions are missing. Running database initialization command.");
        await commandRunner.RunAsync("create-database", cancellationToken);
    }

    private async Task EnsureInitialAdministratorAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM employee WHERE role = 'admin')", connection);
        var hasAdministrator = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        if (hasAdministrator)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.InitialAdminLogin) || string.IsNullOrWhiteSpace(settings.InitialAdminPassword))
        {
            throw new InvalidOperationException("Set INITIAL_ADMIN_LOGIN and INITIAL_ADMIN_PASSWORD to create the first administrator.");
        }

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Models.Employee>();
        var administrator = new Models.Employee
        {
            FirstName = "System",
            LastName = "Administrator",
            Role = "admin",
            Login = settings.InitialAdminLogin
        };
        administrator.PasswordHash = hasher.HashPassword(administrator, settings.InitialAdminPassword);

        await using var insert = new NpgsqlCommand("INSERT INTO employee (first_name, last_name, role, login, password_hash) VALUES (@first_name, @last_name, 'admin', @login, @password_hash) ON CONFLICT (login) DO NOTHING", connection);
        insert.Parameters.AddWithValue("first_name", administrator.FirstName);
        insert.Parameters.AddWithValue("last_name", administrator.LastName);
        insert.Parameters.AddWithValue("login", administrator.Login);
        insert.Parameters.AddWithValue("password_hash", administrator.PasswordHash);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureDemoPasswordsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var accounts = new[]
        {
            (Login: "demo.manager", Password: settings.DemoManagerPassword),
            (Login: "demo.mechanic", Password: settings.DemoMechanicPassword),
            (Login: "demo.director", Password: settings.DemoDirectorPassword)
        };
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Models.Employee>();

        foreach (var account in accounts)
        {
            if (string.IsNullOrWhiteSpace(account.Password))
            {
                continue;
            }

            var employee = new Models.Employee { Login = account.Login };
            var passwordHash = hasher.HashPassword(employee, account.Password);
            await using var command = new NpgsqlCommand("UPDATE employee SET password_hash = @password_hash WHERE login = @login AND password_hash = '!'", connection);
            command.Parameters.AddWithValue("login", account.Login);
            command.Parameters.AddWithValue("password_hash", passwordHash);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private async Task VerifyApplicationConnectionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
