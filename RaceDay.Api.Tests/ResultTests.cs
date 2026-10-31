
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private record Setup(
        TestUser Organiser,
        TestUser Participant,
        EventResponse Event,
        EnrolmentResponse Enrolment);

    // Create an Organiser, Participant, event,
    // category and enrolment for each test.
    private async Task<Setup> ArrangeAsync()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");

        var participant = await _factory.CreateUserAsync("Participant");

        var ev = await organiser.Client.CreateEventAsync();

        var category = await organiser.Client
            .CreateCategoryAsync(ev.EventId);

        var enrolment = await participant.Client
            .EnrolAsync(ev.EventId, category.CategoryId);

        return new Setup(
            organiser,
            participant,
            ev,
            enrolment);
    }

    private static CreateResultRequest NewResult(
        int enrolmentId,
        int position = 1) => new()
        {
            EnrolmentId = enrolmentId,
            FinishTime = TimeSpan.FromMinutes(45)
            + TimeSpan.FromSeconds(30),
            FinishPosition = position
        };

    // TEST 1: Owning Organiser can capture results
    [Fact]
    public async Task CaptureResult_ByOwningOrganiser_Returns201()
    {
        var s = await ArrangeAsync();

        var res = await s.Organiser.Client.PostAsJsonAsync(
            $"/api/events/{s.Event.EventId}/results",
            NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var body = await res.Content
            .ReadFromJsonAsync<ResultResponse>();

        Assert.NotNull(body);

        Assert.Equal(
            TimeSpan.FromSeconds(45 * 60 + 30),
            body.FinishTime);

        Assert.Equal(1, body.FinishPosition);

        Assert.Equal(
            s.Participant.UserId,
            body.ParticipantId);
    }

    // TEST 2: Participant and anonymous users cannot capture results
    [Fact]
    public async Task CaptureResult_AsParticipant_Returns403_AndAnonymous401()
    {
        var s = await ArrangeAsync();

        var request = NewResult(s.Enrolment.EnrolmentId);

        var participantResponse =
            await s.Participant.Client.PostAsJsonAsync(
                $"/api/events/{s.Event.EventId}/results",
                request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            participantResponse.StatusCode);

        var anonymousResponse =
            await _factory.CreateClient().PostAsJsonAsync(
                $"/api/events/{s.Event.EventId}/results",
                request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);
    }

    // TEST 3: Another Organiser cannot capture results
    [Fact]
    public async Task CaptureResult_ByOtherOrganiser_Returns403()
    {
        var s = await ArrangeAsync();

        var other = await _factory.CreateUserAsync("Organiser");

        var res = await other.Client.PostAsJsonAsync(
            $"/api/events/{s.Event.EventId}/results",
            NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(
            HttpStatusCode.Forbidden,
            res.StatusCode);
    }

    // TEST 4: Enrolment must belong to the correct event
    [Fact]
    public async Task CaptureResult_EnrolmentFromAnotherEvent_Returns400()
    {
        var s = await ArrangeAsync();

        var otherEvent = await s.Organiser.Client
            .CreateEventAsync("Second Event");

        var res = await s.Organiser.Client.PostAsJsonAsync(
            $"/api/events/{otherEvent.EventId}/results",
            NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(
            HttpStatusCode.BadRequest,
            res.StatusCode);
    }

    // TEST 5: Invalid result values are rejected
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task CaptureResult_InvalidValues_Returns400(
        int seconds,
        int position)
    {
        var s = await ArrangeAsync();

        var request = new CreateResultRequest
        {
            EnrolmentId = s.Enrolment.EnrolmentId,
            FinishTime = TimeSpan.FromSeconds(seconds),
            FinishPosition = position
        };

        var res = await s.Organiser.Client.PostAsJsonAsync(
            $"/api/events/{s.Event.EventId}/results",
            request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            res.StatusCode);
    }

    // TEST 6: Duplicate results are rejected
    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var s = await ArrangeAsync();

        var url = $"/api/events/{s.Event.EventId}/results";

        await s.Organiser.Client.PostAsJsonAsync(
            url,
            NewResult(s.Enrolment.EnrolmentId));

        var res = await s.Organiser.Client.PostAsJsonAsync(
            url,
            NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(
            HttpStatusCode.Conflict,
            res.StatusCode);
    }

    // TEST 7: Only the owning Organiser can update results
    [Fact]
    public async Task UpdateResult_ByOwner_Returns200_ByOthersRejected()
    {
        var s = await ArrangeAsync();

        var createResponse =
            await s.Organiser.Client.PostAsJsonAsync(
                $"/api/events/{s.Event.EventId}/results",
                NewResult(s.Enrolment.EnrolmentId));

        var created = await createResponse.Content
            .ReadFromJsonAsync<ResultResponse>();

        Assert.NotNull(created);

        var correction = new UpdateResultRequest
        {
            FinishTime = TimeSpan.FromMinutes(40),
            FinishPosition = 2
        };

        var other = await _factory.CreateUserAsync("Organiser");

        var ok = await s.Organiser.Client.PutAsJsonAsync(
            $"/api/results/{created.ResultId}",
            correction);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var updated = await ok.Content
            .ReadFromJsonAsync<ResultResponse>();

        Assert.NotNull(updated);

        Assert.Equal(2, updated.FinishPosition);

        Assert.Equal(
            TimeSpan.FromMinutes(40),
            updated.FinishTime);

        var otherResponse = await other.Client.PutAsJsonAsync(
            $"/api/results/{created.ResultId}",
            correction);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            otherResponse.StatusCode);

        var participantResponse =
            await s.Participant.Client.PutAsJsonAsync(
                $"/api/results/{created.ResultId}",
                correction);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            participantResponse.StatusCode);

        var missingResponse =
            await s.Organiser.Client.PutAsJsonAsync(
                "/api/results/999999",
                correction);

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingResponse.StatusCode);
    }

    // TEST 8: Participants see only their own published results
    [Fact]
    public async Task MyResults_ParticipantSeesOnlyOwn_OrganiserRejected()
    {
        var s = await ArrangeAsync();

        var stranger = await _factory.CreateUserAsync("Participant");

        // STEP 1: Capture a race result
        var captureResponse =
            await s.Organiser.Client.PostAsJsonAsync(
                $"/api/events/{s.Event.EventId}/results",
                NewResult(s.Enrolment.EnrolmentId));

        Assert.Equal(
            HttpStatusCode.Created,
            captureResponse.StatusCode);

        // STEP 2: Set the event date to yesterday.
        // RaceDay only allows publishing after the event.
        // This changes the isolated test event.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            var testEvent = await db.Events.SingleAsync(
                e => e.EventId == s.Event.EventId);

            testEvent.EventDate = DateTime.UtcNow.AddDays(-1);

            await db.SaveChangesAsync();
        }

        // STEP 3: Verify that unpublished results are hidden
        var unpublishedResponse =
            await s.Participant.Client.GetAsync("/api/results/me");

        Assert.Equal(
            HttpStatusCode.OK,
            unpublishedResponse.StatusCode);

        var unpublished = await unpublishedResponse.Content
            .ReadFromJsonAsync<List<ResultResponse>>();

        Assert.NotNull(unpublished);
        Assert.Empty(unpublished);

        // STEP 4: Publish results as the owning Organiser
        var publishResponse =
            await s.Organiser.Client.PostAsync(
                $"/api/events/{s.Event.EventId}/results/publish",
                null);

        var publishBody = await publishResponse.Content
            .ReadAsStringAsync();

        Assert.True(
            publishResponse.StatusCode == HttpStatusCode.OK,
            $"Publishing failed with " +
            $"{(int)publishResponse.StatusCode}: {publishBody}");

        // STEP 5: Participant sees their published result
        var mineResponse =
            await s.Participant.Client.GetAsync("/api/results/me");

        Assert.Equal(
            HttpStatusCode.OK,
            mineResponse.StatusCode);

        var mine = await mineResponse.Content
            .ReadFromJsonAsync<List<ResultResponse>>();

        Assert.NotNull(mine);
        Assert.Single(mine);

        Assert.Equal(
            s.Participant.UserId,
            mine[0].ParticipantId);

        // STEP 6: Another Participant sees no results
        var theirsResponse =
            await stranger.Client.GetAsync("/api/results/me");

        Assert.Equal(
            HttpStatusCode.OK,
            theirsResponse.StatusCode);

        var theirs = await theirsResponse.Content
            .ReadFromJsonAsync<List<ResultResponse>>();

        Assert.NotNull(theirs);
        Assert.Empty(theirs);

        // STEP 7: Organisers cannot access Participant results
        var organiserResponse =
            await s.Organiser.Client.GetAsync("/api/results/me");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            organiserResponse.StatusCode);

        // STEP 8: Anonymous users cannot access results
        var anonymousResponse =
            await _factory.CreateClient()
                .GetAsync("/api/results/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);
    }

    // TEST 9: Only the owning Organiser can view event results
    [Fact]
    public async Task EventResults_OwningOrganiserOnly()
    {
        var s = await ArrangeAsync();

        var other = await _factory.CreateUserAsync("Organiser");

        await s.Organiser.Client.PostAsJsonAsync(
            $"/api/events/{s.Event.EventId}/results",
            NewResult(s.Enrolment.EnrolmentId));

        var url = $"/api/events/{s.Event.EventId}/results";

        // Owning Organiser can access the results
        var ok = await s.Organiser.Client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var results = await ok.Content
            .ReadFromJsonAsync<List<ResultResponse>>();

        Assert.NotNull(results);
        Assert.Single(results);

        // Other Organiser is rejected
        var otherResponse = await other.Client.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            otherResponse.StatusCode);

        // Participant is rejected
        var participantResponse =
            await s.Participant.Client.GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            participantResponse.StatusCode);

        // Anonymous user is rejected
        var anonymousResponse =
            await _factory.CreateClient().GetAsync(url);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            anonymousResponse.StatusCode);

        // Non-existent event returns 404
        var missingResponse =
            await s.Organiser.Client.GetAsync(
                "/api/events/999999/results");

        Assert.Equal(
            HttpStatusCode.NotFound,
            missingResponse.StatusCode);
    }
}
