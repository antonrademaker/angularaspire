# Implementation Tasks: Event Administration UI

**Feature**: Event Administration UI (`003-event-admin-ui`)
**Spec**: [specs/003-event-admin-ui/spec.md](specs/003-event-admin-ui/spec.md)
**Status**: Pending

## Dependencies
- **Phase 1 (Setup)** must be completed before any other phase.
- **Phase 2 (Foundational)** must be completed before User Stories.
- **Phase 3 (US1)** is a prerequisite for all subsequent User Stories.
- **Phase 4 (US2)** and **Phase 5 (US3)** can be executed in parallel but are prerequisites for **Phase 6 (US4)**.
- **Phase 6 (US4)** is a prerequisite for **Phase 7 (US5)**.

## Phase 1: Setup & Infrastructure
**Goal**: Initialize project structure and shared kernel dependencies.

- [x] T001 Create EventManagement service structure in [src/Shared/EventManagement/](src/Shared/EventManagement/)
- [x] T002 Create PrivateApi controller structure in [src/PrivateApi/EventManagement/](src/PrivateApi/EventManagement/)
- [x] T003 Create Angular module structure in [src/PrivateApp/src/app/event-management/](src/PrivateApp/src/app/event-management/)
- [x] T004 [P] Register EventManagementDbContext in [src/AppHost/Program.cs](src/AppHost/Program.cs)
- [x] T005 [P] Configure OpenTelemetry for EventManagement in [src/ServiceDefaults/Extensions.cs](src/ServiceDefaults/Extensions.cs)

## Phase 2: Foundational Data & Services
**Goal**: Implement core shared entities and base service infrastructure.

- [x] T006 Implement Location and Room entities in [src/Shared/EventManagement/Entities/Location.cs](src/Shared/EventManagement/Entities/Location.cs)
- [x] T007 Implement Tag entity in [src/Shared/EventManagement/Entities/Tag.cs](src/Shared/EventManagement/Entities/Tag.cs)
- [x] T008 Implement Person entity (Organizer/Speaker base) in [src/Shared/EventManagement/Entities/Person.cs](src/Shared/EventManagement/Entities/Person.cs)
- [x] T009 Create EF Core migrations for foundational entities in [src/Shared/EventManagement/Data/Migrations/](src/Shared/EventManagement/Data/Migrations/)
- [x] T010 Implement BaseEventService with common validation logic in [src/Shared/EventManagement/Services/BaseEventService.cs](src/Shared/EventManagement/Services/BaseEventService.cs)

## Phase 3: User Story 1 - Create and Configure Event
**Goal**: Enable organizers to create new events (P1).

- [x] T011 [US1] Implement Event and EventSeries entities in [src/Shared/EventManagement/Entities/Event.cs](src/Shared/EventManagement/Entities/Event.cs)
- [ ] T012 [US1] Implement CreateEvent and GetEvent endpoints in [src/PrivateApi/EventManagement/EventController.cs](src/PrivateApi/EventManagement/EventController.cs)
- [ ] T013 [US1] Create Event List component in [src/PrivateApp/src/app/event-management/components/event-list/event-list.component.ts](src/PrivateApp/src/app/event-management/components/event-list/event-list.component.ts)
- [ ] T014 [US1] Create Event Create/Edit Form in [src/PrivateApp/src/app/event-management/components/event-form/event-form.component.ts](src/PrivateApp/src/app/event-management/components/event-form/event-form.component.ts)
- [ ] T015 [US1] Implement multi-day date picker validation in [src/PrivateApp/src/app/shared-ui/validators/date-range.validator.ts](src/PrivateApp/src/app/shared-ui/validators/date-range.validator.ts)
- [ ] T016 [US1] [P] Write integration tests for Event creation in [tests/PrivateApi.Tests/EventManagement/EventTests.cs](tests/PrivateApi.Tests/EventManagement/EventTests.cs)

## Phase 4: User Story 2 - Manage Event Tracks
**Goal**: Enable organizers to define tracks for sessions (P1).

- [ ] T017 [US2] Implement Track entity in [src/Shared/EventManagement/Entities/Track.cs](src/Shared/EventManagement/Entities/Track.cs)
- [ ] T018 [US2] Implement Track management endpoints (Add, Update, Delete, Reorder) in [src/PrivateApi/EventManagement/TrackController.cs](src/PrivateApi/EventManagement/TrackController.cs)
- [ ] T019 [US2] Create Track Management UI component in [src/PrivateApp/src/app/event-management/components/track-manager/track-manager.component.ts](src/PrivateApp/src/app/event-management/components/track-manager/track-manager.component.ts)
- [ ] T020 [US2] Implement drag-and-drop reordering for tracks in [src/PrivateApp/src/app/event-management/components/track-list/track-list.component.ts](src/PrivateApp/src/app/event-management/components/track-list/track-list.component.ts)

## Phase 5: User Story 3 - Manage Time Slots
**Goal**: Enable organizers to define the schedule grid (P1).

- [ ] T021 [US3] Implement TimeSlot entity in [src/Shared/EventManagement/Entities/TimeSlot.cs](src/Shared/EventManagement/Entities/TimeSlot.cs)
- [ ] T022 [US3] Implement TimeSlot management endpoints in [src/PrivateApi/EventManagement/TimeSlotController.cs](src/PrivateApi/EventManagement/TimeSlotController.cs)
- [ ] T023 [US3] Create Schedule Grid visualization component in [src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.ts](src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.ts)
- [ ] T024 [US3] Implement "Copy Day Schedule" logic in [src/Shared/EventManagement/Services/ScheduleService.cs](src/Shared/EventManagement/Services/ScheduleService.cs)

## Phase 6: User Story 4 - Assign Sessions
**Goal**: Enable assigning sessions to time slots (P2).

- [ ] T025 [US4] Implement Session and SessionAssignment entities in [src/Shared/EventManagement/Entities/Session.cs](src/Shared/EventManagement/Entities/Session.cs)
- [ ] T026 [US4] Implement AssignSession endpoint in [src/PrivateApi/EventManagement/SessionController.cs](src/PrivateApi/EventManagement/SessionController.cs)
- [ ] T027 [US4] Create Unassigned Sessions Sidebar component in [src/PrivateApp/src/app/event-management/components/session-sidebar/session-sidebar.component.ts](src/PrivateApp/src/app/event-management/components/session-sidebar/session-sidebar.component.ts)
- [ ] T028 [US4] Implement Drag-and-Drop assignment logic in [src/PrivateApp/src/app/event-management/services/schedule-drag-drop.service.ts](src/PrivateApp/src/app/event-management/services/schedule-drag-drop.service.ts)

## Phase 7: User Story 5 - Swap Sessions
**Goal**: Enable optimizing the schedule via swapping (P2).

- [ ] T029 [US5] Implement SwapSessions endpoint logic in [src/Shared/EventManagement/Services/SessionService.cs](src/Shared/EventManagement/Services/SessionService.cs)
- [ ] T030 [US5] Update Drag-and-Drop service to handle swap operations in [src/PrivateApp/src/app/event-management/services/schedule-drag-drop.service.ts](src/PrivateApp/src/app/event-management/services/schedule-drag-drop.service.ts)
- [ ] T031 [US5] [P] Write unit tests for swap validation logic in [tests/Shared.Tests/EventManagement/SessionSwapTests.cs](tests/Shared.Tests/EventManagement/SessionSwapTests.cs)

## Phase 8: User Story 6 - Manage Organizers
**Goal**: Enable collaborative event management (P2).

- [ ] T032 [US6] Implement EventOrganizer junction entity in [src/Shared/EventManagement/Entities/EventOrganizer.cs](src/Shared/EventManagement/Entities/EventOrganizer.cs)
- [ ] T033 [US6] Implement Add/Remove Organizer endpoints in [src/PrivateApi/EventManagement/OrganizerController.cs](src/PrivateApi/EventManagement/OrganizerController.cs)
- [ ] T034 [US6] Create Organizer Management UI in [src/PrivateApp/src/app/event-management/components/organizer-list/organizer-list.component.ts](src/PrivateApp/src/app/event-management/components/organizer-list/organizer-list.component.ts)

## Phase 9: User Story 7 - Event Status & Visibility
**Goal**: Manage event lifecycle (P2).

- [ ] T035 [US7] Implement Status State Machine validation in [src/Shared/EventManagement/Domain/EventStatusMachine.cs](src/Shared/EventManagement/Domain/EventStatusMachine.cs)
- [ ] T036 [US7] Implement PublishEvent endpoint with validation in [src/PrivateApi/EventManagement/EventController.cs](src/PrivateApi/EventManagement/EventController.cs)
- [ ] T037 [US7] Update PublicApp to filter events by status in [src/PublicApi/EventManagement/PublicEventController.cs](src/PublicApi/EventManagement/PublicEventController.cs)

## Phase 10: User Story 10 - Mobile Experience
**Goal**: Enable mobile management (P2).

- [ ] T038 [US10] Implement responsive CSS grid for Schedule View in [src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.scss](src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.scss)
- [ ] T039 [US10] Create Mobile List View component for schedule in [src/PrivateApp/src/app/event-management/components/mobile-schedule/mobile-schedule.component.ts](src/PrivateApp/src/app/event-management/components/mobile-schedule/mobile-schedule.component.ts)
- [ ] T040 [US10] Implement touch-friendly drag handles in [src/PrivateApp/src/app/event-management/components/session-card/session-card.component.ts](src/PrivateApp/src/app/event-management/components/session-card/session-card.component.ts)

## Phase 11: User Story 11 - Accessibility
**Goal**: Ensure inclusive access (P2).

- [ ] T041 [US11] Add ARIA labels to Schedule Grid and Session Cards in [src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.html](src/PrivateApp/src/app/event-management/components/schedule-grid/schedule-grid.component.html)
- [ ] T042 [US11] Implement keyboard navigation (arrow keys) for grid in [src/PrivateApp/src/app/event-management/directives/keyboard-nav.directive.ts](src/PrivateApp/src/app/event-management/directives/keyboard-nav.directive.ts)
- [ ] T043 [US11] Implement Live Region service for announcements in [src/PrivateApp/src/app/shared-ui/services/a11y-announcer.service.ts](src/PrivateApp/src/app/shared-ui/services/a11y-announcer.service.ts)

## Phase 12: Polish & Cross-Cutting
**Goal**: Security, Performance, and Resilience.

- [ ] T044 Implement File Upload Validation (FR-299) in [src/PrivateApi/Middleware/FileUploadValidator.cs](src/PrivateApi/Middleware/FileUploadValidator.cs)
- [ ] T045 Implement Rate Limiting (FR-305) in [src/PublicApi/Program.cs](src/PublicApi/Program.cs)
- [ ] T046 Implement Stress Test Script (FR-306) in [tests/k6/stress-test-schedule.js](tests/k6/stress-test-schedule.js)
- [ ] T047 Implement UI Rollback Logic (FR-307) in [src/PrivateApp/src/app/event-management/store/schedule.effects.ts](src/PrivateApp/src/app/event-management/store/schedule.effects.ts)
