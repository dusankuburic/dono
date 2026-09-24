# Building Manager - .NET Architecture

**Technology Stack:** C# / .NET 8 / Orleans / Blazor Hybrid / Azure

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                    Blazor Hybrid (MAUI)                             │   │
│   │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐                 │   │
│   │  │   Android   │  │    iOS      │  │   Windows   │                 │   │
│   │  └─────────────┘  └─────────────┘  └─────────────┘                 │   │
│   │                                                                      │   │
│   │  ┌──────────────────────────────────────────────────────────────┐   │   │
│   │  │           Shared Blazor Web UI (Razor Components)            │   │   │
│   │  │   - Dashboard, Residents, Keys, Work Orders, Finance, etc.   │   │   │
│   │  └──────────────────────────────────────────────────────────────┘   │   │
│   │                                                                      │   │
│   │  ┌──────────────────────────────────────────────────────────────┐   │   │
│   │  │           Local State (SQLite) + Sync Engine                  │   │   │
│   │  └──────────────────────────────────────────────────────────────┘   │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     │ Orleans Client
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ORLEANS SILO CLUSTER                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                        GRAIN ACTORS                                  │   │
│   │                                                                      │   │
│   │   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │   │
│   │   │ BuildingGrain│  │  UserGrain   │  │DeadlineGrain │   ...       │   │
│   │   │   (per bldg) │  │  (per user)  │  │ (per bldg)   │             │   │
│   │   └──────────────┘  └──────────────┘  └──────────────┘             │   │
│   │                                                                      │   │
│   │   State persisted to: PostgreSQL + Azure Blob Storage               │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      STREAMING / STREAMS                             │   │
│   │   - Real-time updates to connected clients                          │   │
│   │   - Event notifications (work orders, announcements)                │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                     REMINDERS / TIMERS                               │   │
│   │   - Deadline reminders                                              │   │
│   │   - Key return overdue alerts                                       │   │
│   │   - 48-hour emergency tracking                                      │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            DATA LAYER                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                     │
│   │ Azure SQL    │  │ Azure Redis  │  │Azure Blob    │                     │
│   │ PostgreSQL   │  │   Cache      │  │ Storage      │                     │
│   └──────────────┘  └──────────────┘  └──────────────┘                     │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
building-manager-dotnet/
├── BuildingManager.sln
│
├── src/
│   ├── BuildingManager.Core/                    # Domain layer
│   │   ├── Entities/                            # Domain entities
│   │   ├── Events/                              # Domain events
│   │   ├── Services/                            # Domain services
│   │   ├── ValueObjects/                        # Value objects
│   │   └── Exceptions/                          # Domain exceptions
│   │
│   ├── BuildingManager.Infrastructure/          # Infrastructure layer
│   │   ├── Data/                                # EF Core DbContext
│   │   ├── Repositories/                        # Repository implementations
│   │   ├── Services/                            # External services
│   │   └── Migrations/                          # DB migrations
│   │
│   ├── BuildingManager.Orleans.Interfaces/      # Grain interfaces
│   │   ├── IBuildingGrain.cs                    # Building actor
│   │   ├── IUserGrain.cs                        # User actor
│   │   ├── INotificationGrain.cs                # Notification actor
│   │   └── IDeadlineGrain.cs                    # Deadline actor
│   │
│   ├── BuildingManager.Orleans.Grains/          # Grain implementations
│   │   ├── BuildingGrain.cs
│   │   ├── UserGrain.cs
│   │   └── State/                               # Grain state classes
│   │
│   ├── BuildingManager.Orleans.Silo/            # Orleans host
│   │   ├── Program.cs
│   │   ├── SiloHostBuilderExtensions.cs
│   │   └── appsettings.json
│   │
│   ├── BuildingManager.Blazor/                  # Shared Blazor UI
│   │   ├── Components/                          # Reusable components
│   │   ├── Pages/                               # Page components
│   │   ├── Services/                            # Client services
│   │   └── Shared/                              # Layout, nav, etc.
│   │
│   ├── BuildingManager.Blazor.Mobile/           # MAUI hybrid wrapper
│   │   ├── MainPage.xaml                        # WebView host
│   │   ├── Platforms/                           # Platform-specific
│   │   └── Resources/
│   │
│   └── BuildingManager.Api/                     # REST API (optional)
│       ├── Controllers/
│       └── Program.cs
│
├── tests/
│   ├── BuildingManager.Tests/                   # Unit tests
│   └── BuildingManager.IntegrationTests/        # Integration tests
│
└── infra/
    └── azure/                                   # Azure deployment
        ├── main.bicep
        └── parameters.json
```

---

## Orleans Grain Design

### Why Orleans?

| Feature | Benefit for Building Manager |
|---------|------------------------------|
| Virtual Actors | Each building = independent grain, auto-scaled |
| Persistence | State automatically persisted to PostgreSQL |
| Reminders | Built-in timers for deadlines, overdue keys |
| Streams | Real-time notifications to connected clients |
| Distributed | Silo can scale across multiple Azure instances |

### Key Grains

```csharp
// Building Grain - Core entity
public interface IBuildingGrain : IGrainWithStringKey
{
    Task<BuildingState> GetStateAsync();
    Task<Unit> GetUnitAsync(int unitNumber);
    Task<Key> CheckoutKeyAsync(Guid keyId, KeyCheckout checkout);
    Task<WorkOrder> CreateWorkOrderAsync(CreateWorkOrderRequest req);
    Task RecordVoteAsync(Guid assemblyId, Guid itemId, Vote vote);
    // ... 20+ methods
}

// User Grain - Session management
public interface IUserGrain : IGrainWithStringKey
{
    Task<UserProfile> GetProfileAsync();
    Task<IEnumerable<string>> GetAccessibleBuildingsAsync();
}

// Notification Grain - Push notifications
public interface INotificationGrain : IGrainWithStringKey
{
    Task PushNotificationAsync(Notification notification);
    Task SubscribeAsync(string connectionId);
    Task UnsubscribeAsync(string connectionId);
}

// Deadline Grain - Legal compliance
public interface IDeadlineGrain : IGrainWithStringKey
{
    Task<IEnumerable<Deadline>> GetUpcomingDeadlinesAsync();
    Task RegisterReminderAsync(Deadline deadline);
}
```

---

## Offline-First Strategy

```
┌─────────────────────────────────────────────────────────────────┐
│                    BLAZOR HYBRID CLIENT                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              ONLINE PATH                                 │   │
│   │                                                          │   │
│   │   UI Component → Orleans Client → Silo → Grain → State  │   │
│   │                                                          │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              OFFLINE PATH                                │   │
│   │                                                          │   │
│   │   UI Component → Local Repository → SQLite → Disk       │   │
│   │                                                          │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              SYNC ENGINE                                 │   │
│   │                                                          │   │
│   │   - Detects connectivity changes                        │   │
│   │   - Queues local changes                                │   │
│   │   - Pushes changes when online                          │   │
│   │   - Pulls remote changes                                │   │
│   │   - Resolves conflicts (last-write-wins or manual)      │   │
│   │                                                          │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Sync Strategy

| Entity | Offline | Sync Direction |
|--------|---------|----------------|
| Buildings | Read-only | Server → Client |
| Units | Read-only | Server → Client |
| Residents | CRUD | Bidirectional |
| Keys | CRUD | Bidirectional |
| Work Orders | Create, Update | Bidirectional |
| Announcements | Read-only | Server → Client |
| Voting | Create only | Client → Server |
| Finance | Read-only | Server → Client |

---

## Azure Deployment

### Infrastructure

```bicep
// main.bicep
param location string = 'westeurope'
param appName string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: 'P1v3'
    tier: 'PremiumV3'
  }
}

resource siloApp 'Microsoft.Web/sites@2023-01-01' = {
  name: '${appName}-silo'
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      // Orleans Silo configuration
    }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: '${appName}-db'
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
}

resource redis 'Microsoft.Cache/Redis@2023-04-01' = {
  name: '${appName}-redis'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: '${appName}storage'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
}
```

### Deployment Commands

```bash
# Create resource group
az group create --name building-manager-rg --location westeurope

# Deploy infrastructure
az deployment group create \
  --resource-group building-manager-rg \
  --template-file infra/azure/main.bicep \
  --parameters appName=buildingmanager

# Deploy silo
az webapp deployment source config-zip \
  --resource-group building-manager-rg \
  --name buildingmanager-silo \
  --src silo.zip

# Deploy mobile app stores
# iOS: App Store Connect
# Android: Google Play Console
```

---

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 / Rider / VS Code
- Docker Desktop
- Azure CLI

### Local Development

```bash
# Clone repository
cd building-manager-dotnet

# Restore dependencies
dotnet restore

# Start infrastructure (PostgreSQL, Redis)
docker-compose up -d

# Apply database migrations
dotnet ef database update --project src/BuildingManager.Infrastructure

# Run Orleans Silo
dotnet run --project src/BuildingManager.Orleans.Silo

# Run Blazor app (in another terminal)
dotnet run --project src/BuildingManager.Blazor

# Run mobile app
dotnet run --project src/BuildingManager.Blazor.Mobile
```

### Run Tests

```bash
# Unit tests
dotnet test tests/BuildingManager.Tests

# Integration tests (requires Docker)
dotnet test tests/BuildingManager.IntegrationTests
```

---

## Key Benefits of This Stack

| Aspect | Benefit |
|--------|---------|
| **Shared Code** | Blazor UI shared between web, mobile, desktop |
| **Orleans Actors** | Each building = isolated state, natural concurrency |
| **Azure Native** | Seamless integration with Azure services |
| **Offline First** | SQLite + sync engine built into client |
| **Type Safety** | Full C# type system across all layers |
| **Performance** | Native mobile, compiled Blazor |
| **Scalability** | Orleans scales horizontally automatically |
