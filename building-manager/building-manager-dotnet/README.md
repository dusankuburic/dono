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
├── BuildingManager.Infrastructure/    # External services (blob storage)
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

# Secrets are not in appsettings. Give the API a JWT secret (>= 32 chars)
# and, for the seeded admin account, credentials — via user secrets:
dotnet user-secrets init --project src/BuildingManager.Api
dotnet user-secrets set "Jwt:Secret" "dev-only-secret-at-least-32-characters!" --project src/BuildingManager.Api
dotnet user-secrets set "Auth:AdminEmail" "admin@example.com" --project src/BuildingManager.Api
dotnet user-secrets set "Auth:AdminPassword" "a-strong-password" --project src/BuildingManager.Api
# ... or environment variables: Jwt__Secret / Auth__AdminEmail / Auth__AdminPassword.

# Orleans clustering/persistence/reminders tables must exist before the silo
# starts. Apply the vendored scripts (infra/orleans-sql/, in numbered order —
# main-branch copies from the Orleans repo; the version-tagged ones lag the
# runtime) to the building_manager_orleans database, e.g. with psql:
#   for f in infra/orleans-sql/*.sql; do psql "$ORLEANS_DB_URL" -f "$f"; done

# Run silo (dashboard on http://localhost:8081, health on http://localhost:8080/health)
dotnet run --project src/BuildingManager.Orleans.Silo

# Run API (health on http://localhost:5000/health: checks the Orleans cluster)
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

## Deployment (Azure)

Provisioned with `infra/azure/main.bicep` (validated with `az bicep build`;
not yet exercised against a live subscription — expect small adjustments on
the first real deployment):

```bash
az group create -n bm-prod -l westeurope
cp infra/azure/main.parameters.example.json infra/azure/main.parameters.json
az deployment group create -g bm-prod -f infra/azure/main.bicep -p @infra/azure/main.parameters.json
```

This creates: a VNet + Azure Container Apps environment, the silo and API as
container apps (Orleans gateway on an internal additional TCP port — the silo
runs a single replica for now), PostgreSQL flexible server (with the
`building_manager_orleans` database), a storage account (documents container +
static website hosting the Blazor client), Key Vault (JWT secret, admin
credentials, connection strings; RBAC-assigned to the apps' managed
identities) and an ACR the CI pipeline pushes to.

Apply the Orleans schema once (scripts are vendored in `infra/orleans-sql/`,
in order — they are the main-branch copies because the version-tagged ones
lag the runtime):

```bash
for f in infra/orleans-sql/*.sql; do
  az postgres flexible-server execute -n <postgres-name> -u bmadmin -p '<password>' -d building_manager_orleans -f "$f"
done
```

CI (`.github/workflows/ci.yml` at the repository root) builds and tests on
every push/PR with the .NET 8 SDK. On pushes to main it also builds the silo
and API images, pushes them to ACR, updates both container apps and publishes
the Blazor client to the storage static website — but only when these
repository secrets exist: `AZURE_CREDENTIALS` (service principal with
contributor rights on the resource group), `ACR_NAME`, `RESOURCE_GROUP`,
`STORAGE_ACCOUNT`. Without them the deploy job is skipped.


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
hashes. `Jwt:Secret` (≥ 32 chars) is required configuration — the API refuses
to start without it. It is not in appsettings (nor are the admin seeding
credentials); set it via user secrets or environment variables locally. In
production, set `KeyVault:VaultUri` and the API loads every secret from Azure
Key Vault with the host's managed identity (secret names use `--` separators,
e.g. `Jwt--Secret`, `Auth--AdminPassword`, `ConnectionStrings--OrleansStorage`).
The API reaches the Orleans cluster through the same clustering table the silo
writes to (ADO.NET gateway discovery via `ConnectionStrings:OrleansStorage`;
it falls back to a static loopback gateway only when no connection string is set).

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

The Blazor client prints the minutes from `/buildings/{id}/assemblies/{assemblyId}/minutes`,
and both documents are generated server-side as PDFs:
`GET .../{assemblyId}/notice/pdf` (the formal notice, printable for posting)
and `GET .../{assemblyId}/minutes/pdf`. Not yet implemented: sending
notices/minutes to owners by email, reminders, tracking owners who miss sessions.

### Announcements

`/api/buildings/{id}/announcements` (Blazor: **Announcements**). Everyone with
access reads the feed (published, not expired, pinned first; emergencies are
highlighted). Managers publish now or at a future time, optionally until an
expiry date, pin/unpin (`PUT .../{announcementId}/pin`) and archive
(`POST .../{announcementId}/archive`, which expires it but keeps it on record).
`GET .../announcements/history` lists everything kept, including scheduled
and expired items. Unpinned announcements are pruned after 12 months. The
author is taken from the signed-in account. Delivery by SMS/email/push and
recipient selection are not implemented yet.

### Notifications

In-app notifications (the bell in the header, `/api/notifications`): a
per-user grain keyed by the account email keeps the last 100 (dropping the
oldest read ones first). Members are notified when an announcement reaches
the feed, when the status of their work order changes (matched by the
reporting resident's email) and when an assembly is scheduled or repeated.
Endpoints: `GET /api/notifications?limit=`, `GET unread-count`,
`PUT {id}/read`, `PUT read-all`.

Email and SMS go through `IEmailSender`/`ISmsSender`. By default both log
instead of sending (reset emails print the token). Configure Azure
Communication Services to send for real: `Email:ConnectionString` +
`Email:SenderAddress` (verified sender) and `Sms:ConnectionString` +
`Sms:From` (E.164; SMS costs apply). Email/SMS fan-out on events, delivery
preferences and quiet hours are not implemented yet.

### Background jobs

A singleton scheduler grain (`ISchedulerGrain`, key "global") runs on an
Orleans reminder every 5 minutes (backed by the ADO.NET reminder table in
`building_manager_orleans`, so schedules survive restarts):

- publishes scheduled announcements when they come due (sets `SentAt`) and
  notifies the building's members in-app — until then they stay out of the feed;
- on the first pass of each day, reminds members of a noticed assembly two
  days, one day and on the day of the meeting (at most one reminder per
  assembly per day, tracked per assembly);
- daily digest for managers: keys past their expected return
  (UC-002) and emergency work orders past the 48-hour deadline (UC-023),
  matched to member accounts with a management role.

Any caller can trigger a pass with `RunSweepAsync()` (used by tests). Payment
reminders (UC-008) and deadline tracking (UC-012) are later phases.

### Documents

`/api/buildings/{id}/documents` (Blazor: **Documents**). Managers upload
PDFs and images (PDF, PNG, JPG, WebP, GIF, HEIC; 25 MB per file, 1 GB per
building) into folders — registration, assembly minutes, financial reports,
contracts, permits, insurance, other — marked "managers only" or "all
owners". Owners with building access see and download only the shared ones
(`GET .../{documentId}/content` streams the file; the API filters the list
and checks access per document). The uploader is the signed-in account.
Content lives in Azure Blob Storage (`Storage:BlobConnectionString` /
`Storage:ContainerName` in appsettings; run Azurite locally with
`docker compose up -d`). Full-text search and shareable public links are
not implemented yet.

### Fees, payments and expenses

`/api/buildings/{id}/finance/...` (Blazor: **Finance**).

- `GET status?year=&month=`: per-unit fee status for a billing month. Due =
  fixed amount + per m² × area, using the fee structure in force by the end of
  that month. Standing: paid / not paid this month (last month paid) / 2+ months
  unpaid / no fee. Managers see every unit; other users only the units where
  they are registered as a resident with their account email.
  `GET status/pdf` and `GET status/xlsx` export the same (scoped) table as a
  PDF (QuestPDF) or Excel sheet (ClosedXML).
- `POST payments`: unit, amount, billing month (`month`/`year`), date received
  (`paymentDate`, defaults to now, not in the future), method, reference, notes,
  optional client `id` for safe retries. The Blazor form warns before saving a
  payment that matches an existing one (same unit, month and amount).
- `GET ledger?year=&month=` (managers): payments received and expenses incurred
  that month, plus payments received in other months for it.
- `GET payments?unit=`: a unit's payments of the last 12 months (owners: own units only).
- `POST expenses`: category, amount, description, vendor, date.

Payment reminders and financial-report exports beyond the fee status are not
implemented yet.

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
