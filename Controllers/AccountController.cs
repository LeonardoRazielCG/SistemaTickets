using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaTickets.Models;
using System.Security.Claims;

namespace SistemaTickets.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, lo mandamos al dashboard
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // SIMULACIÓN DE VALIDACIÓN (Posteriormente podremos conectar la BD)
            string userRole = "";
            string userName = "";

            if (model.Email == "admin@soporte.com" && model.Password == "Admin123")
            {
                userRole = "Admin";
                userName = "Leonardo Castro";
            }

            else if (model.Email == "usuario@cliente.com" && model.Password == "User123")
            {
                userRole = "User";
                userName = "Don Chano";
            }
            
            else
            {
                // Si las credenciales no coinciden con ninguno 
                ModelState.AddModelError(string.Empty, "Usuario o contraseña Incorrecos");
                return View(model);
            }

            //Aqui se van a crear las identidades Claims incluyendo el rol detectado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, userRole) // <--- aqui se debe de guardar si es Admin o es User

            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authPropierties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
                authPropierties);

            if (userRole == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Create","Tickets");
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}