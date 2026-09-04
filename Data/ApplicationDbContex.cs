using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SistemaTickets.Models;

namespace SistemaTickets.Data
{
    public class ApplicationDbContex : DbContext
    {
        public ApplicationDbContex(DbContextOptions<ApplicationDbContex> options) 
            :base(options)
            { }

        public DbSet<Usuario> Usuarios { get; set; }    
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
    }
}
