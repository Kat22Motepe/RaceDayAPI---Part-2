using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using RaceDay.Domain;

namespace RaceDay.API.Tests;

public class AuthTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    public AuthTests(ApiTestFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task Register_ValidParticipant_ReturnsCreated()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("A", "B", "p1@test.co.za", "Pa$$w0rd!", UserRole.Participant));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var dto = new RegisterDto("A", "B", "dup@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        await _client.PostAsJsonAsync("/api/auth/register", dto);
        var res = await _client.PostAsJsonAsync("/api/auth/register", dto);
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Login_Correct_ReturnsToken()
    {
        var login = await Helpers.RegisterAndLogin(_client,
            "login@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.Equal("Participant", login.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await Helpers.RegisterAndLogin(_client, "wrong@test.co.za", "correct", UserRole.Participant);
        var res = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto("wrong@test.co.za", "incorrect"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
