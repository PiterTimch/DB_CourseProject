using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleServiceStation.Infrastructure;
using VehicleServiceStation.Models;

namespace VehicleServiceStation.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        [Authorize(Roles = AccessRoles.Staff + "," + AccessRoles.Guest + "," + AccessRoles.Director)]
        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
        }
    }
}
