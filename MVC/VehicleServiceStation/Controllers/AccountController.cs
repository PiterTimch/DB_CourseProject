using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

public sealed class AccountController(
    IEmployeeRepository employees,
    IPasswordHasher<Employee> passwordHasher) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginForm());
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginForm form, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var employee = await employees.FindByLoginAsync(form.Login.Trim(), cancellationToken);
        if (employee is null || passwordHasher.VerifyHashedPassword(employee, employee.PasswordHash, form.Password) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Неправильний логін або пароль.");
            return View(form);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
            new(ClaimTypes.Name, employee.Login),
            new(ClaimTypes.GivenName, employee.FirstName),
            new(ClaimTypes.Surname, employee.LastName),
            new(ClaimTypes.Role, employee.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
