using System.Threading.Tasks;
using Shared.SessionManagement;

namespace Shared.SessionManagement
{
    public interface ISessionManagementService
    {
        // Sessions
        Task<SessionDto> CreateSessionAsync(CreateSessionRequest request);
        Task<SessionDto> GetSessionAsync(string sessionId);
        Task<SessionDto> UpdateSessionAsync(UpdateSessionRequest request);
        Task DeleteSessionAsync(string sessionId);
        Task<PagedList<SessionDto>> ListSessionsAsync(ListSessionsRequest request);

        // Scheduling
        Task AssignSessionAsync(AssignSessionRequest request);
        Task UnassignSessionAsync(string assignmentId);
        Task<List<SessionAssignmentDto>> GetScheduleAsync(string eventId);
        Task SwapSessionsAsync(SwapSessionsRequest request);

        // Materials
        Task<SessionMaterialDto> AddMaterialAsync(AddMaterialRequest request);
        Task RemoveMaterialAsync(string materialId);

        // Feedback
        Task SubmitFeedbackAsync(SubmitFeedbackRequest request);
        Task<SessionFeedbackSummaryDto> GetFeedbackSummaryAsync(string sessionId);
    }
}
