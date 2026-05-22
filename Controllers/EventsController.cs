using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class EventsController : Controller
{
    private readonly ApiService _api;
    public EventsController(ApiService api) => _api = api;

    private IActionResult RequireAuth()
    {
        if (HttpContext.Session.GetString("AccessToken") == null)
            return RedirectToAction("Login", "Auth");
        return null!;
    }

    public async Task<IActionResult> Index(int page = 1, string? filter = null)
    {
        if (RequireAuth() is { } r) return r;
        bool? isActive = filter switch { "active" => true, "inactive" => false, _ => null };
        var result = await _api.GetEventsAsync(page, isActive);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (RequireAuth() is { } r) return r;
        var venues = await _api.GetVenuesAsync();
        return View(new EventFormViewModel
        {
            Venues = venues?.Items ?? new()
        });
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
            EditId = id,
            Request = new CreateEventRequest
            {
                Name = ev.Name, Description = ev.Description,
                PosterUrl = ev.PosterUrl, Type = ev.Type,
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
}
