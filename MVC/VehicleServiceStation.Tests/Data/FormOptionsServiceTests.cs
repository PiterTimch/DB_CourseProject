using Moq;
using VehicleServiceStation.Data;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Models;

namespace VehicleServiceStation.Tests.Data;

public sealed class FormOptionsServiceTests
{
    [Fact]
    public async Task Customers_cars_employees_services_and_parts_are_mapped_to_select_options()
    {
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Customer>)[new Customer { CustomerId = 3, LastName = "Петренко", FirstName = "Іван", Phone = "+380501234567" }]);
        var cars = new Mock<ICarRepository>();
        cars.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Car>)[new Car { CarId = 4, Model = "Honda Civic", LicensePlate = "AA1234BB", CustomerName = "Іван Петренко" }]);
        var employees = new Mock<IEmployeeRepository>();
        employees.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<Employee>)[
            new Employee { EmployeeId = 5, FirstName = "Олег", LastName = "Майстер", Role = "manager" },
            new Employee { EmployeeId = 6, FirstName = "Гість", LastName = "Користувач", Role = "guest" }]);
        var catalog = new Mock<ICatalogRepository>();
        catalog.Setup(r => r.GetServicesAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<CatalogItem>)[new CatalogItem { Id = 7, Name = "Діагностика", Price = 500 }]);
        catalog.Setup(r => r.GetPartsAsync(It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<CatalogItem>)[new CatalogItem { Id = 8, Name = "Фільтр", Article = "F-1", Price = 200, IsPart = true }]);
        var service = new FormOptionsService(customers.Object, cars.Object, employees.Object, catalog.Object);

        Assert.Equal("3", Assert.Single(await service.CustomersAsync()).Value);
        Assert.Equal("4", Assert.Single(await service.CarsAsync()).Value);
        var employeeOptions = await service.EmployeesAsync();
        Assert.Equal("5", Assert.Single(employeeOptions).Value);
        Assert.Equal("7", Assert.Single(await service.ServicesAsync()).Value);
        Assert.Equal("8", Assert.Single(await service.PartsAsync()).Value);
    }
}
