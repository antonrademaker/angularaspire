using System.Threading.Tasks;
using Shared.Registration;

namespace Shared.Registration
{
    public interface IRegistrationService
    {
        // Session Registration
        Task<RegistrationDto> RegisterForSessionAsync(RegisterForSessionRequest request);
        Task CancelRegistrationAsync(string registrationId);
        Task<PagedList<RegistrationDto>> GetSessionRegistrationsAsync(string sessionId);
        
        // Waitlist
        Task<WaitlistStatusDto> GetWaitlistStatusAsync(string sessionId);
        Task PromoteFromWaitlistAsync(string registrationId); // Admin override
    }
}
