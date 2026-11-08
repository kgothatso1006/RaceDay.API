using Microsoft.AspNetCore.Mvc;
using RaceDay.MVC.Models;
using System.Text.Json;

namespace RaceDay.MVC.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public AuthController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("RaceDayApi");
            var response = await client.PostAsJsonAsync("/api/auth/login", model);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponseViewModel>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Save the token and role in the Session
                HttpContext.Session.SetString("JWToken", authResponse.Token);
                HttpContext.Session.SetString("UserRole", authResponse.Role);
                HttpContext.Session.SetString("UserName", authResponse.FirstName);

                // Redirect based on role
                if (authResponse.Role == "Organiser")
                    return RedirectToAction("Index", "Organiser");

                return RedirectToAction("Index", "Participant");
            }

            ViewBag.Error = "Invalid email or password";
            return View(model);
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _clientFactory.CreateClient("RaceDayApi");
            var response = await client.PostAsJsonAsync("/api/auth/register", model);

            if (response.IsSuccessStatusCode)
            {
                var loginModel = new LoginViewModel { Email = model.Email, Password = model.Password };
                return await Login(loginModel);
            }

            ViewBag.Error = "Registration failed. Email might already be in use.";
            return View(model);
        }

        // GET: /Auth/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}