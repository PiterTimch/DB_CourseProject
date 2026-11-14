using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.Staff)]
public sealed class CarsController(ICarRepository cars, FormOptionsService options) : DataErrorController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await cars.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadCustomersAsync(cancellationToken);
        return View(new CarForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CarForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync(cancellationToken);
            return View(form);
        }

        try
        {
            await cars.CreateAsync(form, cancellationToken);
            TempData["Notice"] = "Автомобіль зареєстровано.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            await LoadCustomersAsync(cancellationToken);
            return View(form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var form = await cars.GetFormAsync(id, cancellationToken);
        if (form is null)
        {
            return NotFound();
        }

        await LoadCustomersAsync(cancellationToken);
        return View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CarForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync(cancellationToken);
            return View(form);
        }

        try
        {
            await cars.UpdateAsync(form, cancellationToken);
            TempData["Notice"] = "Дані автомобіля оновлено.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            await LoadCustomersAsync(cancellationToken);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await cars.DeleteAsync(id, cancellationToken);
            TempData["Notice"] = "Автомобіль видалено.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Автомобіль має історію замовлень і не може бути видалений.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCustomersAsync(CancellationToken cancellationToken)
    {
        ViewBag.Customers = await options.CustomersAsync(cancellationToken);
    }
}
