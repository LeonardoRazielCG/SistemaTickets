using System.ComponentModel.DataAnnotations;


namespace SistemaTickets.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El correo o usuario es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
