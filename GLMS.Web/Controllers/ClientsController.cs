using GLMS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GLMS.Web.Controllers
{
    public class ClientsController : ApiBaseController
    {
        public ClientsController(IHttpClientFactory factory)
            : base(factory) { }

        // GET: /Clients
        public async Task<IActionResult> Index()
        {
            var clients = await ApiGet<List<Client>>("api/clients")
                          ?? new List<Client>();
            return View(clients);
        }

        // GET: /Clients/Create
        public IActionResult Create() => View();

        // POST: /Clients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Client client)
        {
            if (ModelState.IsValid)
            {
                var response = await ApiPost("api/clients", client);

                if (response.IsSuccessStatusCode)
                    return RedirectToAction(nameof(Index));

                ModelState.AddModelError("",
                    "Failed to save client. Please try again.");
            }
            return View(client);
        }
    }
}