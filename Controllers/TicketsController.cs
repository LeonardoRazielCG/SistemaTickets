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

        // 1. Muestra la pantalla interactiva
        [HttpGet]
        public IActionResult StatusTicketUser(int id)
        {
            // Buscamos el ticket por ID
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View("StatusTicketUser", ticket);
        }

        // 2. Procesa y guarda el comentario del usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarComentarioUsuario(int ticketId, string mensaje)
        {
            var ticket = TicketRepository.Tickets.FirstOrDefault(t => t.Id == ticketId);

            if (ticket != null && !string.IsNullOrWhiteSpace(mensaje))
            {
                // Agregamos el nuevo comentario a la lista
                ticket.Comentarios.Add(new Comentario
                {
                    Autor = User.Identity?.Name ?? ticket.UsuarioEmail, // Toma el correo del usuario
                    Texto = mensaje,
                    Fecha = DateTime.Now
                });

                // Si el ticket ya estaba resuelto y el usuario pregunta algo más, vuelve a "En Proceso"
                if (ticket.Estado == "Resuelto")
                {
                    ticket.Estado = "En Proceso";
                }

                TempData["Mensaje"] = "Tu comentario ha sido enviado al equipo de Sistemas.";
            }

            // Recargamos la misma vista para mostrar el comentario nuevo en la lista
            return RedirectToAction("StatusTicketUser", new { id = ticketId });
        }


    }
}