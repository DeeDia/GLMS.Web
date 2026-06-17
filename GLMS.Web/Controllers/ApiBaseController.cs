using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    // All MVC controllers that call the API inherit from this
    public class ApiBaseController : Controller
    {
        private readonly IHttpClientFactory _factory;
        protected readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiBaseController(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        // Creates an HttpClient with the JWT token attached
        // if the user is logged in
        protected HttpClient GetApiClient()
        {
            var client = _factory.CreateClient("GLMSApi");

            // Get the token from session and attach it to the request
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        // Shortcut: GET request that returns a deserialised object
        protected async Task<T?> ApiGet<T>(string endpoint)
        {
            var client = GetApiClient();
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode) return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, JsonOpts);
        }

        // Shortcut: POST request that sends JSON and returns the response
        protected async Task<HttpResponseMessage> ApiPost<T>(
            string endpoint, T data)
        {
            var client = GetApiClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(
                json, Encoding.UTF8, "application/json");
            return await client.PostAsync(endpoint, content);
        }

        // Shortcut: PATCH request for status updates
        protected async Task<HttpResponseMessage> ApiPatch<T>(
            string endpoint, T data)
        {
            var client = GetApiClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(
                json, Encoding.UTF8, "application/json");
            return await client.PatchAsync(endpoint, content);
        }
    }
}