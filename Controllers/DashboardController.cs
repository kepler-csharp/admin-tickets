using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class DashboardController : Controller
{
    private readonly ApiService _api;
    public DashboardController(ApiService api) => _api = api;

    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("AccessToken") == null)
            return RedirectToAction("Login", "Auth");

        var data = await _api.GetDashboardAsync();
        return View(data);
    }
}
