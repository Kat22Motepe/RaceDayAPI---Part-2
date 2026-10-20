using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Dtos;
using RaceDay.API.Services;
using RaceDay.Domain;
using RaceDay.Infrastructure;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db) => _db = db;

    // POST /api/auth/register
    [HttpPost("auth/register")]
    [AllowAnonymous] // Anyone can register
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        // 1. Check if email is already taken
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email already registered." });

        // 2. Scramble the password using our PasswordHasher
        var (hash, salt) = PasswordHasher.Hash(dto.Password);

        // 3. Save the new user
        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = dto.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 4. Send back the safe data (no passwords!)
        var response = new UserDto(user.Id, user.FirstName, user.LastName, user.Email,
                                   user.Role, user.PhoneNumber, user.DateOfBirth, user.ProfilePictureUrl);

        return CreatedAtAction(nameof(GetMe), new { id = user.Id }, response);
    }

    // POST /api/auth/login
    [HttpPost("auth/login")]
    [AllowAnonymous] // Anyone can login
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // 1. Check if user exists AND if the password matches the hash
        if (user is null || !PasswordHasher.Verify(dto.Password, user.PasswordHash, user.PasswordSalt))
            return Unauthorized(new { message = "Invalid email or password." });

        // 2. Create a new "ticket" (Session)
        var session = new Session
        {
            Token = Guid.NewGuid().ToString("N"), // Random 32-character string
            UserId = user.Id,
            Role = user.Role,
            ExpiresAt = DateTime.UtcNow.AddHours(8), // Valid for 8 hours
            CreatedAt = DateTime.UtcNow
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        // 3. Send the token back to the user
        return Ok(new LoginResponse(session.Token, user.Role.ToString(), user.Id, user.FirstName));
    }

    // POST /api/auth/logout
    [HttpPost("auth/logout")]
    public async Task<IActionResult> Logout()
    {
        // Get the User ID from the Session Filter we built in Step 7
        var userId = (int)HttpContext.Items["UserId"]!;

        // Delete all sessions for this user so they are logged out everywhere
        var sessions = await _db.Sessions.Where(s => s.UserId == userId).ToListAsync();
        _db.Sessions.RemoveRange(sessions);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // GET /api/users/me
    [HttpGet("users/me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var user = await _db.Users.FindAsync(userId);

        if (user is null) return NotFound();

        return Ok(new UserDto(user.Id, user.FirstName, user.LastName, user.Email,
                              user.Role, user.PhoneNumber, user.DateOfBirth, user.ProfilePictureUrl));
    }

    // PUT /api/users/me
    [HttpPut("users/me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileDto dto)
    {
        var userId = (int)HttpContext.Items["UserId"]!;
        var user = await _db.Users.FindAsync(userId);

        if (user is null) return NotFound();

        // Update their details
        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.PhoneNumber = dto.PhoneNumber;
        user.DateOfBirth = dto.DateOfBirth;
        user.ProfilePictureUrl = dto.ProfilePictureUrl;

        await _db.SaveChangesAsync();

        return Ok(new UserDto(user.Id, user.FirstName, user.LastName, user.Email,
                              user.Role, user.PhoneNumber, user.DateOfBirth, user.ProfilePictureUrl));
    }
}
