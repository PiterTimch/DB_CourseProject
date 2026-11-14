using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.CatalogManagers)]
public sealed class PartsController(ICatalogRepository catalog) : DataErrorController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await catalog.GetPartsAsync(cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new PartForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(PartForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await catalog.CreatePartAsync(form, cancellationToken);
            TempData["Notice"] = "Запчастину додано.";
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
        var form = await catalog.GetPartFormAsync(id, cancellationToken);
        return form is null ? NotFound() : View(form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(PartForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        try
        {
            await catalog.UpdatePartAsync(form, cancellationToken);
            TempData["Notice"] = "Запчастину оновлено.";
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
            await catalog.DeletePartAsync(id, cancellationToken);
            TempData["Notice"] = "Запчастину видалено.";
        }
        catch (Exception)
        {
            TempData["Error"] = "Запчастина використовується в замовленнях і не може бути видалена.";
        }

        return RedirectToAction(nameof(Index));
    }
}
