using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;
using Shared.Data;

namespace Shared.EventManagement;

/// <summary>
/// Implementation of social event management service
/// Handles social events, RSVPs, capacity management, and waitlists
/// </summary>
public class SocialEventService : ISocialEventService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SocialEventService> _logger;

    public SocialEventService(AppDbContext context, ILogger<SocialEventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Social Event Discovery Operations

    public async Task<IEnumerable<SocialEventResponse>> GetSocialEventsForEventAsync(
        Guid eventId,
        bool publishedOnly = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SocialEvent> query = _context.SocialEvents
            .Where(se => se.EventId == eventId);

        if (publishedOnly)
        {
            query = query.Where(se => se.IsPublished && se.Status != SocialEventStatus.Cancelled);
        }

        List<SocialEvent> socialEvents = await query
            .OrderBy(se => se.DisplayOrder)
            .ThenBy(se => se.StartTime)
            .ToListAsync(cancellationToken);

        return socialEvents.Select(MapToResponse);
    }

    public async Task<SocialEventResponse?> GetSocialEventByIdAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        return socialEvent != null ? MapToResponse(socialEvent) : null;
    }

    public async Task<SocialEventResponse?> GetSocialEventBySlugAsync(
        Guid eventId,
        string slug,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.EventId == eventId && se.Slug == slug, cancellationToken);

        return socialEvent != null ? MapToResponse(socialEvent) : null;
    }

    public async Task<PagedResult<SocialEventResponse>> SearchSocialEventsAsync(
        SocialEventSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<SocialEvent> query = _context.SocialEvents.AsQueryable();

        // Apply filters
        if (request.EventId.HasValue)
        {
            query = query.Where(se => se.EventId == request.EventId.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(se => se.Type == request.Type.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(se => se.Status == request.Status.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(se => se.StartTime >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(se => se.EndTime <= request.ToDate.Value);
        }

        if (request.PublishedOnly)
        {
            query = query.Where(se => se.IsPublished && se.Status != SocialEventStatus.Cancelled);
        }

        if (request.HasAvailableSpots == true)
        {
            query = query.Where(se => se.MaxCapacity == null || se.CurrentRsvpCount < se.MaxCapacity);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(se =>
                se.Title.ToLower().Contains(searchLower) ||
                se.Description.ToLower().Contains(searchLower) ||
                (se.Location != null && se.Location.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "title" => request.SortDescending ? query.OrderByDescending(se => se.Title) : query.OrderBy(se => se.Title),
            "type" => request.SortDescending ? query.OrderByDescending(se => se.Type) : query.OrderBy(se => se.Type),
            "createdat" => request.SortDescending ? query.OrderByDescending(se => se.CreatedAt) : query.OrderBy(se => se.CreatedAt),
            "displayorder" => request.SortDescending ? query.OrderByDescending(se => se.DisplayOrder) : query.OrderBy(se => se.DisplayOrder),
            _ => request.SortDescending ? query.OrderByDescending(se => se.StartTime) : query.OrderBy(se => se.StartTime)
        };

        // Apply pagination
        List<SocialEvent> socialEvents = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SocialEventResponse>(
            socialEvents.Select(MapToResponse),
            totalCount,
            request.Page,
            request.PageSize);
    }

    #endregion

    #region Social Event Management Operations

    public async Task<SocialEventResponse> CreateSocialEventAsync(
        CreateSocialEventRequest request,
        Guid createdByUserId,
        CancellationToken cancellationToken = default)
    {
        // Verify parent event exists
        var eventExists = await _context.Events
            .AnyAsync(e => e.Id == request.EventId, cancellationToken);

        if (!eventExists)
        {
            throw new InvalidOperationException($"Event with ID {request.EventId} not found");
        }

        // Generate slug if not provided
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? GenerateSlug(request.Title)
            : request.Slug;

        // Ensure slug is unique within the event
        var slugExists = await _context.SocialEvents
            .AnyAsync(se => se.EventId == request.EventId && se.Slug == slug, cancellationToken);

        if (slugExists)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks}";
        }

        var socialEvent = new SocialEvent
        {
            EventId = request.EventId,
            Title = request.Title,
            Slug = slug,
            Description = request.Description,
            Type = request.Type,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location,
            Room = request.Room,
            MaxCapacity = request.MaxCapacity,
            RsvpRequired = request.RsvpRequired,
            RsvpDeadline = request.RsvpDeadline,
            GuestsAllowed = request.GuestsAllowed,
            MaxGuestsPerAttendee = request.MaxGuestsPerAttendee,
            DressCode = request.DressCode,
            CostPerPerson = request.CostPerPerson,
            Currency = request.Currency,
            DietaryInfo = request.DietaryInfo,
            Notes = request.Notes,
            ImageUrl = request.ImageUrl,
            DisplayOrder = request.DisplayOrder,
            Status = SocialEventStatus.Draft,
            IsPublished = false,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Handle tags
        if (request.Tags != null && request.Tags.Any())
        {
            socialEvent.Tags = JsonSerializer.Serialize(request.Tags);
        }

        _context.SocialEvents.Add(socialEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created social event {SocialEventId} ({Title}) for event {EventId}",
            socialEvent.Id, socialEvent.Title, socialEvent.EventId);

        return MapToResponse(socialEvent);
    }

    public async Task<SocialEventResponse?> UpdateSocialEventAsync(
        Guid socialEventId,
        UpdateSocialEventRequest request,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return null;
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            socialEvent.Title = request.Title;
        }

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            // Verify slug uniqueness
            var slugExists = await _context.SocialEvents
                .AnyAsync(se => se.EventId == socialEvent.EventId && se.Slug == request.Slug && se.Id != socialEventId, cancellationToken);

            if (slugExists)
            {
                throw new InvalidOperationException($"Slug '{request.Slug}' is already in use for this event");
            }
            socialEvent.Slug = request.Slug;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            socialEvent.Description = request.Description;
        }

        if (request.Type.HasValue)
        {
            socialEvent.Type = request.Type.Value;
        }

        if (request.StartTime.HasValue)
        {
            socialEvent.StartTime = request.StartTime.Value;
        }

        if (request.EndTime.HasValue)
        {
            socialEvent.EndTime = request.EndTime.Value;
        }

        if (request.Location != null)
        {
            socialEvent.Location = request.Location;
        }

        if (request.Room != null)
        {
            socialEvent.Room = request.Room;
        }

        if (request.MaxCapacity.HasValue)
        {
            socialEvent.MaxCapacity = request.MaxCapacity.Value > 0 ? request.MaxCapacity.Value : null;
        }

        if (request.RsvpRequired.HasValue)
        {
            socialEvent.RsvpRequired = request.RsvpRequired.Value;
        }

        if (request.RsvpDeadline.HasValue)
        {
            socialEvent.RsvpDeadline = request.RsvpDeadline.Value;
        }

        if (request.GuestsAllowed.HasValue)
        {
            socialEvent.GuestsAllowed = request.GuestsAllowed.Value;
        }

        if (request.MaxGuestsPerAttendee.HasValue)
        {
            socialEvent.MaxGuestsPerAttendee = request.MaxGuestsPerAttendee.Value;
        }

        if (request.DressCode != null)
        {
            socialEvent.DressCode = request.DressCode;
        }

        if (request.CostPerPerson.HasValue)
        {
            socialEvent.CostPerPerson = request.CostPerPerson.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            socialEvent.Currency = request.Currency;
        }

        if (request.DietaryInfo != null)
        {
            socialEvent.DietaryInfo = request.DietaryInfo;
        }

        if (request.Notes != null)
        {
            socialEvent.Notes = request.Notes;
        }

        if (request.ImageUrl != null)
        {
            socialEvent.ImageUrl = request.ImageUrl;
        }

        if (request.Status.HasValue)
        {
            socialEvent.Status = request.Status.Value;
        }

        if (request.DisplayOrder.HasValue)
        {
            socialEvent.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.IsPublished.HasValue)
        {
            socialEvent.IsPublished = request.IsPublished.Value;
        }

        if (request.Tags != null)
        {
            socialEvent.Tags = request.Tags.Any()
                ? JsonSerializer.Serialize(request.Tags)
                : null;
        }

        socialEvent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated social event {SocialEventId} ({Title})",
            socialEvent.Id, socialEvent.Title);

        return MapToResponse(socialEvent);
    }

    public async Task<bool> DeleteSocialEventAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return false;
        }

        _context.SocialEvents.Remove(socialEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deleted social event {SocialEventId} ({Title})",
            socialEventId, socialEvent.Title);

        return true;
    }

    public async Task<bool> PublishSocialEventAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return false;
        }

        socialEvent.IsPublished = true;
        socialEvent.Status = SocialEventStatus.Published;
        socialEvent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Published social event {SocialEventId} ({Title})",
            socialEventId, socialEvent.Title);

        return true;
    }

    public async Task<bool> CancelSocialEventAsync(
        Guid socialEventId,
        string? cancellationReason = null,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return false;
        }

        socialEvent.Status = SocialEventStatus.Cancelled;
        socialEvent.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(cancellationReason))
        {
            socialEvent.Notes = string.IsNullOrWhiteSpace(socialEvent.Notes)
                ? $"Cancelled: {cancellationReason}"
                : $"{socialEvent.Notes}\n\nCancelled: {cancellationReason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cancelled social event {SocialEventId} ({Title}). Reason: {Reason}",
            socialEventId, socialEvent.Title, cancellationReason ?? "Not specified");

        return true;
    }

    #endregion

    #region RSVP Operations

    public async Task<RsvpResult> CreateRsvpAsync(
        Guid socialEventId,
        Guid userId,
        CreateRsvpRequest request,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return RsvpResult.Failed("Social event not found", RsvpErrorCode.SocialEventNotFound);
        }

        if (socialEvent.Status == SocialEventStatus.Cancelled)
        {
            return RsvpResult.Failed("This event has been cancelled", RsvpErrorCode.SocialEventCancelled);
        }

        if (!socialEvent.IsPublished)
        {
            return RsvpResult.Failed("This event is not yet published", RsvpErrorCode.SocialEventNotPublished);
        }

        if (socialEvent.RsvpDeadline.HasValue && DateTime.UtcNow > socialEvent.RsvpDeadline.Value)
        {
            return RsvpResult.Failed("RSVP deadline has passed", RsvpErrorCode.RsvpDeadlinePassed);
        }

        // Check if user already has an RSVP
        SocialEventRsvp? existingRsvp = await _context.SocialEventRsvps
            .FirstOrDefaultAsync(r => r.SocialEventId == socialEventId && r.UserId == userId, cancellationToken);

        if (existingRsvp != null && existingRsvp.Status != SocialEventRsvpStatus.Cancelled)
        {
            return RsvpResult.Failed("You have already RSVP'd to this event", RsvpErrorCode.AlreadyRsvped);
        }

        // Validate guest count
        if (request.GuestCount > 0)
        {
            if (!socialEvent.GuestsAllowed)
            {
                return RsvpResult.Failed("Guests are not allowed for this event", RsvpErrorCode.GuestLimitExceeded);
            }

            if (request.GuestCount > socialEvent.MaxGuestsPerAttendee)
            {
                return RsvpResult.Failed(
                    $"Maximum {socialEvent.MaxGuestsPerAttendee} guests allowed per attendee",
                    RsvpErrorCode.GuestLimitExceeded);
            }
        }

        // Calculate total spots needed (attendee + guests)
        var totalSpotsNeeded = 1 + request.GuestCount;

        // Check capacity
        var isWaitlisted = false;
        var waitlistPosition = (int?)null;

        if (socialEvent.MaxCapacity.HasValue)
        {
            var availableSpots = socialEvent.MaxCapacity.Value - socialEvent.CurrentRsvpCount;

            if (availableSpots < totalSpotsNeeded)
            {
                // Add to waitlist
                isWaitlisted = true;
                waitlistPosition = socialEvent.WaitlistCount + 1;
            }
        }

        // Create or update RSVP
        SocialEventRsvp rsvp;
        if (existingRsvp != null)
        {
            // Reactivate cancelled RSVP
            rsvp = existingRsvp;
            rsvp.Status = isWaitlisted ? SocialEventRsvpStatus.Registered : SocialEventRsvpStatus.Confirmed;
        }
        else
        {
            rsvp = new SocialEventRsvp
            {
                SocialEventId = socialEventId,
                UserId = userId,
                Status = isWaitlisted ? SocialEventRsvpStatus.Registered : SocialEventRsvpStatus.Confirmed
            };
            _context.SocialEventRsvps.Add(rsvp);
        }

        rsvp.GuestCount = request.GuestCount;
        rsvp.GuestNames = request.GuestNames;
        rsvp.DietaryRequirements = request.DietaryRequirements;
        rsvp.Notes = request.Notes;
        rsvp.IsWaitlisted = isWaitlisted;
        rsvp.WaitlistPosition = waitlistPosition;
        rsvp.RegisteredAt = DateTime.UtcNow;
        rsvp.UpdatedAt = DateTime.UtcNow;

        if (!isWaitlisted)
        {
            rsvp.ConfirmedAt = DateTime.UtcNow;
        }

        // Set payment status based on event cost
        rsvp.PaymentStatus = socialEvent.CostPerPerson > 0
            ? PaymentStatus.Pending
            : PaymentStatus.NotRequired;

        // Update social event counts
        if (isWaitlisted)
        {
            socialEvent.WaitlistCount++;
        }
        else
        {
            socialEvent.CurrentRsvpCount += totalSpotsNeeded;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created RSVP {RsvpId} for user {UserId} to social event {SocialEventId}. Waitlisted: {IsWaitlisted}",
            rsvp.Id, userId, socialEventId, isWaitlisted);

        return RsvpResult.Succeeded(MapToRsvpResponse(rsvp, socialEvent.Title), isWaitlisted, waitlistPosition);
    }

    public async Task<RsvpResult> UpdateRsvpAsync(
        Guid rsvpId,
        UpdateRsvpRequest request,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .Include(r => r.SocialEvent)
            .FirstOrDefaultAsync(r => r.Id == rsvpId, cancellationToken);

        if (rsvp == null || rsvp.SocialEvent == null)
        {
            return RsvpResult.Failed("RSVP not found", RsvpErrorCode.RsvpNotFound);
        }

        // Validate guest count if being updated
        if (request.GuestCount.HasValue && request.GuestCount.Value > 0)
        {
            if (!rsvp.SocialEvent.GuestsAllowed)
            {
                return RsvpResult.Failed("Guests are not allowed for this event", RsvpErrorCode.GuestLimitExceeded);
            }

            if (request.GuestCount.Value > rsvp.SocialEvent.MaxGuestsPerAttendee)
            {
                return RsvpResult.Failed(
                    $"Maximum {rsvp.SocialEvent.MaxGuestsPerAttendee} guests allowed per attendee",
                    RsvpErrorCode.GuestLimitExceeded);
            }
        }

        // Update fields
        if (request.Status.HasValue)
        {
            // Handle status change
            SocialEventRsvpStatus oldStatus = rsvp.Status;
            rsvp.Status = request.Status.Value;

            if (request.Status.Value == SocialEventRsvpStatus.Declined)
            {
                rsvp.DeclinedAt = DateTime.UtcNow;

                // Free up spots if they were confirmed
                if (!rsvp.IsWaitlisted && (oldStatus == SocialEventRsvpStatus.Confirmed || oldStatus == SocialEventRsvpStatus.Registered))
                {
                    rsvp.SocialEvent.CurrentRsvpCount -= (1 + rsvp.GuestCount);
                    if (rsvp.SocialEvent.CurrentRsvpCount < 0)
                    {
                        rsvp.SocialEvent.CurrentRsvpCount = 0;
                    }
                }
            }
        }

        if (request.GuestCount.HasValue)
        {
            var guestDiff = request.GuestCount.Value - rsvp.GuestCount;
            rsvp.GuestCount = request.GuestCount.Value;

            // Adjust capacity count if not waitlisted
            if (!rsvp.IsWaitlisted && rsvp.Status == SocialEventRsvpStatus.Confirmed)
            {
                rsvp.SocialEvent.CurrentRsvpCount += guestDiff;
            }
        }

        if (request.GuestNames != null)
        {
            rsvp.GuestNames = request.GuestNames;
        }

        if (request.DietaryRequirements != null)
        {
            rsvp.DietaryRequirements = request.DietaryRequirements;
        }

        if (request.Notes != null)
        {
            rsvp.Notes = request.Notes;
        }

        rsvp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated RSVP {RsvpId}", rsvpId);

        return RsvpResult.Succeeded(MapToRsvpResponse(rsvp, rsvp.SocialEvent.Title), rsvp.IsWaitlisted, rsvp.WaitlistPosition);
    }

    public async Task<bool> CancelRsvpAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .Include(r => r.SocialEvent)
            .FirstOrDefaultAsync(r => r.Id == rsvpId, cancellationToken);

        if (rsvp == null || rsvp.SocialEvent == null)
        {
            return false;
        }

        var wasConfirmed = !rsvp.IsWaitlisted && (rsvp.Status == SocialEventRsvpStatus.Confirmed || rsvp.Status == SocialEventRsvpStatus.Registered);

        rsvp.Status = SocialEventRsvpStatus.Cancelled;
        rsvp.UpdatedAt = DateTime.UtcNow;

        // Free up spots
        if (wasConfirmed)
        {
            rsvp.SocialEvent.CurrentRsvpCount -= (1 + rsvp.GuestCount);
            if (rsvp.SocialEvent.CurrentRsvpCount < 0)
            {
                rsvp.SocialEvent.CurrentRsvpCount = 0;
            }
        }
        else if (rsvp.IsWaitlisted)
        {
            rsvp.SocialEvent.WaitlistCount--;
            if (rsvp.SocialEvent.WaitlistCount < 0)
            {
                rsvp.SocialEvent.WaitlistCount = 0;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Cancelled RSVP {RsvpId}", rsvpId);

        // Process waitlist if spot became available
        if (wasConfirmed)
        {
            await ProcessWaitlistAsync(rsvp.SocialEventId, cancellationToken);
        }

        return true;
    }

    public async Task<SocialEventRsvpResponse?> GetUserRsvpAsync(
        Guid socialEventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .Include(r => r.SocialEvent)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.SocialEventId == socialEventId && r.UserId == userId, cancellationToken);

        if (rsvp == null)
        {
            return null;
        }

        return MapToRsvpResponse(rsvp, rsvp.SocialEvent?.Title ?? string.Empty);
    }

    public async Task<IEnumerable<SocialEventRsvpResponse>> GetUserRsvpsForEventAsync(
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        List<SocialEventRsvp> rsvps = await _context.SocialEventRsvps
            .Include(r => r.SocialEvent)
            .Include(r => r.User)
            .Where(r => r.SocialEvent!.EventId == eventId && r.UserId == userId)
            .ToListAsync(cancellationToken);

        return rsvps.Select(r => MapToRsvpResponse(r, r.SocialEvent?.Title ?? string.Empty));
    }

    public async Task<PagedResult<SocialEventRsvpResponse>> GetRsvpsForSocialEventAsync(
        Guid socialEventId,
        RsvpSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return new PagedResult<SocialEventRsvpResponse>([], 0, request.Page, request.PageSize);
        }

        IQueryable<SocialEventRsvp> query = _context.SocialEventRsvps
            .Include(r => r.User)
            .Where(r => r.SocialEventId == socialEventId);

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(r => r.Status == request.Status.Value);
        }

        if (request.IsWaitlisted.HasValue)
        {
            query = query.Where(r => r.IsWaitlisted == request.IsWaitlisted.Value);
        }

        if (request.IsCheckedIn.HasValue)
        {
            query = query.Where(r => r.IsCheckedIn == request.IsCheckedIn.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchLower = request.SearchText.ToLower();
            query = query.Where(r =>
                (r.User != null && r.User.FullName != null && r.User.FullName.ToLower().Contains(searchLower)) ||
                (r.User != null && r.User.Email.ToLower().Contains(searchLower)) ||
                (r.GuestNames != null && r.GuestNames.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "status" => request.SortDescending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "confirmedat" => request.SortDescending ? query.OrderByDescending(r => r.ConfirmedAt) : query.OrderBy(r => r.ConfirmedAt),
            "guestcount" => request.SortDescending ? query.OrderByDescending(r => r.GuestCount) : query.OrderBy(r => r.GuestCount),
            _ => request.SortDescending ? query.OrderByDescending(r => r.RegisteredAt) : query.OrderBy(r => r.RegisteredAt)
        };

        List<SocialEventRsvp> rsvps = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SocialEventRsvpResponse>(
            rsvps.Select(r => MapToRsvpResponse(r, socialEvent.Title)),
            totalCount,
            request.Page,
            request.PageSize);
    }

    #endregion

    #region Capacity and Waitlist Operations

    public async Task<RsvpAvailability> CheckRsvpAvailabilityAsync(
        Guid socialEventId,
        int requestedSpots = 1,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null)
        {
            return new RsvpAvailability
            {
                IsAvailable = false,
                Message = "Social event not found"
            };
        }

        var isDeadlinePassed = socialEvent.RsvpDeadline.HasValue && DateTime.UtcNow > socialEvent.RsvpDeadline.Value;
        var availableSpots = socialEvent.MaxCapacity.HasValue
            ? socialEvent.MaxCapacity.Value - socialEvent.CurrentRsvpCount
            : int.MaxValue;

        return new RsvpAvailability
        {
            IsAvailable = !isDeadlinePassed && availableSpots >= requestedSpots,
            AvailableSpots = Math.Max(0, availableSpots),
            WaitlistSize = socialEvent.WaitlistCount,
            WaitlistEnabled = socialEvent.MaxCapacity.HasValue,
            RsvpDeadline = socialEvent.RsvpDeadline,
            IsDeadlinePassed = isDeadlinePassed,
            Message = isDeadlinePassed
                ? "RSVP deadline has passed"
                : availableSpots < requestedSpots
                    ? $"Only {availableSpots} spots available"
                    : null
        };
    }

    public async Task<int> ProcessWaitlistAsync(
        Guid socialEventId,
        CancellationToken cancellationToken = default)
    {
        SocialEvent? socialEvent = await _context.SocialEvents
            .FirstOrDefaultAsync(se => se.Id == socialEventId, cancellationToken);

        if (socialEvent == null || !socialEvent.MaxCapacity.HasValue)
        {
            return 0;
        }

        var availableSpots = socialEvent.MaxCapacity.Value - socialEvent.CurrentRsvpCount;
        if (availableSpots <= 0)
        {
            return 0;
        }

        // Get waitlisted RSVPs in order
        List<SocialEventRsvp> waitlistedRsvps = await _context.SocialEventRsvps
            .Where(r => r.SocialEventId == socialEventId && r.IsWaitlisted)
            .OrderBy(r => r.WaitlistPosition)
            .ThenBy(r => r.RegisteredAt)
            .ToListAsync(cancellationToken);

        var promotedCount = 0;

        foreach (SocialEventRsvp? rsvp in waitlistedRsvps)
        {
            var spotsNeeded = 1 + rsvp.GuestCount;
            if (availableSpots >= spotsNeeded)
            {
                rsvp.IsWaitlisted = false;
                rsvp.WaitlistPosition = null;
                rsvp.Status = SocialEventRsvpStatus.Confirmed;
                rsvp.ConfirmedAt = DateTime.UtcNow;
                rsvp.UpdatedAt = DateTime.UtcNow;

                socialEvent.CurrentRsvpCount += spotsNeeded;
                socialEvent.WaitlistCount--;
                availableSpots -= spotsNeeded;
                promotedCount++;

                _logger.LogInformation(
                    "Promoted RSVP {RsvpId} from waitlist for social event {SocialEventId}",
                    rsvp.Id, socialEventId);
            }
            else
            {
                break; // No more room
            }
        }

        // Reorder remaining waitlist positions
        var remainingWaitlist = waitlistedRsvps.Where(r => r.IsWaitlisted).ToList();
        for (int i = 0; i < remainingWaitlist.Count; i++)
        {
            remainingWaitlist[i].WaitlistPosition = i + 1;
        }

        if (promotedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return promotedCount;
    }

    #endregion

    #region Admin Operations

    public async Task<bool> ConfirmRsvpAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .FirstOrDefaultAsync(r => r.Id == rsvpId, cancellationToken);

        if (rsvp == null)
        {
            return false;
        }

        rsvp.Status = SocialEventRsvpStatus.Confirmed;
        rsvp.ConfirmedAt = DateTime.UtcNow;
        rsvp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Confirmed RSVP {RsvpId}", rsvpId);

        return true;
    }

    public async Task<bool> MarkAsNoShowAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .FirstOrDefaultAsync(r => r.Id == rsvpId, cancellationToken);

        if (rsvp == null)
        {
            return false;
        }

        rsvp.Status = SocialEventRsvpStatus.NoShow;
        rsvp.NoShowAt = DateTime.UtcNow;
        rsvp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Marked RSVP {RsvpId} as no-show", rsvpId);

        return true;
    }

    public async Task<bool> CheckInAttendeeAsync(
        Guid rsvpId,
        CancellationToken cancellationToken = default)
    {
        SocialEventRsvp? rsvp = await _context.SocialEventRsvps
            .FirstOrDefaultAsync(r => r.Id == rsvpId, cancellationToken);

        if (rsvp == null)
        {
            return false;
        }

        rsvp.IsCheckedIn = true;
        rsvp.CheckedInAt = DateTime.UtcNow;
        rsvp.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Checked in RSVP {RsvpId}", rsvpId);

        return true;
    }

    #endregion

    #region Helper Methods

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug.Length > 100 ? slug[..100] : slug;
    }

    private static SocialEventResponse MapToResponse(SocialEvent socialEvent)
    {
        var availableSpots = socialEvent.MaxCapacity.HasValue
            ? Math.Max(0, socialEvent.MaxCapacity.Value - socialEvent.CurrentRsvpCount)
            : int.MaxValue;

        // Convert Tags JSON string to IEnumerable<string> for response
        List<string>? tags = !string.IsNullOrEmpty(socialEvent.Tags)
            ? JsonSerializer.Deserialize<List<string>>(socialEvent.Tags)
            : null;

        return new SocialEventResponse(
            socialEvent.Id,
            socialEvent.EventId,
            socialEvent.Title,
            socialEvent.Slug,
            socialEvent.Description,
            socialEvent.Type,
            socialEvent.StartTime,
            socialEvent.EndTime,
            socialEvent.DurationMinutes,
            socialEvent.Location,
            socialEvent.Room,
            socialEvent.MaxCapacity,
            socialEvent.CurrentRsvpCount,
            socialEvent.WaitlistCount,
            availableSpots,
            socialEvent.RsvpRequired,
            socialEvent.RsvpDeadline,
            socialEvent.GuestsAllowed,
            socialEvent.MaxGuestsPerAttendee,
            socialEvent.DressCode,
            socialEvent.CostPerPerson,
            socialEvent.Currency,
            socialEvent.DietaryInfo,
            socialEvent.Notes,
            socialEvent.ImageUrl,
            socialEvent.Status,
            socialEvent.DisplayOrder,
            socialEvent.IsPublished,
            tags,
            socialEvent.CreatedAt
        );
    }

    private static SocialEventRsvpResponse MapToRsvpResponse(SocialEventRsvp rsvp, string socialEventTitle)
    {
        return new SocialEventRsvpResponse(
            rsvp.Id,
            rsvp.SocialEventId,
            socialEventTitle,
            rsvp.UserId,
            rsvp.User?.FullName,
            rsvp.User?.Email,
            rsvp.Status,
            rsvp.GuestCount,
            rsvp.GuestNames,
            rsvp.DietaryRequirements,
            rsvp.Notes,
            rsvp.IsWaitlisted,
            rsvp.WaitlistPosition,
            rsvp.RegisteredAt,
            rsvp.ConfirmedAt,
            rsvp.CheckedInAt,
            rsvp.IsCheckedIn,
            rsvp.AmountPaid,
            rsvp.PaymentStatus
        );
    }

    #endregion
}
