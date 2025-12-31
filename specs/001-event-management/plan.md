# Implementation Plan: Event Management System

**Branch**: `001-event-management` | **Date**: 2025-12-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-event-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Event Management System with queue-based registration, real-time SignalR notifications, multi-track sessions, and flexible event templates. Supports OAuth 2.0 authentication, PostgreSQL storage with JSON columns for extensibility, and comprehensive API integration with tiered rate limiting. Technical approach uses .NET 10 APIs with Angular 21 frontends, Aspire orchestration, and complete NSwag SDK generation.

## Technical Context

**Language/Version**: .NET 10, Angular 21, TypeScript 5.9, Node.js 22+  
**Primary Dependencies**: Aspire 10, Entity Framework Core, FluentValidation, NSwag, SignalR, OpenTelemetry  
**Storage**: PostgreSQL (primary with JSON columns), Redis (caching), Azure Blob Storage (files)  
**Testing**: xUnit (.NET), Vitest (Angular), Playwright (E2E), gRPC testing tools  
**Target Platform**: Azure Container Apps with Docker containers, local development via Aspire orchestration  
**Project Type**: aspire-multi - .NET APIs and Angular apps with Aspire orchestration  
**Architecture Method**: IDesign - business service boundaries, volatile/stable separation, contract-first development  
**Service Boundaries**: EventManagement, Registration, SessionManagement, UserManagement, Notifications (5 business services)  
**Code Generation**: Complete Angular SDK generated via NSwag from C# APIs, gRPC .proto files from C# interfaces  
**Authentication**: OAuth 2.0 with JWT tokens for users, tiered API rate limiting for external integrations  
**Real-time**: SignalR for registration updates, event changes, session notifications with email backup  
**Observability**: OpenTelemetry tracing, Prometheus metrics, structured logging with correlation IDs  
**Performance Goals**: <200ms API response p95, <2s initial Angular load, 95+ Lighthouse score, 2000 req/s capacity  
**Constraints**: <150ms p95 API response, <1.5MB initial bundle size, queue-based capacity management  
**Scale/Scope**: 10k concurrent users, public/private Angular apps, external API integrations, multi-track events

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Pre-Design Check (2025-12-31)**:
✅ **I. Service-First Architecture**: Event management organized by business services (EventManagement, Registration, SessionManagement, UserManagement, Notifications)  
✅ **II. API Contract Consistency**: Shared DTOs across public/private APIs, OpenAPI auto-generated, gRPC contracts from C# interfaces  
✅ **III. Cross-Platform Code Quality**: .NET 10 with StyleCop, Angular 21 with ESLint/Prettier, TypeScript strict mode  
✅ **IV. Comprehensive Testing**: xUnit (business logic), Vitest (Angular components), Playwright (E2E workflows)  
✅ **V. Modern Deployment**: Aspire 10 orchestration with Azure Container Apps, PostgreSQL + Redis infrastructure  
✅ **VI. Observability First**: OpenTelemetry distributed tracing, correlation IDs across Angular → API → gRPC service calls  
✅ **VII. IDesign Architecture**: Business capabilities as service boundaries, volatile event logic separated from stable infrastructure  
✅ **VIII. Code Generation and Type Safety**: Complete Angular SDK (models, services, HTTP clients) generated from C# APIs, .proto files auto-generated

**Post-Design Re-Check (2025-12-31)**:
✅ **I. Service-First Architecture**: CONFIRMED - 5 business services with clear C# interface contracts, gRPC inter-service communication  
✅ **II. API Contract Consistency**: CONFIRMED - Public/Private APIs share DTOs, NSwag generates OpenAPI specs, unified error handling  
✅ **III. Cross-Platform Code Quality**: CONFIRMED - OAuth 2.0 implementation follows security standards, PostgreSQL with proper indexing  
✅ **IV. Comprehensive Testing**: CONFIRMED - Queue processing testable via integration tests, SignalR testable via test clients  
✅ **V. Modern Deployment**: CONFIRMED - Aspire handles OAuth providers, PostgreSQL containerized, Redis for queue backing  
✅ **VI. Observability First**: CONFIRMED - SignalR connection tracking, queue metrics, OAuth audit trails with correlation IDs  
✅ **VII. IDesign Architecture**: CONFIRMED - Registration queue isolated from event management, authentication separated from business logic  
✅ **VIII. Code Generation**: CONFIRMED - NSwag generates OAuth-aware Angular clients, SignalR TypeScript client generation

**Gate Status**: ✅ PASS - All constitutional requirements satisfied with enhanced OAuth 2.0 + PostgreSQL + SignalR architecture

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)
<!--
  ACTION REQUIRED: Replace the placeholder tree below with the concrete layout
  for this feature. Delete unused options and expand the chosen structure with
  real business service paths following IDesign principles. The delivered plan must
  not include Option labels.
-->

```text
# IDesign Service-Oriented Structure (Business Capabilities)
src/
├── PublicApi/                 # Public .NET API endpoints
│   ├── EventManagement/       # Event lifecycle endpoints (NOT Controllers folder)
│   ├── Registration/          # Registration workflow endpoints
│   ├── SessionManagement/     # Session scheduling endpoints
│   ├── UserManagement/        # User profile endpoints
│   └── Program.cs
├── PrivateApi/               # Private .NET API endpoints (same business services)
│   ├── EventManagement/       # Admin event management
│   ├── Registration/          # Admin registration management
│   ├── SessionManagement/     # Admin session management
│   ├── UserManagement/        # Admin user management
│   └── Program.cs
├── Shared/                   # Business Services (gRPC implementations)
│   ├── EventManagement/       # Complete event business service
│   ├── Registration/          # Complete registration business service
│   ├── SessionManagement/     # Complete session business service
│   ├── UserManagement/        # Complete user business service
│   └── Notifications/         # Complete notification business service
├── PublicApp/               # Public Angular application
│   └── src/app/
│       ├── event-management/   # Event discovery and details (business capability)
│       ├── registration/       # Registration workflow (business capability)
│       ├── session-management/ # Session browsing and subscription
│       └── shared-ui/         # UI components only (NO business logic)
├── PrivateApp/             # Private Angular application
│   └── src/app/
│       ├── event-management/   # Event creation and admin
│       ├── registration/       # Registration administration  
│       ├── session-management/ # Session administration
│       └── shared-ui/         # UI components only
└── SharedUI/               # Pure UI component library (NO business services)
    └── src/lib/

protos/                     # Generated gRPC Service Contracts (from C# interfaces)
├── EventManagement.proto    # Generated from IEventManagementService.cs
├── Registration.proto       # Generated from IRegistrationService.cs
├── SessionManagement.proto  # Generated from ISessionManagementService.cs
├── UserManagement.proto     # Generated from IUserManagementService.cs
└── Notifications.proto      # Generated from INotificationService.cs
└── Common.proto             # Generated shared types and enums

tests/
├── PublicApi.Tests/        # Public API tests
├── PrivateApi.Tests/       # Private API tests
├── Shared.Tests/           # Shared library tests
├── PublicApp/e2e/         # Public app E2E tests
└── PrivateApp/e2e/        # Private app E2E tests
```

# [REMOVE IF UNUSED] Option 2: Web application (when "frontend" + "backend" detected)
backend/
├── src/
│   ├── models/
│   ├── services/
│   └── api/
└── tests/

frontend/
├── src/
│   ├── components/
│   ├── pages/
│   └── services/
└── tests/

# [REMOVE IF UNUSED] Option 3: Mobile + API (when "iOS/Android" detected)
api/
└── [same as backend above]

ios/ or android/
└── [platform-specific structure: feature modules, UI flows, platform tests]
```

**Structure Decision**: [Document the selected structure and reference the real
directories captured above]

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
