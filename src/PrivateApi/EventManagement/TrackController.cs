using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.EventManagement;
using Shared.EventManagement.Entities;

namespace PrivateApi.EventManagement;

[ApiController]
[Route("api/events/{eventId}/tracks")]
public class TrackController : ControllerBase
{
    private readonly EventDbContext _context;

    public TrackController(EventDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Track>>> GetTracks(Guid eventId)
    {
        return await _context.Tracks
            .Where(t => t.EventId == eventId)
            .OrderBy(t => t.Order)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Track>> CreateTrack(Guid eventId, CreateTrackRequest request)
    {
        var track = new Track
        {
            EventId = eventId,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Order = await _context.Tracks.CountAsync(t => t.EventId == eventId)
        };

        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTracks), new { eventId }, track);
    }

    [HttpPut("{trackId}")]
    public async Task<IActionResult> UpdateTrack(Guid eventId, Guid trackId, UpdateTrackRequest request)
    {
        var track = await _context.Tracks.FirstOrDefaultAsync(t => t.Id == trackId && t.EventId == eventId);
        if (track == null)
        {
            return NotFound();
        }

        track.Name = request.Name;
        track.Description = request.Description;
        track.Color = request.Color;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{trackId}")]
    public async Task<IActionResult> DeleteTrack(Guid eventId, Guid trackId)
    {
        var track = await _context.Tracks.FirstOrDefaultAsync(t => t.Id == trackId && t.EventId == eventId);
        if (track == null)
        {
            return NotFound();
        }

        _context.Tracks.Remove(track);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderTracks(Guid eventId, List<Guid> trackIds)
    {
        var tracks = await _context.Tracks.Where(t => t.EventId == eventId).ToListAsync();

        foreach (var track in tracks)
        {
            var index = trackIds.IndexOf(track.Id);
            if (index != -1)
            {
                track.Order = index;
            }
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateTrackRequest(string Name, string? Description, string? Color);
public record UpdateTrackRequest(string Name, string? Description, string? Color);
