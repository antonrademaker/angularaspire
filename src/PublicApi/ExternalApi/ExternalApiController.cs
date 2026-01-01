using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.ApiManagement;
using Shared.EventManagement;
using PublicApi.Middleware;

namespace PublicApi.ExternalApi;

/// <summary>
/// External API controller for third-party integrations
/// Requires API key authentication via X-Api-Key header
/// </summary>
[ApiController]
[Route("api/external")]
[Produces("application/json")]
public class ExternalApiController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ISocialEventService _socialEventService;
    private readonly ILogger<ExternalApiController> _logger;

    public ExternalApiController(
        IEventService eventService,
        ISocialEventService socialEventService,
        ILogger<ExternalApiController> logger)
    {
        _eventService = eventService;
        _socialEventService = socialEventService;
        _logger = logger;
    }

    #region Events API

    /// <summary>
    /// List all events with pagination
    /// </summary>
    /// <remarks>
    /// Required scope: events:read
    /// </remarks>
    [HttpGet("events")]
    [ProducesResponseType(typeof(ExternalApiResponse<ExternalPagedResult<ExternalEventDto>>), 200)]
    [ProducesResponseType(typeof(RateLimitErrorResponse), 429)]
    public async Task<IActionResult> ListEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        // Validate scope
        if (!HttpContext.HasApiKeyScope("events:read") && !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("events:read");
        }

        var searchRequest = new EventSearchRequest
        {
            Status = ParseEventStatus(status),
            StartDateFrom = fromDate,
            StartDateTo = toDate,
            Page = page,
            PageSize = Math.Min(pageSize, 100) // Cap at 100
        };

        var result = await _eventService.SearchEventsAsync(searchRequest);

        var events = result.Events.Select(e => MapToExternalDto(e)).ToList();
        var pagedResult = new ExternalPagedResult<ExternalEventDto>
        {
            Items = events,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        };

        return Ok(ExternalApiResponse<ExternalPagedResult<ExternalEventDto>>.CreateSuccess(pagedResult));
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    /// <remarks>
    /// Required scope: events:read
    /// </remarks>
    [HttpGet("events/{id:guid}")]
    [ProducesResponseType(typeof(ExternalApiResponse<ExternalEventDto>), 200)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 404)]
    public async Task<IActionResult> GetEvent(Guid id, CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("events:read") && !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("events:read");
        }

        var @event = await _eventService.GetEventByIdAsync(id);
        
        if (@event == null)
        {
            return NotFound(ExternalApiResponse<object>.CreateError("EVENT_NOT_FOUND", $"Event with ID {id} not found"));
        }

        return Ok(ExternalApiResponse<ExternalEventDto>.CreateSuccess(MapToExternalDto(@event)));
    }

    /// <summary>
    /// Create a new event
    /// </summary>
    /// <remarks>
    /// Required scope: events:write
    /// Requires Standard tier or above
    /// </remarks>
    [HttpPost("events")]
    [ProducesResponseType(typeof(ExternalApiResponse<ExternalEventDto>), 201)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 403)]
    public async Task<IActionResult> CreateEvent(
        [FromBody] CreateExternalEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("events:write") && !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("events:write");
        }

        // Check tier (Free tier cannot create events)
        var tier = HttpContext.GetApiKeyTier();
        if (tier == ApiKeyTier.Free)
        {
            return StatusCode(403, ExternalApiResponse<object>.CreateError(
                "INSUFFICIENT_TIER", 
                "Creating events requires Standard tier or above"));
        }

        var userId = HttpContext.GetApiKeyUserId();
        if (!userId.HasValue)
        {
            return BadRequest(ExternalApiResponse<object>.CreateError("INVALID_API_KEY", "Could not determine user from API key"));
        }

        var createRequest = new CreateEventRequest
        {
            Title = request.Title,
            Description = request.Description ?? string.Empty,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            VenueName = request.Location,
            VenueAddress = request.VenueAddress,
            IsVirtual = request.IsVirtual,
            VirtualMeetingUrl = request.MeetingUrl,
            MaxAttendees = request.MaxCapacity,
            RegistrationOpenDate = request.RegistrationOpens,
            RegistrationCloseDate = request.RegistrationCloses
        };

        var createdEvent = await _eventService.CreateEventAsync(createRequest, userId.Value);

        var eventDto = MapToExternalDto(createdEvent);
        return CreatedAtAction(nameof(GetEvent), new { id = eventDto.Id }, 
            ExternalApiResponse<ExternalEventDto>.CreateSuccess(eventDto));
    }

    /// <summary>
    /// Update an existing event
    /// </summary>
    /// <remarks>
    /// Required scope: events:write
    /// </remarks>
    [HttpPut("events/{id:guid}")]
    [ProducesResponseType(typeof(ExternalApiResponse<ExternalEventDto>), 200)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateEvent(
        Guid id,
        [FromBody] UpdateExternalEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("events:write") && !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("events:write");
        }

        var userId = HttpContext.GetApiKeyUserId();
        if (!userId.HasValue)
        {
            return BadRequest(ExternalApiResponse<object>.CreateError("INVALID_API_KEY", "Could not determine user from API key"));
        }

        // Get the existing event first to validate it exists
        var existingEvent = await _eventService.GetEventByIdAsync(id);
        if (existingEvent == null)
        {
            return NotFound(ExternalApiResponse<object>.CreateError("EVENT_NOT_FOUND", $"Event with ID {id} not found"));
        }

        var updateRequest = new UpdateEventRequest
        {
            Title = request.Title ?? existingEvent.Title,
            Description = request.Description ?? existingEvent.Description,
            Slug = existingEvent.Slug, // Keep existing slug
            StartDate = request.StartDate ?? existingEvent.StartDate,
            EndDate = request.EndDate ?? existingEvent.EndDate,
            VenueName = request.Location ?? existingEvent.VenueName,
            VenueAddress = request.VenueAddress ?? existingEvent.VenueAddress,
            IsVirtual = request.IsVirtual ?? existingEvent.IsVirtual,
            VirtualMeetingUrl = request.MeetingUrl ?? existingEvent.VirtualMeetingUrl,
            MaxAttendees = request.MaxCapacity ?? existingEvent.MaxAttendees
        };

        var result = await _eventService.UpdateEventAsync(id, updateRequest, userId.Value);

        if (result == null)
        {
            return NotFound(ExternalApiResponse<object>.CreateError("EVENT_NOT_FOUND", $"Event with ID {id} not found"));
        }

        return Ok(ExternalApiResponse<ExternalEventDto>.CreateSuccess(MapToExternalDto(result)));
    }

    /// <summary>
    /// Delete an event
    /// </summary>
    /// <remarks>
    /// Required scope: events:delete
    /// Requires Premium tier or above
    /// </remarks>
    [HttpDelete("events/{id:guid}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 403)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("events:delete") && !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("events:delete");
        }

        // Check tier (only Premium and Enterprise can delete)
        var tier = HttpContext.GetApiKeyTier();
        if (tier < ApiKeyTier.Premium)
        {
            return StatusCode(403, ExternalApiResponse<object>.CreateError(
                "INSUFFICIENT_TIER", 
                "Deleting events requires Premium tier or above"));
        }

        var userId = HttpContext.GetApiKeyUserId();
        if (!userId.HasValue)
        {
            return BadRequest(ExternalApiResponse<object>.CreateError("INVALID_API_KEY", "Could not determine user from API key"));
        }

        var success = await _eventService.DeleteEventAsync(id, userId.Value);

        if (!success)
        {
            return NotFound(ExternalApiResponse<object>.CreateError("EVENT_NOT_FOUND", $"Event with ID {id} not found"));
        }

        return NoContent();
    }

    #endregion

    #region Social Events API

    /// <summary>
    /// List social events for an event
    /// </summary>
    [HttpGet("events/{eventId:guid}/social-events")]
    [ProducesResponseType(typeof(ExternalApiResponse<IEnumerable<ExternalSocialEventDto>>), 200)]
    public async Task<IActionResult> ListSocialEvents(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("social_events:read") && 
            !HttpContext.HasApiKeyScope("events:read") && 
            !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("social_events:read");
        }

        var socialEvents = await _socialEventService.GetSocialEventsForEventAsync(eventId, true, cancellationToken);
        var dtos = socialEvents.Select(MapToExternalSocialEventDto);

        return Ok(ExternalApiResponse<IEnumerable<ExternalSocialEventDto>>.CreateSuccess(dtos));
    }

    /// <summary>
    /// Get social event by ID
    /// </summary>
    [HttpGet("social-events/{id:guid}")]
    [ProducesResponseType(typeof(ExternalApiResponse<ExternalSocialEventDto>), 200)]
    [ProducesResponseType(typeof(ExternalApiResponse<object>), 404)]
    public async Task<IActionResult> GetSocialEvent(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!HttpContext.HasApiKeyScope("social_events:read") && 
            !HttpContext.HasApiKeyScope("events:read") && 
            !HttpContext.HasApiKeyScope("*"))
        {
            return Forbidden("social_events:read");
        }

        var socialEvent = await _socialEventService.GetSocialEventByIdAsync(id, cancellationToken);
        
        if (socialEvent == null)
        {
            return NotFound(ExternalApiResponse<object>.CreateError("SOCIAL_EVENT_NOT_FOUND", $"Social event with ID {id} not found"));
        }

        return Ok(ExternalApiResponse<ExternalSocialEventDto>.CreateSuccess(MapToExternalSocialEventDto(socialEvent)));
    }

    #endregion

    #region API Info

    /// <summary>
    /// Get API key information and rate limit status
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ExternalApiResponse<ApiKeyInfoDto>), 200)]
    public IActionResult GetApiKeyInfo()
    {
        var validation = HttpContext.GetApiKeyValidation();
        
        if (validation == null)
        {
            return Unauthorized(ExternalApiResponse<object>.CreateError("UNAUTHORIZED", "Invalid or missing API key"));
        }

        var info = new ApiKeyInfoDto
        {
            ApiKeyId = validation.ApiKeyId!.Value,
            UserId = validation.UserId!.Value,
            Tier = validation.Tier!.Value.ToString(),
            Scopes = validation.Scopes,
            RateLimit = validation.RateLimit,
            CurrentUsage = validation.CurrentWindowRequests,
            RemainingRequests = Math.Max(0, validation.RateLimit - validation.CurrentWindowRequests)
        };

        return Ok(ExternalApiResponse<ApiKeyInfoDto>.CreateSuccess(info));
    }

    #endregion

    #region Private Methods

    private IActionResult Forbidden(string requiredScope)
    {
        return StatusCode(403, ExternalApiResponse<object>.CreateError(
            "INSUFFICIENT_SCOPE",
            $"This operation requires the '{requiredScope}' scope"));
    }

    private static EventStatus? ParseEventStatus(string? status)
    {
        if (string.IsNullOrEmpty(status)) return null;
        return Enum.TryParse<EventStatus>(status, true, out var result) ? result : null;
    }

    private static ExternalEventDto MapToExternalDto(Event @event)
    {
        return new ExternalEventDto
        {
            Id = @event.Id,
            Title = @event.Title,
            Description = @event.Description,
            StartDate = @event.StartDate,
            EndDate = @event.EndDate,
            Location = @event.VenueName,
            VenueAddress = @event.VenueAddress,
            IsVirtual = @event.IsVirtual,
            MeetingUrl = @event.VirtualMeetingUrl,
            MaxCapacity = @event.MaxAttendees,
            CurrentRegistrations = @event.CurrentAttendees,
            Status = @event.Status.ToString(),
            OrganizerId = @event.CreatedByUserId,
            CreatedAt = @event.CreatedAt,
            UpdatedAt = @event.UpdatedAt
        };
    }

    private static ExternalSocialEventDto MapToExternalSocialEventDto(SocialEventResponse socialEvent)
    {
        return new ExternalSocialEventDto
        {
            Id = socialEvent.Id,
            EventId = socialEvent.EventId,
            Name = socialEvent.Title,
            Description = socialEvent.Description,
            StartTime = socialEvent.StartTime,
            EndTime = socialEvent.EndTime,
            Location = socialEvent.Location,
            MaxCapacity = socialEvent.MaxCapacity ?? 0,
            CurrentRsvps = socialEvent.CurrentRsvpCount,
            Status = socialEvent.Status.ToString(),
            RequiresRegistration = socialEvent.RsvpRequired
        };
    }

    #endregion
}

#region DTOs

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ExternalApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiErrorInfo? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ExternalApiResponse<T> CreateSuccess(T data) => new() { Success = true, Data = data };
    
    public static ExternalApiResponse<T> CreateError(string code, string message) => new() 
    { 
        Success = false, 
        Error = new ApiErrorInfo { Code = code, Message = message } 
    };
}

/// <summary>
/// Paged result for external API
/// </summary>
public class ExternalPagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class ApiErrorInfo
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// External event DTO (simplified for external consumers)
/// </summary>
public class ExternalEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public string? VenueAddress { get; set; }
    public bool IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentRegistrations { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid OrganizerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request to create an event via external API
/// </summary>
public class CreateExternalEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Location { get; set; }
    public string? VenueAddress { get; set; }
    public bool IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public int MaxCapacity { get; set; } = 100;
    public DateTime? RegistrationOpens { get; set; }
    public DateTime? RegistrationCloses { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Request to update an event via external API
/// </summary>
public class UpdateExternalEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Location { get; set; }
    public string? VenueAddress { get; set; }
    public bool? IsVirtual { get; set; }
    public string? MeetingUrl { get; set; }
    public int? MaxCapacity { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// External social event DTO
/// </summary>
public class ExternalSocialEventDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Location { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentRsvps { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresRegistration { get; set; }
}

/// <summary>
/// API key information response
/// </summary>
public class ApiKeyInfoDto
{
    public Guid ApiKeyId { get; set; }
    public Guid UserId { get; set; }
    public string Tier { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = [];
    public int RateLimit { get; set; }
    public int CurrentUsage { get; set; }
    public int RemainingRequests { get; set; }
}

#endregion
