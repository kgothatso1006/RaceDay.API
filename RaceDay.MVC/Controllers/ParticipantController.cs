using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Services;
using System.Text;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class ParticipantController : Controller
    {
        private readonly ApiService _apiService;

        public ParticipantController(ApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("JWToken") == null)
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

        // GET: Participant/EventDetails/5
        [HttpGet]
        public async Task<IActionResult> EventDetails(int id)
        {
            if (HttpContext.Session.GetString("JWToken") == null)
                return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            // Get event details
            var eventResponse = await client.GetAsync($"/api/events/{id}");
            var eventContent = await eventResponse.Content.ReadAsStringAsync();
            ViewBag.EventDetails = JsonSerializer.Deserialize<JsonElement>(eventContent);

            // Get categories for this event
            var catResponse = await client.GetAsync($"/api/events/{id}/categories");
            var catContent = await catResponse.Content.ReadAsStringAsync();
            var categories = JsonSerializer.Deserialize<List<JsonElement>>(catContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            ViewBag.EventId = id;
            return View(categories);
        }

        // POST: Participant/Enrol
        [HttpPost]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            if (HttpContext.Session.GetString("JWToken") == null)
                return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            // Send empty object for race number (API auto-generates it)
            var json = JsonSerializer.Serialize(new { });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"/api/categories/{categoryId}/enrol", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "You have successfully enrolled!";
            }
            else
            {
                TempData["Error"] = "Failed to enrol. You might already be enrolled or the category is full.";
            }

            return RedirectToAction("EventDetails", new { id = eventId });
        } // <-- This closing brace ends the Enrol method

        // GET: Participant/MyResults
        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            if (HttpContext.Session.GetString("JWToken") == null)
                return RedirectToAction("Login", "Auth");

            var client = _apiService.GetClient();

            // Fetch results from the API (the API automatically filters by the logged-in user)
            var response = await client.GetAsync("/api/results");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return View(results);
            }

            return View(new List<JsonElement>());
        }
    }
}