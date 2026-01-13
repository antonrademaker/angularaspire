# Checklist: Security & Permissions Quality

**Feature**: Event Administration UI
**Domain**: Security & Permissions
**Rigor**: Deep Dive
**Focus**: In-Person Scenarios
**Created**: 2026-01-04

## Access Control & Roles
- [ ] CHK001 - Is the "Organizer" role explicitly defined with full edit permissions equivalent to the creator? [Access Control, Spec §FR-025]
- [ ] CHK002 - Are requirements defined for a user holding multiple roles (Speaker + Organizer) simultaneously? [Access Control, Spec §FR-022]
- [ ] CHK003 - Is the restriction that "Speakers" cannot manage their own availability explicitly defined? [Access Control, Spec §FR-099]
- [ ] CHK004 - Are requirements defined for removing an organizer and revoking their access immediately? [Access Control, User Story 6]
- [ ] CHK005 - Is the requirement to prevent speakers from seeing other speakers' session stats defined? [Access Control, Spec §FR-104]
- [ ] CHK006 - Are requirements defined for "Internal Review" voting permissions (Organizers only)? [Access Control, Spec §FR-133]

## Data Privacy & GDPR
- [ ] CHK007 - Are requirements defined for "Hard Delete" of personal data (Speaker, Attendee profiles)? [GDPR, Spec §FR-150]
- [ ] CHK008 - Is the requirement to "Anonymize" historical records (Session Feedback, Attendance) upon user deletion defined? [GDPR, Spec §FR-151]
- [ ] CHK009 - Is the requirement to log all GDPR deletion requests for compliance audits defined? [GDPR, Spec §FR-152]
- [ ] CHK010 - Are requirements defined for "Soft Delete" of non-personal entities (Events, Sessions)? [Data Privacy, Spec §FR-148]
- [ ] CHK011 - Is the requirement to "Hard Delete" association tables (Organizer, SessionAssignment) defined? [Data Privacy, Spec §FR-149]

## Visibility & Exposure
- [ ] CHK012 - Is the rule that "Draft" events must be invisible to ALL PublicApps defined? [Visibility, Spec §FR-019]
- [ ] CHK013 - Is the rule that "Private" events are visible ONLY to their assigned PublicApp defined? [Visibility, Spec §FR-021]
- [ ] CHK014 - Are requirements defined for "Session Material" visibility (Public vs Registered Attendees)? [Visibility, Spec §FR-094, §FR-228]
- [ ] CHK015 - Are requirements defined for "Session Recording" visibility (Public vs Registered Attendees)? [Visibility, Spec §FR-097]
- [ ] CHK016 - Is the requirement to hide "Rejected" sessions from public views defined? [Visibility, Spec §FR-269]
- [ ] CHK017 - Is the requirement to hide "Draft" sessions regardless of other flags defined? [Visibility, Spec §FR-244]
- [ ] CHK018 - Are requirements defined for controlling whether speakers see individual feedback comments? [Visibility, Spec §FR-089]

## Audit & Compliance
- [ ] CHK019 - Are requirements defined for logging "Status Changes" (e.g., Event Draft -> Public)? [Audit, Spec §FR-271]
- [ ] CHK020 - Is the requirement to capture "Old Value" and "New Value" for change comparison defined? [Audit, Spec §FR-273]
- [ ] CHK021 - Is the requirement to link audit logs to the specific "Person" who performed the action defined? [Audit, Spec §FR-274]
- [ ] CHK022 - Are requirements defined for tracking "Room Changes" with effective dates? [Audit, Spec §FR-034]
- [ ] CHK023 - Is the requirement to snapshot "Room Configuration" at assignment time (historical accuracy) defined? [Audit, Spec §FR-161]

## Content Security
- [ ] CHK024 - Are requirements defined for securing "Session Material" downloads (e.g., signed URLs)? [Gap - Secure Download Implementation]
- [ ] CHK025 - Is the requirement to validate file types/MIME types for uploads defined? [Gap - Upload Security]
- [ ] CHK026 - Are requirements defined for sanitizing "Rich Text" content in Announcements/Descriptions? [Gap - XSS Prevention]
- [ ] CHK027 - Is the requirement to prevent "ID Enumeration" attacks (using UUIDv7) implied/defined? [Security, Spec §FR-166]

## Dependencies & Assumptions
- [ ] CHK028 - Is the dependency on the "PrivateApp" authentication infrastructure clearly stated? [Assumption, Spec §Assumptions]
- [ ] CHK029 - Is the assumption that "PublicApp" identifiers are securely configured validated? [Assumption, Spec §FR-026]
