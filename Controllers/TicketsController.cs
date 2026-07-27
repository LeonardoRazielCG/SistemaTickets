using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SistemaTickets.Models;


namespace SistemaTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        //Esta es una lista estatica para simular la BD temporal
        private static readonly List<Ticket> _ticketsEnMemoria = new List<Ticket>
        {
            new Ticket
            {
                Id = 101,
                Titulo = "Fallo de impresora",
                Categoria = "Hardware",
                Prioridad = "Media",
                Descripcion = "No imprime",
                Estado = "En Proceso",
                FechaCreacion = DateTime.Now.AddDays(-1),
                UsuarioEmail = "Usuario@Cliente.com"

            }
        };

        [HttpGet]
        public IActionResult Create()
        {
            return View();//Esto debe de mostrar el formulario de Tickets
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                return View(ticket);
            }
            // Asignar datos automáticos
            ticket.Id = _ticketsEnMemoria.Count > 0 ? _ticketsEnMemoria.Max(t => t.Id) + 1 : 1001;
            ticket.FechaCreacion = DateTime.Now;
            ticket.Estado = "Abierto";
            ticket.UsuarioEmail = User.Identity?.Name ?? "usuario@cliente.com";

            // Guardar en la lista en memoria
            _ticketsEnMemoria.Add(ticket);

            TempData["SuccessMessage"] = "Tu Ticket se creo correctamente";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Index()
        {
            //Filtra los tickets pertenecientes al usuario actual
            var misTickets = _ticketsEnMemoria
                .Where(t => t.UsuarioEmail == (User.Identity?.Name ?? "Usuario@Cliente.com"))
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();
            return View(misTickets);
        }

    }
}
