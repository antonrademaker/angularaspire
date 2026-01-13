# Phase 0: Research & Unknowns

**Feature**: Event Administration UI
**Branch**: `003-event-admin-ui`

## Unknowns & Clarifications

### 1. Room Combination Modeling
**Context**: FR-206 requires a `RoomCombination` entity linking multiple rooms.
**Question**: How should this be modeled in EF Core to ensure data integrity and prevent double-booking (FR-209)?
**Research Task**: Evaluate EF Core patterns for composite resources.
**Decision**: Use a `RoomCombination` entity that has a many-to-many relationship with `Room`. Add a `RoomCombinationRoom` join table.
**Rationale**: Allows explicit definition of combinations. Double-booking prevention must be handled in application logic (checking if any room in a combination is booked, or if the combination itself is booked).

### 2. Blob Storage Infrastructure
**Context**: FR-225 requires external blob storage for session materials.
**Question**: What is the existing infrastructure for blob storage in the Aspire setup?
**Research Task**: Check `AppHost` for existing storage resources.
**Decision**: Use Azure Blob Storage (or emulator for dev).
**Rationale**: Standard Aspire pattern.

### 3. Calendar Export Library
**Context**: FR-277 requires on-demand iCal generation.
**Question**: Which .NET library to use?
**Research Task**: Evaluate `Ical.Net`.
**Decision**: Use `Ical.Net`.
**Rationale**: Industry standard, robust, active.

### 4. Notification Polymorphism
**Context**: FR-211 requires polymorphic notifications (Attendee, Speaker, Broadcast).
**Question**: EF Core inheritance strategy?
**Research Task**: Compare TPH vs TPT.
**Decision**: Use Table-Per-Hierarchy (TPH) with a `Discriminator` column.
**Rationale**: Better performance for querying all notifications for a user regardless of type.

## Technology Choices

| Area | Choice | Rationale |
|------|--------|-----------|
| **ORM** | EF Core 10 | Standard for solution |
| **Blob Storage** | Azure Blob Storage | Scalable, Aspire-supported |
| **Calendar** | Ical.Net | Standard library |
| **IDs** | UUIDv7 | Time-sortable GUIDs for performance |
| **Validation** | FluentValidation | Clean separation of rules |

## Best Practices

- **Audit Trail**: Use `AuditLog` entity populated via EF Core Interceptors.
- **Soft Delete**: Global query filters in EF Core.
- **Concurrency**: Optimistic concurrency with `RowVersion` or `UpdatedAt` checks.
