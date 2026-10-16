using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace RaceDay.Api.Tests;

/// <summary>
/// Boots the real API in memory. SQL Server is swapped for the EF Core in-memory provider so the tests
/// run anywhere (including GitHub Actions) while still exercising routing, validation, cookies and authorisation.
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<RaceDayDbContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated(); // also seeds roles
        return host;
    }
}

public record TestUser(HttpClient Client, int UserId, string Email);

public static class TestSupport
{
    public const string Password = "Passw0rd!123";

    /// <summary>Registers a fresh user and (optionally) logs them in on their own cookie-keeping client.</summary>
    public static async Task<TestUser> CreateUserAsync(this RaceDayFactory factory, string role, bool login = true)
    {
        var client = factory.CreateClient();
        var email = $"{Guid.NewGuid():N}@raceday.test";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FirstName = "Test",
            LastName = role,
            Email = email,
            Password = Password,
            Role = role
        });
        reg.EnsureSuccessStatusCode();
        var body = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!;

        if (login)
        {
            var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = Password });
            res.EnsureSuccessStatusCode();
        }
        return new TestUser(client, body.UserId, email);
    }

    public static EventRequest NewEventRequest(string name = "Joburg 10km") => new()
    {
        Name = name,
        Description = "Test event",
        EventDate = DateTime.UtcNow.AddMonths(2),
        Location = "Johannesburg",
        Distance = 10m,
        EventType = "Run"
    };

    public static async Task<EventResponse> CreateEventAsync(this HttpClient organiser, string name = "Joburg 10km")
    {
        var res = await organiser.PostAsJsonAsync("/api/events", NewEventRequest(name));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<EventResponse>())!;
    }

    public static async Task<CategoryResponse> CreateCategoryAsync(this HttpClient organiser, int eventId, string name = "Senior")
    {
        var res = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryRequest { Name = name });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CategoryResponse>())!;
    }

    public static async Task<EnrolmentResponse> EnrolAsync(this HttpClient participant, int eventId, int categoryId)
    {
        var res = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new EnrolRequest { CategoryId = categoryId });
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
    }
}