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


        public async Task<List<EnrolmentViewModel>> GetMyEnrolmentsAsync()
        {
            // Get the current Participant's API authentication cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to view your enrolments.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare authenticated GET request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/enrolments/me");

            // Attach the user's API cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send request to RaceDay API
            using var response = await client.SendAsync(request);

            // Check whether the API request succeeded
            response.EnsureSuccessStatusCode();

            // Convert JSON response into enrolment objects
            var enrolments = await response.Content
                .ReadFromJsonAsync<List<EnrolmentViewModel>>();

            return enrolments ?? new List<EnrolmentViewModel>();
        }

        public async Task<List<RaceResultViewModel>>
            GetEventResultsAsync(int eventId)
        {
            // Validate the event ID
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.",
                    nameof(eventId));
            }

            // Retrieve the authenticated Organiser's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to view race results.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated GET request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/results");

            // Attach the Organiser's API cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send request to RaceDay API
            using var response = await client.SendAsync(request);

            // Check for unsuccessful responses
            response.EnsureSuccessStatusCode();

            // Convert JSON into RaceResultViewModel objects
            var results = await response.Content
                .ReadFromJsonAsync<List<RaceResultViewModel>>();

            // Return results ordered by finishing position
            return (results ?? new List<RaceResultViewModel>())
                .OrderBy(r => r.FinishPosition)
                .ToList();
        }

        public async Task<HttpResponseMessage> CaptureResultAsync(
            int eventId,
            CaptureResultViewModel model)
        {
            // Validate the event ID
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.",
                    nameof(eventId));
            }

            // Retrieve the Organiser's API authentication cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to capture results.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the request body
            var resultData = new
            {
                enrolmentId = model.EnrolmentId,
                finishTime = model.FinishTime,
                finishPosition = model.FinishPosition
            };

            // Create authenticated POST request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/results");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            request.Content = JsonContent.Create(resultData);

            // Send the result to the API
            return await client.SendAsync(request);
        }

        public async Task<List<RaceResultViewModel>> GetMyResultsAsync()
        {
            // Retrieve the Participant's API authentication cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to view your results.");
            }

            // Create the API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated GET request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/results/me");

            // Attach the Participant's API cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send the request
            using var response = await client.SendAsync(request);

            // Raise an exception for unsuccessful responses
            response.EnsureSuccessStatusCode();

            // Convert the JSON response into RaceResultViewModel objects
            var results = await response.Content
                .ReadFromJsonAsync<List<RaceResultViewModel>>();

            // Display the most recently captured results first
            return (results ?? new List<RaceResultViewModel>())
                .OrderByDescending(r => r.CapturedAt)
                .ToList();
        }

        public async Task<HttpResponseMessage> PublishEventResultsAsync(
            int eventId)
        {
            // Validate the event ID
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid event ID.",
                    nameof(eventId));
            }

            // Retrieve the logged-in Organiser's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to publish results.");
            }

            // Create our existing API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated publishing request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/results/publish");

            // Attach the Organiser's API authentication cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Send the request to RaceDay API
            // The controller will inspect and dispose the response
            return await client.SendAsync(request);
        }


        public async Task<HttpResponseMessage> UpdateMyProfileAsync(
            UpdateProfileViewModel model)
        {
            // Retrieve the logged-in user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to update your profile.");
            }

            // Create our existing API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the profile update data
            var profileData = new
            {
                firstName = model.FirstName,
                lastName = model.LastName,
                phoneNumber = model.PhoneNumber
            };

            // Create the authenticated PUT request
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "api/users/me");

            // Attach the user's authentication cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Add the JSON request body
            request.Content = JsonContent.Create(profileData);

            // Send request to RaceDay API
            // The calling controller will dispose the response
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> UploadMyProfilePictureAsync(
            IFormFile file)
        {
            // Check that an image was selected
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "Please select a profile picture.");
            }

            // Retrieve the logged-in user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to upload a picture.");
            }

            // Create our existing API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated POST request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/users/me/profile-picture");

            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Create multipart/form-data content
            using var formData = new MultipartFormDataContent();

            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    file.ContentType);

            // The field name "file" matches the API parameter
            formData.Add(
                fileContent,
                "file",
                Path.GetFileName(file.FileName));

            request.Content = formData;

            // Send the image to RaceDay API
            // The controller will inspect and dispose the response
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage>
            GetMyProfilePictureAsync()
        {
            // Retrieve the authenticated user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to view your profile picture.");
            }

            // Create the existing RaceDay API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the authenticated GET request
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/users/me/profile-picture");

            // Attach the user's API authentication cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Retrieve the image from RaceDay API
            // The calling controller must dispose the response
            return await client.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetEventBannerAsync(
            int eventId)
        {
            if (eventId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eventId),
                    "A valid event ID is required.");
            }

            // Create our existing RaceDay API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Retrieve the event's banner from the API
            var response = await client.GetAsync(
                $"api/events/{eventId}/banner");

            // The calling controller must dispose this response
            return response;
        }

        public async Task<HttpResponseMessage> UploadEventBannerAsync(
            int eventId,
            IFormFile file)
        {
            if (eventId <= 0)
            {
                throw new ArgumentException(
                    "A valid event ID is required.");
            }

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "Please select an event banner.");
            }

            // Retrieve the logged-in user's API cookie
            var cookie = _httpContextAccessor
                .HttpContext?
                .Session.GetString("ApiAuthCookie");

            if (string.IsNullOrWhiteSpace(cookie))
            {
                throw new InvalidOperationException(
                    "You must be logged in to upload an event banner.");
            }

            // Create our existing API client
            var client = _httpClientFactory
                .CreateClient("RaceDayApi");

            // Prepare the upload request
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/banner");

            // Attach the authentication cookie
            request.Headers.TryAddWithoutValidation(
                "Cookie", cookie);

            // Create multipart form data
            using var formData = new MultipartFormDataContent();

            using var stream = file.OpenReadStream();

            using var fileContent = new StreamContent(stream);

            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    file.ContentType);

            formData.Add(
                fileContent,
                "file",
                Path.GetFileName(file.FileName));

            request.Content = formData;

            // Send the image to RaceDay API
            // The calling controller must dispose the response
            return await client.SendAsync(request);
        }


    }
}
