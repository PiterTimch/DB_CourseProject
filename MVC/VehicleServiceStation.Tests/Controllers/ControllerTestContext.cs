using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace VehicleServiceStation.Tests.Controllers;

internal static class ControllerTestContext
{
    public static void Initialize(Controller controller, ClaimsPrincipal? user = null)
    {
        var http = new DefaultHttpContext { User = user ?? new ClaimsPrincipal(new ClaimsIdentity()) };
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(http, new TestTempDataProvider());
    }

    public static ClaimsPrincipal UserWithId(string? id) => new(new ClaimsIdentity(
        id is null ? [] : [new Claim(ClaimTypes.NameIdentifier, id)], "test"));

    private sealed class TestTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
