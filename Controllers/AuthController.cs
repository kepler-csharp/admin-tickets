using System.Text;
using System.Text.Json;
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

        // Decode the JWT payload and verify the Admin role before granting access
        var role = ExtractRole(result.AccessToken);
        if (role != "Admin")
        {
            ViewBag.Error = "Acceso denegado. Este portal es exclusivo para administradores.";
            return View(dto);
        }

        HttpContext.Session.SetString("AccessToken", result.AccessToken);
        HttpContext.Session.SetString("UserEmail", dto.Email);
        HttpContext.Session.SetString("UserRole", role);
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

    /// <summary>
    /// Decodes the JWT payload (base64url, second segment) without any external library.
    /// Looks for the role claim using the full ASP.NET Identity URI and short aliases.
    /// </summary>
    private static string? ExtractRole(string accessToken)
    {
        try
        {
            var parts = accessToken.Split('.');
            if (parts.Length != 3) return null;

            // Base64url → Base64 → bytes → UTF-8 string
            var payload = parts[1];
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "=";  break;
            }

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // ASP.NET Identity serializes roles under the full URI claim type
            string[] roleKeys =
            [
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                "role",
                "roles"
            ];

            foreach (var key in roleKeys)
            {
                if (!root.TryGetProperty(key, out var val)) continue;

                if (val.ValueKind == JsonValueKind.String)
                    return val.GetString();

                // Could be an array if the user has multiple roles
                if (val.ValueKind == JsonValueKind.Array)
                    foreach (var item in val.EnumerateArray())
                        if (item.GetString() == "Admin") return "Admin";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
