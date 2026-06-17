using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GLMS.Api.Data;
using GLMS.Api.Models;

namespace GLMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly GlmsDbContext _context;

        public ClientsController(GlmsDbContext context)
        {
            _context = context;
        }

        // GET: api/clients
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var clients = await _context.Clients.ToListAsync();
            return Ok(clients);
        }

        // GET: api/clients/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Contracts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
                return NotFound(new { message = $"Client {id} not found." });

            return Ok(client);
        }

        // POST: api/clients
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Client client)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById),
                new { id = client.Id }, client);
        }
    }
}