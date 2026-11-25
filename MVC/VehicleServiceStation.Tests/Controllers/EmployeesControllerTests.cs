using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using VehicleServiceStation.Controllers;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models;

namespace VehicleServiceStation.Tests.Controllers;

public sealed class EmployeesControllerTests
{
    [Fact]
    public async Task Delete_does_not_delete_current_user()
    {
        var employees = new Mock<IEmployeeRepository>();
        var controller = new EmployeesController(employees.Object, Mock.Of<IPasswordHasher<Employee>>());
        ControllerTestContext.Initialize(controller, ControllerTestContext.UserWithId("42"));
        var result = Assert.IsType<RedirectToActionResult>(await controller.Delete(42, CancellationToken.None));
        Assert.Equal(nameof(EmployeesController.Index), result.ActionName);
        Assert.Equal("Не можна видалити власний обліковий запис під час активного сеансу.", controller.TempData["Error"]);
        employees.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
