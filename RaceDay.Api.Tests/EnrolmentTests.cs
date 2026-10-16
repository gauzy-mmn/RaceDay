namespace RaceDay.Api.Tests;

public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndIsRecorded()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);

        var res = await participant.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments",
            new EnrolRequest { CategoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var enrolment = await res.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participant.UserId, enrolment!.ParticipantId);
        Assert.Equal(ev.EventId, enrolment.EventId);
        Assert.Equal(cat.CategoryId, enrolment.CategoryId);

        // The participant sees it in their own list...
        var mine = await (await participant.Client.GetAsync("/api/enrolments/me")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(mine!, e => e.EnrolmentId == enrolment.EnrolmentId && e.CategoryId == cat.CategoryId);

        // ...and the organiser sees it against their event.
        var all = await (await organiser.Client.GetAsync($"/api/events/{ev.EventId}/enrolments")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Contains(all!, e => e.EnrolmentId == enrolment.EnrolmentId && e.ParticipantId == participant.UserId);
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        await participant.Client.EnrolAsync(ev.EventId, cat.CategoryId);

        var res = await participant.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest { CategoryId = cat.CategoryId });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403_AndAnonymous401()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        var request = new EnrolRequest { CategoryId = cat.CategoryId };

        Assert.Equal(HttpStatusCode.Forbidden,
            (await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", request)).StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var eventA = await organiser.Client.CreateEventAsync("Event A");
        var eventB = await organiser.Client.CreateEventAsync("Event B");
        var catB = await organiser.Client.CreateCategoryAsync(eventB.EventId);

        var res = await participant.Client.PostAsJsonAsync($"/api/events/{eventA.EventId}/enrolments", new EnrolRequest { CategoryId = catB.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownEvent_Returns404()
    {
        var participant = await _factory.CreateUserAsync("Participant");
        var res = await participant.Client.PostAsJsonAsync("/api/events/999999/enrolments", new EnrolRequest { CategoryId = 1 });
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns400()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();

        var res = await participant.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/enrolments", new EnrolRequest());

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_OnlyForParticipants_AndOnlyOwnRows()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var alice = await _factory.CreateUserAsync("Participant");
        var bob = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        await alice.Client.EnrolAsync(ev.EventId, cat.CategoryId);

        var bobsList = await (await bob.Client.GetAsync("/api/enrolments/me")).Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Empty(bobsList!);

        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.Client.GetAsync("/api/enrolments/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/enrolments/me")).StatusCode);
    }

    [Fact]
    public async Task EventEnrolments_ParticipantAndOtherOrganiserAreRejected()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await owner.Client.CreateEventAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.Client.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/events/{ev.EventId}/enrolments")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.Client.GetAsync("/api/events/999999/enrolments")).StatusCode);
    }

    [Fact]
    public async Task Withdraw_ByOwner_Returns204_ByAnotherParticipant403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var alice = await _factory.CreateUserAsync("Participant");
        var bob = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        var enrolment = await alice.Client.EnrolAsync(ev.EventId, cat.CategoryId);

        Assert.Equal(HttpStatusCode.Forbidden, (await bob.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await alice.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await alice.Client.DeleteAsync($"/api/enrolments/{enrolment.EnrolmentId}")).StatusCode);
    }
}