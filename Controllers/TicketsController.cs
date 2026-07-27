using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        [HttpGet]
        public IActionResult Create()
        {
            return View();//Esto debe de mostrar el formulario de Tickets
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(IFormCollection collection)
        {
            TempData["SuccessMessage"] = "Tu Ticket se creo correctamente";
            return RedirectToAction("Create");
        }
    }
}
