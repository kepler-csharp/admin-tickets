using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class EmployeesController : Controller
{
    private readonly ApiService _api;
    public EmployeesController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public IActionResult Index()
    {
        if (RequireAuth() is { } r) return r;
        return View();
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
}
