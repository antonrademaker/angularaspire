# Feature Specification: Event Administration UI

**Feature Branch**: `003-event-admin-ui`  
**Created**: 2026-01-04  
**Status**: Draft  
**Input**: User description: "Add event management to the PrivateApp: create multi-day events with tracks, time slots, sessions. Support event editions, organizers, draft/public/private status. Enable easy session swapping between time slots and tracks."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Configure a New Event (Priority: P1)

As an event organizer, I need to create a new event with basic information so I can start planning a conference or meetup.

**Why this priority**: Without the ability to create events, no other functionality can be used. This is the foundational capability.

**Independent Test**: Can be fully tested by creating an event with name, dates, and status. Delivers immediate value by establishing the event container for all subsequent planning.

**Acceptance Scenarios**:

1. **Given** I am logged in as an admin, **When** I click "Create Event" and enter name, start date, end date, and optional edition, **Then** a new event is created in Draft status
2. **Given** I am creating an event, **When** I select dates spanning multiple days, **Then** the system accepts the date range and displays the event duration
3. **Given** an event exists with the same name, **When** I create a new event with that name and different edition, **Then** both events are linked as editions of the same series
4. **Given** I have created an event, **When** I view the event list, **Then** I can see the event with its status (Draft/Public/Private)

---

### User Story 2 - Manage Event Tracks (Priority: P1)

As an event organizer, I need to create and manage tracks within an event so I can organize sessions by topic or audience.

**Why this priority**: Tracks are required to organize time slots and sessions. Without tracks, sessions cannot be scheduled.

**Independent Test**: Can be tested by creating an event, adding tracks with names and tags, and verifying they appear in the event structure.

**Acceptance Scenarios**:

1. **Given** an event exists, **When** I add a new track with a name and subject tags, **Then** the track appears in the event's track list
2. **Given** an event has multiple tracks, **When** I reorder the tracks, **Then** the display order is updated
3. **Given** a track exists, **When** I edit its name or tags, **Then** the changes are saved and visible immediately
4. **Given** a track has no assigned sessions, **When** I delete the track, **Then** the track is removed from the event

---

### User Story 3 - Create and Manage Time Slots (Priority: P1)

As an event organizer, I need to define time slots within tracks so I know when sessions can be scheduled.

**Why this priority**: Time slots define the schedule structure. Sessions cannot be assigned without time slots.

**Independent Test**: Can be tested by creating time slots in a track and verifying the schedule grid displays correctly.

**Acceptance Scenarios**:

1. **Given** a track exists, **When** I create a time slot with start time, end time, and optional room, **Then** the time slot appears in the track schedule
2. **Given** I am creating time slots for a multi-day event, **When** I specify different dates, **Then** the time slots are grouped by day
3. **Given** multiple tracks exist, **When** I view the schedule, **Then** I see a grid/timeline view showing all tracks and their time slots
4. **Given** a time slot exists, **When** I edit its times or room, **Then** the schedule updates accordingly

---

### User Story 4 - Assign Sessions to Time Slots (Priority: P2)

As an event organizer, I need to assign existing sessions to time slots so I can build the event schedule.

**Why this priority**: Core scheduling capability that depends on events, tracks, and time slots being in place.

**Independent Test**: Can be tested by dragging an unassigned session to a time slot and verifying assignment.

**Acceptance Scenarios**:

1. **Given** sessions and time slots exist, **When** I drag a session onto an empty time slot, **Then** the session is assigned to that slot
2. **Given** I am viewing the schedule, **When** I see unassigned sessions, **Then** they appear in a sidebar/panel for easy access
3. **Given** a session is assigned to a time slot, **When** I view the schedule, **Then** I see the session title, speakers, and duration in the slot
4. **Given** a session is assigned, **When** I click the session in the schedule, **Then** I can view session details and edit assignment

---

### User Story 5 - Swap Sessions Between Slots and Tracks (Priority: P2)

As an event organizer, I need to easily move sessions between time slots and tracks so I can optimize the schedule.

**Why this priority**: Schedule optimization is critical for good attendee experience and managing speaker availability.

**Independent Test**: Can be tested by dragging a session from one slot to another and verifying the swap.

**Acceptance Scenarios**:

1. **Given** a session is in time slot A, **When** I drag it to empty time slot B, **Then** the session moves to slot B
2. **Given** sessions exist in slot A and slot B, **When** I drag session from A to B, **Then** the sessions swap positions
3. **Given** a session is in Track 1, **When** I drag it to a time slot in Track 2, **Then** the session moves to Track 2
4. **Given** I make a schedule change, **When** I view the session's history, **Then** I can see the previous slot assignment

---

### User Story 6 - Manage Event Organizers (Priority: P2)

As an event owner, I need to assign organizers to an event so multiple people can help manage it.

**Why this priority**: Collaboration is important but not blocking for initial event setup.

**Independent Test**: Can be tested by adding a user as organizer and verifying they can access event management.

**Acceptance Scenarios**:

1. **Given** an event exists, **When** I add a user as an organizer, **Then** they appear in the organizers list
2. **Given** a user is an organizer, **When** they log in, **Then** they can edit the event configuration
3. **Given** a speaker exists in the system, **When** I add them as an organizer, **Then** they have both speaker and organizer roles for that event
4. **Given** multiple organizers exist, **When** I remove one, **Then** they no longer have edit access to the event

---

### User Story 7 - Manage Event Status and Visibility (Priority: P2)

As an event organizer, I need to control event visibility so I can prepare events privately before publishing.

**Why this priority**: Essential for managing the event lifecycle from planning to public announcement.

**Independent Test**: Can be tested by changing event status and verifying visibility rules in PublicApp.

**Acceptance Scenarios**:

1. **Given** an event is in Draft status, **When** I view PublicApp, **Then** the event is not visible
2. **Given** an event is in Draft, **When** I change status to Public, **Then** the event becomes visible in any PublicApp showing public events
3. **Given** an event is marked Private, **When** I view the assigned PublicApp, **Then** the event is visible only there
4. **Given** an event is Private, **When** I view other PublicApps, **Then** the event is not visible

---

### User Story 8 - Link Event Editions (Priority: P3)

As an event organizer, I need to link multiple editions of the same event so attendees can see event history.

**Why this priority**: Nice-to-have feature that enhances user experience but is not required for basic operation.

**Independent Test**: Can be tested by creating multiple events with same name/code and verifying they link as editions.

**Acceptance Scenarios**:

1. **Given** events "TechConf 2024" and "TechConf 2025" exist, **When** I view either event, **Then** I can see links to other editions
2. **Given** I am creating a new edition, **When** I select an existing event series, **Then** the new event is automatically linked
3. **Given** an event has previous editions, **When** I view in PublicApp, **Then** previous editions are shown (if their status allows)

---

### User Story 9 - Tag Management for Tracks (Priority: P3)

As an event organizer, I need to assign subject tags to tracks so attendees can filter by interest.

**Why this priority**: Enhances discoverability but tracks function without tags.

**Independent Test**: Can be tested by adding tags to a track and filtering tracks by tag.

**Acceptance Scenarios**:

1. **Given** I am editing a track, **When** I add subject tags, **Then** the tags are associated with the track
2. **Given** tracks have tags, **When** I view the schedule, **Then** I can filter tracks by tag
3. **Given** a session has tags, **When** it is assigned to a track with matching tags, **Then** the system highlights the match

---

### User Story 10 - Mobile Experience (Priority: P2)

As an organizer on the go, I need to manage the event schedule from my mobile device so I can handle last-minute changes during the event.

**Why this priority**: Critical for "day-of" operations when organizers are not at their desks.

**Independent Test**: Can be tested by accessing the admin UI from a mobile viewport and performing key actions.

**Acceptance Scenarios**:

1. **Given** I am on a mobile device, **When** I view the schedule, **Then** I see a responsive list view instead of a wide grid
2. **Given** I am on mobile, **When** I tap a session, **Then** I can edit its room or time slot
3. **Given** I am on mobile, **When** I need to announce a change, **Then** I can post an announcement easily

---

### User Story 11 - Accessibility Verification (Priority: P2)

As an organizer with accessibility needs, I need to be able to navigate the schedule using a keyboard and screen reader.

**Why this priority**: Compliance and inclusivity requirement.

**Independent Test**: Can be tested using a screen reader (e.g., NVDA, VoiceOver) and keyboard-only navigation.

**Acceptance Scenarios**:

1. **Given** I am using a screen reader, **When** I navigate the schedule grid, **Then** the relationships between time slots, tracks, and sessions are announced clearly
2. **Given** I am using keyboard only, **When** I tab through the interface, **Then** focus indicators are visible and logical
3. **Given** I am using a screen reader, **When** a dynamic update occurs (e.g., drag-and-drop), **Then** the change is announced via ARIA live regions

---

### Edge Cases

- What happens when a time slot is deleted that has an assigned session? → Session becomes unassigned, user is warned before deletion
- How does system handle overlapping time slots in the same track? → System prevents creation of overlapping slots
- What happens when a speaker assigned to a session is also scheduled in another session at the same time? → System warns of conflict but allows it (speaker may be in multiple rooms)
- How does the system handle timezone differences for multi-timezone events? → All times stored in UTC, displayed in event's configured timezone
- What happens when an event with sessions is changed from Public to Draft? → All sessions remain, event just becomes invisible in PublicApp
- What happens when deleting an event with sessions and speakers assigned? → Confirm deletion, cascade removes session-event links (speakers remain in system)

## Requirements *(mandatory)*

### Functional Requirements

**Event Management**
- **FR-001**: System MUST allow creation of events with name, start date, end date, and optional edition identifier
- **FR-002**: System MUST support events spanning multiple days
- **FR-003**: System MUST support event status values: Draft, Public, Private
- **FR-004**: System MUST link events as editions via explicit series code (not automatic name matching)
- **FR-005**: System MUST support zero or more organizers per event
- **FR-053**: System MUST allow cloning an existing event's structure (tracks, time slots, rooms) to a new event
- **FR-054**: System MUST allow selecting which elements to clone (tracks only, tracks+slots, full structure)

**Track Management**
- **FR-006**: System MUST allow creation of multiple tracks per event
- **FR-007**: System MUST allow assigning subject tags to tracks
- **FR-008**: System MUST allow reordering tracks within an event
- **FR-009**: System MUST prevent deletion of tracks with assigned sessions unless confirmed

**Tag Management**
- **FR-038**: System MUST support a predefined tag library managed by admins
- **FR-039**: System MUST allow organizers to add new tags (freeform entry)
- **FR-040**: System MUST provide autocomplete from existing tags to prevent duplicates

**Time Slot Management**
- **FR-010**: System MUST allow creation of time slots within tracks with start time, end time, and optional room
- **FR-011**: System MUST prevent overlapping time slots within the same track
- **FR-012**: System MUST display time slots in a visual schedule/grid format
- **FR-013**: System MUST group time slots by day for multi-day events
- **FR-029**: System MUST allow copying time slot structure (empty slots) from one day to another
- **FR-030**: System MUST allow copying a complete day's schedule (time slots with session assignments) to another day
- **FR-043**: System MUST support special slot types: Session, Break, Lunch, Networking, Keynote
- **FR-044**: System MUST display special slot types with distinct visual styling
- **FR-045**: System MUST prevent session assignment to non-session slot types (Break, Lunch, Networking)
- **FR-046**: System MUST allow configuring time slots to span all tracks visually (full-width display)
- **FR-284**: System MUST allow arbitrary gaps between time slots (buffer time) for room setup or transitions

**Room & Location Management**
- **FR-031**: System MUST support Location entity with name and address
- **FR-032**: System MUST support Room entity with name, capacity, and equipment tags, belonging to a Location
- **FR-033**: System MUST preserve room state at session assignment time (past sessions show historical room data)
- **FR-034**: System MUST track room changes with effective dates (room history)
- **FR-035**: System MUST NOT retroactively change room information for past session assignments when room is updated
- **FR-285**: System MUST display a warning for future sessions if a room's capacity is reduced below the current registration count

**Logistics & Printing**
- **FR-286**: System MUST support batch printing of session schedules (e.g., all sessions in a track or room)
- **FR-287**: System MUST support on-the-spot printing of individual session details or room schedules
- **FR-288**: Printed schedules MUST include session title, time, speakers, and room name

**Session Assignment**
- **FR-014**: System MUST allow assigning sessions to time slots via drag-and-drop
- **FR-015**: System MUST allow moving sessions between time slots within the same track
- **FR-016**: System MUST allow moving sessions between different tracks
- **FR-017**: System MUST allow swapping two sessions between their time slots
- **FR-018**: System MUST display unassigned sessions in an accessible panel
- **FR-019**: System MUST allow the same session to be assigned to multiple time slots (for repeating sessions)
- **FR-028**: System MUST display a visual conflict indicator when a speaker is scheduled in overlapping time slots (soft warning, no blocking)

**Session Types**
- **FR-047**: System MUST support session types (e.g., Workshop, Talk, Panel, Lightning Talk) with suggested durations
- **FR-048**: Admins MUST be able to configure global session types and default durations (system-wide)
- **FR-049**: Organizers MUST be able to override session type durations at the event level
- **FR-050**: Organizers MUST be able to add event-specific session types
- **FR-051**: System MUST filter/prioritize sessions matching time slot duration in assignment UI
- **FR-052**: System MUST allow assigning sessions with non-matching duration (override with visual indicator)

**Visibility Rules**
- **FR-019**: System MUST hide Draft events from all PublicApps
- **FR-020**: System MUST show Public events in any PublicApp configured for public events
- **FR-021**: System MUST show Private events only in the specifically assigned PublicApp
- **FR-026**: Each PublicApp deployment MUST have a unique identifier configured in deployment settings (appsettings/environment)
- **FR-027**: System MUST store event-to-PublicApp assignments in the database, referencing the deployment identifier

**Timezone Display**
- **FR-055**: System MUST store event times in UTC with event's local timezone reference
- **FR-056**: System MUST display times in event's local timezone for in-person attendees
- **FR-057**: System MUST display dual times for remote attendees (user timezone primary, event timezone secondary)
- **FR-058**: System MUST determine attendance mode from user's registration or preference

**Hybrid Events**
- **FR-059**: System MUST support event-level hybrid mode setting (in-person only, virtual only, hybrid)
- **FR-060**: When event is hybrid, system MUST allow per-session attendance mode (in-person, virtual, both)
- **FR-061**: System MUST display attendance mode indicator on sessions in schedule view

**Session Capacity**
- **FR-062**: System MUST support session capacity limits
- **FR-063**: Session capacity MUST default to assigned room's capacity
- **FR-064**: Organizers MUST be able to override session capacity independent of room capacity

**Session Prerequisites**
- **FR-065**: System MUST allow linking sessions as "recommended prior" (soft prerequisite)
- **FR-066**: System MUST display prerequisite recommendations in session details view
- **FR-067**: System MUST warn organizers when scheduling prerequisite session after dependent session

**Schedule Filtering**
- **FR-068**: System MUST support filtering schedule by speaker (multi-select)
- **FR-069**: System MUST support filtering schedule by tag (multi-select)
- **FR-070**: System MUST support filtering schedule by track (multi-select)
- **FR-071**: System MUST support combining filters (AND logic across categories)

**Session Registration**
- **FR-072**: System MUST support optional session RSVP (track interest, no enforcement)
- **FR-073**: System MUST allow sessions to require registration with capacity enforcement (per session setting)
- **FR-074**: Workshop session type MUST support required registration by default
- **FR-075**: System MUST display registration count vs capacity for sessions with registration enabled

**Waitlist**
- **FR-076**: System MUST support waitlist for sessions that reach capacity
- **FR-077**: System MUST auto-promote waitlisted attendees when spots open (FIFO order)
- **FR-078**: System MUST notify attendees when promoted from waitlist

**Calendar Export**
- **FR-079**: System MUST support exporting personalized schedule to iCal format
- **FR-080**: Export MUST include only sessions user has registered/RSVP'd for
- **FR-081**: System MUST support "Add to Google Calendar" direct link for individual sessions

**Change Notifications**
- **FR-082**: System MUST notify registered attendees when session time changes (push/email)
- **FR-083**: System MUST notify registered attendees when session room changes (push/email)
- **FR-084**: System MUST notify registered attendees when session is cancelled (push/email)
- **FR-085**: Organizers MUST be able to include custom message with change notifications

**Session Feedback**
- **FR-086**: System MUST support session feedback with star rating (1-5)
- **FR-087**: System MUST support optional written comments with feedback
- **FR-088**: System MUST allow speakers to view aggregate ratings for their sessions
- **FR-089**: System MUST allow configuring whether speakers see individual comments (organizer setting)
- **FR-090**: System MUST only allow feedback from attendees who registered/attended the session

**Session Materials**
- **FR-091**: System MUST allow speakers to upload session materials (slides, handouts, code samples)
- **FR-092**: System MUST allow speakers to provide external URLs for materials (e.g., SharePoint, GitHub)
- **FR-093**: System MUST allow attendees to download uploaded materials from session details
- **FR-094**: System MUST support configuring material visibility (registered attendees only vs all)

**Session Recordings**
- **FR-095**: System MUST allow organizers to add video recording URL to sessions (post-event)
- **FR-096**: System MUST display recording link in session details when available
- **FR-097**: System MUST support configuring recording visibility (registered attendees only vs all)

**Speaker Availability**
- **FR-098**: System MUST support defining speaker availability windows per event
- **FR-099**: Organizers MUST be able to manage speaker availability (speakers do not self-manage)
- **FR-100**: System MUST warn when scheduling a session outside speaker's available times
- **FR-101**: System MUST NOT send notifications to speakers when their availability is updated

**Analytics & Stats**
- **FR-102**: System MUST display basic event stats (total registrations, RSVP counts per session)
- **FR-103**: Organizers MUST be able to view stats for all sessions in their events
- **FR-104**: Speakers MUST only see stats for their own sessions
- **FR-105**: System MUST display session popularity ranking (by RSVP/registration count)

**Speaker Roles**
- **FR-106**: System MUST support multiple speakers per session with role designation
- **FR-107**: System MUST support speaker roles: Primary Speaker, Co-Speaker, Presenter, Moderator, Panelist, Host
- **FR-108**: System MUST display speaker roles in session details
- **FR-109**: System MUST distinguish primary speaker visually in session listings

**Sponsorship**
- **FR-110**: System MUST allow marking sessions as sponsored
- **FR-111**: System MUST support sponsor logo display on sponsored sessions
- **FR-112**: System MUST support sponsor name and optional link on sponsored sessions

**Equipment Requirements**
- **FR-113**: System MUST allow sessions to specify equipment requirements (projector, microphone, whiteboard, etc.)
- **FR-114**: System MUST store equipment tags on rooms
- **FR-115**: System MUST warn when assigning session to room missing required equipment
- **FR-116**: System MUST allow filtering rooms by equipment when assigning time slots

**Session Language**
- **FR-117**: System MUST support session language tag (e.g., English, Dutch, German)
- **FR-118**: System MUST display session language in session details and schedule
- **FR-119**: System MUST allow filtering schedule by session language

**Accessibility**
- **FR-120**: System MUST support room accessibility tags (wheelchair accessible, hearing loop, etc.)
- **FR-121**: System MUST support session accommodations (sign language interpreter, live captioning, etc.)
- **FR-122**: System MUST allow attendees to indicate accessibility needs during registration
- **FR-123**: System MUST allow filtering sessions by accessibility accommodations
- **FR-124**: System MUST display accessibility icons in schedule and session details

**Session Difficulty**
- **FR-125**: System MUST support session difficulty levels (Beginner, Intermediate, Advanced)
- **FR-126**: System MUST display difficulty level in session details and schedule
- **FR-127**: System MUST allow filtering schedule by difficulty level

**Personal Agenda**
- **FR-128**: System MUST support personal agenda/schedule builder for attendees
- **FR-129**: System MUST display visual conflict warning when attendee adds overlapping sessions (soft warning, no blocking)
- **FR-130**: System MUST allow attendees to keep conflicting sessions in agenda (defer decision)
- **FR-131**: System MUST allow viewing only "my agenda" sessions in schedule view
- **FR-132**: Calendar export MUST use personal agenda sessions

**Session Review (Internal)**
- **FR-133**: Organizers MUST be able to vote on sessions (approve/reject/maybe)
- **FR-134**: System MUST display aggregate vote results to organizers
- **FR-135**: System MUST support session status based on voting (under review, accepted, rejected)

**Event Branding**
- **FR-136**: System MUST support event logo upload
- **FR-137**: System MUST support primary color configuration per event
- **FR-138**: System MUST apply event branding in PublicApp schedule view

**Event Announcements**
- **FR-139**: Organizers MUST be able to post event announcements
- **FR-140**: System MUST display announcements to attendees in PublicApp
- **FR-141**: System MUST show announcement timestamp and support chronological ordering

**Venue Maps**
- **FR-142**: System MUST support uploading floor plan images or PDFs per location
- **FR-143**: System MUST support multiple floor plans per location (multi-floor venues)
- **FR-144**: System MUST display floor plans in PublicApp venue/location view
- **FR-145**: System MUST allow labeling floor plans (e.g., "Ground Floor", "Level 1")
- **FR-146**: System MUST support linking rooms to specific floor plans
- **FR-147**: System MUST display floor reference in room/session details

**Data Deletion & GDPR**
- **FR-148**: System MUST soft delete main entities (Event, Session, Speaker, Room, Location)
- **FR-149**: System MUST hard delete join/association tables (SessionAssignment, Organizer, etc.)
- **FR-150**: System MUST support GDPR hard delete for personal data (Speaker, Attendee, User profiles)
- **FR-151**: System MUST anonymize historical records when GDPR deletion removes referenced person
- **FR-152**: System MUST log GDPR deletion requests for compliance audit

**Session Ownership Model**
- **FR-153**: Sessions MUST be distinct instances per event (not shared references)
- **FR-154**: System MUST support linking session to source/parent session (for clone tracking)
- **FR-155**: System MUST support cloning session from one event to another
- **FR-156**: System MUST allow viewing session history across events (via clone lineage)

**Audit Columns**
- **FR-157**: All main entities MUST have CreatedAt, CreatedBy timestamps
- **FR-158**: All main entities MUST have UpdatedAt, UpdatedBy timestamps (updated on every change)
- **FR-159**: Soft-deleted entities MUST have DeletedAt, DeletedBy timestamps
- **FR-160**: System MUST automatically populate audit columns from current user context

**Room Versioning**
- **FR-161**: SessionAssignment MUST store room snapshot (name, capacity, equipment at assignment time)
- **FR-162**: Past sessions MUST display room info from snapshot, not current room state
- **FR-163**: Room MUST support "active/inactive" status (no longer in use)
- **FR-164**: Inactive rooms MUST NOT appear in room selection for new time slots
- **FR-165**: Inactive rooms MUST remain visible in historical session assignments

**Entity Identifiers**
- **FR-166**: All entities MUST use timestamp-based GUIDs as primary key (UUIDv7 or similar for index performance)
- **FR-167**: Main entities MUST have human-readable short code for URLs and display (e.g., EVT-2026-001, SES-ABC123)
- **FR-168**: Short codes MUST be unique within their entity type
- **FR-169**: System MUST support lookup by either GUID or short code

**Room Setup Configurations**
- **FR-170**: Room MUST support multiple setup configurations (e.g., Theater, Classroom, U-Shape, Workshop, Conference)
- **FR-171**: Each room configuration MUST have its own capacity
- **FR-172**: SessionAssignment MUST reference specific room configuration (not just room)
- **FR-173**: System MUST use configuration-specific capacity for session capacity validation
- **FR-174**: Room configuration snapshot MUST be stored with SessionAssignment for history

**Tag Model**
- **FR-175**: System MUST use single Tag entity for all tag types
- **FR-176**: Tag MUST have type discriminator (Subject, Technology, Audience, etc.)
- **FR-177**: Tags MUST be reusable across Sessions, Tracks, Speakers
- **FR-178**: System MUST support cross-entity tag search ("find everything tagged AI")

**Speaker Profile Model**
- **FR-179**: Speaker MUST have global profile (name, photo, company, bio, social links)
- **FR-180**: EventSpeaker join MUST support event-specific overrides (bio, photo, company)
- **FR-181**: System MUST snapshot speaker profile data when event ends (freeze for history)
- **FR-182**: Past events MUST display speaker info from snapshot, not current profile
- **FR-183**: System MUST use override values if present, otherwise fall back to global profile

**Event Series Model**
- **FR-184**: System MUST support EventSeries entity (name, description, logo, URL slug)
- **FR-185**: Event MUST have optional reference to EventSeries
- **FR-186**: System MUST support querying all events in a series
- **FR-187**: EventSeries MAY have its own branding that events can inherit

**Time Block Model**
- **FR-188**: Event MUST support defining suggested time blocks (templates)
- **FR-189**: Tracks MAY customize time slots independent of event time blocks
- **FR-190**: System MUST support aligning track time slots to event time blocks for grid display
- **FR-191**: System MUST handle non-aligned time slots gracefully in schedule view
- **FR-192**: Time blocks spanning all tracks (keynotes, breaks) MUST be event-level, not track-level

**Session Assignment Model**
- **FR-193**: SessionAssignment MUST be a junction table linking Session, Track, and TimeSlot
- **FR-194**: SessionAssignment MUST capture RoomConfiguration snapshot at assignment time
- **FR-195**: SessionAssignment MUST support sessions spanning multiple consecutive time slots
- **FR-196**: System MUST maintain assignment history for audit/rescheduling tracking
- **FR-197**: SessionAssignment MUST have full audit columns (CreatedAt/By, UpdatedAt/By)

**Registration Model**
- **FR-198**: Registration MUST be a single table with Status enum (Registered, Waitlisted, Cancelled)
- **FR-199**: Registration MUST have Position column for waitlist ordering
- **FR-200**: System MUST auto-promote first waitlisted attendee when registration cancels
- **FR-201**: Registration MUST link to Session (not SessionAssignment) for stability
- **FR-202**: Registration MUST have full audit columns for compliance tracking

**Location and Room Model**
- **FR-203**: Location MUST represent venue/building with address, map URL, accessibility info
- **FR-204**: Room MUST belong to exactly one Location
- **FR-205**: Room MUST support multiple RoomConfigurations (setups with different capacities)
- **FR-206**: System MUST support RoomCombination entity linking multiple Rooms as a combined space
- **FR-207**: RoomCombination MUST have its own name, capacity, and available configurations
- **FR-208**: SessionAssignment MAY reference either a single Room or a RoomCombination
- **FR-209**: System MUST prevent double-booking when individual rooms are used in a combination
- **FR-210**: RoomCombination SHOULD indicate which rooms are involved for venue logistics

**Notification Model**
- **FR-211**: Notification MUST be a single table with polymorphic target (Attendee, Speaker, Broadcast)
- **FR-212**: Notification MUST track delivery status (Pending, Sent, Failed, Read)
- **FR-213**: Notification MUST support template-based content generation
- **FR-214**: System MUST handle speakers who are also attendees (may receive both notification types)
- **FR-215**: Notification MUST have retry tracking for failed deliveries
- **FR-216**: Broadcast notifications MUST support filtering criteria (event, track, session)

**Person/User Identity Model**
- **FR-217**: System MUST have a single Person/User entity for identity management
- **FR-218**: EventParticipant junction MUST link Person to Event with one or more roles
- **FR-219**: Roles (Speaker, Attendee, Organizer) MUST be combinable per event participation
- **FR-220**: Notifications MUST deduplicate when person has multiple roles (avoid spam)
- **FR-221**: Person profile (name, email, bio, photo) MUST be shared across all event participations
- **FR-222**: EventSpeaker/EventAttendee views MAY provide role-specific data extensions

**Session Material Model**
- **FR-223**: SessionMaterial MUST store file metadata (URL, MIME type, size, filename)
- **FR-224**: SessionMaterial MUST support material types (Slides, Handout, Recording, Code, Other)
- **FR-225**: Actual files MUST be stored in external blob storage (Azure Blob/S3)
- **FR-226**: SessionMaterial MUST support versioning (replace with history)
- **FR-227**: SessionMaterial MUST track download counts for analytics
- **FR-228**: SessionMaterial MUST have visibility control (Public, Attendees, Speakers)

**Session Feedback Model**
- **FR-229**: SessionFeedback MUST capture numeric rating (1-5 scale) per session
- **FR-230**: SessionFeedback MAY include optional text comment
- **FR-231**: SessionFeedback MUST link to Session and Person (attendee)
- **FR-232**: SessionFeedback MUST support anonymous submission option
- **FR-233**: System MUST prevent duplicate feedback from same person per session
- **FR-234**: Event-level feedback is OUT OF SCOPE (external survey tool)

**Event Status Model**
- **FR-235**: Event MUST have Status enum (Draft, Published, Active, Completed, Cancelled, Archived)
- **FR-236**: Draft events MUST NOT be visible in PublicApp
- **FR-237**: Published events MUST be visible but registration may be controlled separately
- **FR-238**: Active status indicates event is currently running
- **FR-239**: Completed events MUST remain viewable with materials/recordings
- **FR-240**: State transitions MUST be enforced in application logic

**Session Status Model**
- **FR-241**: Session MUST have exclusive base state (Draft, Scheduled, Cancelled)
- **FR-242**: Session MUST have combinable flags: IsConfirmed, IsPublished
- **FR-243**: Session can be Confirmed but not yet Published (speaker confirmed, pending schedule finalization)
- **FR-244**: Draft sessions MUST NOT be visible in PublicApp regardless of flags
- **FR-245**: Cancelled sessions MUST clear IsPublished flag automatically
- **FR-246**: Only Scheduled+Confirmed+Published sessions appear in public schedule

**Sponsor Model**
- **FR-247**: Sponsor MUST be a separate entity with name, logo, URL, description
- **FR-248**: EventSponsor junction MUST link Sponsor to Event with tier level
- **FR-249**: Sponsor tiers MUST be configurable per event (default: Platinum, Gold, Silver, Bronze, Partner)
- **FR-250**: Sponsors MUST be ordered by tier level then by custom sort order within tier
- **FR-251**: Sponsor MAY be reused across multiple events (shared profile)
- **FR-252**: EventSponsor MAY have event-specific overrides (booth location, featured status)

**Announcement Model**
- **FR-253**: Announcement MUST have title, content (rich text), and priority level
- **FR-254**: Announcement MUST support scheduled publishing (PublishAt, ExpireAt)
- **FR-255**: Announcement MUST have target scope (All, Attendees, Speakers, Track-specific)
- **FR-256**: Announcement MUST link to Event (event-level announcements)
- **FR-257**: Announcements MUST be ordered by priority then publish date (newest first)
- **FR-258**: Expired announcements MUST be auto-hidden from public views

**Equipment Model**
- **FR-259**: Equipment MUST be a separate entity with name, description, category
- **FR-260**: Equipment types MUST be configurable per event/venue (projector, microphone, whiteboard, etc.)
- **FR-261**: SessionEquipment junction MUST link Session to Equipment with quantity
- **FR-262**: System MUST support generating equipment lists per room/time slot for AV team
- **FR-263**: Equipment MAY have availability limits per venue for conflict detection
- **FR-264**: Room MAY have default equipment that sessions inherit unless overridden
- **FR-282**: Admins MUST be able to define global equipment types available to all events
- **FR-283**: Organizers MUST be able to request new equipment types (added to event-specific list)

**CFP/Submission Model**
- **FR-265**: Session MUST have SubmissionStatus (NotSubmitted, Submitted, UnderReview, Accepted, Rejected, Waitlisted)
- **FR-266**: Session MUST track SubmittedAt timestamp when submitted via CFP
- **FR-267**: Session MUST support internal review votes (SubmissionVote junction to Person with score)
- **FR-268**: Session MUST support internal review comments (SubmissionComment entity)
- **FR-269**: Rejected sessions MUST be retained but hidden from public views
- **FR-270**: SubmissionStatus MUST be independent of Session base status (Draft/Scheduled/Cancelled)

**Audit Log Model**
- **FR-271**: AuditLog MUST capture action type (Create, Update, Delete, StatusChange)
- **FR-272**: AuditLog MUST store entity type and entity ID for polymorphic tracking
- **FR-273**: AuditLog MUST capture old and new values (JSON) for change comparison
- **FR-274**: AuditLog MUST link to Person who performed the action
- **FR-275**: AuditLog MUST have timestamp and optional IP address/user agent
- **FR-276**: AuditLog MUST support querying activity feed per entity or per user

**Calendar Export**
- **FR-277**: System MUST generate iCal (RFC 5545) on-demand from Session data
- **FR-278**: Calendar export MUST support filtering (personal agenda, track, full event)
- **FR-279**: iCal events MUST include location, description, speakers in appropriate fields
- **FR-280**: System MUST support subscribable calendar URLs (auto-updating)
- **FR-281**: Calendar export MUST handle timezone conversion correctly

**User Roles**
- **FR-022**: System MUST support a user being a speaker, attendee, and organizer simultaneously
- **FR-023**: System MUST allow speakers to participate in multiple events
- **FR-024**: System MUST allow speakers to present multiple sessions per event
- **FR-025**: Organizers MUST have full edit permissions equivalent to the event creator

**Concurrency**
- **FR-036**: System MUST implement optimistic concurrency for schedule edits (version/timestamp check)
- **FR-037**: System MUST warn users when a conflict is detected due to concurrent edits

**Audit & History**
- **FR-041**: System MUST maintain change history for schedule modifications (who/when/what)
- **FR-042**: System MUST allow viewing history of changes for sessions, time slots, and tracks

**Accessibility & UX**
- **FR-289**: System MUST support keyboard navigation for the schedule grid (arrow keys to move, Enter to select/edit)
- **FR-290**: System MUST provide ARIA labels for all interactive schedule elements (slots, sessions, drag handles)
- **FR-291**: System MUST announce dynamic updates (e.g., "Session moved to Track 1") to screen readers via live regions
- **FR-292**: System MUST support a "List View" alternative to the grid for better accessibility and mobile support
- **FR-293**: System MUST warn users of "Unsaved Changes" before navigating away from the schedule editor
- **FR-294**: System MUST display a "Success Toast" notification upon successful save of schedule changes
- **FR-295**: System MUST display "Skeleton Loading" states while fetching schedule data to reduce perceived latency

**Mobile Experience**
- **FR-296**: System MUST render a stacked "Day View" on mobile devices instead of the full multi-track grid
- **FR-297**: System MUST support "Tap-to-Assign" interaction on mobile as an alternative to drag-and-drop
- **FR-298**: System MUST allow collapsing tracks in the mobile view to focus on specific content

### Key Entities

- **Event**: A conference or meetup with name, dates (start/end), optional edition, status (Draft/Public/Private), and associated tracks
- **EventSeries**: Logical grouping of events by name/code linking multiple editions
- **Track**: A themed or topic-based stream within an event containing time slots, with subject tags
- **TimeSlot**: A scheduled block of time within a track where sessions can be assigned, linked to a room
- **Location**: A venue or building containing one or more rooms
- **Room**: A specific space within a location with name, capacity, and equipment tags; versioned to preserve history
- **Organizer**: Association between a user and an event granting management permissions
- **SessionAssignment**: Link between a session and a specific time slot, storing room snapshot at assignment time (preserves historical accuracy)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Organizers can create a new event with all required information in under 3 minutes
- **SC-002**: Organizers can build a complete schedule with 20+ sessions in under 30 minutes
- **SC-003**: Session swapping between time slots completes in under 2 seconds
- **SC-004**: Schedule view loads and renders within 1 second for events with 100+ sessions
- **SC-005**: 90% of organizers can successfully create an event and assign sessions without documentation
- **SC-006**: Event status changes (Draft → Public) reflect in PublicApp within 5 seconds
- **SC-007**: Drag-and-drop interactions feel responsive with visual feedback appearing within 100ms

## Assumptions

- The existing Session and Speaker entities will be extended rather than replaced
- The PrivateApp already has authentication and authorization infrastructure
- Subject tags are managed separately and can be reused across tracks, sessions, and speakers
- The schedule view will use a calendar/grid-like visualization component
- PublicApp configuration for event assignment is handled separately from this feature
- Real-time equipment inventory management (stock tracking) is handled by the venue and is out of scope

## Clarifications

### Session 2026-01-04

- Q: What level of access should organizers have when added to an event? → A: Full access - Organizers have same permissions as event creator
- Q: Can a session appear in multiple time slots within the same event? → A: Yes - Same session can appear in multiple time slots (repeating workshops/sessions)
- Q: How are Private events assigned to specific PublicApps? → A: B - PublicApp identifier in deployment config; event-to-PublicApp assignment stored in database
- Q: What should happen when a speaker is scheduled in overlapping sessions? → A: A - Soft warning with visual conflict indicator (allows overflow/recording scenarios)
- Q: Should schedule grid support copying a day's schedule to another day? → A: B+ - Copy time slot structure AND copy complete event schedule (with sessions) to different days
- Q: Should rooms/locations be managed as a separate entity? → A: B - Room entity with metadata, linked to Location, with change history (past sessions preserve room state at time of event)
- Q: What happens when two organizers edit the same schedule simultaneously? → A: B - Optimistic concurrency with conflict warning
- Q: How are event editions linked? → A: B - Shared series code (explicit grouping, not automatic name matching)
- Q: Are subject tags predefined by admins or freeform entry? → A: C - Both predefined library and organizer freeform entry with autocomplete
- Q: Should the system maintain an audit log of schedule changes? → A: B - Simple change history (who/when/what changed)
- Q: Should schedule support non-session time slots (breaks, lunch)? → A: C - Special slot types (Break, Lunch, Networking, Keynote) with distinct styling
- Q: Should keynote sessions span across all tracks visually? → A: C - Configurable per slot (organizer chooses whether slot spans all tracks)
- Q: Should sessions have a type (Workshop, Talk, Panel) with default durations? → A: B - Session types with suggested duration; configurable at admin level (global defaults) and event level (organizer overrides)
- Q: Should schedule warn when session duration doesn't match slot duration? → A: C - Smart match (filter/suggest sessions matching slot duration, allow override)
- Q: Should system support event templates for quick setup? → A: B - Clone from previous event (same series or any event, copies track/slot structure)
- Q: Should schedule support timezone display options? → A: Context-aware - In-person attendees see event local time; remote attendees see both (user timezone primary, event timezone secondary)
- Q: Should system support hybrid events? → A: C - Session-level hybrid control (in-person/virtual/both), enabled via event-level setting
- Q: Should system support session capacity limits? → A: C - Session-level override (inherits room capacity by default, can override per session)
- Q: How should system handle session prerequisites/dependencies? → A: B - Soft recommendation (display suggested order, no hard enforcement)
- Q: Should schedule view support filtering by speaker? → A: C - Multi-select filters (combine speaker, tag, track filters)
- Q: Should system support session-level registration? → A: B+C - Optional RSVP by default (track interest); workshops can require seat reservation with capacity enforcement
- Q: Should system support waitlists for full sessions? → A: C - Auto-promote waitlist (automatic promotion when spot opens)
- Q: Should schedule support exporting to external calendars? → A: C - Personalized export (only registered/RSVP'd sessions)
- Q: Should session changes trigger notifications to attendees? → A: C - Push/email notification (proactive alert for time/room changes)
- Q: Should system support session feedback/ratings? → A: C - Full feedback (rating + comments, with speaker visibility controls)
- Q: Should system support session materials/attachments? → A: B - File upload + URL option (for linking to protected external resources like SharePoint/GitHub)
- Q: Should sessions support video recordings? → A: B - External link (organizers add YouTube/Vimeo URL after event)
- Q: Should system support speaker availability for scheduling? → A: C - Full availability windows, managed by organizers (not speakers), no speaker notifications for availability changes
- Q: Should schedule support printing/PDF export? → A: A - No print support (digital only, export is separate scope)
- Q: Should system support event analytics/dashboard? → A: B - Basic stats (registration counts, RSVP totals); organizers see all, speakers see only their own sessions
- Q: Should system support session co-hosting with speaker roles? → A: C - Full roles (Primary speaker, Co-speaker, Presenter, Moderator, Panelist, Host) with display differences
- Q: Should system support sponsor integration? → A: B - Simple sponsor badge (mark sessions as sponsored with logo)
- Q: Should time slots support equipment/AV requirements? → A: C - Session-level requirements with room validation (sessions specify needs, system validates against room equipment)
- Q: Should system support session language/translation? → A: B - Session language tag (indicate primary language, attendees can filter)
- Q: Should system support accessibility features? → A: C - Full accessibility (room tags, session accommodations, attendee needs matching)
- Q: Should system support session difficulty/experience level? → A: B - Simple level tag (Beginner, Intermediate, Advanced)
- Q: Should system support personal agenda builder for attendees? → A: C - Full agenda builder with visual conflict warning (no blocking, let users delay decisions)
- Q: Should system support session Q&A features? → A: A - External tools (Slido, Mentimeter) - out of scope
- Q: Should system support check-in/attendance tracking? → A: A - No check-in for now (assume RSVP = attended)
- Q: Should system support session submission/CFP workflow? → A: A - No external CFP, but organizers can vote on sessions internally
- Q: Should system support event branding/theming? → A: B - Basic branding (logo and primary color per event)
- Q: Should system support event announcements/news feed? → A: B - Simple announcements (organizers post, attendees see in app)
- Q: Should system support venue maps/floor plans? → A: B - Static image/PDF upload with multiple files per location (e.g., multiple floors)
- Q: Should rooms be linkable to floor plan position? → A: B - Simple reference (room shows which floor it's on)
- Q: Should system support social sharing? → A: A - No social sharing (out of scope)
- Q: Should entities support soft delete or hard delete? → A: C - Mixed (soft delete main entities, hard delete joins) + GDPR hard delete capability for personal data
- Q: Should Event own Sessions or be independent? → A: C - Hybrid (sessions can be cloned/reused across events, each event gets distinct instance with link to source)
- Q: Should timestamps use simple or full audit pattern? → A: C - Full audit columns (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, DeletedAt, DeletedBy)
- Q: Should Room entity use versioning for history? → A: B - Manual snapshots at assignment time + room can be marked as "no longer in use"
- Q: Should entity IDs be GUIDs, integers, or hybrid? → A: C - Hybrid (timestamp-based GUID as PK + human-readable short code for URLs/display)
- Q: How should room setups (conference, workshop, U-shape) be modeled? → A: B - Room configuration entity (each room has multiple configs with different capacities per setup)
- Q: Should Tags be polymorphic or separate per usage? → A: B - Single Tag table with type discriminator, reusable across entities
- Q: Should Speaker profile be event-scoped or global? → A: C - Global profile + event-specific override; speaker info frozen/snapshotted after event ends
- Q: How should EventSeries relationship be modeled? → A: B - Separate EventSeries entity with its own metadata (logo, description, slug)
- Q: Should TimeSlot be per-track or shared across tracks? → A: C - Hybrid (event defines suggested time blocks, tracks can customize/deviate)
- Q: How should session-to-slot assignment be structured? → A: B - Separate SessionAssignment junction table (rescheduling history, multi-slot support)
- Q: How should attendee registration be modeled? → A: A - Single Registration table with Status column (Registered, Waitlisted, Cancelled) and Position for waitlist ordering
- Q: How should venue/room location hierarchy be modeled? → A: B - Location → Room hierarchy, with support for room combinations (e.g., rooms 3+4+5 combined by removing walls)
- Q: How should notifications/communications be tracked? → A: A - Polymorphic Notification table (targets: attendee, speaker, broadcast); note speakers can also be attendees
- Q: How should person identity be modeled given overlapping roles? → A: A - Single Person/User entity with multiple roles per event (Speaker, Attendee, Organizer simultaneously)
- Q: How should session materials/attachments be stored? → A: A - SessionMaterial entity with file metadata (URL, type, size); actual files in blob storage
- Q: How should attendee feedback be structured? → A: A - SessionFeedback entity for session ratings/comments; event-level feedback via external tool (out of scope)
- Q: How should event lifecycle states be modeled? → A: A - Simple Status enum (Draft, Published, Active, Completed, Cancelled, Archived)
- Q: Should sessions have independent status? → A: Yes, as flags - combinable (IsConfirmed, IsPublished) + exclusive states (Draft, Cancelled)
- Q: How should event sponsors be modeled? → A: A - Sponsor entity with tier levels (Platinum, Gold, Silver, Bronze, Partner) per event
- Q: How should event announcements be modeled? → A: A - Announcement entity with title, content, publish/expire dates, priority, target scope
- Q: How should session equipment requirements be tracked? → A: A - Equipment entity with SessionEquipment junction for custom equipment types and quantities
- Q: How should CFP submissions be modeled? → A: B - Session entity with SubmissionStatus (single source of truth for all session information)
- Q: How should the audit trail be stored? → A: A - AuditLog entity with action, entity type/ID, old/new values, user, timestamp
- Q: How should calendar export data be structured? → A: A - Generate iCal on-demand from Session/Event data (no separate entity)

## Security & Content Management

### FR-299: File Upload Validation
- **Requirement**: The system MUST validate all file uploads against a strict allowlist of file types and enforce size limits.
- **Allowed Types**:
  - Documents: PDF (`.pdf`), Word (`.doc`, `.docx`)
  - Presentations: PowerPoint (`.ppt`, `.pptx`)
  - Archives: ZIP (`.zip`) - primarily for code samples
- **Size Limits**:
  - Maximum file size: 50MB per file.
  - The system MUST reject files exceeding this limit with a clear error message.
- **Validation Mechanism**: Validation MUST check both file extension and MIME type.
- **Error Handling**: Invalid file types MUST be rejected with a clear user-friendly error message listing allowed types.

### FR-300: Content Security & XSS Prevention
- **Trust Model**:
  - **Organizers**: Trusted. Input from organizers (e.g., session descriptions, announcements) is considered safe and MAY contain HTML or rich Markdown.
  - **Public/Others**: Untrusted. Input from non-organizers (e.g., CFP submissions, feedback) MUST be treated as untrusted.
- **Sanitization**:
  - Untrusted input MUST be sanitized to prevent XSS attacks.
  - Safe Markdown is the preferred format for untrusted text fields.
  - Script tags, iframes, and other active content MUST be stripped from untrusted input.

### FR-301: Secure Downloads & Visibility
- **Storage Provider**: File storage and security SHOULD be offloaded to the storage provider (e.g., OneDrive, SharePoint, Azure Blob Storage with SAS).
- **Access Control**: The system relies on the provider's access control mechanisms for the actual file download.
- **Pre-Event Visibility**:
  - The system MUST support marking specific downloads/materials as "Visible in Advance".
  - Use Case: Installation instructions or prerequisites for workshops that attendees need to access before the event starts.
  - Default behavior: Materials are typically available to registered attendees; "Visible in Advance" overrides this for earlier access if needed.

## API Standards & Resilience

### FR-302: API Pagination
- **Requirement**: All list endpoints returning potentially large datasets (e.g., Sessions, Attendees, Audit Logs) MUST support pagination.
- **Threshold**: Pagination is REQUIRED for any endpoint capable of returning more than 50 items.
- **Mechanism**: Standard page-based or cursor-based pagination SHOULD be used.
- **Default**: Default page size SHOULD be 20 items if not specified.

### FR-303: Resilience & Retry Policies
- **External Dependencies**: Interactions with external services (Blob Storage, Email Service, Identity Provider) MUST implement resilience patterns.
- **Retry Logic**: The system MUST implement exponential backoff retry logic for transient failures (e.g., timeouts, 503 errors).
- **Circuit Breaker**: A circuit breaker pattern SHOULD be used to prevent cascading failures when an external service is down for an extended period.

### FR-304: Caching Strategy
- **Public Data**: Publicly accessible data (e.g., Published Event Schedules, Speaker Lists) MUST be cacheable.
- **Cache-Control**: Public API endpoints MUST include appropriate `Cache-Control` headers (e.g., `public, max-age=300` for 5-minute caching) to allow caching by CDNs or clients.
- **Invalidation**: Critical status changes (e.g., Event Cancellation) SHOULD trigger cache invalidation or use short TTLs to ensure timely propagation.

### FR-305: Rate Limiting
- **Requirement**: Public-facing API endpoints MUST implement rate limiting to prevent abuse and Denial of Service (DoS) attacks.
- **Scope**: Rate limits SHOULD be applied per IP address or per client identifier.
- **Response**: Requests exceeding the limit MUST be rejected with a `429 Too Many Requests` status code.

## Testing Strategy

### FR-306: Stress Testing
- **Requirement**: The system MUST be validated under high-concurrency load conditions.
- **Target**: Simulate 50 concurrent users performing active schedule management (creating, moving, editing sessions) simultaneously.
- **Success Criteria**: The system MUST maintain sub-second response times (p95 < 1s) and zero data corruption/loss during the test.

### FR-307: UI Resilience & Rollback
- **Requirement**: The UI MUST handle network failures or server errors gracefully during complex interactions (e.g., drag-and-drop).
- **Mechanism**: Optimistic UI updates SHOULD be used for responsiveness, but MUST be capable of automatic rollback.
- **Behavior**: If a backend operation fails (e.g., 500 error or timeout), the UI MUST revert the visual state to its previous valid configuration and display a user-friendly error toast.



