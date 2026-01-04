using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Common;

namespace Shared.SessionManagement;

/// <summary>
/// Service implementation for speaker profile management
/// </summary>
public class SpeakerService : ISpeakerService
{
    private readonly SessionDbContext _context;
    private readonly ILogger<SpeakerService> _logger;

    public SpeakerService(SessionDbContext context, ILogger<SpeakerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<SpeakerProfileResponse>> CreateSpeakerProfileAsync(CreateSpeakerProfileRequest request)
    {
        try
        {
            // Check if user already has a speaker profile
            var existingProfile = await _context.SpeakerProfiles
                .FirstOrDefaultAsync(sp => sp.UserId == request.UserId);

            if (existingProfile != null)
            {
                return Result<SpeakerProfileResponse>.Failure("Speaker profile already exists for this user");
            }

            var speakerProfile = new SpeakerProfile
            {
                UserId = request.UserId,
                DisplayName = request.DisplayName,
                Title = request.Title,
                Company = request.Company,
                ShortBio = request.ShortBio,
                FullBio = request.FullBio,
                PhotoUrl = request.PhotoUrl,
                ContactEmail = request.ContactEmail,
                PhoneNumber = request.PhoneNumber,
                WebsiteUrl = request.WebsiteUrl,
                SocialLinks = request.SocialLinks != null ? JsonSerializer.Serialize(request.SocialLinks) : null,
                ExpertiseAreas = request.ExpertiseAreas != null ? JsonSerializer.Serialize(request.ExpertiseAreas) : null,
                PreferredSessionTypes = request.PreferredSessionTypes,
                AvailabilityNotes = request.AvailabilityNotes,
                IsPublic = request.IsPublic,
                IsActive = true,
                Status = SpeakerStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.SpeakerProfiles.Add(speakerProfile);
            await _context.SaveChangesAsync();

            return Result<SpeakerProfileResponse>.Success(MapToResponse(speakerProfile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating speaker profile for user {UserId}", request.UserId);
            return Result<SpeakerProfileResponse>.Failure("Failed to create speaker profile");
        }
    }

    /// <inheritdoc />
    public async Task<SpeakerProfileResponse?> GetSpeakerProfileAsync(Guid speakerId)
    {
        var speakerProfile = await _context.SpeakerProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.Id == speakerId);

        return speakerProfile != null ? MapToResponse(speakerProfile) : null;
    }

    /// <inheritdoc />
    public async Task<SpeakerProfileResponse?> GetSpeakerProfileByUserIdAsync(Guid userId)
    {
        var speakerProfile = await _context.SpeakerProfiles
            .Include(sp => sp.User)
            .FirstOrDefaultAsync(sp => sp.UserId == userId);

        return speakerProfile != null ? MapToResponse(speakerProfile) : null;
    }

    /// <inheritdoc />
    public async Task<Result<SpeakerProfileResponse>> UpdateSpeakerProfileAsync(Guid speakerId, UpdateSpeakerProfileRequest request)
    {
        try
        {
            var speakerProfile = await _context.SpeakerProfiles
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(sp => sp.Id == speakerId);

            if (speakerProfile == null)
            {
                return Result<SpeakerProfileResponse>.Failure("Speaker profile not found");
            }

            // Update only provided fields
            if (!string.IsNullOrEmpty(request.DisplayName))
                speakerProfile.DisplayName = request.DisplayName;
            if (request.Title != null)
                speakerProfile.Title = request.Title;
            if (request.Company != null)
                speakerProfile.Company = request.Company;
            if (request.ShortBio != null)
                speakerProfile.ShortBio = request.ShortBio;
            if (request.FullBio != null)
                speakerProfile.FullBio = request.FullBio;
            if (request.PhotoUrl != null)
                speakerProfile.PhotoUrl = request.PhotoUrl;
            if (request.ContactEmail != null)
                speakerProfile.ContactEmail = request.ContactEmail;
            if (request.PhoneNumber != null)
                speakerProfile.PhoneNumber = request.PhoneNumber;
            if (request.WebsiteUrl != null)
                speakerProfile.WebsiteUrl = request.WebsiteUrl;
            if (request.SocialLinks != null)
                speakerProfile.SocialLinks = JsonSerializer.Serialize(request.SocialLinks);
            if (request.ExpertiseAreas != null)
                speakerProfile.ExpertiseAreas = JsonSerializer.Serialize(request.ExpertiseAreas);
            if (request.PreferredSessionTypes != null)
                speakerProfile.PreferredSessionTypes = request.PreferredSessionTypes;
            if (request.AvailabilityNotes != null)
                speakerProfile.AvailabilityNotes = request.AvailabilityNotes;
            if (request.IsPublic.HasValue)
                speakerProfile.IsPublic = request.IsPublic.Value;
            if (request.IsActive.HasValue)
                speakerProfile.IsActive = request.IsActive.Value;

            speakerProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<SpeakerProfileResponse>.Success(MapToResponse(speakerProfile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating speaker profile {SpeakerId}", speakerId);
            return Result<SpeakerProfileResponse>.Failure("Failed to update speaker profile");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteSpeakerProfileAsync(Guid speakerId)
    {
        try
        {
            var speakerProfile = await _context.SpeakerProfiles
                .Include(sp => sp.SessionAssignments)
                .FirstOrDefaultAsync(sp => sp.Id == speakerId);

            if (speakerProfile == null)
            {
                return Result<bool>.Failure("Speaker profile not found");
            }

            // Check if speaker is assigned to any sessions
            if (speakerProfile.SessionAssignments.Any())
            {
                return Result<bool>.Failure($"Speaker is assigned to {speakerProfile.SessionAssignments.Count} session(s). Remove assignments first.");
            }

            _context.SpeakerProfiles.Remove(speakerProfile);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting speaker profile {SpeakerId}", speakerId);
            return Result<bool>.Failure("Failed to delete speaker profile");
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<SpeakerProfileResponse>> SearchSpeakersAsync(SpeakerSearchRequest request)
    {
        var query = _context.SpeakerProfiles.Include(sp => sp.User).AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerm = request.Query.ToLower();
            query = query.Where(sp =>
                sp.DisplayName.ToLower().Contains(searchTerm) ||
                (sp.Company != null && sp.Company.ToLower().Contains(searchTerm)) ||
                (sp.ShortBio != null && sp.ShortBio.ToLower().Contains(searchTerm)) ||
                (sp.Title != null && sp.Title.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrEmpty(request.Company))
        {
            query = query.Where(sp => sp.Company != null && sp.Company.ToLower().Contains(request.Company.ToLower()));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(sp => sp.Status == request.Status.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(sp => sp.IsActive == request.IsActive.Value);
        }

        if (request.IsPublic.HasValue)
        {
            query = query.Where(sp => sp.IsPublic == request.IsPublic.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = request.SortBy?.ToLower() switch
        {
            "company" => request.SortDescending ? query.OrderByDescending(sp => sp.Company) : query.OrderBy(sp => sp.Company),
            "createdat" => request.SortDescending ? query.OrderByDescending(sp => sp.CreatedAt) : query.OrderBy(sp => sp.CreatedAt),
            "updatedat" => request.SortDescending ? query.OrderByDescending(sp => sp.UpdatedAt) : query.OrderBy(sp => sp.UpdatedAt),
            _ => request.SortDescending ? query.OrderByDescending(sp => sp.DisplayName) : query.OrderBy(sp => sp.DisplayName)
        };

        // Apply pagination
        var speakers = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<SpeakerProfileResponse>(
            speakers.Select(MapToResponse).ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SpeakerProfileResponse>> GetSpeakersByEventAsync(Guid eventId)
    {
        var speakerIds = await _context.SessionSpeakers
            .Include(ss => ss.Session)
            .Where(ss => ss.Session != null && ss.Session.EventId == eventId)
            .Select(ss => ss.SpeakerId)
            .Distinct()
            .ToListAsync();

        var speakers = await _context.SpeakerProfiles
            .Include(sp => sp.User)
            .Where(sp => speakerIds.Contains(sp.Id))
            .ToListAsync();

        return speakers.Select(MapToResponse);
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<SessionSpeakerDetailResponse>>> AssignSpeakerToSessionAsync(Guid sessionId, AssignSpeakerRequest request)
    {
        try
        {
            // Verify session exists
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Session not found");
            }

            // Verify speaker exists
            var speaker = await _context.SpeakerProfiles.FindAsync(request.SpeakerId);
            if (speaker == null)
            {
                return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Speaker profile not found");
            }

            // Check if speaker is already assigned
            var existingAssignment = await _context.SessionSpeakers
                .FirstOrDefaultAsync(ss => ss.SessionId == sessionId && ss.SpeakerId == request.SpeakerId);

            if (existingAssignment != null)
            {
                return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Speaker is already assigned to this session");
            }

            // Check for schedule conflicts
            var conflictingSessions = await _context.SessionSpeakers
                .Include(ss => ss.Session)
                .Where(ss => ss.SpeakerId == request.SpeakerId && ss.Session != null &&
                    ss.Session.StartTime < session.EndTime && ss.Session.EndTime > session.StartTime)
                .ToListAsync();

            if (conflictingSessions.Any())
            {
                return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure(
                    $"Speaker has a schedule conflict with {conflictingSessions.Count} other session(s)");
            }

            // Create the assignment
            var sessionSpeaker = new SessionSpeaker
            {
                SessionId = sessionId,
                SpeakerId = request.SpeakerId,
                Role = request.Role,
                DisplayOrder = request.DisplayOrder
            };

            _context.SessionSpeakers.Add(sessionSpeaker);
            await _context.SaveChangesAsync();

            // Return all session speakers
            return await GetSessionSpeakersAsync(sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning speaker {SpeakerId} to session {SessionId}", request.SpeakerId, sessionId);
            return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Failed to assign speaker to session");
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RemoveSpeakerFromSessionAsync(Guid sessionId, Guid speakerId)
    {
        try
        {
            var sessionSpeaker = await _context.SessionSpeakers
                .FirstOrDefaultAsync(ss => ss.SessionId == sessionId && ss.SpeakerId == speakerId);

            if (sessionSpeaker == null)
            {
                return Result<bool>.Failure("Speaker assignment not found");
            }

            _context.SessionSpeakers.Remove(sessionSpeaker);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing speaker {SpeakerId} from session {SessionId}", speakerId, sessionId);
            return Result<bool>.Failure("Failed to remove speaker from session");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<SessionSpeakerDetailResponse>>> GetSessionSpeakersAsync(Guid sessionId)
    {
        try
        {
            // Verify session exists
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null)
            {
                return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Session not found");
            }

            var sessionSpeakers = await _context.SessionSpeakers
                .Include(ss => ss.Speaker)
                    .ThenInclude(sp => sp!.User)
                .Where(ss => ss.SessionId == sessionId)
                .OrderBy(ss => ss.DisplayOrder)
                .ToListAsync();

            var responses = sessionSpeakers.Select(MapToSessionSpeakerDetailResponse).ToList();
            return Result<IEnumerable<SessionSpeakerDetailResponse>>.Success(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting speakers for session {SessionId}", sessionId);
            return Result<IEnumerable<SessionSpeakerDetailResponse>>.Failure("Failed to get session speakers");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SessionSpeakerDetailResponse>> UpdateSessionSpeakerAsync(Guid sessionId, Guid speakerId, UpdateSessionSpeakerRequest request)
    {
        try
        {
            var sessionSpeaker = await _context.SessionSpeakers
                .Include(ss => ss.Speaker)
                    .ThenInclude(sp => sp!.User)
                .FirstOrDefaultAsync(ss => ss.SessionId == sessionId && ss.SpeakerId == speakerId);

            if (sessionSpeaker == null)
            {
                return Result<SessionSpeakerDetailResponse>.Failure("Speaker assignment not found");
            }

            if (request.Role.HasValue)
                sessionSpeaker.Role = request.Role.Value;
            if (request.DisplayOrder.HasValue)
                sessionSpeaker.DisplayOrder = request.DisplayOrder.Value;
            if (request.Status.HasValue)
            {
                var previousStatus = sessionSpeaker.Status;
                sessionSpeaker.Status = request.Status.Value;
                
                // Set timestamps based on status changes
                if (request.Status.Value == SessionSpeakerStatus.Contacted && previousStatus != SessionSpeakerStatus.Contacted)
                    sessionSpeaker.ContactedAt = DateTime.UtcNow;
                if (request.Status.Value == SessionSpeakerStatus.Confirmed && previousStatus != SessionSpeakerStatus.Confirmed)
                    sessionSpeaker.ConfirmedAt = DateTime.UtcNow;
            }
            if (request.StatusNotes != null)
                sessionSpeaker.StatusNotes = request.StatusNotes;

            await _context.SaveChangesAsync();

            return Result<SessionSpeakerDetailResponse>.Success(MapToSessionSpeakerDetailResponse(sessionSpeaker));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating session speaker {SpeakerId} for session {SessionId}", speakerId, sessionId);
            return Result<SessionSpeakerDetailResponse>.Failure("Failed to update session speaker");
        }
    }

    /// <inheritdoc />
    public async Task<Result<SpeakerProfileResponse>> UpdateSpeakerStatusAsync(Guid speakerId, SpeakerStatus status)
    {
        try
        {
            var speakerProfile = await _context.SpeakerProfiles
                .Include(sp => sp.User)
                .FirstOrDefaultAsync(sp => sp.Id == speakerId);

            if (speakerProfile == null)
            {
                return Result<SpeakerProfileResponse>.Failure("Speaker profile not found");
            }

            speakerProfile.Status = status;
            speakerProfile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<SpeakerProfileResponse>.Success(MapToResponse(speakerProfile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating speaker status {SpeakerId}", speakerId);
            return Result<SpeakerProfileResponse>.Failure("Failed to update speaker status");
        }
    }

    private static SpeakerProfileResponse MapToResponse(SpeakerProfile speakerProfile)
    {
        return new SpeakerProfileResponse
        {
            Id = speakerProfile.Id,
            UserId = speakerProfile.UserId,
            DisplayName = speakerProfile.DisplayName,
            Title = speakerProfile.Title,
            Company = speakerProfile.Company,
            ShortBio = speakerProfile.ShortBio,
            FullBio = speakerProfile.FullBio,
            PhotoUrl = speakerProfile.PhotoUrl,
            ContactEmail = speakerProfile.ContactEmail,
            PhoneNumber = speakerProfile.PhoneNumber,
            WebsiteUrl = speakerProfile.WebsiteUrl,
            SocialLinks = !string.IsNullOrEmpty(speakerProfile.SocialLinks) ? JsonSerializer.Deserialize<Dictionary<string, string>>(speakerProfile.SocialLinks) : null,
            ExpertiseAreas = !string.IsNullOrEmpty(speakerProfile.ExpertiseAreas) ? JsonSerializer.Deserialize<List<string>>(speakerProfile.ExpertiseAreas) : null,
            PreferredSessionTypes = speakerProfile.PreferredSessionTypes,
            AvailabilityNotes = speakerProfile.AvailabilityNotes,
            IsPublic = speakerProfile.IsPublic,
            IsActive = speakerProfile.IsActive,
            Status = speakerProfile.Status,
            CreatedAt = speakerProfile.CreatedAt,
            UpdatedAt = speakerProfile.UpdatedAt,
            UserEmail = speakerProfile.User?.Email,
            UserFullName = speakerProfile.User?.FullName
        };
    }

    private static SessionSpeakerDetailResponse MapToSessionSpeakerDetailResponse(SessionSpeaker sessionSpeaker)
    {
        return new SessionSpeakerDetailResponse
        {
            Id = sessionSpeaker.Id,
            SessionId = sessionSpeaker.SessionId,
            SpeakerId = sessionSpeaker.SpeakerId,
            Role = sessionSpeaker.Role,
            Status = sessionSpeaker.Status,
            StatusNotes = sessionSpeaker.StatusNotes,
            ContactedAt = sessionSpeaker.ContactedAt,
            ConfirmedAt = sessionSpeaker.ConfirmedAt,
            DisplayOrder = sessionSpeaker.DisplayOrder,
            Speaker = sessionSpeaker.Speaker != null ? MapToResponse(sessionSpeaker.Speaker) : null
        };
    }
}
