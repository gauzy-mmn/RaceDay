namespace RaceDay.Api.Tests;

public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;
    public AuthTests(RaceDayFactory factory) => _factory = factory;

    private static RegisterRequest NewRegistration(string role = "Participant") => new()
    {
        FirstName = "Thandi",
        LastName = "Nkosi",
        Email = $"{Guid.NewGuid():N}@raceday.test",
        Password = TestSupport.Password,
        Role = role
    };

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_ValidRequest_Returns201WithRole(string role)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", NewRegistration(role));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(role, body!.Role);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task Register_PasswordIsStoredHashedNotPlain()
    {
        var user = await _factory.CreateUserAsync("Participant", login: false);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var stored = db.Users.Single(u => u.Email == user.Email);

        Assert.NotEqual(TestSupport.Password, stored.PasswordHash);
        Assert.DoesNotContain(TestSupport.Password, stored.PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var res = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_Returns400()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", NewRegistration("Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var request = NewRegistration();
        request.Password = "short";
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var request = NewRegistration();
        request.Email = "not-an-email";
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithRole()
    {
        var user = await _factory.CreateUserAsync("Organiser", login: false);
        var res = await user.Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = TestSupport.Password });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Organiser", (await res.Content.ReadFromJsonAsync<AuthResponse>())!.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var user = await _factory.CreateUserAsync("Participant", login: false);
        var res = await user.Client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = user.Email, Password = "WrongPassword1" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var res = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@raceday.test", Password = TestSupport.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Session_PersistsAcrossRequests_AndLogoutEndsIt()
    {
        var user = await _factory.CreateUserAsync("Participant");

        var before = await user.Client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        var logout = await user.Client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var after = await user.Client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_Returns401()
    {
        var res = await _factory.CreateClient().PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Profile_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/users/me", new UpdateProfileRequest { FirstName = "A", LastName = "B" })).StatusCode);
    }

    [Fact]
    public async Task Profile_CanBeViewedAndUpdated_ByEitherRole()
    {
        foreach (var role in AppRoles.All)
        {
            var user = await _factory.CreateUserAsync(role);

            var update = await user.Client.PutAsJsonAsync("/api/users/me",
                new UpdateProfileRequest { FirstName = "Updated", LastName = "Name", PhoneNumber = "0821234567" });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);

            var me = await (await user.Client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<UserResponse>();
            Assert.Equal("Updated", me!.FirstName);
            Assert.Equal(role, me.Role);
            Assert.Equal(user.Email, me.Email);
        }
    }
}