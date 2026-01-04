# Event Management System - Developer Quickstart

**Date**: 2025-12-31  
**Phase**: 1 - Development Setup  
**Updated**: OAuth 2.0, PostgreSQL, SignalR, Queue-based registration

## Architecture Overview

**Authentication**: OAuth 2.0 with JWT tokens, refresh token rotation  
**Database**: PostgreSQL with JSON columns for flexible event data  
**Real-time**: SignalR for registration updates, event changes, session notifications  
**Capacity Management**: Redis-backed queue system for high-demand registration  
**API Integration**: Tiered rate limiting based on partner API key types

## Prerequisites

- **.NET 10 SDK** - Latest version with Aspire workload
- **Node.js 22+** - For Angular development (LTS)
- **PostgreSQL** - For primary data storage
- **Docker Desktop** - For containerized dependencies  
- **Visual Studio 2022** or **VS Code** - With C# and Angular extensions

### Install .NET Aspire Workload
```bash
dotnet workload install aspire
```

### Global Tools
```bash
dotnet tool install -g dotnet-ef
npm install -g @angular/cli@21
```

## Solution Structure

```
AngularAspire/
├── src/
│   ├── AppHost/                           # Aspire orchestration
│   ├── ServiceDefaults/                   # Shared service configuration
│   │
│   ├── PublicApi/                         # Public REST API (.NET 10)
│   ├── PrivateApi/                        # Private REST API (.NET 10)
│   ├── Shared/                            # Business Services (gRPC)
│   │   ├── EventManagement/               # Event business service
│   │   ├── Registration/                  # Registration business service
│   │   ├── SessionManagement/             # Session business service
│   │   ├── UserManagement/                # User business service
│   │   └── Notifications/                 # Notification business service
│   │
│   ├── PublicApp/                         # Public Angular 21 app
│   ├── PrivateApp/                        # Private Angular 21 app
│   └── SharedUI/                          # Shared UI components
│
├── tests/
└── protos/                                # Generated gRPC contracts
│   │
│   ├── Shared/                            # Shared Libraries
│   │   ├── EventManagement.Contracts/     # gRPC contracts
│   │   ├── EventManagement.Domain/        # Domain models
│   │   └── EventManagement.Infrastructure/ # Shared infrastructure
│   │
│   └── Tests/                             # Test Projects
│       ├── EventManagement.Tests/         # Unit tests
│       ├── Integration.Tests/             # Integration tests
│       └── E2E.Tests/                     # Playwright tests
└── docs/                                  # Documentation
```

## 5-Minute Startup

### 1. Clone and Setup
```bash
git clone https://github.com/yourorg/angularaspire.git
cd angularaspire
```

### 2. Create Solution and Projects
```bash
# Create solution
dotnet new sln -n EventManagement

# Aspire orchestration
dotnet new aspire-apphost -n EventManagement.AppHost
dotnet new aspire-servicedefaults -n EventManagement.ServiceDefaults

# Business services
dotnet new webapi -n EventManagement.Api
dotnet new webapi -n Registration.Api
dotnet new webapi -n SessionManagement.Api  
dotnet new webapi -n UserManagement.Api
dotnet new webapi -n Notification.Api

# API Gateways
dotnet new webapi -n Public.Gateway
dotnet new webapi -n Admin.Gateway

# Shared libraries
dotnet new classlib -n EventManagement.Contracts
dotnet new classlib -n EventManagement.Domain
dotnet new classlib -n EventManagement.Infrastructure

# Angular apps with modern setup
ng new PublicApp --routing --style=scss --standalone --package-manager=npm
ng new AdminApp --routing --style=scss --standalone --package-manager=npm

# Test projects
dotnet new xunit -n EventManagement.Tests
dotnet new xunit -n Integration.Tests
dotnet new mstest -n E2E.Tests

# Add all to solution
dotnet sln add **/*.csproj
```

### 3. Configure Aspire Orchestration
Update `AppHost/Program.cs`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("eventdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume();

// .NET APIs
var publicApi = builder.AddProject<Projects.PublicApi>("public-api")
    .WithReference(postgres)
    .WithReference(redis);

var privateApi = builder.AddProject<Projects.PrivateApi>("private-api")
    .WithReference(postgres)
    .WithReference(redis);

// Angular Applications as JavaScript Apps (Development Mode)
var publicApp = builder.AddJavaScriptApp("public-app", "../PublicApp")
    .WithPackageManager("npm")
    .WithPackageManagerCommand("run dev -- --port {{Port}}")
    .WithEnvironment("PUBLIC_API_URL", publicApi.GetEndpoint("https"))
    .WaitFor(publicApi);

var privateApp = builder.AddJavaScriptApp("private-app", "../PrivateApp")
    .WithPackageManager("npm")
    .WithPackageManagerCommand("run dev -- --port {{Port}}")
    .WithEnvironment("PRIVATE_API_URL", privateApi.GetEndpoint("https"))
    .WaitFor(privateApi);

builder.Build().Run();
```
### 4. Configure Angular Proxy for Service Discovery

Create `PublicApp/proxy.conf.js`:
```javascript
const FALLBACK_API_URL = 'https://localhost:5001';

function getApiUrl() {
  // Read Aspire-injected environment variable
  return process.env.PUBLIC_API_URL || FALLBACK_API_URL;
}

const config = {
  "/api/*": {
    "target": getApiUrl(),
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
};

module.exports = config;
```

Create `PrivateApp/proxy.conf.js`:
```javascript
const FALLBACK_API_URL = 'https://localhost:5002';

function getApiUrl() {
  // Read Aspire-injected environment variable  
  return process.env.PRIVATE_API_URL || FALLBACK_API_URL;
}

const config = {
  "/api/*": {
    "target": getApiUrl(),
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
};

module.exports = config;
```

Update both Angular apps' `package.json`:
```json
{
  "scripts": {
    "dev": "ng serve --proxy-config proxy.conf.js --host 0.0.0.0",
    "build": "ng build --configuration production",
    "test": "vitest",
    "test:ui": "vitest --ui",
    "lint": "ng lint"
  }
}
```

### 5. Start Development Environment
```bash
# Start Aspire orchestration (this starts everything)
cd src/AppHost
dotnet run

# This will start:
# - PostgreSQL container
# - Redis container  
# - PublicApi (.NET API)
# - PrivateApi (.NET API)
# - PublicApp (Angular dev server with HMR)
# - PrivateApp (Angular dev server with HMR)
# - Aspire dashboard at http://localhost:15888
```

### 6. Verify Setup
- **Aspire Dashboard**: http://localhost:15888
- **Public App**: Dynamic port (shown in Aspire dashboard)
- **Private App**: Dynamic port (shown in Aspire dashboard)  
- **Public API**: Dynamic port with Swagger UI
- **Private API**: Dynamic port with Swagger UI

**Key Benefits of This Setup:**
✅ **Hot Module Replacement**: Angular changes reload instantly  
✅ **Service Discovery**: No hardcoded API URLs, reads from Aspire environment  
✅ **Independent Development**: Frontend and backend can be developed separately  
✅ **Production Ready**: Same codebase works for production static file serving

## Database Setup

### Entity Framework Configuration
Add to each service's `Program.cs`:

```csharp
builder.AddServiceDefaults();

// Entity Framework with SQL Server
builder.Services.AddDbContext<EventDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("eventdb")));

// Redis caching
builder.AddRedisOutputCache("redis");
builder.Services.AddStackExchangeRedisCache(options => {
    options.Configuration = builder.Configuration.GetConnectionString("redis");
});

var app = builder.Build();
app.MapDefaultEndpoints();
```

### Database Migrations
```bash
# EventManagement.Api
cd src/Services/EventManagement.Api
dotnet ef migrations add InitialCreate
dotnet ef database update

# Registration.Api  
cd ../Registration.Api
dotnet ef migrations add InitialCreate
dotnet ef database update

# Repeat for other services...
```

## gRPC Service Communication

### Configure gRPC Client in Gateway
`Public.Gateway/Program.cs`:

```csharp
builder.Services.AddGrpcClient<EventService.EventServiceClient>(options => {
    options.Address = new Uri("https://eventmanagement-api");
});

builder.Services.AddGrpcClient<RegistrationService.RegistrationServiceClient>(options => {
    options.Address = new Uri("https://registration-api");  
});
```

### Add gRPC Service in Business Service
`EventManagement.Api/Program.cs`:

```csharp
builder.Services.AddGrpc();

var app = builder.Build();
app.MapGrpcService<EventGrpcService>();
```

## Angular App Setup

### Install Dependencies
```bash
# PublicApp
cd src/Apps/PublicApp
npm install @angular/material @angular/cdk @angular/animations
## NSwag SDK Generation

### Configure NSwag in .NET APIs

Add to `PublicApi.csproj`:
```xml
<ItemGroup>
  <PackageReference Include="NSwag.MSBuild" Version="14.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>

<Target Name="NSwag" AfterTargets="PostBuildEvent" Condition=" '$(Configuration)' == 'Debug' ">
  <Exec Command="$(NSwagExe_Net80) run nswag.json" />
</Target>
```

Create `PublicApi/nswag.json`:
```json
{
  "runtime": "Net80",
  "defaultVariables": null,
  "documentGenerator": {
    "aspNetCoreToOpenApi": {
      "project": "PublicApi.csproj",
      "msBuildProjectExtensionsPath": null,
      "configuration": "Debug",
      "runtime": null,
      "targetFramework": null,
      "noBuild": false,
      "verbose": true
    }
  },
  "codeGenerators": {
    "openApiToTypeScriptClient": {
      "className": "PublicApiClient",
      "moduleName": "",
      "namespace": "",
      "typeScriptVersion": 5.9,
      "template": "Angular",
      "promiseType": "Promise",
      "httpClass": "HttpClient",
      "withCredentials": false,
      "useSingletonProvider": true,
      "injectionTokenType": "InjectionToken",
      "rxJsVersion": 7.0,
      "dateTimeType": "Date",
      "nullValue": "Undefined",
      "generateClientClasses": true,
      "generateClientInterfaces": true,
      "generateOptionalParameters": true,
      "exportTypes": true,
      "wrapDtoExceptions": true,
      "exceptionClass": "ApiException",
      "generateResponseClasses": true,
      "output": "../PublicApp/src/app/shared/api/public-api.client.ts"
    }
  }
}
```

### Generate Angular SDK
```bash
# Build .NET API (triggers NSwag generation)
cd src/PublicApi
dotnet build

# Generated TypeScript client will be available at:
# PublicApp/src/app/shared/api/public-api.client.ts
```

### Use Generated SDK in Angular

Update `PublicApp/src/app/app.config.ts`:
```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { PUBLIC_API_BASE_URL, PublicApiClient } from './shared/api/public-api.client';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    // Generated NSwag client
    PublicApiClient,
    { provide: PUBLIC_API_BASE_URL, useValue: '/api' }
  ]
};
```

Use in Angular component:
```typescript
import { Component, inject } from '@angular/core';
import { PublicApiClient } from './shared/api/public-api.client';

@Component({
  selector: 'app-events',
  template: `
    <div>
      @for (event of events$ | async; track event.id) {
        <div>{{ event.title }}</div>
      }
    </div>
  `
})
export class EventsComponent {
  private apiClient = inject(PublicApiClient);
  
  events$ = this.apiClient.getEvents();
}
```

## Testing Setup

### Unit Testing
```bash
# .NET Tests
cd src/Tests/EventManagement.Tests
dotnet test

# Angular Tests with Vitest
cd src/Apps/PublicApp
npm run test
# or with UI
npm run test:ui
```

### Integration Testing
`Integration.Tests/EventApiTests.cs`:

```csharp
public class EventApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetEvents_ReturnsEvents()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act  
        var response = await client.GetAsync("/api/v1/events");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var events = await response.Content.ReadFromJsonAsync<EventListResponse>();
        Assert.NotNull(events);
    }
}
```

### E2E Testing with Playwright
```bash
cd src/Tests/E2E.Tests
npx playwright install
npx playwright test
```

## Development Workflow

### 1. Feature Development
1. Create feature branch: `git checkout -b feature/event-registration`
2. Update C# service interfaces if needed (auto-generates .proto files)
3. Run code generation: `dotnet build` (regenerates .proto and TypeScript models)
4. Implement backend service changes
5. Update API gateway if needed
6. Implement frontend changes (using generated TypeScript models)
7. Add tests at all layers
8. Verify with Aspire dashboard

### 2. Database Changes
```bash
# Add migration
dotnet ef migrations add AddEventStatus --project EventManagement.Api

# Update database
dotnet ef database update --project EventManagement.Api

# Update other services as needed
```

### 3. Service Contract Changes
1. Update C# interfaces in `EventManagement.Contracts` project
2. Build solution to regenerate .proto files and TypeScript models: `dotnet build`
3. Update OpenAPI specs are auto-generated from controllers
4. Update Angular services to use generated TypeScript models
5. Update tests to match new contracts

### 4. Code Generation Setup
```bash
# Add to EventManagement.Contracts project
dotnet add package protobuf-net.Grpc
dotnet add package NSwag.MSBuild

# Configure MSBuild for automatic generation
# Generates TypeScript models on build
```

**Code Generation Workflow**:
```mermaid
graph TD
    A[C# Service Interface] --> B[.proto Generation]
    A --> C[TypeScript Model Generation]
    B --> D[gRPC Client Generation]
    C --> E[Angular Service Updates]
    D --> F[Service Communication]
    E --> F
```

### 4. Frontend Development
```bash
# Generate Angular components
ng generate component events/event-list --standalone
ng generate service services/event-api

# Generate NgRx state management
ng generate @ngrx/schematics:feature events --reducers ../state/app.state.ts
```

## Production Deployment

### Container Registry
```bash
# Build and push services
dotnet publish --os linux --arch x64 -p:PublishProfile=DefaultContainer
docker push myregistry/eventmanagement-api:latest

# Build and push Angular apps
ng build --prod
docker build -t myregistry/publicapp:latest .
docker push myregistry/publicapp:latest
```

### Azure Container Apps
Deploy via Aspire:
```bash
dotnet run --publisher azd
```

## Troubleshooting

### Common Issues
1. **Aspire services not starting**: Check Docker Desktop is running
2. **Angular apps not loading**: Verify Node.js version >= 18
3. **Database connection errors**: Confirm SQL Server container is healthy
4. **gRPC communication failures**: Check service discovery configuration
5. **CORS issues**: Configure CORS policies in gateway services

### Useful Commands
```bash
# View Aspire logs
dotnet run --project EventManagement.AppHost --verbosity detailed

# Reset databases  
dotnet ef database drop --project EventManagement.Api
dotnet ef database update --project EventManagement.Api

# Clear Angular cache
ng cache clean

# View container logs
docker logs aspire-sql-1
```

### Performance Monitoring
- **Aspire Dashboard**: Real-time service metrics
- **Application Insights**: Production telemetry
- **OpenTelemetry**: Distributed tracing
- **Prometheus**: Custom metrics collection

## Next Steps

1. **Complete the data models** - Implement Entity Framework entities
2. **Add authentication** - JWT tokens with role-based access
3. **Implement caching** - Redis for frequently accessed data  
4. **Add validation** - FluentValidation for robust input validation
5. **Performance optimization** - Query optimization and connection pooling
6. **Security hardening** - Input sanitization and rate limiting

This quickstart gets you running with a fully orchestrated, production-ready architecture following IDesign principles and constitutional requirements.