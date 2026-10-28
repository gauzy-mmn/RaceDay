using System.Net.Http;
using System.Net.Http.Json;
using RaceDay.Models;

namespace RaceDay.Services
{
    public class RaceDayApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RaceDayApiService(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

    }
}
