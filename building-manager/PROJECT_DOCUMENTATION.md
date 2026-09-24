# Building Manager Application - Technical Project Documentation

**Version:** 2.0 (C# Stack)  
**Last Updated:** 2024  
**Status:** Ready for Development

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Technology Stack](#technology-stack)
3. [System Architecture](#system-architecture)
4. [Orleans Grain Design](#orleans-grain-design)
5. [Core Modules Overview](#core-modules-overview)
6. [API Design](#api-design)
7. [Security & Compliance](#security--compliance)
8. [Data Management](#data-management)
9. [Mobile Application](#mobile-application)
10. [Azure Deployment](#azure-deployment)
11. [Testing Strategy](#testing-strategy)
12. [Implementation Roadmap](#implementation-roadmap)

---

## Executive Summary

### Project Scope

The Building Manager Application is a mobile-first digital platform for managing residential buildings in Serbia. It serves two primary user types:

| User Type | Description | Key Needs |
|-----------|-------------|-----------|
| Regular Manager | Owner-elected volunteer | Simple tools, offline access, easy to use |
| Professional Manager | Licensed professional | Multi-building support, compliance tracking, 24/7 logging |

### Key Technical Requirements

| Requirement | Priority | Description |
|-------------|----------|-------------|
| Mobile-first | Critical | Blazor Hybrid (iOS, Android, Windows) |
| Offline capability | Critical | Core features work without internet |
| Serbian law compliance | Critical | Voting rules, deadlines, professional manager requirements |
| Multi-tenant | High | Support multiple buildings per manager |
| Real-time sync | High | Changes propagate immediately when online |
| Scalability | High | Orleans virtual actors for horizontal scaling |

### Success Metrics

| Metric | Target |
|--------|--------|
| App launch time | < 3 seconds |
| Screen load time | < 2 seconds |
| API response time | < 200ms (p95) |
| Offline data access | < 1 second |
| Uptime | 99.5% |

---

## Technology Stack

### Backend

| Component | Technology | Rationale |
|-----------|------------|-----------|
| Runtime | **.NET 8** | Latest LTS, excellent performance |
| Framework | **ASP.NET Core** | Industry standard for .NET |
| Actor Model | **Orleans 8** | Virtual actors, auto-scale, persistence |
| ORM | **Entity Framework Core 8** | Type-safe, migrations, LINQ |
| Database | **PostgreSQL** | Relational, JSON support, Azure-compatible |
| Cache | **Redis** | Session, grain storage, caching |
| File Storage | **Azure Blob Storage** | Scalable, cost-effective |

### Mobile

| Component | Technology | Rationale |
|-----------|------------|-----------|
| Framework | **.NET MAUI** | Single codebase for iOS/Android/Windows |
| UI | **Blazor Hybrid** | Shared web/mobile UI components |
| Component Library | **MudBlazor** | Material Design, accessible |
| Local DB | **SQLite** | Offline data storage |
| State Management | **CommunityToolkit.Mvvm** | MVVM pattern |

### Infrastructure

| Component | Technology | Rationale |
|-----------|------------|-----------|
| Hosting | **Azure App Service** | Managed, auto-scale |
| Database | **Azure Database for PostgreSQL** | Managed PostgreSQL |
| Cache | **Azure Cache for Redis** | Managed Redis |
| Storage | **Azure Blob Storage** | Files, photos |
| Monitoring | **Application Insights** | Integrated monitoring |
| CI/CD | **GitHub Actions** | Integrated with code |

### Development Tools

| Tool | Purpose |
|------|---------|
| Visual Studio 2022 | IDE |
| Rider | Alternative IDE |
| dotnet EF | Migrations |
| Orleans Dashboard | Grain monitoring |
| Serilog | Structured logging |
| xUnit | Testing |
| Moq | Mocking |

---

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                           CLIENT LAYER                               │
├─────────────────┬─────────────────┬─────────────────────────────────┤
│   Android App   │    iOS App      │        Windows App              │
│   (MAUI)        │    (MAUI)       │        (MAUI)                   │
├─────────────────┴─────────────────┴─────────────────────────────────┤
│                    Shared Blazor UI Components                       │
│              (Dashboard, Residents, Keys, Work Orders, etc.)         │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             │ Orleans Client
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        ORLEANS SILO CLUSTER                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   ┌──────────────┐ ┌──────────────┐ ┌──────────────┐               │
│   │BuildingGrain │ │  UserGrain   │ │DeadlineGrain │  ...          │
│   │  (per bldg)  │ │  (per user)  │ │ (per bldg)   │               │
│   └──────────────┘ └──────────────┘ └──────────────┘               │
│                                                                      │
│   ┌────────────────────────────────────────────────────────────┐    │
│   │  Streams: Real-time notifications to connected clients     │    │
│   └────────────────────────────────────────────────────────────┘    │
│                                                                      │
│   ┌────────────────────────────────────────────────────────────┐    │
│   │  Reminders: Deadline alerts, overdue keys, 48h tracking   │    │
│   └────────────────────────────────────────────────────────────┘    │
│                                                                      │
└────────────────────────────┬────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          DATA LAYER                                  │
├─────────────────┬─────────────────┬─────────────────────────────────┤
│   PostgreSQL    │     Redis       │    Azure Blob Storage           │
│   (Grain State) │  (Cache/Queue)  │    (Files/Images)               │
└─────────────────┴─────────────────┴─────────────────────────────────┘
```

### Orleans Benefits

| Feature | Benefit |
|---------|---------|
| Virtual Actors | Each building = independent grain, no explicit lifecycle |
| Auto-Scale | Silo automatically distributes grains across cluster |
| Persistence | Grain state automatically saved to PostgreSQL |
| Reminders | Built-in durable timers for deadlines |
| Streams | Pub/sub for real-time notifications |
| Geo-Distribution | Can deploy silos in multiple Azure regions |

---

## Orleans Grain Design

### Core Grains

| Grain | Key | Purpose |
|-------|-----|---------|
| `IBuildingGrain` | buildingId | All building operations |
| `IUserGrain` | userId | User session, permissions |
| `INotificationGrain` | buildingId | Push notifications |
| `IDeadlineGrain` | buildingId | Legal deadline tracking |
| `IAssemblyGrain` | assemblyId | Meeting and voting |

### Building Grain (Primary)

```csharp
public interface IBuildingGrain : IGrainWithStringKey
{
    // State
    Task<BuildingState> GetStateAsync();
    Task InitializeAsync(BuildingInfo info);
    
    // Units
    Task<Unit> GetUnitAsync(int unitNumber);
    Task<IEnumerable<Unit>> GetAllUnitsAsync();
    Task AddOrUpdateUnitAsync(Unit unit);
    
    // Residents
    Task<IEnumerable<Resident>> GetResidentsAsync();
    Task AddResidentAsync(Resident resident);
    
    // Keys
    Task<Key> CheckoutKeyAsync(Guid keyId, KeyCheckoutInfo checkout);
    Task<Key> CheckinKeyAsync(Guid keyId, string condition, string? notes);
    Task<IEnumerable<Key>> GetOverdueKeysAsync();
    
    // Work Orders
    Task<WorkOrder> CreateWorkOrderAsync(CreateWorkOrderRequest request);
    Task<WorkOrder> UpdateWorkOrderStatusAsync(Guid id, WorkOrderStatus status);
    
    // Assembly & Voting
    Task<QuorumStatus> GetQuorumStatusAsync(Guid assemblyId);
    Task RecordVoteAsync(Guid assemblyId, Guid itemId, VoteRecord vote);
    
    // Finance
    Task<FinancialSummary> GetFinancialSummaryAsync();
}
```

---

## Core Modules Overview

### Module Summary

| # | Module | MVP | Description |
|---|--------|-----|-------------|
| 1 | Building & Units | ✓ | Building profile, unit registry |
| 2 | Residents | ✓ | Owner, tenant, family member management |
| 3 | Keys | ✓ | Inventory, checkout/checkin, history |
| 4 | Maintenance | ✓ | Work orders, tracking, history |
| 5 | Communication | ✓ | Announcements, notifications |
| 6 | Assembly | ✓ | Meetings, voting, minutes |
| 7 | Finance | ✓ | Fees, payments, expenses |
| 8 | Documents | Phase 2 | File storage, sharing |
| 9 | Service Providers | Phase 2 | Contractor directory |
| 10 | Deadlines | Phase 2 | Legal deadline tracking |
| 11 | Professional Manager | Phase 2 | License, insurance, 24/7 logs |
| 12 | Reports | Phase 3 | Semi-annual, financial reports |
| 13 | Multi-Building | Phase 4 | Portfolio management |

---

## API Design

### REST API (Optional - primarily Orleans)

When REST API is needed (third-party integrations):

```
GET    /api/v1/buildings
POST   /api/v1/buildings
GET    /api/v1/buildings/{id}
PUT    /api/v1/buildings/{id}

GET    /api/v1/buildings/{id}/residents
POST   /api/v1/buildings/{id}/residents

GET    /api/v1/buildings/{id}/keys
POST   /api/v1/keys/{id}/checkout
POST   /api/v1/keys/{id}/checkin

GET    /api/v1/buildings/{id}/work-orders
POST   /api/v1/buildings/{id}/work-orders
```

### Standard Response Format

```json
{
  "success": true,
  "data": { },
  "meta": {
    "timestamp": "2024-01-15T10:30:00Z",
    "requestId": "req-123456"
  }
}
```

---

## Security & Compliance

### Authentication & Authorization

| Aspect | Implementation |
|--------|---------------|
| Authentication | JWT + ASP.NET Core Identity |
| Password Storage | BCrypt/Argon2 |
| MFA | Authenticator app (Phase 2) |
| Authorization | Role-based + claims |

### Role Permissions Matrix

| Resource | Admin | Manager | Owner | Tenant |
|----------|-------|---------|-------|--------|
| Building CRUD | ✓ | ✓ (assigned) | View own | View own |
| Residents | Full | Full | View all | View limited |
| Keys | Full | Full | Own unit | None |
| Work Orders | Full | Full | Create/View own | Create/View own |
| Assemblies | Full | Full | Vote | View |
| Finance | Full | Full | View building | View own |

### Serbian Law Compliance

| Requirement | Implementation |
|-------------|----------------|
| 3-day assembly notice | System enforces minimum, warns if violated |
| Quorum calculation | Auto-calculated from ownership shares |
| Voting thresholds | Validated before recording decision |
| 48-hour emergency | Countdown timer, escalating alerts |
| 24/7 logging | All reports timestamped, stored |
| License tracking | Expiration alerts at 90/60/30 days |

---

## Data Management

### Grain Persistence

Orleans grains persist state automatically to PostgreSQL:

```csharp
// Silo configuration
siloBuilder.AddAdoNetGrainStorage("Default", options =>
{
    options.ConnectionString = connectionString;
    options.Invariant = "Npgsql";
});
```

### Offline Data Strategy

**Cached Entities (SQLite on device):**
- Building data
- Units
- Residents
- Keys
- Work orders (active)
- Announcements (recent)

**Sync Rules:**
- Full sync on first login
- Delta sync on subsequent opens
- Background sync every 5 minutes
- Immediate sync after mutations when online
- Queue mutations when offline

---

## Mobile Application

### Screen Structure

```
App
├── Auth
│   ├── Login
│   ├── Register
│   └── ForgotPassword
├── Main (Tab Navigator)
│   ├── Dashboard
│   ├── Residents
│   ├── Keys
│   ├── Work Orders
│   └── More
│       ├── Announcements
│       ├── Assemblies
│       ├── Finance
│       ├── Documents
│       ├── Settings
│       └── Profile
└── Modals
    ├── AddResident
    ├── KeyCheckout
    ├── CreateWorkOrder
    └── CreateAnnouncement
```

### Offline Capabilities

| Feature | Offline | Notes |
|---------|---------|-------|
| View residents | ✓ | Cached locally |
| Add resident | ✓ | Queued for sync |
| View keys | ✓ | Cached |
| Checkout key | ✓ | Queued |
| Create work order | ✓ | Queued |
| Create announcement | ✗ | Requires network |
| Vote | ✗ | Requires network |

---

## Azure Deployment

### Infrastructure

```bicep
// Resources
- Azure App Service (Silo)
- Azure Database for PostgreSQL
- Azure Cache for Redis
- Azure Blob Storage
- Application Insights
- Azure Key Vault
```

### Deployment Commands

```bash
# Create infrastructure
az deployment sub create \
  --template-file infra/azure/main.bicep \
  --parameters appName=buildingmanager location=westeurope

# Deploy silo
az webapp deployment source config-zip \
  --resource-group building-manager-rg \
  --name buildingmanager-silo \
  --src silo.zip
```

---

## Testing Strategy

### Test Pyramid

```
        ┌─────────────────┐
        │   E2E Tests     │  5%
        │  (Playwright)   │
        ├─────────────────┤
        │ Integration     │  15%
        │ Tests (xUnit)   │
        ├─────────────────┤
        │   Unit Tests    │  80%
        │   (xUnit+Moq)   │
        └─────────────────┘
```

### Orleans Testing

```csharp
// Grain unit test
[Fact]
public async Task CheckoutKey_WhenAvailable_MarksAsCheckedOut()
{
    // Arrange
    var grain = new BuildingGrain();
    
    // Act
    await grain.CheckoutKeyAsync(keyId, checkoutInfo);
    
    // Assert
    var key = await grain.GetKeyAsync(keyId);
    Assert.Equal(KeyStatus.CheckedOut, key.Status);
}
```

---

## Implementation Roadmap

### Phase 1: MVP (Months 1-4)

**Sprint 1-2: Foundation**
- [ ] Solution setup, CI/CD
- [ ] Orleans silo configuration
- [ ] Database schema (EF Core)
- [ ] Authentication

**Sprint 3-4: Building & Residents**
- [ ] Building grain
- [ ] Unit management
- [ ] Resident management
- [ ] Offline storage

**Sprint 5-6: Keys & Maintenance**
- [ ] Key inventory
- [ ] Checkout/checkin workflow
- [ ] Work order lifecycle
- [ ] Photo upload

**Sprint 7-8: Communication & Assembly**
- [ ] Announcements
- [ ] Orleans streams for notifications
- [ ] Assembly scheduling
- [ ] Basic voting

### Phase 2: Legal Compliance (Months 5-7)

- Deadline tracking with reminders
- Enhanced voting (thresholds, quorum)
- Unavailable owner tracking
- Professional manager features
- Emergency 48-hour tracking

### Phase 3: Enhanced Features (Months 8-10)

- Service provider directory
- Fee proposals (3 bids)
- Semi-annual reports
- Owners' rules workflow
- Multi-language support

### Phase 4: Scale & Platform (Months 11-12)

- Portfolio dashboard
- Forced administration
- Substitute manager
- Online payment
- Advanced analytics

---

*Document Version: 2.0 (C# Stack)*  
*Last Updated: 2024*
