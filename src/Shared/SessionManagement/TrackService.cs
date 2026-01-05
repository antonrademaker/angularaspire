using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.EventManagement.Entities;

namespace Shared.SessionManagement;

/// <summary>
/// Track service implementation for track management operations
/// </summary>
public class TrackService : ITrackService
{
    private readonly SessionDbContext _context;
    private readonly ILogger<TrackService> _logger;

    public TrackService(SessionDbContext context, ILogger<TrackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Track CRUD Operations

    public async Task<Result<TrackResponse>> CreateTrackAsync(CreateTrackRequest request)
    {
        try
        {
            // Validate event exists
            var eventExists = await _context.Set<Event>()
                .AnyAsync(e => e.Id == request.EventId);
            if (!eventExists)
            {
                return Result<TrackResponse>.Failure("Event not found");
            }

            // Check for duplicate slug
            var slugExists = await _context.Tracks
                .AnyAsync(t => t.EventId == request.EventId && t.Slug == request.Slug);
            if (slugExists)
            {
                return Result<TrackResponse>.Failure("A track with this slug already exists in the event");
            }

            // Get next display order if not specified
            if (request.DisplayOrder <= 0)
            {
                var maxOrder = await _context.Tracks
                    .Where(t => t.EventId == request.EventId)
                    .MaxAsync(t => (int?)t.DisplayOrder) ?? 0;
                request.DisplayOrder = maxOrder + 1;
            }

            var track = new Track
            {
                EventId = request.EventId,
                Name = request.Name,
                Description = request.Description,
                Slug = request.Slug,
                Color = request.Color,
                Icon = request.Icon,
                AudienceLevel = request.AudienceLevel,
                Category = request.Category,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                MaxConcurrentSessions = request.MaxConcurrentSessions,
                Tags = request.Tags?.Any() == true ? JsonSerializer.Serialize(request.Tags) : null,
                CustomFields = request.CustomFields?.Any() == true ? JsonSerializer.Serialize(request.CustomFields) : null,
                CreatedByUserId = Guid.NewGuid(), // TODO: Get from current user context
                CreatedAt = DateTime.UtcNow
            };

            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Track created: {TrackId} - {Name} for Event {EventId}",
                track.Id, track.Name, track.EventId);

            return Result<TrackResponse>.Success(await MapToTrackResponseAsync(track));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating track: {Name} for Event {EventId}", request.Name, request.EventId);
            return Result<TrackResponse>.Failure("Failed to create track");
        }
    }

    public async Task<TrackResponse?> GetTrackAsync(Guid trackId)
    {
        Track? track = await _context.Tracks
            .FirstOrDefaultAsync(t => t.Id == trackId);

        return track != null ? await MapToTrackResponseAsync(track) : null;
    }

    public async Task<TrackResponse?> GetTrackBySlugAsync(Guid eventId, string slug)
    {
        Track? track = await _context.Tracks
            .FirstOrDefaultAsync(t => t.EventId == eventId && t.Slug == slug);

        return track != null ? await MapToTrackResponseAsync(track) : null;
    }

    public async Task<Result<TrackResponse>> UpdateTrackAsync(Guid trackId, UpdateTrackRequest request)
    {
        try
        {
            Track? track = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
            {
                return Result<TrackResponse>.Failure("Track not found");
            }

            // Update properties if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                track.Name = request.Name;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                track.Description = request.Description;
            }

            if (!string.IsNullOrEmpty(request.Slug))
            {
                // Check for duplicate slug
                var slugExists = await _context.Tracks
                    .AnyAsync(t => t.EventId == track.EventId && t.Slug == request.Slug && t.Id != trackId);
                if (slugExists)
                {
                    return Result<TrackResponse>.Failure("A track with this slug already exists in the event");
                }
                track.Slug = request.Slug;
            }
            if (request.Color != null)
            {
                track.Color = request.Color;
            }

            if (request.Icon != null)
            {
                track.Icon = request.Icon;
            }

            if (request.AudienceLevel.HasValue)
            {
                track.AudienceLevel = request.AudienceLevel.Value;
            }

            if (request.Category.HasValue)
            {
                track.Category = request.Category.Value;
            }

            if (request.DisplayOrder.HasValue)
            {
                track.DisplayOrder = request.DisplayOrder.Value;
            }

            if (request.IsActive.HasValue)
            {
                track.IsActive = request.IsActive.Value;
            }

            if (request.MaxConcurrentSessions.HasValue)
            {
                track.MaxConcurrentSessions = request.MaxConcurrentSessions.Value;
            }

            if (request.Tags != null)
            {
                track.Tags = request.Tags.Count != 0 ? JsonSerializer.Serialize(request.Tags) : null;
            }

            if (request.CustomFields != null)
            {
                track.CustomFields = request.CustomFields.Count != 0 ? JsonSerializer.Serialize(request.CustomFields) : null;
            }

            track.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Track updated: {TrackId} - {Name}", track.Id, track.Name);

            return Result<TrackResponse>.Success(await MapToTrackResponseAsync(track));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating track: {TrackId}", trackId);
            return Result<TrackResponse>.Failure("Failed to update track");
        }
    }

    public async Task<Result> DeleteTrackAsync(Guid trackId)
    {
        try
        {
            Track? track = await _context.Tracks
                .Include(t => t.Sessions)
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
            {
                return Result.Failure("Track not found");
            }

            // Check if track has sessions
            if (track.Sessions.Any(s => s.Status != SessionStatus.Cancelled))
            {
                return Result.Failure("Cannot delete track with active sessions");
            }

            _context.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Track deleted: {TrackId} - {Name}", track.Id, track.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting track: {TrackId}", trackId);
            return Result.Failure("Failed to delete track");
        }
    }

    // Track Listing and Search

    public async Task<IEnumerable<TrackResponse>> GetEventTracksAsync(Guid eventId, bool includeInactive = false)
    {
        IQueryable<Track> query = _context.Tracks
            .Where(t => t.EventId == eventId);

        if (!includeInactive)
        {
            query = query.Where(t => t.IsActive);
        }

        List<Track> tracks = await query
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();

        var responses = new List<TrackResponse>();
        foreach (Track? track in tracks)
        {
            responses.Add(await MapToTrackResponseAsync(track));
        }

        return responses;
    }

    public async Task<PagedResult<TrackResponse>> SearchTracksAsync(TrackSearchRequest request)
    {
        IQueryable<Track> query = _context.Tracks.AsQueryable();

        // Apply filters
        if (request.EventId.HasValue)
        {
            query = query.Where(t => t.EventId == request.EventId.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(t =>
                t.Name.ToLower().Contains(searchLower) ||
                t.Description.ToLower().Contains(searchLower));
        }

        if (request.AudienceLevels?.Any() == true)
        {
            query = query.Where(t => request.AudienceLevels.Contains(t.AudienceLevel));
        }

        if (request.Categories?.Any() == true)
        {
            query = query.Where(t => request.Categories.Contains(t.Category));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == request.IsActive.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply paging and sorting
        query = query.OrderBy(t => t.DisplayOrder)
                     .Skip(request.Skip)
                     .Take(request.PageSize);

        List<Track> tracks = await query.ToListAsync();
        var trackResponses = new List<TrackResponse>();

        foreach (Track? track in tracks)
        {
            trackResponses.Add(await MapToTrackResponseAsync(track));
        }

        return new PagedResult<TrackResponse>(trackResponses, totalCount, request.Page, request.PageSize);
    }

    // Track Organization

    public async Task<Result> ReorderTracksAsync(Guid eventId, IEnumerable<TrackOrderItem> trackOrders)
    {
        try
        {
            var trackOrdersList = trackOrders.ToList();
            var trackIds = trackOrdersList.Select(to => to.TrackId).ToList();

            List<Track> tracks = await _context.Tracks
                .Where(t => t.EventId == eventId && trackIds.Contains(t.Id))
                .ToListAsync();

            if (tracks.Count != trackOrdersList.Count)
            {
                return Result.Failure("Some tracks were not found");
            }

            foreach (TrackOrderItem? trackOrder in trackOrdersList)
            {
                Track track = tracks.First(t => t.Id == trackOrder.TrackId);
                track.DisplayOrder = trackOrder.DisplayOrder;
                track.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tracks reordered for event {EventId}: {Count} tracks updated",
                eventId, tracks.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering tracks for event {EventId}", eventId);
            return Result.Failure("Failed to reorder tracks");
        }
    }

    public async Task<Result<TrackResponse>> UpdateTrackStatusAsync(Guid trackId, bool isActive)
    {
        try
        {
            Track? track = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
            {
                return Result<TrackResponse>.Failure("Track not found");
            }

            track.IsActive = isActive;
            track.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Track status updated: {TrackId} - Active: {IsActive}", trackId, isActive);

            return Result<TrackResponse>.Success(await MapToTrackResponseAsync(track));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating track status: {TrackId}", trackId);
            return Result<TrackResponse>.Failure("Failed to update track status");
        }
    }

    // Track Statistics

    public async Task<TrackStatistics> GetTrackStatisticsAsync(Guid trackId)
    {
        Track? track = await _context.Tracks
            .Include(t => t.Sessions)
            .ThenInclude(s => s.Subscriptions)
            .FirstOrDefaultAsync(t => t.Id == trackId);

        if (track == null)
        {
            return new TrackStatistics { TrackId = trackId, TrackName = "Unknown Track" };
        }

        ICollection<Session> sessions = track.Sessions;
        var subscriptions = sessions.SelectMany(s => s.Subscriptions).ToList();

        var totalCapacity = sessions.Where(s => s.MaxAttendees.HasValue).Sum(s => s.MaxAttendees!.Value);
        var totalOccupied = sessions.Sum(s => s.CurrentAttendees);
        var capacityUtilization = totalCapacity > 0 ? (double)totalOccupied / totalCapacity * 100 : 0;

        return new TrackStatistics
        {
            TrackId = track.Id,
            TrackName = track.Name,
            TotalSessions = sessions.Count,
            PublishedSessions = sessions.Count(s => s.Status == SessionStatus.Published),
            DraftSessions = sessions.Count(s => s.Status == SessionStatus.Draft),
            CompletedSessions = sessions.Count(s => s.Status == SessionStatus.Completed),
            CancelledSessions = sessions.Count(s => s.Status == SessionStatus.Cancelled),
            TotalSubscriptions = subscriptions.Count,
            ConfirmedSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Confirmed),
            WaitlistedSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Waitlisted),
            AverageSessionCapacityUtilization = capacityUtilization,
            LastSessionDateTime = sessions.Where(s => s.EndTime <= DateTime.UtcNow)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault()?.EndTime,
            NextSessionDateTime = sessions.Where(s => s.StartTime > DateTime.UtcNow)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault()?.StartTime
        };
    }

    public async Task<IEnumerable<TrackStatistics>> GetEventTrackStatisticsAsync(Guid eventId)
    {
        List<Track> tracks = await _context.Tracks
            .Where(t => t.EventId == eventId)
            .ToListAsync();

        var statistics = new List<TrackStatistics>();

        foreach (Track? track in tracks)
        {
            statistics.Add(await GetTrackStatisticsAsync(track.Id));
        }

        return statistics.OrderBy(s => s.TrackName);
    }

    // Helper Methods

    private async Task<TrackResponse> MapToTrackResponseAsync(Track track)
    {
        // Deserialize JSON strings for response
        List<string>? tags = !string.IsNullOrEmpty(track.Tags) ? JsonSerializer.Deserialize<List<string>>(track.Tags) : null;
        Dictionary<string, object>? customFields = !string.IsNullOrEmpty(track.CustomFields) ? JsonSerializer.Deserialize<Dictionary<string, object>>(track.CustomFields) : null;

        // Get session count if sessions are loaded
        int? sessionCount = null;
        int? activeSessionCount = null;
        if (track.Sessions != null)
        {
            sessionCount = track.Sessions.Count;
            activeSessionCount = track.Sessions.Count(s => s.IsPublished && s.Status != SessionStatus.Cancelled);
        }

        return new TrackResponse
        {
            Id = track.Id,
            EventId = track.EventId,
            EventName = "Event Name", // TODO: Load from Event entity when needed
            Name = track.Name,
            Description = track.Description,
            Slug = track.Slug,
            Color = track.Color,
            Icon = track.Icon,
            AudienceLevel = track.AudienceLevel,
            Category = track.Category,
            DisplayOrder = track.DisplayOrder,
            IsActive = track.IsActive,
            MaxConcurrentSessions = track.MaxConcurrentSessions,
            Tags = tags,
            CustomFields = customFields,
            CreatedAt = track.CreatedAt,
            UpdatedAt = track.UpdatedAt,
            CreatedByUserName = "User Name", // TODO: Load from User entity when needed
            SessionCount = sessionCount,
            ActiveSessionCount = activeSessionCount
        };
    }
}
