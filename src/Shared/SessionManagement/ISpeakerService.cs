using Shared.Common;

namespace Shared.SessionManagement;

/// <summary>
/// Interface for speaker profile management service
/// </summary>
public interface ISpeakerService
{
    /// <summary>
    /// Create a new speaker profile
    /// </summary>
    Task<Result<SpeakerProfileResponse>> CreateSpeakerProfileAsync(CreateSpeakerProfileRequest request);

    /// <summary>
    /// Get a speaker profile by ID
    /// </summary>
    Task<SpeakerProfileResponse?> GetSpeakerProfileAsync(Guid speakerId);

    /// <summary>
    /// Get a speaker profile by user ID
    /// </summary>
    Task<SpeakerProfileResponse?> GetSpeakerProfileByUserIdAsync(Guid userId);

    /// <summary>
    /// Update a speaker profile
    /// </summary>
    Task<Result<SpeakerProfileResponse>> UpdateSpeakerProfileAsync(Guid speakerId, UpdateSpeakerProfileRequest request);

    /// <summary>
    /// Delete a speaker profile
    /// </summary>
    Task<Result<bool>> DeleteSpeakerProfileAsync(Guid speakerId);

    /// <summary>
    /// Search speaker profiles
    /// </summary>
    Task<PagedResult<SpeakerProfileResponse>> SearchSpeakersAsync(SpeakerSearchRequest request);

    /// <summary>
    /// Get all speakers for an event
    /// </summary>
    Task<IEnumerable<SpeakerProfileResponse>> GetSpeakersByEventAsync(Guid eventId);

    /// <summary>
    /// Assign a speaker to a session
    /// </summary>
    Task<Result<IEnumerable<SessionSpeakerDetailResponse>>> AssignSpeakerToSessionAsync(Guid sessionId, AssignSpeakerRequest request);

    /// <summary>
    /// Remove a speaker from a session
    /// </summary>
    Task<Result<bool>> RemoveSpeakerFromSessionAsync(Guid sessionId, Guid speakerId);

    /// <summary>
    /// Get speakers for a session
    /// </summary>
    Task<Result<IEnumerable<SessionSpeakerDetailResponse>>> GetSessionSpeakersAsync(Guid sessionId);

    /// <summary>
    /// Update a session speaker assignment
    /// </summary>
    Task<Result<SessionSpeakerDetailResponse>> UpdateSessionSpeakerAsync(Guid sessionId, Guid speakerId, UpdateSessionSpeakerRequest request);

    /// <summary>
    /// Update speaker status
    /// </summary>
    Task<Result<SpeakerProfileResponse>> UpdateSpeakerStatusAsync(Guid speakerId, SpeakerStatus status);
}

/// <summary>
/// Speaker profile response DTO
/// </summary>
public class SpeakerProfileResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? ShortBio { get; set; }
    public string? FullBio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
    public List<string>? ExpertiseAreas { get; set; }
    public string? PreferredSessionTypes { get; set; }
    public string? AvailabilityNotes { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public SpeakerStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Related user info
    public string? UserEmail { get; set; }
    public string? UserFullName { get; set; }
}

/// <summary>
/// Create speaker profile request
/// </summary>
public class CreateSpeakerProfileRequest
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? ShortBio { get; set; }
    public string? FullBio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
    public List<string>? ExpertiseAreas { get; set; }
    public string? PreferredSessionTypes { get; set; }
    public string? AvailabilityNotes { get; set; }
    public bool IsPublic { get; set; } = true;
}

/// <summary>
/// Update speaker profile request
/// </summary>
public class UpdateSpeakerProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? ShortBio { get; set; }
    public string? FullBio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? WebsiteUrl { get; set; }
    public Dictionary<string, string>? SocialLinks { get; set; }
    public List<string>? ExpertiseAreas { get; set; }
    public string? PreferredSessionTypes { get; set; }
    public string? AvailabilityNotes { get; set; }
    public bool? IsPublic { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Speaker search request
/// </summary>
public class SpeakerSearchRequest
{
    public string? Query { get; set; }
    public string? Company { get; set; }
    public List<string>? ExpertiseAreas { get; set; }
    public SpeakerStatus? Status { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsPublic { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "DisplayName";
    public bool SortDescending { get; set; } = false;
}

/// <summary>
/// Assign speaker to session request
/// </summary>
public class AssignSpeakerRequest
{
    public Guid SpeakerId { get; set; }
    public SpeakerRole Role { get; set; } = SpeakerRole.Speaker;
    public int DisplayOrder { get; set; } = 0;
}

/// <summary>
/// Update session speaker request
/// </summary>
public class UpdateSessionSpeakerRequest
{
    public SpeakerRole? Role { get; set; }
    public int? DisplayOrder { get; set; }
    public SessionSpeakerStatus? Status { get; set; }
    public string? StatusNotes { get; set; }
}

/// <summary>
/// Extended session speaker response with full speaker profile details
/// </summary>
public class SessionSpeakerDetailResponse
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid SpeakerId { get; set; }
    public SpeakerRole Role { get; set; }
    public SessionSpeakerStatus Status { get; set; }
    public string? StatusNotes { get; set; }
    public DateTime? ContactedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public int DisplayOrder { get; set; }
    public SpeakerProfileResponse? Speaker { get; set; }
}

/// <summary>
/// Update speaker status request
/// </summary>
public class UpdateSpeakerStatusRequest
{
    public SpeakerStatus Status { get; set; }
}
