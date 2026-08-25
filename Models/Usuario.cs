using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaTickets.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        public int Nomina { get; set; }
        public string? Departamento { get; set; }
        public string? Nombre { get; set; }

        [Required]
        public string Correo { get; set; }
        public string? NombreUsuario { get; set; }
        public string? Contrasena { get; set; }
        public string Estatus { get; set; } = "Activo";
        public string Rol { get; set; } = "Usuario";

        public string? FotoPerfilUrl { get; set; }
    }
}
