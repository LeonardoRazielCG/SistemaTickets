using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Data;
using SistemaTickets.Models;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        // GET: /Home/Index (ALL TICKETS)
        [HttpGet]
        public IActionResult Index()
        {
            var todosLosTickets = TicketRepository.Tickets
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();

            return View(todosLosTickets);
        }

        // GET: /Home/Pendientes
        [HttpGet]
        public IActionResult Pendientes()
        {
            var ticketsPendientes = TicketRepository.Tickets
                .Where(t => t.Estado == "Abierto" || t.Estado == "En Proceso")
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();

            return View(ticketsPendientes);
        }

        // GET: /Home/Resueltos (RESOLVED TICKETS)
        [HttpGet]
        public IActionResult Resueltos()
        {
            var ticketsResueltos = TicketRepository.Tickets
                .Where(t => t.Estado == "Resuelto" || t.Estado == "Cerrado")
                .OrderByDescending(t => t.FechaCreacion)
                .ToList();

            return View(ticketsResueltos);
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

            ticket.Id = TicketRepository.Tickets.Count > 0 ? TicketRepository.Tickets.Max(t => t.Id) + 1 : 101;
            ticket.FechaCreacion = DateTime.Now;
            ticket.Estado = "Abierto";
            ticket.UsuarioEmail = User.Identity?.Name ?? "Administrador";

            TicketRepository.Tickets.Add(ticket);

            TempData["Success"] = $"¡Ticket #{ticket.Id} creado correctamente por el Administrador!";
            return RedirectToAction("Index");
        }

        // POST: Marcar como Resuelto de forma rápida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAsResolved(int id)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket != null)
            {
                ticket.Estado = "Resuelto";
                ticket.Comentarios.Add(new Comentario
                {
                    Autor = User.Identity?.Name ?? "Administrador",
                    Texto = "El ticket ha sido marcado como RESUELTO.",
                    Fecha = DateTime.Now
                });

                TempData["Success"] = $"El ticket #{id} ha sido marcado como Resuelto correctamente.";
            }

            string returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // GET: /Home/Details/101
        [HttpGet]
        public IActionResult Details(int id)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);
            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // POST: Actualizar Estado / Asignación
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
                TempData["Success"] = $"Ticket #{id} actualizado a '{nuevoEstado}'.";
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
    }
}