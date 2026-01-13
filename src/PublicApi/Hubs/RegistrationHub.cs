using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PublicApi.Hubs;

/// <summary>
/// SignalR hub for real-time registration updates and notifications
/// Provides live updates for queue status, registration confirmations, and event capacity changes
/// </summary>
[Authorize] // Require authentication for all hub methods
public class RegistrationHub : Hub
{
    private readonly ILogger<RegistrationHub> _logger;

    public RegistrationHub(ILogger<RegistrationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} connected to RegistrationHub with connection {ConnectionId}",
            userId, connectionId);

        if (userId.HasValue)
        {
            // Add user to their personal group for direct notifications
            await Groups.AddToGroupAsync(connectionId, GetUserGroup(userId.Value));
            _logger.LogDebug("Added user {UserId} to personal group", userId.Value);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        var connectionId = Context.ConnectionId;

        _logger.LogInformation("User {UserId} disconnected from RegistrationHub with connection {ConnectionId}. Exception: {Exception}",
            userId, connectionId, exception?.Message);

        if (userId.HasValue)
        {
            // Remove user from their personal group
            await Groups.RemoveFromGroupAsync(connectionId, GetUserGroup(userId.Value));
            _logger.LogDebug("Removed user {UserId} from personal group", userId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Subscribe to updates for a specific event's queue and registration status
    /// </summary>
    /// <param name="eventId">Event ID to monitor</param>
    [HubMethodName("JoinEventUpdates")]
    public async Task JoinEventUpdatesAsync(string eventId)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var eventGuid))
            {
                _logger.LogWarning("Invalid event ID format: {EventId} from user {UserId}", eventId, GetCurrentUserId());
                await Clients.Caller.SendAsync("Error", "Invalid event ID format");
                return;
            }

            var eventGroup = GetEventGroup(eventGuid);
            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);

            _logger.LogInformation("User {UserId} joined event {EventId} updates group", GetCurrentUserId(), eventGuid);

            // Send confirmation to client
            await Clients.Caller.SendAsync("JoinedEventUpdates", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining event updates for event {EventId} by user {UserId}", eventId, GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "Failed to join event updates");
        }
    }

    /// <summary>
    /// Unsubscribe from updates for a specific event
    /// </summary>
    /// <param name="eventId">Event ID to stop monitoring</param>
    [HubMethodName("LeaveEventUpdates")]
    public async Task LeaveEventUpdatesAsync(string eventId)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var eventGuid))
            {
                _logger.LogWarning("Invalid event ID format: {EventId} from user {UserId}", eventId, GetCurrentUserId());
                return;
            }

            var eventGroup = GetEventGroup(eventGuid);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, eventGroup);

            _logger.LogInformation("User {UserId} left event {EventId} updates group", GetCurrentUserId(), eventGuid);

            // Send confirmation to client
            await Clients.Caller.SendAsync("LeftEventUpdates", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving event updates for event {EventId} by user {UserId}", eventId, GetCurrentUserId());
        }
    }

    /// <summary>
    /// Request current queue status for an event
    /// </summary>
    /// <param name="eventId">Event ID to get status for</param>
    [HubMethodName("RequestQueueStatus")]
    public async Task RequestQueueStatusAsync(string eventId)
    {
        try
        {
            if (!Guid.TryParse(eventId, out var eventGuid))
            {
                _logger.LogWarning("Invalid event ID format: {EventId} from user {UserId}", eventId, GetCurrentUserId());
                await Clients.Caller.SendAsync("Error", "Invalid event ID format");
                return;
            }

            _logger.LogDebug("User {UserId} requested queue status for event {EventId}", GetCurrentUserId(), eventGuid);

            // Send request acknowledgment - actual status will be sent via IRegistrationService
            await Clients.Caller.SendAsync("QueueStatusRequested", eventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting queue status for event {EventId} by user {UserId}", eventId, GetCurrentUserId());
            await Clients.Caller.SendAsync("Error", "Failed to request queue status");
        }
    }

    /// <summary>
    /// Ping method for connection testing
    /// </summary>
    [HubMethodName("Ping")]
    public async Task PingAsync()
    {
        await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
    }

    /// <summary>
    /// Get the current authenticated user ID
    /// </summary>
    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Get the SignalR group name for a user's personal notifications
    /// </summary>
    private static string GetUserGroup(Guid userId) => $"user_{userId}";

    /// <summary>
    /// Get the SignalR group name for event-specific notifications
    /// </summary>
    private static string GetEventGroup(Guid eventId) => $"event_{eventId}";
}

/// <summary>
/// Extension methods for sending registration-related SignalR notifications
/// </summary>
public static class RegistrationHubExtensions
{
    /// <summary>
    /// Send registration confirmation notification to a specific user
    /// </summary>
    public static async Task NotifyRegistrationConfirmedAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid userId,
        object registrationData)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("RegistrationConfirmed", registrationData);
    }

    /// <summary>
    /// Send registration queue update to a specific user
    /// </summary>
    public static async Task NotifyQueuePositionChangedAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid userId,
        int newPosition,
        int? estimatedWaitMinutes = null)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("QueuePositionChanged", new
        {
            Position = newPosition,
            EstimatedWaitMinutes = estimatedWaitMinutes,
            UpdatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send queue status update to all users monitoring an event
    /// </summary>
    public static async Task NotifyEventQueueStatusAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid eventId,
        object queueStatus)
    {
        var eventGroup = $"event_{eventId}";
        await hubContext.Clients.Group(eventGroup).SendAsync("QueueStatusUpdated", queueStatus);
    }

    /// <summary>
    /// Send registration cancellation notification to a specific user
    /// </summary>
    public static async Task NotifyRegistrationCancelledAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid userId,
        Guid eventId,
        string reason)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("RegistrationCancelled", new
        {
            EventId = eventId,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send event capacity update to all users monitoring an event
    /// </summary>
    public static async Task NotifyEventCapacityChangedAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid eventId,
        int newCapacity,
        int currentRegistrations)
    {
        var eventGroup = $"event_{eventId}";
        await hubContext.Clients.Group(eventGroup).SendAsync("CapacityChanged", new
        {
            EventId = eventId,
            NewCapacity = newCapacity,
            CurrentRegistrations = currentRegistrations,
            AvailableSpots = Math.Max(0, newCapacity - currentRegistrations),
            UpdatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send queue processing notification (when queued registrations become confirmed)
    /// </summary>
    public static async Task NotifyQueueProcessedAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid eventId,
        int processedCount,
        List<Guid> confirmedUserIds)
    {
        var eventGroup = $"event_{eventId}";

        // Notify all event watchers
        await hubContext.Clients.Group(eventGroup).SendAsync("QueueProcessed", new
        {
            EventId = eventId,
            ProcessedCount = processedCount,
            ProcessedAt = DateTime.UtcNow
        });

        // Notify individual users who got confirmed
        foreach (var userId in confirmedUserIds)
        {
            var userGroup = $"user_{userId}";
            await hubContext.Clients.Group(userGroup).SendAsync("RegistrationConfirmedFromQueue", new
            {
                EventId = eventId,
                ConfirmedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Send registration error notification to a specific user
    /// </summary>
    public static async Task NotifyRegistrationErrorAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid userId,
        Guid eventId,
        string errorMessage)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("RegistrationError", new
        {
            EventId = eventId,
            Error = errorMessage,
            OccurredAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send wait time estimate update to queued users
    /// </summary>
    public static async Task NotifyWaitTimeUpdatedAsync(
        this IHubContext<RegistrationHub> hubContext,
        Guid userId,
        int estimatedWaitMinutes)
    {
        var userGroup = $"user_{userId}";
        await hubContext.Clients.Group(userGroup).SendAsync("WaitTimeUpdated", new
        {
            EstimatedWaitMinutes = estimatedWaitMinutes,
            UpdatedAt = DateTime.UtcNow
        });
    }
}
