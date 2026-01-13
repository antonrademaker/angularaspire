# angularaspire Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-30

## Active Technologies
- .NET 10, Angular 21, TypeScript 5.9 + Aspire 10, Entity Framework Core, FluentValidation, NSwag, OpenTelemetry (001-event-management)
- PostgreSQL (primary), Redis (caching), Azure Blob Storage (files) (001-event-management)
- Testing: xUnit (.NET), Vitest (Angular), Playwright (E2E) (001-event-management)
- .NET 10, Angular 21, TypeScript 5.9, Node.js 22+ + Aspire 10, Entity Framework Core, FluentValidation, NSwag, SignalR, OpenTelemetry (001-event-management)
- PostgreSQL (primary with JSON columns), Redis (caching), Azure Blob Storage (files) (001-event-management)
- [e.g., .NET 10, Angular 21, TypeScript 5.9, or NEEDS CLARIFICATION] + [e.g., Aspire 10, gRPC, Entity Framework, OpenTelemetry, or NEEDS CLARIFICATION] (003-event-admin-ui)
- [if applicable, e.g., SQL Server, PostgreSQL, Redis, or N/A] (003-event-admin-ui)
- .NET 10, Angular 21, TypeScript 5.9 + Aspire, gRPC, Entity Framework Core, OpenTelemetry (003-event-admin-ui)

## Project Structure

```text
src/
├── PublicApi/                 # Public .NET API endpoints
├── PrivateApi/               # Private .NET API endpoints  
├── Shared/                   # Business Services (gRPC implementations)
├── PublicApp/               # Public Angular application
├── PrivateApp/             # Private Angular application
└── SharedUI/               # Pure UI component library
tests/
├── PublicApi.Tests/        # Public API tests
├── PrivateApi.Tests/       # Private API tests
├── Shared.Tests/           # Shared library tests
├── PublicApp/e2e/         # Public app E2E tests
└── PrivateApp/e2e/        # Private app E2E tests
```

## Angular Development with Aspire

**Development Mode**: Two Angular dev servers as Aspire JavaScript apps
- PublicApp: Angular dev server with proxy.conf.js reading Aspire env vars
- PrivateApp: Angular dev server with proxy.conf.js reading Aspire env vars  
- Service Discovery: No hardcoded API URLs, proxy reads ASPIRE_SERVICE_* environment variables
- HMR: Full Hot Module Replacement support for optimal developer experience

**Production Mode**: .NET APIs serve Angular static files
- ApiA serves PublicApp static files from wwwroot/public-app/
- ApiB serves PrivateApp static files from wwwroot/private-app/
- AppHost orchestrates .NET processes only (no Angular dev servers)

## Code Generation Requirements

**NSwag SDK**: Complete Angular SDK generated from C# APIs
- TypeScript models from C# DTOs with strict typing
- HTTP client services with dependency injection
- Validation services from FluentValidation rules
- Testing utilities and mock services
- Observable patterns with RxJS integration

**gRPC Contracts**: Generated from C# interfaces
- .proto files auto-generated from C# service interfaces
- Avoid manual .proto file creation

## Commands

```bash
# Development
npm run dev          # Start Angular dev server with Aspire proxy
dotnet run --project AppHost  # Start Aspire orchestration

# Testing  
npm run test         # Vitest for Angular components
dotnet test         # xUnit for .NET services
npx playwright test # E2E testing

# Build
npm run build       # Production Angular build
dotnet build        # .NET compilation with NSwag SDK generation
```

## Code Style

**C#**: StyleCop analyzers, nullable reference types enabled, ConfigureAwait(false) for async
**Angular**: ESLint + Prettier, TypeScript strict mode, OnPush change detection
**Architecture**: IDesign service boundaries, business capabilities not technical layers

## Recent Changes
- 003-event-admin-ui: Added .NET 10, Angular 17+, TypeScript 5.x + Aspire, gRPC, Entity Framework Core, OpenTelemetry
- 003-event-admin-ui: Added [e.g., .NET 10, Angular 21, TypeScript 5.9, or NEEDS CLARIFICATION] + [e.g., Aspire 10, gRPC, Entity Framework, OpenTelemetry, or NEEDS CLARIFICATION]
- 001-event-management: Added .NET 10, Angular 21, TypeScript 5.9, Node.js 22+ + Aspire 10, Entity Framework Core, FluentValidation, NSwag, SignalR, OpenTelemetry


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
