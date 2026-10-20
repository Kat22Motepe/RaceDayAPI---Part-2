using System.Net.Http.Json;
using RaceDay.API.Dtos;
using RaceDay.Domain;

namespace RaceDay.API.Tests;

public static class Helpers
{
    // Registers a user and logs them in, returning their token
    public static async Task<LoginResponse> RegisterAndLogin(
        HttpClient client, string email, string password, UserRole role)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto(
            "Test", "User", email, password, role));

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, password));

        return (await res.Content.ReadFromJsonAsync<LoginResponse>())!;
    }

    // Attaches the token to the request headers
    public static void UseToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
}
