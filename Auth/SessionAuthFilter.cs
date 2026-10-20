using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RaceDay.Infrastructure;

namespace RaceDay.API.Auth;

public class SessionAuthFilter : IAsyncAuthorizationFilter
{
    private readonly RaceDayDbContext _db;

    public SessionAuthFilter(RaceDayDbContext db) => _db = db;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var endpoint = ctx.ActionDescriptor.EndpointMetadata;

        // If the endpoint has the "AllowAnonymous" sticker, allow access.
        if (endpoint.OfType<AllowAnonymousAttribute>().Any())
            return;

        // Look for the ticket in the header
        var header = ctx.HttpContext.Request.Headers["Authorization"].ToString();
        var token = header.StartsWith("Bearer ") ? header["Bearer ".Length..].Trim() : null;

        // If there's no ticket, block them.
        if (string.IsNullOrWhiteSpace(token))
        {
            ctx.Result = new UnauthorizedObjectResult(new { message = "No session token provided." });
            return;
        }

        // Check if the ticket is in the database and hasn't expired
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Token == token && s.ExpiresAt > DateTime.UtcNow);

        if (session is null)
        {
            ctx.Result = new UnauthorizedObjectResult(new { message = "Invalid or expired session." });
            return;
        }

        // Save their ID and Role so the controllers can use it
        ctx.HttpContext.Items["UserId"] = session.UserId;
        ctx.HttpContext.Items["Role"] = session.Role;

        // Check if this endpoint requires a specific role
        var required = endpoint.OfType<RequireRoleAttribute>().FirstOrDefault();
        if (required is not null && session.Role != required.Role)
        {
            ctx.Result = new ForbidResult(); // 403 Forbidden
        }
    }
}
