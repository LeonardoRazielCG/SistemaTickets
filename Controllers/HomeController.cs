using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistemaTickets.Controllers
{
    [Authorize]// Bloquea el acceso a usuarios no autenticados
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        // Acción para mostrar la vista del formulario
        [HttpGet]
        public IActionResult CrearTicket()
        {
            return View();
        }

        // (Opcional por ahora) Acción POST para cuando el usuario envíe el formulario
        [HttpPost]
        public IActionResult CrearTicket(dynamic model)
        {
            // Aquí procesaremos los datos más adelante
            return RedirectToAction("Index");
        }
    }
}
