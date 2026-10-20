using System.Net;
using System.Net.Http.Json;
using RaceDay.API.Dtos;
using RaceDay.Domain;

namespace RaceDay.API.Tests;

public class EnrolmentTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;
    public EnrolmentTests(ApiTestFactory f) => _client = f.CreateClient();

    // Helper to create a fresh event and category for the participant to join
    private async Task<(int eventId, int categoryId)> SetupEventAndCategory()
    {
        var org = await Helpers.RegisterAndLogin(_client,
            $"org_{Guid.NewGuid():N}@test.co.za", "Pa$$w0rd!", UserRole.Organiser);
        _client.UseToken(org.Token);

        var evRes = await _client.PostAsJsonAsync("/api/events",
            new CreateEventDto("Race", "D", DateTime.UtcNow.AddDays(20),
                "PTA", 10, EventType.Run, null));
        var ev = await evRes.Content.ReadFromJsonAsync<EventDto>();

        var catRes = await _client.PostAsJsonAsync($"/api/events/{ev!.Id}/categories",
            new CreateCategoryDto("Open", 18, 99, 10));
        var cat = await catRes.Content.ReadFromJsonAsync<CategoryDto>();

        return (ev.Id, cat!.Id);
    }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var (eventId, categoryId) = await SetupEventAndCategory();

        var p = await Helpers.RegisterAndLogin(_client,
            $"p_{Guid.NewGuid():N}@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        _client.UseToken(p.Token);

        var enrolRes = await _client.PostAsJsonAsync(
            $"/api/events/{eventId}/enrolments",
            new CreateEnrolmentDto(categoryId));
        Assert.Equal(HttpStatusCode.Created, enrolRes.StatusCode);

        var mine = await _client.GetFromJsonAsync<List<EnrolmentDto>>("/api/enrolments/mine");
        Assert.Single(mine!);
        Assert.Equal(eventId, mine![0].EventId);
        Assert.Equal(categoryId, mine[0].CategoryId);
    }

    [Fact]
    public async Task Participant_CannotEnrolTwice_ReturnsConflict()
    {
        var (eventId, categoryId) = await SetupEventAndCategory();

        var p = await Helpers.RegisterAndLogin(_client,
            $"p2_{Guid.NewGuid():N}@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        _client.UseToken(p.Token);

        await _client.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentDto(categoryId));
        var second = await _client.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentDto(categoryId));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithWrongCategory_ReturnsBadRequest()
    {
        var (eventId, _) = await SetupEventAndCategory();
        var (_, otherCategory) = await SetupEventAndCategory();

        var p = await Helpers.RegisterAndLogin(_client,
            $"p3_{Guid.NewGuid():N}@test.co.za", "Pa$$w0rd!", UserRole.Participant);
        _client.UseToken(p.Token);

        var res = await _client.PostAsJsonAsync($"/api/events/{eventId}/enrolments",
            new CreateEnrolmentDto(otherCategory));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
