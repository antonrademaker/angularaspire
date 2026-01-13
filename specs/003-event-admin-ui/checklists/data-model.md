# Checklist: Data Model & Schema Quality

**Feature**: Event Administration UI
**Domain**: Data Model & Schema
**Rigor**: Deep Dive
**Focus**: In-Person Scenarios
**Created**: 2026-01-04

## Requirement Completeness
- [ ] CHK001 - Are all core entities (Event, Session, Speaker, Room) explicitly defined with primary keys? [Completeness, Spec §Data Model]
- [ ] CHK002 - Are audit columns (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy) specified for ALL main entities? [Completeness, Spec §FR-157, §FR-158]
- [ ] CHK003 - Is the `SessionAssignment` junction table defined with all necessary foreign keys (Session, Track, TimeSlot)? [Completeness, Spec §FR-193]
- [ ] CHK004 - Are `RoomConfiguration` and `RoomCombination` entities defined to handle complex physical setups? [Completeness, Spec §FR-205, §FR-206]
- [ ] CHK005 - Is the `EventParticipant` junction defined to handle multi-role users (Speaker + Attendee)? [Completeness, Spec §FR-218]
- [ ] CHK006 - Are `SessionMaterial` and `SessionFeedback` entities defined with necessary metadata? [Completeness, Spec §FR-223, §FR-229]
- [ ] CHK007 - Is the `AuditLog` entity defined with polymorphic target fields (EntityType, EntityId)? [Completeness, Spec §FR-272]
- [ ] CHK008 - Are `Sponsor` and `Announcement` entities defined with event linkage? [Completeness, Spec §FR-248, §FR-256]
- [ ] CHK009 - Is the `Equipment` entity defined with category and description fields? [Completeness, Spec §FR-259]
- [ ] CHK010 - Are `EventSeries` requirements defined for linking multiple event editions? [Completeness, Spec §FR-184]

## Requirement Clarity
- [ ] CHK011 - Is the `Status` enum for Events clearly defined with all allowed values? [Clarity, Spec §FR-235]
- [ ] CHK012 - Are the `Session` status flags (IsConfirmed, IsPublished) clearly distinguished from the base status? [Clarity, Spec §FR-242]
- [ ] CHK013 - Is the behavior of `RoomSnapshot` in `SessionAssignment` clearly specified (JSON vs Reference)? [Clarity, Spec §FR-161]
- [ ] CHK014 - Are the rules for `RoomCombination` capacity calculation explicitly defined? [Clarity, Spec §FR-207]
- [ ] CHK015 - Is the `Position` column in `Registration` clearly defined for waitlist ordering logic? [Clarity, Spec §FR-199]
- [ ] CHK016 - Are `TimeSlot` types (Session, Break, Lunch) clearly enumerated? [Clarity, Spec §Data Model]
- [ ] CHK017 - Is the `Discriminator` column for polymorphic `Notification` targets clearly specified? [Clarity, Spec §FR-211]
- [ ] CHK018 - Are `SessionMaterial` visibility options (Public, Attendees, Speakers) clearly defined? [Clarity, Spec §FR-228]

## Requirement Consistency
- [ ] CHK019 - Do `Event` and `Session` status lifecycles align without logical conflicts? [Consistency, Spec §FR-240, §FR-246]
- [ ] CHK020 - Is the `Person` entity usage consistent across Speaker, Attendee, and Organizer roles? [Consistency, Spec §FR-217]
- [ ] CHK021 - Are `Room` capacity rules consistent between simple rooms and room combinations? [Consistency, Spec §FR-209]
- [ ] CHK022 - Is the `Tag` entity usage consistent across Sessions, Tracks, and Speakers? [Consistency, Spec §FR-177]
- [ ] CHK023 - Are timestamp formats (UTC) consistent across all entities? [Consistency, Spec §FR-055]

## Data Integrity & Constraints
- [ ] CHK024 - Are uniqueness constraints defined for `ShortCode` fields on main entities? [Data Integrity, Spec §FR-168]
- [ ] CHK025 - Is the prevention of double-booking for `RoomCombination` explicitly required? [Data Integrity, Spec §FR-209]
- [ ] CHK026 - Are foreign key constraints specified for all relationship tables? [Data Integrity, Spec §Data Model]
- [ ] CHK027 - Is the requirement to prevent duplicate `SessionFeedback` from the same person defined? [Data Integrity, Spec §FR-233]
- [ ] CHK028 - Are `SessionAssignment` constraints defined to prevent overlapping slots in the same track? [Data Integrity, Spec §FR-011]
- [ ] CHK029 - Is the requirement to clear `IsPublished` flag when a session is Cancelled defined? [Data Integrity, Spec §FR-245]

## Scenario Coverage (In-Person Focus)
- [ ] CHK030 - Does the data model support physical room attributes (MapUrl, Accessibility)? [Coverage, Spec §FR-203, §FR-120]
- [ ] CHK031 - Are requirements defined for generating equipment lists per room? [Coverage, Spec §FR-262]
- [ ] CHK032 - Does the model support multi-floor venue maps? [Coverage, Spec §FR-143]
- [ ] CHK033 - Are requirements defined for printing/exporting badges or schedules (implied by in-person)? [Gap - Print/Badge support mentioned as out of scope but relevant for data model]
- [ ] CHK034 - Does the model support on-site check-in status (even if manual)? [Coverage, Spec §FR-039 - Check-in out of scope, but data model impact?]
- [ ] CHK035 - Are requirements defined for handling physical room changes last-minute? [Coverage, Spec §FR-083]

## Edge Case Coverage
- [ ] CHK036 - Are requirements defined for handling "soft deleted" entities in historical reports? [Edge Case, Spec §FR-159]
- [ ] CHK037 - Is the behavior defined for `SessionAssignment` when a Room becomes inactive? [Edge Case, Spec §FR-165]
- [ ] CHK038 - Are requirements defined for anonymizing data when a Person is GDPR-deleted? [Edge Case, Spec §FR-151]
- [ ] CHK039 - Is the handling of `TimeSlot` alignment across tracks defined? [Edge Case, Spec §FR-191]
- [ ] CHK040 - Are requirements defined for sessions spanning multiple consecutive time slots? [Edge Case, Spec §FR-195]

## Non-Functional Requirements (Data)
- [ ] CHK041 - Are UUIDv7 requirements specified for index performance? [Performance, Spec §FR-166]
- [ ] CHK042 - Are blob storage requirements defined for large files (SessionMaterial)? [Scalability, Spec §FR-225]
- [ ] CHK043 - Are optimistic concurrency requirements defined for schedule edits? [Concurrency, Spec §FR-036]
- [ ] CHK044 - Are GDPR logging requirements defined for compliance audits? [Compliance, Spec §FR-152]

## Dependencies & Assumptions
- [ ] CHK045 - Is the dependency on external Identity Provider (UserId) clearly documented? [Dependency, Spec §FR-217]
- [ ] CHK046 - Is the assumption about external blob storage availability validated? [Assumption, Spec §FR-225]
- [ ] CHK047 - Is the dependency on `Ical.Net` library for export compatible with the data model? [Dependency, Spec §FR-277]
