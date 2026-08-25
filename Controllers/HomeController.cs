using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Data;
using SistemaTickets.Models;
using System.Globalization;
using System.Runtime.CompilerServices;
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

            //Cargar solo los usuarios con rol de admin 
            ViewBag.Tecnicos = await _context.Usuarios
                .Where(u => u.Rol == "Admin" && u.Estatus == "Activo")
                .ToListAsync();

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

        //GET: /Home/Configuración
        [HttpGet]
        public async Task<IActionResult> Configuracion()
        {
            //Carga laa lista completa de los usuarios desde el SQL Server
            var usuarios = await _context.Usuarios.ToListAsync();
            return View(usuarios);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(Usuario nuevoUsuario)
        {
            //omitimos validaciones de campos opcionales o generados
            ModelState.Remove(nameof(Usuario.FotoPerfilUrl));
            ModelState.Remove(nameof(Usuario.Contrasena));

            if (ModelState.IsValid)
            {
                //Asignar una contraseña temporal por defecto para el primer acceso
                nuevoUsuario.Contrasena = "123456";
                nuevoUsuario.NombreUsuario = nuevoUsuario.Correo;
                nuevoUsuario.Estatus = "Activo";

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Usuario {nuevoUsuario.Nombre} Registrado coreectamente";
                return RedirectToAction("Configuracion");
            }

            TempData["Error"] = $"Ocurrio un error al intentar registrar el usuario";
            return RedirectToAction("Configuracion");

        }

        // POST: /Home/ActualizarFotoPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarFotoPerfil(IFormFile foto)
        {
            if (foto != null && foto.Length > 0)
            {
                // 1. Directorio destino
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "perfiles");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 2. Nombre único del archivo
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";

                // 3. DECLARACIÓN DE filePath (debe ir antes de usarse en FileStream)
                string filePath = Path.Combine(folderPath, fileName);

                // 4. Guardar archivo en disco
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                // 5. URL relativa para guardar en BD
                string fotoUrl = $"/uploads/perfiles/{fileName}";

                TempData["Success"] = "Foto de perfil guardada correctamente.";
            }

            return RedirectToAction("Configuracion");
        }

        // POST: /Home/AsignarAMi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarAMi(int id)
        {
            var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null)
            {
                // Toma el nombre del usuario autenticado en la sesión
                string usuarioActual = User.Identity?.Name ?? "Administrador TI";

                ticket.AsignadoA = usuarioActual;
                ticket.Estado = "En Proceso"; // Cambia el estado automáticamente al tomarlo

                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Te has asignado el ticket #{id} correctamente.";
            }

            return RedirectToAction("Details", new { id });
        }
    }
}