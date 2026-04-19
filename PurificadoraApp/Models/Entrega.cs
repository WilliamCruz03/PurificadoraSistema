using SQLite;

namespace PurificadoraApp.Models
{
    public class EntregaLocal
    {
        [PrimaryKey, AutoIncrement]
        public int IdLocal { get; set; }

        public string? IdRemoto { get; set; }

        // Referencia al cliente
        public string? ClienteId { get; set; }

        // Campos para mostrar (pueden venir del cliente)
        public string ClienteNombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;

        public string RepartidorId { get; set; } = string.Empty;
        public string RepartidorNombre { get; set; } = string.Empty;

        public int CantidadGarrafones { get; set; }
        public DateTime FechaHoraRegistro { get; set; }
        public DateTime? FechaSincronizacion { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Estado de sincronización: 0=Pendiente, 1=Sincronizado, 2=Error
        public int EstadoSync { get; set; }

        public string? ErrorMessage { get; set; }
        public int Version { get; set; }
    }
}