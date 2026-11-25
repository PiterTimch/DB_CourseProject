using Microsoft.AspNetCore.Mvc;
using Moq;
using VehicleServiceStation.Controllers;
using VehicleServiceStation.Data;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Tests.Controllers;

public sealed class OrdersControllerTests
{
    [Fact]
    public async Task Create_forbids_when_employee_claim_is_not_an_integer()
    {
        var orders = new Mock<IOrderRepository>();
        var options = new FormOptionsService(Mock.Of<ICustomerRepository>(), Mock.Of<ICarRepository>(), Mock.Of<IEmployeeRepository>(), Mock.Of<ICatalogRepository>());
        var controller = new OrdersController(orders.Object, options);
        ControllerTestContext.Initialize(controller, ControllerTestContext.UserWithId("invalid"));
        var result = await controller.Create(new OrderCreateForm { CarId = 1 }, CancellationToken.None);
        Assert.IsType<ForbidResult>(result);
        orders.Verify(r => r.CreateAsync(It.IsAny<OrderCreateForm>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
