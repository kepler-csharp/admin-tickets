using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class CustomersController : Controller
{
    private readonly ApiService _api;
    public CustomersController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public async Task<IActionResult> Index(int page = 1)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.GetCustomersAsync(page);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (RequireAuth() is { } r) return r;
        var customer = await _api.GetCustomerAsync(id);
        if (customer == null) return NotFound();
        return View(new CustomerEditViewModel
        {
            User    = customer,
            Request = new UpdateUserRequest
            {
                FullName = customer.FullName,
                Email    = customer.Email,
                IsActive = customer.IsActive
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, UpdateUserRequest request)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.UpdateCustomerAsync(id, request);
        TempData[result != null ? "Success" : "Error"] =
            result != null ? $"Cliente \"{result.FullName}\" actualizado." : "No se pudo actualizar el cliente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(string id)
    {
        if (RequireAuth() is { } r) return r;
        var ok = await _api.DeactivateCustomerAsync(id);
        TempData[ok ? "Success" : "Error"] =
            ok ? "Cliente desactivado." : "No se pudo desactivar el cliente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Reactivate(string id)
    {
        if (RequireAuth() is { } r) return r;
        var ok = await _api.ReactivateCustomerAsync(id);
        TempData[ok ? "Success" : "Error"] =
            ok ? "Cliente reactivado." : "No se pudo reactivar el cliente.";
        return RedirectToAction(nameof(Index));
    }
}
