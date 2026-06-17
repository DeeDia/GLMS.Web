using GLMS.Web.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ServiceRequestsController : ApiBaseController
    {
        public ServiceRequestsController(IHttpClientFactory factory)
            : base(factory) { }

        // GET: /ServiceRequests
        public async Task<IActionResult> Index()
        {
            var requests = await ApiGet<List<ServiceRequest>>(
                "api/servicerequests")
                ?? new List<ServiceRequest>();
            return View(requests);
        }

        // GET: /ServiceRequests/Create
        public async Task<IActionResult> Create()
        {
            // Get live rate from the API
            var rateResult = await ApiGet<CurrencyRateResult>(
                "api/currency/rate");
            var rate = rateResult?.Rate ?? 18.50m;

            // Get only Active contracts from the API
            var contracts = await ApiGet<List<Contract>>(
                "api/contracts?status=Active")
                ?? new List<Contract>();

            ViewBag.Contracts = new SelectList(
                contracts, "Id", "ServiceLevel");
            ViewBag.ExchangeRate = rate;
            return View();
        }

        // POST: /ServiceRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest request)
        {
            ModelState.Remove("Contract");

            if (ModelState.IsValid)
            {
                // Get the live rate at time of saving
                var rateResult = await ApiGet<CurrencyRateResult>(
                    "api/currency/rate");
                request.ExchangeRate = rateResult?.Rate ?? 18.50m;

                // Send to API — the API handles workflow guard
                // and ZAR calculation
                var response = await ApiPost(
                    "api/servicerequests", request);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));

                // Read the error message from the API response
                var errorJson = await response.Content
                    .ReadAsStringAsync();

                try
                {
                    var errorObj = JsonSerializer.Deserialize
                        <ApiErrorResponse>(errorJson,
                        new JsonSerializerOptions
                        { PropertyNameCaseInsensitive = true });

                    ModelState.AddModelError("",
                        errorObj?.Message
                        ?? "Failed to save request.");
                }
                catch
                {
                    ModelState.AddModelError("",
                        "Failed to save request. Please try again.");
                }
            }

            await ReloadCreateView();
            return View(request);
        }

        private async Task ReloadCreateView()
        {
            var rateResult = await ApiGet<CurrencyRateResult>(
                "api/currency/rate");
            var rate = rateResult?.Rate ?? 18.50m;

            var contracts = await ApiGet<List<Contract>>(
                "api/contracts?status=Active")
                ?? new List<Contract>();

            ViewBag.Contracts = new SelectList(
                contracts, "Id", "ServiceLevel");
            ViewBag.ExchangeRate = rate;
        }
    }

    // Maps the JSON response from api/currency/rate
    public class CurrencyRateResult
    {
        public decimal Rate { get; set; }
    }

    // Maps error messages from the API
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}