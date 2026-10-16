namespace RaceDay.Api.Tests;

public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task GetEvents_Anonymous_Returns200()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var created = await organiser.Client.CreateEventAsync();

        var res = await _factory.CreateClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var events = await res.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.EventId == created.EventId);
    }

    [Fact]
    public async Task GetEventById_Anonymous_ReturnsEvent_AndUnknownIs404()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var created = await organiser.Client.CreateEventAsync("Soweto Marathon");
        var anon = _factory.CreateClient();

        var ok = await anon.GetAsync($"/api/events/{created.EventId}");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Soweto Marathon", (await ok.Content.ReadFromJsonAsync<EventResponse>())!.Name);

        Assert.Equal(HttpStatusCode.NotFound, (await anon.GetAsync("/api/events/999999")).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_Returns401()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/events", TestSupport.NewEventRequest());
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await _factory.CreateUserAsync("Participant");
        var res = await participant.Client.PostAsJsonAsync("/api/events", TestSupport.NewEventRequest());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201AndOwnsEvent()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var res = await organiser.Client.PostAsJsonAsync("/api/events", TestSupport.NewEventRequest());

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var ev = await res.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(organiser.UserId, ev!.OrganiserId);
        Assert.Equal("Run", ev.EventType);
    }

    [Theory]
    [InlineData("Swim")]
    [InlineData("")]
    public async Task CreateEvent_InvalidEventType_Returns400(string eventType)
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var request = TestSupport.NewEventRequest();
        request.EventType = eventType;

        var res = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_NegativeDistance_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var request = TestSupport.NewEventRequest();
        request.Distance = -5m;

        var res = await organiser.Client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_ByOwner_Returns200()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var update = TestSupport.NewEventRequest("Renamed Event");
        update.EventType = "Cycle";

        var res = await organiser.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", update);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal("Renamed Event", body!.Name);
        Assert.Equal("Cycle", body.EventType);
    }

    [Fact]
    public async Task UpdateEvent_ByDifferentOrganiser_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var res = await other.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", TestSupport.NewEventRequest("Hijacked"));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsParticipantOrAnonymous_IsRejected()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await owner.Client.CreateEventAsync();
        var request = TestSupport.NewEventRequest("Nope");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.Client.PutAsJsonAsync($"/api/events/{ev.EventId}", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PutAsJsonAsync($"/api/events/{ev.EventId}", request)).StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_Unknown_Returns404()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var res = await organiser.Client.PutAsJsonAsync("/api/events/999999", TestSupport.NewEventRequest());
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByOwner_Returns204ThenEventIsGone()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        await organiser.Client.CreateCategoryAsync(ev.EventId);

        var res = await organiser.Client.DeleteAsync($"/api/events/{ev.EventId}");

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await organiser.Client.GetAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ByOtherOrganiser_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsParticipant_Returns403_AndAnonymous401()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await owner.Client.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.Client.DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().DeleteAsync($"/api/events/{ev.EventId}")).StatusCode);
    }
}