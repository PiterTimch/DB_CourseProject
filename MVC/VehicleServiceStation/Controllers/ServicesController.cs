using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.CatalogManagers)]
public sealed class ServicesController(ICatalogRepository catalog) : DataErrorController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await catalog.GetServicesAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ServiceForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ServiceForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await catalog.CreateServiceAsync(form, cancellationToken);
            TempData["Notice"] = "Послугу додано.";
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
        var form = await catalog.GetServiceFormAsync(id, cancellationToken);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ServiceForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await catalog.UpdateServiceAsync(form, cancellationToken);
            TempData["Notice"] = "Послугу оновлено.";
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
            await catalog.DeleteServiceAsync(id, cancellationToken);
            TempData["Notice"] = "Послугу видалено.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Послуга використовується в замовленнях і не може бути видалена.";
        }

        return RedirectToAction(nameof(Index));
    }
}
