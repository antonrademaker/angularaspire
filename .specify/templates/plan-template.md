# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

[Extract from feature spec: primary requirement + technical approach from research]

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: [e.g., .NET 8.0, Angular 17, or NEEDS CLARIFICATION]  
**Primary Dependencies**: [e.g., Aspire 8.0, gRPC, Entity Framework, OpenTelemetry, or NEEDS CLARIFICATION]  
**Storage**: [if applicable, e.g., SQL Server, PostgreSQL, Redis, or N/A]  
**Testing**: [e.g., xUnit, Jest, Playwright, gRPC testing tools, or NEEDS CLARIFICATION]  
**Target Platform**: [e.g., Docker containers, Azure Container Apps, Kubernetes, or NEEDS CLARIFICATION]
**Project Type**: [aspire-multi - determines source structure with .NET APIs and Angular apps]  
**Architecture Method**: [IDesign - business service boundaries, avoid functional decomposition, or NEEDS CLARIFICATION]  
**Service Boundaries**: [business capabilities identified, e.g., EventManagement, Registration, SessionManagement, or NEEDS CLARIFICATION]  
**Observability**: [e.g., OpenTelemetry tracing, Prometheus metrics, structured logging, or NEEDS CLARIFICATION]  
**Performance Goals**: [domain-specific, e.g., 1000 req/s APIs, <3s initial load, 90+ Lighthouse score, or NEEDS CLARIFICATION]  
**Constraints**: [domain-specific, e.g., <200ms p95 API response, <2MB bundle size, gRPC compatibility, or NEEDS CLARIFICATION]  
**Scale/Scope**: [domain-specific, e.g., 10k users, multiple APIs, public/private apps, or NEEDS CLARIFICATION]

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

[Gates determined based on constitution file]

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

protos/                     # gRPC Business Service Contracts
├── EventManagement.proto    # Event lifecycle service contract
├── Registration.proto       # Registration workflow service contract
├── SessionManagement.proto  # Session management service contract
├── UserManagement.proto     # User management service contract
└── Notifications.proto      # Notification service contract
└── common.proto

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
