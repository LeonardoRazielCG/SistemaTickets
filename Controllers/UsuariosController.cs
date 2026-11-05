using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Data;
using SistemaTickets.Models;


namespace SistemaTickets.Controllers
{

    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly ApplicationDbContex _context;

        public UsuariosController(ApplicationDbContex context)
        {
            _context = context;
        }

        //Lista los usuarios 
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            return View(usuarios);
        }

        //Edita datos de los usuarios 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, string nombre, string departamento, string rol, int nomina, string correo)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "El usuario no fue encontrado";
                return RedirectToAction("Configuracion", "Home");
            }
            try
            {
                usuario.Nombre = nombre;
                usuario.Departamento = departamento;
                usuario.Rol = rol;
                usuario.Nomina = nomina;
                usuario.Correo = correo;

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"El Usuario {usuario.Nombre} fue actualizado correctamente";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar el usuario" + ex.Message;
            }

            return RedirectToAction("Configuracion", "Home");
        }

        //esto hace que el administrador pueda restablecer la contraseña del usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarContrasena(int id, string nuevaContrasena)
        {
            if (string.IsNullOrWhiteSpace(nuevaContrasena))
            {
                TempData["Error"] = "La contraseña no puede estar vacia";
                return RedirectToAction("Configuracion", "Home");
            }

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "El usuario no fue encontrado";
                return RedirectToAction("Configuracion", "Home");
            }

            try
            {
                usuario.Contrasena = nuevaContrasena;

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Contraseña restablecida correctamente para {usuario.Nombre}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al restablecer la contraseña:" + ex.Message;
            }

            return RedirectToAction("Configuracion", "Home");
        }

        //Esto cambia el estatus de activo o inactivo en caso de que un usuario ya no este 
        //en la empresa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstatus(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "El usuario no fue encontrado";
                return RedirectToAction("Configuracion", "Home");
            }
            try
            {
                usuario.Estatus = (usuario.Estatus == "Activo") ? "Inactivo" : "Activo";

                _context.Update(usuario);
                await _context.SaveChangesAsync();

                TempData["Succes"] = $"El usuario {usuario.Nombre} cambio a {usuario.Estatus}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al caambiar estatus" + ex.Message;
            }
            return RedirectToAction("Configuracion", "Home");
        }

        //esta es la elimininacion del usuario pero hay que ver la manera de borrarlo aunque tenga tickets 
        // y se puedan visualizar de todas maneras
    }
    
}
