using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GLMS.Api.Data;
using GLMS.Api.Models;

namespace GLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ServiceRequestsController : ControllerBase
    {
        private readonly GlmsDbContext _context;

        public ServiceRequestsController(GlmsDbContext context)
        {
            _context = context;
        }

        // GET: api/servicerequests
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _context.ServiceRequests
                .Include(r => r.Contract)
                .ToListAsync();
            return Ok(requests);
        }

        // POST: api/servicerequests
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] ServiceRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── WORKFLOW GUARD ────────────────────────────────────────────
            // The API enforces the rule — not just the frontend
            var contract = await _context.Contracts
                .FindAsync(request.ContractId);

            if (contract == null)
                return BadRequest(new { message = "Contract not found." });

            if (contract.Status == ContractStatus.Expired ||
                contract.Status == ContractStatus.OnHold)
            {
                return BadRequest(new
                {
                    message = $"Cannot raise a request against a " +
                              $"{contract.Status} contract. " +
                              $"Only Active contracts are allowed."
                });
            }

            // ── CURRENCY CALCULATION ──────────────────────────────────────
            // CostZAR is calculated here on the server
            // ExchangeRate should be passed in the request body
            request.CostZAR = Math.Round(
                request.CostUSD * request.ExchangeRate, 2,
                MidpointRounding.AwayFromZero);

            request.DateRaised = DateTime.Now;

            _context.ServiceRequests.Add(request);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAll),
                new { id = request.Id }, request);
        }
    }
}