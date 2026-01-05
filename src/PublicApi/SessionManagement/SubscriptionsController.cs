using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.SessionManagement;

namespace PublicApi.SessionManagement;

/// <summary>
/// Subscribe to session request DTO
/// </summary>
public class SubscribeToSessionRequest
{
    /// <summary>
    /// Optional notes for the subscription
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Unsubscribe from session request DTO
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Reason for unsubscribing
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// API response for session subscription
/// </summary>
public class SessionSubscriptionResponse
{
    /// <summary>
    /// Subscription ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Session title
    /// </summary>
    public string SessionTitle { get; set; } = string.Empty;

    /// <summary>
    /// Session start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Session end time
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Track name
    /// </summary>
    public string? TrackName { get; set; }

    /// <summary>
    /// Room/location
    /// </summary>
    public string? Room { get; set; }

    /// <summary>
    /// Subscription status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Whether user is on waitlist
    /// </summary>
    public bool IsWaitlisted { get; set; }

    /// <summary>
    /// Waitlist position (if waitlisted)
    /// </summary>
    public int? WaitlistPosition { get; set; }

    /// <summary>
    /// When subscription was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Whether user has checked in
    /// </summary>
    public bool IsCheckedIn { get; set; }
}

/// <summary>
/// Session availability response
/// </summary>
public class SessionAvailabilityResponse
{
    /// <summary>
    /// Session ID
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Whether the session is available for subscription
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Whether there is remaining capacity
    /// </summary>
    public bool HasCapacity { get; set; }

    /// <summary>
    /// Maximum attendees (null for unlimited)
    /// </summary>
    public int? MaxAttendees { get; set; }

    /// <summary>
    /// Current number of confirmed attendees
    /// </summary>
    public int CurrentAttendees { get; set; }

    /// <summary>
    /// Remaining spots
    /// </summary>
    public int? SpotsRemaining { get; set; }

    /// <summary>
    /// Number of users on waitlist
    /// </summary>
    public int WaitlistCount { get; set; }

    /// <summary>
    /// Human-readable availability message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// User's current subscription status (if subscribed)
    /// </summary>
    public string? UserStatus { get; set; }

    /// <summary>
    /// User's waitlist position (if waitlisted)
    /// </summary>
    public int? UserWaitlistPosition { get; set; }
}

/// <summary>
/// Request validator for subscribe requests
/// </summary>
public class SubscribeToSessionRequestValidator : AbstractValidator<SubscribeToSessionRequest>
{
    public SubscribeToSessionRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters");
    }
}

/// <summary>
/// Public API controller for session subscriptions
/// Allows attendees to subscribe to and manage their session subscriptions
/// </summary>
[ApiController]
[Route("api/v1/sessions")]
[Authorize]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionService subscriptionService,
        ISessionService sessionService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _sessionService = sessionService;
        _logger = logger;
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user identity");
        }

        return userId;
    }

    // ==================== Session Discovery ====================

    /// <summary>
    /// Get sessions for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="trackId">Optional track filter</param>
    /// <returns>List of sessions</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<SessionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventSessions(
        [FromQuery] Guid eventId,
        [FromQuery] Guid? trackId = null)
    {
        var sessions = await _sessionService.GetEventSessionsAsync(eventId, trackId, includeUnpublished: false);
        return Ok(sessions);
    }

    /// <summary>
    /// Get session details by ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Session details</returns>
    [HttpGet("{sessionId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null || !session.IsPublished)
        {
            return NotFound(new { message = "Session not found" });
        }

        return Ok(session);
    }

    /// <summary>
    /// Check session availability
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Availability information</returns>
    [HttpGet("{sessionId:guid}/availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SessionAvailabilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionAvailability(Guid sessionId)
    {
        var session = await _sessionService.GetSessionAsync(sessionId);
        if (session == null)
        {
            return NotFound(new { message = "Session not found" });
        }

        var availability = await _subscriptionService.CheckAvailabilityAsync(sessionId);

        var response = new SessionAvailabilityResponse
        {
            SessionId = availability.SessionId,
            IsAvailable = availability.IsAvailable,
            HasCapacity = availability.HasCapacity,
            MaxAttendees = availability.MaxAttendees,
            CurrentAttendees = availability.CurrentAttendees,
            SpotsRemaining = availability.SpotsRemaining,
            WaitlistCount = availability.WaitlistCount,
            Message = availability.Message ?? string.Empty
        };

        // If user is authenticated, include their subscription status
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                var userId = GetUserId();
                var subscription = await _subscriptionService.GetUserSessionSubscriptionAsync(sessionId, userId);
                if (subscription != null)
                {
                    response.UserStatus = subscription.Status.ToString();
                    response.UserWaitlistPosition = subscription.WaitlistPosition;
                }
            }
            catch
            {
                // Ignore errors getting user subscription
            }
        }

        return Ok(response);
    }

    // ==================== Subscription Management ====================

    /// <summary>
    /// Subscribe to a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Subscription request</param>
    /// <returns>Subscription details</returns>
    [HttpPost("{sessionId:guid}/subscribe")]
    [ProducesResponseType(typeof(SessionSubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Subscribe(
        Guid sessionId,
        [FromBody] SubscribeToSessionRequest? request = null)
    {
        var userId = GetUserId();

        var subscribeRequest = new SubscribeRequest
        {
            SessionId = sessionId,
            UserId = userId,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            Notes = request?.Notes
        };

        var result = await _subscriptionService.SubscribeAsync(subscribeRequest);

        if (!result.IsSuccess)
        {
            var error = result.Errors.FirstOrDefault() ?? "Subscription failed";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ApiError { Code = "NOT_FOUND", Message = error });
            }

            if (error.Contains("already subscribed", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new ApiError { Code = "ALREADY_SUBSCRIBED", Message = error });
            }

            return BadRequest(new ApiError { Code = "SUBSCRIPTION_FAILED", Message = error });
        }

        // Get session details for response
        var session = await _sessionService.GetSessionAsync(sessionId);
        var response = MapToSubscriptionResponse(result.Value!, session);

        _logger.LogInformation(
            "User {UserId} subscribed to session {SessionId}. Waitlisted: {IsWaitlisted}",
            userId, sessionId, result.Value!.IsWaitlisted);

        return CreatedAtAction(
            nameof(GetMySubscription),
            new { sessionId },
            response);
    }

    /// <summary>
    /// Unsubscribe from a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="request">Unsubscribe request</param>
    /// <returns>Success result</returns>
    [HttpPost("{sessionId:guid}/unsubscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Unsubscribe(
        Guid sessionId,
        [FromBody] UnsubscribeRequest? request = null)
    {
        var userId = GetUserId();

        var result = await _subscriptionService.UnsubscribeAsync(sessionId, userId, request?.Reason);

        if (!result.IsSuccess)
        {
            var error = result.Errors.FirstOrDefault() ?? "Unsubscribe failed";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ApiError { Code = "NOT_FOUND", Message = error });
            }

            return BadRequest(new ApiError { Code = "UNSUBSCRIBE_FAILED", Message = error });
        }

        _logger.LogInformation("User {UserId} unsubscribed from session {SessionId}", userId, sessionId);

        return NoContent();
    }

    /// <summary>
    /// Get my subscription for a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Subscription details</returns>
    [HttpGet("{sessionId:guid}/my-subscription")]
    [ProducesResponseType(typeof(SessionSubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMySubscription(Guid sessionId)
    {
        var userId = GetUserId();

        var subscription = await _subscriptionService.GetUserSessionSubscriptionAsync(sessionId, userId);
        if (subscription == null)
        {
            return NotFound(new { message = "You are not subscribed to this session" });
        }

        var session = await _sessionService.GetSessionAsync(sessionId);
        return Ok(MapToSubscriptionResponse(subscription, session));
    }

    // ==================== User's Subscriptions ====================

    /// <summary>
    /// Get all my subscriptions
    /// </summary>
    /// <param name="eventId">Optional event filter</param>
    /// <returns>List of subscriptions</returns>
    [HttpGet("/api/v1/my-subscriptions")]
    [ProducesResponseType(typeof(IEnumerable<SessionSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscriptions([FromQuery] Guid? eventId = null)
    {
        var userId = GetUserId();

        var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId, eventId);

        // Get session details for each subscription
        var sessionIds = subscriptions.Select(s => s.SessionId).Distinct().ToList();
        var responses = new List<SessionSubscriptionResponse>();

        foreach (var subscription in subscriptions)
        {
            var session = await _sessionService.GetSessionAsync(subscription.SessionId);
            responses.Add(MapToSubscriptionResponse(subscription, session));
        }

        return Ok(responses);
    }

    /// <summary>
    /// Get my subscriptions for a specific event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>List of subscriptions for the event</returns>
    [HttpGet("/api/v1/events/{eventId:guid}/my-subscriptions")]
    [ProducesResponseType(typeof(IEnumerable<SessionSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyEventSubscriptions(Guid eventId)
    {
        return await GetMySubscriptions(eventId);
    }

    // ==================== Check-In ====================

    /// <summary>
    /// Check in to a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{sessionId:guid}/check-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(Guid sessionId)
    {
        var userId = GetUserId();

        var result = await _subscriptionService.CheckInAsync(sessionId, userId);

        if (!result.IsSuccess)
        {
            var error = result.Errors.FirstOrDefault() ?? "Check-in failed";

            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new ApiError { Code = "NOT_FOUND", Message = error });
            }

            return BadRequest(new ApiError { Code = "CHECK_IN_FAILED", Message = error });
        }

        _logger.LogInformation("User {UserId} checked in to session {SessionId}", userId, sessionId);

        return Ok(new { message = "Checked in successfully" });
    }

    // ==================== Session Schedule ====================

    /// <summary>
    /// Get my schedule (confirmed subscriptions) for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>List of confirmed sessions</returns>
    [HttpGet("/api/v1/events/{eventId:guid}/my-schedule")]
    [ProducesResponseType(typeof(IEnumerable<SessionScheduleItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySchedule(Guid eventId)
    {
        var userId = GetUserId();

        var subscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId, eventId);
        var confirmedSubscriptions = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Confirmed || s.Status == SubscriptionStatus.Attended)
            .ToList();

        var scheduleItems = new List<SessionScheduleItem>();

        foreach (var subscription in confirmedSubscriptions)
        {
            var session = await _sessionService.GetSessionAsync(subscription.SessionId);
            if (session != null)
            {
                scheduleItems.Add(new SessionScheduleItem
                {
                    SessionId = session.Id,
                    Title = session.Title,
                    Description = session.Description,
                    StartTime = session.StartTime,
                    EndTime = session.EndTime,
                    TrackId = session.TrackId,
                    TrackName = session.TrackName,
                    Room = session.Room,
                    Building = session.Building,
                    IsVirtual = session.IsVirtual,
                    VirtualUrl = session.VirtualUrl,
                    Type = session.Type.ToString(),
                    SubscriptionId = subscription.Id,
                    IsCheckedIn = subscription.CheckedInAt.HasValue
                });
            }
        }

        return Ok(scheduleItems.OrderBy(s => s.StartTime));
    }

    // ==================== Helper Methods ====================

    private static SessionSubscriptionResponse MapToSubscriptionResponse(
        SubscriptionResponse subscription,
        SessionResponse? session)
    {
        return new SessionSubscriptionResponse
        {
            Id = subscription.Id,
            SessionId = subscription.SessionId,
            SessionTitle = session?.Title ?? subscription.SessionTitle,
            StartTime = session?.StartTime ?? DateTime.MinValue,
            EndTime = session?.EndTime ?? DateTime.MinValue,
            TrackName = session?.TrackName,
            Room = session?.Room,
            Status = subscription.Status.ToString(),
            IsWaitlisted = subscription.IsWaitlisted,
            WaitlistPosition = subscription.WaitlistPosition,
            CreatedAt = subscription.CreatedAt,
            IsCheckedIn = subscription.CheckedInAt.HasValue
        };
    }
}

/// <summary>
/// Session schedule item for user's schedule view
/// </summary>
public class SessionScheduleItem
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public Guid? TrackId { get; set; }
    public string? TrackName { get; set; }
    public string? Room { get; set; }
    public string? Building { get; set; }
    public bool IsVirtual { get; set; }
    public string? VirtualUrl { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid SubscriptionId { get; set; }
    public bool IsCheckedIn { get; set; }
}

/// <summary>
/// API error response
/// </summary>
public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
