using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using RaceDay.Domain;

namespace RaceDay.API.Tests;

public class RoleTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    public RoleTests(ApiTestFactory f) => _client = f.CreateClient();

    private static readonly CreateEventDto SampleEvent =
        new("Test Race", "Desc", DateTime.UtcNow.AddDays(30), "JHB", 10, EventType.Run, null);

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
        var login = await Helpers.RegisterAndLogin(_client,
            "p_create@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        _client.UseToken(login.Token);
        var res = await _client.PostAsJsonAsync("/api/events", SampleEvent);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCreateEvent_ReturnsCreated()
    {
        var login = await Helpers.RegisterAndLogin(_client,
            "o_create@test.co.za", "Pa$$w0rd!", UserRole.Organiser);
        _client.UseToken(login.Token);
        var res = await _client.PostAsJsonAsync("/api/events", SampleEvent);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotAccessEvents_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_ReturnsForbidden()
    {
        var login = await Helpers.RegisterAndLogin(_client,
            "o_enrol@test.co.za", "Pa$$w0rd!", UserRole.Organiser);
        _client.UseToken(login.Token);
        var res = await _client.PostAsJsonAsync("/api/events/1/enrolments",
            new CreateEnrolmentDto(1));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
