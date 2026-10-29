using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Models;

namespace RaceDay.Services
{
    public class RaceDayApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RaceDayApiService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // Retrieve events from the existing RaceDay API
        public async Task<string> GetEventsAsync()
        {
            using var client =
                _httpClientFactory.CreateClient("RaceDayApi");

            return await client.GetStringAsync("api/events");
        }

        public async Task<HttpResponseMessage> LoginAsync(
            LoginViewModel model)
        {
            var client = _httpClientFactory.CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/auth/login",
                new
                {
                    email = model.Email,
                    password = model.Password
                });

            return response;
        }

        public async Task<HttpResponseMessage> GetMyProfileAsync()
        {
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "API authentication session is missing.");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/users/me");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> LogoutAsync()
        {
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "API authentication session is missing.");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/auth/logout");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            return await client.SendAsync(request);
        }


        public async Task<HttpResponseMessage> RegisterAsync(
            RegisterViewModel model)
        {
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            var response = await client.PostAsJsonAsync(
                "api/auth/register",
                new
                {
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    email = model.Email,
                    phoneNumber = model.PhoneNumber,
                    password = model.Password,
                    role = model.Role
                });

            return response;
        }

        public async Task<List<EventViewModel>> GetEventListAsync()
        {
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            var events = await client.GetFromJsonAsync<List<EventViewModel>>(
                "api/events");

            return events ?? new List<EventViewModel>();
        }


    }
}
