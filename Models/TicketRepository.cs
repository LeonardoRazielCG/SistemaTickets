namespace SistemaTickets.Models
{
    public class TicketRepository
    {
        public static List<Ticket> Tickets { get; set; } = new List<Ticket>
        {
            new Ticket
            {
                Id = 101,
                Titulo = "Fallo en impresora de almacén",
                Categoria = "Hardware",
                Prioridad = "Media",
                Descripcion = "La impresora no responde al enviar documentos desde la terminal 2.",
                Estado = "En Proceso",
                FechaCreacion = DateTime.Now.AddDays(-1),
                UsuarioEmail = "usuario@cliente.com",
                AsignadoA = "Soporte IT",
                Comentarios = new List<Comentario>
                {
                    new Comentario { Autor = "Admin", Texto = "Revisando conexion IP local.", Fecha = DateTime.Now.AddHours(-2) }
                }
            }
        };
    }
}
