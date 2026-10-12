# Gap Analysis — Missing Features & Azure Integrations

**Updated:** 2026-10-12
**Compared:** `BUSINESS_REQUIREMENTS.md` (v2.0, UC-001..UC-034), `PROJECT_DOCUMENTATION.md`,
`IMPLEMENTATION_PLAN.md`, `technical/openapi.yaml`, `technical/prisma/schema.prisma`
vs. `building-manager-dotnet/` at commit `bf2ee24`.
Paths below are relative to `building-manager-dotnet/src/` unless stated otherwise; line
numbers refer to commit `bf2ee24`. Uncommitted work in progress at the time of writing
(building documents on Blob Storage — `DocumentsController`, `Infrastructure/Storage/`) is
not counted.

---

## Current state in one paragraph

The backend covers the P0 core: auth (JWT + rotating refresh tokens, password reset,
session revocation, rate-limited auth endpoints), buildings/units/residents, keys, work
orders, owners' assemblies with Serbian quorum/majority rules, per-month finance with
per-unit fee standing, and announcements. All state lives in Orleans grains persisted to
PostgreSQL (ADO.NET); 127 tests pass. The web client covers finance, assemblies and
announcements fully, but units, residents, keys and work orders are read-only tables, so a
building can only be set up through Swagger. Nothing time-based fires on its own, nothing is
delivered (email/SMS/push), there is no file storage, PDF/Excel, mobile app, IaC or CI/CD,
and the current Orleans topology only runs on a single machine.

---

## 1. Defects found (quick fixes)

| # | Defect | Where |
|---|---|---|
| 1 | Work-order status `notes` are accepted but never stored | `BuildingManager.Orleans.Grains/Grains/BuildingGrain.cs:418-435` |
| 2 | API connects to the silo with static loopback clustering → API and silo must share one host, no scale-out | `BuildingManager.Api/Program.cs:200` |
| 3 | Silo also binds the Orleans silo/gateway ports in Kestrel ("co-hosting" comment is wrong; Orleans opens its own sockets). Overlapping listeners likely fail on Linux/containers, and the silo has no HTTP/health endpoint | `BuildingManager.Orleans.Silo/Program.cs:25-31` |
| 4 | `DevLoggingEmailSender` registered in every environment: reset tokens go to production logs, users never receive the email | `BuildingManager.Api/Program.cs:161` |
| 5 | Dev `Jwt:Secret` committed and accepted in Production (passes the 32-char check). Fail startup in Production if unchanged | `BuildingManager.Api/appsettings.json:35` |
| 6 | `/health` has no checks (no Orleans, no Postgres) — always healthy | `BuildingManager.Api/Program.cs:61,219` |
| 7 | Orleans dashboard unauthenticated, listening on all interfaces | `BuildingManager.Orleans.Silo/Program.cs:66-71` |
| 8 | Fee settings can't show the current fee (no GET endpoint); fee changes are always effective from now | `BuildingManager.Blazor/Pages/Finance.razor:135`, `BuildingManager.Api/Controllers/BuildingsController.cs:408` |
| 9 | Buildings list "view" button is `<MudButton Icon=…>` (not a MudButton parameter) → likely an empty button; should be `MudIconButton` | `BuildingManager.Blazor/Pages/Buildings.razor:34` |
| 10 | Work-order filter only offers All/New/In Progress/Completed (Acknowledged, WaitingParts, WaitingDecision, Cancelled unreachable) | `BuildingManager.Blazor/Pages/WorkOrders.razor:19-21` |
| 11 | Session grains never clear their state → session rows accumulate forever | `BuildingManager.Orleans.Grains/Grains/UserSessionGrain.cs` |
| 12 | Grain methods with no endpoint: overdue keys, key history, mark key lost/damaged, update building info, remove unit/resident, edit/delete payments and expenses | `IBuildingGrain` vs `BuildingsController` |

---

## 2. Security & privacy gaps

- **Anyone can self-register as Manager or SubstituteManager**
  (`BuildingManager.Api/Controllers/AuthController.cs:41-47`). Managers should be invited or
  approved (and license-checked, UC-014).
- **No company isolation:** `CompanyAdmin` bypasses building access for *every* building
  (`BuildingManager.Api/Authorization/BuildingAccessHandler.cs:34`). There is no
  Company/Organisation entity, so one management company's admin sees all others' buildings.
- **One global role per account:** a person can't be Owner in one building and Manager of
  another. Roles should belong to the building membership, not the account.
- **Over-broad reads:** every member, tenants included, can read the full resident directory
  (names, phones, emails), key holders and all work orders. Needs role-filtered reads for
  ZZPL (Zakon o zaštiti podataka o ličnosti — Serbia's GDPR equivalent).
- **Session validation fails open** when the session grain is unreachable, so revoked tokens
  keep working until expiry (`BuildingManager.Api/Program.cs:114-120`) — a deliberate
  trade-off worth revisiting.
- Tokens stored in `localStorage` with no CSP or security headers; no HSTS; no forwarded
  headers (behind Azure ingress the per-IP auth rate limiter sees only the proxy IP).
- Missing: email verification, MFA, account lockout (only per-IP limiting), audit log (votes,
  finance, keys, delegation), data export/erasure, retention policy, privacy notice.

---

## 3. Missing features

### 3.1 Use-case scorecard

Status: **Done** · **Mostly done** · **Partial** · **Backend only** (API/grain, no UI) · **Missing**

| UC | Feature | Pri | Status | What's missing |
|----|---------|-----|--------|----------------|
| 001 | Resident directory | P0 | Partial | Search/sort/filter, tap-to-call, emergency contacts, offline cache, role-filtered visibility |
| 002 | Key handout | P0 | Backend only | UI, receipt (PDF/SMS), ID photo, overdue alerts (grain method has no endpoint) |
| 003 | Resident maintenance request | P0 | Backend only | Resident "report a problem" form, photos, push to manager, offline queue |
| 004 | Manage work orders | P0 | Partial | Status/notes/assignment/cost UI, notes persisted (defect 1), photos, readable numbers (WO-2026-0001), notifications, PDF export |
| 005 | Send announcement | P0 | Mostly done | Recipient targeting (owners/tenants/units), email/SMS/push delivery, delivery/read stats, attachments |
| 006 | Record assembly meeting | P0 | Mostly done | PDF minutes + distribution to owners |
| 007 | Notify about assembly | P0 | Partial | Actual notice delivery, reminders (2 days / 1 day / morning of), per-owner delivery tracking, proxy-form PDF, agenda attachments |
| 008 | Track fee payments | P0 | Mostly done | Reminders, unpaid list PDF/Excel, payment confirmation download |
| 009 | Register new resident | P0 | Backend only | UI, ownership shares = 100% validation, move-in/out with history, account invitation, ID upload |
| 010 | Return key | P0 | Backend only | UI, "Keys due" list, lost key → replacement task |
| 011 | Register residential community (SZ) | P1 | Missing | Auto-name "SZ [Street] [Number]", decision upload, 15-day countdown, application PDF, MB/PIB/account milestones |
| 012 | Legal deadlines | P1 | Missing | Deadline engine, legal deadline table, 7/3/1-day reminders, escalation, custom deadlines |
| 013 | 24/7 problem reports | P1 | Missing | Fast intake log, response-time tracking, feed into semi-annual report |
| 014 | Manager license & insurance | P1 | Missing | 10-year license, ≥ €10k insurance, 90/60/30-day alerts, company compliance view |
| 015 | Correct majority voting | P1 | Done | `BuildingManager.Core/Rules/AssemblyRules.cs` |
| 016 | Unavailable owners | P1 | Missing | Flag after 3 missed sessions, exclude from quorum, auto-restore |
| 017 | Forced administration | P1 | Missing | Assignment, government reports, handover/archive, appeal |
| 018 | Fee structure | P1 | Partial | Read current fee, future effective dates, categories, quarterly/annual, projection preview |
| 019 | Financial summary | P1 | Partial | Breakdowns/charts, PDF/Excel, owner building totals, annual statement |
| 020 | Fee proposal with 3 bids | P2 | Missing | All |
| 021 | Semi-annual report | P2 | Missing | All |
| 022 | Owners' Rules (Pravilnik) | P2 | Missing | All |
| 023 | 48-hour emergency | P2 | Partial | Deadline logic exists only in the unused Core entity; no countdown, escalation or compliance report |
| 024 | Service providers | P2 | Missing | All |
| 025 | Record expense | P2 | Partial | Receipt upload, link to provider/work order, accounting export |
| 026 | Building documents | P2 | Missing | All |
| 027 | Reserve common areas | P3 | Missing | All |
| 028 | Online payment | P3 | Missing | All |
| 029 | Digital voting | P3 | Missing | Owners vote themselves (today the manager enters every vote), re-auth, live tally |
| 030 | Multi-building dashboard | P3 | Partial | Portfolio metrics, cross-building alerts, consolidated finance, bulk actions |
| 031 | Substitute delegation | P3 | Partial | Date range, scope, power-of-attorney upload, audit, revocation |
| 032 | Financial report generator | P3 | Missing | All |
| 033 | Notification preferences | P3 | Missing | All |
| 034 | Offline sync | P3 | Missing | All |

Unnumbered roadmap items: in-app messaging with the manager, public API + webhooks, bulk
import, advanced analytics.

### 3.2 Gaps beyond the use cases

- **Manager setup screens (P0 blocker):** add/edit/remove units, residents, keys; edit building.
  Finance and assemblies both need units with ownership shares, which today only Swagger can create.
- **Finance:**
  - corrections and reversals (storno) for payments and expenses, with an audit trail
  - Serbian fee components: current maintenance, investment maintenance, manager fee
  - running balance and total arrears per unit
  - opening balances when taking over a building
  - one payment covering several months
  - advance payments / credit carried forward
  - optional statutory late interest
- **Accounts and admin:** user management UI (list, deactivate, change role), profile editing,
  resident ↔ account invitation flow.
- **Localization:** Serbian in Latin and Cyrillic, plus English, with Serbian date formats.
  Today only RSD formatting exists.
- **Lists:** pagination on every list endpoint.

---

## 4. Platform & architecture gaps

- **No scheduler.** Scheduled announcements, overdue keys, 48-hour deadlines and assembly
  dates are only evaluated when someone reads them. `Microsoft.Orleans.Reminders` is
  referenced but no reminder service is configured.
- **No real-time.** No Orleans streams, grain observers or SignalR, despite README/ARCHITECTURE claims.
- **`BuildingGrain` state grows without bound.** Units, residents, keys (with full checkout
  history), every work order and announcements live in one state blob rewritten on every
  change; work orders are never pruned. Split work orders into per-building-per-year grains,
  as was done for finance.
- **No read model.** Portfolio dashboards and reports (UC-019/030/032) would have to activate
  every grain. Project domain events into Postgres tables — the unused EF Core layer could
  become this read model — or delete that layer.
- **Dead code:**
  - Core entities, value objects and repository interfaces
  - the Infrastructure EF layer: no `AddDbContext`, no migrations, empty `Repositories/`
  - unused packages: MediatR, FluentValidation, AutoMapper, Ardalis.*, StackExchange.Redis,
    Azure.Storage.Blobs, App Configuration, HealthChecks.UI, Moq, Testcontainers
  - the MAUI project: not in the solution, no `MauiProgram.cs`
  - the Blazor project-root `appsettings.json`
  - `tests/BuildingManager.Tests/Domain/*`, which tests the dead entities
- **Mobile.** A PWA on the existing Blazor WASM client (service worker, installable, offline
  cache, web push) is far cheaper than finishing MAUI and covers most of UC-034's intent.
- **Ops:**
  - Orleans SQL schema scripts aren't in the repo (manual bootstrap).
  - No Dockerfiles; `infra/azure/` is empty; no CI/CD.
  - No API integration or UI tests.
  - `ARCHITECTURE.md` and the README "Key Features" describe Redis, Blob, SQLite offline
    sync, streams and "full Serbian law compliance", none of which exist.

---

## 5. Azure integrations

### Tier 1 — required to run in production

| Service | Purpose | Hook / note |
|---|---|---|
| **Container Apps** (or App Service + VNet) | Host the API with the silo co-hosted in-process (`UseOrleans` in the API) — fixes defects 2 and 3; scale out on the existing ADO.NET Postgres clustering | Optionally .NET Aspire AppHost + `azd` to generate Bicep and replace docker-compose locally |
| **Database for PostgreSQL – Flexible Server** | Orleans clustering, grain storage, reminders (`Microsoft.Orleans.Reminders.AdoNet`); PITR and geo-redundant backups for legal records | Silo already uses Npgsql via ADO.NET — repoint the connection string |
| **Key Vault + managed identity** | JWT signing key (optionally RS256 with a Key Vault key), DB password, ACS keys | `Azure.Identity` referenced, unused |
| **Static Web Apps** | Host the Blazor WASM client with custom domain and CDN | Client is already a static publish |
| **Application Insights** | Azure Monitor OpenTelemetry distro + Orleans activity propagation; availability tests and alerts toward the 99.5% target | Replaces the declared-but-unused Serilog App Insights sink |
| **GitHub Actions (OIDC) + Container Registry + Bicep** | Build, test, deploy, apply the Orleans schema | `infra/azure/` exists but is empty |

### Tier 2 — unlock specced features

| Service | Unlocks | Hook / note |
|---|---|---|
| **Communication Services – Email** | Password reset (broken in prod today), assembly notices, minutes, reminders, announcements | Drop-in behind `IEmailSender` |
| **Communication Services – SMS** | Emergency announcements, 24h escalation (UC-023), SMS receipts (UC-002) | Verify delivery and alphanumeric sender ID for +381; fall back to a regional gateway (e.g. Infobip) behind the same interface |
| **Blob Storage** | Documents (UC-026), work-order photos, receipts, generated PDFs | Config + Azurite already present. User-delegation SAS, lifecycle tiers, immutability (WORM) for minutes and finance records, soft delete, Defender for Storage malware scanning |
| **Orleans Reminders** (on Postgres) | The scheduler for UC-007/008/012/014/023, scheduled announcement delivery, session cleanup | Package already referenced — needs config + `IRemindable` grains |
| **Container Apps Jobs / Functions** | Batch work: monthly reports, statement import | Only where reminders don't fit |
| **Service Bus** | Transactional outbox for domain events → notification fan-out with retries; webhooks (Sprint 16) | Keeps email/SMS I/O out of grain calls |
| **SignalR Service** | Live announcements, work-order status, live vote tally (UC-029) | Fed by Orleans streams or grain observers |
| **Notification Hubs** | Native push (FCM v1 / APNs) | For PWA web push, check Notification Hubs browser-push support or use VAPID directly |
| **App Configuration** | Feature flags per building (SMS on/off, beta features), central config | Package declared, referenced by no project |
| **Entra External ID** (optional) | MFA, email verification, social/passwordless sign-in, step-up auth for digital voting | Azure AD B2C is closed to new customers; trade-off: migrating self-rolled accounts |

### Tier 3 — value-add and AI

- **AI Document Intelligence:** scan receipts/invoices into expense drafts (UC-025); parse
  bank statement PDFs. Avoid storing ID document scans (ZZPL data minimisation).
- **Azure OpenAI (AI Foundry):**
  - draft announcements and notices in Serbian
  - turn assembly notes into draft minutes
  - triage resident reports by category and urgency
  - Q&A over the Pravilnik and building documents (RAG with AI Search)
  - Content Safety for resident-submitted text
- **AI Speech:** dictation for the 24/7 problem log (UC-013's "logged in under 60 seconds").
- **AI Translator:** English versions of announcements for foreign tenants. Serbian
  Latin↔Cyrillic is a fixed transliteration and needs no service.
- **AI Search:** document full-text search (UC-026), if Postgres full-text isn't enough.
- **Azure Maps:** geocode buildings, portfolio map (UC-030).
- **Power BI Embedded:** portfolio analytics (Sprint 16), later.
- **Azure Load Testing:** validate p95 < 200 ms and 1,000 req/s.
- **Front Door + WAF:** edge TLS, WAF and rate limiting shared across instances (the
  current limiter is in-memory per instance).

### Skip for now

- **Redis** — Orleans grains already act as the in-memory cache, and SignalR Service
  covers the backplane. Remove the Redis container, package and connection strings.
- **Cosmos DB, AKS** — cost and complexity without benefit at this scale.

**Region:** there is no Azure region in Serbia; use a nearby EU region (e.g. Poland
Central, Italy North, Germany West Central). Confirm with counsel that ZZPL treats
transfers to the EU as adequate.

---

## 6. Non-Azure integrations that matter in Serbia

- **NBS IPS QR code on monthly payment slips (uplatnica)** per unit, with a structured
  reference number (poziv na broj, model 97) per unit and month. High value, low cost.
- **Bank statement import** (bank XML/CSV export) matched on that reference to record
  payments automatically — the "checking bank statements" pain point in the BR.
- **SEF (Sistem elektronskih faktura):** check whether residential communities must receive
  supplier e-invoices through SEF; if so, import them as expenses via the SEF API.
- **PDF / Excel:** QuestPDF (check the community-licence revenue threshold) and ClosedXML.
- **Online card payments (UC-028):** Serbian bank e-commerce gateways — outside Azure.

---

## 7. Suggested order

1. **Fix and unblock:** section 1 defects + manager screens for units, residents, keys and
   work orders (no more Swagger).
2. **Production foundation:**
   - co-host the silo in the API
   - Key Vault
   - OpenTelemetry / App Insights and real health checks
   - Dockerfile, Bicep/azd, GitHub Actions
   - Postgres Flexible and Static Web Apps
3. **Notifications:** ACS Email first (password reset), with an outbox and Orleans
   reminders; then recipient targeting, SMS, assembly notices and reminders.
4. **Documents and reports:** Blob plus QuestPDF/ClosedXML — minutes, payment
   confirmations, unpaid list, financial summary.
5. **Serbian payments:** IPS QR slips and bank statement import.
6. **P1 compliance:** deadline engine, licence and insurance, 24/7 log, SZ registration,
   semi-annual report.
7. **Later:** PWA with push, digital voting with External ID MFA, AI add-ons.
