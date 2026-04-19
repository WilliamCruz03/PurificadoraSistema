namespace PurificadoraApp.Models
{
    public class UsuarioSesion
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;  // "Repartidor" o "Admin"
        public DateTime FechaInicio { get; set; }
    }
}