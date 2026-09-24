# Building Manager - Implementation Plan

**Generated:** 2024-02-18  
**Stack:** C# / .NET 8 / Orleans / Blazor Hybrid / Azure

---

## Code Review Summary

### Current State

| Component | Status | Completion |
|-----------|--------|------------|
| Solution file | ✅ Complete | 100% |
| .csproj files (8) | ✅ Complete | 100% |
| Grain interfaces | ⚠️ Partial | 40% |
| Source code files | ❌ Missing | 0% |
| Test projects | ❌ Missing | 0% |
| Docker compose | ✅ Complete | 100% |
| Azure infra | ❌ Missing | 0% |

### Critical Gaps

1. **No source code** - All projects empty except interfaces
2. **Missing grain implementations** - BuildingGrain, UserSessionGrain, etc.
3. **Missing domain entities** - Core layer empty
4. **Missing infrastructure** - DbContext, repositories, migrations
5. **Missing tests** - Test project doesn't exist
6. **Missing configuration** - No appsettings.json files

---

## Implementation Plan

### Phase 1: Foundation (Week 1-2)

#### 1.1 Project Setup Fixes

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create Directory.Build.props for shared settings | P0 | 15 min |
| Create Directory.Packages.props for central packages | P0 | 15 min |
| Fix test project - add .csproj | P0 | 30 min |
| Add appsettings.json to all projects | P0 | 30 min |
| Create .editorconfig | P1 | 15 min |

#### 1.2 Core Layer

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create BaseEntity with Id, CreatedAt, UpdatedAt | P0 | 15 min |
| Create Building aggregate root | P0 | 1 hour |
| Create Unit entity | P0 | 30 min |
| Create Resident entity | P0 | 30 min |
| Create Key entity | P0 | 30 min |
| Create WorkOrder entity | P0 | 30 min |
| Create Announcement entity | P0 | 30 min |
| Create Assembly/AgendaItem/Vote entities | P0 | 1 hour |
| Create Payment/Expense entities | P0 | 30 min |
| Create value objects (Address, Money, Percentage) | P1 | 1 hour |
| Create domain exceptions | P1 | 30 min |
| Create domain events | P2 | 1 hour |

#### 1.3 Infrastructure Layer

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create BuildingManagerDbContext | P0 | 1 hour |
| Create DbSet configurations | P0 | 1 hour |
| Create entity configurations (FluentAPI) | P0 | 2 hours |
| Create initial migration | P0 | 30 min |
| Create repository interfaces (IRepository<T>) | P0 | 30 min |
| Create repository implementations | P0 | 2 hours |
| Create IUnitOfWork | P1 | 30 min |
| Add Redis configuration | P1 | 30 min |
| Add Blob Storage service | P2 | 1 hour |

---

### Phase 2: Orleans Backend (Week 3-4)

#### 2.1 Grain Interfaces (Complete)

| Task | Priority | Est. Time |
|------|----------|-----------|
| Complete IBuildingGrain (exists, review) | P0 | 30 min |
| Create IUserGrain (user profile, buildings) | P0 | 30 min |
| Create INotificationGrain (push notifications) | P0 | 30 min |
| Create IDeadlineGrain (legal deadlines) | P0 | 30 min |
| Create IAssemblyGrain (meeting management) | P0 | 30 min |
| Create IDashboardGrain (aggregated stats) | P1 | 30 min |
| Create ISyncGrain (offline sync) | P1 | 30 min |

#### 2.2 Grain State Classes

| Task | Priority | Est. Time |
|------|----------|-----------|
| BuildingState (complete) | P0 | 30 min |
| UserState | P0 | 15 min |
| NotificationState | P0 | 15 min |
| DeadlineState | P0 | 15 min |
| AssemblyState | P0 | 30 min |

#### 2.3 Grain Implementations

| Task | Priority | Est. Time |
|------|----------|-----------|
| BuildingGrain - core methods | P0 | 4 hours |
| BuildingGrain - key management | P0 | 2 hours |
| BuildingGrain - work orders | P0 | 2 hours |
| BuildingGrain - finance | P0 | 2 hours |
| BuildingGrain - assembly/voting | P0 | 3 hours |
| UserGrain | P0 | 2 hours |
| NotificationGrain | P1 | 2 hours |
| DeadlineGrain with reminders | P1 | 3 hours |
| AssemblyGrain | P1 | 3 hours |

#### 2.4 Orleans Silo

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create Program.cs with silo builder | P0 | 1 hour |
| Configure PostgreSQL persistence | P0 | 30 min |
| Configure Redis grain storage | P0 | 30 min |
| Configure reminders | P0 | 30 min |
| Configure streaming | P1 | 1 hour |
| Add Orleans Dashboard | P1 | 30 min |
| Add health checks | P1 | 30 min |
| Configure Serilog | P0 | 30 min |

---

### Phase 3: API Layer (Week 5)

#### 3.1 REST API

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create Program.cs with Orleans client | P0 | 1 hour |
| Create base ApiController | P0 | 30 min |
| BuildingsController | P0 | 2 hours |
| ResidentsController | P0 | 2 hours |
| KeysController | P0 | 2 hours |
| WorkOrdersController | P0 | 2 hours |
| AnnouncementsController | P0 | 1 hour |
| AssembliesController | P0 | 2 hours |
| FinanceController | P0 | 2 hours |
| SyncController | P1 | 2 hours |
| Add Swagger/OpenAPI | P0 | 30 min |
| Add API versioning | P1 | 1 hour |
| Add rate limiting | P1 | 1 hour |

#### 3.2 Authentication

| Task | Priority | Est. Time |
|------|----------|-----------|
| Configure JWT authentication | P0 | 2 hours |
| Create AuthController | P0 | 2 hours |
| Create user registration | P0 | 2 hours |
| Create password reset | P1 | 1 hour |
| Create role management | P1 | 2 hours |

---

### Phase 4: Blazor UI (Week 6-8)

#### 4.1 Blazor Project Setup

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create Program.cs | P0 | 30 min |
| Create _Imports.razor | P0 | 15 min |
| Create App.razor with router | P0 | 15 min |
| Configure Orleans client | P0 | 1 hour |
| Configure MudBlazor | P0 | 30 min |
| Create MainLayout.razor | P0 | 30 min |
| Create NavMenu.razor | P0 | 30 min |
| Configure local storage | P0 | 30 min |
| Create auth state provider | P0 | 2 hours |

#### 4.2 Shared Services

| Task | Priority | Est. Time |
|------|----------|-----------|
| IBuildingService (Orleans client wrapper) | P0 | 2 hours |
| IAuthService | P0 | 1 hour |
| ISyncService | P0 | 2 hours |
| INotificationService | P1 | 1 hour |
| IOfflineService | P1 | 2 hours |

#### 4.3 Auth Pages

| Task | Priority | Est. Time |
|------|----------|-----------|
| Login.razor | P0 | 2 hours |
| Register.razor | P0 | 2 hours |
| ForgotPassword.razor | P1 | 1 hour |
| ResetPassword.razor | P1 | 1 hour |

#### 4.4 Main Pages

| Task | Priority | Est. Time |
|------|----------|-----------|
| Dashboard.razor | P0 | 3 hours |
| Residents/List.razor | P0 | 2 hours |
| Residents/Detail.razor | P0 | 2 hours |
| Residents/Create.razor | P0 | 2 hours |
| Residents/Edit.razor | P0 | 1 hour |
| Keys/List.razor | P0 | 2 hours |
| Keys/Checkout.razor (modal) | P0 | 2 hours |
| Keys/Checkin.razor (modal) | P0 | 1 hour |
| Keys/Overdue.razor | P0 | 1 hour |
| WorkOrders/List.razor | P0 | 2 hours |
| WorkOrders/Detail.razor | P0 | 2 hours |
| WorkOrders/Create.razor | P0 | 2 hours |
| Announcements/List.razor | P0 | 1 hour |
| Announcements/Create.razor | P0 | 2 hours |
| Assemblies/List.razor | P0 | 2 hours |
| Assemblies/Detail.razor | P0 | 3 hours |
| Assemblies/Create.razor | P0 | 2 hours |
| Assemblies/Voting.razor | P0 | 3 hours |
| Finance/Dashboard.razor | P0 | 2 hours |
| Finance/Payments.razor | P0 | 2 hours |
| Finance/Expenses.razor | P0 | 2 hours |

#### 4.5 Shared Components

| Task | Priority | Est. Time |
|------|----------|-----------|
| BuildingSelector.razor | P0 | 1 hour |
| StatusBadge.razor | P0 | 30 min |
| PriorityBadge.razor | P0 | 30 min |
| ResidentCard.razor | P0 | 1 hour |
| KeyCard.razor | P0 | 1 hour |
| WorkOrderCard.razor | P0 | 1 hour |
| SearchBox.razor | P0 | 30 min |
| ConfirmDialog.razor | P0 | 1 hour |
| LoadingIndicator.razor | P0 | 30 min |
| OfflineBanner.razor | P0 | 30 min |
| EmptyState.razor | P0 | 30 min |

---

### Phase 5: Mobile (Week 9-10)

#### 5.1 MAUI Project

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create MauiProgram.cs | P0 | 1 hour |
| Create MainPage.xaml (BlazorWebView) | P0 | 1 hour |
| Configure Android platform | P0 | 2 hours |
| Configure iOS platform | P0 | 2 hours |
| Configure Windows platform | P0 | 1 hour |
| Add app icons | P0 | 30 min |
| Add splash screen | P0 | 30 min |
| Configure SQLite for offline | P0 | 2 hours |
| Create sync engine | P0 | 4 hours |
| Configure push notifications | P1 | 3 hours |
| Configure deep linking | P1 | 2 hours |
| Add biometric auth | P2 | 2 hours |

#### 5.2 Platform-Specific

| Task | Priority | Est. Time |
|------|----------|-----------|
| Android permissions | P0 | 1 hour |
| iOS permissions | P0 | 1 hour |
| Android notification channels | P1 | 1 hour |
| iOS notification categories | P1 | 1 hour |
| Camera service | P1 | 2 hours |
| File picker service | P1 | 1 hour |
| Phone/Email service | P1 | 1 hour |

---

### Phase 6: Testing (Week 11)

#### 6.1 Unit Tests

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create test project | P0 | 30 min |
| Domain entity tests | P0 | 3 hours |
| Value object tests | P0 | 1 hour |
| Grain unit tests (mocked) | P0 | 4 hours |
| Service tests | P0 | 3 hours |
| Controller tests | P1 | 3 hours |

#### 6.2 Integration Tests

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create test database factory | P0 | 2 hours |
| Create Orleans test cluster | P0 | 2 hours |
| Repository integration tests | P0 | 3 hours |
| Grain integration tests | P0 | 4 hours |
| API integration tests | P1 | 3 hours |
| Sync integration tests | P1 | 2 hours |

#### 6.3 E2E Tests

| Task | Priority | Est. Time |
|------|----------|-----------|
| Setup Playwright | P2 | 1 hour |
| Login flow test | P2 | 1 hour |
| Key checkout flow test | P2 | 2 hours |
| Work order flow test | P2 | 2 hours |
| Assembly voting test | P2 | 2 hours |

---

### Phase 7: Azure Deployment (Week 12)

#### 7.1 Infrastructure

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create main.bicep | P0 | 2 hours |
| Create parameters.json | P0 | 30 min |
| Create Azure pipeline YAML | P0 | 2 hours |
| Configure managed identity | P0 | 1 hour |
| Configure Key Vault | P0 | 1 hour |
| Setup Application Insights | P0 | 1 hour |

#### 7.2 Deployment

| Task | Priority | Est. Time |
|------|----------|-----------|
| Create Dockerfile for Silo | P0 | 1 hour |
| Create Dockerfile for API | P0 | 1 hour |
| Configure CI/CD pipeline | P0 | 2 hours |
| Setup staging environment | P0 | 1 hour |
| Setup production environment | P0 | 1 hour |
| Configure auto-scaling | P1 | 1 hour |
| Setup alerting rules | P1 | 1 hour |

---

## Detailed Task List

### Week 1: Foundation

```
Day 1-2: Project Setup
├── Create Directory.Build.props
├── Create Directory.Packages.props  
├── Create test project .csproj
├── Add appsettings.json files
├── Create .editorconfig

Day 3-5: Core Layer
├── Create BaseEntity
├── Create Building aggregate
├── Create Unit entity
├── Create Resident entity
├── Create Key entity
├── Create WorkOrder entity
├── Create Announcement entity
├── Create Assembly entities
├── Create Finance entities
├── Create value objects
├── Create exceptions
```

### Week 2: Infrastructure

```
Day 1-3: Database
├── Create DbContext
├── Create entity configurations
├── Create initial migration
├── Create repositories

Day 4-5: Services
├── Create repository implementations
├── Configure Redis
├── Add Blob Storage service
```

### Week 3-4: Orleans Backend

```
Week 3:
├── Complete grain interfaces
├── Create grain state classes
├── Implement BuildingGrain (core)
├── Implement BuildingGrain (keys)
├── Implement BuildingGrain (work orders)

Week 4:
├── Implement BuildingGrain (finance)
├── Implement BuildingGrain (assembly)
├── Implement UserGrain
├── Implement NotificationGrain
├── Implement DeadlineGrain
├── Configure Silo
```

### Week 5: API

```
├── Create Program.cs
├── Create controllers (8)
├── Add authentication
├── Add Swagger
```

### Week 6-8: Blazor UI

```
Week 6:
├── Blazor project setup
├── Auth pages
├── Dashboard
├── Shared services

Week 7:
├── Residents pages
├── Keys pages
├── Work orders pages

Week 8:
├── Announcements pages
├── Assemblies pages
├── Finance pages
├── Shared components
```

### Week 9-10: Mobile

```
Week 9:
├── MAUI project setup
├── Platform configurations
├── SQLite offline storage

Week 10:
├── Sync engine
├── Push notifications
├── Platform services
```

### Week 11: Testing

```
├── Unit tests
├── Integration tests
├── E2E tests (if time)
```

### Week 12: Deployment

```
├── Azure infrastructure
├── CI/CD pipeline
├── Deployment
├── Monitoring setup
```

---

## File Creation Checklist

### Immediate (Before Coding)

- [ ] `Directory.Build.props`
- [ ] `Directory.Packages.props`
- [ ] `tests/BuildingManager.Tests/BuildingManager.Tests.csproj`
- [ ] `src/BuildingManager.Orleans.Silo/appsettings.json`
- [ ] `src/BuildingManager.Api/appsettings.json`
- [ ] `src/BuildingManager.Blazor/appsettings.json`
- [ ] `.editorconfig`

### Core Layer

- [ ] `src/BuildingManager.Core/Entities/BaseEntity.cs`
- [ ] `src/BuildingManager.Core/Entities/Building.cs`
- [ ] `src/BuildingManager.Core/Entities/Unit.cs`
- [ ] `src/BuildingManager.Core/Entities/Resident.cs`
- [ ] `src/BuildingManager.Core/Entities/Key.cs`
- [ ] `src/BuildingManager.Core/Entities/KeyTransaction.cs`
- [ ] `src/BuildingManager.Core/Entities/WorkOrder.cs`
- [ ] `src/BuildingManager.Core/Entities/WorkOrderNote.cs`
- [ ] `src/BuildingManager.Core/Entities/Announcement.cs`
- [ ] `src/BuildingManager.Core/Entities/Assembly.cs`
- [ ] `src/BuildingManager.Core/Entities/AgendaItem.cs`
- [ ] `src/BuildingManager.Core/Entities/Vote.cs`
- [ ] `src/BuildingManager.Core/Entities/Attendance.cs`
- [ ] `src/BuildingManager.Core/Entities/Payment.cs`
- [ ] `src/BuildingManager.Core/Entities/Expense.cs`
- [ ] `src/BuildingManager.Core/Entities/FeeStructure.cs`
- [ ] `src/BuildingManager.Core/ValueObjects/Address.cs`
- [ ] `src/BuildingManager.Core/ValueObjects/Money.cs`
- [ ] `src/BuildingManager.Core/ValueObjects/Percentage.cs`
- [ ] `src/BuildingManager.Core/Enums/*.cs`
- [ ] `src/BuildingManager.Core/Exceptions/*.cs`
- [ ] `src/BuildingManager.Core/Events/*.cs`

### Infrastructure Layer

- [ ] `src/BuildingManager.Infrastructure/Data/BuildingManagerDbContext.cs`
- [ ] `src/BuildingManager.Infrastructure/Data/Configurations/*.cs`
- [ ] `src/BuildingManager.Infrastructure/Repositories/*.cs`
- [ ] `src/BuildingManager.Infrastructure/Services/*.cs`

### Orleans Layer

- [ ] `src/BuildingManager.Orleans.Interfaces/IBuildingGrain.cs` (review)
- [ ] `src/BuildingManager.Orleans.Interfaces/IUserGrain.cs`
- [ ] `src/BuildingManager.Orleans.Interfaces/INotificationGrain.cs`
- [ ] `src/BuildingManager.Orleans.Interfaces/IDeadlineGrain.cs`
- [ ] `src/BuildingManager.Orleans.Interfaces/IAssemblyGrain.cs`
- [ ] `src/BuildingManager.Orleans.Grains/State/*.cs`
- [ ] `src/BuildingManager.Orleans.Grains/BuildingGrain.cs`
- [ ] `src/BuildingManager.Orleans.Grains/UserGrain.cs`
- [ ] `src/BuildingManager.Orleans.Grains/NotificationGrain.cs`
- [ ] `src/BuildingManager.Orleans.Grains/DeadlineGrain.cs`
- [ ] `src/BuildingManager.Orleans.Grains/AssemblyGrain.cs`
- [ ] `src/BuildingManager.Orleans.Silo/Program.cs`

### API Layer

- [ ] `src/BuildingManager.Api/Program.cs`
- [ ] `src/BuildingManager.Api/Controllers/*.cs`

### Blazor Layer

- [ ] `src/BuildingManager.Blazor/Program.cs`
- [ ] `src/BuildingManager.Blazor/_Imports.razor`
- [ ] `src/BuildingManager.Blazor/App.razor`
- [ ] `src/BuildingManager.Blazor/Shared/*.razor`
- [ ] `src/BuildingManager.Blazor/Services/*.cs`
- [ ] `src/BuildingManager.Blazor/Pages/*.razor`
- [ ] `src/BuildingManager.Blazor/Components/*.razor`

### Mobile Layer

- [ ] `src/BuildingManager.Blazor.Mobile/MauiProgram.cs`
- [ ] `src/BuildingManager.Blazor.Mobile/MainPage.xaml`
- [ ] `src/BuildingManager.Blazor.Mobile/MainPage.xaml.cs`
- [ ] `src/BuildingManager.Blazor.Mobile/Platforms/Android/*.cs`
- [ ] `src/BuildingManager.Blazor.Mobile/Platforms/iOS/*.cs`
- [ ] `src/BuildingManager.Blazor.Mobile/Platforms/Windows/*.cs`
- [ ] `src/BuildingManager.Blazor.Mobile/Resources/*.svg`

---

## Estimated Timeline

| Phase | Duration | Cumulative |
|-------|----------|------------|
| Phase 1: Foundation | 2 weeks | 2 weeks |
| Phase 2: Orleans Backend | 2 weeks | 4 weeks |
| Phase 3: API | 1 week | 5 weeks |
| Phase 4: Blazor UI | 3 weeks | 8 weeks |
| Phase 5: Mobile | 2 weeks | 10 weeks |
| Phase 6: Testing | 1 week | 11 weeks |
| Phase 7: Deployment | 1 week | 12 weeks |

**Total: 12 weeks (3 months)**

---

## Priority Order for MVP

If timeline is tight, implement in this order:

1. **Core + Infrastructure** (data layer)
2. **BuildingGrain** (single grain with all features)
3. **Silo** (can run and test)
4. **API** (3-4 core controllers)
5. **Blazor Dashboard + 2-3 pages**
6. **Mobile wrapper** (reuse Blazor UI)
7. **Testing** (critical paths only)
8. **Deploy**

This gives a working MVP in ~8 weeks.
