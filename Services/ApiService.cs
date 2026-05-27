using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using admin_tickets.Models;

namespace admin_tickets.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _ctx;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ApiService(HttpClient http, IHttpContextAccessor ctx)
    {
        _http = http;
        _ctx  = ctx;
    }

    private void AttachToken()
    {
        var token = _ctx.HttpContext?.Session.GetString("AccessToken");
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    private StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj, _json), Encoding.UTF8, "application/json");

    private async Task<T?> Read<T>(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) return default;
        var wrapper = JsonSerializer.Deserialize<ApiResponse<T>>(json, _json);
        return wrapper is { Success: true } ? wrapper.Data : default;
    }

    // ── Auth ──────────────────────────────────────────────────────────────────
    public async Task<AuthResponse?> LoginAsync(LoginDto dto)
    {
        var res = await _http.PostAsync("api/auth/login", Json(dto));
        if (!res.IsSuccessStatusCode) return null;

        var json = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.TryGetProperty("accessToken", out var at)
                          && at.ValueKind == JsonValueKind.String
            ? at.GetString() : null;

        if (string.IsNullOrEmpty(accessToken)) return null;

        var refreshToken = "";
        if (root.TryGetProperty("refreshToken", out var rt))
        {
            if (rt.ValueKind == JsonValueKind.String)
                refreshToken = rt.GetString() ?? "";
            else if (rt.ValueKind == JsonValueKind.Object
                     && rt.TryGetProperty("result", out var rtResult)
                     && rtResult.ValueKind == JsonValueKind.String)
                refreshToken = rtResult.GetString() ?? "";
        }

        return new AuthResponse { AccessToken = accessToken, RefreshToken = refreshToken };
    }

    public async Task LogoutAsync(string token)
    {
        AttachToken();
        await _http.PostAsync("api/auth/logout", null);
    }

    public async Task<bool> RegisterEmployeeAsync(RegisterDto dto, string role)
    {
        AttachToken();
        var endpoint = role switch
        {
            "Scanner"      => "api/auth/register-scanner",
            "Receptionist" => "api/auth/register-receptionist",
            _              => "api/auth/register-admin"
        };
        var res = await _http.PostAsync(endpoint, Json(dto));
        return res.IsSuccessStatusCode;
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    public async Task<DashboardDto?> GetDashboardAsync()
    {
        AttachToken();
        var res = await _http.GetAsync("api/admin/dashboard");
        return await Read<DashboardDto>(res);
    }

    // ── Customers ─────────────────────────────────────────────────────────────
    public async Task<PagedResult<UserAdminDto>?> GetCustomersAsync(int page = 1, int pageSize = 20)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/admin/customers?page={page}&pageSize={pageSize}");
        return await Read<PagedResult<UserAdminDto>>(res);
    }

    public async Task<UserAdminDto?> GetCustomerAsync(string id)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/admin/customers/{id}");
        return await Read<UserAdminDto>(res);
    }

    public async Task<UserAdminDto?> UpdateCustomerAsync(string id, UpdateUserRequest req)
    {
        AttachToken();
        var res = await _http.PutAsync($"api/admin/customers/{id}", Json(req));
        return await Read<UserAdminDto>(res);
    }

    public async Task<bool> DeactivateCustomerAsync(string id)
    {
        AttachToken();
        var res = await _http.DeleteAsync($"api/admin/customers/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ReactivateCustomerAsync(string id)
    {
        AttachToken();
        var res = await _http.PatchAsync($"api/admin/customers/{id}/reactivate", null);
        return res.IsSuccessStatusCode;
    }

    // ── Employees ─────────────────────────────────────────────────────────────
    public async Task<PagedResult<UserAdminDto>?> GetEmployeesAsync(int page = 1, int pageSize = 20)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/admin/employees?page={page}&pageSize={pageSize}");
        return await Read<PagedResult<UserAdminDto>>(res);
    }

    public async Task<UserAdminDto?> GetEmployeeAsync(string id)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/admin/employees/{id}");
        return await Read<UserAdminDto>(res);
    }

    public async Task<UserAdminDto?> UpdateEmployeeAsync(string id, UpdateUserRequest req)
    {
        AttachToken();
        var res = await _http.PutAsync($"api/admin/employees/{id}", Json(req));
        return await Read<UserAdminDto>(res);
    }

    public async Task<bool> DeactivateEmployeeAsync(string id)
    {
        AttachToken();
        var res = await _http.DeleteAsync($"api/admin/employees/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> ResetEmployeePasswordAsync(string id, string newPassword)
    {
        AttachToken();
        var res = await _http.PatchAsync(
            $"api/admin/employees/{id}/reset-password",
            Json(new AdminResetPasswordRequest { NewPassword = newPassword }));
        return res.IsSuccessStatusCode;
    }

    // ── Reports ───────────────────────────────────────────────────────────────
    public async Task<(byte[]? Bytes, string FileName)> ExportSalesAsync(DateTime from, DateTime to)
    {
        AttachToken();
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr   = to.ToString("yyyy-MM-dd");
        var res = await _http.GetAsync($"api/admin/reports/export?from={fromStr}&to={toStr}");
        if (!res.IsSuccessStatusCode) return (null, "");
        var bytes    = await res.Content.ReadAsByteArrayAsync();
        var fileName = $"ventas_{fromStr}_{toStr}.csv";
        return (bytes, fileName);
    }

    // ── Audit Log ─────────────────────────────────────────────────────────────
    public async Task<PagedResult<AuditLogDto>?> GetAuditLogAsync(
        string? adminEmail = null, string? action = null,
        DateTime? from = null, DateTime? to = null,
        int page = 1, int pageSize = 50)
    {
        AttachToken();
        var qs = $"page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(adminEmail)) qs += $"&adminEmail={Uri.EscapeDataString(adminEmail)}";
        if (!string.IsNullOrEmpty(action))     qs += $"&action={Uri.EscapeDataString(action)}";
        if (from.HasValue) qs += $"&from={from.Value:yyyy-MM-dd}";
        if (to.HasValue)   qs += $"&to={to.Value:yyyy-MM-dd}";
        var res = await _http.GetAsync($"api/admin/audit-log?{qs}");
        return await Read<PagedResult<AuditLogDto>>(res);
    }

    // ── Events ────────────────────────────────────────────────────────────────
    public async Task<PagedResult<EventDto>?> GetEventsAsync(int page = 1, bool? isActive = null)
    {
        AttachToken();
        var q = isActive.HasValue ? $"&isActive={isActive}" : "";
        var res = await _http.GetAsync($"api/events?page={page}&pageSize=12{q}");
        return await Read<PagedResult<EventDto>>(res);
    }

    public async Task<EventDto?> GetEventAsync(int id)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/events/{id}");
        return await Read<EventDto>(res);
    }

    public async Task<EventDto?> CreateEventAsync(CreateEventRequest req)
    {
        AttachToken();
        var res = await _http.PostAsync("api/events", Json(req));
        return await Read<EventDto>(res);
    }

    public async Task<EventDto?> UpdateEventAsync(int id, UpdateEventRequest req)
    {
        AttachToken();
        var res = await _http.PutAsync($"api/events/{id}", Json(req));
        return await Read<EventDto>(res);
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        AttachToken();
        var res = await _http.DeleteAsync($"api/events/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task<EventStatsDto?> GetEventStatsAsync(int id)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/events/{id}/stats");
        return await Read<EventStatsDto>(res);
    }

    public async Task<EventDto?> UploadEventPhotoAsync(int id, IFormFile file)
    {
        AttachToken();
        using var content = new MultipartFormDataContent();
        using var stream  = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", file.FileName);
        var res = await _http.PostAsync($"api/events/{id}/upload-photo", content);
        return await Read<EventDto>(res);
    }

    // ── Venues ────────────────────────────────────────────────────────────────
    public async Task<PagedResult<VenueDto>?> GetVenuesAsync(int page = 1)
    {
        AttachToken();
        var res = await _http.GetAsync($"api/venues?page={page}&pageSize=20");
        return await Read<PagedResult<VenueDto>>(res);
    }

    public async Task<VenueDto?> CreateVenueAsync(CreateVenueRequest req)
    {
        AttachToken();
        var res = await _http.PostAsync("api/venues", Json(req));
        return await Read<VenueDto>(res);
    }

    public async Task<bool> DeleteVenueAsync(int id)
    {
        AttachToken();
        var res = await _http.DeleteAsync($"api/venues/{id}");
        return res.IsSuccessStatusCode;
    }

    // ── Showtimes ─────────────────────────────────────────────────────────────
    public async Task<PagedResult<ShowtimeDto>?> GetShowtimesAsync(int page = 1, int? eventId = null)
    {
        AttachToken();
        var q = eventId.HasValue ? $"&eventId={eventId}" : "";
        var res = await _http.GetAsync($"api/showtimes?page={page}&pageSize=20{q}");
        return await Read<PagedResult<ShowtimeDto>>(res);
    }

    public async Task<ShowtimeDto?> CreateShowtimeAsync(CreateShowtimeRequest req)
    {
        AttachToken();
        var res = await _http.PostAsync("api/showtimes", Json(req));
        return await Read<ShowtimeDto>(res);
    }
}
