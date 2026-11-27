using Npgsql;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class RepositoryIntegrationTests
{
    [Fact]
    public async Task Customer_create_and_read_round_trip_preserves_fields()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var customers = new CustomerRepository(dataSource);
        var form = Customer("Створений");
        var id = await customers.CreateAsync(form);
        try
        {
            var saved = await customers.GetFormAsync(id);
            Assert.NotNull(saved);
            Assert.Equal(form.FirstName, saved.FirstName);
            Assert.Equal(form.LastName, saved.LastName);
            Assert.Equal(form.Phone, saved.Phone);
        }
        finally
        {
            await customers.DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Customer_update_is_visible_in_the_joined_customer_list()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var customers = new CustomerRepository(dataSource);
        var form = Customer("До оновлення");
        var id = await customers.CreateAsync(form);
        try
        {
            form.CustomerId = id;
            form.FirstName = "Після оновлення";
            form.LastName = "Інтеграційний запис";
            await customers.UpdateAsync(form);

            var saved = await customers.GetAllAsync();
            var customer = Assert.Single(saved, item => item.CustomerId == id);
            Assert.Equal(form.FirstName, customer.FirstName);
            Assert.Equal(form.LastName, customer.LastName);
            Assert.Equal(0, customer.CarCount);
        }
        finally
        {
            await customers.DeleteAsync(id);
        }
    }

    [Fact]
    public async Task Car_create_joins_back_to_its_customer()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var customers = new CustomerRepository(dataSource);
        var cars = new CarRepository(dataSource);
        var customer = Customer("Власник авто");
        var customerId = await customers.CreateAsync(customer);
        var vin = $"IT{Guid.NewGuid():N}"[..17].ToUpperInvariant();
        var plate = $"IT{Random.Shared.Next(1000, 9999)}AA";
        var carId = 0;
        try
        {
            carId = await cars.CreateAsync(new CarForm
            {
                CustomerId = customerId,
                Vin = vin,
                LicensePlate = plate,
                Model = "Integration Test Car"
            });

            var car = await cars.GetAsync(carId);
            Assert.NotNull(car);
            Assert.Equal(customerId, car.CustomerId);
            Assert.Equal("Integration Test Car", car.Model);
            Assert.Contains(customer.LastName, car.CustomerName);
        }
        finally
        {
            if (carId != 0)
            {
                await cars.DeleteAsync(carId);
            }

            await customers.DeleteAsync(customerId);
        }
    }

    [Fact]
    public async Task Service_catalog_create_update_and_list_share_database_state()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var catalog = new CatalogRepository(dataSource);
        var form = new ServiceForm { Name = $"IT service {Guid.NewGuid():N}", Price = 123.45m };
        var id = await catalog.CreateServiceAsync(form);
        try
        {
            var saved = await catalog.GetServiceFormAsync(id);
            Assert.NotNull(saved);
            Assert.Equal(123.45m, saved.Price);

            saved.Price = 234.56m;
            await catalog.UpdateServiceAsync(saved);
            var listed = Assert.Single(await catalog.GetServicesAsync(), item => item.Id == id);
            Assert.Equal(234.56m, listed.Price);
        }
        finally
        {
            await catalog.DeleteServiceAsync(id);
        }
    }

    [Fact]
    public async Task Part_catalog_normalizes_article_and_persists_price_changes()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var catalog = new CatalogRepository(dataSource);
        var article = $"it-{Guid.NewGuid():N}"[..24];
        var id = await catalog.CreatePartAsync(new PartForm
        {
            Name = "Integration Test Part",
            Article = article,
            Price = 10.25m
        });
        try
        {
            var saved = await catalog.GetPartFormAsync(id);
            Assert.NotNull(saved);
            Assert.Equal(article.ToUpperInvariant(), saved.Article);

            saved.Price = 11.75m;
            await catalog.UpdatePartAsync(saved);
            var listed = Assert.Single(await catalog.GetPartsAsync(), item => item.Id == id);
            Assert.Equal(11.75m, listed.Price);
            Assert.True(listed.IsPart);
        }
        finally
        {
            await catalog.DeletePartAsync(id);
        }
    }

    [Fact]
    public async Task Employee_create_and_update_are_visible_by_login()
    {
        await using var dataSource = await OpenDataSourceAsync();
        var employees = new EmployeeRepository(dataSource);
        var login = $"it.{Guid.NewGuid():N}";
        var id = await employees.CreateAsync(new Employee
        {
            FirstName = "Інтеграційний",
            LastName = "Працівник",
            Role = "manager",
            Login = login,
            PasswordHash = "integration-test-hash"
        });
        try
        {
            var saved = await employees.FindByLoginAsync(login);
            Assert.NotNull(saved);
            Assert.Equal(id, saved.EmployeeId);

            await employees.UpdateAsync(new Employee
            {
                EmployeeId = id,
                FirstName = "Оновлений",
                LastName = "Працівник",
                Role = "manager",
                Login = login,
                PasswordHash = saved.PasswordHash
            }, null);

            var updated = await employees.FindByLoginAsync(login);
            Assert.NotNull(updated);
            Assert.Equal("Оновлений", updated.FirstName);
        }
        finally
        {
            await employees.DeleteAsync(id);
        }
    }

    private static async Task<NpgsqlDataSource> OpenDataSourceAsync()
    {
        EnvironmentFile.Load();
        return NpgsqlDataSource.Create(DatabaseSettings.Load().BuildApplicationConnectionString());
    }

    private static CustomerForm Customer(string firstName) => new()
    {
        CustomerType = "individual",
        FirstName = firstName,
        LastName = $"ІТ {Guid.NewGuid():N}"[..15],
        Phone = $"+380{Random.Shared.Next(50_000_000, 99_999_999)}"
    };
}
