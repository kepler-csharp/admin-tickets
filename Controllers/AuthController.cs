using AdminPortal.Models;
using AdminPortal.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminPortal.Controllers;

public class AuthController : Controller
{
    private readonly ApiService _api;
    public AuthController(ApiService api) => _api = api;

    [HttpGet]
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("AccessToken") != null)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var result = await _api.LoginAsync(dto);
        if (result == null)
        {
            ViewBag.Error = "Credenciales inválidas. Verifica tu correo y contraseña.";
            return View(dto);
        }

        HttpContext.Session.SetString("AccessToken", result.AccessToken);
        HttpContext.Session.SetString("UserEmail", dto.Email);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Session.GetString("AccessToken") ?? "";
        await _api.LogoutAsync(token);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
