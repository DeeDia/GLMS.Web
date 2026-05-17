using GLMS.Web.Data;
using GLMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ClientsController : Controller
    {
        private readonly GlmsDbContext _context;

        
        public ClientsController(GlmsDbContext context)
        {
            _context = context;
        }

        // GET: /Clients — shows list of all clients
        public async Task<IActionResult> Index()
        {
            var clients = await _context.Clients.ToListAsync();
            return View(clients);
        }

        // GET: /Clients/Create — shows the empty form
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Clients/Create — saves the form data
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (ModelState.IsValid)
            {
                _context.Clients.Add(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(client);
        }
    }
}