using admin_tickets.Models;
using admin_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin_tickets.Controllers;

public class EventsController : Controller
{
    private readonly ApiService _api;
    public EventsController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public async Task<IActionResult> Index(int page = 1, string? filter = null)
    {
        if (RequireAuth() is { } r) return r;
        bool? isActive = filter switch { "active" => true, "inactive" => false, _ => null };
        var result = await _api.GetEventsAsync(page, isActive);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Stats(int id)
    {
        if (RequireAuth() is { } r) return r;
        var stats = await _api.GetEventStatsAsync(id);
        if (stats == null) return NotFound();
        return View(stats);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (RequireAuth() is { } r) return r;
        var venues = await _api.GetVenuesAsync();
        return View(new EventFormViewModel { Venues = venues?.Items ?? new() });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.CreateEventAsync(request);
        if (result == null)
        {
            ViewBag.Error = "No se pudo crear el evento. Revisa los datos.";
            var venues = await _api.GetVenuesAsync();
            return View(new EventFormViewModel { Request = request, Venues = venues?.Items ?? new() });
        }
        TempData["Success"] = $"Evento \"{result.Name}\" creado exitosamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (RequireAuth() is { } r) return r;
        var ev = await _api.GetEventAsync(id);
        if (ev == null) return NotFound();
        var venues = await _api.GetVenuesAsync();
        return View(new EventFormViewModel
        {
            EditId  = id,
            Request = new CreateEventRequest
            {
                Name            = ev.Name,
                Description     = ev.Description,
                PosterUrl       = ev.PosterUrl,
                Type            = ev.Type,
                DurationMinutes = ev.DurationMinutes
            },
            Venues = venues?.Items ?? new()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, UpdateEventRequest request)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.UpdateEventAsync(id, request);
        if (result == null)
        {
            ViewBag.Error = "No se pudo actualizar el evento.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        TempData["Success"] = $"Evento \"{result.Name}\" actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireAuth() is { } r) return r;
        await _api.DeleteEventAsync(id);
        TempData["Success"] = "Evento eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file)
    {
        if (RequireAuth() is { } r) return r;
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Selecciona un archivo válido.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var result = await _api.UploadEventPhotoAsync(id, file);
        TempData[result != null ? "Success" : "Error"] =
            result != null ? "Imagen del evento actualizada." : "No se pudo subir la imagen.";
        return RedirectToAction(nameof(Edit), new { id });
    }
}
