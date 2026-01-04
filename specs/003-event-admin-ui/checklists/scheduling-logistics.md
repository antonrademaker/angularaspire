# Checklist: Scheduling & Logistics Quality

**Feature**: Event Administration UI
**Domain**: Scheduling & Logistics
**Rigor**: Deep Dive
**Focus**: In-Person Scenarios
**Created**: 2026-01-04

## Requirement Completeness
- [ ] CHK001 - Are requirements defined for creating time slots with specific types (Session, Break, Lunch)? [Completeness, Spec §FR-010, §FR-043]
- [ ] CHK002 - Is the logic for preventing overlapping time slots within a track explicitly defined? [Completeness, Spec §FR-011]
- [ ] CHK003 - Are requirements defined for sessions spanning multiple consecutive time slots? [Completeness, Spec §FR-195]
- [ ] CHK004 - Is the behavior for assigning sessions to "non-session" slot types (Break, Lunch) defined? [Completeness, Spec §FR-045]
- [ ] CHK005 - Are requirements defined for "full-width" slots that span all tracks (e.g., Keynotes)? [Completeness, Spec §FR-046, §FR-192]
- [ ] CHK006 - Is the logic for copying time slot structures between days defined? [Completeness, Spec §FR-029, §FR-030]
- [ ] CHK007 - Are requirements defined for handling time zones in multi-day events? [Completeness, Spec §FR-055]
- [ ] CHK008 - Is the requirement to snapshot room configuration at assignment time defined? [Completeness, Spec §FR-161, §FR-174]

## Requirement Clarity
- [ ] CHK009 - Is "soft warning" vs "hard blocking" clearly distinguished for speaker conflicts? [Clarity, Spec §FR-028, §FR-100]
- [ ] CHK010 - Are the rules for "Session Capacity" vs "Room Capacity" overrides clearly defined? [Clarity, Spec §FR-063, §FR-064]
- [ ] CHK011 - Is the behavior of the waitlist promotion (FIFO) clearly specified? [Clarity, Spec §FR-077]
- [ ] CHK012 - Are the criteria for "matching duration" filtering in assignment UI clearly defined? [Clarity, Spec §FR-051]
- [ ] CHK013 - Is the definition of "Inactive Room" visibility in historical assignments clear? [Clarity, Spec §FR-165]
- [ ] CHK014 - Are the rules for equipment matching warnings (Session vs Room) explicit? [Clarity, Spec §FR-115]
- [ ] CHK015 - Is the behavior of "Personal Agenda" conflict warnings defined? [Clarity, Spec §FR-129]

## Requirement Consistency
- [ ] CHK016 - Do time slot alignment rules align with the "Event Time Block" model? [Consistency, Spec §FR-190]
- [ ] CHK017 - Is the handling of "Draft" events consistent with public schedule visibility? [Consistency, Spec §FR-019]
- [ ] CHK018 - Are speaker availability checks consistent with session assignment logic? [Consistency, Spec §FR-100]
- [ ] CHK019 - Do room configuration capacity rules align with session capacity validation? [Consistency, Spec §FR-173]

## Scenario Coverage (In-Person Focus)
- [ ] CHK020 - Are requirements defined for physical room change notifications to attendees? [Coverage, Spec §FR-083]
- [ ] CHK021 - Is the scenario of a speaker being in two rooms at once (e.g., panel + talk) addressed? [Coverage, Spec §FR-028]
- [ ] CHK022 - Are requirements defined for "Room Setup" changes between sessions (buffer time)? [Gap - Setup time not explicitly mentioned in FRs]
- [ ] CHK023 - Is the scenario of a room becoming unavailable (maintenance/issue) handled? [Coverage, Spec §FR-163]
- [ ] CHK024 - Are requirements defined for printing physical room schedules? [Gap - Print support]
- [ ] CHK025 - Is the handling of "Overflow Rooms" (simulcast) defined? [Gap - Advanced In-Person Scenario]

## Edge Case Coverage
- [ ] CHK026 - Is the behavior defined when a time slot is deleted with assigned sessions? [Edge Case, User Scenarios]
- [ ] CHK027 - Are requirements defined for handling timezone changes (DST) during an event? [Edge Case, Spec §FR-055]
- [ ] CHK028 - Is the behavior defined when a session's duration exceeds the time slot? [Edge Case, Spec §FR-052]
- [ ] CHK029 - Are requirements defined for handling "Unassigned" sessions that were previously scheduled? [Edge Case, Spec §FR-018]
- [ ] CHK030 - Is the behavior defined when a room's capacity is reduced *after* sessions are booked? [Edge Case, Spec §FR-035]

## Logistics & Operations
- [ ] CHK031 - Are requirements defined for equipment inventory management (not just tagging)? [Gap - Inventory counts]
- [ ] CHK032 - Is the requirement to track "Effective Date" for room changes defined? [Logistics, Spec §FR-034]
- [ ] CHK033 - Are requirements defined for "Session Prerequisites" enforcement? [Logistics, Spec §FR-067]
- [ ] CHK034 - Is the logic for "Session Difficulty" filtering defined? [Logistics, Spec §FR-127]
- [ ] CHK035 - Are requirements defined for "Session Language" display and filtering? [Logistics, Spec §FR-119]

## Dependencies & Assumptions
- [ ] CHK036 - Is the dependency on "Speaker Availability" data being populated validated? [Assumption, Spec §FR-099]
- [ ] CHK037 - Is the assumption that "Room Configuration" implies specific capacity validated? [Assumption, Spec §FR-171]
- [ ] CHK038 - Is the dependency on push/email services for change notifications documented? [Dependency, Spec §FR-082]
