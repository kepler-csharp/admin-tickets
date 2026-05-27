using admin_tickets.Models;
using admin_tickets.Services;
using Microsoft.AspNetCore.Mvc;

namespace admin_tickets.Controllers;

public class EmployeesController : Controller
{
    private readonly ApiService _api;
    public EmployeesController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public async Task<IActionResult> Index(int page = 1)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.GetEmployeesAsync(page);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (RequireAuth() is { } r) return r;
        var emp = await _api.GetEmployeeAsync(id);
        if (emp == null) return NotFound();
        return View(new EmployeeEditViewModel
        {
            Employee = emp,
            Request  = new UpdateUserRequest
            {
                FullName = emp.FullName,
                Email    = emp.Email,
                IsActive = emp.IsActive
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string id, UpdateUserRequest request)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.UpdateEmployeeAsync(id, request);
        TempData[result != null ? "Success" : "Error"] =
            result != null ? $"Empleado \"{result.FullName}\" actualizado." : "No se pudo actualizar el empleado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RegisterDto dto, string role)
    {
        if (RequireAuth() is { } r) return r;
        var ok = await _api.RegisterEmployeeAsync(dto, role);
        TempData[ok ? "Success" : "Error"] =
            ok ? $"Empleado \"{dto.FullName}\" registrado como {role}."
               : "No se pudo registrar el empleado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(string id)
    {
        if (RequireAuth() is { } r) return r;
        var ok = await _api.DeactivateEmployeeAsync(id);
        TempData[ok ? "Success" : "Error"] =
            ok ? "Empleado desactivado." : "No se pudo desactivar el empleado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("Employees/ResetPassword/{id}")]
    public async Task<IActionResult> ResetPassword(string id, [FromForm] string newPassword)
    {
        if (RequireAuth() is { } r) return r;
        var ok = await _api.ResetEmployeePasswordAsync(id, newPassword);
        TempData[ok ? "Success" : "Error"] =
            ok ? "Contraseña restablecida correctamente." : "No se pudo restablecer la contraseña.";
        return RedirectToAction(nameof(Index));
    }
}
