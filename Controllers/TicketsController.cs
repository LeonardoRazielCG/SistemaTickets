using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Data;
using SistemaTickets.Models;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContex _context;

        public TicketsController(ApplicationDbContex context)
        {
            _context = context;
        }

        // GET: /Tickets
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            string usuarioEmail = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            string usuarioActual = User.Identity?.Name ?? string.Empty;
            bool esAdmin = User.IsInRole("Admin");

            List<Ticket> tickets;

            if (esAdmin)
            {
                tickets = await _context.Tickets
                    .OrderByDescending(t => t.Id)
                    .ToListAsync();
            }
            else
            {
                // Filtramos por UsuarioCorreo o UsuarioEmail
                tickets = await _context.Tickets
                    .Where(t => t.UsuarioCorreo.ToLower() == usuarioEmail.ToLower() || 
                                t.UsuarioEmail.ToLower() == usuarioActual.ToLower())
                    .OrderByDescending(t => t.Id)
                    .ToListAsync();
            }

            return View(tickets);
        }

        // GET: /Tickets/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categorias = await _context.Categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View();
        }

        // POST: /Tickets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Ticket model)
        {
            ModelState.Remove(nameof(Ticket.UsuarioNombre));
            ModelState.Remove(nameof(Ticket.UsuarioCorreo));
            ModelState.Remove(nameof(Ticket.UsuarioEmail));
            ModelState.Remove(nameof(Ticket.FechaCreacion));
            ModelState.Remove(nameof(Ticket.Estado));

            if (ModelState.IsValid)
            {
                string nombreUsuario = User.Identity?.Name ?? "Usuario Anónimo";
                string correoUsuario = User.FindFirstValue(ClaimTypes.Email) ?? "sin-correo@empresa.com";

                model.UsuarioNombre = nombreUsuario;
                model.UsuarioCorreo = correoUsuario;
                model.UsuarioEmail = correoUsuario;
                model.FechaCreacion = DateTime.Now;
                model.Estado = "Abierto";

                _context.Tickets.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"¡Ticket #{model.Id} enviado correctamente!";
                return RedirectToAction("Index");
            }

            //Si falla la validacion

            ViewBag.Categorias = await _context.Categorias
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(model);
        }

        // GET: /Tickets/StatusTicketUser/1
        // 1. Muestra la pantalla interactiva leyendo de SQL Server
        [HttpGet]
        public async Task<IActionResult> StatusTicketUser(int id)
        {
            // Buscamos el ticket por ID e incluimos sus comentarios desde SQL Server
            var ticket = await _context.Tickets
                .Include(t => t.Comentarios)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View("StatusTicketUser", ticket);
        }

        // POST: /Tickets/GuardarComentarioUsuario
        // 2. Procesa y guarda el comentario del usuario en la BD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarComentarioUsuario(int ticketId, string mensaje)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Comentarios)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket != null && !string.IsNullOrWhiteSpace(mensaje))
            {
                string autor = User.Identity?.Name ?? ticket.UsuarioCorreo;

                // Creamos el nuevo comentario vinculado al Ticket
                var comentario = new Comentario
                {
                    TicketId = ticketId,
                    Autor = autor,
                    Texto = mensaje,
                    Fecha = DateTime.Now
                };

                _context.Comentarios.Add(comentario);

                // Si el ticket ya estaba resuelto y el usuario pregunta algo más, vuelve a "En Proceso"
                if (string.Equals(ticket.Estado, "Resuelto", StringComparison.OrdinalIgnoreCase))
                {
                    ticket.Estado = "En Proceso";
                }

                // Guardamos los cambios en SQL Server (comentario e historia de estado)
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Tu comentario ha sido enviado al equipo de Sistemas.";
            }

            // Recargamos la misma vista para mostrar el comentario recién publicado
            return RedirectToAction("StatusTicketUser", new { id = ticketId });
        }

        // GET: /Tickets/ConfiguracionUser
        [HttpGet]
        public async Task<IActionResult> ConfiguracionUser()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario != null) 
            {
                ViewBag.FotoPerfil = usuario.FotoPerfilUrl;
                ViewBag.Departamento = usuario.Departamento;
                ViewBag.Nombre = usuario.Nombre;
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ConfiguracionUser(Usuario model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] = "Perfil actualizado correctamente.";
            return RedirectToAction(nameof(ConfiguracionUser));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarPerfilUser(string nombre, string departamento, string? nuevaPassword, IFormFile? foto)
        {
            // 1. Obtener ID del usuario actual
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdClaim);
            var usuario = await _context.Usuarios.FindAsync(userId);

            if (usuario == null) return NotFound();

            try
            {
                // 2. Actualizar datos básicos
                usuario.Nombre = nombre;
                usuario.Departamento = departamento;

                // 3. Procesar foto si subió una
                if (foto != null && foto.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(foto.FileName);
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/perfiles", fileName);

                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await foto.CopyToAsync(stream);
                    }

                    usuario.FotoPerfilUrl = "/uploads/perfiles/" + fileName;
                }

                // 4. Actualizar contraseña si ingresó una nueva
                if (!string.IsNullOrWhiteSpace(nuevaPassword))
                {
                    usuario.Contrasena = nuevaPassword;
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                // --- ACTUALIZAR LA SESIÓN EN CALIENTE ---
                var claims = new List<System.Security.Claims.Claim>
        {
                     new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                     new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, usuario.Nombre),
                     new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, usuario.Correo ?? "")
        };

                var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, new System.Security.Claims.ClaimsPrincipal(claimsIdentity));
                // ----------------------------------------

                TempData["Success"] = "Tu perfil ha sido actualizado correctamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Ocurrió un error al actualizar los datos: " + ex.Message;
            }

            return RedirectToAction("ConfiguracionUser");
        }
    }
}