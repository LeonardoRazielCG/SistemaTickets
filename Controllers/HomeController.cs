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
        public async Task<IActionResult> CrearTicket()
        {
            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
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
        // POST: Actualizar Estado / Asignación Múltiple
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

                if (!string.IsNullOrEmpty(asignadoA) && asignadoA != "Sin asignar")
                {
                    // Si ya hay un encargado y es distinto, los combinamos (Atención Conjunta)
                    if (!string.IsNullOrEmpty(ticket.AsignadoA) && ticket.AsignadoA != "Sin asignar" && !ticket.AsignadoA.Contains(asignadoA))
                    {
                        ticket.AsignadoA = $"{ticket.AsignadoA}, {asignadoA}";
                    }
                    else
                    {
                        ticket.AsignadoA = asignadoA;
                    }
                }

                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Ticket #{id} actualizado correctamente.";
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

            //Carga las categorias de la BD pra pestaña caatalogos
            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();

            //Obtiene el usuario autenticado para mostrr dtos del perfil
            string correoUsuario = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            var usuarioActual = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Correo == correoUsuario || u.NombreUsuario == correoUsuario);
            ViewBag.UsuarioActual = usuarioActual;

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
                // 1. Directorio destino en wwwroot
                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "perfiles");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 2. Generar nombre único
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(foto.FileName)}";
                string filePath = Path.Combine(folderPath, fileName);

                // 3. Guardar archivo físicamente
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await foto.CopyToAsync(stream);
                }

                // 4. URL relativa para SQL Server
                string fotoUrl = $"/uploads/perfiles/{fileName}";

                // 5. Obtener el usuario autenticado e impactar en SQL Server
                string correoUsuario = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
                var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correoUsuario || u.NombreUsuario == correoUsuario);

                if (usuario != null)
                {
                    usuario.FotoPerfilUrl = fotoUrl;
                    _context.Entry(usuario).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Foto de perfil actualizada e impactada en la base de datos.";
                }
                else
                {
                    TempData["Error"] = "No se encontró el usuario activo en la base de datos.";
                }
            }
            else
            {
                TempData["Error"] = "Por favor selecciona una imagen válida.";
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
                string miNombre = User.Identity?.Name ?? "Leonardo C."; // Toma el usuario autenticado

                if (string.IsNullOrEmpty(ticket.AsignadoA) || ticket.AsignadoA == "Sin asignar")
                {
                    ticket.AsignadoA = miNombre;
                }
                else if (!ticket.AsignadoA.Contains(miNombre))
                {
                    ticket.AsignadoA = $"{ticket.AsignadoA}, {miNombre}";
                }

                ticket.Estado = "En Proceso";

                _context.Entry(ticket).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Te has sumado a la atención del ticket #{id}.";
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> MisTickets()
        {
            string miNombre = User.Identity?.Name ?? "Leonardo C.";
            var misTickets = await _context.Tickets
                .Where(t => t.AsignadoA.Contains(miNombre))
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            return View("Index", misTickets);//Reutiliza la vista principal
        }

        // POST: /Home/CrearCategoria
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearCategoria(string nombreCategoria, string descripcionCategoria)
        {
            if (!string.IsNullOrWhiteSpace(nombreCategoria))
            {
                var nuevaCategoria = new Categoria
                {
                    Nombre = nombreCategoria.Trim(),
                    Descripcion = descripcionCategoria?.Trim() ?? string.Empty
                };

                _context.Categorias.Add(nuevaCategoria);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Categoría '{nombreCategoria}' agregada correctamente.";
            }
            else
            {
                TempData["Error"] = "El nombre de la categoría no puede estar vacío.";
            }

            return RedirectToAction("Configuracion");
        }

        // Eliminar categoria
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if(categoria !=null)
            {
                _context.Categorias.Remove(categoria);
                await _context.SaveChangesAsync();
                TempData["success"] = "se elimino correctamente";
            }
            else
            {
                TempData["Error"] = "No se encontro la categoria a eliminar";

            }

            return RedirectToAction("Configuracion");
        }

    }
}