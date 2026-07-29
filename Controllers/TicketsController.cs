using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Data;
using SistemaTickets.Models;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        // GET: /Tickets/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Ticket ticket)
        {
            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            // Obtener el usuario/nombre autenticado actual
            string usuarioActual = User.Identity?.Name ?? "Usuario Anónimo";

            // Asignar ID dinámico y datos iniciales
            ticket.Id = TicketRepository.Tickets.Count > 0 ? TicketRepository.Tickets.Max(t => t.Id) + 1 : 101;
            ticket.FechaCreacion = DateTime.Now;
            ticket.Estado = "Abierto";
            ticket.UsuarioEmail = usuarioActual; // Guardamos exactamente lo que retorna User.Identity.Name

            // Guardar en la lista compartida
            TicketRepository.Tickets.Add(ticket);

            TempData["Success"] = $"¡Ticket #{ticket.Id} enviado correctamente!";
            return RedirectToAction("Index");
        }

        // GET: /Tickets/Index
        [HttpGet]
        public IActionResult Index()
        {
            string usuarioActual = User.Identity?.Name ?? "Usuario Anónimo";

            // Si es Admin, ve todos los tickets; si es Usuario normal, solo ve los que él creó
            List<Ticket> ticketsAMostrar;

            if (User.IsInRole("Admin"))
            {
                ticketsAMostrar = TicketRepository.Tickets
                    .OrderByDescending(t => t.FechaCreacion)
                    .ToList();
            }
            else
            {
                ticketsAMostrar = TicketRepository.Tickets
                    .Where(t => t.UsuarioEmail.Equals(usuarioActual, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(t => t.FechaCreacion)
                    .ToList();
            }

            return View(ticketsAMostrar);
        }
    }
}