using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Dtos;
using RaceDay.Domain;
using RaceDay.Infrastructure;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    // POST /api/enrolments/5/result (Organiser only, must own the event)
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> Capture(int enrolmentId, CaptureResultDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        // 1. Find the enrolment and include the Event so we can check the owner
        var enr = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == enrolmentId);

        if (enr is null) return NotFound(new { message = "Enrolment not found." });

        // Ownership check. Only the organiser who created the event can capture results
        if (enr.Event.OrganiserId != userId) return Forbid();

        // 2. Check if a result was already captured for this enrolment
        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "Result already captured for this enrolment." });

        // 3. Save the result!
        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId = enr.EventId },
            new ResultDto(result.Id, enr.Id, enr.ParticipantId,
                $"{enr.Participant.FirstName} {enr.Participant.LastName}",
                enr.EventId, enr.Event.Name, enr.Category.Name,
                result.FinishTime, result.Position, result.CapturedAt));
    }

    // GET /api/results/mine (Participant only)
    [HttpGet("results/mine")]
    [RequireRole(UserRole.Participant)]
    public async Task<IActionResult> GetMine()
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        var list = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    // GET /api/events/5/results (Organiser only, must own the event)
    [HttpGet("events/{eventId:int}/results")]
    [RequireRole(UserRole.Organiser)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var userId = (int)HttpContext.Items["UserId"]!;

        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();

        // Ownership check!
        if (ev.OrganiserId != userId) return Forbid();

        var list = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Position) // Sort by 1st, 2nd, 3rd place
            .ToListAsync();

        return Ok(list.Select(ToDto));
    }

    // Helper method to turn a Result into a DTO
    private static ResultDto ToDto(Result r) => new(
        r.Id, r.EnrolmentId, r.Enrolment.ParticipantId,
        $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
        r.Enrolment.EventId, r.Enrolment.Event.Name, r.Enrolment.Category.Name,
        r.FinishTime, r.Position, r.CapturedAt);
}
