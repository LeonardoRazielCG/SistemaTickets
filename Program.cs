using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SistemaTickets.Models;
using SistemaTickets.Data;


var builder = WebApplication.CreateBuilder(args);

// Aderir servicios al contenedor
builder.Services.AddControllersWithViews();


//REGISTRAR DBNCONTEXT PARA ENTITY FRAMEWORK Y SQL SERVER
builder.Services.AddDbContext<ApplicationDbContex>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// 1. REGISTRAR SERVICIO DE AUTENTICACIÓN POR COOKIES
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // Ruta si el usuario no está autenticado
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Duración de la sesión
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. ACTIVAR AUTENTICACIÓN Y AUTORIZACIÓN (En este orden específico)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();