using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System;

namespace SistemaTickets.Models
{
    [Table("Ticket")]
    public class Ticket
    {
        [Key]
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Prioridad { get; set; } = "Media";
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = "Abierto";
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public string UsuarioEmail { get; set; } = string.Empty;

        public string? AsignadoA { get; set; } = "Sin asignar";
        public List<Comentario> Comentarios { get; set; } = new List<Comentario>();

    }

   
}