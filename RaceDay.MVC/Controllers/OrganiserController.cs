using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class OrganiserController : Controller
    {
        private readonly ApiService _apiService;

        public OrganiserController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("JWToken") == null || HttpContext.Session.GetString("UserRole") != "Organiser")
                return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var response = await client.GetAsync("/api/events");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(events);
            }

            return View(new List<JsonElement>());
        }

        [HttpGet]
        public IActionResult CreateEvent()
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvent(string eventName, string description, DateTime eventDate, TimeSpan startTime, string venue, string location, IFormFile bannerFile)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            // 1. Create the Event
            var newEvent = new { EventName = eventName, Description = description, EventDate = eventDate, StartTime = startTime, Venue = venue, Location = location, Status = "Upcoming" };
            var content = new StringContent(JsonSerializer.Serialize(newEvent), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/events", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createdEvent = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var newEventId = createdEvent.GetProperty("eventId").GetInt32();

                // 2. Upload the Banner Image if one was selected
                if (bannerFile != null && bannerFile.Length > 0)
                {
                    using var formData = new MultipartFormDataContent();
                    var streamContent = new StreamContent(bannerFile.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(bannerFile.ContentType);
                    formData.Add(streamContent, "file", bannerFile.FileName);

                    await client.PostAsync($"/api/events/{newEventId}/banner", formData);
                }

                return RedirectToAction("Index");
            }

            ViewBag.Error = "Failed to create event.";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageCategories(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var eventResponse = await client.GetAsync($"/api/events/{id}");
            ViewBag.EventDetails = JsonSerializer.Deserialize<JsonElement>(await eventResponse.Content.ReadAsStringAsync());

            var catResponse = await client.GetAsync($"/api/events/{id}/categories");
            var categories = JsonSerializer.Deserialize<List<JsonElement>>(await catResponse.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.EventId = id;
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(int eventId, string categoryName, decimal distanceKm, int maxParticipants, decimal entryFee)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();
            var newCat = new { CategoryName = categoryName, DistanceKm = distanceKm, MaxParticipants = maxParticipants, EntryFee = entryFee };
            var content = new StringContent(JsonSerializer.Serialize(newCat), Encoding.UTF8, "application/json");

            await client.PostAsync($"/api/events/{eventId}/categories", content);
            return RedirectToAction("ManageCategories", new { id = eventId });
        }

        [HttpGet]
        public async Task<IActionResult> EventEnrolments(int id)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            var eventResponse = await client.GetAsync($"/api/events/{id}");
            ViewBag.EventDetails = JsonSerializer.Deserialize<JsonElement>(await eventResponse.Content.ReadAsStringAsync());
            ViewBag.EventId = id;

            var enrolResponse = await client.GetAsync($"/api/events/{id}/enrolments");

            if (enrolResponse.IsSuccessStatusCode)
            {
                var content = await enrolResponse.Content.ReadAsStringAsync();
                var enrolments = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(enrolments);
            }

            return View(new List<JsonElement>());
        }

        [HttpPost]
        public async Task<IActionResult> RecordResult(int enrolmentId, int eventId, string finishTime, int position)
        {
            if (HttpContext.Session.GetString("UserRole") != "Organiser") return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            // Format time correctly for API (HH:mm:ss)
            TimeSpan time = TimeSpan.Parse(finishTime);

            var resultData = new { FinishTime = time, Position = position, ResultStatus = "Finished" };
            var content = new StringContent(JsonSerializer.Serialize(resultData), Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"/api/enrolments/{enrolmentId}/result", content);

            if (response.IsSuccessStatusCode)
                TempData["Success"] = "Result recorded successfully!";
            else
                TempData["Error"] = "Failed to record result. It might already exist.";

            return RedirectToAction("EventEnrolments", new { id = eventId });
        }
    }
}