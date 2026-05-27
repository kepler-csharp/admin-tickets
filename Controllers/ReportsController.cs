using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class ReportsController : Controller
{
    private readonly ApiService _api;
    public ReportsController(ApiService api) => _api = api;

    private IActionResult? RequireAuth() =>
        HttpContext.Session.GetString("AccessToken") == null
            ? RedirectToAction("Login", "Auth") : null;

    public IActionResult Index()
    {
        if (RequireAuth() is { } r) return r;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(DateTime from, DateTime to)
    {
        if (RequireAuth() is { } r) return r;
        if (from > to)
        {
            TempData["Error"] = "La fecha 'Desde' debe ser anterior a 'Hasta'.";
            return RedirectToAction(nameof(Index));
        }
        var (bytes, fileName) = await _api.ExportSalesAsync(from, to);
        if (bytes == null)
        {
            TempData["Error"] = "No se pudo generar el reporte.";
            return RedirectToAction(nameof(Index));
        }
        return File(bytes, "text/csv", fileName);
    }

    public async Task<IActionResult> AuditLog(
        string? adminEmail, string? action,
        DateTime? from, DateTime? to,
        int page = 1)
    {
        if (RequireAuth() is { } r) return r;
        var result = await _api.GetAuditLogAsync(adminEmail, action, from, to, page);
        ViewBag.AdminEmail = adminEmail;
        ViewBag.Action     = action;
        ViewBag.From       = from;
        ViewBag.To         = to;
        return View(result);
    }
}
