namespace RaceDay.Api.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private record Setup(TestUser Organiser, TestUser Participant, EventResponse Event, EnrolmentResponse Enrolment);

    private async Task<Setup> ArrangeAsync()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        var enrolment = await participant.Client.EnrolAsync(ev.EventId, cat.CategoryId);
        return new Setup(organiser, participant, ev, enrolment);
    }

    private static CreateResultRequest NewResult(int enrolmentId, int position = 1) => new()
    {
        EnrolmentId = enrolmentId,
        FinishTime = TimeSpan.FromMinutes(45) + TimeSpan.FromSeconds(30),
        FinishPosition = position
    };

    [Fact]
    public async Task CaptureResult_ByOwningOrganiser_Returns201()
    {
        var s = await ArrangeAsync();

        var res = await s.Organiser.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(TimeSpan.FromSeconds(45 * 60 + 30), body!.FinishTime);
        Assert.Equal(1, body.FinishPosition);
        Assert.Equal(s.Participant.UserId, body.ParticipantId);
    }

    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403_AndAnonymous401()
    {
        var s = await ArrangeAsync();
        var request = NewResult(s.Enrolment.EnrolmentId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await s.Participant.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", request)).StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ByOtherOrganiser_Returns403()
    {
        var s = await ArrangeAsync();
        var other = await _factory.CreateUserAsync("Organiser");

        var res = await other.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_EnrolmentFromAnotherEvent_Returns400()
    {
        var s = await ArrangeAsync();
        var otherEvent = await s.Organiser.Client.CreateEventAsync("Second Event");

        var res = await s.Organiser.Client.PostAsJsonAsync($"/api/events/{otherEvent.EventId}/results", NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task CaptureResult_InvalidValues_Returns400(int seconds, int position)
    {
        var s = await ArrangeAsync();
        var request = new CreateResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = TimeSpan.FromSeconds(seconds),
            FinishPosition = position
        };

        var res = await s.Organiser.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var s = await ArrangeAsync();
        var url = $"/api/events/{s.Event.EventId}/results";
        await s.Organiser.Client.PostAsJsonAsync(url, NewResult(s.Enrolment.EnrolmentId));

        var res = await s.Organiser.Client.PostAsJsonAsync(url, NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task UpdateResult_ByOwner_Returns200_ByOthersRejected()
    {
        var s = await ArrangeAsync();
        var created = await (await s.Organiser.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results",
            NewResult(s.Enrolment.EnrolmentId))).Content.ReadFromJsonAsync<ResultResponse>();
        var correction = new UpdateResultRequest { FinishTime = TimeSpan.FromMinutes(40), FinishPosition = 2 };
        var other = await _factory.CreateUserAsync("Organiser");

        var ok = await s.Organiser.Client.PutAsJsonAsync($"/api/results/{created!.ResultId}", correction);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var updated = await ok.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(2, updated!.FinishPosition);
        Assert.Equal(TimeSpan.FromMinutes(40), updated.FinishTime);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.PutAsJsonAsync($"/api/results/{created.ResultId}", correction)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await s.Participant.Client.PutAsJsonAsync($"/api/results/{created.ResultId}", correction)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await s.Organiser.Client.PutAsJsonAsync("/api/results/999999", correction)).StatusCode);
    }

    [Fact]
    public async Task MyResults_ParticipantSeesOnlyOwn_OrganiserRejected()
    {
        var s = await ArrangeAsync();
        var stranger = await _factory.CreateUserAsync("Participant");
        await s.Organiser.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", NewResult(s.Enrolment.EnrolmentId));

        var mine = await (await s.Participant.Client.GetAsync("/api/results/me")).Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Single(mine!);
        Assert.Equal(s.Participant.UserId, mine![0].ParticipantId);

        var theirs = await (await stranger.Client.GetAsync("/api/results/me")).Content.ReadFromJsonAsync<List<ResultResponse>>();
        Assert.Empty(theirs!);

        Assert.Equal(HttpStatusCode.Forbidden, (await s.Organiser.Client.GetAsync("/api/results/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync("/api/results/me")).StatusCode);
    }

    [Fact]
    public async Task EventResults_OwningOrganiserOnly()
    {
        var s = await ArrangeAsync();
        var other = await _factory.CreateUserAsync("Organiser");
        await s.Organiser.Client.PostAsJsonAsync($"/api/events/{s.Event.EventId}/results", NewResult(s.Enrolment.EnrolmentId));
        var url = $"/api/events/{s.Event.EventId}/results";

        var ok = await s.Organiser.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Single((await ok.Content.ReadFromJsonAsync<List<ResultResponse>>())!);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await s.Participant.Client.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await s.Organiser.Client.GetAsync("/api/events/999999/results")).StatusCode);
    }
}
