using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.EventManagement.Entities;

namespace Shared.EventManagement;

/// <summary>
/// Business service implementation for event management operations
/// Implements IDesign business service patterns with full CRUD and search capabilities
/// </summary>
public class EventService : IEventService
{
    private readonly EventDbContext _eventContext;
    private readonly ILogger<EventService> _logger;
    private readonly bool _isInMemory;

    public EventService(
        EventDbContext eventContext,
        ILogger<EventService> logger,
        IOptions<DatabaseOptions>? databaseOptions = null)
    {
        _eventContext = eventContext;
        _logger = logger;
        _isInMemory = databaseOptions?.Value?.UseInMemoryDatabase ?? false;
    }

    // Event Discovery Operations

    public async Task<EventSearchResult> SearchEventsAsync(EventSearchRequest searchRequest)
    {

        var searchText = (searchRequest.SearchText ?? string.Empty).Replace(Environment.NewLine, "");
        _logger.LogInformation("Searching events with criteria: {SearchText}", searchRequest.SearchText);

        IQueryable<Event> query = _eventContext.Events.AsQueryable();

        // Only include user navigation when not using InMemory (avoids cross-context issues in tests)
        if (!_isInMemory)
        {
            // query = query.Include(e => e.CreatedByUser);
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchRequest.SearchText))
        {
            var searchText = searchRequest.SearchText.ToLower();
            query = query.Where(e =>
                e.Title.ToLower().Contains(searchText) ||
                e.Description.ToLower().Contains(searchText));
        }

        if (searchRequest.Statuses != null && searchRequest.Statuses.Any())
        {
            query = query.Where(e => searchRequest.Statuses.Contains(e.Status));
        }
        else if (searchRequest.Status.HasValue)
        {
            query = query.Where(e => e.Status == searchRequest.Status.Value);
        }

        if (searchRequest.StartDateFrom.HasValue)
        {
            query = query.Where(e => e.StartDate >= searchRequest.StartDateFrom.Value);
        }

        if (searchRequest.StartDateTo.HasValue)
        {
            query = query.Where(e => e.StartDate <= searchRequest.StartDateTo.Value);
        }

        if (searchRequest.OrganizerId.HasValue)
        {
            query = query.Where(e => e.CreatedBy == searchRequest.OrganizerId.Value);
        }

        // Apply sorting
        query = ApplySorting(query, searchRequest.SortBy, searchRequest.SortAscending);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        List<Event> events = await query
            .Skip((searchRequest.Page - 1) * searchRequest.PageSize)
            .Take(searchRequest.PageSize)
            .ToListAsync();

        return new EventSearchResult
        {
            Events = events,
            TotalCount = totalCount,
            CurrentPage = searchRequest.Page,
            PageSize = searchRequest.PageSize
        };
    }

    public async Task<Event?> GetEventByIdAsync(Guid eventId, bool includeDetails = true)
    {
        _logger.LogInformation("Getting event by ID: {EventId}", eventId);

        IQueryable<Event> query = _eventContext.Events.AsQueryable();

        // Only include user navigation when not using InMemory and includeDetails is true
        if (includeDetails && !_isInMemory)
        {
            // query = query.Include(e => e.CreatedByUser);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<Event?> GetEventBySlugAsync(string slug, bool includeDetails = true)
    {
        _logger.LogInformation("Getting event by slug: {Slug}", slug);

        IQueryable<Event> query = _eventContext.Events.AsQueryable();

        // Only include user navigation when not using InMemory and includeDetails is true
        if (includeDetails && !_isInMemory)
        {
            // query = query.Include(e => e.CreatedByUser);
        }

        return await query.FirstOrDefaultAsync(e => e.Slug == slug);
    }

    public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int limit = 10)
    {
        _logger.LogInformation("Getting upcoming events (limit: {Limit})", limit);

        IQueryable<Event> query = _eventContext.Events
            .Where(e => e.StartDate > DateTime.UtcNow)
            .Where(e => e.Status == EventStatus.Published);

        return await query
            .OrderBy(e => e.StartDate)
            .Take(limit)
            .ToListAsync();
    }

    /*
    public async Task<IEnumerable<Event>> GetEventsByTagsAsync(string[] tags, int limit = 20)
    {
        _logger.LogInformation("Getting events by tags: {Tags}", string.Join(", ", tags));

        // PostgreSQL JSON queries for tag matching
        List<Event> events = await _eventContext.Events
            .Where(e => e.Status == EventStatus.Published)
            .Where(e => e.Visibility == EventVisibility.Public)
            .Where(e => e.Tags != null)
            .ToListAsync(); // Execute query first, then filter in memory

        // Filter by tags in memory (PostgreSQL JSON querying can be complex)
        IEnumerable<Event> filteredEvents = events
            .Where(e => e.Tags != null && ContainsAnyTag(e.Tags, tags))
            .OrderBy(e => e.StartDate)
            .Take(limit);

        return filteredEvents;
    }
    */

    // Event Management Operations

    public async Task<Event> CreateEventAsync(CreateEventRequest eventData, Guid createdByUserId)
    {
        _logger.LogInformation("Creating new event: {Title}", eventData.Title.Replace("\r", "").Replace("\n", ""));

        // Validate slug uniqueness
        Event? existingEvent = await _eventContext.Events
            .FirstOrDefaultAsync(e => e.Slug == eventData.Slug);
        if (existingEvent != null)
        {
            throw new InvalidOperationException($"Event with slug '{eventData.Slug}' already exists");
        }

        var newEvent = new Event
        {
            Title = eventData.Title,
            Description = eventData.Description,
            DetailedDescription = eventData.DetailedDescription,
            Slug = eventData.Slug,
            StartDate = eventData.StartDate,
            EndDate = eventData.EndDate,
            Status = eventData.Status,
            LogoUrl = eventData.LogoUrl,
            PrimaryColor = eventData.PrimaryColor,
            SeriesId = eventData.SeriesId,
            CreatedBy = createdByUserId,
            UpdatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _eventContext.Events.Add(newEvent);
        await _eventContext.SaveChangesAsync();

        _logger.LogInformation("Created event with ID: {EventId}", newEvent.Id);
        return newEvent;
    }

    public async Task<Event?> UpdateEventAsync(Guid eventId, UpdateEventRequest eventData, Guid updatedByUserId)
    {
        _logger.LogInformation("Updating event: {EventId}", eventId);

        Event? existingEvent = await _eventContext.Events.FindAsync(eventId);
        if (existingEvent == null)
        {
            return null;
        }

        // Validate slug uniqueness (excluding current event)
        if (existingEvent.Slug != eventData.Slug)
        {
            var slugExists = await _eventContext.Events
                .AnyAsync(e => e.Slug == eventData.Slug && e.Id != eventId);
            if (slugExists)
            {
                throw new InvalidOperationException($"Event with slug '{eventData.Slug}' already exists");
            }
        }

        // Update properties
        existingEvent.Title = eventData.Title;
        existingEvent.Description = eventData.Description;
        existingEvent.DetailedDescription = eventData.DetailedDescription;
        existingEvent.Slug = eventData.Slug;
        existingEvent.StartDate = eventData.StartDate;
        existingEvent.EndDate = eventData.EndDate;
        existingEvent.Status = eventData.Status;
        existingEvent.LogoUrl = eventData.LogoUrl;
        existingEvent.PrimaryColor = eventData.PrimaryColor;
        existingEvent.SeriesId = eventData.SeriesId;
        existingEvent.UpdatedBy = updatedByUserId;
        existingEvent.UpdatedAt = DateTime.UtcNow;

        await _eventContext.SaveChangesAsync();

        _logger.LogInformation("Updated event: {EventId}", eventId);
        return existingEvent;
    }

    public async Task<Shared.Common.Result> UpdateEventStatusAsync(Guid eventId, EventStatus newStatus, Guid userId)
    {
        var evt = await _eventContext.Events.FindAsync(eventId);
        if (evt == null)
            return Shared.Common.Result.Failure("Event not found");

        var transitionResult = Shared.EventManagement.Domain.EventStatusMachine.CanTransition(evt.Status, newStatus);
        if (!transitionResult.IsSuccess)
        {
            return transitionResult;
        }

        evt.Status = newStatus;
        evt.UpdatedAt = DateTime.UtcNow;
        evt.UpdatedBy = userId;

        await _eventContext.SaveChangesAsync();
        return Shared.Common.Result.Success();
    }

    public async Task<bool> DeleteEventAsync(Guid eventId, Guid deletedByUserId)
    {
        _logger.LogInformation("Deleting event: {EventId} by user: {UserId}", eventId, deletedByUserId);

        Event? existingEvent = await _eventContext.Events.FindAsync(eventId);
        if (existingEvent == null)
        {
            return false;
        }

        _eventContext.Events.Remove(existingEvent);
        await _eventContext.SaveChangesAsync();

        _logger.LogInformation("Deleted event: {EventId}", eventId);
        return true;
    }

    public async Task<Event?> ChangeEventStatusAsync(Guid eventId, EventStatus newStatus, Guid updatedByUserId)
    {
        _logger.LogInformation("Changing event status: {EventId} to {Status}", eventId, newStatus);

        Event? existingEvent = await _eventContext.Events.FindAsync(eventId);
        if (existingEvent == null)
        {
            return null;
        }

        existingEvent.Status = newStatus;
        existingEvent.UpdatedAt = DateTime.UtcNow;

        await _eventContext.SaveChangesAsync();

        _logger.LogInformation("Changed event status: {EventId} to {Status}", eventId, newStatus);
        return existingEvent;
    }

    // Registration Management

    /*
    public async Task<bool> IsRegistrationAvailableAsync(Guid eventId)
    {
        Event? eventItem = await _eventContext.Events.FindAsync(eventId);
        if (eventItem == null)
        {
            return false;
        }

        // Check if event is published and registration is open
        if (eventItem.Status != EventStatus.Published)
        {
            return false;
        }

        // Check registration dates
        DateTime now = DateTime.UtcNow;
        if (eventItem.RegistrationOpenDate.HasValue && now < eventItem.RegistrationOpenDate.Value)
        {
            return false;
        }

        if (eventItem.RegistrationCloseDate.HasValue && now > eventItem.RegistrationCloseDate.Value)
        {
            return false;
        }

        // Check capacity
        return eventItem.CurrentAttendees < eventItem.MaxAttendees;
    }

    public async Task<int> UpdateAttendeeCountAsync(Guid eventId, int increment)
    {
        Event? eventItem = await _eventContext.Events.FindAsync(eventId);
        if (eventItem == null)
        {
            throw new InvalidOperationException($"Event with ID {eventId} not found");
        }

        eventItem.CurrentAttendees = Math.Max(0, eventItem.CurrentAttendees + increment);
        eventItem.UpdatedAt = DateTime.UtcNow;

        await _eventContext.SaveChangesAsync();

        return eventItem.CurrentAttendees;
    }
    */

    // Reporting and Analytics

    public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(Guid organizerUserId, bool includeStats = false)
    {
        IOrderedQueryable<Event> query = _eventContext.Events
            .Where(e => e.CreatedBy == organizerUserId)
            .OrderByDescending(e => e.CreatedAt);

        return await query.ToListAsync();
    }

    /*
    public async Task<EventRegistrationStats?> GetEventStatsAsync(Guid eventId)
    {
        Event? eventItem = await _eventContext.Events.FindAsync(eventId);
        if (eventItem == null)
        {
            return null;
        }

        return new EventRegistrationStats
        {
            EventId = eventItem.Id,
            EventTitle = eventItem.Title,
            MaxAttendees = eventItem.MaxAttendees,
            CurrentAttendees = eventItem.CurrentAttendees
        };
    }
    */

    // Private helper methods

    private IQueryable<Event> ApplySorting(IQueryable<Event> query, string? sortBy, bool ascending)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query.OrderBy(e => e.StartDate);
        }

        return sortBy.ToLower() switch
        {
            "title" => ascending ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title),
            "startdate" => ascending ? query.OrderBy(e => e.StartDate) : query.OrderByDescending(e => e.StartDate),
            "createdat" => ascending ? query.OrderBy(e => e.CreatedAt) : query.OrderByDescending(e => e.CreatedAt),
            // "attendees" => ascending ? query.OrderBy(e => e.CurrentAttendees) : query.OrderByDescending(e => e.CurrentAttendees),
            _ => query.OrderBy(e => e.StartDate) // Default sort by start date
        };
    }

    /*
    private bool ContainsAnyTag(string? tagsJson, string[] searchTags)
    {
        if (string.IsNullOrEmpty(tagsJson))
        {
            return false;
        }

        List<string>? eventTags = JsonSerializer.Deserialize<List<string>>(tagsJson);
        if (eventTags == null || eventTags.Count == 0)
        {
            return false;
        }

        return searchTags.Any(searchTag =>
            eventTags.Any(t => t.Equals(searchTag, StringComparison.OrdinalIgnoreCase)));
    }
    */
}
