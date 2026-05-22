namespace AdminPortal.Models;

// ── Auth ──────────────────────────────────────────────────────────────────────
public class LoginDto
{
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterDto
{
    public string FullName { get; set; } = "";
    public string Email    { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string AccessToken  { get; set; } = "";
    public string RefreshToken { get; set; } = "";
}

// ── Generic wrapper ───────────────────────────────────────────────────────────
public class ApiResponse<T>
{
    public bool   Success { get; set; }
    public string Message { get; set; } = "";
    public T?     Data    { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items      { get; set; } = new();
    public int     TotalCount { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
    public int     TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

// ── Dashboard ─────────────────────────────────────────────────────────────────
public class DashboardDto
{
    public decimal          TotalRevenue        { get; set; }
    public int              TotalTicketsSold    { get; set; }
    public int              ActiveEvents        { get; set; }
    public decimal          TodayRevenue        { get; set; }
    public int              TodayTicketsSold    { get; set; }
    public double           AverageOccupancyPct { get; set; }
    public List<DailyRevenueDto> RevenueByDay  { get; set; } = new();
    public List<TopEventDto>     TopEvents     { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date        { get; set; }
    public decimal  Revenue     { get; set; }
    public int      TicketsSold { get; set; }
}

public class TopEventDto
{
    public int     EventId      { get; set; }
    public string  EventName    { get; set; } = "";
    public int     TicketsSold  { get; set; }
    public decimal Revenue      { get; set; }
    public double  OccupancyPct { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────
public enum EventType { Movie = 0, Concert = 1, Theater = 2, Sports = 3, Other = 4 }

public class EventDto
{
    public int       Id              { get; set; }
    public string    Name            { get; set; } = "";
    public string    Description     { get; set; } = "";
    public string?   PosterUrl       { get; set; }
    public string    VenueName       { get; set; } = "";
    public string    VenueCity       { get; set; } = "";
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
    public bool      IsActive        { get; set; }
    public DateTime  CreatedAt       { get; set; }
}

public class CreateEventRequest
{
    public string    Name            { get; set; } = "";
    public string    Description     { get; set; } = "";
    public string?   PosterUrl       { get; set; }
    public int       VenueId         { get; set; }
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
}

public class UpdateEventRequest
{
    public string    Name            { get; set; } = "";
    public string    Description     { get; set; } = "";
    public string?   PosterUrl       { get; set; }
    public int       VenueId         { get; set; }
    public EventType Type            { get; set; }
    public int       DurationMinutes { get; set; }
    public bool      IsActive        { get; set; }
}

// ── Venues ────────────────────────────────────────────────────────────────────
public class VenueDto
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";
    public string Address  { get; set; } = "";
    public string City     { get; set; } = "";
    public int    Capacity { get; set; }
    public bool   IsActive { get; set; }
}

public class CreateVenueRequest
{
    public string Name     { get; set; } = "";
    public string Address  { get; set; } = "";
    public string City     { get; set; } = "";
    public int    Capacity { get; set; }
}

// ── Showtimes ─────────────────────────────────────────────────────────────────
public enum ShowtimeStatus { Active = 0, Cancelled = 1, Completed = 2, SoldOut = 3 }
public enum SeatType        { Standard = 0, Premium = 1, VIP = 2 }

public class ShowtimeDto
{
    public int           Id             { get; set; }
    public int           EventId        { get; set; }
    public string        EventName      { get; set; } = "";
    public DateTime      StartTime      { get; set; }
    public DateTime      EndTime        { get; set; }
    public decimal       BasePrice      { get; set; }
    public ShowtimeStatus Status        { get; set; }
    public int           AvailableSeats { get; set; }
    public int           TotalSeats     { get; set; }
}

public class CreateShowtimeRequest
{
    public int      EventId    { get; set; }
    public DateTime StartTime  { get; set; }
    public decimal  BasePrice  { get; set; }
    public List<SeatRowRequest> SeatLayout { get; set; } = new();
}

public class SeatRowRequest
{
    public string   Row       { get; set; } = "";
    public int      SeatCount { get; set; }
    public SeatType Type      { get; set; } = SeatType.Standard;
}

// ── View Models ───────────────────────────────────────────────────────────────
public class EventFormViewModel
{
    public CreateEventRequest Request { get; set; } = new();
    public List<VenueDto>     Venues  { get; set; } = new();
    public int?               EditId  { get; set; }
}

public class ShowtimeFormViewModel
{
    public CreateShowtimeRequest Request { get; set; } = new();
    public List<EventDto>        Events  { get; set; } = new();
}
