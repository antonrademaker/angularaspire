using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Registration;
using System.Security.Claims;
using FluentValidation;

namespace PublicApi.Registration;

/// <summary>
/// Registration request DTO for API
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Event ID to register for
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Registration priority level
    /// </summary>
    public RegistrationPriority Priority { get; set; } = RegistrationPriority.Normal;

    /// <summary>
    /// Custom registration data (dietary restrictions, accessibility needs, etc.)
    /// </summary>
    public Dictionary<string, object>? RegistrationData { get; set; }

    /// <summary>
    /// Optional notes about the registration
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Registration update request DTO
/// </summary>
public class UpdateRegistrationRequest
{
    /// <summary>
    /// Updated registration data
    /// </summary>
    public Dictionary<string, object> RegistrationData { get; set; } = new();
}

/// <summary>
/// Registration response DTO for API
/// </summary>
public class RegistrationResponse
{
    /// <summary>
    /// Registration ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Event information
    /// </summary>
    public EventSummary Event { get; set; } = null!;

    /// <summary>
    /// User information
    /// </summary>
    public UserSummary User { get; set; } = null!;

    /// <summary>
    /// Registration status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Registration priority
    /// </summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>
    /// Queue position (if queued)
    /// </summary>
    public int? QueuePosition { get; set; }

    /// <summary>
    /// When registration was created
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// When registration was confirmed
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Estimated wait time in minutes (if queued)
    /// </summary>
    public int? EstimatedWaitTimeMinutes { get; set; }

    /// <summary>
    /// Custom registration data
    /// </summary>
    public Dictionary<string, object>? RegistrationData { get; set; }

    /// <summary>
    /// Whether registration can be cancelled
    /// </summary>
    public bool CanBeCancelled { get; set; }

    /// <summary>
    /// Registration confirmation URL (if applicable)
    /// </summary>
    public string? ConfirmationUrl { get; set; }
}

/// <summary>
/// Event summary for registration responses
/// </summary>
public class EventSummary
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? Venue { get; set; }
    public int? MaxCapacity { get; set; }
    public int CurrentRegistrations { get; set; }
}

/// <summary>
/// User summary for registration responses
/// </summary>
public class UserSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Queue status response
/// </summary>
public class QueueStatusResponse
{
    /// <summary>
    /// Event ID
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// Number of confirmed registrations
    /// </summary>
    public int ConfirmedCount { get; set; }

    /// <summary>
    /// Number in queue
    /// </summary>
    public int QueuedCount { get; set; }

    /// <summary>
    /// Event capacity
    /// </summary>
    public int? MaxCapacity { get; set; }

    /// <summary>
    /// Available spots
    /// </summary>
    public int? AvailableSpots { get; set; }

    /// <summary>
    /// Whether event is at capacity
    /// </summary>
    public bool IsAtCapacity { get; set; }

    /// <summary>
    /// Processing rate (registrations per minute)
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Estimated queue clear time in minutes
    /// </summary>
    public int? EstimatedClearTimeMinutes { get; set; }
}

/// <summary>
/// Registration request validator
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.EventId)
            .NotEmpty()
            .WithMessage("Event ID is required");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority level");

        RuleFor(x => x.RegistrationData)
            .Must(data => data == null || data.Count <= 50)
            .WithMessage("Registration data cannot exceed 50 fields");

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes cannot exceed 2000 characters");
    }
}

/// <summary>
/// Registration management controller for event registrations
/// Handles registration lifecycle, queue processing, and user interactions
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Require authentication for all registration operations
[Produces("application/json")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ILogger<RegistrationController> _logger;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public RegistrationController(
        IRegistrationService registrationService,
        ILogger<RegistrationController> logger,
        IValidator<RegisterRequest> registerValidator)
    {
        _registrationService = registrationService ?? throw new ArgumentNullException(nameof(registrationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
    }

    /// <summary>
    /// Register for an event
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result with queue information</returns>
    [HttpPost]
    [ProducesResponseType<RegistrationResult>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
            }

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            // Build registration request
            var registrationRequest = new RegistrationRequest
            {
                EventId = request.EventId,
                UserId = userId.Value,
                Priority = request.Priority,
                RegistrationData = request.RegistrationData,
                Source = "web",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].FirstOrDefault(),
                Notes = request.Notes
            };

            _logger.LogInformation("Processing registration for user {UserId} to event {EventId}",
                userId.Value, request.EventId);

            var result = await _registrationService.RegisterUserAsync(registrationRequest, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Registration successful for user {UserId} to event {EventId}. Queued: {IsQueued}, Position: {QueuePosition}",
                    userId.Value, request.EventId, result.IsQueued, result.QueuePosition);

                return CreatedAtAction(
                    nameof(GetRegistrationAsync),
                    new { id = result.Registration!.Id },
                    result);
            }
            else
            {
                _logger.LogWarning("Registration failed for user {UserId} to event {EventId}: {Message}",
                    userId.Value, request.EventId, result.Message);

                return result.ErrorCode switch
                {
                    "ALREADY_REGISTERED" => Conflict(new ProblemDetails
                    {
                        Title = "Already Registered",
                        Detail = result.Message,
                        Status = StatusCodes.Status409Conflict
                    }),
                    "EVENT_FULL" => Conflict(new ProblemDetails
                    {
                        Title = "Event Full",
                        Detail = result.Message,
                        Status = StatusCodes.Status409Conflict
                    }),
                    "REGISTRATION_CLOSED" => BadRequest(new ProblemDetails
                    {
                        Title = "Registration Closed",
                        Detail = result.Message,
                        Status = StatusCodes.Status400BadRequest
                    }),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                    {
                        Title = "Registration Failed",
                        Detail = result.Message,
                        Status = StatusCodes.Status500InternalServerError
                    })
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing registration for event {EventId}", request.EventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing your registration",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get registration by ID
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRegistrationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            var registration = await _registrationService.GetRegistrationAsync(id, includeUser: true, includeEvent: true, cancellationToken);
            if (registration == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Registration Not Found",
                    Detail = $"Registration with ID {id} was not found",
                    Status = StatusCodes.Status404NotFound
                });
            }

            // Ensure user can only access their own registrations
            if (registration.UserId != userId.Value)
            {
                return Forbid("You can only access your own registrations");
            }

            var response = await MapToRegistrationResponse(registration);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving the registration",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get current user's registrations
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user registrations</returns>
    [HttpGet("my-registrations")]
    [ProducesResponseType<List<RegistrationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRegistrationsAsync(
        [FromQuery] RegistrationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            var registrations = await _registrationService.GetUserRegistrationsAsync(
                userId.Value, status, includeEvents: true, cancellationToken);

            var responses = new List<RegistrationResponse>();
            foreach (var registration in registrations)
            {
                responses.Add(await MapToRegistrationResponse(registration));
            }

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registrations for user {UserId}", GetCurrentUserId());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving your registrations",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Cancel a registration
    /// </summary>
    /// <param name="id">Registration ID to cancel</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result</returns>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelRegistrationAsync(
        Guid id,
        [FromBody] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            _logger.LogInformation("Cancelling registration {RegistrationId} for user {UserId}", id, userId.Value);

            var result = await _registrationService.CancelRegistrationAsync(id, userId.Value, reason, cancellationToken);

            if (result)
            {
                return Ok(new { message = "Registration cancelled successfully" });
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Cancellation Failed",
                    Detail = "Registration could not be cancelled. It may not exist, already be cancelled, or you may not have permission to cancel it.",
                    Status = StatusCodes.Status400BadRequest
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while cancelling the registration",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Update registration data
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated registration</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RegistrationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRegistrationAsync(
        Guid id,
        [FromBody] UpdateRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            // Validate registration data
            if (request.RegistrationData.Count > 50)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    ["RegistrationData"] = new[] { "Registration data cannot exceed 50 fields" }
                }));
            }

            var result = await _registrationService.UpdateRegistrationDataAsync(
                id, userId.Value, request.RegistrationData, cancellationToken);

            if (result)
            {
                var registration = await _registrationService.GetRegistrationAsync(id, includeUser: true, includeEvent: true, cancellationToken);
                if (registration != null)
                {
                    var response = await MapToRegistrationResponse(registration);
                    return Ok(response);
                }
            }

            return BadRequest(new ProblemDetails
            {
                Title = "Update Failed",
                Detail = "Registration could not be updated. It may not exist or you may not have permission to update it.",
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating registration {RegistrationId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating the registration",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get queue status for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue status information</returns>
    [HttpGet("events/{eventId:guid}/queue-status")]
    [AllowAnonymous] // Allow anonymous access to queue status
    [ProducesResponseType<QueueStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventQueueStatusAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueStatus = await _registrationService.GetQueueStatusAsync(eventId, cancellationToken);

            var response = new QueueStatusResponse
            {
                EventId = queueStatus.EventId,
                ConfirmedCount = queueStatus.ConfirmedCount,
                QueuedCount = queueStatus.QueuedCount,
                MaxCapacity = queueStatus.MaxCapacity,
                AvailableSpots = queueStatus.AvailableSpots,
                IsAtCapacity = queueStatus.IsAtCapacity,
                ProcessingRate = queueStatus.ProcessingRate,
                EstimatedClearTimeMinutes = queueStatus.EstimatedClearTimeMinutes
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue status for event {EventId}", eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving queue status",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Get user's queue position for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue position information</returns>
    [HttpGet("events/{eventId:guid}/my-queue-position")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyQueuePositionAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized("User not authenticated");
            }

            var position = await _registrationService.GetUserQueuePositionAsync(userId.Value, eventId, cancellationToken);

            if (position.HasValue)
            {
                var registration = await _registrationService.GetUserRegistrationAsync(userId.Value, eventId, cancellationToken);
                return Ok(new
                {
                    eventId,
                    queuePosition = position.Value,
                    estimatedWaitTimeMinutes = registration?.EstimatedWaitTimeMinutes,
                    status = registration?.Status.ToString(),
                    message = $"You are #{position.Value} in the queue"
                });
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not in Queue",
                    Detail = "You are not currently in the queue for this event",
                    Status = StatusCodes.Status404NotFound
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving queue position for user {UserId} in event {EventId}", GetCurrentUserId(), eventId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while retrieving your queue position",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Confirm registration via confirmation token
    /// </summary>
    /// <param name="token">Confirmation token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation result</returns>
    [HttpPost("confirm/{token}")]
    [AllowAnonymous] // Allow anonymous confirmation via email links
    [ProducesResponseType<RegistrationResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmRegistrationAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid Token",
                    Detail = "Confirmation token is required",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _logger.LogInformation("Processing registration confirmation for token {Token}", token);

            var result = await _registrationService.ConfirmRegistrationAsync(token, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return result.ErrorCode switch
                {
                    "TOKEN_NOT_FOUND" => NotFound(new ProblemDetails
                    {
                        Title = "Token Not Found",
                        Detail = result.Message,
                        Status = StatusCodes.Status404NotFound
                    }),
                    "TOKEN_EXPIRED" => BadRequest(new ProblemDetails
                    {
                        Title = "Token Expired",
                        Detail = result.Message,
                        Status = StatusCodes.Status400BadRequest
                    }),
                    _ => BadRequest(new ProblemDetails
                    {
                        Title = "Confirmation Failed",
                        Detail = result.Message,
                        Status = StatusCodes.Status400BadRequest
                    })
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming registration with token {Token}", token);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while confirming your registration",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    // Private helper methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<RegistrationResponse> MapToRegistrationResponse(Shared.Registration.Registration registration)
    {
        var queuePosition = registration.IsQueued
            ? await _registrationService.GetUserQueuePositionAsync(registration.UserId, registration.EventId)
            : null;

        return new RegistrationResponse
        {
            Id = registration.Id,
            Event = new EventSummary
            {
                Id = registration.Event.Id, // Both are Guid now
                Slug = registration.Event.Slug,
                Title = registration.Event.Title, // Event has Title property
                StartDateTime = registration.Event.StartDate,
                EndDateTime = registration.Event.EndDate,
                Location = registration.Event.VenueName ?? "TBA", // Using VenueName as Location
                Venue = registration.Event.VenueAddress,
                MaxCapacity = registration.Event.MaxAttendees,
                CurrentRegistrations = registration.Event.CurrentAttendees
            },
            User = new UserSummary
            {
                Id = registration.User.Id, // Both are Guid now
                Name = registration.User.FullName, // User uses FullName, not Name
                Email = registration.User.Email
            },
            Status = registration.Status.ToString(),
            Priority = registration.Priority.ToString(),
            QueuePosition = queuePosition,
            RegisteredAt = registration.RegisteredAt,
            ConfirmedAt = registration.ConfirmedAt,
            EstimatedWaitTimeMinutes = registration.EstimatedWaitTimeMinutes,
            RegistrationData = registration.RegistrationData,
            CanBeCancelled = registration.CanBeCancelled,
            ConfirmationUrl = registration.ConfirmationToken != null
                ? Url.Action(nameof(ConfirmRegistrationAsync), "Registration", new { token = registration.ConfirmationToken }, Request.Scheme)
                : null
        };
    }
}