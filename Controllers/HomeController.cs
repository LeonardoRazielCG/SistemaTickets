using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Data;
using SistemaTickets.Models;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        // GET: Dashboard Admin (Muestra todos los tickets)
        public IActionResult Index()
        {
            var todosLosTickets = TicketRepository.Tickets
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();

            return View(todosLosTickets);
        }

        // GET: Detalle y Gestión del Ticket
        [HttpGet]
        public IActionResult Details(int id)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // POST: Cambiar Estado o Asignación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, string nuevoEstado, string asignadoA)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket != null)
            {
                ticket.Estado = nuevoEstado;
                if (!string.IsNullOrEmpty(asignadoA))
                {
                    ticket.AsignadoA = asignadoA;
                }
                TempData["Success"] = $"Ticket #{id} actualizado correctamente.";
            }
            return RedirectToAction("Details", new { id });
        }

        // POST: Agregar Comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int id, string textoComentario)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket != null && !string.IsNullOrWhiteSpace(textoComentario))
            {
                ticket.Comentarios.Add(new Comentario
                {
                    Autor = User.Identity?.Name ?? "Administrador",
                    Texto = textoComentario,
                    Fecha = DateTime.Now
                });
            }
            return RedirectToAction("Details", new { id });
        }

        // GET: /Home/CrearTicket
        [HttpGet]
        public IActionResult CrearTicket()
        {
            return View();
        }

        // POST: /Home/CrearTicket
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearTicket(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            // Asignar ID dinámico y datos iniciales
            ticket.Id = TicketRepository.Tickets.Count > 0 ? TicketRepository.Tickets.Max(t => t.Id) + 1 : 101;
            ticket.FechaCreacion = DateTime.Now;
            ticket.Estado = "Abierto";
            ticket.UsuarioEmail = User.Identity?.Name ?? "Administrador";

            // Agregar a la lista compartida
            TicketRepository.Tickets.Add(ticket);

            TempData["Success"] = $"¡Ticket #{ticket.Id} creado correctamente por el Administrador!";

            // Redirige directamente al Dashboard general del Admin
            return RedirectToAction("Index");
        }
    }
}