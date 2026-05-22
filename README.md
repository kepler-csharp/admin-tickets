# Kepler Admin Portal

An administration portal for the Kepler theater management system, built with ASP.NET Core 8 MVC. This application provides administrators with a centralized interface to manage events, venues, showtimes, and staff accounts. It communicates exclusively with the Kepler Central API over HTTP REST using JWT Bearer authentication.

Access to this portal is restricted to users with the `Admin` role. Any other role — including Customer, Scanner, and Receptionist — is denied entry at the login stage.

---

## Table of Contents

- [Requirements](#requirements)
- [Project Structure](#project-structure)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Authentication and Access Control](#authentication-and-access-control)
- [Features](#features)
- [API Integration](#api-integration)
- [Session Management](#session-management)
- [Known API Issues](#known-api-issues)

---

## Requirements

- .NET 8 SDK
- The Kepler Central API running and reachable (local or remote)

No additional NuGet packages are required beyond those included in the `Microsoft.NET.Sdk.Web` framework. The project uses no third-party libraries.

---

## Project Structure

```
AdminPortal/
|
+-- Controllers/
|   +-- AuthController.cs        Login, logout, JWT role validation
|   +-- DashboardController.cs   Business metrics overview
|   +-- EventsController.cs      Event listing, creation, editing, deletion
|   +-- VenuesController.cs      Venue listing, creation, deletion
|   +-- ShowtimesController.cs   Showtime listing and creation with seat layout builder
|   +-- EmployeesController.cs   Staff registration (Admin, Scanner, Receptionist)
|
+-- Models/
|   +-- ApiModels.cs             All DTOs, enums, and view models
|
+-- Services/
|   +-- ApiService.cs            HTTP client wrapper for all Central API calls
|
+-- Views/
|   +-- Auth/
|   |   +-- Login.cshtml         Login page (standalone, no layout)
|   +-- Dashboard/
|   |   +-- Index.cshtml         KPI cards and revenue chart
|   +-- Events/
|   |   +-- Index.cshtml         Event grid with filters and pagination
|   |   +-- Create.cshtml        New event form
|   |   +-- Edit.cshtml          Edit existing event form
|   +-- Venues/
|   |   +-- Index.cshtml         Venue table with inline create modal
|   +-- Showtimes/
|   |   +-- Index.cshtml         Showtime table with pagination
|   |   +-- Create.cshtml        New showtime form with dynamic seat row builder
|   +-- Employees/
|   |   +-- Index.cshtml         Role reference cards and inline register modal
|   +-- Shared/
|       +-- _Layout.cshtml       Sidebar layout with navigation and user info
|
+-- wwwroot/
|   +-- css/site.css             Full design system (Calcite color palette)
|   +-- js/site.js               Toast auto-dismiss
|
+-- Program.cs                   Service registration, session, HTTP client
+-- appsettings.json             API base URL and logging configuration
+-- NuGet.config                 Clears remote package sources for offline builds
```

---

## Configuration

Open `appsettings.json` and set `ApiBaseUrl` to the base address of the Kepler Central API:

```json
{
  "ApiBaseUrl": "https://api.kepler.andrescortes.dev/",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

The trailing slash on `ApiBaseUrl` is required. All relative paths in `ApiService` are appended directly to this base address.

For local development against a local API instance:

```json
{
  "ApiBaseUrl": "http://localhost:5000/"
}
```

---

## Running the Application

```bash
dotnet run
```

The application starts on:

```
http://localhost:5114
https://localhost:7142
```

To run on a specific port:

```bash
dotnet run --urls "http://localhost:5277"
```

To run in production mode:

```bash
dotnet run --environment Production
```

To publish a self-contained release build:

```bash
dotnet publish -c Release -o ./publish
./publish/AdminPortal
```

---

## Authentication and Access Control

### Login flow

1. The administrator submits their email and password on the login page.
2. The portal sends a `POST /api/auth/login` request to the Central API.
3. If the API returns a valid response, the portal decodes the JWT payload client-side without any external library, using manual base64url decoding.
4. The role claim is extracted from the decoded payload. The portal checks for the role under the full ASP.NET Identity URI:
   ```
   http://schemas.microsoft.com/ws/2008/06/identity/claims/role
   ```
   as well as the short aliases `role` and `roles`.
5. If the role is not exactly `Admin`, access is denied with an error message and no session is created. The credentials are never saved.
6. If the role is `Admin`, the access token, email, and role are written to the server-side session and the user is redirected to the dashboard.

### Session guard

Every controller action other than login checks for the presence of `AccessToken` in the session. If it is absent, the user is redirected to the login page. There is no middleware-level auth; the guard is enforced per-action via a shared `RequireAuth()` helper.

### Logout

Logout calls `POST /api/auth/logout` on the Central API (which adds the token to the Redis blacklist) and then clears the local session entirely.

### Default admin credential (seeded by the API)

| Field | Value |
|---|---|
| Email | admin@tickets.com |
| Password | Admin1234! |

---

## Features

### Dashboard

Displays a real-time overview of theater performance pulled from `GET /api/admin/dashboard`:

- Total revenue (all time)
- Total tickets sold (all time)
- Number of active events
- Average occupancy percentage
- Today's revenue and tickets sold
- Revenue-by-day line chart (Chart.js, loaded from CDN)
- Top events ranked by revenue and occupancy

### Events

Full CRUD management for theater events:

- Grid view with poster image, event type badge, and active/inactive status
- Filter by All, Active, or Inactive
- Pagination (12 events per page)
- Create form: name, description, type, duration, venue, poster URL
- Edit form: all fields including active status toggle
- Soft delete (the API sets `IsActive = false`; historical data is preserved)

### Venues

Management of physical or logical spaces used for events:

- Table view with name, city, address, capacity, and status
- Create via inline modal (no page navigation)
- Soft delete

### Showtimes

Scheduling of individual performances:

- Table view with event name, start/end times, base price, seat availability, and status
- Status labels: Active, Cancelled, Completed, SoldOut
- Create form with a dynamic seat row builder: add rows by letter, set seat count per row, and assign a type (Standard, Premium, VIP). The layout is serialized to JSON and sent to the API.

### Employees

Staff account registration with role-based access separation:

- Three role cards explaining what each role can and cannot access
- Register modal: full name, email, temporary password, and role selection
- Roles available: Admin, Receptionist (Taquilla portal only), Scanner (Access portal only)

---

## API Integration

All communication with the Central API goes through `ApiService`. The service is registered as a typed `HttpClient` with the base address from configuration.

### Token attachment

Before every authenticated request, `AttachToken()` reads the JWT from the current session and sets the `Authorization: Bearer <token>` header on the outgoing request.

### Response parsing

Most endpoints return responses wrapped in:

```json
{
  "success": true,
  "message": "OK",
  "data": { }
}
```

The private `Read<T>()` method handles this envelope for all endpoints except login.

### Endpoints consumed

| Method | Endpoint | Feature |
|---|---|---|
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/logout` | Logout |
| POST | `/api/auth/register-admin` | Register admin |
| POST | `/api/auth/register-scanner` | Register scanner |
| POST | `/api/auth/register-receptionist` | Register receptionist |
| GET | `/api/admin/dashboard` | Dashboard metrics |
| GET | `/api/events` | Event list |
| GET | `/api/events/{id}` | Single event (edit form) |
| POST | `/api/events` | Create event |
| PUT | `/api/events/{id}` | Update event |
| DELETE | `/api/events/{id}` | Soft-delete event |
| GET | `/api/venues` | Venue list |
| POST | `/api/venues` | Create venue |
| DELETE | `/api/venues/{id}` | Soft-delete venue |
| GET | `/api/showtimes` | Showtime list |
| POST | `/api/showtimes` | Create showtime with seat layout |

---

## Session Management

Sessions are stored in memory on the server using `AddDistributedMemoryCache`. The session cookie is HTTP-only and marked as essential.

| Key | Value stored |
|---|---|
| `AccessToken` | JWT Bearer token |
| `UserEmail` | Authenticated user's email address |
| `UserRole` | Authenticated user's role (`Admin`) |

Session lifetime is 8 hours. Closing the browser does not end the session; the user must log out explicitly or wait for the session to expire.

In a multi-instance deployment, replace `AddDistributedMemoryCache` with a distributed provider such as Redis (`AddStackExchangeRedisCache`) to ensure session consistency across instances.

---

## Known API Issues

### Refresh token serialized as Task object

The Central API's `POST /api/auth/login` endpoint has a bug in `AuthControllerService.cs`: `GenerateRefreshToken` is an async method but is called without `await`, causing the returned `Task<string>` to be serialized directly into the JSON response:

```json
{
  "accessToken": "eyJ...",
  "refreshToken": {
    "result": "99c23577-086f-414d-a14e-ccc05f8bef33",
    "id": 9,
    "status": 5,
    "isCompleted": true,
    ...
  }
}
```

The portal handles this by using `JsonDocument` to parse the response manually. If `refreshToken` is an object, it reads the value from `refreshToken.result`. If it is a plain string (after the API is fixed), it reads it directly.

To fix this on the API side, add `await` on line 66 of `AuthControllerService.cs`:

```csharp
// Before (bug)
var refreshToken = _jwtService.GenerateRefreshToken(user.Id);

// After (fixed)
var refreshToken = await _jwtService.GenerateRefreshToken(user.Id);
```

The portal will continue working correctly in both the fixed and unfixed states.
