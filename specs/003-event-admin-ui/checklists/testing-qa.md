# Checklist: Testing & QA Quality

**Feature**: Event Administration UI
**Domain**: Testing & QA
**Rigor**: Deep Dive
**Focus**: In-Person Scenarios
**Created**: 2026-01-04

## Acceptance Criteria Quality
- [ ] CHK001 - Are "Given-When-Then" scenarios defined for all P1 User Stories? [Completeness, Spec §User Scenarios]
- [ ] CHK002 - Are "Independent Tests" defined for every User Story to allow isolated verification? [Testability, Spec §User Scenarios]
- [ ] CHK003 - Is the "Success Criterion" for event creation speed (3 minutes) objectively measurable? [Measurability, Spec §SC-001]
- [ ] CHK004 - Is the "Success Criterion" for schedule building speed (30 minutes) measurable? [Measurability, Spec §SC-002]
- [ ] CHK005 - Is the "Success Criterion" for drag-and-drop responsiveness (100ms) testable with standard tools? [Measurability, Spec §SC-007]
- [ ] CHK006 - Are acceptance criteria defined for "Negative Scenarios" (e.g., invalid dates, overlapping slots)? [Coverage, Spec §Edge Cases]

## Test Scenario Coverage
- [ ] CHK007 - Are test scenarios defined for "Multi-Day" event creation? [Coverage, Spec §User Story 1]
- [ ] CHK008 - Are test scenarios defined for "Session Swapping" between tracks? [Coverage, Spec §User Story 5]
- [ ] CHK009 - Are test scenarios defined for "Concurrent Edits" by multiple organizers? [Coverage, Spec §FR-037]
- [ ] CHK010 - Are test scenarios defined for "Timezone Display" verification (Local vs Remote)? [Coverage, Spec §FR-056]
- [ ] CHK011 - Are test scenarios defined for "Waitlist Promotion" logic? [Coverage, Spec §FR-077]
- [ ] CHK012 - Are test scenarios defined for "Room Capacity" enforcement? [Coverage, Spec §FR-063]

## Performance & Load Testing
- [ ] CHK013 - Is the load test target defined for "Schedule View" rendering with 100+ sessions? [Performance, Spec §SC-004]
- [ ] CHK014 - Is the latency target defined for "PublicApp" status reflection (5 seconds)? [Performance, Spec §SC-006]
- [ ] CHK015 - Are stress test requirements defined for "High Concurrency" registration/RSVP? [Performance, Gap]
- [ ] CHK016 - Are requirements defined for testing "Large File Uploads" (Session Materials)? [Performance, Gap]

## Edge Case & Resilience Testing
- [ ] CHK017 - Are test cases defined for "Network Failure" during drag-and-drop operations? [Resilience, Gap]
- [ ] CHK018 - Are test cases defined for "Session Deletion" when attendees are already registered? [Edge Case, Spec §Edge Cases]
- [ ] CHK019 - Are test cases defined for "Speaker Availability" conflicts? [Edge Case, Spec §FR-100]
- [ ] CHK020 - Are test cases defined for "Daylight Savings Time" transitions during an event? [Edge Case, Spec §FR-055]

## User Acceptance Testing (UAT)
- [ ] CHK021 - Is the "No Documentation" usability goal (90% success) defined with a testing protocol? [Usability, Spec §SC-005]
- [ ] CHK022 - Are UAT scenarios defined for "Mobile" users (Organizers on the go)? [Coverage, Gap]
- [ ] CHK023 - Are UAT scenarios defined for "Accessibility" verification (Screen Readers)? [Coverage, Gap]

## Data & State Testing
- [ ] CHK024 - Are verification steps defined for "Audit Log" accuracy after complex edits? [Data Integrity, Spec §FR-273]
- [ ] CHK025 - Are verification steps defined for "Room Snapshot" history after room updates? [Data Integrity, Spec §FR-162]
- [ ] CHK026 - Are verification steps defined for "GDPR Anonymization" results? [Data Integrity, Spec §FR-151]
