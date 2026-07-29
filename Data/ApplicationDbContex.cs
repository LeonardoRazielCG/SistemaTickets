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
            public DbSet<Ticket> Ticket { get; set; }
    }
}
