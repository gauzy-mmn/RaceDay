using System.Net.Http;


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
    }
}
