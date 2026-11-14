using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Data.Repositories;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models;
using VehicleServiceStation.Models.Forms;

namespace VehicleServiceStation.Controllers;

[Authorize(Roles = AccessRoles.Staff + "," + AccessRoles.Guest)]
public sealed class HistoryController(ICarRepository cars, IOrderRepository orders) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(HistoryLookupForm form, CancellationToken cancellationToken)
    {
        if (!Request.Query.ContainsKey(nameof(form.CarId)))
        {
            return View(new HistoryPage());
        }

        if (!ModelState.IsValid)
        {
            return View(new HistoryPage { CarId = form.CarId });
        }

        var car = await cars.GetAsync(form.CarId, cancellationToken);
        if (car is null)
        {
            ModelState.AddModelError(nameof(form.CarId), "Автомобіль із таким системним ідентифікатором не знайдено.");
            return View(new HistoryPage { CarId = form.CarId });
        }

        return View(new HistoryPage
        {
            CarId = form.CarId,
            Car = car,
            Orders = await orders.GetCarHistoryAsync(form.CarId, cancellationToken)
        });
    }
}
