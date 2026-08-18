using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Data;
using SistemaTickets.Models;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    public class AccountController : Controller
    {
        //Decalramos la referencia al DBContext para comunicarnos con la BD
        private readonly ApplicationDbContex _context;

        //inyectmso ApplicationDbContext mediante el constructor del controlador 
        public AccountController(ApplicationDbContex context)
        {
            _context = context;
        }

        // GET: /Account/Login
        //Muestra la vista del formulario del inicio de sesion.
        [HttpGet]
        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, lo mandamos al dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                { 
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction("Index", "Usuario");
            }
            return View();
        }

        // POST: /Account/Login
        //procesa los datos enviados desde el formulario de login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Validar que las reglas del ViewModel (campos requeridos, etc.) se cumplan
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Normalizamos el correo ingresado eliminando espacios accidentales
            string emailIngresado = model.Email.Trim().ToLower() ?? "";
            string passwordIngresada = model.Password.Trim() ?? "";

            /*consulta a sql server mediante LINQ y Entity Fraamework
              buxca un usuario donde el correo coincida y el estatus sea 'Activo'*/
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(
                u => u.Correo.ToLower() == emailIngresado
                && u .Contrasena == passwordIngresada);

            //Si no encuentra coincidencia en la BD
            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Correo o contraseña incorrecta");
                return View();
            }

            if (!string.Equals(usuario.Estatus, "Activo", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "El usuario se encuentra inactivo.");
                return View(model);
            }

            //Aqui se van a crear las identidades Claims incluyendo el rol detectado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre ?? usuario.Correo),
                new Claim(ClaimTypes.Email, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol ?? "Usuario") // <-- Asigna el rol Admin o user registrado en el SQL

            };

            //Crea la identidad asociada al esquema de autenticación por cookies
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe, // Mantiene la cookie si se marcó "Recordar mi sesión"
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            // Iniciar sesión creando la Cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Redirección según el Rol extraído de SQL Server
            if (string.Equals(usuario.Rol, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Create", "Tickets");
        }

        // GET: /Account/Logout
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}