# Building Manager - .NET Application

Mobile-first digital platform for building managers.

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | .NET 8 + Orleans (Virtual Actors) |
| Mobile | .NET MAUI + Blazor Hybrid |
| UI | Blazor + MudBlazor |
| Database | PostgreSQL |
| Cache | Redis |
| Storage | Azure Blob Storage |
| Hosting | Azure |

## Project Structure

```
src/
├── BuildingManager.Core/              # Domain layer
├── BuildingManager.Infrastructure/    # Data access, external services
├── BuildingManager.Orleans.Interfaces/# Grain interfaces
├── BuildingManager.Orleans.Grains/    # Grain implementations
├── BuildingManager.Orleans.Silo/      # Orleans host
├── BuildingManager.Blazor/            # Shared Blazor UI
├── BuildingManager.Blazor.Mobile/     # MAUI mobile wrapper
└── BuildingManager.Api/               # REST API (optional)

tests/
├── BuildingManager.Tests/
└── BuildingManager.IntegrationTests/   # planned (not yet created)
```

## Quick Start

```bash
# Restore dependencies
dotnet restore

# Start infrastructure (PostgreSQL, Redis, Azurite)
# The init script creates the building_manager and building_manager_orleans databases.
docker-compose up -d

# Create and apply EF Core migrations
dotnet ef migrations add Initial --project src/BuildingManager.Infrastructure
dotnet ef database update --project src/BuildingManager.Infrastructure

# Orleans clustering/persistence tables must exist before the silo starts.
# Apply the Orleans PostgreSQL scripts to the building_manager_orleans database:
# https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/adonet-configuration

# Run silo (dashboard on http://localhost:8081)
dotnet run --project src/BuildingManager.Orleans.Silo

# Run API
dotnet run --project src/BuildingManager.Api

# Run Blazor WebAssembly client (needs a static file server, e.g. VS/V Code
# launch or `dotnet tool install -g dotnet-serve` then `dotnet serve` on the
# publish output). Serve it on http://localhost:5002 or :7071, the origins the
# API allows (Cors:AllowedOrigins). The client reads the API address from
# src/BuildingManager.Blazor/wwwroot/appsettings.json (ApiBaseUrl, default
# http://localhost:5000); the appsettings.json in the project root is not
# shipped to the browser.
```

If the client stops at "Loading..." with an SRI `integrity` error for
`dotnet.native.wasm` in the browser console, the incremental build cache is
stale (e.g. after a .NET runtime update): run
`dotnet clean src/BuildingManager.Blazor` and publish again.

## Authentication

The API requires a JWT for all `/api/buildings` endpoints. Obtain one via:

```bash
# Register (auto-logs-in) or log in; self-registration allows Owner, Tenant,
# Manager and SubstituteManager roles only.
curl -X POST http://localhost:5000/api/auth/register -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","displayName":"Your Name","role":2,"password":"S3curePass!"}'
curl -X POST http://localhost:5000/api/auth/login -H "Content-Type: application/json" \
  -d '{"email":"you@example.com","password":"S3curePass!"}'

curl -H "Authorization: Bearer <token>" http://localhost:5000/api/buildings
```

Logging out (`POST /api/auth/logout`) terminates the session grain so the token
stops working immediately, before its expiry. Passwords are stored as PBKDF2
hashes. Set `Jwt:Secret` (≥ 32 chars) in configuration before production use.

### Sessions, refresh and password reset

- Access tokens last 60 minutes; login/register also return a **refresh token**
  (`{sessionId}:{secret}`, only the SHA-256 hash is stored). `POST /api/auth/refresh`
  rotates it — each refresh token is single-use, and the session lives as long
  as its refresh token (7 days). The Blazor client refreshes automatically on
  401 and retries the failed request.
- `POST /api/auth/forgot-password` always returns 200 (no account enumeration);
  in development the reset token is written to the API log by
  `DevLoggingEmailSender` — swap in a real `IEmailSender` for production.
  `POST /api/auth/reset-password` consumes the single-use token (1 hour validity).
- **Password reset and change revoke every active session** for the account
  (the account tracks its sessions; all are terminated on reset/change).
  `POST /api/auth/change-password` requires the current password and also logs
  out the requesting device — redirect to the login page afterwards.
- The Blazor client covers these flows at `/forgot-password`, `/reset-password`
  (accepts `?email=` and `?token=`) and `/account/password` (linked from the
  app bar).
- **Brute-force protection**: login/register/refresh/forgot/reset are limited
  to 10 requests/minute per client IP (`RateLimit:AuthRequestsPerMinute`),
  returning 429 with a ProblemDetails body.
- **Admin seeding**: set `Auth:AdminEmail` / `Auth:AdminPassword` (and optionally
  `Auth:AdminDisplayName`) to have the API seed an Admin account at startup.

### Roles and building access

- **Read endpoints and work-order reporting**: any authenticated user with
  access to the building (residents report problems, management resolves them).
- **Mutations** (units, residents, keys, announcements, finance, fee
  structures, work-order status): `Manager`, `SubstituteManager`, `Admin` or
  `CompanyAdmin` only.
- **Building access** is per user account. Creating a building grants the
  creator access; managers grant/revoke it for others via
  `PUT/DELETE /api/buildings/{id}/access/{email}` and list members with
  `GET /api/buildings/{id}/access` (also on the building's detail page in the
  Blazor client). Admins bypass access checks (and are not listed as members),
  and `GET /api/buildings` only lists buildings the caller can access.

### Owners' assemblies

`/api/buildings/{id}/assemblies` (Blazor: **Assemblies** in the menu). Anyone
with access to the building can read assemblies and minutes; only management
roles run them. One grain per assembly, keyed `{buildingId}|{assemblyId}`.

1. **Schedule** with date, location and agenda (each item: simple, two-thirds
   or unanimous majority, or informational). Dates in the past are rejected;
   less than 3 days' notice needs `allowShortNotice`.
2. **Record the notice** (and that it was posted in the building). The agenda
   is fixed from then on.
3. **Start** the meeting, at most an hour before the announced time. The units'
   ownership shares are snapshotted as the voting basis.
4. **Attendance** per unit: in person, proxy (holder required), written or
   electronic. Quorum is computed from the shares: 51% for a first session,
   one third for a repeated one.
5. **Votes** per present unit and item, only while quorum is reached; closing an
   item decides it by its majority rule (`AssemblyRules`, shared with the domain
   model).
6. **Complete**: with quorum every voted item must be decided first. Without
   quorum no decisions are made and a first session can be followed by a
   repeated session (`POST .../{assemblyId}/repeat`) with the same agenda.

The Blazor client prints the minutes from `/buildings/{id}/assemblies/{assemblyId}/minutes`.
Not yet implemented: sending notices/minutes to owners, reminders, PDF export,
tracking owners who miss sessions.

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed documentation.

## Key Features

- Orleans virtual actors for scalable building management
- Blazor Hybrid for shared web/mobile UI
- Offline-first with SQLite sync
- Full Serbian law compliance
- Real-time notifications via Orleans streams

## Requirements

- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 / Rider / VS Code with C# extension
