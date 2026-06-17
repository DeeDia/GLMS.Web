using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GLMS.Web.Controllers
{
    public class LoginController : Controller
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _config;

        public LoginController(
            IHttpClientFactory factory,
            IConfiguration config)
        {
            _factory = factory;
            _config = config;
        }

        // GET: /Login
        public IActionResult Index()
        {
            if (!string.IsNullOrEmpty(
                HttpContext.Session.GetString("JwtToken")))
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Login
        [HttpPost]
        public async Task<IActionResult> Index(
            string username, string password)
        {
            try
            {
                // Use the named client which has SSL bypass configured
                var client = _factory.CreateClient("GLMSApi");

                var payload = JsonSerializer.Serialize(
                    new { username, password });
                var content = new StringContent(
                    payload, Encoding.UTF8, "application/json");

                var response = await client
                    .PostAsync("api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content
                        .ReadAsStringAsync();
                    var opts = new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true };
                    var result = JsonSerializer
                        .Deserialize<LoginResult>(json, opts);

                    if (result != null)
                    {
                        HttpContext.Session.SetString(
                            "JwtToken", result.Token);
                        HttpContext.Session.SetString(
                            "Username", result.Username);
                        HttpContext.Session.SetString(
                            "UserRole", result.Role);
                    }

                    return RedirectToAction("Index", "Home");
                }

                ViewBag.Error = "Invalid username or password.";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Cannot connect to API: {ex.Message}";
            }

            return View();
        }

        // GET: /Login/Register
        public IActionResult Register() => View();

        // POST: /Login/Register
        [HttpPost]
        public async Task<IActionResult> Register(
            string username, string password)
        {
            try
            {
                var client = _factory.CreateClient("GLMSApi");

                var payload = JsonSerializer.Serialize(
                    new { username, password });
                var content = new StringContent(
                    payload, Encoding.UTF8, "application/json");

                var response = await client
                    .PostAsync("api/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] =
                        "Account created! Please log in.";
                    return RedirectToAction("Index");
                }

                var errorBody = await response.Content
                    .ReadAsStringAsync();
                ViewBag.Error = $"Registration failed: {errorBody}";
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Cannot connect to API: {ex.Message}";
            }

            return View();
        }

        // GET: /Login/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }

    public class LoginResult
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}