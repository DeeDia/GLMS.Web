using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using GLMS.Api.Data;
using GLMS.Api.Models;

namespace GLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize]  ← uncomment this once JWT is working
    public class ContractsController : ControllerBase
    {
        private readonly GlmsDbContext _context;

        public ContractsController(GlmsDbContext context)
        {
            _context = context;
        }

        // GET: api/contracts
        // Optional query params: ?status=Active&startDate=2024-01-01&endDate=2024-12-31
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? status,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Contracts
                .Include(c => c.Client)
                .AsQueryable();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<ContractStatus>(status, out var parsedStatus))
            {
                query = query.Where(c => c.Status == parsedStatus);
            }

            // Filter by date range if provided
            if (startDate.HasValue)
                query = query.Where(c => c.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(c => c.EndDate <= endDate.Value);

            var contracts = await query.ToListAsync();

            // Return 200 OK with JSON array
            return Ok(contracts);
        }

        // GET: api/contracts/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return NotFound(new { message = $"Contract {id} not found." });

            return Ok(contract);
        }

        // POST: api/contracts
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] GLMS.Api.Models.Contract contract)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Contracts.Add(contract);
            await _context.SaveChangesAsync();

            // Return 201 Created with the new contract and its location
            return CreatedAtAction(nameof(GetById),
                new { id = contract.Id }, contract);
        }

        // PATCH: api/contracts/5/status
        // Body: { "status": "Active" }
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(
            int id, [FromBody] UpdateStatusRequest request)
        {
            var contract = await _context.Contracts.FindAsync(id);

            if (contract == null)
                return NotFound(new { message = $"Contract {id} not found." });

            if (!Enum.TryParse<ContractStatus>(request.Status, out var newStatus))
                return BadRequest(new { message = $"Invalid status: {request.Status}" });

            contract.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated.", contract });
        }
    }

    // Simple request model for the PATCH endpoint
    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}