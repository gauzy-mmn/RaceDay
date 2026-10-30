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

        public async Task<HttpResponseMessage> CreateEventAsync(
            EventViewModel model)
        {
            // Retrieve the logged-in user's API session
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to create an event.");
            }

            // Create an HTTP client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the event information
            var eventData = new
            {
                name = model.Name,
                description = model.Description,
                eventDate = model.EventDate,
                location = model.Location,
                distance = model.Distance,
                eventType = model.EventType
            };

            // Create an authenticated POST request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/events");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            request.Content = JsonContent.Create(eventData);

            // Send the event to the API
            return await client.SendAsync(request);
        }

        public async Task<EventViewModel?> GetEventByIdAsync(int id)
        {
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            using var response = await client.GetAsync(
                $"api/events/{id}");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<EventViewModel>();
        }

        public async Task<HttpResponseMessage> UpdateEventAsync(
            int id,
            EventViewModel model)
        {
            // Retrieve the current user's API authentication
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to update an event.");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare updated event details
            var eventData = new
            {
                name = model.Name,
                description = model.Description,
                eventDate = model.EventDate,
                location = model.Location,
                distance = model.Distance,
                eventType = model.EventType
            };

            // Send authenticated PUT request
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/events/{id}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            request.Content = JsonContent.Create(eventData);

            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteEventAsync(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.", nameof(id));
            }

            // Retrieve the current user's API session
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to delete an event.");
            }

            // Create the HTTP client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare authenticated DELETE request
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/events/{id}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send request to RaceDay API
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> CreateCategoryAsync(
            int eventId,
            CategoryViewModel model)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.", nameof(eventId));
            }

            // Retrieve authenticated user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to create a category.");
            }

            // Create API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare category information
            var categoryData = new
            {
                name = model.Name,
                description = model.Description
            };

            // Create authenticated API request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/categories");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            request.Content = JsonContent.Create(categoryData);

            // Send request to RaceDay API
            return await client.SendAsync(request);
        }

        public async Task<List<CategoryViewModel>>
            GetEventCategoriesAsync(int eventId)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.", nameof(eventId));
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Retrieve categories for the selected event
            using var response = await client.GetAsync(
                $"api/events/{eventId}/categories");

            // Check whether the request succeeded
            response.EnsureSuccessStatusCode();

            // Convert JSON into category objects
            var categories = await response.Content
                .ReadFromJsonAsync<List<CategoryViewModel>>();

            return categories ?? new List<CategoryViewModel>();
        }

        public async Task<HttpResponseMessage> UpdateCategoryAsync(
            int categoryId,
            CategoryViewModel model)
        {
            if (categoryId <= 0)
            {
                throw new ArgumentException(
                    "Invalid category ID.", nameof(categoryId));
            }

            // Retrieve the logged-in user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to update a category.");
            }

            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // The API expects only name and description
            var categoryData = new
            {
                name = model.Name,
                description = model.Description
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/categories/{categoryId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            request.Content = JsonContent.Create(categoryData);

            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteCategoryAsync(
            int categoryId)
        {
            // Validate the category ID
            if (categoryId <= 0)
            {
                throw new ArgumentException(
                    "Invalid category ID.",
                    nameof(categoryId));
            }

            // Retrieve the current user's API session
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to delete a category.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the DELETE request
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/categories/{categoryId}");

            // Attach the authenticated user's API cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send the request to RaceDay API
            return await client.SendAsync(request);
        }

        public async Task<List<EnrolmentViewModel>>
            GetEventEnrolmentsAsync(int eventId)
        {
            // Validate the event ID
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.",
                    nameof(eventId));
            }

            // Retrieve the current user's API session
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to view enrolments.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated GET request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/enrolments");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send the request to RaceDay API
            using var response = await client.SendAsync(request);

            // Reject unsuccessful API responses
            response.EnsureSuccessStatusCode();

            // Convert JSON response into enrolment objects
            var enrolments = await response.Content
                .ReadFromJsonAsync<List<EnrolmentViewModel>>();

            return enrolments ?? new List<EnrolmentViewModel>();
        }

        public async Task<HttpResponseMessage> EnrolEventAsync(
            int eventId,
            int categoryId)
        {
            // Validate the supplied IDs
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.",
                    nameof(eventId));
            }

            if (categoryId <= 0)
            {
                throw new ArgumentException(
                    "Invalid category ID.",
                    nameof(categoryId));
            }

            // Retrieve the logged-in Participant's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to enrol in an event.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the enrolment request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/enrolments");

            // Attach the authenticated Participant's API cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send the selected category
            request.Content = JsonContent.Create(new
            {
                categoryId = categoryId
            });

            // Send request to RaceDay API
            return await client.SendAsync(request);
        }



    }
}
