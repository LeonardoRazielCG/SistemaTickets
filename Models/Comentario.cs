using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTickets.Models
{
    [Table("Comentarios")]
    public class Comentario
    {
        [Key]
        public int Id { get; set; }

        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        public string Autor { get; set; } = string.Empty;

        public string Texto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;
    }
}