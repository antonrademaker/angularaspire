using System.Threading.Tasks;
using Shared.EventManagement;

namespace Shared.EventManagement
{
    public interface IEventManagementService
    {
        // Event Lifecycle
        Task<EventDto> CreateEventAsync(CreateEventRequest request);
        Task<EventDto> GetEventAsync(string eventId);
        Task<EventDto> UpdateEventAsync(UpdateEventRequest request);
        Task DeleteEventAsync(string eventId);
        Task<PagedList<EventDto>> ListEventsAsync(ListEventsRequest request);

        // Tracks
        Task<TrackDto> CreateTrackAsync(CreateTrackRequest request);
        Task<TrackDto> UpdateTrackAsync(UpdateTrackRequest request);
        Task DeleteTrackAsync(string trackId);
        Task<List<TrackDto>> GetEventTracksAsync(string eventId);

        // Time Slots
        Task<TimeSlotDto> CreateTimeSlotAsync(CreateTimeSlotRequest request);
        Task<TimeSlotDto> UpdateTimeSlotAsync(UpdateTimeSlotRequest request);
        Task DeleteTimeSlotAsync(string timeSlotId);
        Task<List<TimeSlotDto>> GetEventTimeSlotsAsync(string eventId);

        // Infrastructure
        Task<LocationDto> CreateLocationAsync(CreateLocationRequest request);
        Task<RoomDto> CreateRoomAsync(CreateRoomRequest request);
        Task<List<LocationDto>> ListLocationsAsync();
    }
}
