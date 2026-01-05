# Tasks: Event Management System

**Input**: Design documents from `/specs/001-event-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/  
**Branch**: `001-event-management`

## Task Organization Strategy

Following the user stories from spec.md organized by priority (P1, P2, P3, P4) with infrastructure first approach.

**Phase Structure**:
- **Phase 1**: Setup (project structure and solution)
- **Phase 2**: Foundational (Aspire orchestration, database, authentication)  
- **Phase 3**: User Story 1 - Event Registration and Discovery (P1) 🎯 MVP
- **Phase 4**: User Story 2 - Session Management and Track Organization (P2)
- **Phase 5**: User Story 3 - Speaker and Social Event Management (P3)
- **Phase 6**: User Story 4 - External API Integration (P4)
- **Phase 7**: Polish & Cross-Cutting Concerns

## Format: `- [ ] [TaskID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All tasks include exact file paths

---

## Phase 1: Setup (Project Structure)

**Goal**: Create complete solution structure following IDesign principles  
**Independent Test**: Solution builds without errors, all projects reference correctly

- [x] T001 Create .NET solution file EventManagement.sln in repository root
- [x] T002 [P] Create AppHost project for Aspire orchestration in src/AppHost/AppHost.csproj
- [x] T003 [P] Create ServiceDefaults project for shared configuration in src/ServiceDefaults/ServiceDefaults.csproj
- [x] T004 [P] Create PublicApi .NET project in src/PublicApi/PublicApi.csproj
- [x] T005 [P] Create PrivateApi .NET project in src/PrivateApi/PrivateApi.csproj
- [x] T006 [P] Create Shared business services library in src/Shared/Shared.csproj
- [x] T007 [P] Create PublicApp Angular project in src/PublicApp/ using Angular CLI
- [x] T008 [P] Create PrivateApp Angular project in src/PrivateApp/ using Angular CLI
- [x] T009 [P] Create SharedUI component library in src/PublicApp/projects/shared-ui/
- [x] T010 Add all projects to solution file with proper dependencies
- [x] T011 [P] Create tests directory structure: tests/PublicApi.Tests/, tests/PrivateApi.Tests/, tests/Shared.Tests/
- [x] T012 [P] Create E2E test projects: tests/PublicApp.e2e/, tests/PrivateApp.e2e/

**Checkpoint**: Phase 1 Complete - Solution structure established

---

## Phase 2: Foundational (Aspire Orchestration & Core Infrastructure)

**Goal**: Configure Aspire to orchestrate all services and dependencies  
**Independent Test**: `dotnet run --project src/AppHost` starts all services, Aspire dashboard accessible

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Aspire Infrastructure
- [x] T013 Configure Aspire AppHost in src/AppHost/Program.cs with PostgreSQL container
- [x] T014 [P] Configure Redis container for queue management in src/AppHost/Program.cs
- [x] T015 [P] Configure Azure Blob Storage emulator in src/AppHost/Program.cs
- [x] T016 Register PublicApi project in Aspire orchestration with health checks
- [x] T017 [P] Register PrivateApi project in Aspire orchestration with health checks
- [x] T018 Configure PublicApp as JavaScript resource with proxy configuration
- [x] T019 [P] Configure PrivateApp as JavaScript resource with proxy configuration
- [x] T020 [P] Create Angular proxy.conf.js for PublicApp reading Aspire environment variables
- [x] T021 [P] Create Angular proxy.conf.js for PrivateApp reading Aspire environment variables
- [x] T022 Configure ServiceDefaults with OpenTelemetry tracing and health checks
- [x] T023 [P] Add NuGet packages for Entity Framework Core, SignalR, FluentValidation to ServiceDefaults

### Authentication & User Management Foundation
- [x] T024 Create OAuth 2.0 configuration in src/PublicApi/Program.cs and src/PrivateApi/Program.cs
- [x] T025 [P] Create JWT token service in src/Shared/UserManagement/TokenService.cs
- [x] T026 [P] Create User entity with OAuth fields in src/Shared/UserManagement/User.cs
- [x] T027 [P] Create UserDbContext with PostgreSQL configuration in src/Shared/UserManagement/UserDbContext.cs

**Checkpoint**: Phase 2 Complete - Foundation ready, user story implementation can begin

---

## Phase 3: User Story 1 - Event Registration and Discovery (P1) 🎯 MVP

**Story Goal**: Event attendees can discover and register for events, viewing basic event information and securing their participation. The system must adapt to changing event types, registration requirements, and discovery patterns.

**Independent Test**: Create event, search for it by date/category, view details, complete registration, receive SignalR + email confirmation. Delivers immediate value as a basic event listing and registration system.

### Event Discovery (US1 Core)
- [x] T028 [P] [US1] Create Event entity with JSON custom fields in src/Shared/EventManagement/Event.cs
- [x] T029 [P] [US1] Create EventDbContext in src/Shared/EventManagement/EventDbContext.cs
- [x] T030 [US1] Create IEventService interface in src/Shared/EventManagement/IEventService.cs
- [x] T031 [P] [US1] Create EventService implementation in src/Shared/EventManagement/EventService.cs
- [x] T032 [P] [US1] Create event search endpoints (GET /api/events, GET /api/events/{id}) in src/PublicApi/EventManagement/EventsController.cs
- [x] T033 [P] [US1] Create event list component in src/PublicApp/src/app/event-management/event-list/event-list.component.ts
- [x] T034 [P] [US1] Create event detail component in src/PublicApp/src/app/event-management/event-detail/event-detail.component.ts

### Registration with Queue Processing (US1 Core)
- [x] T035 [P] [US1] Create Registration entity in src/Shared/Registration/Registration.cs
- [x] T036 [P] [US1] Create RegistrationDbContext in src/Shared/Registration/RegistrationDbContext.cs
- [x] T037 [US1] Create IRegistrationService with queue processing in src/Shared/Registration/IRegistrationService.cs
- [x] T038 [US1] Create RegistrationService implementation in src/Shared/Registration/RegistrationService.cs
- [x] T039 [US1] Create Redis queue service for capacity management in src/Shared/Registration/QueueService.cs
- [x] T040 [P] [US1] Create registration endpoints (POST /api/registrations) in src/PublicApi/Registration/RegistrationController.cs
- [x] T041 [P] [US1] Create registration form component in src/PublicApp/src/app/registration/registration-form/registration-form.component.ts

### Real-time Notifications (US1 Core)
- [x] T042 [US1] Create SignalR hub for registration updates in src/PublicApi/Hubs/RegistrationHub.cs
- [x] T043 [P] [US1] Create email notification service in src/Shared/Notifications/EmailService.cs
- [x] T044 [P] [US1] Create SignalR client service in src/PublicApp/src/app/shared/services/signalr.service.ts
- [x] T045 [P] [US1] Integrate SignalR notifications in registration components

### My Events View (US1 Acceptance Scenario 3)
- [x] T046 [P] [US1] Create "My Events" endpoint (GET /api/users/{id}/registrations) in src/PublicApi/Registration/RegistrationController.cs
- [x] T047 [P] [US1] Create "My Events" component in src/PublicApp/src/app/registration/my-events/my-events.component.ts

### Testing US1
- [x] T048 [P] [US1] Create unit tests for EventService in tests/Shared.Tests/EventManagement/EventServiceTests.cs
- [x] T049 [P] [US1] Create unit tests for RegistrationService in tests/Shared.Tests/Registration/RegistrationServiceTests.cs
- [x] T050 [P] [US1] Create integration tests for registration flow in tests/PublicApi.Tests/Registration/RegistrationIntegrationTests.cs
- [x] T051 [P] [US1] Create E2E tests for event discovery and registration in tests/PublicApp.e2e/registration.spec.ts

**Checkpoint**: User Story 1 complete - Basic event listing and registration system operational

---

## Phase 4: User Story 2 - Session Management and Track Organization (P2)

**Story Goal**: Event organizers can create structured events with multiple tracks and sessions, and attendees can subscribe to specific sessions within events.

**Independent Test**: Create event with multiple tracks and sessions, register for event, subscribe to specific sessions with capacity management. Delivers value as a comprehensive conference management tool.

### Track Management (US2 Core)
- [x] T052 [P] [US2] Create Track entity in src/Shared/SessionManagement/Track.cs
- [x] T053 [P] [US2] Create ITrackService interface in src/Shared/SessionManagement/ITrackService.cs
- [x] T054 [P] [US2] Create TrackService implementation in src/Shared/SessionManagement/TrackService.cs
- [x] T055 [P] [US2] Create track management endpoints in src/PrivateApi/SessionManagement/TracksController.cs
- [x] T056 [P] [US2] Create track management components in src/PrivateApp/src/app/session-management/track-list/track-list.component.ts

### Session Management (US2 Core)
- [x] T057 [P] [US2] Create Session entity with capacity limits in src/Shared/SessionManagement/Session.cs
- [x] T058 [P] [US2] Create SessionDbContext in src/Shared/SessionManagement/SessionDbContext.cs
- [x] T059 [US2] Create ISessionService interface in src/Shared/SessionManagement/ISessionService.cs
- [x] T060 [US2] Create SessionService with conflict detection in src/Shared/SessionManagement/SessionService.cs
- [x] T061 [P] [US2] Create session management endpoints in src/PrivateApi/SessionManagement/SessionsController.cs
- [x] T062 [P] [US2] Create session list component in src/PrivateApp/src/app/session-management/session-list/session-list.component.ts
- [x] T063 [P] [US2] Create session form/dialog component in src/PrivateApp/src/app/session-management/session-dialog/session-dialog.component.ts

### Attendee Session Subscription (US2 Scenario 3)
- [x] T064 [P] [US2] Create Subscription entity in src/Shared/SessionManagement/Subscription.cs
- [x] T065 [US2] Create ISubscriptionService interface in src/Shared/SessionManagement/SubscriptionService.cs
- [x] T066 [US2] Create SubscriptionService with capacity management in src/Shared/SessionManagement/SubscriptionService.cs
- [x] T067 [P] [US2] Create session subscription endpoints in src/PublicApi/SessionManagement/SubscriptionsController.cs
- [x] T068 [P] [US2] Create session browsing components in src/PublicApp/src/app/session-management/session-browser/session-browser.component.ts

### Public Session Viewing (US2 Scenario 2)
- [x] T069 [P] [US2] Create public session endpoints (GET /api/events/{id}/sessions) in src/PublicApi/SessionManagement/SessionsController.cs
- [x] T070 [P] [US2] Create public track endpoints (GET /api/events/{id}/tracks) in src/PublicApi/SessionManagement/TracksController.cs

### Testing US2
- [x] T071 [P] [US2] Create unit tests for SessionService in tests/Shared.Tests/SessionManagement/SessionServiceTests.cs
- [x] T072 [P] [US2] Create unit tests for TrackService in tests/Shared.Tests/SessionManagement/TrackServiceTests.cs
- [x] T073 [P] [US2] Create integration tests for session subscription in tests/PublicApi.Tests/SessionManagement/SubscriptionIntegrationTests.cs
- [x] T074 [US2] Create E2E Playwright test for session CRUD in admin app in tests/PrivateApp.e2e/session-management.spec.ts

**Checkpoint**: User Story 2 complete - Multi-track conference management operational

---

## Phase 5: User Story 3 - Speaker and Social Event Management (P3)

**Story Goal**: Event organizers can manage speaker profiles and social events, while attendees can connect with speakers and participate in networking opportunities.

**Independent Test**: Add speaker profiles to sessions, create social events, attendees can RSVP to social events. Delivers value as a complete event ecosystem.

### Speaker Management (US3 Scenario 1)
- [x] T075 [P] [US3] Create SpeakerProfile entity in src/Shared/SessionManagement/SpeakerProfile.cs
- [x] T076 [P] [US3] Create SessionSpeaker join entity in src/Shared/SessionManagement/SessionSpeaker.cs
- [x] T077 [P] [US3] Create speaker management endpoints in src/PrivateApi/SessionManagement/SpeakersController.cs
- [x] T078 [P] [US3] Create public speaker endpoints (GET /api/events/{id}/speakers) in src/PublicApi/SessionManagement/SpeakersController.cs
- [x] T079 [P] [US3] Create speaker profile components in src/PrivateApp/src/app/session-management/speaker-manager/speaker-manager.component.ts

### Social Events (US3 Scenario 2)
- [x] T080 [P] [US3] Create SocialEvent entity in src/Shared/EventManagement/SocialEvent.cs
- [x] T081 [P] [US3] Create SocialEventRsvp entity in src/Shared/EventManagement/SocialEventRsvp.cs
- [x] T082 [P] [US3] Create ISocialEventService interface in src/Shared/EventManagement/ISocialEventService.cs
- [x] T083 [US3] Create SocialEventService implementation in src/Shared/EventManagement/SocialEventService.cs
- [x] T084 [P] [US3] Create social event endpoints in src/PublicApi/EventManagement/SocialEventsController.cs
- [x] T085 [P] [US3] Create social event components in src/PublicApp/src/app/social-events/social-event-list/social-event-list.component.ts

### Testing US3
- [x] T086 [P] [US3] Create unit tests for SocialEventService in tests/Shared.Tests/EventManagement/SocialEventServiceTests.cs

**Checkpoint**: User Story 3 complete - Speaker profiles and social events operational

---

## Phase 6: User Story 4 - External API Integration (P4)

**Story Goal**: External systems can integrate with the platform via API to access event data, manage registrations, and sync with other platforms.

**Independent Test**: Generate API key, query event data via REST API, register users via API, verify rate limiting with proper HTTP status codes. Delivers value as an integration platform.

### API Key Management (US4 Core)
- [x] T087 [P] [US4] Create ApiKey entity in src/Shared/ApiManagement/ApiKey.cs
- [x] T088 [P] [US4] Create ApiKeyDbContext in src/Shared/ApiManagement/ApiKeyDbContext.cs
- [x] T089 [P] [US4] Create IApiKeyService interface in src/Shared/ApiManagement/IApiKeyService.cs
- [x] T090 [US4] Create ApiKeyService implementation in src/Shared/ApiManagement/ApiKeyService.cs
- [x] T091 [P] [US4] Create API key management endpoints in src/PrivateApi/ApiManagement/ApiKeysController.cs

### Rate Limiting (US4 Scenario 3)
- [x] T092 [US4] Configure tiered rate limiting middleware in src/PublicApi/Middleware/RateLimitingMiddleware.cs
- [x] T093 [P] [US4] Create ApiKeyAuthenticationHandler in src/PublicApi/Middleware/ApiKeyAuthenticationHandler.cs

### External API Endpoints (US4 Scenarios 1 & 2)
- [x] T094 [P] [US4] Create external API events endpoints in src/PublicApi/ExternalApi/ExternalApiController.cs
- [x] T095 [P] [US4] Create external API registration endpoints in src/PublicApi/ExternalApi/ExternalApiController.cs

### Documentation
- [x] T096 [P] [US4] Create external API documentation endpoints in src/PublicApi/Documentation/DocsController.cs
- [x] T097 [P] [US4] Create API client examples in docs/api-examples/

### Testing US4
- [x] T098 [P] [US4] Create integration tests for API rate limiting in tests/PublicApi.Tests/RateLimiting/RateLimitingTests.cs
- [x] T099 [P] [US4] Create external API integration tests in tests/PublicApi.Tests/ApiIntegration/ExternalApiTests.cs

**Checkpoint**: User Story 4 complete - External API integration operational

---

## Phase 7: Polish & Cross-Cutting Concerns

**Goal**: Complete the system with production-ready features  
**Independent Test**: All features work together, performance meets requirements, security is properly configured

### Code Generation & SDK
- [x] T100 [P] Create NSwag configuration for Angular SDK generation in src/PublicApi/nswag.json
- [x] T101 [P] Generate TypeScript clients for Angular apps in src/PublicApp/src/app/shared/api/
- [x] T102 [P] Configure gRPC proto generation from C# interfaces in src/Shared/ (OPTIONAL - Low priority)

### Production Configuration
- [x] T103 [P] Configure production appsettings for all APIs in src/*/appsettings.Production.json
- [x] T104 [P] Create Docker configurations for all services in src/*/Dockerfile
- [x] T105 [P] Configure Azure Container Apps deployment in deployment/containerapp.yaml
- [x] T106 [P] Create CI/CD pipeline configuration in .github/workflows/

### Performance & Monitoring
- [x] T107 [P] Configure application insights and custom metrics in src/ServiceDefaults/EventManagementMetrics.cs
- [x] T108 [P] Add health check extensions in src/ServiceDefaults/HealthCheckExtensions.cs
- [x] T109 [P] Add performance budgets to Angular applications in angular.json

### Final E2E Validation
- [x] T110 [P] Create comprehensive E2E test suite in tests/e2e/full-workflow.spec.ts
- [x] T112 [P] Implement Swap Sessions feature (User Story 5)
- [ ] T111 Run quickstart.md validation scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion
- **Phase 3 (US1)**: Depends on Phase 2 completion - MVP delivery
- **Phase 4 (US2)**: Can begin after Phase 2, builds on Phase 3 entities
- **Phase 5 (US3)**: Can begin after Phase 2, builds on Phase 4 session entities
- **Phase 6 (US4)**: Can begin after Phase 2, independent of user stories
- **Phase 7 (Polish)**: Depends on all user stories complete

### User Story Independence

Each user story (US1-US4) can be implemented and tested independently after foundational Phase 2 is complete:

| Story | Dependencies | Independent Test |
|-------|--------------|------------------|
| US1   | Phase 2      | Event search → Register → Confirm |
| US2   | Phase 2, Event entity | Create tracks/sessions → Subscribe |
| US3   | Phase 2, Session entity | Add speakers → Create social events |
| US4   | Phase 2      | API key → Query → Rate limit |

### Parallel Execution Opportunities per Phase

**Phase 1**: T002-T012 can all run in parallel
**Phase 2**: T014-T023 can mostly run in parallel after T013
**Phase 3**: T028-T034 (discovery) parallel with T035-T041 (registration)
**Phase 4**: T052-T056 (tracks) parallel with T057-T063 (sessions)
**Phase 5**: T075-T079 (speakers) parallel with T080-T085 (social)
**Phase 6**: T087-T091 (API keys) parallel with T094-T097 (endpoints)

---

## Implementation Strategy

### MVP Scope (User Story 1 Only)

For fastest time-to-value, implement only Phase 1, Phase 2, and Phase 3:
- Event discovery and search
- Basic registration with queue processing
- SignalR real-time notifications
- Email confirmations

**Estimated Tasks**: 51 tasks (T001-T051)

### Full Implementation

All phases for complete Event Management System:
- All 4 user stories
- External API integration
- Production deployment configuration

**Total Tasks**: 111 tasks

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 111 |
| **Phase 1 (Setup)** | 12 tasks |
| **Phase 2 (Foundational)** | 15 tasks |
| **Phase 3 (US1 - P1)** | 24 tasks |
| **Phase 4 (US2 - P2)** | 23 tasks |
| **Phase 5 (US3 - P3)** | 12 tasks |
| **Phase 6 (US4 - P4)** | 13 tasks |
| **Phase 7 (Polish)** | 12 tasks |
| **Parallelizable Tasks** | 78 (70%) |
| **Completed Tasks** | 108 |
| **Remaining Tasks** | 3 |
