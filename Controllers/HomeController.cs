using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Data;
using SistemaTickets.Models;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContex _context;
        public HomeController(ApplicationDbContex context)
        {
            _context = context;
        }

        // GET: /Home/Index (TODOS LOS TICKETS)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var todosLosTickets = await _context.Tickets
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return View(todosLosTickets);
        }

        // GET: /Home/Pendientes
        [HttpGet]
        public async Task<IActionResult> Pendientes()
        {
            var ticketsPendientes = await _context.Tickets
                .Where(t => t.Estado == "Abierto" || t.Estado == "En Proceso")
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return View(ticketsPendientes);
        }

        // GET: /Home/Resueltos 
        [HttpGet]
        public async Task<IActionResult> Resueltos()
        {
            var ticketsResueltos = await _context.Tickets
                .Where(t => t.Estado.ToLower() == "Resuelto" || t.Estado.ToLower() == "Cerrado")
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

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
        public async Task<IActionResult> CrearTicket(Ticket ticket)
        {
            ModelState.Remove(nameof(Ticket.UsuarioNombre));
            ModelState.Remove(nameof(Ticket.UsuarioCorreo));
            ModelState.Remove(nameof(Ticket.UsuarioEmail));
            ModelState.Remove(nameof(Ticket.FechaCreacion));
            ModelState.Remove(nameof(Ticket.Estado));
            ModelState.Remove(nameof(Ticket.AsignadoA));
            ModelState.Remove(nameof(Ticket.Comentarios));

            if (ModelState.IsValid)
            {
                string adminNombre = User.Identity?.Name ?? "Administrador";
                string adminCorreo = User.FindFirstValue(ClaimTypes.Email) ?? "admin@empresa.com";

                ticket.UsuarioNombre = adminNombre;
                ticket.UsuarioCorreo = adminCorreo;
                ticket.UsuarioEmail = adminCorreo;
                ticket.FechaCreacion = DateTime.Now;
                ticket.Estado = "Abierto";

                _context.Tickets.Add(ticket);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Ticket #{ticket.Id} creado correctamente";
                return RedirectToAction("Index");
            }
            return View(ticket);
        }

        // POST: Marcar como Resuelto de forma rápida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsResolved(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Comentarios)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket != null)
            {
                ticket.Estado = "Resuelto";

                var comentario = new Comentario
                {
                    TicketId = id,
                    Autor = User.Identity?.Name ?? "Administrador",
                    Texto = "El ticket ha sido marcado como RESUELTO.",
                    Fecha = DateTime.Now
                };

                _context.Comentarios.Add(comentario);
                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El ticket #{id} ha sido marcado como Resuelto correctamente.";
            }

            string returnUrl = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // GET: /Home/Details/1
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Comentarios)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // POST: Actualizar Estado / Asignación
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string nuevoEstado, string asignadoA)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null)
            {
                if (!string.IsNullOrEmpty(nuevoEstado))
                {
                    ticket.Estado = nuevoEstado;
                }

                if (!string.IsNullOrEmpty(asignadoA))
                {
                    ticket.AsignadoA = asignadoA;
                }

                // Notificamos a Entity Framework que el objeto fue modificado explícitamente
                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Ticket #{id} actualizado correctamente a '{nuevoEstado}'.";
            }

            return RedirectToAction("Details", new { id });
        }

        // POST: Agregar Comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string textoComentario)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null && !string.IsNullOrWhiteSpace(textoComentario))
            {
                var comentario = new Comentario
                {
                    TicketId = id,
                    Autor = User.Identity?.Name ?? "Administrador",
                    Texto = textoComentario,
                    Fecha = DateTime.Now
                };

                _context.Comentarios.Add(comentario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Details", new { id });
        }
    }
}