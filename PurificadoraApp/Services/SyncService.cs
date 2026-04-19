using Supabase;
using PurificadoraApp.Models;
using SQLite;
using System.Text.Json;
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace PurificadoraApp.Services
{
    // Clase que representa la tabla 'entregas' en Supabase
    [Table("entregas")]
    public class EntregaRemota : BaseModel
    {
        [PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Column("repartidor_id")]
        public string RepartidorId { get; set; } = string.Empty;

        [Column("repartidor_nombre")]
        public string RepartidorNombre { get; set; } = string.Empty;

        [Column("cliente_nombre")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Column("direccion")]
        public string Direccion { get; set; } = string.Empty;

        [Column("cantidad_garrafones")]
        public int CantidadGarrafones { get; set; }

        [Column("fecha_hora_registro")]
        public DateTime FechaHoraRegistro { get; set; }

        [Column("version")]
        public int Version { get; set; }
    }

    public class SyncService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly LocalDbService _localDbService;
        private readonly SQLiteAsyncConnection _database;

        public SyncService(Supabase.Client supabaseClient, LocalDbService localDbService)
        {
            _supabaseClient = supabaseClient;
            _localDbService = localDbService;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "purificadora.db3");
            _database = new SQLiteAsyncConnection(dbPath);
        }

        // Verificar si hay conexión a internet
        public async Task<bool> HasInternetConnection()
        {
            try
            {
                var current = Connectivity.Current;
                return current.NetworkAccess == NetworkAccess.Internet;
            }
            catch
            {
                return false;
            }
        }

        // Sincronizar entregas pendientes (SUBIR)
        public async Task<int> SyncPendingDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            var pendientes = await _localDbService.GetEntregasPendientes();
            int sincronizados = 0;

            foreach (var entregaLocal in pendientes)
            {
                try
                {
                    // Crear el objeto para Supabase usando la clase tipada
                    var entregaRemota = new EntregaRemota
                    {
                        RepartidorId = entregaLocal.RepartidorId,
                        RepartidorNombre = entregaLocal.RepartidorNombre,
                        ClienteNombre = entregaLocal.ClienteNombre,
                        Direccion = entregaLocal.Direccion,
                        CantidadGarrafones = entregaLocal.CantidadGarrafones,
                        FechaHoraRegistro = entregaLocal.FechaHoraRegistro,
                        Version = entregaLocal.Version + 1
                    };

                    // Insertar en Supabase - CORREGIDO
                    var response = await _supabaseClient.From<EntregaRemota>().Insert(entregaRemota);

                    // Actualizar estado local
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 1, response.Model?.Id);
                    sincronizados++;
                }
                catch (Exception ex)
                {
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 2, errorMessage: ex.Message);
                }
            }

            return sincronizados;
        }

        // Descargar entregas del administrador (BAJAR)
        public async Task<int> SyncAdminDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            try
            {
                // Obtener entregas de Supabase (solo últimas 30 días)
                var desde = DateTime.Now.AddDays(-30);

                // CORREGIDO: usar el tipo EntregaRemota
                var response = await _supabaseClient
                    .From<EntregaRemota>()
                    .Where(x => x.FechaHoraRegistro >= desde)
                    .Get();

                var entregasRemotas = response.Models;
                int descargados = 0;

                foreach (var item in entregasRemotas)
                {
                    // Guardar en SQLite local (como referencia para el repartidor)
                    var entregaLocal = new EntregaLocal
                    {
                        IdRemoto = item.Id,
                        RepartidorId = item.RepartidorId,
                        RepartidorNombre = item.RepartidorNombre,
                        ClienteNombre = item.ClienteNombre,
                        Direccion = item.Direccion,
                        CantidadGarrafones = item.CantidadGarrafones,
                        FechaHoraRegistro = item.FechaHoraRegistro,
                        EstadoSync = 1, // Ya sincronizado
                        Version = item.Version
                    };

                    await _localDbService.GuardarEntrega(entregaLocal);
                    descargados++;
                }

                return descargados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descargar: {ex.Message}");
                return 0;
            }
        }

        // Sincronización completa
        public async Task<(int subidos, int bajados)> SyncAll()
        {
            var subidos = await SyncPendingDeliveries();
            var bajados = await SyncAdminDeliveries();
            return (subidos, bajados);
        }
    }
}