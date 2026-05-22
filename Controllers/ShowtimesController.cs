using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class ShowtimesController : Controller
{
    private readonly ApiService _api;
    public ShowtimesController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public async Task<IActionResult> Index(int page = 1, int? eventId = null)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.GetShowtimesAsync(page, eventId);
        ViewBag.EventId = eventId;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (RequireAuth() is { } r) return r;
        var events = await _api.GetEventsAsync(1, true);
        return View(new ShowtimeFormViewModel { Events = events?.Items ?? new() });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateShowtimeRequest request, string seatLayout)
    {
        if (RequireAuth() is { } r) return r;

        // Parse seat layout from JSON string sent by the form
        if (!string.IsNullOrEmpty(seatLayout))
        {
            try
            {
                var rows = System.Text.Json.JsonSerializer.Deserialize<List<SeatRowRequest>>(
                    seatLayout,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (rows != null) request.SeatLayout = rows;
            }
            catch { /* ignore parse errors */ }
        }

        var result = await _api.CreateShowtimeAsync(request);
        if (result == null)
        {
            ViewBag.Error = "No se pudo crear la función.";
            var events = await _api.GetEventsAsync(1, true);
            return View(new ShowtimeFormViewModel { Request = request, Events = events?.Items ?? new() });
        }
        TempData["Success"] = "Función creada exitosamente.";
        return RedirectToAction(nameof(Index));
    }
}
