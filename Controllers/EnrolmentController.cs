using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Dtos;
using RaceDay.Domain;
using RaceDay.Infrastructure;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EnrolmentsController(RaceDayDbContext db) => _db = db;

    // POST /api/events/5/enrolments (Participant only)
    [HttpPost("events/{eventId:int}/enrolments")]
    [RequireRole(UserRole.Participant)]
    public async Task<IActionResult> Enrol(int eventId, CreateEnrolmentDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        // 1. Check if the event exists
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        // 2. Check if the category exists and belongs to this event
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId);
        if (category is null || category.EventId != eventId)
            return BadRequest(new { message = "Category does not belong to this event." });

        // 3. Check for double-dipping (already enrolled?)
        var exists = await _db.Enrolments
            .AnyAsync(e => e.ParticipantId == userId && e.EventId == eventId);

        if (exists) return Conflict(new { message = "You are already enrolled in this event." });

        // 4. Save the enrolment
        var enrolment = new Enrolment
        {
            ParticipantId = userId,
            EventId = eventId,
            CategoryId = category.Id,
            Status = EnrolmentStatus.Confirmed
        };

        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        // 5. Load the related data so we can return a nice response
        var full = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .FirstAsync(e => e.Id == enrolment.Id);

        return CreatedAtAction(nameof(GetMine), null, ToDto(full));
    }

    // GET /api/enrolments/mine (Participant only)
    [HttpGet("enrolments/mine")]
    [RequireRole(UserRole.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        var list = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    // GET /api/events/5/enrolments (Organiser only, must own the event)
    [HttpGet("events/{eventId:int}/enrolments")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();

        // Ownership check!
        if (ev.OrganiserId != userId) return Forbid();

        var list = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Participant.LastName)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    // DELETE /api/enrolments/8 (Participant only, must be their own enrolment)
    [HttpDelete("enrolments/{id:int}")]
    [RequireRole(UserRole.Participant)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        var enr = await _db.Enrolments.FindAsync(id);
        if (enr is null) return NotFound();

        // Ownership check. You can only cancel your OWN enrolment
        if (enr.ParticipantId != userId) return Forbid();

        _db.Enrolments.Remove(enr);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Helper method to turn an Enrolment into a DTO
    private static EnrolmentDto ToDto(Enrolment e) => new(
        e.Id, e.ParticipantId,
        $"{e.Participant.FirstName} {e.Participant.LastName}",
        e.EventId, e.Event.Name, e.CategoryId, e.Category.Name,
        e.Status, e.EnrolledAt);
}
