using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Dtos;
using RaceDay.Domain;
using RaceDay.Infrastructure;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    // GET /api/events/5/categories (Anyone can see the categories for an event)
    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> ListForEvent(int eventId)
    {
        // First check if the event exists
        if (!await _db.Events.AnyAsync(e => e.Id == eventId))
            return NotFound(new { message = "Event not found." });

        var cats = await _db.Categories
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(cats.Select(ToDto));
    }

    // POST /api/events/5/categories (Organiser only, must own the event)
    [HttpPost("events/{eventId:int}/categories")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Create(int eventId, CreateCategoryDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var ev = await _db.Events.FindAsync(eventId);

        if (ev is null) return NotFound(new { message = "Event not found." });

        // Ownership check. You can't add a category to someone else's event.
        if (ev.OrganiserId != userId) return Forbid();

        var cat = new Category
        {
            Name = dto.Name.Trim(),
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            DistanceKm = dto.DistanceKm,
            EventId = eventId
        };

        _db.Categories.Add(cat);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ListForEvent), new { eventId = eventId }, ToDto(cat));
    }

    // PUT /api/categories/10 (Organiser only, must own the event)
    [HttpPut("categories/{id:int}")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Update(int id, CreateCategoryDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        // We include the Event so we can check who the Organiser is
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);

        if (cat is null) return NotFound();

        // Ownership check
        if (cat.Event.OrganiserId != userId) return Forbid();

        cat.Name = dto.Name.Trim();
        cat.MinAge = dto.MinAge;
        cat.MaxAge = dto.MaxAge;
        cat.DistanceKm = dto.DistanceKm;

        await _db.SaveChangesAsync();
        return Ok(ToDto(cat));
    }

    // DELETE /api/categories/10 (Organiser only, must own the event)
    [HttpDelete("categories/{id:int}")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var cat = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.Id == id);

        if (cat is null) return NotFound();

        // Ownership check!
        if (cat.Event.OrganiserId != userId) return Forbid();

        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Helper method to turn a Category into a DTO
    private static CategoryDto ToDto(Category c) =>
        new(c.Id, c.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm);
}
