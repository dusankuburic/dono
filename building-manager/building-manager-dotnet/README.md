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
# publish output). Point ApiBaseUrl in appsettings.json at the API.
```

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
