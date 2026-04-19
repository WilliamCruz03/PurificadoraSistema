using SQLite;

namespace PurificadoraApp.Models
{
    public class Cliente
    {
        [PrimaryKey]
        public string Id { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;
        public string? Apellidos { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Propiedad calculada para nombre completo
        public string NombreCompleto => string.IsNullOrWhiteSpace(Apellidos)
            ? Nombre
            : $"{Nombre} {Apellidos}";
    }
}