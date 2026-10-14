# Building Manager - .NET Application

Mobile-first digital platform for building managers.

## Tech Stack

| Component | Technology |
|-----------|------------|
| Backend | .NET 8 ASP.NET Core API hosting an Orleans 8 silo (virtual actors) |
| UI | Blazor WebAssembly + MudBlazor |
| Database | PostgreSQL (Orleans clustering, grain state and reminders) |
| Storage | Azure Blob Storage (documents; Azurite locally) |
| Hosting | Azure Container Apps (API), Static Web Apps (client) |
| Telemetry | OpenTelemetry → Application Insights |

## Project Structure

```
src/
├── BuildingManager.Core/              # Enums, rules (AssemblyRules), password hashing
├── BuildingManager.Infrastructure/    # Blob document storage
├── BuildingManager.Orleans.Interfaces/# Grain interfaces and DTOs
├── BuildingManager.Orleans.Grains/    # Grain implementations
├── BuildingManager.Api/               # REST API + Orleans silo (the deployable)
├── BuildingManager.Blazor/            # Blazor WebAssembly client
└── BuildingManager.Blazor.Mobile/     # MAUI wrapper (stub, not in the solution)

infra/
├── orleans-sql/                       # Orleans PostgreSQL schema + apply.sh
├── postgres/init/                     # Docker init: database + schema on first start
└── azure/main.bicep                   # Azure resources

tests/
└── BuildingManager.Tests/             # Grain tests on an in-process cluster
```

## Quick Start

```bash
# Start PostgreSQL and Azurite. On first start the Orleans schema is applied
# automatically. Ports are configurable if taken, e.g.
# POSTGRES_PORT=55432 docker compose up -d (then point ConnectionStrings__OrleansStorage at it).
docker compose up -d

# Secrets are not in appsettings. Give the API a JWT secret (>= 32 chars)
# and, for the seeded admin account, credentials, via user secrets:
dotnet user-secrets set "Jwt:Secret" "dev-only-secret-at-least-32-characters!" --project src/BuildingManager.Api
dotnet user-secrets set "Auth:AdminEmail" "admin@example.com" --project src/BuildingManager.Api
dotnet user-secrets set "Auth:AdminPassword" "a-strong-password" --project src/BuildingManager.Api
# ... or environment variables: Jwt__Secret / Auth__AdminEmail / Auth__AdminPassword.

# Run the API on http://localhost:5000 (launch profile: Development, local
# connection strings). The API is also the Orleans silo; no separate host.
dotnet run --project src/BuildingManager.Api

# No database at hand? Run with in-memory Orleans storage (data is lost on exit):
Orleans__UseInMemory=true dotnet run --project src/BuildingManager.Api

# Run the Blazor WebAssembly client (needs a static file server, e.g. VS/VS Code
# launch or `dotnet tool install -g dotnet-serve` then `dotnet serve` on the
# publish output). Serve it on http://localhost:5002 or :7071, the origins the
# API allows (Cors:AllowedOrigins). The client reads the API address from
# src/BuildingManager.Blazor/wwwroot/appsettings.json (ApiBaseUrl, default
# http://localhost:5000); the appsettings.json in the project root is not
# shipped to the browser.
```

Everything in containers (API on :5000, client on :5002): put `JWT_SECRET`
(and optionally `ADMIN_EMAIL` / `ADMIN_PASSWORD`) in a `.env` file next to
`docker-compose.yml` (ignored by git), then:

```bash
docker compose --profile app up -d --build
```

Compose names containers after the project (no fixed `container_name`). If an
older `buildingmanager-postgres`/`-redis`/`-azurite` container from a previous
compose file still holds a port, remove it. A Postgres volume created before
the schema was applied automatically has no Orleans tables: run
`infra/orleans-sql/apply.sh` against it once (it uses the standard `PGHOST`,
`PGPORT`, `PGUSER`, `PGPASSWORD` variables) or recreate the volume.

`apply.sh` applies the scripts in `infra/orleans-sql` in name order and records
each in an `orleans_schema_scripts` table, so it is safe to run on every
deployment and only applies new ones. A database whose schema was applied by
hand is detected and its base scripts recorded as applied. The scripts are the
main-branch copies from the Orleans repository (the version-tagged ones lack
queries the 8.2 runtime needs).

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
hashes. `Jwt:Secret` (≥ 32 chars) is required configuration: the API refuses
to start without it. It is not in appsettings (nor are the admin seeding
credentials); set it via user secrets or environment variables locally. In
production, set `KeyVault:VaultUri` and the API loads every secret from Azure
Key Vault with the host's managed identity (secret names use `--` separators,
e.g. `Jwt--Secret`, `Auth--AdminPassword`, `ConnectionStrings--OrleansStorage`).

Health: `GET /health/live` answers while the process runs; `GET /health/ready`
(and `/health`) only when the Orleans cluster and its PostgreSQL database are
reachable (503 otherwise). The Orleans dashboard is off unless
`Orleans:DashboardPort` is set; it listens on localhost unless
`Orleans:DashboardHost` is set, and asks for credentials when
`Orleans:DashboardUsername`/`Orleans:DashboardPassword` are set.

### Sessions, refresh and password reset

- Access tokens last 60 minutes; login/register also return a **refresh token**
  (`{sessionId}:{secret}`, only the SHA-256 hash is stored). `POST /api/auth/refresh`
  rotates it — each refresh token is single-use, and the session lives as long
  as its refresh token (7 days). The Blazor client refreshes automatically on
  401 and retries the failed request. Ended sessions are cleared from storage.
- `POST /api/auth/forgot-password` always returns 200 (no account enumeration);
  in Development the reset token is written to the API log by
  `DevLoggingEmailSender`. Elsewhere nothing is sent (a warning is logged,
  never the token) until an email sender is configured (see Notifications).
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
- **Mutations** (building details, units, residents, keys, announcements,
  finance, fee structures, work-order status): `Manager`, `SubstituteManager`,
  `Admin` or `CompanyAdmin` only.
- **Building access** is per user account. Creating a building grants the
  creator access; managers grant/revoke it for others via
  `PUT/DELETE /api/buildings/{id}/access/{email}` and list members with
  `GET /api/buildings/{id}/access` (also on the building's detail page in the
  Blazor client). Admins bypass access checks (and are not listed as members),
  and `GET /api/buildings` only lists buildings the caller can access.

### Building setup: details, units, residents, keys

All under `/api/buildings/{id}`; changes need a management role.

- **Details**: `GET info`, `PUT /api/buildings/{id}` (renaming also updates
  the building list). Blazor: **Edit details** on the building page.
- **Units**: `POST units` adds or updates the unit with that number (its id is
  kept); `DELETE units/{number}` is refused while active residents live there.
  The building page shows the ownership-share total and warns until it is
  100% (assembly quorum and votes are weighted by these shares). Saving is
  not blocked, so shares can be redistributed one unit at a time.
- **Residents**: `POST residents`, `PUT residents/{residentId}`,
  `DELETE residents/{residentId}` (moves out: leaves the directory, kept on
  record). A resident's unit must exist; the email is what matches a resident
  to their account (fee status, work-order notifications). Blazor:
  **Residents** with search (name, unit, phone, email), role filter and
  one-tap phone links.
- **Keys**: `POST keys` (unique label, optional unit), `POST keys/{id}/checkout`
  and `/checkin` (condition `Lost` or `Damaged` leaves the key unavailable),
  `PUT keys/{id}/status` (available / lost / damaged / in repair, not while
  checked out), `GET keys/overdue`, `GET keys/{id}/history` (managers: holds
  names and phones). Blazor: **Keys** with hand-out (pick a resident or enter
  anyone), check-in, overdue banner and "keys due" filter, history. The
  scheduler also digests overdue keys to managers daily. Key receipts as
  PDF/SMS are not implemented yet.

### Work orders

`/api/buildings/{id}/workorders` (Blazor: **Work Orders**). Anyone with access
reports a problem. A resident's report is linked to the resident whose email
matches their account (users without such a record are asked to have the
manager register them first); managers may report on a resident's behalf or
for themselves. References are numbered per building and year
(`WO-2026-0001`). Emergency urgency starts the 48-hour intervention window,
shown as a countdown. Managers change the status with an optional note
(`PATCH .../{id}/status`); a status change notifies the reporting resident
in-app. Status changes and notes are kept as the work order's history, and
completed or cancelled work orders are final. Not yet implemented: photos,
contractor assignment and costs, PDF export.

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
notices/minutes to owners by email, tracking owners who miss sessions.

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

Email and SMS go through `IEmailSender`/`ISmsSender`. Configure Azure
Communication Services to send for real: `Email:ConnectionString` +
`Email:SenderAddress` (verified sender) and `Sms:ConnectionString` +
`Sms:From` (E.164; SMS costs apply). Without that, emails are logged in
Development and dropped with a warning elsewhere; SMS are logged. Email/SMS
fan-out on events, delivery preferences and quiet hours are not implemented yet.

### Background jobs

A singleton scheduler grain (`ISchedulerGrain`, key "global") runs on an
Orleans reminder every 5 minutes (backed by the reminder table in
`building_manager_orleans`, so schedules survive restarts; each silo wakes it
on startup so the reminder is registered):

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
`Storage:ContainerName`; Azurite locally via `docker compose up -d`).
Full-text search and shareable public links are not implemented yet.

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
- `POST fee-structure`: name, amount per m², fixed amount, optional
  `effectiveFrom` date (today or later; omitted = now, counting for the
  current month). A fee scheduled for the same date replaces the earlier one.
  `GET fee-structures` lists them (scheduled, in force, replaced); Blazor's
  **Fee settings** tab shows the list and schedules changes up to 12 months ahead.

Payment reminders and financial-report exports beyond the fee status are not
implemented yet.

## Running in production (Azure)

One deployable: the API container, which is also an Orleans silo. Every
replica joins the cluster through the membership table in PostgreSQL and keeps
grain state there, so the API scales out (Container Apps: 1–3 replicas by
default).

- **Template**: `infra/azure/main.bicep` creates
  - Container Apps (API) and Container Registry
  - PostgreSQL Flexible Server (B1ms, PostgreSQL 16, 7-day backups)
  - Storage account with the documents container
  - Key Vault with the JWT secret, admin seeding credentials and the database
    and blob connection strings; the API reads them at startup with its
    user-assigned managed identity (`KeyVault__VaultUri`, `AZURE_CLIENT_ID`)
  - Log Analytics and Application Insights
  - Static Web Apps (client)
- **Deploy**: `.github/workflows/building-manager-deploy.yml` at the
  repository root (manual run). In order, it:
  - deploys the template
  - builds the image in ACR
  - applies the Orleans schema (`infra/orleans-sql/apply.sh`, opening the
    database firewall to the runner only while it runs)
  - rolls out the new image
  - publishes the client with the API address
- **One-time setup for deploying**: an Entra app registration with a federated
  credential for this repository (environment `production`) and Contributor +
  User Access Administrator on the subscription or resource group (the
  template assigns roles to the API's identity); repository secrets
  `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
  `POSTGRES_ADMIN_PASSWORD`, `JWT_SECRET` (at least 32 characters) and,
  to seed an admin, `ADMIN_EMAIL` and `ADMIN_PASSWORD`.
- **CI**: `.github/workflows/building-manager-ci.yml` builds, tests, lints the
  template and builds both images on every push/PR.
- **Configuration**: secrets come from Key Vault; other settings are
  environment variables (`Orleans__ClusterId`, `Storage__ContainerName`,
  `APPLICATIONINSIGHTS_CONNECTION_STRING`, `Cors__AllowedOrigins__0`,
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED`). Telemetry is only sent when the
  Application Insights connection string is set.
- **Shutdown**: give containers at least 40 s to stop (compose
  `stop_grace_period`, Container Apps `terminationGracePeriodSeconds: 45`).
  A silo that leaves gracefully hands its grains over without errors. A
  replica that is killed outright is declared dead by the others within
  about a minute.
- **Single replica**: a replacement started after a crash waits up to about
  a minute for the dead silo to expire (`Orleans:IAmAlivePublishSeconds`,
  default 30). Two replicas avoid that gap.
- Not yet exercised against a live subscription: expect small adjustments on
  the first deployment, and check that replicas reach each other on the silo
  port (11111) when running more than one.

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for the original design. Where it
differs from this README (separate silo host, Redis, SQLite offline sync,
Orleans streams), the README describes what is implemented.

## Key Features

- Orleans virtual actors for building management, scaled out over PostgreSQL
- Blazor WebAssembly client (MAUI/offline mobile app not implemented yet)
- Serbian housing law rules for assemblies (quorum, majorities)

## Requirements

- .NET 8 SDK
- Docker Desktop
- Visual Studio 2022 / Rider / VS Code with C# extension
