using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.Staff)]
public sealed class OrdersController(
    IOrderRepository orders,
    FormOptionsService options) : DataErrorController
{
    public async Task<IActionResult> Index(string? query, string? status, CancellationToken cancellationToken)
    {
        ViewBag.Query = query;
        ViewBag.Status = status;
        return View(await orders.GetAllAsync(query, status, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadCarsAsync(cancellationToken);
        return View(new OrderCreateForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderCreateForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCarsAsync(cancellationToken);
            return View(form);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var employeeId))
        {
            return Forbid();
        }

        try
        {
            var orderId = await orders.CreateAsync(form, employeeId, cancellationToken);
            TempData["Notice"] = "Замовлення створено.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            await LoadCarsAsync(cancellationToken);
            return View(form);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var details = await orders.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        await LoadDetailOptionsAsync(cancellationToken);
        return View(details);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var details = await orders.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        await LoadCarsAsync(cancellationToken);
        return View(new OrderEditForm
        {
            OrderId = details.OrderId,
            CarId = details.CarId,
            OrderDate = details.OrderDate,
            Deadline = details.Deadline,
            Mileage = details.Mileage
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(OrderEditForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadCarsAsync(cancellationToken);
            return View(form);
        }

        try
        {
            await orders.UpdateAsync(form, cancellationToken);
            TempData["Notice"] = "Замовлення оновлено.";
            return RedirectToAction(nameof(Details), new { id = form.OrderId });
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            await LoadCarsAsync(cancellationToken);
            return View(form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddService(int id, AddServiceForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvalidDetailsAsync(id, cancellationToken);
        }

        try
        {
            await orders.AddServiceAsync(id, form, cancellationToken);
            TempData["Notice"] = "Послугу додано до замовлення.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            return await InvalidDetailsAsync(id, cancellationToken);
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddPart(int id, AddPartForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvalidDetailsAsync(id, cancellationToken);
        }

        try
        {
            await orders.AddPartAsync(id, form, cancellationToken);
            TempData["Notice"] = "Запчастину додано до замовлення.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            return await InvalidDetailsAsync(id, cancellationToken);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveService(int id, int serviceId, CancellationToken cancellationToken)
    {
        return await ChangeLineAsync(id, () => orders.RemoveServiceAsync(id, serviceId, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> RemovePart(int id, int partId, CancellationToken cancellationToken)
    {
        return await ChangeLineAsync(id, () => orders.RemovePartAsync(id, partId, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Discount(int id, DiscountForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await InvalidDetailsAsync(id, cancellationToken);
        }

        try
        {
            await orders.UpdateDiscountAsync(id, form.Discount, cancellationToken);
            TempData["Notice"] = "Знижку застосовано.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            AddDataError(exception);
            return await InvalidDetailsAsync(id, cancellationToken);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Close(int id, CancellationToken cancellationToken)
    {
        try
        {
            await orders.CloseAsync(id, cancellationToken);
            TempData["Notice"] = "Замовлення закрито. Підсумкову вартість зафіксовано.";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            await orders.DeleteAsync(id, cancellationToken);
            TempData["Notice"] = "Чернетку замовлення видалено.";
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> ChangeLineAsync(int id, Func<Task> change, CancellationToken cancellationToken)
    {
        try
        {
            await change();
            TempData["Notice"] = "Деталі замовлення оновлено.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception exception)
        {
            TempData["Error"] = exception.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task<IActionResult> InvalidDetailsAsync(int id, CancellationToken cancellationToken)
    {
        var details = await orders.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        await LoadDetailOptionsAsync(cancellationToken);
        return View(nameof(Details), details);
    }

    private async Task LoadCarsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Cars = await options.CarsAsync(cancellationToken);
    }

    private async Task LoadDetailOptionsAsync(CancellationToken cancellationToken)
    {
        ViewBag.Services = await options.ServicesAsync(cancellationToken);
        ViewBag.Parts = await options.PartsAsync(cancellationToken);
        ViewBag.Employees = await options.EmployeesAsync(cancellationToken);
    }
}
