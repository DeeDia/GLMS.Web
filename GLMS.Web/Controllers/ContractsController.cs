using GLMS.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ContractsController : ApiBaseController
    {
        private readonly IWebHostEnvironment _env;

        public ContractsController(
            IHttpClientFactory factory,
            IWebHostEnvironment env)
            : base(factory)
        {
            _env = env;
        }

        // GET: /Contracts
        public async Task<IActionResult> Index(
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            // Build query string for the API
            var query = "api/contracts?";
            if (!string.IsNullOrEmpty(statusFilter))
                query += $"status={statusFilter}&";
            if (startDate.HasValue)
                query += $"startDate={startDate:yyyy-MM-dd}&";
            if (endDate.HasValue)
                query += $"endDate={endDate:yyyy-MM-dd}&";

            // Call the API instead of the database
            var contracts = await ApiGet<List<Contract>>(query)
                            ?? new List<Contract>();

            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.StatusList = Enum.GetNames(typeof(ContractStatus));

            return View(contracts);
        }

        // GET: /Contracts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contract = await ApiGet<Contract>($"api/contracts/{id}");
            if (contract == null) return NotFound();
            return View(contract);
        }

        // GET: /Contracts/Create
        public async Task<IActionResult> Create()
        {
            // Get clients from the API for the dropdown
            var clients = await ApiGet<List<Client>>("api/clients")
                          ?? new List<Client>();

            ViewBag.Clients = new SelectList(clients, "Id", "Name");
            return View();
        }

        // POST: /Contracts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Contract contract,
            IFormFile? signedAgreement)
        {
            ModelState.Remove("SignedAgreementPath");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                // Handle PDF upload locally — save to wwwroot/uploads
                if (signedAgreement != null &&
                    signedAgreement.Length > 0)
                {
                    var ext = Path
                        .GetExtension(signedAgreement.FileName)
                        .ToLowerInvariant();

                    if (ext != ".pdf")
                    {
                        ModelState.AddModelError("",
                            "Only PDF files are allowed.");
                        await ReloadCreateView();
                        return View(contract);
                    }

                    var folder = Path.Combine(
                        _env.WebRootPath, "uploads");
                    var fileName = Guid.NewGuid() + "_"
                        + signedAgreement.FileName;
                    var filePath = Path.Combine(folder, fileName);

                    using var stream = new FileStream(
                        filePath, FileMode.Create);
                    await signedAgreement.CopyToAsync(stream);

                    contract.SignedAgreementPath =
                        "/uploads/" + fileName;
                }

                // Send the contract to the API
                var response = await ApiPost("api/contracts", contract);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError("",
                    "Failed to save contract. Please try again.");
            }

            await ReloadCreateView();
            return View(contract);
        }

        // GET: /Contracts/DownloadAgreement/5
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var contract = await ApiGet<Contract>($"api/contracts/{id}");

            if (contract == null ||
                string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var filePath = Path.Combine(
                _env.WebRootPath,
                contract.SignedAgreementPath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);
            return File(bytes, "application/pdf", fileName);
        }

        // Helper to reload client dropdown
        private async Task ReloadCreateView()
        {
            var clients = await ApiGet<List<Client>>("api/clients")
                          ?? new List<Client>();
            ViewBag.Clients = new SelectList(clients, "Id", "Name");
        }
    }
}