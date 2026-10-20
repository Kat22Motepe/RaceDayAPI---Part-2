using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Dtos;
using RaceDay.Domain;
using RaceDay.Infrastructure;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    // GET /api/events (Any logged-in user can see all events)
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EventType? type)
    {
        var query = _db.Events.Include(e => e.Organiser).AsQueryable();

        // Optional filter: ?type=Run or ?type=Cycle
        if (type.HasValue) query = query.Where(e => e.EventType == type.Value);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(ToDto));
    }

    // GET /api/events/mine (Organiser only - gets ONLY their own events)
    [HttpGet("mine")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> GetMine()
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var events = await _db.Events
            .Include(e => e.Organiser)
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToDto));
    }

    // GET /api/events/5 (Get details for one specific event)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.Id == id);
        return ev is null ? NotFound() : Ok(ToDto(ev));
    }

    // POST /api/events (Organiser only)
    [HttpPost]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Create(CreateEventDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var ev = new Event
        {
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            BannerImageUrl = dto.BannerImageUrl,
            OrganiserId = userId // Link the event to the organiser who created it
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        // Load the organiser data so we can return it in the DTO
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetOne), new { id = ev.Id }, ToDto(ev));
    }

    // PUT /api/events/5 (Organiser only and must be the owner)
    [HttpPut("{id:int}")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Update(int id, UpdateEventDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.Id == id);

        if (ev is null) return NotFound();

        // Ownership check so you can't edit someone else's event.
        if (ev.OrganiserId != userId) return Forbid();

        ev.Name = dto.Name.Trim();
        ev.Description = dto.Description.Trim();
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location.Trim();
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType;
        ev.BannerImageUrl = dto.BannerImageUrl;

        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    // DELETE /api/events/5 (Organiser only AND must be the owner)
    [HttpDelete("{id:int}")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var ev = await _db.Events.FindAsync(id);

        if (ev is null) return NotFound();

        // Ownership check
        if (ev.OrganiserId != userId) return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Helper method to turn an Event into a safe DTO
    private static EventDto ToDto(Event e) => new(
        e.Id, e.Name, e.Description, e.EventDate, e.Location, e.DistanceKm,
        e.EventType, e.BannerImageUrl, e.OrganiserId,
        e.Organiser is null ? "Unknown" : $"{e.Organiser.FirstName} {e.Organiser.LastName}");
}
