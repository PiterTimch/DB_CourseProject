using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using VehicleServiceStation.Controllers;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Models;

namespace VehicleServiceStation.Tests.Controllers;

public sealed class HistoryControllerTests
{
    [Fact]
    public async Task Unknown_car_adds_validation_error_and_does_not_query_orders()
    {
        var cars = new Mock<ICarRepository>();
        cars.Setup(r => r.GetAsync(22, It.IsAny<CancellationToken>())).ReturnsAsync((Car?)null);
        var orders = new Mock<IOrderRepository>();
        var controller = new HistoryController(cars.Object, orders.Object);
        ControllerTestContext.Initialize(controller);
        controller.Request.QueryString = new QueryString("?CarId=22");
        var result = Assert.IsType<ViewResult>(await controller.Index(new Models.Forms.HistoryLookupForm { CarId = 22 }, CancellationToken.None));
        Assert.Contains(result.ViewData.ModelState[nameof(Models.Forms.HistoryLookupForm.CarId)]!.Errors,
            error => error.ErrorMessage == "Автомобіль із таким системним ідентифікатором не знайдено.");
        orders.Verify(r => r.GetCarHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
