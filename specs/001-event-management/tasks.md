# Tasks: Event Management System

**Input**: Design documents from `/specs/001-event-management/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/  
**Branch**: `001-event-management`

## Task Organization Strategy

Following the user's request for **structure first, then infrastructure (Aspire), then projects** with small, focused tasks.

**Phase Structure**:
- **Phase 1**: Setup (project structure and solution)
- **Phase 2**: Infrastructure (Aspire orchestration)  
- **Phase 3**: User Story 1 - Event Registration and Discovery (P1)
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

- [x] T001 Create .NET solution file in repository root
- [ ] T002 [P] Create AppHost project for Aspire orchestration in src/AppHost/AppHost.csproj
- [ ] T003 [P] Create ServiceDefaults project for shared configuration in src/ServiceDefaults/ServiceDefaults.csproj
- [ ] T004 [P] Create PublicApi .NET project in src/PublicApi/PublicApi.csproj
- [ ] T005 [P] Create PrivateApi .NET project in src/PrivateApi/PrivateApi.csproj
- [ ] T006 [P] Create Shared business services library in src/Shared/Shared.csproj
- [ ] T007 [P] Create PublicApp Angular project in src/PublicApp/ using Angular CLI 21
- [ ] T008 [P] Create PrivateApp Angular project in src/PrivateApp/ using Angular CLI 21
- [ ] T009 [P] Create SharedUI component library in src/SharedUI/ using Angular CLI 21
- [ ] T010 Add all projects to solution file with proper dependencies
- [ ] T011 [P] Create tests directory structure: tests/PublicApi.Tests/, tests/PrivateApi.Tests/, tests/Shared.Tests/
- [ ] T012 [P] Create E2E test projects: tests/PublicApp.e2e/, tests/PrivateApp.e2e/

---

## Phase 2: Infrastructure (Aspire Orchestration)

**Goal**: Configure Aspire to orchestrate all services and dependencies  
**Independent Test**: `dotnet run --project src/AppHost` starts all services, Aspire dashboard accessible

- [ ] T013 Configure Aspire AppHost in src/AppHost/Program.cs with PostgreSQL container
- [ ] T014 [P] Configure Redis container for queue management in src/AppHost/Program.cs
- [ ] T015 [P] Configure Azure Blob Storage emulator in src/AppHost/Program.cs
- [ ] T016 Register PublicApi project in Aspire orchestration with health checks
- [ ] T017 [P] Register PrivateApi project in Aspire orchestration with health checks
- [ ] T018 Configure PublicApp as JavaScript resource with proxy configuration
- [ ] T019 [P] Configure PrivateApp as JavaScript resource with proxy configuration
- [ ] T020 [P] Create Angular proxy.conf.js for PublicApp reading Aspire environment variables
- [ ] T021 [P] Create Angular proxy.conf.js for PrivateApp reading Aspire environment variables
- [ ] T022 Configure ServiceDefaults with OpenTelemetry tracing and health checks
- [ ] T023 [P] Add NuGet packages for Entity Framework Core, SignalR, FluentValidation to ServiceDefaults

---

## Phase 3: User Story 1 - Event Registration and Discovery (P1)

**Story Goal**: Event attendees can discover and register for events with queue-based processing  
**Independent Test**: Create event, search for it, register successfully, receive SignalR + email confirmation

### Authentication & User Management
- [ ] T024 [US1] Create OAuth 2.0 configuration in src/PrivateApi/Program.cs
- [ ] T025 [P] [US1] Create JWT token service in src/Shared/UserManagement/TokenService.cs
- [ ] T026 [P] [US1] Create User entity with OAuth fields in src/Shared/UserManagement/User.cs
- [ ] T027 [P] [US1] Create UserDbContext with PostgreSQL configuration in src/Shared/UserManagement/UserDbContext.cs

### Event Discovery
- [ ] T028 [P] [US1] Create Event entity with JSON custom fields in src/Shared/EventManagement/Event.cs
- [ ] T029 [P] [US1] Create EventDbContext in src/Shared/EventManagement/EventDbContext.cs
- [ ] T030 [US1] Create IEventService interface in src/Shared/EventManagement/IEventService.cs
- [ ] T031 [P] [US1] Create EventService implementation in src/Shared/EventManagement/EventService.cs
- [ ] T032 [P] [US1] Create event search endpoints in src/PublicApi/EventManagement/EventsController.cs
- [ ] T033 [P] [US1] Create event display components in src/PublicApp/src/app/event-management/event-list.component.ts

### Registration with Queue Processing
- [ ] T034 [P] [US1] Create Registration entity in src/Shared/Registration/Registration.cs
- [ ] T035 [P] [US1] Create RegistrationDbContext in src/Shared/Registration/RegistrationDbContext.cs
- [ ] T036 [US1] Create IRegistrationService with queue processing in src/Shared/Registration/IRegistrationService.cs
- [ ] T037 [US1] Create Redis queue service for capacity management in src/Shared/Registration/QueueService.cs
- [ ] T038 [P] [US1] Create registration endpoints in src/PublicApi/Registration/RegistrationController.cs
- [ ] T039 [P] [US1] Create registration form component in src/PublicApp/src/app/registration/registration-form.component.ts

### Real-time Notifications
- [ ] T040 [US1] Create SignalR hub for registration updates in src/PublicApi/Hubs/RegistrationHub.cs
- [ ] T041 [P] [US1] Create email notification service in src/Shared/Notifications/EmailService.cs
- [ ] T042 [P] [US1] Create SignalR client service in src/PublicApp/src/app/shared/signalr.service.ts
- [ ] T043 [P] [US1] Integrate SignalR notifications in registration components

### Testing US1
- [ ] T044 [P] [US1] Create unit tests for EventService in tests/Shared.Tests/EventManagement/EventServiceTests.cs
- [ ] T045 [P] [US1] Create unit tests for RegistrationService in tests/Shared.Tests/Registration/RegistrationServiceTests.cs
- [ ] T046 [P] [US1] Create integration tests for registration flow in tests/PublicApi.Tests/Registration/RegistrationIntegrationTests.cs
- [ ] T047 [P] [US1] Create E2E tests for event discovery and registration in tests/PublicApp.e2e/registration.spec.ts

---

## Phase 4: User Story 2 - Session Management and Track Organization (P2)

**Story Goal**: Event organizers create multi-track events, attendees subscribe to sessions  
**Independent Test**: Create event with tracks and sessions, users register for event and subscribe to sessions

### Track and Session Management
- [ ] T048 [P] [US2] Create Track entity in src/Shared/SessionManagement/Track.cs
- [ ] T049 [P] [US2] Create Session entity with capacity limits in src/Shared/SessionManagement/Session.cs
- [ ] T050 [P] [US2] Create SessionDbContext in src/Shared/SessionManagement/SessionDbContext.cs
- [ ] T051 [US2] Create ISessionService interface in src/Shared/SessionManagement/ISessionService.cs
- [ ] T052 [US2] Create SessionService with conflict detection in src/Shared/SessionManagement/SessionService.cs

### Organizer Interface
- [ ] T053 [P] [US2] Create track management endpoints in src/PrivateApi/SessionManagement/TracksController.cs
- [ ] T054 [P] [US2] Create session management endpoints in src/PrivateApi/SessionManagement/SessionsController.cs
- [ ] T055 [P] [US2] Create track management components in src/PrivateApp/src/app/session-management/track-manager.component.ts
- [ ] T056 [P] [US2] Create session management components in src/PrivateApp/src/app/session-management/session-manager.component.ts

### Attendee Session Subscription
- [ ] T057 [P] [US2] Create Subscription entity in src/Shared/SessionManagement/Subscription.cs
- [ ] T058 [US2] Create subscription service with capacity management in src/Shared/SessionManagement/SubscriptionService.cs
- [ ] T059 [P] [US2] Create session subscription endpoints in src/PublicApi/SessionManagement/SubscriptionsController.cs
- [ ] T060 [P] [US2] Create session browsing components in src/PublicApp/src/app/session-management/session-browser.component.ts

### Testing US2
- [ ] T061 [P] [US2] Create unit tests for SessionService in tests/Shared.Tests/SessionManagement/SessionServiceTests.cs
- [ ] T062 [P] [US2] Create integration tests for session subscription in tests/PublicApi.Tests/SessionManagement/SubscriptionIntegrationTests.cs

---

## Phase 5: User Story 3 - Speaker and Social Event Management (P3)

**Story Goal**: Manage speaker profiles and social events for networking  
**Independent Test**: Add speakers to sessions, create social events, attendees can RSVP

### Speaker Management
- [ ] T063 [P] [US3] Create SpeakerProfile entity in src/Shared/SessionManagement/SpeakerProfile.cs
- [ ] T064 [P] [US3] Create speaker management endpoints in src/PrivateApi/SessionManagement/SpeakersController.cs
- [ ] T065 [P] [US3] Create speaker profile components in src/PrivateApp/src/app/session-management/speaker-manager.component.ts

### Social Events
- [ ] T066 [P] [US3] Create SocialEvent entity in src/Shared/EventManagement/SocialEvent.cs
- [ ] T067 [P] [US3] Create SocialEventRSVP entity in src/Shared/EventManagement/SocialEventRSVP.cs
- [ ] T068 [US3] Create social event service in src/Shared/EventManagement/SocialEventService.cs
- [ ] T069 [P] [US3] Create social event endpoints in src/PublicApi/EventManagement/SocialEventsController.cs
- [ ] T070 [P] [US3] Create social event components in src/PublicApp/src/app/event-management/social-events.component.ts

### Testing US3
- [ ] T071 [P] [US3] Create unit tests for SocialEventService in tests/Shared.Tests/EventManagement/SocialEventServiceTests.cs

---

## Phase 6: User Story 4 - External API Integration (P4)

**Story Goal**: External systems integrate via REST API with tiered rate limiting  
**Independent Test**: Generate API key, perform CRUD operations, verify rate limiting works

### API Integration Framework
- [ ] T072 [P] [US4] Create API key management in src/PrivateApi/ApiManagement/ApiKeysController.cs
- [ ] T073 [US4] Configure tiered rate limiting middleware in src/PublicApi/Middleware/RateLimitingMiddleware.cs
- [ ] T074 [P] [US4] Create external API documentation endpoints in src/PublicApi/Documentation/
- [ ] T075 [P] [US4] Create API client examples and SDKs in docs/api-examples/

### Testing US4
- [ ] T076 [P] [US4] Create integration tests for API rate limiting in tests/PublicApi.Tests/RateLimiting/RateLimitingTests.cs
- [ ] T077 [P] [US4] Create external API integration tests in tests/PublicApi.Tests/ApiIntegration/ExternalApiTests.cs

---

## Phase 7: Polish & Cross-Cutting Concerns

**Goal**: Complete the system with production-ready features  
**Independent Test**: All features work together, performance meets requirements, security is properly configured

### Code Generation & SDK
- [ ] T078 [P] Create NSwag configuration for Angular SDK generation in src/PublicApi/nswag.json
- [ ] T079 [P] Generate TypeScript clients for Angular apps in src/PublicApp/src/app/shared/api/
- [ ] T080 [P] Configure gRPC proto generation from C# interfaces in src/Shared/

### Production Configuration
- [ ] T081 [P] Configure production appsettings for all APIs in src/*/appsettings.Production.json
- [ ] T082 [P] Create Docker configurations for all services in src/*/Dockerfile
- [ ] T083 [P] Configure Azure Container Apps deployment in deployment/containerapp.yaml
- [ ] T084 [P] Create CI/CD pipeline configuration in .github/workflows/deploy.yml

### Performance & Monitoring
- [ ] T085 [P] Configure application insights and monitoring in src/ServiceDefaults/
- [ ] T086 [P] Add performance budgets to Angular applications in angular.json
- [ ] T087 [P] Create comprehensive E2E test suite in tests/e2e/full-workflow.spec.ts

---

## Dependencies

### User Story Completion Order
1. **US1** (P1) - Event Registration must complete first (foundational authentication and core entities)
2. **US2** (P2) - Sessions depend on events from US1
3. **US3** (P3) - Speakers and social events depend on sessions from US2  
4. **US4** (P4) - External APIs can be developed in parallel after US1

### Parallel Execution Opportunities
- **Phase 1**: All project creation tasks (T002-T012) can run in parallel
- **Phase 2**: Infrastructure tasks (T014-T015, T017, T019, T021) can run in parallel after T013
- **Within User Stories**: Entity creation, UI components, and tests can be developed in parallel

---

## Implementation Strategy

### MVP Scope (Recommended)
**Target**: User Story 1 only for initial deployment
- Basic event discovery and registration
- OAuth 2.0 authentication  
- Queue-based capacity management
- SignalR real-time notifications

### Incremental Delivery
- **Iteration 1**: US1 (P1) - Core event registration
- **Iteration 2**: US1 + US2 - Add session management  
- **Iteration 3**: US1 + US2 + US3 - Complete event experience
- **Iteration 4**: Full system with external API integration

### Success Criteria
- ✅ Each user story independently testable
- ✅ Each story delivers standalone business value
- ✅ Parallel development opportunities maximized
- ✅ Constitutional requirements satisfied throughout