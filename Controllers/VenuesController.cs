using admin_tickets.Models;
using admin_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin_tickets.Controllers;

public class VenuesController : Controller
{
    private readonly ApiService _api;
    public VenuesController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public async Task<IActionResult> Index(int page = 1)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.GetVenuesAsync(page);
        return View(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVenueRequest request)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.CreateVenueAsync(request);
        TempData[result != null ? "Success" : "Error"] =
            result != null ? $"Sede \"{result.Name}\" creada." : "No se pudo crear la sede.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireAuth() is { } r) return r;
        await _api.DeleteVenueAsync(id);
        TempData["Success"] = "Sede eliminada.";
        return RedirectToAction(nameof(Index));
    }
}
