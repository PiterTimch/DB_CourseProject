using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VehicleServiceStation.Controllers;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Tests.Controllers;

public sealed class AccountControllerTests
{
    [Fact]
    public async Task Unknown_login_returns_generic_authentication_error()
    {
        var repository = new Mock<IEmployeeRepository>();
        repository.Setup(r => r.FindByLoginAsync("unknown", It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);
        var controller = new AccountController(repository.Object, Mock.Of<IPasswordHasher<Employee>>());
        ControllerTestContext.Initialize(controller);
        var result = Assert.IsType<ViewResult>(await controller.Login(new LoginForm { Login = "unknown", Password = "incorrect" }));
        Assert.Contains(result.ViewData.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "Неправильний логін або пароль.");
    }

    [Fact]
    public async Task Incorrect_password_returns_same_generic_authentication_error()
    {
        var employee = new Employee { EmployeeId = 7, Login = "known", PasswordHash = "hash" };
        var repository = new Mock<IEmployeeRepository>();
        repository.Setup(r => r.FindByLoginAsync("known", It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        var hasher = new Mock<IPasswordHasher<Employee>>();
        hasher.Setup(h => h.VerifyHashedPassword(employee, "hash", "incorrect")).Returns(PasswordVerificationResult.Failed);
        var controller = new AccountController(repository.Object, hasher.Object);
        ControllerTestContext.Initialize(controller);
        var result = Assert.IsType<ViewResult>(await controller.Login(new LoginForm { Login = "known", Password = "incorrect" }));
        Assert.Contains(result.ViewData.ModelState[string.Empty]!.Errors, error => error.ErrorMessage == "Неправильний логін або пароль.");
    }

    [Fact]
    public async Task Successful_login_does_not_redirect_to_external_url()
    {
        var employee = new Employee { EmployeeId = 7, Login = "known", PasswordHash = "hash", Role = "manager" };
        var repository = new Mock<IEmployeeRepository>();
        repository.Setup(r => r.FindByLoginAsync("known", It.IsAny<CancellationToken>())).ReturnsAsync(employee);
        var hasher = new Mock<IPasswordHasher<Employee>>();
        hasher.Setup(h => h.VerifyHashedPassword(employee, "hash", "correct")).Returns(PasswordVerificationResult.Success);
        var controller = new AccountController(repository.Object, hasher.Object);
        ControllerTestContext.Initialize(controller);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(a => a.SignInAsync(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>(), It.IsAny<string?>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties?>())).Returns(Task.CompletedTask);
        controller.HttpContext.RequestServices = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(url => url.IsLocalUrl("https://outside.example")).Returns(false);
        controller.Url = urlHelper.Object;
        var result = Assert.IsType<RedirectToActionResult>(await controller.Login(new LoginForm { Login = "known", Password = "correct" }, "https://outside.example"));
        Assert.Equal("Index", result.ActionName);
        Assert.Equal("Home", result.ControllerName);
    }
}
