using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.Staff)]
public sealed class CustomersController(ICustomerRepository customers) : DataErrorController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await customers.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CustomerForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await customers.CreateAsync(form, cancellationToken);
            TempData["Notice"] = "Клієнта додано.";
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
        var form = await customers.GetFormAsync(id, cancellationToken);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CustomerForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await customers.UpdateAsync(form, cancellationToken);
            TempData["Notice"] = "Дані клієнта оновлено.";
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
        try
        {
            await customers.DeleteAsync(id, cancellationToken);
            TempData["Notice"] = "Клієнта видалено.";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.ForeignKeyViolation }
                ? "У клієнта є зареєстровані авто. Спочатку видаліть їх або змініть власника."
                : "Не вдалося видалити клієнта.";
        }

        return RedirectToAction(nameof(Index));
    }
}
