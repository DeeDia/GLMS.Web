using GLMS.Web.Data;
using GLMS.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ContractsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly IWebHostEnvironment _env;

        // _env tells us where wwwroot is on the server
        public ContractsController(GlmsDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Contracts
        // Search parameters come from the filter form on the Index page
        public async Task<IActionResult> Index(
            string? statusFilter,
            DateTime? startDate,
            DateTime? endDate)
        {
            // Start with ALL contracts, including their client names
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            // LINQ filter 1 — filter by status if one was selected
            if (!string.IsNullOrEmpty(statusFilter) &&
                Enum.TryParse<ContractStatus>(statusFilter, out var status))
            {
                query = query.Where(c => c.Status == status);
            }

            // LINQ filter 2 — filter by start date range
            if (startDate.HasValue)
            {
                query = query.Where(c => c.StartDate >= startDate.Value);
            }

            // LINQ filter 3 — filter by end date range
            if (endDate.HasValue)
            {
                query = query.Where(c => c.EndDate <= endDate.Value);
            }

            // Pass filter values back to the view so the form stays filled in
            ViewBag.StatusFilter = statusFilter;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            // Pass status options for the dropdown
            ViewBag.StatusList = Enum.GetNames(typeof(ContractStatus));

            var contracts = await query.ToListAsync();
            return View(contracts);
        }

        // GET: /Contracts/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null) return NotFound();
            return View(contract);
        }

        // GET: /Contracts/Create — shows empty form
        public IActionResult Create()
        {
            // Populate client dropdown
            ViewBag.Clients = new SelectList(
                _context.Clients, "Id", "Name");
            return View();
        }

        // POST: /Contracts/Create — saves new contract + PDF
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            GLMS.Web.Models.Contract contract,
            IFormFile? signedAgreement)
        {
            // Remove SignedAgreementPath from validation —
            // it is set by the upload, not typed by the user
            ModelState.Remove("SignedAgreementPath");
            ModelState.Remove("Client");

            if (ModelState.IsValid)
            {
                // Handle PDF upload
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    // Validate file is a PDF
                    var extension = Path
                        .GetExtension(signedAgreement.FileName)
                        .ToLowerInvariant();

                    if (extension != ".pdf")
                    {
                        ModelState.AddModelError("signedAgreement",
                            "Only PDF files are allowed.");
                        ViewBag.Clients = new SelectList(
                            _context.Clients, "Id", "Name");
                        return View(contract);
                    }

                    // Save file to wwwroot/uploads/
                    var uploadsFolder = Path.Combine(
                        _env.WebRootPath, "uploads");

                    // Give the file a unique name so two files
                    // with the same name don't overwrite each other
                    var uniqueFileName = Guid.NewGuid().ToString()
                        + "_" + signedAgreement.FileName;

                    var filePath = Path.Combine(
                        uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await signedAgreement.CopyToAsync(stream);
                    }

                    // Store the relative path in the database
                    contract.SignedAgreementPath = "/uploads/" + uniqueFileName;
                }

                _context.Contracts.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = new SelectList(
                _context.Clients, "Id", "Name");
            return View(contract);
        }

        // GET: /Contracts/DownloadAgreement/5
        // Lets the user download the PDF from the Details page
        public async Task<IActionResult> DownloadAgreement(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);

            if (contract == null ||
                string.IsNullOrEmpty(contract.SignedAgreementPath))
                return NotFound();

            var filePath = Path.Combine(
                _env.WebRootPath,
                contract.SignedAgreementPath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, "application/pdf", fileName);
        }
    }
}