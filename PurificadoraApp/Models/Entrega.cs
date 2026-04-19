using SQLite;
using System;

namespace PurificadoraApp.Models
{
    public class EntregaLocal
    {
        [PrimaryKey, AutoIncrement]
        public int IdLocal { get; set; }

        public string? IdRemoto { get; set; }  // UUID de Supabase

        public string RepartidorId { get; set; } = string.Empty;
        public string RepartidorNombre { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public int CantidadGarrafones { get; set; }
        public DateTime FechaHoraRegistro { get; set; }
        public DateTime? FechaSincronizacion { get; set; }

        // Estado de sincronización: 0=Pendiente, 1=Sincronizado, 2=Error
        public int EstadoSync { get; set; }

        public string? ErrorMessage { get; set; }
        public int Version { get; set; }
    }
}