using GLMS.Web.Data;
using GLMS.Web.Models;
using GLMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ServiceRequestsController : Controller
    {
        private readonly GlmsDbContext _context;
        private readonly CurrencyService _currencyService;

        public ServiceRequestsController(
            GlmsDbContext context,
            CurrencyService currencyService)
        {
            _context = context;
            _currencyService = currencyService;
        }

        // GET: /ServiceRequests
        public async Task<IActionResult> Index()
        {
            var requests = await _context.ServiceRequests
                .Include(r => r.Contract)
                .ToListAsync();
            return View(requests);
        }

        // GET: /ServiceRequests/Create
        public async Task<IActionResult> Create()
        {
            // Fetch the live USD to ZAR rate to show on the form
            var rate = await _currencyService.GetUsdToZarRateAsync();

            // Only show ACTIVE contracts in the dropdown
            // This is the workflow guard — Expired/OnHold contracts
            // are filtered out here so the user cannot select them
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(
                activeContracts,
                "Id",
                "ServiceLevel");

            // Pass the rate to the view so JavaScript can use it
            ViewBag.ExchangeRate = rate;

            return View();
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceRequest request)
        {
            ModelState.Remove("Contract");

            if (ModelState.IsValid)
            {
                //WORKFLOW GUARD
                // Double-check on the server side — never trust
                // only the client (browser) to enforce rules
                var contract = await _context.Contracts
                    .FindAsync(request.ContractId);

                if (contract == null)
                {
                    ModelState.AddModelError("",
                        "Selected contract does not exist.");
                    return await ReloadCreateView();
                }

                if (contract.Status == ContractStatus.Expired ||
                    contract.Status == ContractStatus.OnHold)
                {
                    // Block the request — show a clear error message
                    ModelState.AddModelError("",
                        $"Cannot raise a service request against a " +
                        $"contract that is {contract.Status}. " +
                        $"Only Active contracts are allowed.");
                    return await ReloadCreateView();
                }

                //CURRENCY CALCULATION
                // Fetch the live rate at the moment of saving
                var rate = await _currencyService.GetUsdToZarRateAsync();
                request.ExchangeRate = rate;
                request.CostZAR = _currencyService
                    .ConvertUsdToZar(request.CostUSD, rate);
                request.DateRaised = DateTime.Now;

                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return await ReloadCreateView();
        }

        // Helper — reloads the Create view with dropdowns intact
        private async Task<IActionResult> ReloadCreateView()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            var activeContracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();

            ViewBag.Contracts = new SelectList(
                activeContracts, "Id", "ServiceLevel");
            ViewBag.ExchangeRate = rate;
            return View();
        }
    }
}