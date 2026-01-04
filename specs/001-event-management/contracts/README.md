# API Contract Overview

**Date**: 2025-12-30  
**Feature**: Event Management System  
**Phase**: 1 - API Contract Definition
**Technology Stack**: .NET 10, Angular 21, Aspire 10

## Architecture Overview

The Event Management System exposes two types of APIs leveraging the latest platform capabilities:

### 1. Public REST API (Angular Apps → .NET APIs)
- **Public Angular App**: Basic event browsing, registration, session viewing
- **Private Angular App**: Full event management, administrative functions
- HTTP/REST with OpenAPI specification
- JWT authentication for user actions

### 2. Internal gRPC Services (Service-to-Service)
- Communication between business services
- Strongly typed contracts with backwards compatibility
- Service discovery via Aspire orchestration
- Certificate-based authentication

## REST API Endpoints

### Public Event API (Public Angular App)
Base URL: `/api/v1/events`

**Event Discovery:**
- `GET /events` - List published events (paginated, filterable)
- `GET /events/{id}` - Get event details
- `GET /events/{id}/sessions` - List event sessions
- `GET /events/{id}/tracks` - List event tracks
- `GET /events/{id}/speakers` - List event speakers

**Registration (Authenticated):**
- `POST /events/{id}/registrations` - Register for event
- `GET /registrations/my` - Get user's registrations
- `PUT /registrations/{id}` - Update registration
- `DELETE /registrations/{id}` - Cancel registration

### Private Event API (Private Angular App)
Base URL: `/api/v1/admin`

**Event Management (Admin/Organizer):**
- `POST /events` - Create event
- `PUT /events/{id}` - Update event
- `DELETE /events/{id}` - Delete event
- `POST /events/{id}/publish` - Publish event
- `POST /events/{id}/cancel` - Cancel event

**Registration Management:**
- `GET /events/{id}/registrations` - List event registrations
- `POST /registrations/{id}/approve` - Approve registration
- `POST /registrations/{id}/checkin` - Check-in attendee
- `GET /registrations/export` - Export registration data

**Session Management:**
- `POST /events/{id}/sessions` - Create session
- `PUT /sessions/{id}` - Update session
- `DELETE /sessions/{id}` - Delete session
- `POST /sessions/{id}/speakers` - Assign speakers

**Analytics:**
- `GET /events/{id}/analytics` - Event analytics
- `GET /sessions/{id}/attendance` - Session attendance

## gRPC Service Contracts

### Event Management Service
```proto
syntax = "proto3";

package event_management.v1;
option csharp_namespace = "EventManagement.Contracts";

service EventService {
  // Event lifecycle
  rpc CreateEvent(CreateEventRequest) returns (EventResponse);
  rpc UpdateEvent(UpdateEventRequest) returns (EventResponse);
  rpc GetEvent(GetEventRequest) returns (EventResponse);
  rpc ListEvents(ListEventsRequest) returns (ListEventsResponse);
  rpc PublishEvent(PublishEventRequest) returns (EventResponse);
  rpc CancelEvent(CancelEventRequest) returns (EventResponse);
  
  // Event search
  rpc SearchEvents(SearchEventsRequest) returns (SearchEventsResponse);
  
  // Event validation
  rpc ValidateEvent(ValidateEventRequest) returns (ValidationResponse);
}

message EventResponse {
  string id = 1;
  string title = 2;
  string description = 3;
  EventType type = 4;
  EventStatus status = 5;
  google.protobuf.Timestamp start_date = 6;
  google.protobuf.Timestamp end_date = 7;
  Location location = 8;
  RegistrationSettings registration_settings = 9;
  repeated Track tracks = 10;
  map<string, google.protobuf.Value> metadata = 11;
}

enum EventType {
  EVENT_TYPE_UNSPECIFIED = 0;
  EVENT_TYPE_CONFERENCE = 1;
  EVENT_TYPE_WORKSHOP = 2;
  EVENT_TYPE_WEBINAR = 3;
  EVENT_TYPE_MEETUP = 4;
  EVENT_TYPE_TRAINING = 5;
  EVENT_TYPE_HYBRID = 6;
}

enum EventStatus {
  EVENT_STATUS_UNSPECIFIED = 0;
  EVENT_STATUS_DRAFT = 1;
  EVENT_STATUS_PUBLISHED = 2;
  EVENT_STATUS_REGISTRATION_OPEN = 3;
  EVENT_STATUS_REGISTRATION_CLOSED = 4;
  EVENT_STATUS_IN_PROGRESS = 5;
  EVENT_STATUS_COMPLETED = 6;
  EVENT_STATUS_CANCELLED = 7;
  EVENT_STATUS_ARCHIVED = 8;
}
```

### Registration Service
```proto
syntax = "proto3";

package registration.v1;
option csharp_namespace = "Registration.Contracts";

service RegistrationService {
  // Registration lifecycle
  rpc CreateRegistration(CreateRegistrationRequest) returns (RegistrationResponse);
  rpc UpdateRegistration(UpdateRegistrationRequest) returns (RegistrationResponse);
  rpc CancelRegistration(CancelRegistrationRequest) returns (RegistrationResponse);
  rpc GetRegistration(GetRegistrationRequest) returns (RegistrationResponse);
  
  // Registration queries
  rpc ListUserRegistrations(ListUserRegistrationsRequest) returns (ListRegistrationsResponse);
  rpc ListEventRegistrations(ListEventRegistrationsRequest) returns (ListRegistrationsResponse);
  
  // Registration validation
  rpc ValidateRegistration(ValidateRegistrationRequest) returns (ValidationResponse);
  rpc CheckCapacity(CheckCapacityRequest) returns (CapacityResponse);
  
  // Check-in process
  rpc CheckInAttendee(CheckInRequest) returns (CheckInResponse);
  rpc GetAttendanceReport(AttendanceReportRequest) returns (AttendanceReportResponse);
}

message RegistrationResponse {
  string id = 1;
  string event_id = 2;
  string user_id = 3;
  RegistrationStatus status = 4;
  RegistrationType type = 5;
  google.protobuf.Timestamp registered_at = 6;
  repeated SessionRegistration session_registrations = 7;
  map<string, google.protobuf.Value> custom_fields = 8;
}

enum RegistrationStatus {
  REGISTRATION_STATUS_UNSPECIFIED = 0;
  REGISTRATION_STATUS_PENDING = 1;
  REGISTRATION_STATUS_CONFIRMED = 2;
  REGISTRATION_STATUS_CHECKED_IN = 3;
  REGISTRATION_STATUS_NO_SHOW = 4;
  REGISTRATION_STATUS_CANCELLED = 5;
  REGISTRATION_STATUS_WAITLISTED = 6;
}
```

### Session Management Service
```proto
syntax = "proto3";

package session_management.v1;
option csharp_namespace = "SessionManagement.Contracts";

service SessionService {
  // Session lifecycle
  rpc CreateSession(CreateSessionRequest) returns (SessionResponse);
  rpc UpdateSession(UpdateSessionRequest) returns (SessionResponse);
  rpc DeleteSession(DeleteSessionRequest) returns (google.protobuf.Empty);
  rpc GetSession(GetSessionRequest) returns (SessionResponse);
  
  // Session queries
  rpc ListEventSessions(ListEventSessionsRequest) returns (ListSessionsResponse);
  rpc ListTrackSessions(ListTrackSessionsRequest) returns (ListSessionsResponse);
  rpc SearchSessions(SearchSessionsRequest) returns (SearchSessionsResponse);
  
  // Session scheduling
  rpc ValidateSchedule(ValidateScheduleRequest) returns (ValidationResponse);
  rpc GetScheduleConflicts(GetScheduleConflictsRequest) returns (ScheduleConflictsResponse);
  
  // Speaker management
  rpc AssignSpeaker(AssignSpeakerRequest) returns (SessionResponse);
  rpc RemoveSpeaker(RemoveSpeakerRequest) returns (SessionResponse);
}

message SessionResponse {
  string id = 1;
  string event_id = 2;
  string track_id = 3;
  string title = 4;
  string description = 5;
  SessionType type = 6;
  SessionFormat format = 7;
  google.protobuf.Timestamp start_time = 8;
  google.protobuf.Timestamp end_time = 9;
  string room_name = 10;
  int32 max_capacity = 11;
  repeated SessionSpeaker speakers = 12;
  SkillLevel skill_level = 13;
  repeated string prerequisites = 14;
  repeated string tags = 15;
  int32 registered_count = 16;
  map<string, google.protobuf.Value> metadata = 17;
}
```

### User Management Service
```proto
syntax = "proto3";

package user_management.v1;
option csharp_namespace = "UserManagement.Contracts";

service UserService {
  // User lifecycle
  rpc CreateUser(CreateUserRequest) returns (UserResponse);
  rpc UpdateUser(UpdateUserRequest) returns (UserResponse);
  rpc GetUser(GetUserRequest) returns (UserResponse);
  rpc GetUserByEmail(GetUserByEmailRequest) returns (UserResponse);
  
  // User queries
  rpc ListUsers(ListUsersRequest) returns (ListUsersResponse);
  rpc SearchUsers(SearchUsersRequest) returns (SearchUsersResponse);
  
  // Authentication support
  rpc ValidateUser(ValidateUserRequest) returns (ValidationResponse);
  rpc UpdateLastLogin(UpdateLastLoginRequest) returns (UserResponse);
  
  // Profile management
  rpc UpdateProfile(UpdateProfileRequest) returns (UserResponse);
  rpc UpdatePreferences(UpdatePreferencesRequest) returns (UserResponse);
}

message UserResponse {
  string id = 1;
  string email = 2;
  string first_name = 3;
  string last_name = 4;
  string company_name = 5;
  string job_title = 6;
  UserType type = 7;
  map<string, google.protobuf.Value> profile = 8;
  NotificationPreferences notification_preferences = 9;
  google.protobuf.Timestamp created_at = 10;
  google.protobuf.Timestamp last_login_at = 11;
}
```

### Notification Service
```proto
syntax = "proto3";

package notification.v1;
option csharp_namespace = "Notification.Contracts";

service NotificationService {
  // Send notifications
  rpc SendNotification(SendNotificationRequest) returns (NotificationResponse);
  rpc SendBulkNotifications(SendBulkNotificationsRequest) returns (BulkNotificationResponse);
  
  // Template management
  rpc CreateTemplate(CreateTemplateRequest) returns (TemplateResponse);
  rpc UpdateTemplate(UpdateTemplateRequest) returns (TemplateResponse);
  rpc GetTemplate(GetTemplateRequest) returns (TemplateResponse);
  
  // Notification tracking
  rpc GetNotificationStatus(GetNotificationStatusRequest) returns (NotificationStatusResponse);
  rpc ListUserNotifications(ListUserNotificationsRequest) returns (ListNotificationsResponse);
  
  // Preferences
  rpc UpdateUserPreferences(UpdateUserPreferencesRequest) returns (UserPreferencesResponse);
}

message SendNotificationRequest {
  string user_id = 1;
  string template_id = 2;
  NotificationChannel channel = 3;
  map<string, string> variables = 4;
  google.protobuf.Timestamp scheduled_at = 5;
  NotificationPriority priority = 6;
}

enum NotificationChannel {
  NOTIFICATION_CHANNEL_UNSPECIFIED = 0;
  NOTIFICATION_CHANNEL_EMAIL = 1;
  NOTIFICATION_CHANNEL_SMS = 2;
  NOTIFICATION_CHANNEL_PUSH = 3;
  NOTIFICATION_CHANNEL_IN_APP = 4;
}
```

## Shared Messages and Types

### Common Value Objects
```proto
message Location {
  string name = 1;
  string address = 2;
  string city = 3;
  string state_province = 4;
  string country = 5;
  string postal_code = 6;
  double latitude = 7;
  double longitude = 8;
}

message VirtualLocation {
  string platform = 1;
  string access_link = 2;
  string access_instructions = 3;
  map<string, google.protobuf.Value> platform_settings = 4;
}

message RegistrationSettings {
  google.protobuf.Timestamp open_date = 1;
  google.protobuf.Timestamp close_date = 2;
  int32 max_capacity = 3;
  bool require_approval = 4;
  bool allow_waitlist = 5;
  double registration_fee = 6;
  string currency = 7;
  map<string, google.protobuf.Value> custom_field_definitions = 8;
}

message ValidationResponse {
  bool is_valid = 1;
  repeated ValidationError errors = 2;
  repeated ValidationWarning warnings = 3;
}

message ValidationError {
  string field = 1;
  string code = 2;
  string message = 3;
}
```

## API Versioning Strategy

### REST API Versioning
- URL versioning: `/api/v1/`, `/api/v2/`
- Backwards compatibility maintained for at least 2 major versions
- Deprecation headers for sunsetting endpoints
- Feature flags for gradual rollout

### gRPC Versioning
- Package versioning: `event_management.v1`, `event_management.v2`
- Backwards compatible field additions
- Field deprecation with clear migration paths
- Service version negotiation via Aspire service discovery

## Security Contracts

### Authentication
```json
{
  "jwt": {
    "issuer": "https://eventmanagement.api",
    "audience": ["public-api", "admin-api"],
    "claims": {
      "sub": "user_id",
      "email": "user_email", 
      "role": ["attendee", "organizer", "admin"],
      "permissions": ["events:read", "registrations:write"]
    }
  }
}
```

### Rate Limiting
```yaml
rate_limits:
  public_api:
    - endpoint: "/api/v1/events"
      limit: "100 requests per minute per IP"
    - endpoint: "/api/v1/registrations"
      limit: "10 requests per minute per user"
  
  admin_api:
    - endpoint: "/api/v1/admin/**"
      limit: "1000 requests per minute per user"
```

These contracts provide:
- **Clear boundaries**: REST for client-server, gRPC for service-to-service
- **Strong typing**: Protobuf ensures contract compliance
- **Versioning strategy**: Supports evolution without breaking changes
- **Security integration**: JWT authentication and role-based access
- **Performance optimization**: Efficient serialization and caching support