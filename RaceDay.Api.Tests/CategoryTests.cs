namespace RaceDay.Api.Tests;

public class CategoryTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public CategoryTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task GetCategories_Anonymous_ReturnsEventCategories()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        await organiser.Client.CreateCategoryAsync(ev.EventId, "Under 20");
        await organiser.Client.CreateCategoryAsync(ev.EventId, "Senior");

        var res = await _factory.CreateClient().GetAsync($"/api/events/{ev.EventId}/categories");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(2, (await res.Content.ReadFromJsonAsync<List<CategoryResponse>>())!.Count);
    }

    [Fact]
    public async Task GetCategories_UnknownEvent_Returns404()
    {
        var res = await _factory.CreateClient().GetAsync("/api/events/999999/categories");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ByOwner_Returns201()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();

        var res = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories",
            new CategoryRequest { Name = "10km", Description = "Ten kilometre route" });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal("10km", (await res.Content.ReadFromJsonAsync<CategoryResponse>())!.Name);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_Returns403_AndAnonymous401()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var request = new CategoryRequest { Name = "Sneaky" };

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", request)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _factory.CreateClient().PostAsJsonAsync($"/api/events/{ev.EventId}/categories", request)).StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ByOtherOrganiser_Returns403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();

        var res = await other.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new CategoryRequest { Name = "Nope" });

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateNameInEvent_Returns409()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        await organiser.Client.CreateCategoryAsync(ev.EventId, "Senior");

        var res = await organiser.Client.PostAsJsonAsync($"/api/events/{ev.EventId}/categories", new CategoryRequest { Name = "Senior" });

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ByOwner_Returns200_ByOtherOrganiser403()
    {
        var owner = await _factory.CreateUserAsync("Organiser");
        var other = await _factory.CreateUserAsync("Organiser");
        var ev = await owner.Client.CreateEventAsync();
        var cat = await owner.Client.CreateCategoryAsync(ev.EventId);

        var ok = await owner.Client.PutAsJsonAsync($"/api/categories/{cat.CategoryId}", new CategoryRequest { Name = "Masters" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal("Masters", (await ok.Content.ReadFromJsonAsync<CategoryResponse>())!.Name);

        var denied = await other.Client.PutAsJsonAsync($"/api/categories/{cat.CategoryId}", new CategoryRequest { Name = "Hijack" });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_Unused_Returns204()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);

        Assert.Equal(HttpStatusCode.NoContent, (await organiser.Client.DeleteAsync($"/api/categories/{cat.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);
        await participant.Client.EnrolAsync(ev.EventId, cat.CategoryId);

        Assert.Equal(HttpStatusCode.Conflict, (await organiser.Client.DeleteAsync($"/api/categories/{cat.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsParticipant_Returns403()
    {
        var organiser = await _factory.CreateUserAsync("Organiser");
        var participant = await _factory.CreateUserAsync("Participant");
        var ev = await organiser.Client.CreateEventAsync();
        var cat = await organiser.Client.CreateCategoryAsync(ev.EventId);

        Assert.Equal(HttpStatusCode.Forbidden, (await participant.Client.DeleteAsync($"/api/categories/{cat.CategoryId}")).StatusCode);
    }
}