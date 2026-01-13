using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace PrivateApi.EventManagement;

[ApiController]
[Route("api/events/{eventId}/organizers")]
public class OrganizerController : ControllerBase
{
    private readonly EventDbContext _context;

    public OrganizerController(EventDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventOrganizerDto>>> GetOrganizers(Guid eventId)
    {
        var organizers = await _context.EventOrganizers
            .Include(eo => eo.Person)
            .Where(eo => eo.EventId == eventId)
            .Select(eo => new EventOrganizerDto(
                eo.PersonId,
                eo.Person!.FirstName,
                eo.Person.LastName,
                eo.Person.Email,
                eo.Role,
                eo.Person.PhotoUrl
            ))
            .ToListAsync();

        return organizers;
    }

    [HttpPost]
    public async Task<ActionResult> AddOrganizer(Guid eventId, AddOrganizerRequest request)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.Id == eventId);
        if (!eventExists)
            return NotFound("Event not found");

        var person = await _context.People.FirstOrDefaultAsync(p => p.Email == request.Email);
        if (person == null)
        {
            // Create person if not exists (invited)
            person = new Person
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                // UserId is null until they register/login
            };
            _context.People.Add(person);
            await _context.SaveChangesAsync();
        }

        var existingOrganizer = await _context.EventOrganizers
            .FirstOrDefaultAsync(eo => eo.EventId == eventId && eo.PersonId == person.Id);

        if (existingOrganizer != null)
        {
            return Conflict("Person is already an organizer for this event");
        }

        var organizer = new EventOrganizer
        {
            EventId = eventId,
            PersonId = person.Id,
            Role = request.Role
        };

        _context.EventOrganizers.Add(organizer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrganizers), new { eventId }, null);
    }

    [HttpDelete("{personId}")]
    public async Task<IActionResult> RemoveOrganizer(Guid eventId, Guid personId)
    {
        var organizer = await _context.EventOrganizers
            .FirstOrDefaultAsync(eo => eo.EventId == eventId && eo.PersonId == personId);

        if (organizer == null)
        {
            return NotFound();
        }

        _context.EventOrganizers.Remove(organizer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record EventOrganizerDto(Guid PersonId, string FirstName, string LastName, string Email, OrganizerRole Role, string? PhotoUrl);
public record AddOrganizerRequest(string Email, string FirstName, string LastName, OrganizerRole Role);
