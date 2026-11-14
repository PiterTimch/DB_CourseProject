using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.Admin)]
public sealed class EmployeesController(
    IEmployeeRepository employees,
    IPasswordHasher<Employee> passwordHasher) : DataErrorController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await employees.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new EmployeeCreateForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(EmployeeCreateForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var employee = new Employee
        {
            FirstName = form.FirstName.Trim(),
            LastName = form.LastName.Trim(),
            Role = form.Role,
            Login = form.Login.Trim()
        };
        employee.PasswordHash = passwordHasher.HashPassword(employee, form.Password);

        try
        {
            await employees.CreateAsync(employee, cancellationToken);
            TempData["Notice"] = "Обліковий запис створено.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            return View(form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var employee = await employees.GetAsync(id, cancellationToken);
        if (employee is null)
        {
            return NotFound();
        }

        return View(new EmployeeEditForm
        {
            EmployeeId = employee.EmployeeId,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Role = employee.Role,
            Login = employee.Login
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EmployeeEditForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var current = await employees.GetAsync(form.EmployeeId, cancellationToken);
        if (current is null)
        {
            return NotFound();
        }

        var employee = new Employee
        {
            EmployeeId = form.EmployeeId,
            FirstName = form.FirstName.Trim(),
            LastName = form.LastName.Trim(),
            Role = form.Role,
            Login = form.Login.Trim(),
            PasswordHash = current.PasswordHash
        };
        var passwordHash = string.IsNullOrWhiteSpace(form.Password)
            ? null
            : passwordHasher.HashPassword(employee, form.Password);

        try
        {
            await employees.UpdateAsync(employee, passwordHash, cancellationToken);
            TempData["Notice"] = "Обліковий запис оновлено.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentId) && currentId == id)
        {
            TempData["Error"] = "Не можна видалити власний обліковий запис під час активного сеансу.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await employees.DeleteAsync(id, cancellationToken);
            TempData["Notice"] = "Обліковий запис видалено.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Працівник використовується в замовленнях і не може бути видалений.";
        }

        return RedirectToAction(nameof(Index));
    }
}
